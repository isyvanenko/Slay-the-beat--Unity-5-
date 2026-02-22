using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users; // Required for pairing
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
    [Header("Input System Rework")]
    public PlayerInput p1Input; 
    public PlayerInput p2Input; 

    [Header("Audio & Sync")]
    public AudioSource musicSource;   
    public PlayableDirector director; 
    public Slider progressSlider; 

    [Header("Score References")]
    public PlayerScoreManager p1ScoreManager; 
    public PlayerScoreManager p2ScoreManager; 

    [Header("Timing Settings")]
    public float noteSpeed = 300f;    
    public float bpm = 120f;            
    public bool holdsAreInBeats = true; 
    public float startDelay = 3.0f;   
    public float manualLatencyAdjustment = 0.0f; 

    [Header("Lane References")]
    public LaneController p1Left, p1Down, p1Up, p1Right;
    
    [Header("Player 2 Panel")]
    public GameObject p2Panel; 
    public LaneController p2Left, p2Down, p2Up, p2Right;

    [Header("Chart Data")]
    public TextChartLoader textLoader; 
    public List<NoteEvent> songChart = new List<NoteEvent>();

    [Header("Song Settings")]
    public SongGradeData thisSongGrades; 

    private double dspSongStartTime;
    private bool musicStarted = false;
    public float spawnOffset; 
    private int currentNoteIndex = 0;
    private float secondsPerBeat; 

    void Start()
    {
        // --- 0. Sync with Bridge ---
        if (GameDataBridge.SelectedSong != null)
        {
            thisSongGrades = GameDataBridge.SelectedSong;
        }

        // --- 1. Setup Inputs (LOCKING TO SESSION CONFIG) ---
        SetupPlayerInputs();

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
        p1Left.noteSpeed = p1Down.noteSpeed = p1Up.noteSpeed = p1Right.noteSpeed = safeSpeed;
        if (p2Left != null) p2Left.noteSpeed = p2Down.noteSpeed = p2Up.noteSpeed = p2Right.noteSpeed = safeSpeed;

        // --- 5. Start Music ---
        dspSongStartTime = AudioSettings.dspTime + startDelay;
        if (musicSource != null && musicSource.clip != null) 
            musicSource.PlayScheduled(dspSongStartTime);
    }

    void SetupPlayerInputs()
{
    // --- Player 1 ---
    if (p1Input != null && SessionConfig.Player1Device != null)
    {
        p1Input.user.UnpairDevices(); 
        // This pairs the DEVICE, regardless of what the scheme is named
        InputUser.PerformPairingWithDevice(SessionConfig.Player1Device, p1Input.user);
        
        p1Left.Initialize(p1Input.actions["Left"]);
        p1Down.Initialize(p1Input.actions["Down"]);
        p1Up.Initialize(p1Input.actions["Up"]);
        p1Right.Initialize(p1Input.actions["Right"]);

        Debug.Log($"<color=cyan>P1 Locked to Device ID:</color> {SessionConfig.Player1Device.deviceId}");
    }

    // --- Player 2 ---
    if (SessionConfig.PlayerCount == 2 && p2Input != null && SessionConfig.Player2Device != null)
    {
        p2Input.user.UnpairDevices();
        InputUser.PerformPairingWithDevice(SessionConfig.Player2Device, p2Input.user);

        p2Left.Initialize(p2Input.actions["Left"]);
        p2Down.Initialize(p2Input.actions["Down"]);
        p2Up.Initialize(p2Input.actions["Up"]);
        p2Right.Initialize(p2Input.actions["Right"]);
    }
}

    public double GetSongTime() { return AudioSettings.dspTime - dspSongStartTime; }

    void Update() 
    {
        double songTime = GetSongTime();
        if (progressSlider != null && musicSource != null && musicSource.clip != null)
            progressSlider.value = Mathf.Clamp01((float)songTime / musicSource.clip.length);

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
        switch (laneIndex) {
            case 0: p1Left.SpawnNote(duration, noteTime, this); if(p2Left && SessionConfig.PlayerCount == 2) p2Left.SpawnNote(duration, noteTime, this); break;
            case 1: p1Down.SpawnNote(duration, noteTime, this); if(p2Down && SessionConfig.PlayerCount == 2) p2Down.SpawnNote(duration, noteTime, this); break;
            case 2: p1Up.SpawnNote(duration, noteTime, this); if(p2Up && SessionConfig.PlayerCount == 2) p2Up.SpawnNote(duration, noteTime, this); break;
            case 3: p1Right.SpawnNote(duration, noteTime, this); if(p2Right && SessionConfig.PlayerCount == 2) p2Right.SpawnNote(duration, noteTime, this); break;
        }
    }

    public void EndLevel()
    {
        if (p1ScoreManager != null) {
            GameSessionData.P1Score = p1ScoreManager.currentScore;
            GameSessionData.P1MaxCombo = p1ScoreManager.maxCombo; 
        }
        if (SessionConfig.PlayerCount == 2 && p2ScoreManager != null) {
            GameSessionData.P2Score = p2ScoreManager.currentScore;
            GameSessionData.P2MaxCombo = p2ScoreManager.maxCombo;
            GameSessionData.IsTwoPlayer = true;
        }
        GameSessionData.CurrentSongGrades = thisSongGrades;
        TransitionManager.Instance.LoadScene("ResultsScene");
    }
}