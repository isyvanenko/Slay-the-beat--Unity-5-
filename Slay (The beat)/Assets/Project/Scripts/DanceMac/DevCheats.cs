using UnityEngine;
using UnityEngine.InputSystem;

public class DevCheats : MonoBehaviour
{
    private GameplayManager manager;

    void Start()
    {
        manager = FindFirstObjectByType<GameplayManager>();
    }

    void Update()
    {
        // --- KEYBOARD CHEATS ---

        // 1. PRESS 'U' TO INSTANTLY END SONG (Trigger Results)
        if (Keyboard.current.uKey.wasPressedThisFrame)
        {
            Debug.Log("DEV: Instant Win Triggered");
            manager.EndLevel();
        }

        // 2. PRESS 'I' TO ADD 100,000 SCORE (Test Grades)
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            if (manager.p1ScoreManager != null)
            {
                manager.p1ScoreManager.AddScore(1000000);
                Debug.Log("DEV: Added 100k Score to P1");
            }
        }

        // 3. PRESS 'O' TO ADD 50 COMBO
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            if (manager.p1ScoreManager != null)
            {
                // Forcefully bumping the max combo for testing
                manager.p1ScoreManager.maxCombo += 50;
                Debug.Log("DEV: Added 50 to Max Combo");
            }
        }

        // 4. PRESS 'P' TO TOGGLE SLOW-MO (Good for testing sync)
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            Time.timeScale = (Time.timeScale == 1.0f) ? 0.5f : 1.0f;
            Debug.Log("DEV: TimeScale toggled to " + Time.timeScale);
        }
    }
}