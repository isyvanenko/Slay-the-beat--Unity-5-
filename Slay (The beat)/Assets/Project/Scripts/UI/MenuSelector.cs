using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;
using TMPro;

/// <summary>
/// A generic menu selector that supports horizontal navigation, visual feedback, and selection events.
/// </summary>
/// <remarks>
/// This component powers a horizontal button menu (like an arcade cabinet or main menu) with:
/// - Smooth scaling, background color, and outline pulsing for the selected item
/// - Input cooldown and navigation sounds
/// - Optional particle effects and sprites tied to each menu item
/// - Pause action support to jump to a different scene
/// - Fade-in animation when the menu becomes active
/// - Selection events that can be wired to scene loading or other logic
/// 
/// The menu is designed for UI canvases with CanvasGroup, enabling fade-in and blocking interaction until fully visible.
/// </remarks>
[RequireComponent(typeof(CanvasGroup))]
public class MenuSelector : MonoBehaviour
{
    [Header("Pause Settings")]
    /// <summary>If true, the pause action (e.g., Escape or Start button) can be used to load a pause scene.</summary>
    public bool canUsePauseAction = true;

    [Header("Buttons")]
    /// <summary>Array of RectTransform for each button in the menu.</summary>
    public RectTransform[] buttons;
    
    /// <summary>Array of outline images for each button.</summary>
    public Image[] outlines;
    
    /// <summary>Array of background images for each button.</summary>
    public Image[] backgrounds;

    [Header("Selection Text")]
    /// <summary>Text component that displays the description of the currently selected item.</summary>
    public TMP_Text selectionText;

    /// <summary>Array of text strings corresponding to each button index.</summary>
    [TextArea]
    public string[] selectionTexts;

    [Header("Visual Settings")]
    /// <summary>Scale of the selected button.</summary>
    public float selectedScale = 1.2f;
    
    /// <summary>Scale of unselected buttons.</summary>
    public float normalScale = 1f;

    /// <summary>Animation speed for scaling and color transitions.</summary>
    public float animSpeed = 8f;
    
    /// <summary>Speed for background color transitions.</summary>
    public float bgSpeed = 6f;
    
    /// <summary>Speed for the fade-in effect of the menu.</summary>
    public float fadeSpeed = 2f;

    /// <summary>Gold color used for the outline of the selected button (pulsing).</summary>
    public Color outlineGold = new Color(1f, 0.85f, 0f);
    
    /// <summary>Black color for outlines of unselected buttons.</summary>
    public Color outlineBlack = Color.black;

    /// <summary>White color for the background of the selected button.</summary>
    public Color bgWhite = Color.white;
    
    /// <summary>Grey color for backgrounds of unselected buttons.</summary>
    public Color bgGrey = new Color(0.3f, 0.3f, 0.3f);

    [Header("Input Cooldown")]
    /// <summary>Minimum time between navigation moves (prevents rapid scrolling).</summary>
    public float moveCooldown = 0.15f;

    [Header("Audio")]
    /// <summary>AudioSource used to play navigation sounds.</summary>
    public AudioSource audioSource;
    
    /// <summary>Sound played when switching selection.</summary>
    public AudioClip switchSound;

    [Header("Selection Event")]
    /// <summary>UnityEvent invoked when a menu item is selected, passing the index of the selected button.</summary>
    public UnityEvent<int> onSelectIndex;

    [Header("Selection Effects")]
    /// <summary>Particle systems (or GameObjects) to activate when a button is selected.</summary>
    public GameObject[] selectionParticles;
    
    /// <summary>Sprites/GameObjects to show when a button is selected.</summary>
    public GameObject[] selectionSprites;

    [Header("Animators")]
    /// <summary>GameObject with an Animator for a laser effect.</summary>
    public GameObject laserObj;
    
    /// <summary>GameObject with an Animator for a spotlight effect.</summary>
    public GameObject spotlightObj;

    [Header("Pause Menu Scene Loading")]
    [Tooltip("If player uses Pause action map, load this scene")]
    /// <summary>Scene name to load when the pause action is triggered.</summary>
    public string pauseSceneName;

    private Animator laser;
    private Animator spotlights;

    // INPUT
    private InputActions input;

    private InputAction left;
    private InputAction right;

    private InputAction select;
    private InputAction startBtn;

    // NEW PAUSE ACTION
    private InputAction pauseAction;

    // STATE
    /// <summary>Currently selected button index.</summary>
    private int index = 0;

    private float lastMoveTime;
    private float pulseTime;

    private bool isFading = true;
    private bool hasSelected = false;

    private CanvasGroup canvasGroup;

    /// <summary>
    /// Initializes input actions and caches the CanvasGroup.
    /// </summary>
    void Awake()
    {
        input = new InputActions();

        left = input.UI.NavigateLeft;
        right = input.UI.NavigateRight;

        select = input.UI.Select;
        startBtn = input.UI.Start;

        // ACTION MAP: Pause
        pauseAction = input.UI.Pause;

        canvasGroup = GetComponent<CanvasGroup>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Caches animator components for laser and spotlight effects.
    /// </summary>
    private void Start()
    {
        if (laserObj != null)
            laser = laserObj.GetComponent<Animator>();

        if (spotlightObj != null)
            spotlights = spotlightObj.GetComponent<Animator>();
    }

    /// <summary>
    /// Enables input actions, resets visuals, and starts the fade-in coroutine when the object is enabled.
    /// </summary>
    void OnEnable()
    {
        left.performed += ctx => Move(-1);
        right.performed += ctx => Move(+1);

        select.performed += ctx => ActivateIndex(index);
        startBtn.performed += ctx => ActivateIndex(index);

        // PAUSE INPUT
        pauseAction.performed += HandlePausePressed;

        left.Enable();
        right.Enable();

        select.Enable();
        startBtn.Enable();

        pauseAction.Enable();

        SetInitialVisuals();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        isFading = true;
        hasSelected = false;

        StartCoroutine(FadeInCanvas());
    }

    /// <summary>
    /// Disables input actions and stops coroutines when the object is disabled.
    /// </summary>
    void OnDisable()
    {
        left.Disable();
        right.Disable();

        select.Disable();
        startBtn.Disable();

        pauseAction.Disable();

        StopAllCoroutines();
    }

    /// <summary>
    /// Coroutine that fades in the menu canvas.
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

        UpdateSelectionText();
    }

    /// <summary>
    /// Updates button visuals (scale, background color, outline pulse) each frame.
    /// </summary>
    void Update()
    {
        if (isFading || hasSelected)
            return;

        pulseTime += Time.unscaledDeltaTime;

        for (int i = 0; i < buttons.Length; i++)
        {
            bool selected = (i == index);

            // SCALE
            float targetScale = selected
                ? selectedScale
                : normalScale;

            buttons[i].localScale = Vector3.Lerp(
                buttons[i].localScale,
                Vector3.one * targetScale,
                Time.unscaledDeltaTime * animSpeed
            );

            // BACKGROUND
            if (i < backgrounds.Length && backgrounds[i] != null)
            {
                Color target = selected
                    ? bgWhite
                    : bgGrey;

                backgrounds[i].color = Color.Lerp(
                    backgrounds[i].color,
                    target,
                    Time.unscaledDeltaTime * bgSpeed
                );
            }

            // OUTLINES
            if (i < outlines.Length && outlines[i] != null)
            {
                if (selected)
                {
                    float pulse =
                        (Mathf.Sin(pulseTime * 3.5f) + 1f) * 0.5f;

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

            // PARTICLES
            if (i < selectionParticles.Length &&
                selectionParticles[i] != null)
            {
                selectionParticles[i].SetActive(selected);
            }

            // SPRITES
            if (i < selectionSprites.Length &&
                selectionSprites[i] != null)
            {
                selectionSprites[i].SetActive(selected);
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

        UpdateSelectionText();
    }

    /// <summary>
    /// Updates the selection description text based on the current index.
    /// </summary>
    private void UpdateSelectionText()
    {
        if (selectionText == null)
            return;

        if (selectionTexts != null &&
            index < selectionTexts.Length)
        {
            selectionText.text = selectionTexts[index];
        }
    }

    /// <summary>
    /// Resets all button visuals to their initial state.
    /// </summary>
    private void SetInitialVisuals()
    {
        pulseTime = 0f;

        hasSelected = false;

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].localScale =
                Vector3.one * normalScale;

            if (i < backgrounds.Length &&
                backgrounds[i] != null)
            {
                backgrounds[i].color = bgGrey;
            }

            if (i < outlines.Length &&
                outlines[i] != null)
            {
                outlines[i].color = outlineBlack;
            }

            if (i < selectionParticles.Length &&
                selectionParticles[i] != null)
            {
                selectionParticles[i].SetActive(false);
            }

            if (i < selectionSprites.Length &&
                selectionSprites[i] != null)
            {
                selectionSprites[i].SetActive(false);
            }
        }

        UpdateSelectionText();
    }

    /// <summary>
    /// Activates the menu item at the given index, triggering visual effects, the selection event, and preventing further input.
    /// </summary>
    /// <param name="idx">Index of the button to activate.</param>
    private void ActivateIndex(int idx)
    {
        if (isFading || hasSelected)
            return;

        hasSelected = true;

        if (laser != null)
            laser.SetBool("PlayerSelection", true);

        if (spotlights != null)
            spotlights.SetBool("SpotlightGone?", true);

        // DISABLE PARTICLES
        for (int i = 0; i < selectionParticles.Length; i++)
        {
            if (selectionParticles[i] != null)
            {
                selectionParticles[i].SetActive(false);
            }
        }

        // ENABLE ALL SPRITES
        for (int i = 0; i < selectionSprites.Length; i++)
        {
            if (selectionSprites[i] != null)
            {
                selectionSprites[i].SetActive(true);
            }
        }

        // FIRE EVENT
        onSelectIndex?.Invoke(idx);
    }

    // =========================
    // PAUSE ACTION
    // =========================
    /// <summary>
    /// Handles the pause input action, loading the pause scene if conditions are met.
    /// </summary>
    /// <param name="ctx">InputAction callback context.</param>
    /// <remarks>
    /// Conditions for pause action to work:
    /// - canUsePauseAction is true
    /// - The menu is active in the hierarchy
    /// - The menu is not fading and no selection has been made
    /// - pauseSceneName is not empty
    /// - TransitionManager.Instance exists
    /// </remarks>
    private void HandlePausePressed(InputAction.CallbackContext ctx)
    {
        // ❌ BLOCK if feature disabled
        if (!canUsePauseAction)
            return;

        // ❌ BLOCK if object is not active in hierarchy
        if (!gameObject.activeInHierarchy)
            return;

        // ❌ BLOCK if menu is transitioning or locked
        if (isFading || hasSelected)
            return;

        if (string.IsNullOrEmpty(pauseSceneName))
        {
            Debug.LogWarning("Pause Scene Name is empty!");
            return;
        }

        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.LoadScene(pauseSceneName);
        }
        else
        {
            Debug.LogError("TransitionManager.Instance is NULL!");
        }
    }
}