using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.IO;
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

    public GameObject loadSessionMenu;
    public RectTransform savedFolderContent;
    public GameObject savedSessionPrefab;

    

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
        loadSessionMenu.SetActive(false);
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
        string currentBPM = ((int)bpm).ToString();
        int tempBPM;

        int.TryParse(bpmInputField.text,System.Globalization.NumberStyles.Integer,null, out tempBPM);
        if (tempBPM > 0)
        {
            if (tempBPM >= 40 && tempBPM <= 150)
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
            else
            {
                bpmInputField.text = currentBPM;
            }
        }
        else
        {
            bpmInputField.text = currentBPM;
        }

    }

    public void ChangeBPM(double newBPM)
    {
        if (newBPM != bpm)
        {
            bpm = newBPM;
            timeBetweenDownBeats = (60 * signatureHi) / bpm;
            nextDownBeatTime = AudioSettings.dspTime + 2.0f;
            nextUpBeatTime = nextDownBeatTime;
            beatCount = 0;
            foreach (Looper looper in loopers)
            {
                looper.UpdatedBPM();
            }
            bpmInputField.text = ((int)bpm).ToString();
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
            newLooper.trackName = "LooperTrack0" + (loopers.Count + 1).ToString();
            newLooper.SetTrackName();
            loopers.Add(newLooper);
        }
    }


    public void SaveData()
    {

        //create save folder
        if (loopers.Count == 0 || bRecordingALoop) { return; }
        string date = $"{DateTime.Now.Month}_{DateTime.Now.Day}_{DateTime.Now.Year}_{DateTime.Now.Hour},{DateTime.Now.Minute},{DateTime.Now.Second}";
        string savedLoopsFolderPath = Application.persistentDataPath + "/SavedLoops";
        //create the saved loops folder if it does not exist
        if (!File.Exists(savedLoopsFolderPath))
        {
            Directory.CreateDirectory(savedLoopsFolderPath);
        }
        string saveFolderPath = $"{savedLoopsFolderPath}/{date}";
        Directory.CreateDirectory(saveFolderPath);




        //write bpm info
        string infoFilePath = saveFolderPath + "/info.txt";
        if (!File.Exists(infoFilePath))
        {
            File.WriteAllText(infoFilePath, "");
        }
        File.AppendAllText(infoFilePath, ((int)bpm).ToString() + "\n");
        for (int i = 0; i < loopers.Count; i++)
        {
            if (loopers[i].bHasClip)
            {
                string looperInfo = loopers[i].trackName + "::" + ((int)loopers[i].recordedBPM).ToString() + ";;" + ((int)loopers[i].numberOfMeasures).ToString() + "\n";
                File.AppendAllText(infoFilePath, looperInfo);
            }
        }

        for (int i = 0; i < loopers.Count; i++)
        {
            if (loopers[i].bHasClip)
            {
                AudioClip clip = loopers[i].GetComponent<AudioSource>().clip;
                string path = saveFolderPath + "/" + loopers[i].trackName + ".wav";
                SavWav.Save(path, clip);
            }
            
        }
        
        

    }


    public void LoadData()
    {
        DisableLoadSessionMenu();
        string savedLoopsFolderPath = Application.persistentDataPath + "/SavedLoops";

        //return if there are no saved files
        if (!Directory.Exists(savedLoopsFolderPath)) { return; }
        RefreshSavedFolderContent();
        loadSessionMenu.SetActive(true);

    }

    public void RefreshSavedFolderContent()
    {
        string savedLoopsFolderPath = Application.persistentDataPath + "/SavedLoops";
        foreach (Transform child in savedFolderContent)
        {
            Destroy(child.gameObject);
        }
        loadSessionMenu.SetActive(true);
        try
        {
            string[] dir = Directory.GetDirectories(savedLoopsFolderPath);
            for (int i = 0; i < dir.Length; i++)
            {
                GameObject savedSession = Instantiate(savedSessionPrefab, savedFolderContent);
                savedSession.GetComponentInChildren<TextMeshProUGUI>().text = dir[i].Substring(dir[i].LastIndexOf("\\") + 1);
                int value = i;
                savedSession.GetComponent<Button>().onClick.AddListener(delegate { LoadPreviousSession(value); });
            }

        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    public void LoadPreviousSession(int sessionIndex)
    {
        string savedLoopsFolderPath = Application.persistentDataPath + "/SavedLoops";
        if (!Directory.Exists(savedLoopsFolderPath)) {
            DisableLoadSessionMenu();
            return; 
        }
        try
        {
            string[] dir = Directory.GetDirectories(savedLoopsFolderPath);
            if (sessionIndex < dir.Length)
            {
                if (!Directory.Exists(dir[sessionIndex]))
                {
                    return;
                }
                foreach (Looper looper in loopers)
                {
                    looper.ClearLoop();
                    Destroy(looper.gameObject);
                }
                loopers.Clear();
                StreamReader sr = new StreamReader(dir[sessionIndex] + "/info.txt");
                List<string> sessionInfo = new List<string>();
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    sessionInfo.Add(line);
                }
                double newBPM = Convert.ToDouble(sessionInfo[0]);
                for (int i = 1; i < sessionInfo.Count;i++)
                {
                    //create looper

                    string loopName = sessionInfo[i].Substring(0, sessionInfo[i].IndexOf("::"));
                    double loopBPM = Convert.ToDouble(sessionInfo[i].Substring(sessionInfo[i].IndexOf("::") + 2,sessionInfo[i].IndexOf(";;") - (sessionInfo[i].IndexOf("::") + 2)));
                    double loopMeasures = Convert.ToDouble(sessionInfo[i].Substring(sessionInfo[i].IndexOf(";;") + 2));
                    Looper newLooper = Instantiate(looperPrefab, backgroundPanel).GetComponent<Looper>();
                    newLooper.trackName = loopName;
                    newLooper.recordedBPM = loopBPM;
                    newLooper.numberOfMeasures = loopMeasures;
                    newLooper.SetTrackName();
                    loopers.Add(newLooper);
                }
                string[] files = Directory.GetFiles(dir[sessionIndex],"*wav");
                for (int i = 0; i < files.Length;i++)
                {

                    StartCoroutine(LoadAudio(files[i], i));
                    
                    
                }
                ChangeBPM(newBPM);

            }
            DisableLoadSessionMenu();

        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }



    private IEnumerator LoadAudio(string url, int looperIndex)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
        {
            yield return www.SendWebRequest();


            AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
            if (myClip == null)
            {
                Debug.Log("Failed to load clip");
            }
            else
            {
                myClip.name = loopers[looperIndex].trackName;
                loopers[looperIndex].SetAudioClip(myClip);
            }
            
        }
    }



    public void DisableLoadSessionMenu()
    {
        loadSessionMenu.SetActive(false);
    }
}
