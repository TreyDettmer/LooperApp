using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Looper : MonoBehaviour
{
    public Slider progressBar;
    public Image looperBackgroundImage;
    public GameObject bpmWarningObject;
    public TMP_InputField trackNameInputField;
    public Button playButton;
    public TextMeshProUGUI recordButtonText;

    public AudioSource audioSource0;
    public AudioSource audioSource1;
    int frequency = 44100;

    double playbackBuffer = .15f;


    double recordingStartTime = 100f;
    double recordingFirstBeatTime = 200f;

    double firstClipStartTime = 0f;
    double secondClipStartTime = 0f;
    public bool bIsRecording = false;
    public bool bHasClip = false;
    public bool bPaused = true;
    int clipIndex = 0;
    bool bScheduledFirstClip = true;
    bool bScheduledSecondClip = false;
    public double numberOfMeasures = 0;
    public double recordedBPM = 120;
    public bool bIsPlaying = false;

    Coroutine switchCoroutine0;
    Coroutine switchCoroutine1;


    public string trackName = "testname";

    // Start is called before the first frame update
    void Start()
    {
        
        
        if (Microphone.devices.Length <= 0)
        {
            Debug.LogWarning("Microphone not connected.");
            return;
        }
        playButton.enabled = false;
        bpmWarningObject.SetActive(false);
    }



    public void Record()
    {
        
        bIsRecording = !bIsRecording;
        if (bIsRecording == true)
        {
            if (Station.instance.bRecordingALoop) {
                bIsRecording = false;
                return; 
            }
            recordButtonText.text = "Stop";
            recordedBPM = Station.instance.bpm;
            Station.instance.bRecordingALoop = true;
            audioSource0.Stop();
            audioSource1.Stop();
            Microphone.End(null);
            audioSource0.clip = null;
            audioSource1.clip = null;
            //start recording
            audioSource0.clip = Microphone.Start(null, true, 40, frequency);
            recordingStartTime = AudioSettings.dspTime;
            if (Station.instance.recordingCountIn == 1)
            {
                recordingFirstBeatTime = Station.instance.nextDownBeatTime;
            }
            else if (Station.instance.recordingCountIn == 2)
            {
                recordingFirstBeatTime = Station.instance.nextDownBeatTime + Station.instance.timeBetweenDownBeats;
            }
            else
            {
                recordingFirstBeatTime = Station.instance.nextDownBeatTime + 2 * Station.instance.timeBetweenDownBeats;
            }
            
            StartCoroutine(ChangeLooperBackgroundRoutine(recordingFirstBeatTime - recordingStartTime));




        }
        else
        {
            
            looperBackgroundImage.color = new Color(173f/255f, 173f/255f, 173f/255f, 222f/255f);
            recordButtonText.text = "Record";
            if (audioSource0.clip == null) { return; }

            //stop recording and store recorded data into audio clip
            int uncutLength = Microphone.GetPosition(null);
            
            Microphone.End(null);
            float[] uncutClipData = new float[uncutLength];
            audioSource0.clip.GetData(uncutClipData, 0);
            double endTime = AudioSettings.dspTime;

            if (recordingFirstBeatTime - recordingStartTime < 0)
            {
                Debug.LogError("Recording somehow started after the downbeat.");
            }
            double beginningTimeToDiscard = recordingFirstBeatTime - recordingStartTime;

            int beginningSamplesToDiscard = Mathf.RoundToInt((float)(frequency * beginningTimeToDiscard));

            double endTimeToDiscard;
            if (Station.instance.recordingCountOut == 1)
            {
                endTimeToDiscard = endTime - Station.instance.previousDownBeatTime;
            }
            else if (Station.instance.recordingCountOut == 2)
            {
                endTimeToDiscard = endTime - (Station.instance.previousDownBeatTime - Station.instance.timeBetweenDownBeats);
            }
            else
            {
                endTimeToDiscard = endTime - (Station.instance.previousDownBeatTime - 2 * Station.instance.timeBetweenDownBeats);
            }
            if (endTimeToDiscard < 0)
            {
                Station.instance.ShowErrorMessage("Failed to create clip: Did not record long enough.");
                Station.instance.bRecordingALoop = false;
                looperBackgroundImage.color = new Color(173f / 255f, 173f / 255f, 173f / 255f, 180f / 255f);
                return;
            }
            int endSamplesToDiscard = Mathf.Max(0,Mathf.RoundToInt((float)(frequency * endTimeToDiscard)) - 400);
;
            int clipLength = uncutLength - beginningSamplesToDiscard - endSamplesToDiscard;
            if (clipLength <= 0)
            {
                Station.instance.ShowErrorMessage("Failed to create clip: clip length is less than or equal to zero.");
                looperBackgroundImage.color = new Color(173f / 255f, 173f / 255f, 173f / 255f, 180f / 255f);
                Debug.Log($"SAMPLES: Uncutlength {uncutLength}" +
                    $" beginningSamplesToDiscard {beginningSamplesToDiscard} endSamplesToDiscard {endSamplesToDiscard} " +
                    $"CurrentTime {AudioSettings.dspTime} PreviousDownBeatTime {Station.instance.previousDownBeatTime}");
                Station.instance.bRecordingALoop = false;
                return;
            }
            //Debug.Log(clipLength);
            float[] clipData = new float[clipLength];
            
            for (int i = 0; i < clipLength; i++)
            {
                if (beginningSamplesToDiscard + i < uncutLength)
                {
                    clipData[i] = uncutClipData[beginningSamplesToDiscard + i];
                }
            }
            
            audioSource0.clip = AudioClip.Create("clip0", clipData.Length, 1, frequency, false);
            audioSource0.clip.SetData(clipData, 0);
            audioSource1.clip = AudioClip.Create("clip1", clipData.Length, 1, frequency, false);
            audioSource1.clip.SetData(clipData, 0);

            numberOfMeasures = Mathf.Ceil((float)((clipData.Length / frequency) / Station.instance.timeBetweenDownBeats));
            
            bHasClip = true;
            Station.instance.bRecordingALoop = false;
            playButton.enabled = true;
            looperBackgroundImage.color = new Color(173f / 255f, 173f / 255f, 173f / 255f, 222f / 255f);
        }
    }

    IEnumerator ChangeLooperBackgroundRoutine(double wait)
    {
        yield return new WaitForSeconds((float)wait);
        looperBackgroundImage.color = new Color(1f, 0f, 0f, 222f / 255f);
    }

    public void Playback()
    {
        if (bHasClip)
        {
            if (bPaused)
            {
                //make sure that we have at least 0.6 seconds to prepare to play
                if (Station.instance.nextDownBeatTime - AudioSettings.dspTime >= .6)
                {
                    bIsPlaying = true;
                    bPaused = false;
                    clipIndex = 0;
                    if (switchCoroutine0 != null)
                    {
                        StopCoroutine(switchCoroutine0);
                    }
                    if (switchCoroutine1 != null)
                    {
                        StopCoroutine(switchCoroutine1);
                    }
                    audioSource0.PlayScheduled(Station.instance.nextDownBeatTime - playbackBuffer);
                    firstClipStartTime = Station.instance.nextDownBeatTime - playbackBuffer;
                    StartCoroutine(PlaybackLoop());
                }
            }
            else
            {
                Pause();
            }
        }

    }



    void Pause()
    {
        bPaused = true;
        if (switchCoroutine0 != null)
        {
            StopCoroutine(switchCoroutine0);
        }
        if (switchCoroutine1 != null)
        {
            StopCoroutine(switchCoroutine1);
        }
        if (clipIndex == 0)
        {
            audioSource0.SetScheduledEndTime(Station.instance.nextDownBeatTime);
            audioSource1.Stop();
        }
        else
        {
            audioSource1.SetScheduledEndTime(Station.instance.nextDownBeatTime);
            audioSource0.Stop();
        }
        bIsPlaying = false;
        bScheduledFirstClip = false;
        bScheduledSecondClip = false;
        
    }
    
    IEnumerator PlaybackLoop()
    {
        while (bHasClip && !bPaused)
        {
            if (clipIndex == 0)
            {
                //if the first clip has started playing
                if (AudioSettings.dspTime - firstClipStartTime >= 0.1)
                {

                    if (bScheduledSecondClip == false)
                    {
                        audioSource0.SetScheduledEndTime(Station.instance.nextDownBeatTime + Station.instance.timeBetweenDownBeats * numberOfMeasures);
                        audioSource1.PlayScheduled(Station.instance.nextDownBeatTime + (Station.instance.timeBetweenDownBeats * numberOfMeasures) - playbackBuffer);
                        //if (bInitializedSecondClip)
                        //{
                        //    audioSource1.SetScheduledStartTime(Station.instance.nextDownBeatTime - .15);
                        //}
                        //else
                        //{
                        //    audioSource1.PlayScheduled(Station.instance.nextDownBeatTime - .15);
                        //    bInitializedSecondClip = true;
                        //}
                            
                        secondClipStartTime = Station.instance.nextDownBeatTime + (Station.instance.timeBetweenDownBeats * numberOfMeasures) - playbackBuffer;
                        bScheduledSecondClip = true;
                        bScheduledFirstClip = false;
                        switchCoroutine1 = StartCoroutine(SwitchClipIndex((float)(secondClipStartTime - AudioSettings.dspTime)));

                    }
                    
                }
            }
            else
            {
                //if the second clip has started playing
                if (AudioSettings.dspTime - secondClipStartTime >= 0.1)
                {

                    if (bScheduledFirstClip == false)
                    {
                        audioSource1.SetScheduledEndTime(Station.instance.nextDownBeatTime + Station.instance.timeBetweenDownBeats * numberOfMeasures);
                        //audioSource0.SetScheduledStartTime(Station.instance.nextDownBeatTime - .15);
                        audioSource0.PlayScheduled(Station.instance.nextDownBeatTime + (Station.instance.timeBetweenDownBeats * numberOfMeasures) - playbackBuffer);
                            
                        firstClipStartTime = Station.instance.nextDownBeatTime + (Station.instance.timeBetweenDownBeats * numberOfMeasures) - playbackBuffer;
                        bScheduledFirstClip = true;
                        bScheduledSecondClip = false;
                        switchCoroutine0 = StartCoroutine(SwitchClipIndex((float)(firstClipStartTime - AudioSettings.dspTime)));
                        
                            
                    }
                    
                }
            }
            yield return null;
        }
    }




    

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(AudioSettings.dspTime);
        if (Microphone.IsRecording(null) && bIsRecording)
        {
            if (AudioSettings.dspTime >= recordingFirstBeatTime)
            {
                progressBar.value = (float)(AudioSettings.dspTime - recordingFirstBeatTime) / 40;
            }

        }
        else
        {
            if (clipIndex == 0 && bHasClip)
            {
                int timeSamples = audioSource0.timeSamples;
                int clipLength = audioSource0.clip.samples;
                float percent = (float)timeSamples / clipLength;
                progressBar.value = percent;
            }
            if (clipIndex == 1 && bHasClip)
            {
                int timeSamples = audioSource1.timeSamples;
                int clipLength = audioSource1.clip.samples;
                float percent = (float)timeSamples / clipLength;
                progressBar.value = percent;
            }
            if (!audioSource0.isPlaying && !audioSource1.isPlaying)
            {
                if (progressBar.value != 0)
                {
                    progressBar.value = 0;
                }
            }
        }

    }

    public IEnumerator SwitchClipIndex(float timeToWait)
    {
        yield return new WaitForSeconds(timeToWait);
        if (!bPaused)
        {
            clipIndex = 1 - clipIndex;
        }
    }



    public void ClearLoop()
    {
        bHasClip = false;
        bIsRecording = false;
        playButton.enabled = false;
        audioSource0.Stop();
        audioSource1.Stop();
        if (switchCoroutine0 != null)
        {
            StopCoroutine(switchCoroutine0);
        }
        if (switchCoroutine1 != null)
        {
            StopCoroutine(switchCoroutine1);
        }
        looperBackgroundImage.color = new Color(173f / 255f, 173f / 255f, 173f / 255f, 180f / 255f);
        bScheduledFirstClip = false;
        bScheduledSecondClip = false;
        audioSource0.clip = null;
        audioSource1.clip = null;
        bpmWarningObject.SetActive(false);
    }

    public void UpdatedBPM()
    {
        if (Station.instance.bpm != recordedBPM && bHasClip)
        {
            bpmWarningObject.SetActive(true);           
        }
        else
        {
            bpmWarningObject.SetActive(false);
        }
    }

    public void DeleteLooper()
    {
        ClearLoop();
        Station.instance.loopers.Remove(this);
        Destroy(gameObject);
    }

    public void SetTrackName()
    {
        if (!string.IsNullOrWhiteSpace(trackNameInputField.text))
        {
            trackName = trackNameInputField.text;
        }
        else
        {
            
            trackNameInputField.text = trackName;
        }
    }

    public void SetAudioClip(AudioClip clip)
    {
        audioSource0.clip = clip;
        audioSource1.clip = clip;
        bHasClip = true;
        playButton.enabled = true;
        looperBackgroundImage.color = new Color(173f / 255f, 173f / 255f, 173f / 255f, 222f / 255f);
    }
}
