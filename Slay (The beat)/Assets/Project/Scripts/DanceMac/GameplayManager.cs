using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.Playables;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Serializable container for a single note event in the song chart.
/// </summary>
/// <remarks>
/// Stores timing, lane position, and hold duration for rhythm game notes.
/// Used by the chart loading system and gameplay manager.
/// </remarks>
[System.Serializable]
public class NoteEvent
{
    /// <summary>Time in seconds (or beats) when the note should be hit.</summary>
    public float time;

    /// <summary>Lane index (0-3 typically mapping to Left, Down, Up, Right).</summary>
    public int laneIndex;

    /// <summary>Duration of hold notes. Interpretation depends on holdsAreInBeats setting.</summary>
    public float holdLength;
}

/// <summary>
/// Core gameplay controller that manages rhythm game mechanics, note spawning, and input handling.
/// </summary>
/// <remarks>
/// This class is the central hub for gameplay, handling:
/// - Chart loading and note spawning with precise audio timing
/// - Multiplayer input device pairing and binding configuration
/// - Score management delegation to PlayerScoreManager components
/// - Pause/Resume functionality with audio and director sync
/// - Lane-based note spawning for up to 2 players
/// - Timing offset compensation for latency adjustment
/// - Scene transition management (results, restart, menu navigation)
/// 
/// The system uses AudioSettings.dspTime for sample-accurate music synchronization,
/// ensuring notes align perfectly with the audio timeline.
/// </remarks>
public class GameplayManager : MonoBehaviour
{
    [Header("Input System Rework")]
    /// <summary>Player 1's input controller with bound actions.</summary>
    public PlayerInput p1Input;

    /// <summary>Player 2's input controller for two-player mode.</summary>
    public PlayerInput p2Input;

    [Header("Audio & Sync")]
    /// <summary>AudioSource for music playback with scheduled start.</summary>
    public AudioSource musicSource;

    /// <summary>Timeline director for synchronized cutscenes or visual effects.</summary>
    public PlayableDirector director;

    /// <summary>UI slider showing current song playback progress.</summary>
    public Slider progressSlider;

    [Header("Score References")]
    /// <summary>Score manager for Player 1's scoring and combo tracking.</summary>
    public PlayerScoreManager p1ScoreManager;

    /// <summary>Score manager for Player 2 in two-player mode.</summary>
    public PlayerScoreManager p2ScoreManager;

    [Header("Timing Settings")]
    /// <summary>Note travel speed in pixels per second.</summary>
    public float noteSpeed = 300f;

    /// <summary>Song tempo in beats per minute for timing calculations.</summary>
    public float bpm = 120f;

    /// <summary>If true, hold lengths are in beats; if false, in seconds.</summary>
    public bool holdsAreInBeats = true;

    /// <summary>Delay before music starts, allowing players to prepare.</summary>
    public float startDelay = 3.0f;

    /// <summary>Manual offset adjustment for audio/video sync issues (milliseconds converted to seconds).</summary>
    public float manualLatencyAdjustment = 0.0f;

    [Header("Lane References")]
    /// <summary>Player 1's left lane controller.</summary>
    public LaneController p1Left;

    /// <summary>Player 1's down lane controller.</summary>
    public LaneController p1Down;

    /// <summary>Player 1's up lane controller.</summary>
    public LaneController p1Up;

    /// <summary>Player 1's right lane controller.</summary>
    public LaneController p1Right;

    [Header("Player 2 Panel")]
    /// <summary>Container panel for Player 2's UI elements.</summary>
    public GameObject p2Panel;

    /// <summary>Player 2's left lane controller.</summary>
    public LaneController p2Left;

    /// <summary>Player 2's down lane controller.</summary>
    public LaneController p2Down;

    /// <summary>Player 2's up lane controller.</summary>
    public LaneController p2Up;

    /// <summary>Player 2's right lane controller.</summary>
    public LaneController p2Right;

    [Header("Chart Data")]
    /// <summary>Loader component that parses chart files into NoteEvent lists.</summary>
    public TextChartLoader textLoader;

    /// <summary>Complete list of notes to be spawned during gameplay.</summary>
    public List<NoteEvent> songChart = new List<NoteEvent>();

    [Header("Song Settings")]
    /// <summary>Grade data for the currently playing song.</summary>
    public SongGradeData thisSongGrades;

    [Header("Pause System")]
    /// <summary>Pause menu UI controller.</summary>
    public PauseMenu pauseMenu;

    [Header("Scene Names")]
    /// <summary>Scene name for song selection screen.</summary>
    public string songSelectionSceneName = "SongSelection";

    /// <summary>Scene name for main menu.</summary>
    public string mainMenuSceneName = "MainMenu";

    /// <summary>Scene name for results screen.</summary>
    public string resultsSceneName = "ResultsScene";

    /// <summary>DSP timestamp when the song should start playing.</summary>
    private double dspSongStartTime;

    /// <summary>Flag indicating if music has started playing.</summary>
    private bool musicStarted = false;

    /// <summary>Time offset for note spawning anticipation.</summary>
    public float spawnOffset;

    /// <summary>Current index in songChart for sequential note spawning.</summary>
    private int currentNoteIndex = 0;

    /// <summary>Seconds between beats, derived from BPM.</summary>
    private float secondsPerBeat;

    /// <summary>Flag indicating if the game is currently paused.</summary>
    private bool isPaused = false;

    /// <summary>DSP timestamp when pause began.</summary>
    private double pauseStartTime;

    /// <summary>Accumulated time offset from all pause sessions.</summary>
    private double totalPauseOffset = 0;

    /// <summary>
    /// Initializes gameplay systems, loads chart, and schedules music playback.
    /// </summary>
    /// <remarks>
    /// Setup sequence:
    /// 1. Configure UI visibility based on player count
    /// 2. Load selected song data from GameDataBridge
    /// 3. Setup player input devices and bindings
    /// 4. Load and parse the song chart
    /// 5. Calculate timing parameters (seconds per beat, spawn offset)
    /// 6. Configure lane note speeds
    /// 7. Set up pause menu event listeners
    /// 8. Schedule music playback with start delay
    /// </remarks>
    void Start()
    {
        if (p2Panel != null)
            p2Panel.SetActive(SessionConfig.PlayerCount == 2);

        if (GameDataBridge.SelectedSong != null)
            thisSongGrades = GameDataBridge.SelectedSong;

        SetupPlayerInputs();

        if (textLoader != null) songChart = textLoader.LoadChart();
        secondsPerBeat = 60f / bpm;

        RectTransform receptorRect = p1Left.GetComponent<RectTransform>();
        RectTransform spawnRect = p1Left.spawnPoint.GetComponent<RectTransform>();
        float safeSpeed = (noteSpeed > 0) ? noteSpeed : 300f;
        float pixelDistance = Mathf.Abs(receptorRect.anchoredPosition.y - spawnRect.anchoredPosition.y);
        spawnOffset = (pixelDistance / safeSpeed) + manualLatencyAdjustment;

        p1Left.noteSpeed = p1Down.noteSpeed = p1Up.noteSpeed = p1Right.noteSpeed = safeSpeed;
        if (p2Left != null) p2Left.noteSpeed = p2Down.noteSpeed = p2Up.noteSpeed = p2Right.noteSpeed = safeSpeed;

        if (pauseMenu == null)
            pauseMenu = FindObjectOfType<PauseMenu>(true);

        if (pauseMenu != null)
        {
            pauseMenu.onResume.RemoveAllListeners();
            pauseMenu.onRestart.RemoveAllListeners();
            pauseMenu.onBackToMenu.RemoveAllListeners();

            pauseMenu.onResume.AddListener(ResumeGame);
            pauseMenu.onRestart.AddListener(RestartGame);
            pauseMenu.onBackToMenu.AddListener(BackToSongSelection);
        }

        // Pass song grade data to score managers for star achievements
        if (p1ScoreManager != null && thisSongGrades != null)
        {
            p1ScoreManager.SetSongGradeData(thisSongGrades);
            Debug.Log("<color=cyan>[STARS]</color> Star thresholds set for Player 1");
        }
        
        if (p2ScoreManager != null && thisSongGrades != null && SessionConfig.PlayerCount == 2)
        {
            p2ScoreManager.SetSongGradeData(thisSongGrades);
            Debug.Log("<color=cyan>[STARS]</color> Star thresholds set for Player 2");
        }

        dspSongStartTime = AudioSettings.dspTime + startDelay;
        if (musicSource != null && musicSource.clip != null)
            musicSource.PlayScheduled(dspSongStartTime);
    }

    /// <summary>
    /// Configures player input devices, binding overrides, and lane action mappings.
    /// </summary>
    /// <remarks>
    /// Input setup process:
    /// 1. Activates/deactivates Player 2 UI and input based on session config
    /// 2. Prevents automatic control scheme switching for both players
    /// 3. Pairs actual physical devices with PlayerInput components
    /// 4. Loads custom binding overrides if available (from JSON)
    /// 5. Initializes each lane with its corresponding InputAction
    /// 
    /// Error handling prevents game crashes if device pairing fails.
    /// </remarks>
    void SetupPlayerInputs()
    {
        Debug.Log("<color=yellow>=== STARTING GAMEPLAY INPUT SETUP ===</color>");

        if (SessionConfig.PlayerCount == 2)
        {
            if (p2Panel != null) p2Panel.SetActive(true);
            if (p2Input != null) p2Input.gameObject.SetActive(true);
        }
        else
        {
            if (p2Panel != null) p2Panel.SetActive(false);
            if (p2Input != null) p2Input.gameObject.SetActive(false);
        }

        if (p1Input != null) p1Input.neverAutoSwitchControlSchemes = true;
        if (p2Input != null) p2Input.neverAutoSwitchControlSchemes = true;

        if (p1Input != null && SessionConfig.Player1Device != null)
        {
            try
            {
                if (p1Input.actions != null) p1Input.actions.Disable();

                p1Input.user.UnpairDevices();
                InputUser.PerformPairingWithDevice(SessionConfig.Player1Device, p1Input.user);

                if (!string.IsNullOrEmpty(SessionConfig.P1Bindings))
                {
                    p1Input.actions.RemoveAllBindingOverrides();
                    p1Input.actions.LoadBindingOverridesFromJson(SessionConfig.P1Bindings);
                    Debug.Log("<color=green>[SUCCESS]</color> Player 1 Custom Map Loaded!");
                }

                if (p1Input.actions != null) p1Input.actions.Enable();

                p1Left.Initialize(p1Input.actions["Left"]);
                p1Down.Initialize(p1Input.actions["Down"]);
                p1Up.Initialize(p1Input.actions["Up"]);
                p1Right.Initialize(p1Input.actions["Right"]);
            }
            catch (System.Exception e) { Debug.LogError($"CRASH P1: {e.Message}"); }
        }

        if (SessionConfig.PlayerCount == 2 && p2Input != null && SessionConfig.Player2Device != null)
        {
            try
            {
                if (p2Input.actions != null) p2Input.actions.Disable();

                p2Input.user.UnpairDevices();
                InputUser.PerformPairingWithDevice(SessionConfig.Player2Device, p2Input.user);

                if (!string.IsNullOrEmpty(SessionConfig.P2Bindings))
                {
                    p2Input.actions.RemoveAllBindingOverrides();
                    p2Input.actions.LoadBindingOverridesFromJson(SessionConfig.P2Bindings);
                    Debug.Log("<color=green>[SUCCESS]</color> Player 2 Custom Map Loaded!");
                }

                if (p2Input.actions != null) p2Input.actions.Enable();

                if (p2Left != null) p2Left.Initialize(p2Input.actions["Left"]);
                if (p2Down != null) p2Down.Initialize(p2Input.actions["Down"]);
                if (p2Up != null) p2Up.Initialize(p2Input.actions["Up"]);
                if (p2Right != null) p2Right.Initialize(p2Input.actions["Right"]);
            }
            catch (System.Exception e) { Debug.LogError($"CRASH P2: {e.Message}"); }
        }
    }

    /// <summary>
    /// Updates gameplay each frame, handling pause input, progress tracking, and note spawning.
    /// </summary>
    /// <remarks>
    /// Per-frame operations:
    /// - Checks for pause input (Escape key or Gamepad Start button)
    /// - Updates progress slider if not paused
    /// - Starts timeline director when song begins
    /// - Spawns notes based on current song time and spawn offset
    /// </remarks>
    void Update()
    {
        if (!isPaused && !pauseMenu.IsPaused())
        {
            bool pausePressed = false;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                pausePressed = true;

            if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
                pausePressed = true;

            if (pausePressed)
            {
                PauseGame();
                return;
            }
        }

        if (!isPaused && !pauseMenu.IsPaused())
        {
            double songTime = GetAdjustedSongTime();

            if (progressSlider != null && musicSource != null && musicSource.clip != null)
                progressSlider.value = Mathf.Clamp01((float)songTime / musicSource.clip.length);

            if (director != null && songTime >= 0 && !musicStarted)
            {
                musicStarted = true;
                director.Play();
            }

            CheckSpawns(songTime);
        }
    }

    /// <summary>
    /// Gets the current song playback time adjusted for pause offsets.
    /// </summary>
    /// <returns>Current song time in seconds relative to DSP start time.</returns>
    public double GetAdjustedSongTime()
    {
        return (AudioSettings.dspTime - dspSongStartTime) - totalPauseOffset;
    }

    /// <summary>
    /// Checks and spawns notes that should appear based on current song time.
    /// </summary>
    /// <param name="currentSongTime">Current playback time in seconds.</param>
    /// <remarks>
    /// Uses look-ahead time (current time + spawn offset) to spawn notes early
    /// so they can travel down lanes to reach receptors at the correct moment.
    /// Spawns notes sequentially based on currentNoteIndex.
    /// </remarks>
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

    /// <summary>
    /// Spawns a note on the specified lane for both players (in multiplayer).
    /// </summary>
    /// <param name="laneIndex">Lane index (0=Left, 1=Down, 2=Up, 3=Right).</param>
    /// <param name="duration">Hold duration for long notes.</param>
    /// <param name="noteTime">Absolute song time when note should be hit.</param>
    /// <remarks>
    /// In two-player mode, notes spawn on both players' lanes simultaneously.
    /// This ensures both players see identical note charts.
    /// </remarks>
    void SpawnNote(int laneIndex, float duration, float noteTime)
    {
        switch (laneIndex)
        {
            case 0:
                p1Left.SpawnNote(duration, noteTime, this);
                if (p2Left && SessionConfig.PlayerCount == 2)
                    p2Left.SpawnNote(duration, noteTime, this);
                break;
            case 1:
                p1Down.SpawnNote(duration, noteTime, this);
                if (p2Down && SessionConfig.PlayerCount == 2)
                    p2Down.SpawnNote(duration, noteTime, this);
                break;
            case 2:
                p1Up.SpawnNote(duration, noteTime, this);
                if (p2Up && SessionConfig.PlayerCount == 2)
                    p2Up.SpawnNote(duration, noteTime, this);
                break;
            case 3:
                p1Right.SpawnNote(duration, noteTime, this);
                if (p2Right && SessionConfig.PlayerCount == 2)
                    p2Right.SpawnNote(duration, noteTime, this);
                break;
        }
    }

    /// <summary>
    /// Pauses the game, freezing music, note movement, and input processing.
    /// </summary>
    public void PauseGame()
    {
        if (isPaused || pauseMenu.IsPaused()) return;

        isPaused = true;
        pauseStartTime = AudioSettings.dspTime;

        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();

        if (director != null && director.state == PlayState.Playing)
            director.Pause();

        PauseAllLanes(true);

        if (p1Input != null)
            p1Input.DeactivateInput();
        if (p2Input != null && SessionConfig.PlayerCount == 2)
            p2Input.DeactivateInput();

        if (pauseMenu != null)
            pauseMenu.OpenPauseMenu(this);
    }

    /// <summary>
    /// Resumes gameplay after pause, restoring timing and input.
    /// </summary>
    /// <remarks>
    /// Calculates the exact pause duration to maintain audio sync.
    /// Music, timeline director, note movement, and input are all restored.
    /// </remarks>
    public void ResumeGame()
    {
        if (!isPaused) return;

        double pauseDuration = AudioSettings.dspTime - pauseStartTime;
        totalPauseOffset += pauseDuration;

        if (musicSource != null)
            musicSource.UnPause();

        if (director != null)
            director.Resume();

        PauseAllLanes(false);

        if (p1Input != null)
            p1Input.ActivateInput();
        if (p2Input != null && SessionConfig.PlayerCount == 2)
            p2Input.ActivateInput();

        isPaused = false;
    }

    /// <summary>
    /// Pauses or resumes movement on all lane controllers.
    /// </summary>
    /// <param name="pause">If true, pauses movement; if false, resumes.</param>
    void PauseAllLanes(bool pause)
    {
        LaneController[] allLanes = { p1Left, p1Down, p1Up, p1Right, p2Left, p2Down, p2Up, p2Right };

        foreach (LaneController lane in allLanes)
        {
            if (lane != null)
            {
                if (pause)
                    lane.PauseMovement();
                else
                    lane.ResumeMovement();
            }
        }
    }

    /// <summary>
    /// Restarts the current gameplay by reloading the active scene.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    /// <summary>
    /// Returns to song selection screen, preserving player configuration.
    /// </summary>
    public void BackToSongSelection()
    {
        Time.timeScale = 1f;

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadScene(songSelectionSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(songSelectionSceneName);
    }

    /// <summary>
    /// Returns to main menu, resetting game state.
    /// </summary>
    public void QuitToMenu()
    {
        Time.timeScale = 1f;

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadScene(mainMenuSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Checks if the game is currently paused.
    /// </summary>
    /// <returns>True if paused, false otherwise.</returns>
    public bool IsPaused()
    {
        return isPaused;
    }

    /// <summary>
    /// Ends the current level and transitions to the results scene.
    /// </summary>
    /// <remarks>
    /// Saves all gameplay data to GameSessionData for the results screen:
    /// - Player scores and max combos
    /// - Multiplayer status
    /// - Song grade information
    /// 
    /// Uses TransitionManager for smooth scene transitions if available.
    /// </remarks>
    public void EndLevel()
    {
        if (p1ScoreManager != null)
        {
            GameSessionData.P1Score = p1ScoreManager.currentScore;
            GameSessionData.P1MaxCombo = p1ScoreManager.maxCombo;
        }
        if (SessionConfig.PlayerCount == 2 && p2ScoreManager != null)
        {
            GameSessionData.P2Score = p2ScoreManager.currentScore;
            GameSessionData.P2MaxCombo = p2ScoreManager.maxCombo;
            GameSessionData.IsTwoPlayer = true;
        }
        else
        {
            GameSessionData.IsTwoPlayer = false;
        }

        GameSessionData.CurrentSongGrades = thisSongGrades;

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadScene(resultsSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(resultsSceneName);
    }
}