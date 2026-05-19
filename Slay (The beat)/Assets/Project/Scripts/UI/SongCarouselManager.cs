using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SongCarouselManager : MonoBehaviour
{
    private enum MenuState { Splash, Carousel, Difficulty, Confirming }
    [SerializeField] private MenuState currentState = MenuState.Splash;

    [Header("Data Source")]
    public List<SongGradeData> allSongData;

    [Header("Splash Screen")]
    public CanvasGroup splashCanvasGroup;
    public TextMeshProUGUI splashText;
    public float splashDuration = 2.5f; 

    [Header("Arcade Timer & Warnings")]
    public float maxTimerValue = 30f; 
    public TextMeshProUGUI timerText; 
    public Slider timerSlider; 
    private float currentTimer;
    private float nextWarningTime;
    private Coroutine warningRoutine;

    [Header("Control Prompts (Canvases)")]
    public GameObject controlsSongSelect;       
    public GameObject controlsDifficultyNormal; 
    public GameObject controlsDifficultyForced; 
    public CanvasGroup warningCanvasGroup;      

    [Header("Carousel Setup")]
    public GameObject songPrefab; 
    public Transform container;     
    public RectTransform[] slots; 
    public CanvasGroup carouselCanvasGroup;

    [Header("Main Menu Visuals")]
    public TextMeshProUGUI mainSongTitleText; 
    public Image mainCharacterDisplay;        
    public Image mainBackgroundDisplay;       

    [Header("Difficulty UI Elements")]
    public CanvasGroup difficultyCanvasGroup;
    public Image diffJacketDisplay; 
    public RectTransform[] difficultyBoxes; 
    public Image[] difficultyOutlines;
    public TextMeshProUGUI[] diffStepTexts;
    public TextMeshProUGUI diffSongTitle;
    public GameObject pressAgainText; 

    [Header("Difficulty Lock System")]
    public Sprite lockedSprite;
    public Color unlockedTextColor = Color.black;
    public Color lockedTextColor = Color.gray;
    public AudioClip lockedSound;

    [Header("Visual Settings")]
    public float lerpSpeed = 10f;
    public float normalScale = 1.0f;
    public float selectedScale = 1.2f;
    public Color outlineGold = new Color(1f, 0.85f, 0f);
    public Color unselectedDarkColor = new Color(0.05f, 0.05f, 0.05f, 1f);

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioSource sfxSource;       
    public AudioClip moveSound;         
    public AudioClip selectSound;
    public AudioClip confirmSound;
    public AudioClip cancelSound; 
    public float musicMaxVolume = 0.5f;

    [Header("Pause System")]
    public PauseMenu pauseMenu;

    [Header("Visual Pulse Objects")]
    [Tooltip("First object with SpireMusicVisualPulse component")]
    public SpireMusicVisualPulse visualPulseObject1;
    [Tooltip("Second object with SpireMusicVisualPulse component")]
    public SpireMusicVisualPulse visualPulseObject2;
    [Tooltip("Auto-update visual pulses when song changes")]
    public bool autoUpdateVisualPulses = true;

    private List<RectTransform> spawnedBlocks = new List<RectTransform>();
    private List<CanvasGroup> spawnedGroups = new List<CanvasGroup>(); 
    private List<Vector3> originalScales = new List<Vector3>(); 

    private int currentSongIndex = 0; 
    private int currentDiffIndex = 1; 
    private InputActions input;
    private bool isTransitioning = false;
    private bool wasAutoSelected = false; 
    private bool isPaused = false;
    
    private SongGradeData secretRandomSong = null;
    
    private bool[] lockedDifficulties = new bool[3];
    private Sprite[] originalSprites = new Sprite[3];

    void Awake()
    {
        input = new InputActions();
        input.UI.NavigateLeft.performed += _ => OnMove(-1);
        input.UI.NavigateRight.performed += _ => OnMove(1);
        input.UI.Select.performed += _ => OnConfirm(false); 
        input.UI.Cancel.performed += _ => OnCancel(); 
    }

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

    private void UpdateControlCanvases()
    {
        if (controlsSongSelect) 
            controlsSongSelect.SetActive(currentState == MenuState.Carousel);
            
        if (controlsDifficultyNormal) 
            controlsDifficultyNormal.SetActive(currentState == MenuState.Difficulty && !wasAutoSelected);
            
        if (controlsDifficultyForced) 
            controlsDifficultyForced.SetActive(currentState == MenuState.Difficulty && wasAutoSelected);
    }

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
    /// Updates both visual pulse objects with the current song's gradient
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
    /// Manually force update visual pulses with a specific song data
    /// </summary>
    public void ForceUpdateVisualPulses(SongGradeData songData)
    {
        if (songData == null) return;
        
        if (visualPulseObject1 != null)
            visualPulseObject1.UpdateGradientFromSong(songData);
        
        if (visualPulseObject2 != null)
            visualPulseObject2.UpdateGradientFromSong(songData);
    }

    /// <summary>
    /// Manually force update visual pulses with the current selected song
    /// </summary>
    public void ForceUpdateVisualPulsesWithCurrentSong()
    {
        UpdateVisualPulses();
    }

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
    
    private int GetFirstUnlockedDifficulty()
    {
        for (int i = 0; i < lockedDifficulties.Length; i++)
        {
            if (!lockedDifficulties[i])
                return i;
        }
        return 0;
    }

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

    void OnEnable() => input?.Enable();
    void OnDisable() => input?.Disable();
}