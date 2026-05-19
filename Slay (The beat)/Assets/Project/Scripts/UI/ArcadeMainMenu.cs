using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ArcadeMainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public RectTransform[] buttons;
    public Image[] outlines;
    public Image[] backgrounds;

    [Header("Selection Text")]
    public TMP_Text selectionText;
    public string[] selectionNames;

    [Header("Button Scale Settings")]

    [Tooltip("Extra scale added to buttons 0,1,3,4 when selected")]
    public float sideButtonZoomAmount = 0.08f;

    [Tooltip("Extra scale added to START button (index 2) when selected")]
    public float startButtonZoomAmount = 0.15f;

    [Header("Animation")]
    public float animSpeed = 8f;
    public float bgSpeed = 6f;
    public float fadeSpeed = 2f;

    [Header("Colors")]
    public Color outlineGold = new Color(1f, 0.85f, 0f);
    public Color outlineBlack = Color.black;

    public Color bgWhite = Color.white;
    public Color bgGrey = new Color(0.3f, 0.3f, 0.3f);

    [Header("Input")]
    public float moveCooldown = 0.15f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip switchSound;

    [Header("Special Objects For Start Button (Index 2)")]
    public GameObject[] startButtonObjects;

    [Header("Scene Names")]
    public string recordsScene;
    public string newsScene;
    public string gameplayScene;
    public string settingsScene;

    // Input
    private InputActions input;
    private InputAction left;
    private InputAction right;
    private InputAction select;
    private InputAction startBtn;

    // State
    private int index = 2;

    private float lastMoveTime;
    private float pulseTime;

    private bool isFading = true;
    private bool hasSelected = false;

    private CanvasGroup canvasGroup;

    // ORIGINAL SCALES
    private Vector3[] originalScales;

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

    void OnDisable()
    {
        left.Disable();
        right.Disable();

        select.Disable();
        startBtn.Disable();

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

        UpdateStartButtonObjects();
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

    private void UpdateSelectionText()
    {
        if (selectionText == null)
            return;

        if (selectionNames != null && index < selectionNames.Length)
        {
            selectionText.text = selectionNames[index];
        }
    }

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