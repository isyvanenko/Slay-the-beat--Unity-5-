using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections; // Required for Coroutines

/// <summary>
/// This component is required to fade the entire menu.
/// Add this script to your main menu Canvas, and Unity
/// will automatically add the CanvasGroup component.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class MenuSelector : MonoBehaviour
{
    [Header("Buttons (0 = Settings, 1 = Play, 2 = Reset)")]
    public RectTransform[] buttons;
    public Image[] outlines;
    public Image[] backgrounds;

    [Header("Visual Settings")]
    public float selectedScale = 1.2f;
    public float normalScale = 1f;
    public float animSpeed = 8f;
    public float bgSpeed = 6f;
    public float fadeSpeed = 2f; // Speed for the new fade-in

    public Color outlineGold = new Color(1f, 0.85f, 0f);
    public Color outlineBlack = Color.black;
    public Color bgWhite = Color.white;
    public Color bgGrey = new Color(0.3f, 0.3f, 0.3f);

    [Header("Input Cooldown")]
    public float moveCooldown = 0.15f;

    [Header("Audio")]
    public AudioSource audioSource; // Assign your UI AudioSource here
    public AudioClip switchSound;   // Assign the button switch sound here

    // Input
    private InputActions input;
    private InputAction left;
    private InputAction right;
    private InputAction select;
    private InputAction startBtn;

    // State
    private int index = 1;       // start on Play
    private float lastMoveTime;
    private float pulseTime;
    private bool isFading = true; // Flag to block update/input during fade
    private CanvasGroup canvasGroup;

    void Awake()
    {
        input = new InputActions();

        left = input.UI.NavigateLeft;
        right = input.UI.NavigateRight;
        select = input.UI.Select;
        startBtn = input.UI.Start;

        // Get the CanvasGroup component
        canvasGroup = GetComponent<CanvasGroup>();

        // Try to get AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogWarning("MenuSelector: No AudioSource found or assigned. Add one to this GameObject to play sounds.");
            }
        }
    }

    void OnEnable()
    {
        left.performed += ctx => Move(-1);
        right.performed += ctx => Move(+1);
        select.performed += ctx => ActivateIndex(index);
        startBtn.performed += ctx => ActivateIndex(index);

        left.Enable();
        right.Enable();
        select.Enable();
        startBtn.Enable();

        // --- NEW FADE-IN LOGIC ---
        // 1. Set all buttons to their NORMAL state (instead of pre-selecting one)
        SetInitialVisuals(); 
        
        // 2. Prepare canvas for fade-in
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        isFading = true;

        // 3. Start the fade-in coroutine
        StartCoroutine(FadeInCanvas());
    }

    /// <summary>
    /// Coroutine to fade the canvas alpha from 0 to 1.
    /// </summary>
    IEnumerator FadeInCanvas()
    {
        float alpha = 0f;
        while (alpha < 1f)
        {
            // Move alpha towards 1, independent of game time
            alpha = Mathf.MoveTowards(alpha, 1f, fadeSpeed * Time.unscaledDeltaTime);
            canvasGroup.alpha = alpha;
            yield return null; // Wait for the next frame
        }

        // Ensure it's fully opaque and intractable
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        isFading = false; // Allow updates and input
    }

    void OnDisable()
    {
        left.performed -= ctx => Move(-1);
        right.performed -= ctx => Move(+1);
        select.performed -= ctx => ActivateIndex(index);
        startBtn.performed -= ctx => ActivateIndex(index);

        left.Disable();
        right.Disable();
        select.Disable();
        startBtn.Disable();

        // Stop any running coroutines on this object
        StopAllCoroutines();
    }

    // ----------------------- UPDATE LOOP -----------------------
    void Update()
    {
        // --- NEW ---
        // Don't run any visual updates while fading in
        if (isFading)
            return;

        pulseTime += Time.unscaledDeltaTime;

        for (int i = 0; i < buttons.Length; i++)
        {
            bool selected = (i == index);

            // Smooth scale
            float targetScale = selected ? selectedScale : normalScale;
            buttons[i].localScale = Vector3.Lerp(
                buttons[i].localScale,
                Vector3.one * targetScale,
                Time.unscaledDeltaTime * animSpeed
            );

            // Smooth background fade
            if (i < backgrounds.Length && backgrounds[i] != null)
            {
                Color target = selected ? bgWhite : bgGrey;
                backgrounds[i].color = Color.Lerp(
                    backgrounds[i].color,
                    target,
                    Time.unscaledDeltaTime * bgSpeed
                );
            }

            // Outline smooth pulse
            if (i < outlines.Length && outlines[i] != null)
            {
                if (selected)
                {
                    float pulse = (Mathf.Sin(pulseTime * 3.5f) + 1f) * 0.5f;
                    outlines[i].color = Color.Lerp(outlineBlack, outlineGold, pulse);
                }
                else
                {
                    outlines[i].color = outlineBlack;
                }
            }
        }
    }

    // ----------------------- MOVEMENT -----------------------
    private void Move(int direction)
    {
        // --- NEW ---
        // Don't allow movement while fading
        if (isFading)
            return;

        if (Time.unscaledTime - lastMoveTime < moveCooldown)
            return;

        lastMoveTime = Time.unscaledTime;

        // --- NEW: Play Sound ---
        if (audioSource != null && switchSound != null)
        {
            audioSource.PlayOneShot(switchSound);
        }

        index = (index + direction + buttons.Length) % buttons.Length;
    }

    // Snap everything instantly to a NEUTRAL state
    private void SetInitialVisuals()
    {
        pulseTime = 0; // Reset pulse time so it starts consistently
        for (int i = 0; i < buttons.Length; i++)
        {
            // Set ALL buttons to the unselected state
            buttons[i].localScale = Vector3.one * normalScale;

            if (i < backgrounds.Length)
                backgrounds[i].color = bgGrey;

            if (i < outlines.Length)
                outlines[i].color = outlineBlack;
        }
    }

    // ----------------------- ACTIVATION -----------------------
    private void ActivateIndex(int idx)
    {
        // --- NEW ---
        // Don't allow activation while fading
        if (isFading)
            return;

        switch (idx)
        {
            case 0: OpenSettings(); break;
            case 1: StartGame(); break;
            case 2: ResetGame(); break;
        }
    }

    private void OpenSettings() => Debug.Log("Open Settings");
    private void StartGame() => Debug.Log("Start Game");
    private void ResetGame()
    {
         SceneManager.LoadScene("PromoScene");
    }
}