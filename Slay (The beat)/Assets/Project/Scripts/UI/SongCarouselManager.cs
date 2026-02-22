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

    [Header("Main Menu Visuals")]
    public TextMeshProUGUI mainSongTitleText; // The big title at the top
    public Image mainCharacterDisplay;        // The character silhouette/image
    public Image mainBackgroundDisplay;       // The environment/bg image

    [Header("Difficulty UI Elements")]
    public CanvasGroup difficultyCanvasGroup;
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
        input.UI.NavigateLeft.performed += _ => OnMove(-1);
        input.UI.NavigateRight.performed += _ => OnMove(1);
        input.UI.Select.performed += _ => OnConfirm();
    }

    void Start()
{
    if (allSongData.Count == 0) return;

    // 1. Hide the container immediately so we don't see the "spawn pile"
    carouselCanvasGroup.alpha = 0f;

    // 2. Initialize Carousel Blocks
    for (int i = 0; i < allSongData.Count; i++)
    {
        GameObject go = Instantiate(songPrefab, container);
        RectTransform rt = go.GetComponent<RectTransform>();
        
        // Ensure we record the scale properly
        originalScales.Add(rt.localScale);
        spawnedBlocks.Add(rt);
        spawnedGroups.Add(go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>());
        
        go.GetComponent<SongBlock>()?.UpdateData(allSongData[i]);
    }
    
    // 3. FORCE UNITY TO CALCULATE UI POSITIONS
    // This makes sure the 'slots' positions are accurate before we read them
    Canvas.ForceUpdateCanvases();

    // 4. SNAP POSITIONS IMMEDIATELY
    SnapToPositions();

    // 5. Setup UI State
    difficultyCanvasGroup.alpha = 0f;
    difficultyCanvasGroup.blocksRaycasts = false;
    if(pressAgainText) pressAgainText.SetActive(false);

    UpdateSelectionVisuals();

    // 6. Show the carousel now that everything is snapped
    carouselCanvasGroup.alpha = 1f;
}

    void Update()
    {
        if (isTransitioning) return;

        if (currentState == MenuState.Carousel)
            UpdateCarouselMovement();
        else
            UpdateDifficultyVisuals();
    }

    void OnMove(int dir)
    {
        if (isTransitioning || currentState == MenuState.Confirming) return;

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

    // This handles the Name, Images, and Music update
    void UpdateSelectionVisuals()
    {
        SongGradeData currentData = allSongData[currentSongIndex];

        // Update Text
        if (mainSongTitleText != null) mainSongTitleText.text = currentData.songName;

        // Update Sprites
        if (mainCharacterDisplay != null) mainCharacterDisplay.sprite = currentData.characterSprite;
        if (mainBackgroundDisplay != null) mainBackgroundDisplay.sprite = currentData.environmentSprite;

        // Update Music
        if (musicSource && currentData.songPreviewClip)
        {
            musicSource.Stop();
            musicSource.clip = currentData.songPreviewClip;
            musicSource.volume = musicMaxVolume;
            musicSource.Play();
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
            currentState = MenuState.Confirming;
            if (sfxSource && selectSound) sfxSource.PlayOneShot(selectSound);
            if (pressAgainText) pressAgainText.SetActive(true);
        }
        else if (currentState == MenuState.Confirming)
        {
            if (sfxSource && confirmSound) sfxSource.PlayOneShot(confirmSound);
            GameDataBridge.SelectedDifficulty = currentDiffIndex;
            
            string targetScene = allSongData[currentSongIndex].gameplaySceneName;
            if (!string.IsNullOrEmpty(targetScene))
                TransitionManager.Instance.LoadScene(targetScene);
        }
    }

    IEnumerator TransitionToDifficulty()
    {
        isTransitioning = true;
        if (sfxSource && selectSound) sfxSource.PlayOneShot(selectSound);
        
        diffSongTitle.text = allSongData[currentSongIndex].songName;
        diffStepTexts[0].text = allSongData[currentSongIndex].easySteps.ToString();
        diffStepTexts[1].text = allSongData[currentSongIndex].mediumSteps.ToString();
        diffStepTexts[2].text = allSongData[currentSongIndex].hardSteps.ToString();

        yield return StartCoroutine(FadeGroups(carouselCanvasGroup, difficultyCanvasGroup));
        currentState = MenuState.Difficulty;
        difficultyCanvasGroup.blocksRaycasts = true;
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

    // Add this new method to the script
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
            // Instead of Lerp, we set position and scale directly
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