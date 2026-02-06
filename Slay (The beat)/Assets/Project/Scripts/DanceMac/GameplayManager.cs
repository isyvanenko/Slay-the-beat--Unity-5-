using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class NoteEvent
{
    public float time;
    public int laneIndex;
    public float holdLength;
}

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

    private double dspSongStartTime;
    private bool musicStarted = false;
    public float spawnOffset;
    private int currentNoteIndex = 0;
    private float secondsPerBeat;
    private float totalSongLength = 0f;

    void Start()
    {
        if (debugForceTwoPlayer) SessionConfig.PlayerCount = 2;

        SetupPlayer(1, SessionConfig.Player1Device, p1Left, p1Down, p1Up, p1Right);

        if (SessionConfig.PlayerCount == 2)
        {
            SetupPlayer(2, SessionConfig.Player2Device, p2Left, p2Down, p2Up, p2Right);
            if (p2Panel != null) p2Panel.SetActive(true);
        }
        else if (p2Panel != null)
        {
            p2Panel.SetActive(false);
        }

        if (textLoader != null) songChart = textLoader.LoadChart();
        secondsPerBeat = 60f / bpm;

        if (director != null)
        {
            director.Stop();
            director.time = 0;
            director.Evaluate();
        }

        SyncLaneSpeeds();
        CalculateSpawnOffset();

        if (musicSource != null && musicSource.clip != null)
            totalSongLength = musicSource.clip.length;

        dspSongStartTime = AudioSettings.dspTime + startDelay;
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.PlayScheduled(dspSongStartTime);
        }
    }

    public double GetSongTime()
    {
        return AudioSettings.dspTime - dspSongStartTime;
    }

    void SetupPlayer(int playerNum, InputDevice device, LaneController left, LaneController down, LaneController up, LaneController right)
    {
        if (device == null) device = Keyboard.current;

        // Low pressPoint for triggers/axes to ensure instant dance mat response
        string t = "[pressPoint=0.1]";

        if (device is Keyboard)
        {
            left.Setup(device, new string[] { "<Keyboard>/leftArrow", "<Keyboard>/a" });
            down.Setup(device, new string[] { "<Keyboard>/downArrow", "<Keyboard>/s" });
            up.Setup(device, new string[] { "<Keyboard>/upArrow", "<Keyboard>/w" });
            right.Setup(device, new string[] { "<Keyboard>/rightArrow", "<Keyboard>/d" });
        }
        else
        {
            // --- DANCE MAT SPECIFIC MAPPINGS ---

            // LEFT: Debug identified as Button 3
            left.Setup(device, new string[] {
                "<Joystick>/button3",
                "<Gamepad>/buttonWest",
                "<Gamepad>/dpad/left"
            });

            // DOWN: Debug identified as Button 2
            down.Setup(device, new string[] {
                "<Joystick>/button2",
                "<Gamepad>/buttonSouth",
                "<Gamepad>/dpad/down"
            });

            // UP: Debug identified as Trigger
            up.Setup(device, new string[] {
    "<Joystick>/stick/up",           // Standard Joystick Y-Axis
    "<Joystick>/stick/y",            // Alternative Y-Axis
    "<Gamepad>/leftStick/up",        // Gamepad Left Stick
    "<Gamepad>/dpad/up",             // Standard D-Pad
    "<Gamepad>/rightTrigger",        // Trigger Axis
    "<Gamepad>/leftTrigger",         // Trigger Axis
    "<Joystick>/button5",            // Generic Button 5
    "<Joystick>/button6",            // Generic Button 6
    "<Joystick>/button10",           // Common for "Start/Up" combos
    "<Gamepad>/buttonNorth",         // Triangle/Y
    "<HID::GenericDesktop>/z",       // Some mats map Up to the Z axis
    "<HID::GenericDesktop>/rz"       // Some mats map Up to the Z-Rotation axis
});

            // RIGHT: Debug identified as Button 4
            right.Setup(device, new string[] {
                "<Joystick>/button4",
                "<Gamepad>/buttonEast",
                "<Gamepad>/dpad/right"
            });
        }
    }

    void SyncLaneSpeeds()
    {
        p1Left.noteSpeed = p1Down.noteSpeed = p1Up.noteSpeed = p1Right.noteSpeed = noteSpeed;
        if (p2Left != null)
            p2Left.noteSpeed = p2Down.noteSpeed = p2Up.noteSpeed = p2Right.noteSpeed = noteSpeed;
    }

    void CalculateSpawnOffset()
    {
        RectTransform receptorRect = p1Left.GetComponent<RectTransform>();
        RectTransform spawnRect = p1Left.spawnPoint.GetComponent<RectTransform>();
        float pixelDistance = Mathf.Abs(receptorRect.anchoredPosition.y - spawnRect.anchoredPosition.y);
        spawnOffset = (pixelDistance / noteSpeed) + manualLatencyAdjustment;
    }

    void Update()
    {
        double songTime = GetSongTime();

        if (progressSlider != null && totalSongLength > 0)
            progressSlider.value = Mathf.Clamp01((float)songTime / totalSongLength);

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

    void CheckSpawns(double currentSongTime)
    {
        double lookAheadTime = currentSongTime + spawnOffset;
        while (currentNoteIndex < songChart.Count)
        {
            NoteEvent nextNote = songChart[currentNoteIndex];
            if (lookAheadTime >= nextNote.time)
            {
                float holdDur = holdsAreInBeats ? nextNote.holdLength * secondsPerBeat : nextNote.holdLength;
                SpawnNote(nextNote.laneIndex, holdDur, nextNote.time);
                currentNoteIndex++;
            }
            else break;
        }
    }

    void SpawnNote(int laneIndex, float duration, float noteTime)
    {
        switch (laneIndex)
        {
            case 0: p1Left.SpawnNote(duration, noteTime, this); if (SessionConfig.PlayerCount == 2) p2Left.SpawnNote(duration, noteTime, this); break;
            case 1: p1Down.SpawnNote(duration, noteTime, this); if (SessionConfig.PlayerCount == 2) p2Down.SpawnNote(duration, noteTime, this); break;
            case 2: p1Up.SpawnNote(duration, noteTime, this); if (SessionConfig.PlayerCount == 2) p2Up.SpawnNote(duration, noteTime, this); break;
            case 3: p1Right.SpawnNote(duration, noteTime, this); if (SessionConfig.PlayerCount == 2) p2Right.SpawnNote(duration, noteTime, this); break;
        }
    }
}