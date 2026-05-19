using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class MenuSelector : MonoBehaviour
{
    [Header("Pause Settings")]
public bool canUsePauseAction = true;
    [Header("Buttons")]
    public RectTransform[] buttons;
    public Image[] outlines;
    public Image[] backgrounds;

    [Header("Selection Text")]
    public TMP_Text selectionText;

    [TextArea]
    public string[] selectionTexts;

    [Header("Visual Settings")]
    public float selectedScale = 1.2f;
    public float normalScale = 1f;

    public float animSpeed = 8f;
    public float bgSpeed = 6f;
    public float fadeSpeed = 2f;

    public Color outlineGold = new Color(1f, 0.85f, 0f);
    public Color outlineBlack = Color.black;

    public Color bgWhite = Color.white;
    public Color bgGrey = new Color(0.3f, 0.3f, 0.3f);

    [Header("Input Cooldown")]
    public float moveCooldown = 0.15f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip switchSound;

    [Header("Selection Event")]
    public UnityEvent<int> onSelectIndex;

    [Header("Selection Effects")]
    public GameObject[] selectionParticles;
    public GameObject[] selectionSprites;

    [Header("Animators")]
    public GameObject laserObj;
    public GameObject spotlightObj;

    [Header("Pause Menu Scene Loading")]
    [Tooltip("If player uses Pause action map, load this scene")]
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
    private int index = 0;

    private float lastMoveTime;
    private float pulseTime;

    private bool isFading = true;
    private bool hasSelected = false;

    private CanvasGroup canvasGroup;

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

    private void Start()
    {
        if (laserObj != null)
            laser = laserObj.GetComponent<Animator>();

        if (spotlightObj != null)
            spotlights = spotlightObj.GetComponent<Animator>();
    }

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

    void OnDisable()
    {
        left.Disable();
        right.Disable();

        select.Disable();
        startBtn.Disable();

        pauseAction.Disable();

        StopAllCoroutines();
    }

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