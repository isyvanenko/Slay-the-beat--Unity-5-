using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.Playables;
using UnityEngine.UI;
using System.Collections;
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

    [Header("Pause System")]
    public PauseMenu pauseMenu;

    [Header("Scene Names")]
    public string songSelectionSceneName = "SongSelection";
    public string mainMenuSceneName = "MainMenu";
    public string resultsSceneName = "ResultsScene";

    private double dspSongStartTime;
    private bool musicStarted = false;
    public float spawnOffset; 
    private int currentNoteIndex = 0;
    private float secondsPerBeat; 
    
    private bool isPaused = false;
    private double pauseStartTime;
    private double totalPauseOffset = 0;
    
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

        dspSongStartTime = AudioSettings.dspTime + startDelay;
        if (musicSource != null && musicSource.clip != null) 
            musicSource.PlayScheduled(dspSongStartTime);
    }

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

    public double GetAdjustedSongTime()
    {
        return (AudioSettings.dspTime - dspSongStartTime) - totalPauseOffset;
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
            case 0: 
                p1Left.SpawnNote(duration, noteTime, this); 
                if(p2Left && SessionConfig.PlayerCount == 2) 
                    p2Left.SpawnNote(duration, noteTime, this); 
                break;
            case 1: 
                p1Down.SpawnNote(duration, noteTime, this); 
                if(p2Down && SessionConfig.PlayerCount == 2) 
                    p2Down.SpawnNote(duration, noteTime, this); 
                break;
            case 2: 
                p1Up.SpawnNote(duration, noteTime, this); 
                if(p2Up && SessionConfig.PlayerCount == 2) 
                    p2Up.SpawnNote(duration, noteTime, this); 
                break;
            case 3: 
                p1Right.SpawnNote(duration, noteTime, this); 
                if(p2Right && SessionConfig.PlayerCount == 2) 
                    p2Right.SpawnNote(duration, noteTime, this); 
                break;
        }
    }

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

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void BackToSongSelection()
    {
        Time.timeScale = 1f;
        
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadScene(songSelectionSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(songSelectionSceneName);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadScene(mainMenuSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }

    public bool IsPaused()
    {
        return isPaused;
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