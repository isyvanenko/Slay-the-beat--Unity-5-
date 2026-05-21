using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the song selection carousel, splash intro, difficulty selection,
/// arcade countdown timer, preview audio, visual pulse updates, and pause flow.
/// </summary>
public class SongCarouselManager : MonoBehaviour
{
    /// <summary>High-level UI states used by the song selection flow.</summary>
    private enum MenuState { Splash, Carousel, Difficulty, Confirming }
    /// <summary>Current state of the song selection menu.</summary>
    [SerializeField] private MenuState currentState = MenuState.Splash;

    /// <summary>All song entries available to the carousel, including any random song option.</summary>
    [Header("Data Source")]
    public List<SongGradeData> allSongData;

    /// <summary>Canvas group faded out after the splash screen delay.</summary>
    [Header("Splash Screen")]
    public CanvasGroup splashCanvasGroup;
    /// <summary>Text shown on the splash screen before the carousel appears.</summary>
    public TextMeshProUGUI splashText;
    /// <summary>Seconds to wait before transitioning from splash to carousel.</summary>
    public float splashDuration = 2.5f; 

    /// <summary>Starting value for the arcade selection countdown timer.</summary>
    [Header("Arcade Timer & Warnings")]
    public float maxTimerValue = 30f; 
    /// <summary>Text display for the remaining timer seconds.</summary>
    public TextMeshProUGUI timerText; 
    /// <summary>Slider display for the remaining timer ratio.</summary>
    public Slider timerSlider; 
    /// <summary>Current remaining time before auto-selection.</summary>
    private float currentTimer;
    /// <summary>Next timer value at which the warning flash should trigger.</summary>
    private float nextWarningTime;
    /// <summary>Active warning flash coroutine, if one is running.</summary>
    private Coroutine warningRoutine;

    /// <summary>Control prompt canvas shown during song selection.</summary>
    [Header("Control Prompts (Canvases)")]
    public GameObject controlsSongSelect;       
    /// <summary>Control prompt canvas shown during normal difficulty selection.</summary>
    public GameObject controlsDifficultyNormal; 
    /// <summary>Control prompt canvas shown when difficulty selection was forced by timeout.</summary>
    public GameObject controlsDifficultyForced; 
    /// <summary>Canvas group flashed as a timer warning.</summary>
    public CanvasGroup warningCanvasGroup;      

    /// <summary>Prefab instantiated once for each song in the carousel.</summary>
    [Header("Carousel Setup")]
    public GameObject songPrefab; 
    /// <summary>Parent transform for spawned song carousel blocks.</summary>
    public Transform container;     
    /// <summary>Slot positions and scales used to lay out carousel items.</summary>
    public RectTransform[] slots; 
    /// <summary>Canvas group for fading the carousel UI in and out.</summary>
    public CanvasGroup carouselCanvasGroup;

    /// <summary>Large title text for the currently highlighted song.</summary>
    [Header("Main Menu Visuals")]
    public TextMeshProUGUI mainSongTitleText; 
    /// <summary>Character image shown for the currently highlighted song.</summary>
    public Image mainCharacterDisplay;        
    /// <summary>Background image shown for the currently highlighted song.</summary>
    public Image mainBackgroundDisplay;       

    /// <summary>Canvas group for fading the difficulty selection UI in and out.</summary>
    [Header("Difficulty UI Elements")]
    public CanvasGroup difficultyCanvasGroup;
    /// <summary>Song jacket image shown on the difficulty selection screen.</summary>
    public Image diffJacketDisplay; 
    /// <summary>Selectable difficulty box transforms, ordered easy, medium, hard.</summary>
    public RectTransform[] difficultyBoxes; 
    /// <summary>Outline images used to highlight the selected difficulty.</summary>
    public Image[] difficultyOutlines;
    /// <summary>Step count text for each difficulty.</summary>
    public TextMeshProUGUI[] diffStepTexts;
    /// <summary>Song title shown on the difficulty selection screen.</summary>
    public TextMeshProUGUI diffSongTitle;
    /// <summary>Prompt shown while waiting for a second confirm input.</summary>
    public GameObject pressAgainText; 

    /// <summary>Sprite used for difficulty boxes that are locked because they have no steps.</summary>
    [Header("Difficulty Lock System")]
    public Sprite lockedSprite;
    /// <summary>Text color used for available difficulty step counts.</summary>
    public Color unlockedTextColor = Color.black;
    /// <summary>Text color used for locked difficulty step counts.</summary>
    public Color lockedTextColor = Color.gray;
    /// <summary>Sound played when the player tries to select a locked difficulty.</summary>
    public AudioClip lockedSound;

    /// <summary>Interpolation speed used by carousel and difficulty UI movement.</summary>
    [Header("Visual Settings")]
    public float lerpSpeed = 10f;
    /// <summary>Scale applied to unselected difficulty boxes.</summary>
    public float normalScale = 1.0f;
    /// <summary>Scale applied to the selected difficulty box.</summary>
    public float selectedScale = 1.2f;
    /// <summary>Highlight color used by selected difficulty outlines.</summary>
    public Color outlineGold = new Color(1f, 0.85f, 0f);
    /// <summary>Dark tint used for unselected boxes while confirming a difficulty.</summary>
    public Color unselectedDarkColor = new Color(0.05f, 0.05f, 0.05f, 1f);

    /// <summary>Audio source used for song preview music.</summary>
    [Header("Audio")]
    public AudioSource musicSource;
    /// <summary>Audio source used for UI sound effects.</summary>
    public AudioSource sfxSource;       
    /// <summary>Sound played when moving between songs or difficulties.</summary>
    public AudioClip moveSound;         
    /// <summary>Sound played when selecting a song or entering confirmation.</summary>
    public AudioClip selectSound;
    /// <summary>Sound played when confirming the final song and difficulty choice.</summary>
    public AudioClip confirmSound;
    /// <summary>Sound played when returning from difficulty selection to the carousel.</summary>
    public AudioClip cancelSound; 
    /// <summary>Maximum volume used for song preview playback.</summary>
    public float musicMaxVolume = 0.5f;

    /// <summary>Pause menu used by the song carousel scene.</summary>
    [Header("Pause System")]
    public PauseMenu pauseMenu;

    /// <summary>First optional music visual pulse target updated when song selection changes.</summary>
    [Header("Visual Pulse Objects")]
    [Tooltip("First object with SpireMusicVisualPulse component")]
    public SpireMusicVisualPulse visualPulseObject1;
    /// <summary>Second optional music visual pulse target updated when song selection changes.</summary>
    [Tooltip("Second object with SpireMusicVisualPulse component")]
    public SpireMusicVisualPulse visualPulseObject2;
    /// <summary>Whether visual pulse targets should update automatically when the song changes.</summary>
    [Tooltip("Auto-update visual pulses when song changes")]
    public bool autoUpdateVisualPulses = true;

    /// <summary>Spawned carousel block transforms, one for each song entry.</summary>
    private List<RectTransform> spawnedBlocks = new List<RectTransform>();
    /// <summary>Canvas groups used to fade spawned carousel blocks based on slot position.</summary>
    private List<CanvasGroup> spawnedGroups = new List<CanvasGroup>(); 
    /// <summary>Original local scales captured from spawned carousel blocks.</summary>
    private List<Vector3> originalScales = new List<Vector3>(); 

    /// <summary>Index of the currently selected song in <see cref="allSongData"/>.</summary>
    private int currentSongIndex = 0; 
    /// <summary>Index of the currently selected difficulty box.</summary>
    private int currentDiffIndex = 1; 
    /// <summary>Generated input actions wrapper used by the menu UI.</summary>
    private InputActions input;
    /// <summary>Whether a UI transition is currently running.</summary>
    private bool isTransitioning = false;
    /// <summary>Whether the current selection flow was advanced automatically by the timer.</summary>
    private bool wasAutoSelected = false; 
    /// <summary>Whether this manager has paused the carousel scene.</summary>
    private bool isPaused = false;
    
    /// <summary>Resolved random song hidden behind the random carousel option.</summary>
    private SongGradeData secretRandomSong = null;
    
    /// <summary>Difficulty lock state for easy, medium, and hard.</summary>
    private bool[] lockedDifficulties = new bool[3];
    /// <summary>Original difficulty box sprites restored when difficulties are unlocked.</summary>
    private Sprite[] originalSprites = new Sprite[3];

    /// <summary>
    /// Creates input actions and registers UI navigation, confirm, and cancel callbacks.
    /// </summary>
    void Awake()
    {
        input = new InputActions();
        input.UI.NavigateLeft.performed += _ => OnMove(-1);
        input.UI.NavigateRight.performed += _ => OnMove(1);
        input.UI.Select.performed += _ => OnConfirm(false); 
        input.UI.Cancel.performed += _ => OnCancel(); 
    }

    /// <summary>
    /// Initializes carousel content, pause menu callbacks, difficulty defaults, and starts the splash sequence.
    /// </summary>
    void Start()
    {
        if (allSongData.Count == 0) return;

        currentState = MenuState.Splash;
        carouselCanvasGroup.alpha = 0f;
        difficultyCanvasGroup.alpha = 0f;
        
        if (warningCanvasGroup != null) warningCanvasGroup.alpha = 0f;
        if (splashCanvasGroup != null) 
        {
            splashCanvasGroup.alpha = 1f;
            splashCanvasGroup.gameObject.SetActive(true);
        }

        // Set welcome text instead of stage info
        if (splashText != null)
        {
            splashText.text = "SELECT YOUR SONG";
        }

        // Find Pause Menu if not assigned
        if (pauseMenu == null)
            pauseMenu = FindObjectOfType<PauseMenu>(true);

        // Setup Pause Menu Actions
        if (pauseMenu != null)
        {
            pauseMenu.onResume.RemoveAllListeners();
            pauseMenu.onRestart.RemoveAllListeners();
            pauseMenu.onBackToMenu.RemoveAllListeners();
            
            pauseMenu.onResume.AddListener(ResumeGame);
            pauseMenu.onBackToMenu.AddListener(QuitToMenu);
        }

        if (musicSource != null) musicSource.Stop();

        for (int i = 0; i < allSongData.Count; i++)
        {
            GameObject go = Instantiate(songPrefab, container);
            RectTransform rt = go.GetComponent<RectTransform>();
            
            originalScales.Add(rt.localScale);
            spawnedBlocks.Add(rt);
            spawnedGroups.Add(go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>());
            
            go.GetComponent<SongBlock>()?.UpdateData(allSongData[i]);
        }
        
        for (int i = 0; i < difficultyBoxes.Length && i < 3; i++)
        {
            Image img = difficultyBoxes[i].GetComponent<Image>();
            if (img != null && img.sprite != null)
            {
                originalSprites[i] = img.sprite;
            }
            
            if (diffStepTexts[i] != null)
            {
                diffStepTexts[i].color = unlockedTextColor;
                diffStepTexts[i].raycastTarget = false;
            }
        }
        
        if (pressAgainText) pressAgainText.SetActive(false);
        UpdateControlCanvases(); 

        StartCoroutine(SplashSequenceRoutine());
    }

    /// <summary>
    /// Waits through the splash screen, prepares carousel layout, fades into song selection, and starts the timer.
    /// </summary>
    /// <returns>Coroutine enumerator for the splash-to-carousel sequence.</returns>
    private IEnumerator SplashSequenceRoutine()
    {
        isTransitioning = true;

        yield return new WaitForSeconds(splashDuration);
        
        Canvas.ForceUpdateCanvases();
        
        foreach (RectTransform slot in slots)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(slot);
        }
        
        SnapToPositions();
        
        yield return StartCoroutine(FadeGroups(splashCanvasGroup, carouselCanvasGroup));

        currentState = MenuState.Carousel;
        isTransitioning = false;
        
        UpdateSelectionVisuals(); 
        ResetTimer(); 
        UpdateControlCanvases();
        
        // Force a position refresh to fix the initial placement
        StartCoroutine(ForcePositionRefresh());
    }
    
    /// <summary>
    /// Re-snaps carousel positions across two frames to correct initial layout timing.
    /// </summary>
    /// <returns>Coroutine enumerator for the forced position refresh.</returns>
    private IEnumerator ForcePositionRefresh()
    {
        yield return null; // Wait one frame
        
        // Simulate a tiny movement to trigger the position correction
        int oldIndex = currentSongIndex;
        currentSongIndex = (currentSongIndex + 1) % allSongData.Count;
        SnapToPositions(); // Snap to new position
        yield return null;
        currentSongIndex = oldIndex;
        SnapToPositions(); // Snap back to original position
        
        // Update visuals after refreshing
        UpdateSelectionVisuals();
    }

    /// <summary>
    /// Handles pause input, timer updates, and per-frame carousel or difficulty animations.
    /// </summary>
    void Update()
    {
        // Check for pause input (Escape or Start button) - ONLY when not paused and not transitioning
        if (!isPaused && !isTransitioning && (pauseMenu == null || !pauseMenu.IsPaused()))
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
        
        if (isPaused || (pauseMenu != null && pauseMenu.IsPaused())) return;
        
        if (isTransitioning) return;

        HandleArcadeTimer();

        if (currentState == MenuState.Carousel)
            UpdateCarouselMovement();
        else if (currentState == MenuState.Difficulty)
            UpdateDifficultyVisuals();
    }

    /// <summary>
    /// Decrements the arcade timer, updates timer UI, flashes warnings, and auto-selects on timeout.
    /// </summary>
    private void HandleArcadeTimer()
    {
        if (currentState == MenuState.Splash || currentState == MenuState.Confirming || isTransitioning) return;

        currentTimer -= Time.deltaTime;
        
        if (currentTimer <= nextWarningTime && currentTimer > 0)
        {
            nextWarningTime -= 10f; 
            if (warningRoutine != null) StopCoroutine(warningRoutine);
            warningRoutine = StartCoroutine(FlashWarningRoutine());
        }

        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(currentTimer).ToString();
            timerText.color = currentTimer < 5f ? Color.red : Color.white;
        }

        if (timerSlider != null)
        {
            float targetValue = Mathf.Clamp01(currentTimer / maxTimerValue);
            timerSlider.value = Mathf.Lerp(timerSlider.value, targetValue, Time.deltaTime * 5f);
        }

        if (currentTimer <= 0)
        {
            AutoSelect();
        }
    }

    /// <summary>
    /// Flashes the warning canvas group when the arcade timer reaches a warning threshold.
    /// </summary>
    /// <returns>Coroutine enumerator for the warning pulse animation.</returns>
    private IEnumerator FlashWarningRoutine()
    {
        if (warningCanvasGroup != null)
        {
            if (!warningCanvasGroup.gameObject.activeSelf) 
                warningCanvasGroup.gameObject.SetActive(true);
            
            float duration = 2f;
            float elapsed = 0f;
            float pulses = 3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float phase = (elapsed / duration) * (pulses * Mathf.PI * 2f);
                warningCanvasGroup.alpha = (-Mathf.Cos(phase) + 1f) / 2f;
                yield return null;
            }

            warningCanvasGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// Advances the menu automatically when the arcade timer expires.
    /// </summary>
    private void AutoSelect()
    {
        if (currentState == MenuState.Carousel)
        {
            OnConfirm(true); 
        }
        else if (currentState == MenuState.Difficulty || currentState == MenuState.Confirming)
        {
            currentDiffIndex = GetFirstUnlockedDifficulty();
            
            SongGradeData finalSong = allSongData[currentSongIndex].isRandomOption ? secretRandomSong : allSongData[currentSongIndex];
            GameDataBridge.SelectedSong = finalSong;
            GameDataBridge.SelectedDifficulty = currentDiffIndex;
            
            string targetScene = finalSong.gameplaySceneName;
            if (!string.IsNullOrEmpty(targetScene))
                TransitionManager.Instance.LoadScene(targetScene);
        }
    }

    /// <summary>
    /// Resets the arcade countdown timer and clears active warning UI.
    /// </summary>
    public void ResetTimer()
    {
        currentTimer = maxTimerValue;
        if (timerSlider != null) timerSlider.value = 1f;
        
        nextWarningTime = maxTimerValue - 10f; 
        
        if (warningCanvasGroup != null) 
        {
            warningCanvasGroup.alpha = 0f;
            warningCanvasGroup.blocksRaycasts = false;
            warningCanvasGroup.interactable = false;
            warningCanvasGroup.gameObject.SetActive(true); 
        }
        if (warningRoutine != null) StopCoroutine(warningRoutine);
    }

    /// <summary>
    /// Shows the correct control prompt canvas for the current menu state and selection source.
    /// </summary>
    private void UpdateControlCanvases()
    {
        if (controlsSongSelect) 
            controlsSongSelect.SetActive(currentState == MenuState.Carousel);
            
        if (controlsDifficultyNormal) 
            controlsDifficultyNormal.SetActive(currentState == MenuState.Difficulty && !wasAutoSelected);
            
        if (controlsDifficultyForced) 
            controlsDifficultyForced.SetActive(currentState == MenuState.Difficulty && wasAutoSelected);
    }

    /// <summary>
    /// Moves the selected song or difficulty in the requested direction.
    /// </summary>
    /// <param name="dir">Direction of movement, typically -1 for left and 1 for right.</param>
    void OnMove(int dir)
    {
        if (isTransitioning || currentState == MenuState.Confirming || currentState == MenuState.Splash || isPaused || (pauseMenu != null && pauseMenu.IsPaused())) return;

        if (currentState == MenuState.Carousel)
        {
            currentSongIndex = (currentSongIndex + dir + allSongData.Count) % allSongData.Count;
            if (sfxSource && moveSound) sfxSource.PlayOneShot(moveSound);
            UpdateSelectionVisuals();
        }
        else if (currentState == MenuState.Difficulty)
        {
            int next = currentDiffIndex + dir;
            
            while (next >= 0 && next < difficultyBoxes.Length && lockedDifficulties[next])
            {
                next += dir;
            }
            
            next = Mathf.Clamp(next, 0, difficultyBoxes.Length - 1);
            
            if (next != currentDiffIndex && !lockedDifficulties[next])
            {
                currentDiffIndex = next;
                if (sfxSource && moveSound) sfxSource.PlayOneShot(moveSound);
            }
            else if (lockedDifficulties[next] && sfxSource && lockedSound)
            {
                sfxSource.PlayOneShot(lockedSound);
            }
        }
    }

    /// <summary>
    /// Updates the main song preview visuals, resolves random song data, refreshes visual pulses, and starts preview music.
    /// </summary>
    void UpdateSelectionVisuals()
    {
        if (currentState == MenuState.Splash) return;

        SongGradeData currentData = allSongData[currentSongIndex];

        if (currentData.isRandomOption)
        {
            if (secretRandomSong == null)
            {
                List<SongGradeData> validSongs = new List<SongGradeData>();
                foreach (var s in allSongData) 
                {
                    if (!s.isRandomOption) validSongs.Add(s);
                }

                if (validSongs.Count > 0)
                {
                    secretRandomSong = validSongs[Random.Range(0, validSongs.Count)];
                }
            }
        }
        else
        {
            secretRandomSong = null; 
        }

        if (mainSongTitleText != null) mainSongTitleText.text = currentData.songName;
        if (mainCharacterDisplay != null) mainCharacterDisplay.sprite = currentData.characterSprite;
        if (mainBackgroundDisplay != null) mainBackgroundDisplay.sprite = currentData.environmentSprite;

        // Update visual pulses when song changes
        if (autoUpdateVisualPulses)
        {
            UpdateVisualPulses();
        }

        if (musicSource && currentData.songPreviewClip && !isPaused && (pauseMenu == null || !pauseMenu.IsPaused()))
        {
            musicSource.Stop();
            musicSource.clip = currentData.songPreviewClip;
            musicSource.volume = musicMaxVolume;
            musicSource.Play();
        }
    }

    /// <summary>
    /// Updates both visual pulse objects with the current song's gradient.
    /// </summary>
    private void UpdateVisualPulses()
    {
        SongGradeData currentData = allSongData[currentSongIndex];
        SongGradeData actualData = currentData.isRandomOption ? secretRandomSong : currentData;
        
        if (actualData == null) return;
        
        // Update first visual pulse object
        if (visualPulseObject1 != null)
        {
            if (actualData.visualGradient != null)
            {
                visualPulseObject1.UpdateGradientFromSong(actualData);
                Debug.Log($"Updated Visual Pulse 1 with gradient from song: {actualData.songName}");
            }
            else
            {
                Debug.LogWarning($"Song {actualData.songName} has no visual gradient assigned for Visual Pulse 1");
            }
        }
        
        // Update second visual pulse object
        if (visualPulseObject2 != null)
        {
            if (actualData.visualGradient != null)
            {
                visualPulseObject2.UpdateGradientFromSong(actualData);
                Debug.Log($"Updated Visual Pulse 2 with gradient from song: {actualData.songName}");
            }
            else
            {
                Debug.LogWarning($"Song {actualData.songName} has no visual gradient assigned for Visual Pulse 2");
            }
        }
        
        // If both are null, log warning
        if (visualPulseObject1 == null && visualPulseObject2 == null)
        {
            Debug.LogWarning("Both Visual Pulse objects are not assigned in SongCarouselManager");
        }
    }

    /// <summary>
    /// Manually forces both visual pulse objects to use a specific song's gradient data.
    /// </summary>
    /// <param name="songData">Song data whose visual gradient should be applied.</param>
    public void ForceUpdateVisualPulses(SongGradeData songData)
    {
        if (songData == null) return;
        
        if (visualPulseObject1 != null)
            visualPulseObject1.UpdateGradientFromSong(songData);
        
        if (visualPulseObject2 != null)
            visualPulseObject2.UpdateGradientFromSong(songData);
    }

    /// <summary>
    /// Manually forces both visual pulse objects to use the currently selected song.
    /// </summary>
    public void ForceUpdateVisualPulsesWithCurrentSong()
    {
        UpdateVisualPulses();
    }

    /// <summary>
    /// Confirms the current menu selection, moving from carousel to difficulty, from difficulty to confirmation,
    /// or from confirmation into gameplay.
    /// </summary>
    /// <param name="isAuto">Whether the confirmation was triggered by the arcade timer.</param>
    void OnConfirm(bool isAuto = false)
    {
        if (isTransitioning || currentState == MenuState.Splash || isPaused || (pauseMenu != null && pauseMenu.IsPaused())) return;

        if (currentState == MenuState.Carousel)
        {
            wasAutoSelected = isAuto; 
            StartCoroutine(TransitionToDifficulty());
        }
        else if (currentState == MenuState.Difficulty)
        {
            if (lockedDifficulties[currentDiffIndex])
            {
                if (sfxSource && lockedSound) sfxSource.PlayOneShot(lockedSound);
                return;
            }
            
            currentState = MenuState.Confirming;
            if (sfxSource && selectSound) sfxSource.PlayOneShot(selectSound);
            if (pressAgainText) pressAgainText.SetActive(true);
            
            if (controlsDifficultyNormal) controlsDifficultyNormal.SetActive(false);
            if (controlsDifficultyForced) controlsDifficultyForced.SetActive(false);
        }
        else if (currentState == MenuState.Confirming)
        {
            if (sfxSource && confirmSound) sfxSource.PlayOneShot(confirmSound);
            
            SongGradeData finalSong = allSongData[currentSongIndex].isRandomOption ? secretRandomSong : allSongData[currentSongIndex];
            
            GameDataBridge.SelectedSong = finalSong;
            GameDataBridge.SelectedDifficulty = currentDiffIndex;
            
            string targetScene = finalSong.gameplaySceneName;
            if (!string.IsNullOrEmpty(targetScene))
                TransitionManager.Instance.LoadScene(targetScene);
        }
    }

    /// <summary>
    /// Handles cancel input, returning from difficulty selection to song selection when allowed.
    /// </summary>
    void OnCancel()
    {
        if (isTransitioning || currentState == MenuState.Confirming || currentState == MenuState.Splash || isPaused || (pauseMenu != null && pauseMenu.IsPaused())) return;

        if (currentState == MenuState.Difficulty)
        {
            if (wasAutoSelected) return; 

            if (sfxSource && cancelSound) sfxSource.PlayOneShot(cancelSound);
            StartCoroutine(TransitionToCarousel());
        }
    }
    
    // ========== PAUSE SYSTEM METHODS (Using PauseMenu) ==========
    
    /// <summary>
    /// Pauses song selection, preview audio, UI input, and opens the pause menu.
    /// </summary>
    private void PauseGame()
    {
        if (isPaused || (pauseMenu != null && pauseMenu.IsPaused())) return;
        
        isPaused = true;
        
        // Pause music
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();
        
        // Pause sfx
        if (sfxSource != null && sfxSource.isPlaying)
            sfxSource.Pause();
        
        // Open pause menu
        if (pauseMenu != null)
            pauseMenu.OpenPauseMenu(null); // Pass null since this isn't GameplayManager
        
        // Disable input
        input.UI.Disable();
        
        Time.timeScale = 0f;
        
        Debug.Log("Song Carousel Paused");
    }
    
    /// <summary>
    /// Resumes song selection after the pause menu closes.
    /// </summary>
    private void ResumeGame()
    {
        if (!isPaused) return;
        
        // Resume music
        if (musicSource != null && musicSource.clip != null)
            musicSource.UnPause();
        
        // Resume sfx
        if (sfxSource != null)
            sfxSource.UnPause();
        
        // Re-enable input
        input.UI.Enable();
        
        Time.timeScale = 1f;
        
        isPaused = false;
        
        Debug.Log("Song Carousel Resumed");
    }
    
    /// <summary>
    /// Restores time scale and loads the main menu scene.
    /// </summary>
    private void QuitToMenu()
    {
        Debug.Log("Quitting to Main Menu from Song Carousel");
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Load main menu
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadScene("MainMenu");
        else
            SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Fades from the carousel into difficulty selection and configures difficulty locks and step counts.
    /// </summary>
    /// <returns>Coroutine enumerator for the carousel-to-difficulty transition.</returns>
    IEnumerator TransitionToDifficulty()
    {
        isTransitioning = true;
        ResetTimer(); 
        if (sfxSource && selectSound) sfxSource.PlayOneShot(selectSound);
        
        SongGradeData currentData = allSongData[currentSongIndex];
        SongGradeData statsData = currentData.isRandomOption ? secretRandomSong : currentData;

        diffSongTitle.text = currentData.songName; 
        
        if (statsData != null)
        {
            if (diffStepTexts[0] != null)
            {
                diffStepTexts[0].text = statsData.easySteps.ToString();
                diffStepTexts[0].color = unlockedTextColor;
                diffStepTexts[0].fontStyle = FontStyles.Bold;
            }
            if (diffStepTexts[1] != null)
            {
                diffStepTexts[1].text = statsData.mediumSteps.ToString();
                diffStepTexts[1].color = unlockedTextColor;
                diffStepTexts[1].fontStyle = FontStyles.Bold;
            }
            if (diffStepTexts[2] != null)
            {
                diffStepTexts[2].text = statsData.hardSteps.ToString();
                diffStepTexts[2].color = unlockedTextColor;
                diffStepTexts[2].fontStyle = FontStyles.Bold;
            }
            
            lockedDifficulties[0] = (statsData.easySteps == 0);
            lockedDifficulties[1] = (statsData.mediumSteps == 0);
            lockedDifficulties[2] = (statsData.hardSteps == 0);
            
            for (int i = 0; i < difficultyBoxes.Length && i < 3; i++)
            {
                Image boxImg = difficultyBoxes[i].GetComponent<Image>();
                if (boxImg != null)
                {
                    if (lockedDifficulties[i] && lockedSprite != null)
                    {
                        boxImg.sprite = lockedSprite;
                    }
                    else if (originalSprites[i] != null)
                    {
                        boxImg.sprite = originalSprites[i];
                    }
                }
                
                if (diffStepTexts[i] != null)
                {
                    if (lockedDifficulties[i])
                    {
                        diffStepTexts[i].color = lockedTextColor;
                    }
                    else
                    {
                        diffStepTexts[i].color = unlockedTextColor;
                    }
                }
            }
            
            currentDiffIndex = GetFirstUnlockedDifficulty();
        }

        if (diffJacketDisplay != null)
        {
            diffJacketDisplay.sprite = currentData.songJacketSprite;
        }

        yield return StartCoroutine(FadeGroups(carouselCanvasGroup, difficultyCanvasGroup));
        currentState = MenuState.Difficulty;
        UpdateControlCanvases(); 
        difficultyCanvasGroup.blocksRaycasts = true;
        isTransitioning = false;
    }
    
    /// <summary>
    /// Finds the first available difficulty that is not locked.
    /// </summary>
    /// <returns>Index of the first unlocked difficulty, or 0 if all are locked.</returns>
    private int GetFirstUnlockedDifficulty()
    {
        for (int i = 0; i < lockedDifficulties.Length; i++)
        {
            if (!lockedDifficulties[i])
                return i;
        }
        return 0;
    }

    /// <summary>
    /// Fades from difficulty selection back to the song carousel.
    /// </summary>
    /// <returns>Coroutine enumerator for the difficulty-to-carousel transition.</returns>
    IEnumerator TransitionToCarousel()
    {
        isTransitioning = true;
        ResetTimer(); 
        
        if (pressAgainText) pressAgainText.SetActive(false); 

        yield return StartCoroutine(FadeGroups(difficultyCanvasGroup, carouselCanvasGroup));
        
        currentState = MenuState.Carousel;
        UpdateControlCanvases(); 
        difficultyCanvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    /// <summary>
    /// Smoothly moves spawned song blocks toward their carousel slots.
    /// </summary>
    void UpdateCarouselMovement()
    {
        if (isPaused || (pauseMenu != null && pauseMenu.IsPaused())) return;
        
        for (int i = 0; i < spawnedBlocks.Count; i++)
        {
            int rawDiff = i - currentSongIndex;
            if (rawDiff > spawnedBlocks.Count / 2) rawDiff -= spawnedBlocks.Count;
            if (rawDiff <= -spawnedBlocks.Count / 2) rawDiff += spawnedBlocks.Count;

            int targetSlotIndex = rawDiff + 3; 
            if (targetSlotIndex >= 0 && targetSlotIndex < slots.Length)
            {
                spawnedBlocks[i].position = Vector3.Lerp(spawnedBlocks[i].position, slots[targetSlotIndex].position, Time.deltaTime * lerpSpeed);
                spawnedBlocks[i].localScale = Vector3.Lerp(spawnedBlocks[i].localScale, originalScales[i] * slots[targetSlotIndex].localScale.x, Time.deltaTime * lerpSpeed);
                spawnedGroups[i].alpha = Mathf.MoveTowards(spawnedGroups[i].alpha, (targetSlotIndex == 0 || targetSlotIndex == 6) ? 0f : 1f, Time.deltaTime * 5f);
                if (targetSlotIndex == 3) spawnedBlocks[i].SetAsLastSibling();
            }
        }
    }

    /// <summary>
    /// Immediately places spawned song blocks at their current carousel slot positions.
    /// </summary>
    void SnapToPositions()
    {
        for (int i = 0; i < spawnedBlocks.Count; i++)
        {
            int rawDiff = i - currentSongIndex;
            if (rawDiff > spawnedBlocks.Count / 2) rawDiff -= spawnedBlocks.Count;
            if (rawDiff <= -spawnedBlocks.Count / 2) rawDiff += spawnedBlocks.Count;

            int targetSlotIndex = rawDiff + 3; 
            if (targetSlotIndex >= 0 && targetSlotIndex < slots.Length)
            {
                spawnedBlocks[i].position = slots[targetSlotIndex].position;
                spawnedBlocks[i].localScale = originalScales[i] * slots[targetSlotIndex].localScale.x;
                spawnedGroups[i].alpha = (targetSlotIndex == 0 || targetSlotIndex == 6) ? 0f : 1f;
                if (targetSlotIndex == 3) spawnedBlocks[i].SetAsLastSibling();
            }
        }
    }

    /// <summary>
    /// Animates difficulty box scale, tint, and outline state for the current difficulty selection.
    /// </summary>
    void UpdateDifficultyVisuals()
    {
        if (isPaused || (pauseMenu != null && pauseMenu.IsPaused())) return;
        
        for (int i = 0; i < difficultyBoxes.Length; i++)
        {
            if (lockedDifficulties[i]) continue;
            
            bool isCurrent = (i == currentDiffIndex);
            float targetScale = isCurrent ? selectedScale : normalScale;
            difficultyBoxes[i].localScale = Vector3.Lerp(difficultyBoxes[i].localScale, Vector3.one * targetScale, Time.deltaTime * lerpSpeed);

            Image boxImg = difficultyBoxes[i].GetComponent<Image>();
            boxImg.color = Color.Lerp(boxImg.color, (currentState == MenuState.Confirming && !isCurrent) ? unselectedDarkColor : Color.white, Time.deltaTime * 10f);

            if (difficultyOutlines.Length > i && difficultyOutlines[i] != null)
                difficultyOutlines[i].color = isCurrent ? outlineGold : Color.black;
        }
    }

    /// <summary>
    /// Crossfades one canvas group out while another fades in.
    /// </summary>
    /// <param name="from">Canvas group to fade out and disable for raycasts.</param>
    /// <param name="to">Canvas group to fade in.</param>
    /// <returns>Coroutine enumerator for the crossfade animation.</returns>
    IEnumerator FadeGroups(CanvasGroup from, CanvasGroup to)
    {
        from.blocksRaycasts = false; 

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            from.alpha = 1 - t;
            to.alpha = t;
            yield return null;
        }
        from.alpha = 0; to.alpha = 1;
    }

    /// <summary>
    /// Enables the generated input actions when this component becomes active.
    /// </summary>
    void OnEnable() => input?.Enable();
    /// <summary>
    /// Disables the generated input actions when this component becomes inactive.
    /// </summary>
    void OnDisable() => input?.Disable();
}
