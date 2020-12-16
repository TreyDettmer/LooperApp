using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Looper : MonoBehaviour
{
    public Slider progressBar;
    public Image looperBackgroundImage;
    public Image bpmWarningImage;
    public Button playButton;

    public AudioSource audioSource0;
    public AudioSource audioSource1;
    int frequency = 44100;

    double playbackBuffer = .15f;


    double recordingStartTime = 100f;
    double recordingFirstBeatTime = 200f;

    double firstClipStartTime = 0f;
    double secondClipStartTime = 0f;
    bool bIsRecording = false;
    bool bHasClip = false;
    bool bPaused = true;
    int clipIndex = 0;
    bool bScheduledFirstClip = true;
    bool bScheduledSecondClip = false;
    double numberOfMeasures = 0;
    double recordedBPM = 120;

    Coroutine switchCoroutine0;
    Coroutine switchCoroutine1;

    // Start is called before the first frame update
    void Start()
    {
        
        
        if (Microphone.devices.Length <= 0)
        {
            Debug.LogWarning("Microphone not connected.");
            return;
        }
        playButton.enabled = false;
        bpmWarningImage.enabled = false;
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
            if (audioSource0.clip == null) { return; }
            double t0 = AudioSettings.dspTime;
            //stop recording and store recorded data into audio clip
            int uncutLength = Microphone.GetPosition(null);
            
            Microphone.End(null);
            float[] uncutClipData = new float[uncutLength];
            audioSource0.clip.GetData(uncutClipData, 0);
            double endTime = AudioSettings.dspTime;
            //Debug.Log($"Uncut samples: {uncutLength} Uncut Time: {uncutLength / frequency} seconds");
            if (recordingFirstBeatTime - recordingStartTime < 0)
            {
                Debug.LogError("Recording somehow started after the downbeat.");
            }
            double beginningTimeToDiscard = recordingFirstBeatTime - recordingStartTime;
            //Debug.Log($"Beginning Time to discard: {(beginningTimeToDiscard).ToString("F4")} seconds");
            int beginningSamplesToDiscard = Mathf.RoundToInt((float)(frequency * beginningTimeToDiscard));
            //Debug.Log($"Beginning Samples to discard: {beginningSamplesToDiscard} Beginning Time to discard: {(beginningTimeToDiscard).ToString("F4")} seconds");
            //Debug.Log($"PreviousDownBeatTime: {Station.instance.previousDownBeatTime} EndTime: {endTime}");
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
                Debug.LogWarning("Failed to create clip: Did not record long enough.");
                Station.instance.bRecordingALoop = false;
                return;
            }
            int endSamplesToDiscard = Mathf.Max(0,Mathf.RoundToInt((float)(frequency * endTimeToDiscard)) - 400);
            //Debug.Log($"CurrentTime: {AudioSettings.dspTime} PreviousDownBeatTime: {Station.instance.previousDownBeatTime}");
            //Debug.Log($"End samples to discard: {endSamplesToDiscard}  End Time to discard: {(endTimeToDiscard).ToString("F4")} seconds");

            double t1 = AudioSettings.dspTime;
            //Debug.Log($"UncutLength: {uncutLength} BeginningSamplesToDiscard: {beginningSamplesToDiscard} EndSamplesToDiscard: {endSamplesToDiscard}");
            int clipLength = uncutLength - beginningSamplesToDiscard - endSamplesToDiscard;
            if (clipLength <= 0)
            {
                Debug.LogWarning("Failed to create clip: clip length is less than or equal to zero.");
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
            double t2 = AudioSettings.dspTime;
            
            audioSource0.clip = AudioClip.Create("clip0", clipData.Length, 1, frequency, false);
            audioSource0.clip.SetData(clipData, 0);
            audioSource1.clip = AudioClip.Create("clip1", clipData.Length, 1, frequency, false);
            audioSource1.clip.SetData(clipData, 0);
            //audioSource.PlayScheduled(Station.instance.nextDownBeatTime);
            //audioSource.PlayScheduled()
            numberOfMeasures = Mathf.Ceil((float)((clipData.Length / frequency) / Station.instance.timeBetweenDownBeats));
            Debug.Log($"NumberOfMeasures: {numberOfMeasures}");
            for (int i = 1; i <= 16; i++)
            {

            }
            double t3 = AudioSettings.dspTime;
            //Debug.Log($"t0:{t0.ToString("F4")} t1:{t1.ToString("F4")} t2:{t2.ToString("F4")} t3:{t3.ToString("F4")}");
            bHasClip = true;
            Station.instance.bRecordingALoop = false;
            playButton.enabled = true;
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
        audioSource0.Stop();
        audioSource1.Stop();
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
    //IEnumerator PlaybackStartDelay()
    //{
    //    yield return new WaitUntil(() => AudioSettings.dspTime - Station.instance.previousDownBeatTime <= .05f);

    //    //int samplesToSkipOver = Mathf.RoundToInt(frequency * (AudioSettings.dspTime - Station.instance.previousDownBeatTime));
    //    //audioSource.clip.
    //    //if (bHasClip && !audioSource.isPlaying)
    //    //{
    //    //    audioSource.Play();
    //    //}
    //}



    

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
        bScheduledFirstClip = false;
        bScheduledSecondClip = false;
        audioSource0.clip = null;
        audioSource1.clip = null;
        bpmWarningImage.enabled = false;
    }

    public void UpdatedBPM()
    {
        if (Station.instance.bpm != recordedBPM && bHasClip)
        {
            bpmWarningImage.enabled = true;
        }
        else
        {
            bpmWarningImage.enabled = false;
        }
    }

    public void DeleteLooper()
    {
        ClearLoop();
        Station.instance.loopers.Remove(this);
        Destroy(gameObject);
    }
}
