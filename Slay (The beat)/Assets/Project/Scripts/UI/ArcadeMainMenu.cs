using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Main menu for arcade mode with animated button selection and scene navigation.
/// </summary>
/// <remarks>
/// This component controls a horizontal menu with 5 buttons: RECORDS, NEWS, START, SETTINGS, EXIT.
/// It supports keyboard/gamepad navigation (left/right), selection, and provides visual feedback
/// including scaling, color changes, and a pulsing outline on the selected button.
/// 
/// Key Features:
/// - Smooth button scaling animations with different zoom amounts for side buttons vs start button
/// - Background and outline color transitions based on selection
/// - Input cooldown to prevent rapid navigation
/// - Audio feedback on selection change
/// - Fade-in effect when the menu becomes active
/// - Special handling for the START button (extra scaling, separate object activation)
/// - Scene transitions using TransitionManager or direct SceneManager
/// - Editor and build quit handling for EXIT button
/// </remarks>
public class ArcadeMainMenu : MonoBehaviour
{
    [Header("Buttons")]
    /// <summary>Array of button RectTransforms (order: 0=RECORDS,1=NEWS,2=START,3=SETTINGS,4=EXIT).</summary>
    public RectTransform[] buttons;
    
    /// <summary>Array of outline Image components for each button.</summary>
    public Image[] outlines;
    
    /// <summary>Array of background Image components for each button.</summary>
    public Image[] backgrounds;

    [Header("Selection Text")]
    /// <summary>Text component that displays the name of the currently selected menu item.</summary>
    public TMP_Text selectionText;
    
    /// <summary>Array of display names corresponding to each button index.</summary>
    public string[] selectionNames;

    [Header("Button Scale Settings")]
    [Tooltip("Extra scale added to buttons 0,1,3,4 when selected")]
    /// <summary>Additional scale multiplier for side buttons (index 0,1,3,4) when selected.</summary>
    public float sideButtonZoomAmount = 0.08f;

    [Tooltip("Extra scale added to START button (index 2) when selected")]
    /// <summary>Additional scale multiplier for the START button (index 2) when selected.</summary>
    public float startButtonZoomAmount = 0.15f;

    [Header("Animation")]
    /// <summary>Speed of button scale animation (Lerp factor per second).</summary>
    public float animSpeed = 8f;
    
    /// <summary>Speed of background color transitions.</summary>
    public float bgSpeed = 6f;
    
    /// <summary>Speed of fade-in effect when menu appears.</summary>
    public float fadeSpeed = 2f;

    [Header("Colors")]
    /// <summary>Gold color for the selected button's outline (pulsing).</summary>
    public Color outlineGold = new Color(1f, 0.85f, 0f);
    
    /// <summary>Black color for deselected button outlines.</summary>
    public Color outlineBlack = Color.black;

    /// <summary>White color for the background of the selected button.</summary>
    public Color bgWhite = Color.white;
    
    /// <summary>Grey color for backgrounds of deselected buttons.</summary>
    public Color bgGrey = new Color(0.3f, 0.3f, 0.3f);

    [Header("Input")]
    /// <summary>Cooldown time in seconds between navigation moves to prevent rapid scrolling.</summary>
    public float moveCooldown = 0.15f;

    [Header("Audio")]
    /// <summary>AudioSource used to play navigation sound effects.</summary>
    public AudioSource audioSource;
    
    /// <summary>Sound clip played when switching selection to a different button.</summary>
    public AudioClip switchSound;

    [Header("Special Objects For Start Button (Index 2)")]
    /// <summary>GameObjects that are shown/hidden when the START button is selected.</summary>
    public GameObject[] startButtonObjects;

    [Header("Scene Names")]
    /// <summary>Scene name for the RECORDS menu.</summary>
    public string recordsScene;
    
    /// <summary>Scene name for the NEWS menu.</summary>
    public string newsScene;
    
    /// <summary>Scene name for the main gameplay (song selection or arcade).</summary>
    public string gameplayScene;
    
    /// <summary>Scene name for the SETTINGS menu.</summary>
    public string settingsScene;

    // Input
    private InputActions input;
    private InputAction left;
    private InputAction right;
    private InputAction select;
    private InputAction startBtn;

    // State
    /// <summary>Currently selected button index (0-4).</summary>
    private int index = 2;

    /// <summary>Timestamp of the last navigation input.</summary>
    private float lastMoveTime;
    
    /// <summary>Time accumulator for outline pulsing animation.</summary>
    private float pulseTime;

    /// <summary>Flag indicating if the fade-in animation is still running.</summary>
    private bool isFading = true;
    
    /// <summary>Flag indicating if a selection has been made (prevents double selection).</summary>
    private bool hasSelected = false;

    /// <summary>CanvasGroup for controlling menu fade-in.</summary>
    private CanvasGroup canvasGroup;

    /// <summary>Stores original local scales of buttons for animation reset.</summary>
    private Vector3[] originalScales;

    /// <summary>
    /// Initializes input actions, caches references, and stores original button scales.
    /// </summary>
    void Awake()
    {
        input = new InputActions();

        left = input.UI.NavigateLeft;
        right = input.UI.NavigateRight;
        select = input.UI.Select;
        startBtn = input.UI.Start;

        canvasGroup = GetComponent<CanvasGroup>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Store original scales from inspector
        originalScales = new Vector3[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                originalScales[i] = buttons[i].localScale;
        }
    }

    /// <summary>
    /// Enables input actions, sets up initial visuals, and starts fade-in coroutine.
    /// </summary>
    void OnEnable()
    {
        left.performed += ctx => Move(-1);
        right.performed += ctx => Move(+1);

        select.performed += ctx => SelectCurrentItem();
        startBtn.performed += ctx => SelectCurrentItem();

        left.Enable();
        right.Enable();

        select.Enable();
        startBtn.Enable();

        SetInitialVisuals();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        isFading = true;
        hasSelected = false;

        index = 2;

        StartCoroutine(FadeInCanvas());
    }

    /// <summary>
    /// Disables input actions and stops coroutines when the menu is disabled.
    /// </summary>
    void OnDisable()
    {
        left.Disable();
        right.Disable();

        select.Disable();
        startBtn.Disable();

        StopAllCoroutines();
    }

    /// <summary>
    /// Coroutine that smoothly fades in the menu canvas.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    IEnumerator FadeInCanvas()
    {
        float alpha = 0f;

        while (alpha < 1f)
        {
            alpha = Mathf.MoveTowards(
                alpha,
                1f,
                fadeSpeed * Time.unscaledDeltaTime
            );

            canvasGroup.alpha = alpha;

            yield return null;
        }

        canvasGroup.alpha = 1f;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        isFading = false;

        UpdateStartButtonObjects();
        UpdateSelectionText();
    }

    /// <summary>
    /// Updates button animations (scale, background color, outline pulse) every frame.
    /// </summary>
    void Update()
    {
        if (isFading || hasSelected)
            return;

        pulseTime += Time.unscaledDeltaTime;

        for (int i = 0; i < buttons.Length; i++)
        {
            bool selected = (i == index);

            Vector3 baseScale = originalScales[i];
            Vector3 targetScale = baseScale;

            // START BUTTON
            if (i == 2)
            {
                if (selected)
                {
                    targetScale = baseScale * (1f + startButtonZoomAmount);
                }
            }
            // SIDE BUTTONS
            else
            {
                if (selected)
                {
                    targetScale = baseScale * (1f + sideButtonZoomAmount);
                }
            }

            buttons[i].localScale = Vector3.Lerp(
                buttons[i].localScale,
                targetScale,
                Time.unscaledDeltaTime * animSpeed
            );

            // BACKGROUNDS
            if (i < backgrounds.Length && backgrounds[i] != null)
            {
                Color targetColor = selected ? bgWhite : bgGrey;

                backgrounds[i].color = Color.Lerp(
                    backgrounds[i].color,
                    targetColor,
                    Time.unscaledDeltaTime * bgSpeed
                );
            }

            // OUTLINES
            if (i < outlines.Length && outlines[i] != null)
            {
                if (selected)
                {
                    float pulse = (Mathf.Sin(pulseTime * 3.5f) + 1f) * 0.5f;

                    outlines[i].color = Color.Lerp(
                        outlineBlack,
                        outlineGold,
                        pulse
                    );
                }
                else
                {
                    outlines[i].color = outlineBlack;
                }
            }
        }
    }

    /// <summary>
    /// Moves the selection left or right.
    /// </summary>
    /// <param name="direction">-1 for left, +1 for right.</param>
    private void Move(int direction)
    {
        if (isFading || hasSelected)
            return;

        if (Time.unscaledTime - lastMoveTime < moveCooldown)
            return;

        lastMoveTime = Time.unscaledTime;

        if (audioSource != null && switchSound != null)
        {
            audioSource.PlayOneShot(switchSound);
        }

        index = (index + direction + buttons.Length) % buttons.Length;

        UpdateStartButtonObjects();
        UpdateSelectionText();
    }

    /// <summary>
    /// Executes the action associated with the currently selected button.
    /// </summary>
    /// <remarks>
    /// Button actions:
    /// - Index 0 (RECORDS): Loads recordsScene
    /// - Index 1 (NEWS): Loads newsScene
    /// - Index 2 (START): Loads gameplayScene
    /// - Index 3 (SETTINGS): Loads settingsScene
    /// - Index 4 (EXIT): Quits the application (or stops play in editor)
    /// </remarks>
    private void SelectCurrentItem()
    {
        if (isFading || hasSelected)
            return;

        hasSelected = true;

        switch (index)
        {
            // RECORDS
            case 0:
                Debug.Log("Opening Records");

                if (!string.IsNullOrEmpty(recordsScene))
                {
                    if (TransitionManager.Instance != null)
                        TransitionManager.Instance.LoadScene(recordsScene);
                }

                break;

            // NEWS
            case 1:
                Debug.Log("Opening News");

                if (!string.IsNullOrEmpty(newsScene))
                {
                    if (TransitionManager.Instance != null)
                        TransitionManager.Instance.LoadScene(newsScene);
                }

                break;

            // START
            case 2:
                Debug.Log("Starting Game");

                if (!string.IsNullOrEmpty(gameplayScene))
                {
                    if (TransitionManager.Instance != null)
                        TransitionManager.Instance.LoadScene(gameplayScene);
                }

                break;

            // SETTINGS
            case 3:
                Debug.Log("Opening Settings");

                if (!string.IsNullOrEmpty(settingsScene))
                {
                    if (TransitionManager.Instance != null)
                        TransitionManager.Instance.LoadScene(settingsScene);
                }

                break;

            // EXIT
            case 4:
                Debug.Log("Exit");

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;

            default:
                hasSelected = false;
                break;
        }
    }

    /// <summary>
    /// Shows or hides the start button special objects based on whether START is selected.
    /// </summary>
    private void UpdateStartButtonObjects()
    {
        bool startSelected = (index == 2);

        foreach (GameObject obj in startButtonObjects)
        {
            if (obj != null)
            {
                obj.SetActive(startSelected);
            }
        }
    }

    /// <summary>
    /// Updates the selection text to show the name of the current menu item.
    /// </summary>
    private void UpdateSelectionText()
    {
        if (selectionText == null)
            return;

        if (selectionNames != null && index < selectionNames.Length)
        {
            selectionText.text = selectionNames[index];
        }
    }

    /// <summary>
    /// Resets all visuals to their initial state (scales, colors, selection to START).
    /// </summary>
    private void SetInitialVisuals()
    {
        pulseTime = 0f;

        hasSelected = false;

        index = 2;

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].localScale = originalScales[i];

            if (i < backgrounds.Length && backgrounds[i] != null)
            {
                backgrounds[i].color = bgGrey;
            }

            if (i < outlines.Length && outlines[i] != null)
            {
                outlines[i].color = outlineBlack;
            }
        }

        UpdateStartButtonObjects();
        UpdateSelectionText();
    }
}