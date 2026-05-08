using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro; // Add this for TextMeshPro

public class PauseMenu : MonoBehaviour
{
    [Header("Canvas References")]
    public CanvasGroup pauseCanvasGroup;
    public RectTransform pauseMenuPanel;
    
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
    public float popupScaleSpeed = 5f;
    
    public Color outlineGold = new Color(1f, 0.85f, 0f);
    public Color outlineBlack = Color.black;
    public Color bgWhite = Color.white;
    public Color bgGrey = new Color(0.3f, 0.3f, 0.3f);
    
    [Header("Popup Animation")]
    public Vector3 popupStartScale = new Vector3(0.8f, 0.8f, 0.8f);
    public Vector3 popupTargetScale = Vector3.one;
    public AnimationCurve popupCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Countdown Settings")]
    public bool useCountdown = true; // Toggle countdown on/off (default true)
    public GameObject countdownCanvas;
    public TextMeshProUGUI countdownText; // Using TextMeshPro
    public float countdownDelay = 3f; // Countdown from 3 to 1
    public float countdownScaleSpeed = 5f;
    public AudioClip countdownTickSound;
    
    [Header("Input Cooldown")]
    public float moveCooldown = 0.15f;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip switchSound;
    public AudioClip selectSound;
    
    [Header("Button Actions")]
    public UnityEvent onResume;
    public UnityEvent onRestart;
    public UnityEvent onBackToMenu;
    
    [Header("Additional Features")]
    public GameObject[] specialObjectsOnPause;
    
    // Input
    private InputActions inputActions;
    private InputAction navigateLeft;
    private InputAction navigateRight;
    private InputAction select;
    
    // State
    private int currentIndex = 1;
    private float lastMoveTime;
    private float pulseTime;
    private bool isAnimating = false;
    private bool isPaused = false;
    private bool isCountingDown = false;
    private GameplayManager gameplayManager;
    private float previousTimeScale = 1f;
    
    void Awake()
    {
        inputActions = new InputActions();
        
        navigateLeft = inputActions.UI.NavigateLeft;
        navigateRight = inputActions.UI.NavigateRight;
        select = inputActions.UI.Select;
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        if (pauseCanvasGroup == null)
            pauseCanvasGroup = GetComponent<CanvasGroup>();
        
        // Start hidden
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
        }
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.localScale = popupStartScale;
        
        // Hide countdown canvas initially
        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);
        
        SetSpecialObjectsActive(false);
    }
    
    void OnEnable()
    {
        navigateLeft.performed += ctx => MoveLeft();
        navigateRight.performed += ctx => MoveRight();
        select.performed += ctx => SelectCurrentItem();
    }
    
    void OnDisable()
    {
        navigateLeft.performed -= ctx => MoveLeft();
        navigateRight.performed -= ctx => MoveRight();
        select.performed -= ctx => SelectCurrentItem();
        
        if (inputActions != null && inputActions.UI.enabled)
            inputActions.UI.Disable();
    }
    
    void Update()
    {
        if (!isPaused || isAnimating || isCountingDown) return;
        
        pulseTime += Time.unscaledDeltaTime;
        
        // Animate buttons
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            
            bool selected = (i == currentIndex);
            
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
    
    private void MoveLeft()
    {
        if (isAnimating || isCountingDown) return;
        if (Time.unscaledTime - lastMoveTime < moveCooldown) return;
        
        lastMoveTime = Time.unscaledTime;
        
        if (audioSource != null && switchSound != null)
            audioSource.PlayOneShot(switchSound);
        
        currentIndex = (currentIndex - 1 + buttons.Length) % buttons.Length;
        
        if (buttons[currentIndex] != null)
        {
            var button = buttons[currentIndex].GetComponent<Button>();
            if (button != null && button.interactable)
                EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }
    
    private void MoveRight()
    {
        if (isAnimating || isCountingDown) return;
        if (Time.unscaledTime - lastMoveTime < moveCooldown) return;
        
        lastMoveTime = Time.unscaledTime;
        
        if (audioSource != null && switchSound != null)
            audioSource.PlayOneShot(switchSound);
        
        currentIndex = (currentIndex + 1) % buttons.Length;
        
        if (buttons[currentIndex] != null)
        {
            var button = buttons[currentIndex].GetComponent<Button>();
            if (button != null && button.interactable)
                EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }
    
    private void SelectCurrentItem()
    {
        if (isAnimating || isCountingDown) return;
        
        if (audioSource != null && selectSound != null)
            audioSource.PlayOneShot(selectSound);
        
        switch(currentIndex)
        {
            case 0: // Left button - RESTART
                StartCoroutine(RestartImmediate());
                break;
            case 1: // Middle button - RESUME (with or without countdown based on useCountdown)
                if (useCountdown)
                    StartCoroutine(ResumeWithCountdown());
                else
                    StartCoroutine(ResumeImmediate());
                break;
            case 2: // Right button - BACK TO MENU
                StartCoroutine(BackToMenuImmediate());
                break;
        }
    }
    
    private IEnumerator ResumeImmediate()
    {
        isAnimating = true;
        
        // Quick fade out of pause menu
        if (pauseCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                pauseCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.2f);
                yield return null;
            }
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
        }
        
        isAnimating = false;
        isCountingDown = false;
        
        // Resume the game immediately
        Time.timeScale = previousTimeScale;
        SwitchBackToGameplayInputMap();
        
        isPaused = false;
        
        // Resume gameplay
        onResume?.Invoke();
        
        Debug.Log("Game resumed immediately (no countdown)");
    }
    
    private IEnumerator ResumeWithCountdown()
    {
        isCountingDown = true;
        
        // Hide pause menu UI
        if (pauseCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                pauseCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.3f);
                yield return null;
            }
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
        }
        
        // Show countdown canvas
        if (countdownCanvas != null)
        {
            countdownCanvas.SetActive(true);
            countdownCanvas.transform.localScale = Vector3.zero;
            
            // Pop in animation
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float scale = Mathf.Lerp(0f, 1f, elapsed / 0.2f);
                countdownCanvas.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            countdownCanvas.transform.localScale = Vector3.one;
        }
        
        // Countdown from 3 to 1 (no "GO!")
        for (int i = 3; i > 0; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
                StartCoroutine(AnimateCountdownNumber());
            }
            
            // Play tick sound
            if (audioSource != null && countdownTickSound != null)
                audioSource.PlayOneShot(countdownTickSound);
            
            yield return new WaitForSecondsRealtime(1f);
        }
        
        // Hide countdown canvas
        if (countdownCanvas != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.15f)
            {
                elapsed += Time.unscaledDeltaTime;
                float scale = Mathf.Lerp(1f, 0f, elapsed / 0.15f);
                countdownCanvas.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            countdownCanvas.SetActive(false);
        }
        
        isCountingDown = false;
        
        // Now actually resume the game
        Time.timeScale = previousTimeScale;
        SwitchBackToGameplayInputMap();
        
        isPaused = false;
        
        // Resume gameplay
        onResume?.Invoke();
        
        Debug.Log("Countdown finished - Game resumed");
    }
    
    private IEnumerator AnimateCountdownNumber()
    {
        if (countdownText == null) yield break;
        
        // Scale up animation
        float elapsed = 0f;
        float duration = 0.2f;
        
        Vector3 originalScale = countdownText.transform.localScale;
        Vector3 targetScale = originalScale * 1.5f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, 1.5f, t);
            countdownText.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1.5f, 1f, t);
            countdownText.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        
        countdownText.transform.localScale = originalScale;
    }
    
    private IEnumerator RestartImmediate()
    {
        isAnimating = true;
        
        if (pauseCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                pauseCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.2f);
                yield return null;
            }
        }
        
        Time.timeScale = 1f;
        SwitchBackToGameplayInputMap();
        
        isAnimating = false;
        isPaused = false;
        
        onRestart?.Invoke();
    }
    
    private IEnumerator BackToMenuImmediate()
    {
        isAnimating = true;
        
        if (pauseCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                pauseCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.2f);
                yield return null;
            }
        }
        
        Time.timeScale = 1f;
        SwitchBackToGameplayInputMap();
        
        isAnimating = false;
        isPaused = false;
        
        onBackToMenu?.Invoke();
    }
    
    public void OpenPauseMenu(GameplayManager manager)
    {
        if (isPaused || isAnimating || isCountingDown) return;
        
        gameplayManager = manager;
        isPaused = true;
        
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        SwitchToUIInputMap();
        SetSpecialObjectsActive(true);
        
        StartCoroutine(OpenAnimation());
    }
    
    private void SwitchToUIInputMap()
    {
        if (inputActions != null)
        {
            if (inputActions.Gameplay.enabled)
                inputActions.Gameplay.Disable();
            
            inputActions.UI.Enable();
        }
        
        if (gameplayManager != null)
        {
            if (gameplayManager.p1Input != null)
                gameplayManager.p1Input.DeactivateInput();
            if (gameplayManager.p2Input != null && SessionConfig.PlayerCount == 2)
                gameplayManager.p2Input.DeactivateInput();
        }
    }
    
    private void SwitchBackToGameplayInputMap()
    {
        if (inputActions != null && inputActions.UI.enabled)
            inputActions.UI.Disable();
        
        if (gameplayManager != null)
        {
            if (gameplayManager.p1Input != null)
                gameplayManager.p1Input.ActivateInput();
            if (gameplayManager.p2Input != null && SessionConfig.PlayerCount == 2)
                gameplayManager.p2Input.ActivateInput();
        }
    }
    
    public void ClosePauseMenu()
    {
        if (!isPaused || isAnimating || isCountingDown) return;
        
        StartCoroutine(CloseAnimation());
    }
    
    private IEnumerator OpenAnimation()
    {
        isAnimating = true;
        
        if (audioSource != null && openSound != null)
            audioSource.PlayOneShot(openSound);
        
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.interactable = true;
            pauseCanvasGroup.blocksRaycasts = true;
        }
        
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.unscaledDeltaTime * fadeSpeed;
            float t = Mathf.Clamp01(elapsed);
            
            if (pauseCanvasGroup != null)
                pauseCanvasGroup.alpha = t;
            
            if (pauseMenuPanel != null)
            {
                float scaleT = popupCurve.Evaluate(t);
                pauseMenuPanel.localScale = Vector3.Lerp(popupStartScale, popupTargetScale, scaleT);
            }
            
            yield return null;
        }
        
        if (pauseCanvasGroup != null)
            pauseCanvasGroup.alpha = 1f;
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.localScale = popupTargetScale;
        
        currentIndex = 1;
        
        if (buttons.Length > currentIndex && buttons[currentIndex] != null)
        {
            var button = buttons[currentIndex].GetComponent<Button>();
            if (button != null && button.interactable)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                
                if (backgrounds.Length > currentIndex && backgrounds[currentIndex] != null)
                    backgrounds[currentIndex].color = bgWhite;
                if (outlines.Length > currentIndex && outlines[currentIndex] != null)
                    outlines[currentIndex].color = outlineGold;
            }
        }
        
        isAnimating = false;
    }
    
    private IEnumerator CloseAnimation()
    {
        isAnimating = true;
        
        if (audioSource != null && closeSound != null)
            audioSource.PlayOneShot(closeSound);
        
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.unscaledDeltaTime * fadeSpeed;
            float t = Mathf.Clamp01(elapsed);
            
            if (pauseCanvasGroup != null)
                pauseCanvasGroup.alpha = 1f - t;
            
            if (pauseMenuPanel != null)
            {
                float scaleT = popupCurve.Evaluate(1f - t);
                pauseMenuPanel.localScale = Vector3.Lerp(popupStartScale, popupTargetScale, scaleT);
            }
            
            yield return null;
        }
        
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
        }
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.localScale = popupStartScale;
        
        SetSpecialObjectsActive(false);
        
        isAnimating = false;
        isPaused = false;
    }
    
    private void SetSpecialObjectsActive(bool active)
    {
        foreach (GameObject obj in specialObjectsOnPause)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }
    
    public bool IsPaused()
    {
        return isPaused;
    }
    
    public bool IsCountingDown()
    {
        return isCountingDown;
    }
    
    public void ForceClose()
    {
        if (isPaused)
        {
            StopAllCoroutines();
            
            if (pauseCanvasGroup != null)
            {
                pauseCanvasGroup.alpha = 0f;
                pauseCanvasGroup.interactable = false;
                pauseCanvasGroup.blocksRaycasts = false;
            }
            
            if (pauseMenuPanel != null)
                pauseMenuPanel.localScale = popupStartScale;
            
            if (countdownCanvas != null)
                countdownCanvas.SetActive(false);
            
            SetSpecialObjectsActive(false);
            Time.timeScale = previousTimeScale;
            SwitchBackToGameplayInputMap();
            
            isPaused = false;
            isAnimating = false;
            isCountingDown = false;
        }
    }
}