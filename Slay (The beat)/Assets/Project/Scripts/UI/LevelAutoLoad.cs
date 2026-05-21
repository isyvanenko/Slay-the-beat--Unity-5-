using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem; 

/// <summary>
/// Automatically loads a target scene after a countdown timer expires, resetting all game session data.
/// </summary>
/// <remarks>
/// This component is typically attached to an "attract mode" or "demo" screen that returns to the main menu
/// after a period of inactivity. It ensures a clean slate by wiping all persistent game data before transitioning.
/// 
/// Key Features:
/// - Countdown timer (default 15 seconds)
/// - Optional UI slider showing progress
/// - Full session reset (SessionConfig, GameSessionData, GameDataBridge)
/// - Uses TransitionManager for scene loading (with fallback to direct LoadScene)
/// - Reset includes player devices, scores, combos, round counters, and selected song
/// 
/// Useful for arcade machines, demo kiosks, or any environment where the game should auto-reset
/// after being idle for a set duration.
/// </remarks>
public class LevelAutoLoad : MonoBehaviour
{
    [Header("Timer Settings")]
    /// <summary>Time in seconds before automatically loading the next scene.</summary>
    public float countdownTime = 15f;

    /// <summary>GameObject containing the transition manager (used to get its Animator).</summary>
    public GameObject SceneTransManager;
    
    /// <summary>Animator component from the transition manager (for optional transition effects).</summary>
    private Animator anim;

    [Header("UI")]
    /// <summary>UI slider that visually represents the countdown progress.</summary>
    public Slider timerSlider;

    /// <summary>Current remaining time on the countdown timer.</summary>
    private float timer;
    
    /// <summary>Name of the scene to load when the countdown finishes.</summary>
    public string scenetospawn;

    /// <summary>
    /// Initializes the countdown timer and UI slider.
    /// </summary>
    void Start()
    {
        timer = countdownTime;

        if (timerSlider != null)
        {
            timerSlider.minValue = 0;
            timerSlider.maxValue = 1;
            timerSlider.value = 0; 
        }

        if (SceneTransManager != null)
        {
            anim = SceneTransManager.GetComponent<Animator>();
        }
    }

    /// <summary>
    /// Updates the countdown timer and UI slider each frame.
    /// </summary>
    /// <remarks>
    /// Decrements the timer by Time.deltaTime. The slider progress is calculated as 1 - (timer / countdownTime),
    /// so the slider fills from 0 to 1 as time elapses. When the timer reaches zero or below, LoadNextLevel() is called.
    /// </remarks>
    void Update()
    {
        timer -= Time.deltaTime;

        if (timerSlider != null)
        {
            float progress = 1 - (timer / countdownTime);
            timerSlider.value = Mathf.Clamp01(progress);
        }

        if (timer <= 0)
        {
            LoadNextLevel();
        }
    }

    /// <summary>
    /// Resets all game session data and loads the target scene.
    /// </summary>
    /// <remarks>
    /// First calls ResetEntireGameSession() to clear all persistent data, then uses TransitionManager.Instance
    /// to load the scene specified in scenetospawn. This ensures a fresh start when returning to the main menu.
    /// </remarks>
    void LoadNextLevel()
    {
        // 1. Wipe the data clean
        ResetEntireGameSession();
        
        // 2. Load the start scene (Menu/Title)
        TransitionManager.Instance.LoadScene(scenetospawn);
    }

    /// <summary>
    /// Completely resets all persistent game session data.
    /// </summary>
    /// <remarks>
    /// Resets the following:
    /// - SessionConfig: player count, devices, device types
    /// - GameSessionData: scores, max combos, multiplayer flag, song name, grade data, round counter
    /// - GameDataBridge: selected song
    /// 
    /// This ensures no stale data carries over when the auto-load triggers.
    /// </remarks>
    void ResetEntireGameSession()
    {
        Debug.Log("System: Purging session data for fresh start.");

        // --- Reset SessionConfig ---
        SessionConfig.PlayerCount = 1;
        SessionConfig.Player1Device = null;
        SessionConfig.Player2Device = null;
        SessionConfig.P1DeviceType = "";
        SessionConfig.P2DeviceType = "";

        // --- Reset GameSessionData ---
        GameSessionData.P1Score = 0;
        GameSessionData.P2Score = 0;
        GameSessionData.P1MaxCombo = 0;
        GameSessionData.P2MaxCombo = 0;
        GameSessionData.IsTwoPlayer = false;
        GameSessionData.SongName = "Unknown Song";
        GameSessionData.CurrentSongGrades = null;
        
        // CRITICAL: Reset the round counter so the game doesn't think it's over immediately
        GameSessionData.CurrentRound = 1; 

        // --- Reset Bridge ---
        if (GameDataBridge.SelectedSong != null)
        {
            GameDataBridge.SelectedSong = null;
        }
    }
}