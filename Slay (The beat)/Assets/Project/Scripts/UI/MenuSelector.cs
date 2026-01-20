using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class MenuSelector : MonoBehaviour
{
    [Header("Buttons")]
    public RectTransform[] buttons;
    public Image[] outlines;
    public Image[] backgrounds;

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

    // Input
    private InputActions input;
    private InputAction left;
    private InputAction right;
    private InputAction select;
    private InputAction startBtn;

    // State
    private int index = 0;
    private float lastMoveTime;
    private float pulseTime;
    private bool isFading = true;
    private CanvasGroup canvasGroup;

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

        SetInitialVisuals();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        isFading = true;

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
            alpha = Mathf.MoveTowards(alpha, 1f, fadeSpeed * Time.unscaledDeltaTime);
            canvasGroup.alpha = alpha;
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        isFading = false;
    }

    void Update()
    {
        if (isFading)
            return;

        pulseTime += Time.unscaledDeltaTime;

        for (int i = 0; i < buttons.Length; i++)
        {
            bool selected = (i == index);

            // -------- SCALE --------
            float targetScale = selected ? selectedScale : normalScale;
            buttons[i].localScale = Vector3.Lerp(
                buttons[i].localScale,
                Vector3.one * targetScale,
                Time.unscaledDeltaTime * animSpeed
            );

            // -------- BACKGROUND --------
            if (i < backgrounds.Length && backgrounds[i] != null)
            {
                Color target = selected ? bgWhite : bgGrey;
                backgrounds[i].color = Color.Lerp(
                    backgrounds[i].color,
                    target,
                    Time.unscaledDeltaTime * bgSpeed
                );
            }

            // -------- OUTLINE --------
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

            // -------- PARTICLES --------
            if (i < selectionParticles.Length && selectionParticles[i] != null)
            {
                selectionParticles[i].SetActive(selected);
            }

            // -------- SPRITES --------
            if (i < selectionSprites.Length && selectionSprites[i] != null)
            {
                selectionSprites[i].SetActive(selected);
            }
        }
    }

    private void Move(int direction)
    {
        if (isFading)
            return;

        if (Time.unscaledTime - lastMoveTime < moveCooldown)
            return;

        lastMoveTime = Time.unscaledTime;

        if (audioSource != null && switchSound != null)
            audioSource.PlayOneShot(switchSound);

        index = (index + direction + buttons.Length) % buttons.Length;
    }

    private void SetInitialVisuals()
    {
        pulseTime = 0f;

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].localScale = Vector3.one * normalScale;

            if (i < backgrounds.Length && backgrounds[i] != null)
                backgrounds[i].color = bgGrey;

            if (i < outlines.Length && outlines[i] != null)
                outlines[i].color = outlineBlack;

            if (i < selectionParticles.Length && selectionParticles[i] != null)
                selectionParticles[i].SetActive(false);

            if (i < selectionSprites.Length && selectionSprites[i] != null)
                selectionSprites[i].SetActive(false);
        }
    }

    private void ActivateIndex(int idx)
    {
        if (isFading)
            return;

        onSelectIndex?.Invoke(idx);
    }
}
