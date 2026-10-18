using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.Rendering;

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

    /// <summary>
    /// A named list of songs the player can switch to with Up/Down (e.g. ALL STAGES, REMIX).
    /// </summary>
    [System.Serializable]
    public class SongPlaylist
    {
        /// <summary>Name shown in the playlist selector.</summary>
        public string displayName = "ALL STAGES";
        /// <summary>Shown in the selector but can't be entered yet (e.g. a mode that isn't built).</summary>
        [Tooltip("Shown in the selector but can't be entered yet (e.g. a mode that isn't built)")]
        public bool comingSoon = false;
        /// <summary>Songs shown in the carousel while this playlist is active, including any random song option.</summary>
        public List<SongGradeData> songs = new List<SongGradeData>();

        /// <summary>Whether the player can switch to this playlist.</summary>
        public bool IsSelectable => !comingSoon && songs != null && songs.Count > 0;
    }

    /// <summary>Songs shown in the carousel. Replaced by the active playlist's songs when playlists are set up.</summary>
    [Header("Data Source")]
    [Tooltip("Songs shown in the carousel. Replaced by the active playlist's songs when playlists are set up")]
    public List<SongGradeData> allSongData;

    /// <summary>Playlists switched between with Up/Down, top to bottom. Leave empty to only use <see cref="allSongData"/>.</summary>
    [Header("Playlists")]
    [Tooltip("Playlists switched between with Up/Down, top to bottom. Leave empty to only use All Song Data")]
    public List<SongPlaylist> playlists = new List<SongPlaylist>();
    /// <summary>Playlist shown when the scene opens.</summary>
    [Tooltip("Playlist shown when the scene opens")]
    public int startPlaylistIndex = 1;
    /// <summary>Text showing the playlist above the active one.</summary>
    [Tooltip("Text showing the playlist above the active one")]
    public TextMeshProUGUI previousPlaylistText;
    /// <summary>Text showing the active playlist.</summary>
    [Tooltip("Text showing the active playlist")]
    public TextMeshProUGUI currentPlaylistText;
    /// <summary>Text showing the playlist below the active one.</summary>
    [Tooltip("Text showing the playlist below the active one")]
    public TextMeshProUGUI nextPlaylistText;
    /// <summary>Colour of the active playlist name.</summary>
    public Color activePlaylistColor = Color.white;
    /// <summary>Colour of the neighbouring (inactive) playlist names.</summary>
    public Color inactivePlaylistColor = new Color(1f, 1f, 1f, 0.35f);

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
    /// <summary>Colour multiplier for song covers that aren't selected. Darkens them without making them see-through.</summary>
    [Tooltip("Brightness of the side song covers (1 = full colour, 0 = black). Alpha stays untouched.")]
    [Range(0f, 1f)] public float sideCoverBrightness = 0.35f;

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

    /// <summary>First spire object faded out while the difficulty screen is shown.</summary>
    [Header("Difficulty Transition")]
    [Tooltip("First spire object faded out while the difficulty screen is shown (UI, sprite, or spire pulse object)")]
    public GameObject difficultyFadeSpire1;
    /// <summary>Second spire object faded out while the difficulty screen is shown.</summary>
    [Tooltip("Second spire object faded out while the difficulty screen is shown (UI, sprite, or spire pulse object)")]
    public GameObject difficultyFadeSpire2;

    /// <summary>UI image showing the arrows video, drawn with the same LED dot effect as the demo videos.</summary>
    [Header("Selection Arrows")]
    [Tooltip("UI image showing the arrows video (drawn with the LED dot effect, dark pixels are transparent)")]
    public Image arrowDisplay;
    /// <summary>Looping video containing both arrows that point at the selected song.</summary>
    [Tooltip("Looping video containing both arrows that point at the selected song")]
    public VideoClip arrowVideo;
    /// <summary>Optional render texture the arrow video draws into. One is created at runtime when empty.</summary>
    [Tooltip("Optional render texture the arrow video draws into (created at runtime when empty)")]
    public RenderTexture arrowVideoTexture;

    /// <summary>Spawned carousel block transforms, one for each song entry.</summary>
    private List<RectTransform> spawnedBlocks = new List<RectTransform>();
    /// <summary>Canvas groups used to fade spawned carousel blocks based on slot position.</summary>
    private List<CanvasGroup> spawnedGroups = new List<CanvasGroup>(); 
    /// <summary>Original local scales captured from spawned carousel blocks.</summary>
    private List<Vector3> originalScales = new List<Vector3>(); 

    /// <summary>Index of the currently selected song in <see cref="allSongData"/>.</summary>
    private int currentSongIndex = 0; 
    /// <summary>Index of the active playlist in <see cref="playlists"/>.</summary>
    private int currentPlaylistIndex = 0;
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

    // ========== ANIMATION TUNING (code-only, nothing to assign in the Inspector) ==========

    /// <summary>Duration of panel crossfades between splash, carousel, and difficulty screens.</summary>
    private const float TransitionDuration = 0.4f;
    /// <summary>How much the difficulty panel scales while entering or leaving.</summary>
    private const float PanelScaleOffset = 0.06f;
    /// <summary>Scale kick applied to a carousel block when it becomes selected.</summary>
    private const float CarouselPunch = 0.08f;
    /// <summary>Scale kick applied to a difficulty box when it becomes selected.</summary>
    private const float DifficultyPunch = 0.1f;
    /// <summary>How quickly scale kicks settle back to rest.</summary>
    private const float PunchDecay = 10f;
    /// <summary>Amplitude of the idle "breathing" on the selected item.</summary>
    private const float BreathAmount = 0.015f;
    /// <summary>Speed of the idle "breathing" on the selected item.</summary>
    private const float BreathSpeed = 2.4f;
    /// <summary>Duration of title, character, and background change animations.</summary>
    private const float MainVisualDuration = 0.45f;
    /// <summary>Horizontal distance the character slides in from when the song changes.</summary>
    private const float CharacterSlideDistance = 90f;
    /// <summary>Horizontal distance the title slides in from when the song changes.</summary>
    private const float TitleSlideDistance = 40f;
    /// <summary>Starting zoom of the background when it crossfades in.</summary>
    private const float BackgroundZoom = 1.05f;
    /// <summary>Seconds used to fade out the previous preview track.</summary>
    private const float MusicFadeOutTime = 0.15f;
    /// <summary>Seconds used to fade in the new preview track.</summary>
    private const float MusicFadeInTime = 0.6f;
    /// <summary>Number of LEDs across the width of the demo video.</summary>
    private const float LedDotsAcross = 160f;
    /// <summary>LED diameter as a fraction of its cell (1 = dots touch).</summary>
    private const float LedDotSize = 0.8f;
    /// <summary>Edge softness of each LED.</summary>
    private const float LedDotSoftness = 0.2f;
    /// <summary>Brightness boost so the dotted video doesn't look dimmer than the original.</summary>
    private const float LedBrightness = 1.25f;
    /// <summary>Seconds between samples of the background video's average colour for the arrow tint.</summary>
    private const float ArrowColorSampleInterval = 0.1f;
    /// <summary>How quickly the arrow tint follows the background video's average colour.</summary>
    private const float ArrowColorFollowSpeed = 6f;
    /// <summary>Seconds the old songs take to fade out when switching playlists.</summary>
    private const float PlaylistSwapFadeTime = 0.15f;
    /// <summary>Vertical distance the playlist names slide in from when switching playlists.</summary>
    private const float PlaylistSlideDistance = 30f;

    /// <summary>Smoothed carousel block scales before punch and breathing are applied.</summary>
    private List<Vector3> blockBaseScales = new List<Vector3>();
    /// <summary>Current scale kick per carousel block.</summary>
    private List<float> blockPunch = new List<float>();
    /// <summary>Slot each carousel block occupied last frame, or -1 when off-screen.</summary>
    private List<int> blockSlots = new List<int>();
    /// <summary>Graphics inside each carousel block that get darkened when the block isn't selected.</summary>
    private List<Graphic[]> blockGraphics = new List<Graphic[]>();
    /// <summary>Original colours of each entry in <see cref="blockGraphics"/>.</summary>
    private List<Color[]> blockGraphicColors = new List<Color[]>();
    /// <summary>Current brightness per carousel block (1 = full colour).</summary>
    private List<float> blockBrightness = new List<float>();
    /// <summary>Reusable index list used to sort carousel blocks by depth.</summary>
    private List<int> depthOrder = new List<int>();

    /// <summary>Smoothed difficulty box scales before punch and breathing are applied.</summary>
    private float[] diffBaseScales = new float[0];
    /// <summary>Current scale kick per difficulty box.</summary>
    private float[] diffPunch = new float[0];
    /// <summary>Original rotations of the difficulty boxes, used to restore after a locked shake.</summary>
    private Quaternion[] diffBaseRotations = new Quaternion[0];

    /// <summary>Direction of the last carousel move, used to choose slide-in direction.</summary>
    private int lastMoveDir = 0;
    /// <summary>Song index currently shown in the main visuals, or -1 before the first display.</summary>
    private int lastDisplayedSongIndex = -1;
    /// <summary>Last whole second shown by the timer, used to trigger tick animations.</summary>
    private int lastTimerSecond = -1;
    /// <summary>Current scale kick on the timer text.</summary>
    private float timerPunch = 0f;

    /// <summary>Runtime-created image behind the background used to crossfade between songs.</summary>
    private Image backgroundGhost;
    /// <summary>Active preview music fade coroutine, if any.</summary>
    private Coroutine musicRoutine;

    /// <summary>Two runtime video players, swapped so the old video stays visible while the next one loads.</summary>
    private VideoPlayer[] videoPlayers = new VideoPlayer[0];
    /// <summary>Raw images that display each video player's render texture above the background.</summary>
    private RawImage[] videoLayers = new RawImage[0];
    /// <summary>Render textures each video player draws into.</summary>
    private RenderTexture[] videoTextures = new RenderTexture[0];
    /// <summary>Which video layer is currently showing, or -1 when no video is visible.</summary>
    private int activeVideoLayer = -1;
    /// <summary>Video players that were playing when the menu was paused.</summary>
    private bool[] videoPausedByMenu = new bool[0];
    /// <summary>Active preview video load/fade coroutine, if any.</summary>
    private Coroutine videoRoutine;
    /// <summary>LED screen materials created for the video layers.</summary>
    private Material[] videoMaterials = new Material[0];

    /// <summary>Runtime video player that plays <see cref="arrowVideo"/> on the arrows.</summary>
    private VideoPlayer arrowVideoPlayer;
    /// <summary>Render texture created at runtime for the arrow video, if none was assigned.</summary>
    private RenderTexture arrowRuntimeTexture;
    /// <summary>Whether the arrow video was playing when the menu was paused.</summary>
    private bool arrowVideoPausedByMenu = false;
    /// <summary>LED screen material created for the arrows.</summary>
    private Material arrowMaterial;
    /// <summary>Average colour of the visible background video that the arrows are easing towards.</summary>
    private Color arrowTargetColor = Color.white;
    /// <summary>Current arrow tint applied to the material's _Color.</summary>
    private Color arrowCurrentColor = Color.white;
    /// <summary>Unscaled time of the next background colour sample.</summary>
    private float nextArrowColorSample = 0f;
    /// <summary>Whether a GPU readback of the background colour is still in flight.</summary>
    private bool arrowColorReadbackPending = false;

    /// <summary>Current visibility (0-1) of the difficulty fade spires.</summary>
    private float spireVisibility = 1f;
    /// <summary>Canvas groups used to fade UI spires.</summary>
    private List<CanvasGroup> spireGroups = new List<CanvasGroup>();
    /// <summary>Material pulse effects on the spires, faded through their visibility multiplier.</summary>
    private List<SpireMusicVisualPulse> spirePulses = new List<SpireMusicVisualPulse>();
    /// <summary>Particle pulse effects on the spires, faded through their visibility multiplier.</summary>
    private List<SpireMusicVisualPulseParticles> spireParticlePulses = new List<SpireMusicVisualPulseParticles>();
    /// <summary>Sprite renderers on the spires, with their original alpha.</summary>
    private List<SpriteRenderer> spireSprites = new List<SpriteRenderer>();
    /// <summary>Original alpha of each entry in <see cref="spireSprites"/>.</summary>
    private List<float> spireSpriteAlphas = new List<float>();
    /// <summary>Spires with nothing fadeable; these are toggled on and off instead.</summary>
    private List<GameObject> spireToggleOnly = new List<GameObject>();
    /// <summary>Running animation coroutines keyed by the object they animate.</summary>
    private Dictionary<Object, Coroutine> runningAnimations = new Dictionary<Object, Coroutine>();
    /// <summary>Rest scales captured the first time a transform is animated.</summary>
    private Dictionary<Transform, Vector3> baseScales = new Dictionary<Transform, Vector3>();
    /// <summary>Rest rotations captured the first time a transform is shaken.</summary>
    private Dictionary<Transform, Quaternion> baseRotations = new Dictionary<Transform, Quaternion>();
    /// <summary>Rest anchored positions captured the first time a rect is animated.</summary>
    private Dictionary<RectTransform, Vector2> basePositions = new Dictionary<RectTransform, Vector2>();
    /// <summary>Rest alpha values captured the first time a graphic is animated.</summary>
    private Dictionary<Graphic, float> baseAlphas = new Dictionary<Graphic, float>();

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
        input.Gameplay.Up.performed += _ => OnPlaylistMove(-1);
        input.Gameplay.Down.performed += _ => OnPlaylistMove(1);
    }

    /// <summary>
    /// Initializes carousel content, pause menu callbacks, difficulty defaults, and starts the splash sequence.
    /// </summary>
    void Start()
    {
        if (playlists.Count > 0)
        {
            currentPlaylistIndex = FindSelectablePlaylist(Mathf.Clamp(startPlaylistIndex, 0, playlists.Count - 1));
            if (currentPlaylistIndex >= 0) allSongData = playlists[currentPlaylistIndex].songs;
            UpdatePlaylistTexts(0);
        }

        if (allSongData == null || allSongData.Count == 0) return;

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

        SpawnSongBlocks();

        diffBaseScales = new float[difficultyBoxes.Length];
        diffPunch = new float[difficultyBoxes.Length];
        diffBaseRotations = new Quaternion[difficultyBoxes.Length];
        for (int i = 0; i < difficultyBoxes.Length; i++)
        {
            diffBaseScales[i] = normalScale;
            diffBaseRotations[i] = difficultyBoxes[i].localRotation;
        }

        CreateBackgroundGhost();
        CreateVideoLayers();
        SetupArrows();
        CollectSpireFadeTargets(difficultyFadeSpire1);
        CollectSpireFadeTargets(difficultyFadeSpire2);

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

        // Splash text intro: letters tighten in while fading up, then gently breathe.
        float elapsed = 0f;
        float baseSpacing = splashText != null ? splashText.characterSpacing : 0f;
        float splashAlpha = splashText != null ? GetBaseAlpha(splashText) : 1f;
        Vector3 splashScale = splashText != null ? GetBaseScale(splashText.rectTransform) : Vector3.one;
        while (elapsed < splashDuration)
        {
            elapsed += Time.deltaTime;
            if (splashText != null)
            {
                float intro = EaseOutCubic(Mathf.Clamp01(elapsed / 0.9f));
                splashText.characterSpacing = Mathf.Lerp(baseSpacing + 40f, baseSpacing, intro);
                SetAlpha(splashText, splashAlpha * intro);
                float breath = 1f + Mathf.Sin(elapsed * BreathSpeed) * BreathAmount * intro;
                splashText.rectTransform.localScale = splashScale * Mathf.Lerp(0.92f, 1f, intro) * breath;
            }
            yield return null;
        }

        Canvas.ForceUpdateCanvases();

        foreach (RectTransform slot in slots)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(slot);
        }

        SnapToPositions();
        for (int i = 0; i < spawnedGroups.Count; i++) spawnedGroups[i].alpha = 0f;

        if (splashText != null)
            RunAnimation(splashText, ScaleRoutine(splashText.rectTransform, splashText.rectTransform.localScale, splashScale * 1.15f, TransitionDuration));
        Coroutine introRoutine = StartCoroutine(CarouselIntroRoutine());

        yield return StartCoroutine(FadeGroups(splashCanvasGroup, carouselCanvasGroup));
        yield return introRoutine;

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
        UpdateArrowColor();

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
        else if (currentState == MenuState.Difficulty || currentState == MenuState.Confirming)
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
            int shownSecond = Mathf.CeilToInt(currentTimer);
            timerText.text = shownSecond.ToString();

            // Tick "kick" on each second during the last 10 seconds, stronger in the final 5.
            if (shownSecond != lastTimerSecond)
            {
                if (lastTimerSecond != -1 && shownSecond <= 10)
                    timerPunch = shownSecond <= 5 ? 0.35f : 0.15f;
                lastTimerSecond = shownSecond;
            }
            timerPunch = Mathf.Lerp(timerPunch, 0f, Damp(PunchDecay));
            timerText.rectTransform.localScale = GetBaseScale(timerText.rectTransform) * (1f + timerPunch);
            timerText.color = Color.Lerp(timerText.color, currentTimer < 5f ? Color.red : Color.white, Damp(8f));
        }

        if (timerSlider != null)
        {
            float targetValue = Mathf.Clamp01(currentTimer / maxTimerValue);
            timerSlider.value = Mathf.Lerp(timerSlider.value, targetValue, Damp(5f));
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
        lastTimerSecond = -1;
        // The slider is not snapped to full here; HandleArcadeTimer smoothly refills it.

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
            lastMoveDir = dir;
            blockPunch[currentSongIndex] = CarouselPunch;
            ApplyDepthOrder();
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
                diffPunch[currentDiffIndex] = DifficultyPunch;
                if (sfxSource && moveSound) sfxSource.PlayOneShot(moveSound);
            }
            else if (lockedDifficulties[next])
            {
                RunAnimation(difficultyBoxes[next], ShakeRoutine(difficultyBoxes[next], diffBaseRotations[next]));
                if (sfxSource && lockedSound) sfxSource.PlayOneShot(lockedSound);
            }
            else
            {
                // Hit the end of the row: small nudge on the current box.
                diffPunch[currentDiffIndex] = DifficultyPunch * 0.5f;
            }
        }
    }

    /// <summary>
    /// Switches to the playlist above (-1) or below (1), skipping playlists that can't be entered yet.
    /// </summary>
    /// <param name="dir">-1 for Up, 1 for Down.</param>
    void OnPlaylistMove(int dir)
    {
        if (playlists.Count == 0 || currentPlaylistIndex < 0) return;
        if (isTransitioning || currentState != MenuState.Carousel || isPaused || (pauseMenu != null && pauseMenu.IsPaused())) return;

        int next = currentPlaylistIndex + dir;
        while (next >= 0 && next < playlists.Count && !playlists[next].IsSelectable)
            next += dir;

        if (next < 0 || next >= playlists.Count)
        {
            // Nothing to switch to. Shake the neighbour if it's a "coming soon" playlist.
            int neighbour = currentPlaylistIndex + dir;
            TextMeshProUGUI neighbourText = dir < 0 ? previousPlaylistText : nextPlaylistText;
            if (neighbour >= 0 && neighbour < playlists.Count && neighbourText != null)
            {
                RectTransform rt = neighbourText.rectTransform;
                RunAnimation(rt, ShakeRoutine(rt, GetBaseRotation(rt)));
                if (sfxSource && lockedSound) sfxSource.PlayOneShot(lockedSound);
            }
            return;
        }

        StartCoroutine(SwitchPlaylistRoutine(next, dir));
    }

    /// <summary>
    /// Fades out the current songs, swaps the carousel to another playlist's songs, and pops the new songs in.
    /// </summary>
    /// <param name="index">Playlist to switch to.</param>
    /// <param name="dir">Direction of the switch, used for the name slide.</param>
    /// <returns>Coroutine enumerator for the playlist swap.</returns>
    private IEnumerator SwitchPlaylistRoutine(int index, int dir)
    {
        isTransitioning = true;
        currentPlaylistIndex = index;
        allSongData = playlists[index].songs;
        if (sfxSource && moveSound) sfxSource.PlayOneShot(moveSound);
        UpdatePlaylistTexts(dir);

        float[] startAlphas = new float[spawnedGroups.Count];
        for (int i = 0; i < spawnedGroups.Count; i++) startAlphas[i] = spawnedGroups[i].alpha;

        float elapsed = 0f;
        while (elapsed < PlaylistSwapFadeTime)
        {
            elapsed += Time.deltaTime;
            float remaining = 1f - Mathf.Clamp01(elapsed / PlaylistSwapFadeTime);
            for (int i = 0; i < spawnedGroups.Count; i++) spawnedGroups[i].alpha = startAlphas[i] * remaining;
            yield return null;
        }

        ClearSongBlocks();
        SpawnSongBlocks();

        currentSongIndex = 0;
        secretRandomSong = null;
        lastDisplayedSongIndex = -1;
        lastMoveDir = 0;

        SnapToPositions();
        for (int i = 0; i < spawnedGroups.Count; i++) spawnedGroups[i].alpha = 0f;
        UpdateSelectionVisuals();

        yield return StartCoroutine(CarouselIntroRoutine());
        isTransitioning = false;
    }

    /// <summary>
    /// Spawns one carousel block per entry in <see cref="allSongData"/>.
    /// </summary>
    private void SpawnSongBlocks()
    {
        for (int i = 0; i < allSongData.Count; i++)
        {
            GameObject go = Instantiate(songPrefab, container);
            RectTransform rt = go.GetComponent<RectTransform>();

            originalScales.Add(rt.localScale);
            spawnedBlocks.Add(rt);
            spawnedGroups.Add(go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>());
            blockBaseScales.Add(rt.localScale);
            blockPunch.Add(0f);
            blockSlots.Add(-1);

            Graphic[] graphics = go.GetComponentsInChildren<Graphic>(true);
            Color[] colors = new Color[graphics.Length];
            for (int g = 0; g < graphics.Length; g++) colors[g] = graphics[g].color;
            blockGraphics.Add(graphics);
            blockGraphicColors.Add(colors);
            blockBrightness.Add(-1f);

            go.GetComponent<SongBlock>()?.UpdateData(allSongData[i]);
        }
    }

    /// <summary>
    /// Destroys the spawned carousel blocks and clears their per-block state.
    /// </summary>
    private void ClearSongBlocks()
    {
        foreach (RectTransform block in spawnedBlocks)
        {
            if (block == null) continue;
            baseScales.Remove(block);
            Destroy(block.gameObject);
        }

        spawnedBlocks.Clear();
        spawnedGroups.Clear();
        originalScales.Clear();
        blockBaseScales.Clear();
        blockPunch.Clear();
        blockSlots.Clear();
        blockGraphics.Clear();
        blockGraphicColors.Clear();
        blockBrightness.Clear();
    }

    /// <summary>
    /// Returns the playlist at or nearest to <paramref name="preferred"/> that can be entered, or -1 if none can.
    /// </summary>
    private int FindSelectablePlaylist(int preferred)
    {
        for (int offset = 0; offset < playlists.Count; offset++)
        {
            int below = preferred + offset;
            if (below < playlists.Count && playlists[below].IsSelectable) return below;
            int above = preferred - offset;
            if (above >= 0 && playlists[above].IsSelectable) return above;
        }
        return -1;
    }

    /// <summary>
    /// Shows the active playlist name and its neighbours above and below.
    /// </summary>
    /// <param name="dir">Direction of the switch that caused the update; 0 updates without animating.</param>
    private void UpdatePlaylistTexts(int dir)
    {
        SetPlaylistText(previousPlaylistText, currentPlaylistIndex - 1, false, dir);
        SetPlaylistText(currentPlaylistText, currentPlaylistIndex, true, dir);
        SetPlaylistText(nextPlaylistText, currentPlaylistIndex + 1, false, dir);
    }

    /// <summary>
    /// Fills one playlist name text, hiding it when there's no playlist at that position.
    /// </summary>
    private void SetPlaylistText(TextMeshProUGUI text, int index, bool active, int dir)
    {
        if (text == null) return;

        bool exists = currentPlaylistIndex >= 0 && index >= 0 && index < playlists.Count;
        text.gameObject.SetActive(exists);
        if (!exists) return;

        text.text = playlists[index].displayName;
        Color target = active ? activePlaylistColor : inactivePlaylistColor;
        if (dir == 0)
        {
            text.color = target;
            return;
        }

        // Down scrolls the list up, so the names come in from below (and the other way for Up).
        Vector2 fromOffset = new Vector2(0f, -dir * PlaylistSlideDistance);
        RunAnimation(text, PlaylistTextRoutine(text, target, fromOffset, active ? 1.12f : 1f));
    }

    /// <summary>
    /// Slides and fades a playlist name into place with its active or inactive colour.
    /// </summary>
    private IEnumerator PlaylistTextRoutine(TextMeshProUGUI text, Color target, Vector2 fromOffset, float fromScale)
    {
        RectTransform rt = text.rectTransform;
        Vector2 restPos = GetBasePosition(rt);
        Vector3 restScale = GetBaseScale(rt);
        const float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float eased = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
            rt.anchoredPosition = restPos + fromOffset * (1f - eased);
            rt.localScale = restScale * Mathf.Lerp(fromScale, 1f, eased);
            Color color = target;
            color.a *= eased;
            text.color = color;
            yield return null;
        }

        rt.anchoredPosition = restPos;
        rt.localScale = restScale;
        text.color = target;
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

        // Re-displaying the same song (e.g. the initial layout refresh) should not replay animations or restart music.
        bool songChanged = currentSongIndex != lastDisplayedSongIndex;
        lastDisplayedSongIndex = currentSongIndex;

        if (mainSongTitleText != null)
        {
            mainSongTitleText.text = currentData.songName;
            if (songChanged)
                RunAnimation(mainSongTitleText, PopInRoutine(mainSongTitleText, new Vector2(lastMoveDir * TitleSlideDistance, 0f), 1.08f, MainVisualDuration));
        }

        if (mainCharacterDisplay != null)
        {
            bool spriteChanged = mainCharacterDisplay.sprite != currentData.characterSprite;
            mainCharacterDisplay.sprite = currentData.characterSprite;
            if (songChanged && spriteChanged)
            {
                Vector2 slideFrom = lastMoveDir != 0 ? new Vector2(lastMoveDir * CharacterSlideDistance, 0f) : new Vector2(0f, -CharacterSlideDistance * 0.5f);
                RunAnimation(mainCharacterDisplay, PopInRoutine(mainCharacterDisplay, slideFrom, 0.96f, MainVisualDuration * 1.2f));
            }
        }

        // Songs with a demo video hide the background image entirely (PreviewVideoRoutine fades it out
        // once the video is ready), so the gaps between the LED dots are fully transparent.
        bool songHasVideo = currentData.previewVideo != null && videoPlayers.Length == 2;
        if (mainBackgroundDisplay != null && !songHasVideo)
        {
            Sprite previous = mainBackgroundDisplay.sprite;
            bool backgroundHidden = mainBackgroundDisplay.color.a < GetBaseAlpha(mainBackgroundDisplay) - 0.01f;
            if (songChanged && (previous != currentData.environmentSprite || backgroundHidden))
                RunAnimation(mainBackgroundDisplay, BackgroundCrossfadeRoutine(previous, currentData.environmentSprite));
            else
                mainBackgroundDisplay.sprite = currentData.environmentSprite;
        }

        // Update visual pulses when song changes
        if (autoUpdateVisualPulses)
        {
            UpdateVisualPulses();
        }

        if (songChanged && !isPaused && (pauseMenu == null || !pauseMenu.IsPaused()))
        {
            bool hasVideo = currentData.previewVideo != null && videoPlayers.Length == 2;

            // Songs with a demo video start their preview music only once the video is ready (see PreviewVideoRoutine).
            if (videoPlayers.Length == 2)
            {
                if (videoRoutine != null) StopCoroutine(videoRoutine);
                videoRoutine = StartCoroutine(PreviewVideoRoutine(currentData));
            }

            if (!hasVideo && musicSource && currentData.songPreviewClip)
            {
                if (musicRoutine != null) StopCoroutine(musicRoutine);
                musicRoutine = StartCoroutine(SwitchPreviewRoutine(currentData.songPreviewClip));
            }
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
            
            // A single confirm starts the song. Confirming state locks further input and dims the other boxes.
            currentState = MenuState.Confirming;
            diffPunch[currentDiffIndex] = DifficultyPunch * 2.5f;
            if (sfxSource && confirmSound) sfxSource.PlayOneShot(confirmSound);

            if (controlsDifficultyNormal) controlsDifficultyNormal.SetActive(false);
            if (controlsDifficultyForced) controlsDifficultyForced.SetActive(false);
            
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

        // Pause preview videos
        for (int i = 0; i < videoPlayers.Length; i++)
        {
            videoPausedByMenu[i] = videoPlayers[i].isPlaying;
            if (videoPausedByMenu[i]) videoPlayers[i].Pause();
        }
        arrowVideoPausedByMenu = arrowVideoPlayer != null && arrowVideoPlayer.isPlaying;
        if (arrowVideoPausedByMenu) arrowVideoPlayer.Pause();

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

        // Resume preview videos
        for (int i = 0; i < videoPlayers.Length; i++)
        {
            if (videoPausedByMenu[i]) videoPlayers[i].Play();
            videoPausedByMenu[i] = false;
        }
        if (arrowVideoPausedByMenu) arrowVideoPlayer.Play();
        arrowVideoPausedByMenu = false;

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

        //diffSongTitle.text = currentData.songName; 
        
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

        // The song select screen stays up: only the selected song card remains while the other cards,
        // the character, and the spires fade away. The difficulty panel scales in on top with its boxes staggered.
        Coroutine popRoutine = StartCoroutine(CarouselConfirmPopRoutine(currentSongIndex));
        Coroutine focusRoutine = StartCoroutine(SongFocusRoutine(true));
        Coroutine boxesRoutine = StartCoroutine(DifficultyIntroRoutine());
        if (diffSongTitle != null)
            RunAnimation(diffSongTitle, PopInRoutine(diffSongTitle, new Vector2(0f, -20f), 1.1f, MainVisualDuration));

        yield return StartCoroutine(FadeGroups(null, difficultyCanvasGroup));
        yield return popRoutine;
        yield return focusRoutine;
        yield return boxesRoutine;
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
        
        HidePressAgain();

        // Bring the other song cards, the character, and the spires back while the difficulty panel leaves.
        Coroutine focusRoutine = StartCoroutine(SongFocusRoutine(false));
        yield return StartCoroutine(FadeGroups(difficultyCanvasGroup, null));
        yield return focusRoutine;

        blockPunch[currentSongIndex] = CarouselPunch;
        currentState = MenuState.Carousel;
        UpdateControlCanvases(); 
        difficultyCanvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    /// <summary>
    /// Smoothly moves spawned song blocks toward their carousel slots, with selection kicks and idle breathing.
    /// </summary>
    void UpdateCarouselMovement()
    {
        if (isPaused || (pauseMenu != null && pauseMenu.IsPaused())) return;

        float move = Damp(lerpSpeed);
        float punchSettle = Damp(PunchDecay);
        float fade = Time.deltaTime * 5f;
        float breath = Mathf.Sin(Time.time * BreathSpeed) * BreathAmount;

        for (int i = 0; i < spawnedBlocks.Count; i++)
        {
            int targetSlotIndex = GetTargetSlot(i);
            if (IsSlotValid(targetSlotIndex))
            {
                Vector3 targetScale = originalScales[i] * slots[targetSlotIndex].localScale.x;

                // A block wrapping around the edge would fly across the whole screen; teleport it while it's invisible instead.
                bool wrapped = blockSlots[i] < 0 || Mathf.Abs(blockSlots[i] - targetSlotIndex) > 1;
                if (wrapped && spawnedGroups[i].alpha < 0.05f)
                {
                    spawnedBlocks[i].position = slots[targetSlotIndex].position;
                    blockBaseScales[i] = targetScale;
                }
                else
                {
                    spawnedBlocks[i].position = Vector3.Lerp(spawnedBlocks[i].position, slots[targetSlotIndex].position, move);
                    blockBaseScales[i] = Vector3.Lerp(blockBaseScales[i], targetScale, move);
                }
                blockSlots[i] = targetSlotIndex;

                blockPunch[i] = Mathf.Lerp(blockPunch[i], 0f, punchSettle);
                float extra = 1f + blockPunch[i] + (targetSlotIndex == 3 ? breath : 0f);
                spawnedBlocks[i].localScale = blockBaseScales[i] * extra;
                spawnedGroups[i].alpha = Mathf.MoveTowards(spawnedGroups[i].alpha, IsEdgeSlot(targetSlotIndex) ? 0f : 1f, fade);
                SetBlockBrightness(i, Mathf.MoveTowards(blockBrightness[i], GetTargetBrightness(targetSlotIndex), fade));
            }
            else
            {
                blockSlots[i] = -1;
                spawnedGroups[i].alpha = Mathf.MoveTowards(spawnedGroups[i].alpha, 0f, fade);
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
            int targetSlotIndex = GetTargetSlot(i);
            blockPunch[i] = 0f;
            if (IsSlotValid(targetSlotIndex))
            {
                spawnedBlocks[i].position = slots[targetSlotIndex].position;
                blockBaseScales[i] = originalScales[i] * slots[targetSlotIndex].localScale.x;
                spawnedBlocks[i].localScale = blockBaseScales[i];
                spawnedGroups[i].alpha = IsEdgeSlot(targetSlotIndex) ? 0f : 1f;
                SetBlockBrightness(i, GetTargetBrightness(targetSlotIndex));
                blockSlots[i] = targetSlotIndex;
            }
            else
            {
                spawnedGroups[i].alpha = 0f;
                blockSlots[i] = -1;
            }
        }
        ApplyDepthOrder();
    }

    /// <summary>
    /// Returns the carousel slot a block should occupy for the current selection.
    /// </summary>
    /// <param name="blockIndex">Index of the spawned block.</param>
    /// <returns>Slot index, which may be outside the slot array for off-screen blocks.</returns>
    private int GetTargetSlot(int blockIndex)
    {
        int rawDiff = blockIndex - currentSongIndex;
        if (rawDiff > spawnedBlocks.Count / 2) rawDiff -= spawnedBlocks.Count;
        if (rawDiff <= -spawnedBlocks.Count / 2) rawDiff += spawnedBlocks.Count;
        return rawDiff + 3;
    }

    /// <summary>Whether a slot index refers to an existing slot.</summary>
    private bool IsSlotValid(int slot) => slot >= 0 && slot < slots.Length;

    /// <summary>Whether a slot is one of the hidden edge slots.</summary>
    private bool IsEdgeSlot(int slot) => slot == 0 || slot == 6;

    /// <summary>Brightness a block should have in a slot: full colour in the centre, dimmed on the sides.</summary>
    private float GetTargetBrightness(int slot) => slot == 3 ? 1f : sideCoverBrightness;

    /// <summary>
    /// Darkens a carousel block by scaling the RGB of its graphics. Alpha is kept as-is so the cover stays opaque.
    /// </summary>
    private void SetBlockBrightness(int blockIndex, float brightness)
    {
        if (blockIndex >= blockBrightness.Count) return;
        if (blockBrightness[blockIndex] < 0f) brightness = GetTargetBrightness(GetTargetSlot(blockIndex));
        if (Mathf.Approximately(blockBrightness[blockIndex], brightness)) return;
        blockBrightness[blockIndex] = brightness;

        Graphic[] graphics = blockGraphics[blockIndex];
        Color[] colors = blockGraphicColors[blockIndex];
        for (int g = 0; g < graphics.Length; g++)
        {
            if (graphics[g] == null) continue;
            Color c = colors[g];
            graphics[g].color = new Color(c.r * brightness, c.g * brightness, c.b * brightness, c.a);
        }
    }

    /// <summary>
    /// Orders carousel blocks so the selected song is drawn on top and neighbours layer outward.
    /// </summary>
    private void ApplyDepthOrder()
    {
        depthOrder.Clear();
        for (int i = 0; i < spawnedBlocks.Count; i++) depthOrder.Add(i);
        depthOrder.Sort((a, b) => SlotDistance(b).CompareTo(SlotDistance(a)));
        foreach (int index in depthOrder) spawnedBlocks[index].SetAsLastSibling();
    }

    /// <summary>Distance of a block's target slot from the center slot.</summary>
    private int SlotDistance(int blockIndex)
    {
        int slot = GetTargetSlot(blockIndex);
        return IsSlotValid(slot) ? Mathf.Abs(slot - 3) : int.MaxValue;
    }

    /// <summary>
    /// Animates difficulty box scale, tint, and outline state for the current difficulty selection.
    /// </summary>
    void UpdateDifficultyVisuals()
    {
        if (isPaused || (pauseMenu != null && pauseMenu.IsPaused())) return;

        float move = Damp(lerpSpeed);
        float punchSettle = Damp(PunchDecay);
        float colorBlend = Damp(10f);
        float breath = Mathf.Sin(Time.time * BreathSpeed) * BreathAmount;

        for (int i = 0; i < difficultyBoxes.Length; i++)
        {
            if (lockedDifficulties[i]) continue;

            bool isCurrent = (i == currentDiffIndex);
            float targetScale = isCurrent ? selectedScale : normalScale;
            diffBaseScales[i] = Mathf.Lerp(diffBaseScales[i], targetScale, move);
            diffPunch[i] = Mathf.Lerp(diffPunch[i], 0f, punchSettle);
            float extra = 1f + diffPunch[i] + (isCurrent ? breath : 0f);
            difficultyBoxes[i].localScale = Vector3.one * diffBaseScales[i] * extra;

            Image boxImg = difficultyBoxes[i].GetComponent<Image>();
            if (boxImg != null)
                boxImg.color = Color.Lerp(boxImg.color, (currentState == MenuState.Confirming && !isCurrent) ? unselectedDarkColor : Color.white, colorBlend);

            if (difficultyOutlines.Length > i && difficultyOutlines[i] != null)
                difficultyOutlines[i].color = Color.Lerp(difficultyOutlines[i].color, isCurrent ? outlineGold : Color.black, colorBlend * 1.5f);
        }
    }

    /// <summary>
    /// Crossfades one canvas group out while another fades in, with eased, slightly staggered timing.
    /// The difficulty panel also scales in and out for depth.
    /// </summary>
    /// <param name="from">Canvas group to fade out and disable for raycasts, or null to only fade in.</param>
    /// <param name="to">Canvas group to fade in, or null to only fade out.</param>
    /// <returns>Coroutine enumerator for the crossfade animation.</returns>
    IEnumerator FadeGroups(CanvasGroup from, CanvasGroup to)
    {
        if (from != null) from.blocksRaycasts = false;

        Transform panel = difficultyCanvasGroup != null ? difficultyCanvasGroup.transform : null;
        Vector3 panelScale = panel != null ? GetBaseScale(panel) : Vector3.one;
        bool panelOut = from != null && from == difficultyCanvasGroup && panel != null;
        bool panelIn = to != null && to == difficultyCanvasGroup && panel != null;
        float fromStart = from != null ? from.alpha : 0f;

        float t = 0;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / TransitionDuration);
            if (from != null) from.alpha = fromStart * (1f - EaseOutCubic(Mathf.Clamp01(t / 0.7f)));
            if (to != null) to.alpha = EaseOutCubic(Mathf.Clamp01((t - 0.2f) / 0.8f));

            if (panelOut) panel.localScale = panelScale * Mathf.Lerp(1f, 1f + PanelScaleOffset, EaseOutCubic(t));
            if (panelIn) panel.localScale = panelScale * Mathf.Lerp(1f - PanelScaleOffset, 1f, EaseOutCubic(t));
            yield return null;
        }
        if (from != null) from.alpha = 0;
        if (to != null) to.alpha = 1;
        if (panel != null) panel.localScale = panelScale;
    }

    /// <summary>
    /// Focuses the song select screen on the chosen song (or releases that focus): the other song cards,
    /// the character, and the difficulty fade spires fade out while the carousel canvas itself stays visible.
    /// </summary>
    /// <param name="focus">True to fade everything but the selected song out, false to bring it all back.</param>
    /// <returns>Coroutine enumerator for the focus animation.</returns>
    private IEnumerator SongFocusRoutine(bool focus)
    {
        float duration = TransitionDuration;

        if (mainCharacterDisplay != null)
        {
            float characterTarget = focus ? 0f : GetBaseAlpha(mainCharacterDisplay);
            RunAnimation(mainCharacterDisplay, CharacterFocusRoutine(characterTarget, focus, duration));
        }

        if (!focus) SetSpireToggles(true);

        float[] startAlphas = new float[spawnedGroups.Count];
        for (int i = 0; i < spawnedGroups.Count; i++) startAlphas[i] = spawnedGroups[i].alpha;
        float spireStart = spireVisibility;
        float spireTarget = focus ? 0f : 1f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float eased = EaseOutCubic(Mathf.Clamp01(elapsed / duration));

            for (int i = 0; i < spawnedGroups.Count; i++)
            {
                if (i == currentSongIndex) continue;
                spawnedGroups[i].alpha = Mathf.Lerp(startAlphas[i], GetFocusAlpha(i, focus), eased);
            }
            SetSpireVisibility(Mathf.Lerp(spireStart, spireTarget, eased));
            yield return null;
        }

        for (int i = 0; i < spawnedGroups.Count; i++)
        {
            if (i == currentSongIndex) continue;
            spawnedGroups[i].alpha = GetFocusAlpha(i, focus);
        }
        SetSpireVisibility(spireTarget);
        if (focus) SetSpireToggles(false);
    }

    /// <summary>Alpha a song card should have when the selected song is focused or released.</summary>
    private float GetFocusAlpha(int blockIndex, bool focus)
    {
        if (focus) return 0f;
        int slot = blockSlots[blockIndex];
        return slot >= 0 && !IsEdgeSlot(slot) ? 1f : 0f;
    }

    /// <summary>
    /// Fades the character out (with a slight drop and shrink) or back in to its rest state.
    /// </summary>
    /// <param name="targetAlpha">Alpha to fade to.</param>
    /// <param name="hide">True when hiding the character.</param>
    /// <param name="duration">Fade length in seconds.</param>
    /// <returns>Coroutine enumerator for the character fade.</returns>
    private IEnumerator CharacterFocusRoutine(float targetAlpha, bool hide, float duration)
    {
        RectTransform rt = mainCharacterDisplay.rectTransform;
        Vector3 restScale = GetBaseScale(rt);
        Vector2 restPos = GetBasePosition(rt);
        Vector3 hiddenScale = restScale * 0.95f;
        Vector2 hiddenPos = restPos + new Vector2(0f, -CharacterSlideDistance * 0.4f);

        float startAlpha = mainCharacterDisplay.color.a;
        Vector3 startScale = rt.localScale;
        Vector2 startPos = rt.anchoredPosition;
        Vector3 endScale = hide ? hiddenScale : restScale;
        Vector2 endPos = hide ? hiddenPos : restPos;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float eased = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
            SetAlpha(mainCharacterDisplay, Mathf.Lerp(startAlpha, targetAlpha, eased));
            rt.localScale = Vector3.Lerp(startScale, endScale, eased);
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            yield return null;
        }

        SetAlpha(mainCharacterDisplay, targetAlpha);
        rt.localScale = endScale;
        rt.anchoredPosition = endPos;
    }

    /// <summary>
    /// Finds everything fadeable on a spire object: UI (via a canvas group), spire pulse effects, and sprites.
    /// Objects with none of these are toggled on and off instead of faded.
    /// </summary>
    /// <param name="spire">Spire object to inspect; ignored when null.</param>
    private void CollectSpireFadeTargets(GameObject spire)
    {
        if (spire == null) return;
        bool found = false;

        if (spire.GetComponentInChildren<Graphic>(true) != null)
        {
            CanvasGroup group = spire.GetComponent<CanvasGroup>();
            if (group == null) group = spire.AddComponent<CanvasGroup>();
            spireGroups.Add(group);
            found = true;
        }

        foreach (SpireMusicVisualPulse pulse in spire.GetComponentsInChildren<SpireMusicVisualPulse>(true))
        {
            spirePulses.Add(pulse);
            found = true;
        }

        foreach (SpireMusicVisualPulseParticles pulse in spire.GetComponentsInChildren<SpireMusicVisualPulseParticles>(true))
        {
            spireParticlePulses.Add(pulse);
            found = true;
        }

        foreach (SpriteRenderer sprite in spire.GetComponentsInChildren<SpriteRenderer>(true))
        {
            spireSprites.Add(sprite);
            spireSpriteAlphas.Add(sprite.color.a);
            found = true;
        }

        if (!found) spireToggleOnly.Add(spire);
    }

    /// <summary>Applies a visibility value (0-1) to every fadeable part of the difficulty fade spires.</summary>
    private void SetSpireVisibility(float visibility)
    {
        spireVisibility = visibility;
        foreach (CanvasGroup group in spireGroups) if (group != null) group.alpha = visibility;
        foreach (SpireMusicVisualPulse pulse in spirePulses) if (pulse != null) pulse.Visibility = visibility;
        foreach (SpireMusicVisualPulseParticles pulse in spireParticlePulses) if (pulse != null) pulse.Visibility = visibility;
        for (int i = 0; i < spireSprites.Count; i++)
        {
            if (spireSprites[i] == null) continue;
            Color color = spireSprites[i].color;
            color.a = spireSpriteAlphas[i] * visibility;
            spireSprites[i].color = color;
        }
    }

    /// <summary>Shows or hides spires that cannot be faded.</summary>
    private void SetSpireToggles(bool visible)
    {
        foreach (GameObject spire in spireToggleOnly) if (spire != null) spire.SetActive(visible);
    }

    // ========== ANIMATION HELPERS ==========

    /// <summary>
    /// Scales carousel blocks in from the center outward when the carousel first appears.
    /// </summary>
    /// <returns>Coroutine enumerator for the intro animation.</returns>
    private IEnumerator CarouselIntroRoutine()
    {
        const float duration = 0.5f;
        const float stagger = 0.07f;
        float total = duration + stagger * 3f;
        float elapsed = 0f;

        while (elapsed < total)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < spawnedBlocks.Count; i++)
            {
                int slot = blockSlots[i];
                if (slot < 0) continue;

                float local = Mathf.Clamp01((elapsed - Mathf.Abs(slot - 3) * stagger) / duration);
                spawnedBlocks[i].localScale = blockBaseScales[i] * Mathf.LerpUnclamped(0.7f, 1f, EaseOutBack(local));
                spawnedGroups[i].alpha = (IsEdgeSlot(slot) ? 0f : 1f) * EaseOutCubic(local);
            }
            yield return null;
        }

        for (int i = 0; i < spawnedBlocks.Count; i++)
        {
            if (blockSlots[i] < 0) continue;
            spawnedBlocks[i].localScale = blockBaseScales[i];
            spawnedGroups[i].alpha = IsEdgeSlot(blockSlots[i]) ? 0f : 1f;
        }
    }

    /// <summary>
    /// Gives the chosen carousel block a quick scale pop when the song is selected.
    /// </summary>
    /// <param name="index">Index of the selected block.</param>
    /// <returns>Coroutine enumerator for the pop animation.</returns>
    private IEnumerator CarouselConfirmPopRoutine(int index)
    {
        if (index < 0 || index >= spawnedBlocks.Count) yield break;

        RectTransform block = spawnedBlocks[index];
        Vector3 rest = blockBaseScales[index];
        const float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            block.localScale = rest * (1f + 0.12f * Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f));
            yield return null;
        }
        block.localScale = rest;
        blockPunch[index] = 0f;
    }

    /// <summary>
    /// Pops difficulty boxes and the jacket in with a staggered overshoot when the difficulty screen opens.
    /// </summary>
    /// <returns>Coroutine enumerator for the intro animation.</returns>
    private IEnumerator DifficultyIntroRoutine()
    {
        const float duration = 0.45f;
        const float stagger = 0.08f;
        const float startDelay = 0.1f;
        float total = startDelay + duration + stagger * Mathf.Max(0, difficultyBoxes.Length - 1);
        Vector3 jacketScale = diffJacketDisplay != null ? GetBaseScale(diffJacketDisplay.rectTransform) : Vector3.one;

        for (int i = 0; i < difficultyBoxes.Length; i++)
        {
            bool isCurrent = i == currentDiffIndex && !lockedDifficulties[i];
            diffBaseScales[i] = isCurrent ? selectedScale : normalScale;
            diffPunch[i] = 0f;
            difficultyBoxes[i].localRotation = diffBaseRotations[i];

            Image boxImg = difficultyBoxes[i].GetComponent<Image>();
            if (boxImg != null && !lockedDifficulties[i]) boxImg.color = Color.white;
            if (difficultyOutlines.Length > i && difficultyOutlines[i] != null)
                difficultyOutlines[i].color = isCurrent ? outlineGold : Color.black;
        }

        float elapsed = 0f;
        while (elapsed < total)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < difficultyBoxes.Length; i++)
            {
                float local = Mathf.Clamp01((elapsed - startDelay - i * stagger) / duration);
                difficultyBoxes[i].localScale = Vector3.one * diffBaseScales[i] * Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(local));
            }
            if (diffJacketDisplay != null)
            {
                float jacket = Mathf.Clamp01(elapsed / duration);
                diffJacketDisplay.rectTransform.localScale = jacketScale * Mathf.LerpUnclamped(0.85f, 1f, EaseOutBack(jacket));
            }
            yield return null;
        }

        for (int i = 0; i < difficultyBoxes.Length; i++)
            difficultyBoxes[i].localScale = Vector3.one * diffBaseScales[i];
        if (diffJacketDisplay != null) diffJacketDisplay.rectTransform.localScale = jacketScale;
    }

    /// <summary>
    /// Wiggles a locked difficulty box or playlist name to show it cannot be selected.
    /// </summary>
    /// <param name="box">Transform to wiggle.</param>
    /// <param name="rest">Rotation to return to.</param>
    /// <returns>Coroutine enumerator for the shake animation.</returns>
    private IEnumerator ShakeRoutine(RectTransform box, Quaternion rest)
    {
        const float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float angle = Mathf.Sin(t * Mathf.PI * 6f) * 8f * (1f - t);
            box.localRotation = rest * Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
        box.localRotation = rest;
    }

    /// <summary>
    /// Slides, scales, and fades a graphic into its rest state.
    /// </summary>
    /// <param name="graphic">Graphic to animate.</param>
    /// <param name="fromOffset">Anchored position offset the graphic starts at.</param>
    /// <param name="fromScale">Scale multiplier the graphic starts at.</param>
    /// <param name="duration">Animation length in seconds.</param>
    /// <returns>Coroutine enumerator for the pop-in animation.</returns>
    private IEnumerator PopInRoutine(Graphic graphic, Vector2 fromOffset, float fromScale, float duration)
    {
        RectTransform rt = graphic.rectTransform;
        Vector3 restScale = GetBaseScale(rt);
        Vector2 restPos = GetBasePosition(rt);
        float restAlpha = GetBaseAlpha(graphic);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(t);
            rt.anchoredPosition = restPos + fromOffset * (1f - eased);
            rt.localScale = restScale * Mathf.Lerp(fromScale, 1f, eased);
            SetAlpha(graphic, restAlpha * EaseOutCubic(Mathf.Clamp01(t * 1.6f)));
            yield return null;
        }

        rt.anchoredPosition = restPos;
        rt.localScale = restScale;
        SetAlpha(graphic, restAlpha);
    }

    /// <summary>
    /// Scales a transform between two values with an ease-out curve.
    /// </summary>
    /// <returns>Coroutine enumerator for the scale animation.</returns>
    private IEnumerator ScaleRoutine(Transform target, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.LerpUnclamped(from, to, EaseOutCubic(Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        target.localScale = to;
    }

    /// <summary>
    /// Crossfades the background to a new sprite with a gentle zoom settle.
    /// </summary>
    /// <param name="previous">Sprite currently shown, kept on the ghost layer while the new one fades in.</param>
    /// <param name="next">Sprite to show.</param>
    /// <returns>Coroutine enumerator for the crossfade.</returns>
    private IEnumerator BackgroundCrossfadeRoutine(Sprite previous, Sprite next)
    {
        Image bg = mainBackgroundDisplay;
        Vector3 restScale = GetBaseScale(bg.rectTransform);
        float restAlpha = GetBaseAlpha(bg);

        if (backgroundGhost != null && previous != null)
        {
            CopyRect(bg.rectTransform, backgroundGhost.rectTransform);
            backgroundGhost.rectTransform.localScale = restScale;
            backgroundGhost.sprite = previous;
            backgroundGhost.preserveAspect = bg.preserveAspect;
            backgroundGhost.type = bg.type;
            // Start the ghost at the background's current alpha, so a hidden background (video song) stays hidden.
            backgroundGhost.color = bg.color;
            backgroundGhost.enabled = true;
        }

        bg.sprite = next;
        float duration = MainVisualDuration * 1.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float eased = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
            SetAlpha(bg, restAlpha * eased);
            bg.rectTransform.localScale = restScale * Mathf.Lerp(BackgroundZoom, 1f, eased);
            yield return null;
        }

        SetAlpha(bg, restAlpha);
        bg.rectTransform.localScale = restScale;
        if (backgroundGhost != null) backgroundGhost.enabled = false;
    }

    /// <summary>
    /// Fades the current preview track out, swaps the clip, and fades the new one in.
    /// </summary>
    /// <param name="clip">Preview clip to play.</param>
    /// <returns>Coroutine enumerator for the music crossfade.</returns>
    private IEnumerator SwitchPreviewRoutine(AudioClip clip)
    {
        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < MusicFadeOutTime)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / MusicFadeOutTime);
                yield return null;
            }
        }

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.Play();

        float fadeIn = 0f;
        while (fadeIn < MusicFadeInTime)
        {
            fadeIn += Time.deltaTime;
            musicSource.volume = musicMaxVolume * EaseOutCubic(Mathf.Clamp01(fadeIn / MusicFadeInTime));
            yield return null;
        }
        musicSource.volume = musicMaxVolume;
        musicRoutine = null;
    }

    /// <summary>
    /// Fades the current preview track out and stops it, leaving silence until the next preview starts.
    /// </summary>
    /// <returns>Coroutine enumerator for the music fade-out.</returns>
    private IEnumerator FadeOutMusicRoutine()
    {
        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < MusicFadeOutTime)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / MusicFadeOutTime);
                yield return null;
            }
        }
        musicSource.Stop();
        musicRoutine = null;
    }

    /// <summary>
    /// Loads a song's demo video on the hidden video layer, then fades it in over the current one and
    /// starts the preview music at the same moment. Songs without a video fade back to the background image.
    /// </summary>
    /// <param name="data">Song whose demo video should be shown.</param>
    /// <returns>Coroutine enumerator for the video swap.</returns>
    private IEnumerator PreviewVideoRoutine(SongGradeData data)
    {
        VideoClip clip = data.previewVideo;
        float duration = MainVisualDuration * 1.5f;

        if (clip == null)
        {
            // Cancel anything still loading, then fade the visible video away to reveal the background image.
            for (int i = 0; i < videoPlayers.Length; i++)
                if (i != activeVideoLayer) StopVideoLayer(i);

            if (activeVideoLayer < 0) yield break;

            int fading = activeVideoLayer;
            activeVideoLayer = -1;
            RawImage fadingLayer = videoLayers[fading];
            float startAlpha = fadingLayer.color.a;
            float fadeElapsed = 0f;
            while (fadeElapsed < duration)
            {
                fadeElapsed += Time.deltaTime;
                SetAlpha(fadingLayer, startAlpha * (1f - EaseOutCubic(Mathf.Clamp01(fadeElapsed / duration))));
                yield return null;
            }
            StopVideoLayer(fading);
            yield break;
        }

        // The previous preview goes quiet while the demo loads; the new one starts together with the video.
        if (musicSource != null)
        {
            if (musicRoutine != null) StopCoroutine(musicRoutine);
            musicRoutine = StartCoroutine(FadeOutMusicRoutine());
        }

        int next = activeVideoLayer == 0 ? 1 : 0;
        int other = 1 - next;
        VideoPlayer player = videoPlayers[next];
        RawImage layer = videoLayers[next];

        StopVideoLayer(next);
        PrepareVideoTexture(next, clip);
        player.clip = clip;
        player.Prepare();

        while (!player.isPrepared) yield return null;
        while (isPaused || (pauseMenu != null && pauseMenu.IsPaused())) yield return null;

        // Draw the incoming video above the outgoing one.
        if (layer.transform.GetSiblingIndex() < videoLayers[other].transform.GetSiblingIndex())
            layer.transform.SetSiblingIndex(videoLayers[other].transform.GetSiblingIndex());

        player.Play();

        // Wait for the first decoded frame so the fade never reveals an empty texture.
        float timeout = 1f;
        while (player.frame < 0 && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        yield return null;

        if (musicSource && data.songPreviewClip)
        {
            if (musicRoutine != null) StopCoroutine(musicRoutine);
            musicRoutine = StartCoroutine(SwitchPreviewRoutine(data.songPreviewClip));
        }

        int previous = activeVideoLayer;
        activeVideoLayer = next;

        // The background image isn't used with a video: fade it out as the video fades in.
        RunAnimation(mainBackgroundDisplay, BackgroundHideRoutine(duration));

        Vector3 restScale = GetBaseScale(mainBackgroundDisplay.rectTransform);
        float restAlpha = GetBaseAlpha(mainBackgroundDisplay);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float eased = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
            SetAlpha(layer, restAlpha * eased);
            layer.rectTransform.localScale = restScale * Mathf.Lerp(BackgroundZoom, 1f, eased);
            yield return null;
        }

        SetAlpha(layer, restAlpha);
        layer.rectTransform.localScale = restScale;
        if (previous >= 0 && previous != next) StopVideoLayer(previous);
        videoRoutine = null;
    }

    /// <summary>
    /// Fades the background image (and its crossfade layer) fully out, used while a demo video is showing.
    /// </summary>
    /// <param name="duration">Fade length in seconds.</param>
    /// <returns>Coroutine enumerator for the fade-out.</returns>
    private IEnumerator BackgroundHideRoutine(float duration)
    {
        Image bg = mainBackgroundDisplay;
        Vector3 restScale = GetBaseScale(bg.rectTransform);
        Vector3 startScale = bg.rectTransform.localScale;
        float bgStart = bg.color.a;
        float ghostStart = backgroundGhost != null && backgroundGhost.enabled ? backgroundGhost.color.a : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remaining = 1f - EaseOutCubic(Mathf.Clamp01(elapsed / duration));
            SetAlpha(bg, bgStart * remaining);
            bg.rectTransform.localScale = Vector3.Lerp(restScale, startScale, remaining);
            if (backgroundGhost != null && backgroundGhost.enabled) SetAlpha(backgroundGhost, ghostStart * remaining);
            yield return null;
        }

        SetAlpha(bg, 0f);
        bg.rectTransform.localScale = restScale;
        if (backgroundGhost != null) backgroundGhost.enabled = false;
    }

    /// <summary>
    /// Stops a video layer's playback and hides it.
    /// </summary>
    /// <param name="index">Video layer index.</param>
    private void StopVideoLayer(int index)
    {
        videoPlayers[index].Stop();
        videoPausedByMenu[index] = false;
        SetAlpha(videoLayers[index], 0f);
    }

    /// <summary>
    /// Makes sure a video layer has a render texture matching the clip, and crops it to cover the background area.
    /// </summary>
    /// <param name="index">Video layer index.</param>
    /// <param name="clip">Clip about to play on that layer.</param>
    private void PrepareVideoTexture(int index, VideoClip clip)
    {
        int width = clip.width > 0 ? (int)clip.width : 1280;
        int height = clip.height > 0 ? (int)clip.height : 720;

        RenderTexture texture = videoTextures[index];
        if (texture == null || texture.width != width || texture.height != height)
        {
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }
            texture = new RenderTexture(width, height, 0);
            // Mipmaps let the arrows read the video's average colour from the 1x1 level.
            texture.useMipMap = true;
            texture.autoGenerateMips = true;
            texture.Create();
            videoTextures[index] = texture;
        }

        videoPlayers[index].targetTexture = texture;
        videoLayers[index].texture = texture;

        // Match the background's rect, then crop the video (rather than stretch it) to fill it.
        RectTransform rt = videoLayers[index].rectTransform;
        CopyRect(mainBackgroundDisplay.rectTransform, rt);
        Rect area = rt.rect;
        if (area.height <= 0f) return;

        float areaAspect = area.width / area.height;
        float videoAspect = width / (float)height;
        if (videoAspect > areaAspect)
        {
            float u = areaAspect / videoAspect;
            videoLayers[index].uvRect = new Rect((1f - u) * 0.5f, 0f, u, 1f);
        }
        else
        {
            float v = videoAspect / areaAspect;
            videoLayers[index].uvRect = new Rect(0f, (1f - v) * 0.5f, 1f, v);
        }

        // Keep the LED grid square and aligned to the visible (cropped) part of the video.
        Material led = videoMaterials.Length > index ? videoMaterials[index] : null;
        if (led != null)
        {
            Rect uv = videoLayers[index].uvRect;
            led.SetVector("_UVRect", new Vector4(uv.x, uv.y, uv.width, uv.height));
            led.SetFloat("_Aspect", areaAspect);
            videoLayers[index].SetMaterialDirty();
        }
    }

    /// <summary>
    /// Creates two video layers just above the background image so demo videos can crossfade between songs.
    /// </summary>
    private void CreateVideoLayers()
    {
        if (mainBackgroundDisplay == null) return;

        videoPlayers = new VideoPlayer[2];
        videoLayers = new RawImage[2];
        videoTextures = new RenderTexture[2];
        videoPausedByMenu = new bool[2];
        videoMaterials = new Material[2];

        Shader ledShader = Resources.Load<Shader>("Shaders/LedScreen");
        if (ledShader == null) Debug.LogWarning("LED screen shader not found at Resources/Shaders/LedScreen; demo videos will play without the LED effect.");

        RectTransform bgRect = mainBackgroundDisplay.rectTransform;
        Color tint = mainBackgroundDisplay.color;
        tint.a = 0f;

        for (int i = 0; i < 2; i++)
        {
            GameObject layerObject = new GameObject("PreviewVideo" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(LayoutElement), typeof(VideoPlayer));
            layerObject.layer = mainBackgroundDisplay.gameObject.layer;
            layerObject.GetComponent<LayoutElement>().ignoreLayout = true;

            RectTransform rt = layerObject.GetComponent<RectTransform>();
            rt.SetParent(bgRect.parent, false);
            rt.SetSiblingIndex(bgRect.GetSiblingIndex() + 1);
            CopyRect(bgRect, rt);
            rt.localScale = GetBaseScale(bgRect);

            RawImage raw = layerObject.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.color = tint; // keeps any dimming tint used on the background image
            if (ledShader != null)
            {
                Material led = new Material(ledShader);
                led.SetFloat("_DotCount", LedDotsAcross);
                led.SetFloat("_DotSize", LedDotSize);
                led.SetFloat("_Softness", LedDotSoftness);
                led.SetFloat("_Brightness", LedBrightness);
                raw.material = led;
                videoMaterials[i] = led;
            }
            videoLayers[i] = raw;

            VideoPlayer player = layerObject.GetComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = true;
            player.source = VideoSource.VideoClip;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.skipOnDrop = true;
            player.waitForFirstFrame = true;
            videoPlayers[i] = player;
        }
    }

    /// <summary>
    /// Plays the arrows video on the arrow display with the LED dot effect. The dots match the
    /// background video's dot size, and dark pixels become transparent so only the arrows light up.
    /// </summary>
    private void SetupArrows()
    {
        if (arrowDisplay == null || arrowVideo == null) return;

        RenderTexture texture = arrowVideoTexture;
        if (texture == null)
        {
            int width = arrowVideo.width > 0 ? (int)arrowVideo.width : 1280;
            int height = arrowVideo.height > 0 ? (int)arrowVideo.height : 720;
            arrowRuntimeTexture = new RenderTexture(width, height, 0);
            arrowRuntimeTexture.Create();
            texture = arrowRuntimeTexture;
        }

        // Clear to black so nothing shows (black is transparent) before the first frame arrives.
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = texture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = previousActive;

        Shader ledShader = Resources.Load<Shader>("Shaders/LedScreen");
        if (ledShader == null)
        {
            Debug.LogWarning("LED screen shader not found at Resources/Shaders/LedScreen; the arrows video can't be shown on a UI Image.");
            return;
        }

        // A UI Image can only hold a sprite, so the video is fed to the shader through its own
        // texture slot instead. With no sprite, the Image's UVs simply span its rect (0-1).
        arrowDisplay.sprite = null;
        arrowDisplay.type = Image.Type.Simple;
        arrowDisplay.raycastTarget = false;

        Rect area = arrowDisplay.rectTransform.rect;
        float backgroundWidth = mainBackgroundDisplay != null ? mainBackgroundDisplay.rectTransform.rect.width : 0f;
        float dotsAcross = backgroundWidth > 0f ? LedDotsAcross * area.width / backgroundWidth : LedDotsAcross;

        arrowMaterial = new Material(ledShader);
        arrowMaterial.SetFloat("_DotCount", Mathf.Max(1f, dotsAcross));
        arrowMaterial.SetFloat("_DotSize", LedDotSize);
        arrowMaterial.SetFloat("_Softness", LedDotSoftness);
        arrowMaterial.SetFloat("_Brightness", LedBrightness);
        arrowMaterial.SetFloat("_Aspect", area.height > 0f ? area.width / area.height : 1f);
        arrowMaterial.SetVector("_UVRect", new Vector4(0f, 0f, 1f, 1f));
        arrowMaterial.SetFloat("_BlackToAlpha", 1f);
        arrowMaterial.SetTexture("_OverrideTex", texture);
        arrowMaterial.SetFloat("_UseOverrideTex", 1f);
        arrowDisplay.material = arrowMaterial;

        GameObject playerObject = new GameObject("ArrowVideo", typeof(VideoPlayer));
        playerObject.transform.SetParent(transform, false);
        arrowVideoPlayer = playerObject.GetComponent<VideoPlayer>();
        arrowVideoPlayer.playOnAwake = false;
        arrowVideoPlayer.isLooping = true;
        arrowVideoPlayer.source = VideoSource.VideoClip;
        arrowVideoPlayer.clip = arrowVideo;
        arrowVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        arrowVideoPlayer.targetTexture = texture;
        arrowVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        arrowVideoPlayer.skipOnDrop = true;
        arrowVideoPlayer.Play();
    }

    /// <summary>
    /// Tints the arrows with the average colour of the visible background video (white when none is showing).
    /// The colour is read back from the video's 1x1 mip level without stalling the GPU.
    /// </summary>
    private void UpdateArrowColor()
    {
        if (arrowMaterial == null) return;

        RenderTexture source = activeVideoLayer >= 0 && activeVideoLayer < videoTextures.Length ? videoTextures[activeVideoLayer] : null;
        if (source == null)
        {
            arrowTargetColor = Color.white;
        }
        else if (!arrowColorReadbackPending && Time.unscaledTime >= nextArrowColorSample && SystemInfo.supportsAsyncGPUReadback)
        {
            nextArrowColorSample = Time.unscaledTime + ArrowColorSampleInterval;
            // Read in the texture's own format (format conversion isn't supported for every
            // render texture format, e.g. B8G8R8A8_SRGB on Metal) and decode the bytes here.
            UnityEngine.Experimental.Rendering.GraphicsFormat format = source.graphicsFormat;
            bool isBgra = format == UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_SRGB
                       || format == UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_UNorm;
            bool isRgba = format == UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB
                       || format == UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
            if (isBgra || isRgba)
            {
                arrowColorReadbackPending = true;
                AsyncGPUReadback.Request(source, source.mipmapCount - 1, request =>
                {
                    arrowColorReadbackPending = false;
                    if (request.hasError || arrowMaterial == null || activeVideoLayer < 0) return;
                    var data = request.GetData<byte>();
                    if (data.Length < 4) return;
                    byte r = isBgra ? data[2] : data[0];
                    byte b = isBgra ? data[0] : data[2];
                    arrowTargetColor = new Color(r / 255f, data[1] / 255f, b / 255f, 1f);
                });
            }
        }

        float t = 1f - Mathf.Exp(-ArrowColorFollowSpeed * Time.unscaledDeltaTime);
        arrowCurrentColor = Color.Lerp(arrowCurrentColor, arrowTargetColor, t);
        arrowMaterial.SetColor("_Color", arrowCurrentColor);
    }

    /// <summary>
    /// Releases the runtime video render textures.
    /// </summary>
    void OnDestroy()
    {
        if (arrowRuntimeTexture != null)
        {
            arrowRuntimeTexture.Release();
            Destroy(arrowRuntimeTexture);
        }

        if (arrowMaterial != null) Destroy(arrowMaterial);

        foreach (RenderTexture texture in videoTextures)
        {
            if (texture == null) continue;
            texture.Release();
            Destroy(texture);
        }

        foreach (Material material in videoMaterials)
        {
            if (material != null) Destroy(material);
        }
    }

    /// <summary>
    /// Hides the "press again" prompt and restores its rest scale.
    /// </summary>
    private void HidePressAgain()
    {
        if (!pressAgainText) return;
        if (runningAnimations.TryGetValue(pressAgainText, out Coroutine running) && running != null)
            StopCoroutine(running);
        pressAgainText.transform.localScale = GetBaseScale(pressAgainText.transform);
        pressAgainText.SetActive(false);
    }

    /// <summary>
    /// Creates an image layer behind the background so song backgrounds can crossfade.
    /// </summary>
    private void CreateBackgroundGhost()
    {
        if (mainBackgroundDisplay == null) return;

        GameObject ghostObject = new GameObject("BackgroundCrossfade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        ghostObject.layer = mainBackgroundDisplay.gameObject.layer;
        ghostObject.GetComponent<LayoutElement>().ignoreLayout = true;

        RectTransform ghostRect = ghostObject.GetComponent<RectTransform>();
        ghostRect.SetParent(mainBackgroundDisplay.rectTransform.parent, false);
        ghostRect.SetSiblingIndex(mainBackgroundDisplay.rectTransform.GetSiblingIndex());
        CopyRect(mainBackgroundDisplay.rectTransform, ghostRect);

        backgroundGhost = ghostObject.GetComponent<Image>();
        backgroundGhost.raycastTarget = false;
        backgroundGhost.material = mainBackgroundDisplay.material;
        backgroundGhost.enabled = false;
    }

    /// <summary>
    /// Copies layout values from one rect transform to another.
    /// </summary>
    private static void CopyRect(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localRotation = source.localRotation;
    }

    /// <summary>
    /// Starts an animation on a target, stopping any animation already running on it.
    /// </summary>
    /// <param name="key">Object the animation drives.</param>
    /// <param name="routine">Animation to start.</param>
    /// <returns>The started coroutine.</returns>
    private Coroutine RunAnimation(Object key, IEnumerator routine)
    {
        if (runningAnimations.TryGetValue(key, out Coroutine running) && running != null)
            StopCoroutine(running);
        Coroutine started = StartCoroutine(routine);
        runningAnimations[key] = started;
        return started;
    }

    /// <summary>Returns the rest scale of a transform, capturing it the first time.</summary>
    private Vector3 GetBaseScale(Transform target)
    {
        if (!baseScales.TryGetValue(target, out Vector3 scale))
        {
            scale = target.localScale;
            baseScales[target] = scale;
        }
        return scale;
    }

    /// <summary>Returns the rest rotation of a transform, capturing it the first time.</summary>
    private Quaternion GetBaseRotation(Transform target)
    {
        if (!baseRotations.TryGetValue(target, out Quaternion rotation))
        {
            rotation = target.localRotation;
            baseRotations[target] = rotation;
        }
        return rotation;
    }

    /// <summary>Returns the rest anchored position of a rect, capturing it the first time.</summary>
    private Vector2 GetBasePosition(RectTransform target)
    {
        if (!basePositions.TryGetValue(target, out Vector2 position))
        {
            position = target.anchoredPosition;
            basePositions[target] = position;
        }
        return position;
    }

    /// <summary>Returns the rest alpha of a graphic, capturing it the first time.</summary>
    private float GetBaseAlpha(Graphic target)
    {
        if (!baseAlphas.TryGetValue(target, out float alpha))
        {
            alpha = target.color.a;
            baseAlphas[target] = alpha;
        }
        return alpha;
    }

    /// <summary>Sets only the alpha channel of a graphic's color.</summary>
    private static void SetAlpha(Graphic target, float alpha)
    {
        Color color = target.color;
        color.a = alpha;
        target.color = color;
    }

    /// <summary>Frame-rate independent smoothing factor for Lerp-based following.</summary>
    private static float Damp(float speed) => 1f - Mathf.Exp(-speed * Time.deltaTime);

    /// <summary>Fast start, soft landing.</summary>
    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    /// <summary>Soft landing with a slight overshoot.</summary>
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
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
