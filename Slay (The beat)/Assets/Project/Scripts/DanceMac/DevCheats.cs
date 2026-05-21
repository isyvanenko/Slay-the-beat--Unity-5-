using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Development cheat system for testing and debugging gameplay mechanics.
/// </summary>
/// <remarks>
/// This script provides keyboard shortcuts for development testing, allowing quick
/// access to common test scenarios without going through normal gameplay progression.
/// 
/// **WARNING:** This script should only be active in development builds.
/// For production builds, consider wrapping cheats with:
/// #if UNITY_EDITOR or using Debug.isDebugBuild
/// 
/// Available Cheats:
/// - U: Instantly win/end current song
/// - I: Add 1,000,000 score to Player 1
/// - O: Add 50 to max combo counter
/// - P: Toggle slow-motion mode (0.5x speed)
/// 
/// Use Cases:
/// - Testing grade thresholds with high scores
/// - Verifying combo-based mechanics
/// - Debugging end-of-level flow
/// - Checking audio/visual sync with slow motion
/// </remarks>
public class DevCheats : MonoBehaviour
{
    /// <summary>
    /// Reference to the main GameplayManager controlling the current game session.
    /// </summary>
    /// <remarks>
    /// Found automatically at startup using FindFirstObjectByType.
    /// Used to access score managers and level-ending functionality.
    /// </remarks>
    private GameplayManager manager;

    /// <summary>
    /// Initializes the cheat system by locating the GameplayManager.
    /// </summary>
    /// <remarks>
    /// Called automatically before the first frame. Searches for the first active
    /// GameplayManager in the scene. Assumes only one exists at a time.
    /// </remarks>
    void Start()
    {
        manager = FindFirstObjectByType<GameplayManager>();
    }

    /// <summary>
    /// Checks for developer cheat input each frame and executes corresponding actions.
    /// </summary>
    /// <remarks>
    /// All cheats use the new Input System's Keyboard.current state for frame-accurate
    /// detection. Each cheat is triggered on the exact frame the key is pressed,
    /// not held down.
    /// 
    /// The slow-motion cheat toggles between normal speed (1.0) and half speed (0.5),
    /// which is useful for:
    /// - Debugging note/beat synchronization
    /// - Frame-by-frame analysis of animations
    /// - Testing timing-sensitive mechanics
    /// - Creating dramatic slow-motion effects
    /// 
    /// Time.timeScale affects all time-based operations including:
    /// - Animations
    /// - Particle systems
    /// - Physics calculations
    /// - Audio pitch (if configured)
    /// - Coroutine wait times
    /// </remarks>
    void Update()
    {
        // --- KEYBOARD CHEATS ---

        /// <summary>
        /// CHEAT U: Instantly ends the current song and triggers results screen.
        /// </summary>
        /// <remarks>
        /// Bypasses normal level completion conditions. Useful for testing:
        /// - Results screen UI
        /// - Score calculation at level end
        /// - Transition animations
        /// - Post-level analytics
        /// </remarks>
        if (Keyboard.current.uKey.wasPressedThisFrame)
        {
            Debug.Log("DEV: Instant Win Triggered");
            manager.EndLevel();
        }

        /// <summary>
        /// CHEAT I: Adds 1,000,000 points to Player 1's score.
        /// </summary>
        /// <remarks>
        /// Large score addition for testing grade thresholds and score-based unlocks.
        /// Useful for verifying:
        /// - Grade calculations (S, A, B, C, D, F)
        /// - Score display formatting
        /// - High score tracking
        /// - Score-based achievements
        /// </remarks>
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            if (manager.p1ScoreManager != null)
            {
                manager.p1ScoreManager.AddScore(1000000);
                Debug.Log("DEV: Added 100k Score to P1");
            }
        }

        /// <summary>
        /// CHEAT O: Increases Player 1's max combo counter by 50.
        /// </summary>
        /// <remarks>
        /// Directly increments the maxCombo field for testing combo-based mechanics.
        /// Useful for:
        /// - Combo multiplier effects
        /// - Combo-based achievements
        /// - UI combo display
        /// - Particle effects on combo milestones
        /// 
        /// Note: This modifies maxCombo directly rather than building combo naturally,
        /// allowing immediate testing of high-combo scenarios.
        /// </remarks>
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            if (manager.p1ScoreManager != null)
            {
                // Forcefully bumping the max combo for testing
                manager.p1ScoreManager.maxCombo += 50;
                Debug.Log("DEV: Added 50 to Max Combo");
            }
        }

        /// <summary>
        /// CHEAT P: Toggles game speed between normal (1.0x) and slow-motion (0.5x).
        /// </summary>
        /// <remarks>
        /// Time scale affects:
        /// - Gameplay speed and difficulty
        /// - Animation playback rates
        /// - Audio pitch (if AudioSource.pitch is tied to time scale)
        /// - Physics simulation speed
        /// 
        /// Testing Applications:
        /// - Verifying note timing and hit windows
        /// - Debugging animation-event synchronization
        /// - Slow-motion replay analysis
        /// - Accessibility features (reduced speed mode)
        /// 
        /// Toggle behavior alternates between 1.0 and 0.5. Default speed is 1.0.
        /// </remarks>
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            Time.timeScale = (Time.timeScale == 1.0f) ? 0.5f : 1.0f;
            Debug.Log("DEV: TimeScale toggled to " + Time.timeScale);
        }
    }
}