using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables; 
using UnityEngine.UI; 
using System.Collections.Generic;

[System.Serializable]
public class NoteEvent { 
    public float time; 
    public int laneIndex; 
    public float holdLength; 
}

public class GameplayManager : MonoBehaviour
{
    [Header("Audio & Sync")]
    public AudioSource musicSource;   
    public PlayableDirector director; 
    public Slider progressSlider; 

    [Header("Score References")]
    public PlayerScoreManager p1ScoreManager; // Drag P1 UI here
    public PlayerScoreManager p2ScoreManager; // Drag P2 UI here (optional)

    [Header("Timing Settings")]
    public float noteSpeed = 300f;    
    public float bpm = 120f;            
    public bool holdsAreInBeats = true; 
    public float startDelay = 3.0f;   
    public float manualLatencyAdjustment = 0.0f; 

    [Header("Lane References")]
    public LaneController p1Left;
    public LaneController p1Down;
    public LaneController p1Up;
    public LaneController p1Right;
    
    [Header("Player 2 Panel")]
    public GameObject p2Panel; 
    public LaneController p2Left;
    public LaneController p2Down;
    public LaneController p2Up;
    public LaneController p2Right;

    [Header("Chart Data")]
    public TextChartLoader textLoader; 
    public List<NoteEvent> songChart = new List<NoteEvent>();

    [Header("Song Settings")]
public SongGradeData thisSongGrades; // Drag your created SongGrade file here

    private double dspSongStartTime;
    private bool musicStarted = false;
    public float spawnOffset; 
    private int currentNoteIndex = 0;
    private float secondsPerBeat; 
    
    // Static Data Class to carry scores to Results Screen
    // (Ensure you have a GameSessionData.cs or class defined!)

    void Start()
    {
        // --- 1. Setup Inputs ---
        SetupPlayer1();
        
        if (SessionConfig.PlayerCount == 2) 
        {
            SetupPlayer2();
            if (p2Panel != null) p2Panel.SetActive(true);
        }
        else if (p2Panel != null)
        {
             p2Panel.SetActive(false);
        }

        // --- 2. Load Chart ---
        if (textLoader != null) songChart = textLoader.LoadChart();
        secondsPerBeat = 60f / bpm;

        // --- 3. Calculate Spawn Offset ---
        RectTransform receptorRect = p1Left.GetComponent<RectTransform>();
        RectTransform spawnRect = p1Left.spawnPoint.GetComponent<RectTransform>();
        
        float safeSpeed = (noteSpeed > 0) ? noteSpeed : 300f;
        float pixelDistance = Mathf.Abs(receptorRect.anchoredPosition.y - spawnRect.anchoredPosition.y);
        spawnOffset = (pixelDistance / safeSpeed) + manualLatencyAdjustment;

        // --- 4. Sync Speed ---
        p1Left.noteSpeed = safeSpeed; p1Down.noteSpeed = safeSpeed;
        p1Up.noteSpeed = safeSpeed;   p1Right.noteSpeed = safeSpeed;

        if (p2Left != null) {
            p2Left.noteSpeed = safeSpeed; p2Down.noteSpeed = safeSpeed;
            p2Up.noteSpeed = safeSpeed;   p2Right.noteSpeed = safeSpeed;
        }

        // --- 5. Start Music ---
        dspSongStartTime = AudioSettings.dspTime + startDelay;
        if (musicSource != null) musicSource.PlayScheduled(dspSongStartTime);
    }

    void SetupPlayer1()
    {
        InputDevice device = SessionConfig.Player1Device ?? Keyboard.current;
        string threshold = "[pressPoint=0.7]"; 

        if (device is Keyboard) 
        {
            p1Left.Setup(device,  new string[] { "<Keyboard>/leftArrow" }); 
            p1Down.Setup(device,  new string[] { "<Keyboard>/downArrow" });
            p1Up.Setup(device,    new string[] { "<Keyboard>/upArrow" });   
            p1Right.Setup(device, new string[] { "<Keyboard>/rightArrow" });
        } 
        else 
        {
            // Controller / Mat
            p1Left.Setup(device, new string[] { "<Gamepad>/dpad/left", $"<Gamepad>/leftStick/left{threshold}", $"<Joystick>/stick/left{threshold}" });
            p1Down.Setup(device, new string[] { "<Gamepad>/dpad/down", $"<Gamepad>/leftStick/down{threshold}", $"<Joystick>/stick/down{threshold}" });
            p1Up.Setup(device,   new string[] { "<Gamepad>/dpad/up",   $"<Gamepad>/leftStick/up{threshold}",   $"<Joystick>/stick/up{threshold}" });
            p1Right.Setup(device, new string[] { "<Gamepad>/dpad/right",$"<Gamepad>/leftStick/right{threshold}",$"<Joystick>/stick/right{threshold}" });
        }
    }
    
    void SetupPlayer2() 
    {
        InputDevice device = SessionConfig.Player2Device;
        if (device == null) device = Keyboard.current;

        string threshold = "[pressPoint=0.7]";

        if (device is Keyboard)
        {
            p2Left.Setup(device,  new string[] { "<Keyboard>/leftArrow" }); 
            p2Down.Setup(device,  new string[] { "<Keyboard>/downArrow" });
            p2Up.Setup(device,    new string[] { "<Keyboard>/upArrow" });   
            p2Right.Setup(device, new string[] { "<Keyboard>/rightArrow" });
        }
        else
        {
            p2Left.Setup(device, new string[] { "<Gamepad>/dpad/left", $"<Gamepad>/leftStick/left{threshold}", $"<Joystick>/stick/left{threshold}" });
            p2Down.Setup(device, new string[] { "<Gamepad>/dpad/down", $"<Gamepad>/leftStick/down{threshold}", $"<Joystick>/stick/down{threshold}" });
            p2Up.Setup(device,   new string[] { "<Gamepad>/dpad/up",   $"<Gamepad>/leftStick/up{threshold}",   $"<Joystick>/stick/up{threshold}" });
            p2Right.Setup(device, new string[] { "<Gamepad>/dpad/right",$"<Gamepad>/leftStick/right{threshold}",$"<Joystick>/stick/right{threshold}" });
        }
    }

    public double GetSongTime() { return AudioSettings.dspTime - dspSongStartTime; }

    void Update() {
        double songTime = GetSongTime();
        
        // Update Progress Slider
        if (progressSlider != null && musicSource != null && musicSource.clip != null)
            progressSlider.value = Mathf.Clamp01((float)songTime / musicSource.clip.length);

        // Start Timeline Director
        if (director != null && songTime >= 0 && !musicStarted) {
            musicStarted = true;
            director.Play();
        }
        
        CheckSpawns(songTime);
    }

    void CheckSpawns(double currentSongTime) {
        double lookAheadTime = currentSongTime + spawnOffset;
        while (currentNoteIndex < songChart.Count) {
            NoteEvent nextNote = songChart[currentNoteIndex];
            if (lookAheadTime >= nextNote.time) {
                float holdDur = holdsAreInBeats ? nextNote.holdLength * secondsPerBeat : nextNote.holdLength;
                SpawnNote(nextNote.laneIndex, holdDur, nextNote.time);
                currentNoteIndex++;
            } else break;
        }
    }

    void SpawnNote(int laneIndex, float duration, float noteTime) {
        // Player 1
        switch (laneIndex) {
            case 0: p1Left.SpawnNote(duration, noteTime, this); break;
            case 1: p1Down.SpawnNote(duration, noteTime, this); break;
            case 2: p1Up.SpawnNote(duration, noteTime, this); break;
            case 3: p1Right.SpawnNote(duration, noteTime, this); break;
        }

        // Player 2
        if (SessionConfig.PlayerCount == 2) {
             switch (laneIndex) {
                case 0: if(p2Left) p2Left.SpawnNote(duration, noteTime, this); break;
                case 1: if(p2Down) p2Down.SpawnNote(duration, noteTime, this); break;
                case 2: if(p2Up) p2Up.SpawnNote(duration, noteTime, this); break;
                case 3: if(p2Right) p2Right.SpawnNote(duration, noteTime, this); break;
            }
        }
    }

    // --- TIMELINE SIGNAL FUNCTION ---
   public void EndLevel()
{
    // Save Scores
    if (p1ScoreManager != null) 
    {
        GameSessionData.P1Score = p1ScoreManager.currentScore;
        // THIS IS THE KEY: Grab the max combo from the manager!
        GameSessionData.P1MaxCombo = p1ScoreManager.maxCombo; 
    }
    
    if (SessionConfig.PlayerCount == 2 && p2ScoreManager != null)
    {
        GameSessionData.P2Score = p2ScoreManager.currentScore;
        GameSessionData.P2MaxCombo = p2ScoreManager.maxCombo;
        GameSessionData.IsTwoPlayer = true;
    }
    GameSessionData.CurrentSongGrades = thisSongGrades;
    TransitionManager.Instance.LoadScene("ResultsScene");
}
        
    
}