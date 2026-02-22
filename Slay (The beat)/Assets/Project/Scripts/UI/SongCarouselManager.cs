using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SongCarouselManager : MonoBehaviour
{
    private enum MenuState { Carousel, Difficulty, Confirming }
    [SerializeField] private MenuState currentState = MenuState.Carousel;

    [Header("Data Source")]
    public List<SongGradeData> allSongData;

    [Header("Carousel Setup")]
    public GameObject songPrefab; 
    public Transform container;     
    public RectTransform[] slots; 
    public CanvasGroup carouselCanvasGroup;

    [Header("Difficulty UI Elements")]
    public CanvasGroup difficultyCanvasGroup;
    public RectTransform[] difficultyBoxes; 
    public Image[] difficultyOutlines;
    public TextMeshProUGUI[] diffStepTexts;
    public TextMeshProUGUI diffSongTitle;
    public GameObject pressAgainText; // The "Press Select again to Confirm" prompt

    [Header("Visual Settings")]
    public float lerpSpeed = 10f;
    public float normalScale = 1.0f;
    public float selectedScale = 1.2f;
    public Color outlineGold = new Color(1f, 0.85f, 0f);
    public Color unselectedDarkColor = new Color(0.05f, 0.05f, 0.05f, 1f); // Deep black for unselected

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioSource sfxSource;       
    public AudioClip moveSound;         
    public AudioClip selectSound;
    public AudioClip confirmSound;
    public float musicMaxVolume = 0.5f;

    private List<RectTransform> spawnedBlocks = new List<RectTransform>();
    private List<CanvasGroup> spawnedGroups = new List<CanvasGroup>(); 
    private List<Vector3> originalScales = new List<Vector3>(); 

    private int currentSongIndex = 0; 
    private int currentDiffIndex = 1; 
    private InputActions input;
    private bool isTransitioning = false;

    void Awake()
    {
        input = new InputActions();
        
        // Navigation
        input.UI.NavigateLeft.performed += _ => OnMove(-1);
        input.UI.NavigateRight.performed += _ => OnMove(1);
        
        // Confirmation / Selection
        input.UI.Select.performed += _ => OnConfirm();
        
        // Back / Cancel
        //input.UI.Cancel.performed += _ => OnBack(); 
    }

    void Start()
    {
        if (allSongData.Count == 0) return;

        // Initialize Carousel Blocks
        for (int i = 0; i < allSongData.Count; i++)
        {
            GameObject go = Instantiate(songPrefab, container);
            RectTransform rt = go.GetComponent<RectTransform>();
            originalScales.Add(rt.localScale);
            spawnedBlocks.Add(rt);
            spawnedGroups.Add(go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>());
            go.GetComponent<SongBlock>()?.UpdateData(allSongData[i]);
        }
        
        // Initial UI State
        carouselCanvasGroup.alpha = 1f;
        difficultyCanvasGroup.alpha = 0f;
        difficultyCanvasGroup.blocksRaycasts = false;
        if(pressAgainText) pressAgainText.SetActive(false);

        RefreshCarouselMusic();
    }

    void Update()
    {
        if (isTransitioning) return;

        if (currentState == MenuState.Carousel)
            UpdateCarouselVisuals();
        else
            UpdateDifficultyVisuals();
    }

    // --- INPUT HANDLERS ---
    void OnMove(int dir)
    {
        if (isTransitioning || currentState == MenuState.Confirming) return;

        if (currentState == MenuState.Carousel)
        {
            currentSongIndex = (currentSongIndex + dir + allSongData.Count) % allSongData.Count;
            if (sfxSource && moveSound) sfxSource.PlayOneShot(moveSound);
            RefreshCarouselMusic();
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

    void OnConfirm()
    {
        if (isTransitioning) return;

        if (currentState == MenuState.Carousel)
        {
            GameDataBridge.SelectedSong = allSongData[currentSongIndex];
            StartCoroutine(TransitionToDifficulty());
        }
        else if (currentState == MenuState.Difficulty)
        {
            // First Press: Lock choice
            currentState = MenuState.Confirming;
            if (sfxSource && selectSound) sfxSource.PlayOneShot(selectSound);
            if (pressAgainText) pressAgainText.SetActive(true);
        }
        else if (currentState == MenuState.Confirming)
        {
            // Second Press: Execute Load
            if (sfxSource && confirmSound) sfxSource.PlayOneShot(confirmSound);
            
            GameDataBridge.SelectedDifficulty = currentDiffIndex;
            string targetScene = allSongData[currentSongIndex].gameplaySceneName;
            
            if (!string.IsNullOrEmpty(targetScene))
            {
                TransitionManager.Instance.LoadScene(targetScene);
            }
            else
                Debug.LogError("Scene Name missing on SongData!");
        }
    }

    void OnBack()
    {
        if (isTransitioning) return;

        if (currentState == MenuState.Confirming)
        {
            // Unlock selection
            currentState = MenuState.Difficulty;
            if (pressAgainText) pressAgainText.SetActive(false);
        }
        else if (currentState == MenuState.Difficulty)
        {
            // Return to song selection
            StartCoroutine(TransitionToCarousel());
        }
    }

    // --- TRANSITIONS ---
    IEnumerator TransitionToDifficulty()
    {
        isTransitioning = true;
        if (sfxSource && selectSound) sfxSource.PlayOneShot(selectSound);
        
        // Prep Difficulty UI
        diffSongTitle.text = allSongData[currentSongIndex].songName;
        diffStepTexts[0].text = allSongData[currentSongIndex].easySteps.ToString();
        diffStepTexts[1].text = allSongData[currentSongIndex].mediumSteps.ToString();
        diffStepTexts[2].text = allSongData[currentSongIndex].hardSteps.ToString();

        yield return StartCoroutine(FadeGroups(carouselCanvasGroup, difficultyCanvasGroup));

        currentState = MenuState.Difficulty;
        difficultyCanvasGroup.blocksRaycasts = true;
        isTransitioning = false;
    }

    IEnumerator TransitionToCarousel()
    {
        isTransitioning = true;
        difficultyCanvasGroup.blocksRaycasts = false;

        yield return StartCoroutine(FadeGroups(difficultyCanvasGroup, carouselCanvasGroup));

        currentState = MenuState.Carousel;
        isTransitioning = false;
    }

    IEnumerator FadeGroups(CanvasGroup from, CanvasGroup to)
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            from.alpha = 1 - t;
            to.alpha = t;
            yield return null;
        }
        from.alpha = 0;
        to.alpha = 1;
    }

    // --- VISUAL UPDATES ---
    void UpdateCarouselVisuals()
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

    void UpdateDifficultyVisuals()
    {
        for (int i = 0; i < difficultyBoxes.Length; i++)
        {
            bool isCurrent = (i == currentDiffIndex);
            
            // Scaling
            float targetScale = isCurrent ? selectedScale : normalScale;
            difficultyBoxes[i].localScale = Vector3.Lerp(difficultyBoxes[i].localScale, Vector3.one * targetScale, Time.deltaTime * lerpSpeed);

            // Blackout unselected logic
            Image boxImg = difficultyBoxes[i].GetComponent<Image>();
            if (currentState == MenuState.Confirming)
            {
                boxImg.color = Color.Lerp(boxImg.color, isCurrent ? Color.white : unselectedDarkColor, Time.deltaTime * 10f);
            }
            else
            {
                boxImg.color = Color.Lerp(boxImg.color, Color.white, Time.deltaTime * 10f);
            }

            // Outline logic
            if (difficultyOutlines.Length > i && difficultyOutlines[i] != null)
            {
                difficultyOutlines[i].color = isCurrent ? outlineGold : Color.black;
            }
        }
    }

    void RefreshCarouselMusic()
    {
        if (musicSource && allSongData[currentSongIndex].songPreviewClip)
        {
            musicSource.Stop();
            musicSource.clip = allSongData[currentSongIndex].songPreviewClip;
            musicSource.volume = musicMaxVolume;
            musicSource.Play();
        }
    }

    void OnEnable() => input?.Enable();
    void OnDisable() => input?.Disable();
}