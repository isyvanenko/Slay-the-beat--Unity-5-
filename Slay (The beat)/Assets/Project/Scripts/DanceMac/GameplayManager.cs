using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables; 
using UnityEngine.UI; 
using System.Collections.Generic;

public class GameplayManager : MonoBehaviour
{
    [Header("Audio & Sync")]
    public AudioSource musicSource;   
    public PlayableDirector director; 
    
    [Header("UI")]
    public Slider progressSlider; 

    [Header("Timing Settings")]
    public float noteSpeed = 300f;    
    public float bpm = 120f;            
    public bool holdsAreInBeats = true; 
    public float startDelay = 3.0f;   
    
    [Range(-0.5f, 0.5f)]
    public float manualLatencyAdjustment = 0.0f; 

    [Header("Debug")]
    public bool debugForceTwoPlayer = false; 

    [Header("Chart Data")]
    public TextChartLoader textLoader; 
    public List<NoteEvent> songChart = new List<NoteEvent>();

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

    // Internal Math
    private double dspSongStartTime;
    private bool musicStarted = false;
    public float spawnOffset; 
    private int currentNoteIndex = 0;
    private float secondsPerBeat; 
    private float totalSongLength = 0f; 

    void Start()
    {
        // --- DEBUG VALIDATION ---
        Debug.Log($"<color=cyan>--- GAME LOADED ---</color>");
        
        // Debug override
        if (debugForceTwoPlayer) SessionConfig.PlayerCount = 2;

        Debug.Log($"Mode: {SessionConfig.PlayerCount} Player(s)");
        if (SessionConfig.Player1Device != null) Debug.Log($"P1 Device: {SessionConfig.Player1Device.name}");
        if (SessionConfig.PlayerCount == 2 && SessionConfig.Player2Device != null) Debug.Log($"P2 Device: {SessionConfig.Player2Device.name}");
        // ------------------------

        // 1. Setup Input
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

        // 2. Load Chart
        if (textLoader != null) songChart = textLoader.LoadChart();

        secondsPerBeat = 60f / bpm;

        if (director != null) 
        {
            director.Stop();
            director.time = 0;
            director.Evaluate(); 
        }

        // 3. Calc Spawn Offset & Apply Speed
        // PLAYER 1 SPEED
        p1Left.noteSpeed = noteSpeed; p1Down.noteSpeed = noteSpeed;
        p1Up.noteSpeed = noteSpeed;   p1Right.noteSpeed = noteSpeed;

        // PLAYER 2 SPEED (FIXED: Now synced correctly so arrows don't fly fast)
        if (p2Left != null) {
            p2Left.noteSpeed = noteSpeed; p2Down.noteSpeed = noteSpeed;
            p2Up.noteSpeed = noteSpeed;   p2Right.noteSpeed = noteSpeed;
        }

        RectTransform receptorRect = p1Left.GetComponent<RectTransform>();
        RectTransform spawnRect = p1Left.spawnPoint.GetComponent<RectTransform>();
        float pixelDistance = Mathf.Abs(receptorRect.anchoredPosition.y - spawnRect.anchoredPosition.y);
        spawnOffset = (pixelDistance / noteSpeed) + manualLatencyAdjustment;

        // 4. Get Song Length
        if (musicSource != null && musicSource.clip != null)
        {
            totalSongLength = musicSource.clip.length;
        }

        // 5. Schedule Music
        dspSongStartTime = AudioSettings.dspTime + startDelay;
        if (musicSource != null)
        {
            musicSource.Stop(); 
            musicSource.PlayScheduled(dspSongStartTime);
        }
    }

    void Update()
    {
        double currentDspTime = AudioSettings.dspTime;
        double songTime = currentDspTime - dspSongStartTime;

        // Update Slider
        if (progressSlider != null && totalSongLength > 0)
        {
            float progress = Mathf.Clamp01((float)songTime / totalSongLength);
            progressSlider.value = progress;
        }

        if (director != null)
        {
            if (songTime < 0) director.time = 0;
            else
            {
                if (!musicStarted) { musicStarted = true; director.Play(); }
                director.time = songTime;
                director.Evaluate(); 
            }
        }

        CheckSpawns(songTime);
    }

    public double GetSongTime()
    {
        return AudioSettings.dspTime - dspSongStartTime;
    }

    void CheckSpawns(double currentSongTime)
    {
        double lookAheadTime = currentSongTime + spawnOffset;

        while (currentNoteIndex < songChart.Count)
        {
            NoteEvent nextNote = songChart[currentNoteIndex];

            if (lookAheadTime >= nextNote.time)
            {
                float finalHoldDuration = nextNote.holdLength;
                if (holdsAreInBeats && nextNote.holdLength > 0)
                {
                    finalHoldDuration = nextNote.holdLength * secondsPerBeat;
                }
                
                SpawnNote(nextNote.laneIndex, finalHoldDuration, nextNote.time);
                currentNoteIndex++;
            }
            else
            {
                break;
            }
        }
    }

    void SpawnNote(int laneIndex, float duration, float noteTime)
    {
        // Player 1
        switch (laneIndex)
        {
            case 0: p1Left.SpawnNote(duration, noteTime, this); break;
            case 1: p1Down.SpawnNote(duration, noteTime, this); break;
            case 2: p1Up.SpawnNote(duration, noteTime, this); break;
            case 3: p1Right.SpawnNote(duration, noteTime, this); break;
        }

        // Player 2
        if (SessionConfig.PlayerCount == 2)
        {
            switch (laneIndex) { 
                case 0: p2Left.SpawnNote(duration, noteTime, this); break; 
                case 1: p2Down.SpawnNote(duration, noteTime, this); break; 
                case 2: p2Up.SpawnNote(duration, noteTime, this); break; 
                case 3: p2Right.SpawnNote(duration, noteTime, this); break; 
            }
        }
    }

    // --- UPDATED INPUT SETUP FOR JOYSTICKS ---
    void SetupPlayer1()
    {
        InputDevice device = SessionConfig.Player1Device ?? Keyboard.current;
        
        // --- FIX: LOWER THRESHOLD TO 0.7 ---
        // 0.85 was too high! Many controllers only reach 0.8 when pushed fast.
        // 0.7 is still safe against diagonals (0.707) but ensures your press registers.
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
            p1Left.Setup(device, new string[] { 
                "<Gamepad>/dpad/left", 
                $"<Gamepad>/leftStick/left{threshold}", 
                $"<Joystick>/stick/left{threshold}" 
            });

            p1Down.Setup(device, new string[] { 
                "<Gamepad>/dpad/down", 
                $"<Gamepad>/leftStick/down{threshold}", 
                $"<Joystick>/stick/down{threshold}" 
            });

            p1Up.Setup(device, new string[] { 
                "<Gamepad>/dpad/up", 
                $"<Gamepad>/leftStick/up{threshold}", 
                $"<Joystick>/stick/up{threshold}" 
            });

            p1Right.Setup(device, new string[] { 
                "<Gamepad>/dpad/right", 
                $"<Gamepad>/leftStick/right{threshold}", 
                $"<Joystick>/stick/right{threshold}" 
            });
        }
    }
    
    void SetupPlayer2() 
    {
        InputDevice device = SessionConfig.Player2Device;
        if (device == null) device = Keyboard.current;

        // Apply same safe threshold
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
            p2Left.Setup(device, new string[] { 
                "<Gamepad>/dpad/left", 
                $"<Gamepad>/leftStick/left{threshold}", 
                $"<Joystick>/stick/left{threshold}" 
            });

            p2Down.Setup(device, new string[] { 
                "<Gamepad>/dpad/down", 
                $"<Gamepad>/leftStick/down{threshold}", 
                $"<Joystick>/stick/down{threshold}" 
            });

            p2Up.Setup(device, new string[] { 
                "<Gamepad>/dpad/up", 
                $"<Gamepad>/leftStick/up{threshold}", 
                $"<Joystick>/stick/up{threshold}" 
            });

            p2Right.Setup(device, new string[] { 
                "<Gamepad>/dpad/right", 
                $"<Gamepad>/leftStick/right{threshold}", 
                $"<Joystick>/stick/right{threshold}" 
            });
        }
    }
}
[System.Serializable]
public class NoteEvent 
{ 
    public float time; 
    public int laneIndex; 
    public float holdLength; 
}