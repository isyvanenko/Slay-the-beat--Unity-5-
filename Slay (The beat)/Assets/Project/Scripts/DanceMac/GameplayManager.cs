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
    [Header("Input Asset")]
    public InputActionAsset inputActions; 

    [Header("Audio & Sync")]
    public AudioSource musicSource;   
    public PlayableDirector director; 
    public Slider progressSlider; 

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

    private double dspSongStartTime;
    private bool musicStarted = false;
    public float spawnOffset; 
    private int currentNoteIndex = 0;
    private float secondsPerBeat; 
    private float totalSongLength = 0f; 

    void Start()
    {
        // Setup Players using the Action Asset
        SetupPlayer(1, SessionConfig.Player1Device, p1Left, p1Down, p1Up, p1Right);
        
        if (SessionConfig.PlayerCount == 2) 
        {
            SetupPlayer(2, SessionConfig.Player2Device, p2Left, p2Down, p2Up, p2Right);
            if (p2Panel != null) p2Panel.SetActive(true);
        }

        if (textLoader != null) songChart = textLoader.LoadChart();
        secondsPerBeat = 60f / bpm;

        // Calculate offset for spawning
        RectTransform receptorRect = p1Left.GetComponent<RectTransform>();
        RectTransform spawnRect = p1Left.spawnPoint.GetComponent<RectTransform>();
        float pixelDistance = Mathf.Abs(receptorRect.anchoredPosition.y - spawnRect.anchoredPosition.y);
        spawnOffset = (pixelDistance / noteSpeed) + manualLatencyAdjustment;

        dspSongStartTime = AudioSettings.dspTime + startDelay;
        if (musicSource != null) musicSource.PlayScheduled(dspSongStartTime);
    }

    void SetupPlayer(int playerNum, InputDevice device, LaneController left, LaneController down, LaneController up, LaneController right)
    {
        // Find the map named "Gameplay" in your Input Action Asset
        var map = inputActions.FindActionMap("Gameplay");
        
        left.Setup(map.FindAction("Left"), device);
        down.Setup(map.FindAction("Down"), device);
        up.Setup(map.FindAction("Up"), device);
        right.Setup(map.FindAction("Right"), device);
    }

    public double GetSongTime() { return AudioSettings.dspTime - dspSongStartTime; }

    void Update() {
        double songTime = GetSongTime();
        if (progressSlider != null && musicSource.clip != null)
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
            case 0: p1Left.SpawnNote(duration, noteTime, this); if(p2Left) p2Left.SpawnNote(duration, noteTime, this); break;
            case 1: p1Down.SpawnNote(duration, noteTime, this); if(p2Down) p2Down.SpawnNote(duration, noteTime, this); break;
            case 2: p1Up.SpawnNote(duration, noteTime, this); if(p2Up) p2Up.SpawnNote(duration, noteTime, this); break;
            case 3: p1Right.SpawnNote(duration, noteTime, this); if(p2Right) p2Right.SpawnNote(duration, noteTime, this); break;
        }
    }
}