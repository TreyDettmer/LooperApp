using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class Station : MonoBehaviour
{

    public static Station instance;


    [HideInInspector]
    public double bpm = 140.0F;
    [HideInInspector]
    public float gain = 0.5F;
    [HideInInspector]
    public int signatureHi = 4;
    [HideInInspector]
    public int signatureLo = 4;
    [HideInInspector]
    public double nextTick = 0.0F;
    private double sampleRate = 0.0F;
    [HideInInspector]
    public int accent;
    private bool running = false;
    [HideInInspector]
    public double startTick;
    public double nextDownBeatTime;
    [HideInInspector]
    public double nextUpBeatTime;
    public double currentDownBeatTime;
    public double currentTime;
    public double previousDownBeatTime;
    [HideInInspector]
    public double currentUpBeatTime;
    [HideInInspector]
    public double previousUpBeatTime;

    public AudioSource metronomeLow;
    public AudioSource metronomeHigh;
    public TextMeshProUGUI countInButtonText;
    public TextMeshProUGUI countOutButtonText;
    public GameObject looperPrefab;
    public Transform backgroundPanel;
    public int beatCount = 0;

    public bool bRecordingALoop = false;

    public double timeBetweenDownBeats;

    public int recordingCountIn = 1;
    public int recordingCountOut = 1;


    public TMP_InputField bpmInputField;
    [HideInInspector]
    public bool bUseMetronome = true;

    bool bScheduledNextDownBeatTime = false;
    bool bScheduledNextUpBeatTime = false;

    public List<Looper> loopers = new List<Looper>();

    /// <summary>
    /// Initialize singleton
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

        }
        else if (instance != this)
        {
            Destroy(this);
        }
    }

    void Start()
    {
        //initialize metronome values
        accent = signatureHi;
        double startTick = AudioSettings.dspTime;
        sampleRate = AudioSettings.outputSampleRate;
        nextTick = startTick * sampleRate;
        nextDownBeatTime = AudioSettings.dspTime + 2;
        nextUpBeatTime = nextDownBeatTime;
        timeBetweenDownBeats = (60 * signatureHi) / bpm;
        Looper[] loopersToAdd = FindObjectsOfType<Looper>();
        foreach(Looper looper in loopersToAdd)
        {
            if (!loopers.Contains(looper))
            {
                loopers.Add(looper);
            }
        }
        running = true;
    }

    private void Update()
    {
        if (!running)
        {
            return;
        }
        currentTime = AudioSettings.dspTime;
        double time = AudioSettings.dspTime;
        if (!bScheduledNextDownBeatTime)
        {
            ScheduleNextDownBeatTime();
        }
        //if (time + .3f > nextDownBeatTime)
        //{
        //    if (bUseMetronome)
        //    {
        //        metronomeLow.PlayScheduled(nextDownBeatTime);
        //    }
        //    previousDownBeatTime = currentDownBeatTime;
        //    currentDownBeatTime = nextDownBeatTime;
        //    nextDownBeatTime += 60.0f / bpm * signatureHi;

        //}
        if (!bScheduledNextUpBeatTime)
        {
            ScheduleNextUpBeatTime();
        }

        //if (time + .1f > nextUpBeatTime)
        //{


        //    if (beatCount % 4 != 0)
        //    {
        //        if (bUseMetronome)
        //        {
        //            metronomeHigh.PlayScheduled(nextUpBeatTime);
        //        }
        //    }
        //    previousUpBeatTime = currentUpBeatTime;
        //    currentUpBeatTime = nextUpBeatTime;
        //    nextUpBeatTime += 60.0f / (bpm * 4) * signatureHi;
            
        //}
    }

    public void ChangeBPM()
    {
        int tempBPM;

        int.TryParse(bpmInputField.text,System.Globalization.NumberStyles.Integer,null, out tempBPM);
        if (tempBPM > 0)
        {
            bpm = tempBPM;
            timeBetweenDownBeats = (60 * signatureHi) / bpm;
            nextDownBeatTime = AudioSettings.dspTime + 2.0f;
            nextUpBeatTime = nextDownBeatTime;
            beatCount = 0;
            foreach (Looper looper in loopers)
            {
                looper.UpdatedBPM();
            }
        }

    }

    public void SetMetronome()
    {
        bUseMetronome = !bUseMetronome;
    }

    public void ScheduleNextDownBeatTime()
    {
        bScheduledNextDownBeatTime = true;
        if (bUseMetronome)
        {
            metronomeHigh.PlayScheduled(nextDownBeatTime);
        }
        //Debug.Log("Scheduled");
        Invoke("ResetDownBeatTimeBool",(float)(nextDownBeatTime + .1 - AudioSettings.dspTime));
    }

    public void ScheduleNextUpBeatTime()
    {
        
        bScheduledNextUpBeatTime = true;
        

        if (bUseMetronome)
        {
            metronomeLow.PlayScheduled(nextUpBeatTime);
        }
        
        //beatCount++;
        
        Invoke("ResetUpBeatTimeBool", (float)(nextUpBeatTime + .1 - AudioSettings.dspTime));
    }

    public void ResetDownBeatTimeBool()
    {
        previousDownBeatTime = nextDownBeatTime;
        nextDownBeatTime += 60.0f / bpm * signatureHi;
        bScheduledNextDownBeatTime = false;
        //Debug.Log("Reset");
    }

    public void ResetUpBeatTimeBool()
    {
        
        previousUpBeatTime = nextUpBeatTime;
        nextUpBeatTime += 60.0f / (bpm * 4) * signatureHi;
        bScheduledNextUpBeatTime = false;
    }

    public void PressedCountInButton()
    {
        if (bRecordingALoop) { return; }
        if (recordingCountIn == 3)
        {
            recordingCountIn = 1;
        }
        else
        {
            recordingCountIn++;
        }
        countInButtonText.text = recordingCountIn.ToString();
    }

    public void PressedCountOutButton()
    {
        if (bRecordingALoop) { return; }
        if (recordingCountOut == 3)
        {
            recordingCountOut = 1;
        }
        else
        {
            recordingCountOut++;
        }
        countOutButtonText.text = recordingCountOut.ToString();
    }

    public void AddLooper()
    {
        if (loopers.Count < 5)
        {
            Looper newLooper = Instantiate(looperPrefab, backgroundPanel).GetComponent<Looper>();
            loopers.Add(newLooper);
        }
    }


    ///// <summary>
    ///// Generates metronome beeps
    ///// </summary>
    ///// <param name="data">audio data</param>
    ///// <param name="channels">number of channels of audio</param>
    //void OnAudioFilterRead(float[] data, int channels)
    //{
    //    if (!running)
    //        return;

    //    double samplesPerTick = sampleRate * 60.0F / bpm * 4.0F / signatureLo;
    //    double sample = AudioSettings.dspTime * sampleRate;
    //    int dataLen = data.Length / channels;
        
    //    int n = 0;
    //    while (n < dataLen)
    //    {
    //        float x = gain * amp * Mathf.Sin(phase);
    //        int i = 0;
    //        while (i < channels)
    //        {
    //            data[n * channels + i] += x;
    //            i++;
    //        }
            
    //        while (sample + n >= nextTick)
    //        {
    //            nextTick += samplesPerTick;
    //            amp = 1.0F;
    //            if (++accent > signatureHi)
    //            {
    //                nextDownBeatTime = AudioSettings.dspTime + 4 * (60 / bpm);
    //                previousDownBeatTime = nextDownBeatTime - 4 * (60 / bpm);
    //                //Debug.Log($"Current Time: {AudioSettings.dspTime} Beat Time: {previousDownBeatTime}");
    //                Debug.Log($"Current Beat: {previousDownBeatTime}  Next Beat: {nextDownBeatTime}");
    //                accent = 1;
    //                amp *= 2.0F;
    //            }
                
    //            //Debug.Log("Tick: " + accent + "/" + signatureHi);
    //        }
    //        phase += amp * 0.3F;
    //        amp *= 0.993F;
    //        n++;
    //    }
    //}

}
