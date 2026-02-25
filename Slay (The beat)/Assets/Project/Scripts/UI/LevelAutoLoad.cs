using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem; 

public class LevelAutoLoad : MonoBehaviour
{
    [Header("Timer Settings")]
    public float countdownTime = 15f;

    public GameObject SceneTransManager;
    private Animator anim;

    [Header("UI")]
    public Slider timerSlider;

    private float timer;
    public string scenetospawn;

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

    void LoadNextLevel()
    {
        // 1. Wipe the data clean
        ResetEntireGameSession();
        
        // 2. Load the start scene (Menu/Title)
        TransitionManager.Instance.LoadScene(scenetospawn);
    }

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