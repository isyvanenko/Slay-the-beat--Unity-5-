using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;

public class DeveloperResetManager : MonoBehaviour
{
    // The Singleton instance
    public static DeveloperResetManager Instance { get; private set; }

    [Header("Reset Settings")]
    public string promoSceneName = "PromoScene"; // Change this to your exact scene name!
    
    [Header("Optional: Require Shift Key?")]
    public bool requireShiftModifier = false; // Set to true if you keep accidentally hitting Backspace

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