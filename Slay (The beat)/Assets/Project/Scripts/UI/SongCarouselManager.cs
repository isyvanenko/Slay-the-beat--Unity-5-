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
    public TextMeshProUGUI splashStageText;
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

    private List<RectTransform> spawnedBlocks = new List<RectTransform>();
    private List<CanvasGroup> spawnedGroups = new List<CanvasGroup>(); 
    private List<Vector3> originalScales = new List<Vector3>(); 

    private int currentSongIndex = 0; 
    private int currentDiffIndex = 1; 
    private InputActions input;
    private bool isTransitioning = false;
    private bool wasAutoSelected = false; 
    
    // --- NEW: Secret variable to hold the hidden track ---
    private SongGradeData secretRandomSong = null;

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
        
        Canvas.ForceUpdateCanvases();
        SnapToPositions();
        
        if (pressAgainText) pressAgainText.SetActive(false);
        UpdateControlCanvases(); 

        StartCoroutine(SplashSequenceRoutine());
    }

    private IEnumerator SplashSequenceRoutine()
    {
        isTransitioning = true;

        if (splashStageText != null)
        {
            if (SessionConfig.CurrentStage >= SessionConfig.MaxStages)
            {
                splashStageText.text = "STAGE " + SessionConfig.CurrentStage + "\n<color=yellow>FINAL STAGE</color>";
            }
            else
            {
                splashStageText.text = "STAGE " + SessionConfig.CurrentStage;
            }
        }

        yield return new WaitForSeconds(splashDuration);
        yield return StartCoroutine(FadeGroups(splashCanvasGroup, carouselCanvasGroup));

        currentState = MenuState.Carousel;
        isTransitioning = false;
        
        UpdateSelectionVisuals(); 
        ResetTimer(); 
        UpdateControlCanvases(); 
    }

    void Update()
    {
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
            currentDiffIndex = 0; 
            
            // --- UPDATED: Pass the secret song if we land on random! ---
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
        if (isTransitioning || currentState == MenuState.Confirming || currentState == MenuState.Splash) return;

        if (currentState == MenuState.Carousel)
        {
            currentSongIndex = (currentSongIndex + dir + allSongData.Count) % allSongData.Count;
            if (sfxSource && moveSound) sfxSource.PlayOneShot(moveSound);
            UpdateSelectionVisuals();
        }
        else if (currentState == MenuState.Difficulty)
        {
            int next = Mathf.Clamp(currentDiffIndex + dir, 0, difficultyBoxes.Length - 1);
            if (next != currentDiffIndex)
            {
                currentDiffIndex = next;
                if (sfxSource && moveSound) sfxSource.PlayOneShot(moveSound);
            }
        }
    }

    void UpdateSelectionVisuals()
    {
        if (currentState == MenuState.Splash) return;

        SongGradeData currentData = allSongData[currentSongIndex];

        // --- NEW: Secret Song Generation ---
        if (currentData.isRandomOption)
        {
            if (secretRandomSong == null)
            {
                // Gather all valid songs that ARE NOT the random block
                List<SongGradeData> validSongs = new List<SongGradeData>();
                foreach (var s in allSongData) 
                {
                    if (!s.isRandomOption) validSongs.Add(s);
                }

                // Pick one randomly
                if (validSongs.Count > 0)
                {
                    secretRandomSong = validSongs[Random.Range(0, validSongs.Count)];
                }
            }
        }
        else
        {
            // Clear the secret song if we move off the random block
            secretRandomSong = null; 
        }

        // Visually, display the currentData (the question marks and mysterious text/music)
        if (mainSongTitleText != null) mainSongTitleText.text = currentData.songName;
        if (mainCharacterDisplay != null) mainCharacterDisplay.sprite = currentData.characterSprite;
        if (mainBackgroundDisplay != null) mainBackgroundDisplay.sprite = currentData.environmentSprite;

        if (musicSource && currentData.songPreviewClip)
        {
            musicSource.Stop();
            musicSource.clip = currentData.songPreviewClip;
            musicSource.volume = musicMaxVolume;
            musicSource.Play();
        }
    }

    void OnConfirm(bool isAuto = false)
    {
        if (isTransitioning || currentState == MenuState.Splash) return;

        if (currentState == MenuState.Carousel)
        {
            wasAutoSelected = isAuto; 
            // We wait to set the actual bridge until they pick difficulty
            StartCoroutine(TransitionToDifficulty());
        }
        else if (currentState == MenuState.Difficulty)
        {
            currentState = MenuState.Confirming;
            if (sfxSource && selectSound) sfxSource.PlayOneShot(selectSound);
            if (pressAgainText) pressAgainText.SetActive(true);
            
            if (controlsDifficultyNormal) controlsDifficultyNormal.SetActive(false);
            if (controlsDifficultyForced) controlsDifficultyForced.SetActive(false);
        }
        else if (currentState == MenuState.Confirming)
        {
            if (sfxSource && confirmSound) sfxSource.PlayOneShot(confirmSound);
            
            // --- UPDATED: Assign the real song to the bridge right before loading ---
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
        if (isTransitioning || currentState == MenuState.Confirming || currentState == MenuState.Splash) return;

        if (currentState == MenuState.Difficulty)
        {
            if (wasAutoSelected) return; 

            if (sfxSource && cancelSound) sfxSource.PlayOneShot(cancelSound);
            StartCoroutine(TransitionToCarousel());
        }
    }

    IEnumerator TransitionToDifficulty()
    {
        isTransitioning = true;
        ResetTimer(); 
        if (sfxSource && selectSound) sfxSource.PlayOneShot(selectSound);
        
        SongGradeData currentData = allSongData[currentSongIndex];
        
        // --- NEW: Grab the stats of the SECRET song if we are on random ---
        SongGradeData statsData = currentData.isRandomOption ? secretRandomSong : currentData;

        // Display the Title and Jacket of the Random Block, but the Numbers of the Secret Song
        diffSongTitle.text = currentData.songName; 
        
        if (statsData != null)
        {
            diffStepTexts[0].text = statsData.easySteps.ToString();
            diffStepTexts[1].text = statsData.mediumSteps.ToString();
            diffStepTexts[2].text = statsData.hardSteps.ToString();
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
        for (int i = 0; i < difficultyBoxes.Length; i++)
        {
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