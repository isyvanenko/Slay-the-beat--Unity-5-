using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;

/// <summary>
/// Developer tool that resets the entire game state and returns to the promo scene on keypress.
/// </summary>
/// <remarks>
/// This singleton component provides a quick development shortcut to completely reset
/// the game's state for testing purposes. It's designed to be placed in a persistent
/// GameObject that survives scene loads.
/// 
/// Key Features:
/// - Singleton pattern ensures only one instance exists
/// - Persists across scene loads (DontDestroyOnLoad)
/// - Optional Shift+Backspace modifier to prevent accidental triggers
/// - Wipes all game session data (scores, combos, player devices)
/// - Cleans up Input System user pairings
/// - Uses TransitionManager for smooth scene transitions (with fallback)
/// 
/// **WARNING:** This script should be removed or disabled in production builds.
/// It's intended for development and testing only.
/// 
/// Reset Operations:
/// - Stops/quiets music playback
/// - Resets SessionConfig (player count, devices, stage)
/// - Clears GameSessionData (scores, combos, multiplayer flags)
/// - Unpairs all Input System devices to prevent ghost input
/// - Forces scene transition to promo/start screen
/// </remarks>
public class DeveloperResetManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the DeveloperResetManager.
    /// </summary>
    /// <remarks>
    /// Access via DeveloperResetManager.Instance from any script.
    /// This is the authoritative instance that persists across scenes.
    /// </remarks>
    public static DeveloperResetManager Instance { get; private set; }

    [Header("Reset Settings")]
    /// <summary>
    /// Name of the scene to load when performing a full game reset.
    /// </summary>
    /// <remarks>
    /// Typically this is your splash screen, title screen, or promo scene.
    /// The scene name must match exactly with a scene in Build Settings.
    /// Default: "PromoScene"
    /// </remarks>
    public string promoSceneName = "PromoScene";
    
    [Header("Optional: Require Shift Key?")]
    /// <summary>
    /// When true, requires Shift+Backspace combination to trigger the reset.
    /// </summary>
    /// <remarks>
    /// Set this to true to prevent accidental resets during normal development.
    /// When false, pressing Backspace alone triggers the reset.
    /// 
    /// Recommended to enable this if you frequently type Backspace in
    /// debug consoles or other UI elements.
    /// Default: false
    /// </remarks>
    public bool requireShiftModifier = false;

    /// <summary>
    /// Initializes the singleton instance and marks it as persistent across scenes.
    /// </summary>
    /// <remarks>
    /// Singleton setup:
    /// - If an instance already exists, destroys this duplicate GameObject
    /// - Otherwise, sets this as the singleton instance
    /// - Marks the GameObject as DontDestroyOnLoad to persist through scene transitions
    /// 
    /// This setup ensures only one DeveloperResetManager exists at any time,
    /// and it stays alive throughout the entire game session.
    /// </remarks>
    private void Awake()
    {
        // --- SINGLETON SETUP ---
        // If one of these already exists, destroy this duplicate
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Otherwise, claim the throne and become immortal
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Checks for reset keypress input each frame.
    /// </summary>
    /// <remarks>
    /// Input detection:
    /// - Verifies a keyboard exists before checking keys
    /// - Detects Backspace key press (wasPressedThisFrame for single-frame detection)
    /// - Optionally requires Shift key modifier if configured
    /// - Triggers PerformFullGameReset() when conditions are met
    /// 
    /// Using wasPressedThisFrame prevents multiple rapid resets from
    /// holding down the Backspace key.
    /// </remarks>
    private void Update()
    {
        // Make sure a keyboard actually exists before checking it
        if (Keyboard.current == null) return;

        bool backspacePressed = Keyboard.current.backspaceKey.wasPressedThisFrame;
        
        // Optional safety net: Check if Shift is also held down
        if (requireShiftModifier)
        {
            bool shiftHeld = Keyboard.current.shiftKey.isPressed;
            if (backspacePressed && shiftHeld)
            {
                PerformFullGameReset();
            }
        }
        else
        {
            if (backspacePressed)
            {
                PerformFullGameReset();
            }
        }
    }

    /// <summary>
    /// Executes the complete game reset routine.
    /// </summary>
    /// <remarks>
    /// Reset sequence in order:
    /// 1. **Music Cleanup**: Stops music and resets to normal snapshot
    /// 2. **Session Reset**: Clears player count, stage, and device references
    /// 3. **Game Data Reset**: Zeroes out scores, combos, and multiplayer flags
    /// 4. **Input System Cleanup**: Unpairs all InputUser devices
    /// 5. **Scene Transition**: Loads the promo scene (with or without TransitionManager)
    /// 
    /// The InputSystem cleanup is particularly important to prevent
    /// "ghost input" where devices remain paired from previous sessions.
    /// 
    /// All operations are logged with a warning-level message for visibility
    /// in the Unity Console during development.
    /// </remarks>
    private void PerformFullGameReset()
    {
        Debug.LogWarning("### DEVELOPER OVERRIDE: FULL GAME RESET TRIGGERED ###");

        // 1. Stop the music if your MusicManager is active
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetQuiet(false);
            MusicManager.Instance.SetNormal(1f);
            // Optional: MusicManager.Instance.StopMusic(); if you have a stop function
        }

        // 2. Wipe Session Configuration
        SessionConfig.PlayerCount = 0;
        SessionConfig.CurrentStage = 1;
        SessionConfig.Player1Device = null;
        SessionConfig.Player2Device = null;

        // 3. Wipe Gameplay Scores
        GameSessionData.IsTwoPlayer = false;
        GameSessionData.P1Score = 0;
        GameSessionData.P1MaxCombo = 0;
        GameSessionData.P2Score = 0;
        GameSessionData.P2MaxCombo = 0;

        // 4. Exorcise ALL Input System Ghosts
        var allUsers = InputUser.all;
        for (int i = 0; i < allUsers.Count; i++)
        {
            allUsers[i].UnpairDevices();
        }

        // 5. Force load the Promo Scene
        // We check if your TransitionManager exists. If it does, we use your fancy fade.
        // If it doesn't (or got destroyed), we brute-force load it via Unity's SceneManager.
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.LoadScene(promoSceneName);
        }
        else
        {
            SceneManager.LoadScene(promoSceneName);
        }
    }
}