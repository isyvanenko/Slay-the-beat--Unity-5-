using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

public class ArcadeMainMenu : MonoBehaviour
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

    [Header("Element 1 Specific Objects (Index 1)")]
    public GameObject[] elementOneObjects; // These activate only when index 1 is selected

    [Header("Scene Names")]
    public string statsscene;
    public string gameplayscene;

    // Input
    private InputActions input;
    private InputAction left;
    private InputAction right;
    private InputAction select;
    private InputAction startBtn;

    // State
    private int index = 1; // Start at index 1 (Element 1)
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

        canvasGroup = GetComponent<CanvasGroup>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
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

        index = 1; // Start at Element 1 (index 1)
        
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
        
        UpdateElementOneObjects();
    }

    void Update()
    {
        if (isFading || hasSelected) 
            return;

        pulseTime += Time.unscaledDeltaTime;

        for (int i = 0; i < buttons.Length; i++)
        {
            bool selected = (i == index);

            float targetScale = selected ? selectedScale : normalScale;
            buttons[i].localScale = Vector3.Lerp(
                buttons[i].localScale,
                Vector3.one * targetScale,
                Time.unscaledDeltaTime * animSpeed
            );

            if (i < backgrounds.Length && backgrounds[i] != null)
            {
                Color target = selected ? bgWhite : bgGrey;
                backgrounds[i].color = Color.Lerp(
                    backgrounds[i].color,
                    target,
                    Time.unscaledDeltaTime * bgSpeed
                );
            }

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

    private void Move(int direction)
    {
        if (isFading || hasSelected)
            return;

        if (Time.unscaledTime - lastMoveTime < moveCooldown)
            return;

        lastMoveTime = Time.unscaledTime;

        if (audioSource != null && switchSound != null)
            audioSource.PlayOneShot(switchSound);

        index = (index + direction + buttons.Length) % buttons.Length;
        
        UpdateElementOneObjects();
    }

    private void SelectCurrentItem()
    {
        if (isFading || hasSelected)
            return;

        hasSelected = true;

        // Handle selection based on index
        switch(index)
        {
            case 0:
                // Index 0 - Open Stats (Left button)
                Debug.Log("Selected Index 0 - Opening Stats");
                OpenStats();
                break;
            case 1:
                // Index 1 - Element 1 (Your special middle button)
                Debug.Log("Selected Index 1 - Starting Game");
                StartTheGame();
                break;
            case 2:
                // Index 2 - Start Game (Middle/Right button)
                Debug.Log("Selected Index 2 - Starting Game");
                StartTheGame();
                break;
            case 3:
                // Index 3 - Exit (Right button)
                Debug.Log("Selected Index 3 - Exiting");
                Exit();
                break;
            default:
                Debug.LogWarning($"Unknown index selected: {index}");
                hasSelected = false;
                break;
        }
    }

    private void UpdateElementOneObjects()
    {
        // Activate special objects ONLY when index 1 is selected
        bool isElementOneSelected = (index == 1);
        
        foreach (GameObject obj in elementOneObjects)
        {
            if (obj != null)
                obj.SetActive(isElementOneSelected);
        }
    }

    private void SetInitialVisuals()
    {
        pulseTime = 0f;
        hasSelected = false;
        index = 1; // Start at index 1

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].localScale = Vector3.one * normalScale;

            if (i < backgrounds.Length && backgrounds[i] != null)
                backgrounds[i].color = bgGrey;

            if (i < outlines.Length && outlines[i] != null)
                outlines[i].color = outlineBlack;
        }
        
        // Set index 1 (Element 1) as selected at start
        if (buttons.Length > 1)
        {
            buttons[1].localScale = Vector3.one * selectedScale;
            
            if (backgrounds.Length > 1 && backgrounds[1] != null)
                backgrounds[1].color = bgWhite;
        }
        
        UpdateElementOneObjects();
    }

    public void OpenStats()
    {
        Debug.Log($"OpenStats called with scene: '{statsscene}'");
        
        if (!string.IsNullOrEmpty(statsscene))
        {
            if (TransitionManager.Instance != null)
                TransitionManager.Instance.LoadScene(statsscene, 0.5f);
            else
                Debug.LogError("TransitionManager.Instance is null!");
        }
        else
        {
            Debug.LogError("statsscene is empty! Please assign it in the Inspector.");
        }
    }

    public void Exit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void StartTheGame()
    {
        Debug.Log($"StartTheGame called with scene: '{gameplayscene}'");
        
        if (!string.IsNullOrEmpty(gameplayscene))
        {
            if (TransitionManager.Instance != null)
                TransitionManager.Instance.LoadScene(gameplayscene);
            else
                Debug.LogError("TransitionManager.Instance is null!");
        }
        else
        {
            Debug.LogError("gameplayscene is empty! Please assign it in the Inspector.");
        }
    }
}