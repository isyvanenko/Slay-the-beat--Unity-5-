using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro; // Add this for TextMeshPro

/// <summary>
/// Controls the in-game pause menu, including UI navigation, pause/resume timing,
/// countdown resume flow, menu animations, audio feedback, and pause-related object visibility.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    /// <summary>Canvas group used to fade and enable/disable interaction for the pause menu.</summary>
    [Header("Canvas References")]
    public CanvasGroup pauseCanvasGroup;
    /// <summary>Root panel transform scaled during the pause menu pop-up animation.</summary>
    public RectTransform pauseMenuPanel;
    
    /// <summary>Selectable pause menu button transforms, ordered as restart, resume, and back to menu.</summary>
    [Header("Buttons")]
    public RectTransform[] buttons;
    /// <summary>Outline images animated to show the currently selected button.</summary>
    public Image[] outlines;
    /// <summary>Background images tinted to show selected and unselected button states.</summary>
    public Image[] backgrounds;
    
    /// <summary>Scale applied to the currently selected button.</summary>
    [Header("Visual Settings")]
    public float selectedScale = 1.2f;
    /// <summary>Scale applied to unselected buttons.</summary>
    public float normalScale = 1f;
    /// <summary>Interpolation speed for button scale animation.</summary>
    public float animSpeed = 8f;
    /// <summary>Interpolation speed for button background color changes.</summary>
    public float bgSpeed = 6f;
    /// <summary>Speed multiplier for pause menu fade animations.</summary>
    public float fadeSpeed = 2f;
    /// <summary>Reserved speed value for pop-up scale animation tuning.</summary>
    public float popupScaleSpeed = 5f;
    
    /// <summary>Highlight color used by the selected button outline pulse.</summary>
    public Color outlineGold = new Color(1f, 0.85f, 0f);
    /// <summary>Default outline color for unselected buttons.</summary>
    public Color outlineBlack = Color.black;
    /// <summary>Background color used by the selected button.</summary>
    public Color bgWhite = Color.white;
    /// <summary>Background color used by unselected buttons.</summary>
    public Color bgGrey = new Color(0.3f, 0.3f, 0.3f);
    
    /// <summary>Initial local scale of the pause menu panel before opening.</summary>
    [Header("Popup Animation")]
    public Vector3 popupStartScale = new Vector3(0.8f, 0.8f, 0.8f);
    /// <summary>Final local scale of the pause menu panel after opening.</summary>
    public Vector3 popupTargetScale = Vector3.one;
    /// <summary>Curve used to ease the pause menu panel scale animation.</summary>
    public AnimationCurve popupCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    /// <summary>Whether resuming from pause should show a countdown before gameplay continues.</summary>
    [Header("Countdown Settings")]
    public bool useCountdown = true; // Toggle countdown on/off (default true)
    /// <summary>Canvas shown while counting down before gameplay resumes.</summary>
    public GameObject countdownCanvas;
    /// <summary>Text component that displays the current countdown number.</summary>
    public TextMeshProUGUI countdownText; // Using TextMeshPro
    /// <summary>Configured countdown length in seconds.</summary>
    public float countdownDelay = 3f; // Countdown from 3 to 1
    /// <summary>Reserved speed value for countdown scale animation tuning.</summary>
    public float countdownScaleSpeed = 5f;
    /// <summary>Sound played once per countdown tick.</summary>
    public AudioClip countdownTickSound;
    
    /// <summary>Minimum unscaled time between menu navigation moves.</summary>
    [Header("Input Cooldown")]
    public float moveCooldown = 0.15f;
    
    /// <summary>Audio source used to play pause menu sound effects.</summary>
    [Header("Audio")]
    public AudioSource audioSource;
    /// <summary>Sound played when the pause menu opens.</summary>
    public AudioClip openSound;
    /// <summary>Sound played when the pause menu closes.</summary>
    public AudioClip closeSound;
    /// <summary>Sound played when moving between buttons.</summary>
    public AudioClip switchSound;
    /// <summary>Sound played when selecting a button.</summary>
    public AudioClip selectSound;
    
    /// <summary>Event invoked when gameplay resumes from the pause menu.</summary>
    [Header("Button Actions")]
    public UnityEvent onResume;
    /// <summary>Event invoked when the restart button is selected.</summary>
    public UnityEvent onRestart;
    /// <summary>Event invoked when the back-to-menu button is selected.</summary>
    public UnityEvent onBackToMenu;
    
    /// <summary>Objects enabled while the game is paused and disabled when leaving the pause menu.</summary>
    [Header("Additional Features")]
    public GameObject[] specialObjectsOnPause;
    
    // Input
    /// <summary>Generated input action wrapper used for menu navigation.</summary>
    private InputActions inputActions;
    /// <summary>Input action that moves selection to the previous pause menu item.</summary>
    private InputAction navigateLeft;
    /// <summary>Input action that moves selection to the next pause menu item.</summary>
    private InputAction navigateRight;
    /// <summary>Input action that activates the currently selected pause menu item.</summary>
    private InputAction select;
    
    // State
    /// <summary>Index of the currently selected pause menu button.</summary>
    private int currentIndex = 1;
    /// <summary>Unscaled timestamp of the last accepted navigation move.</summary>
    private float lastMoveTime;
    /// <summary>Timer used to pulse the selected button outline.</summary>
    private float pulseTime;
    /// <summary>Whether a pause menu animation is currently running.</summary>
    private bool isAnimating = false;
    /// <summary>Whether the game is currently paused through this menu.</summary>
    private bool isPaused = false;
    /// <summary>Whether the resume countdown is currently active.</summary>
    private bool isCountingDown = false;
    /// <summary>Gameplay manager whose player inputs are disabled while the pause menu is active.</summary>
    private GameplayManager gameplayManager;
    /// <summary>Time scale captured before pausing so it can be restored on resume.</summary>
    private float previousTimeScale = 1f;
    
    /// <summary>
    /// Initializes input actions, caches optional component references, and hides pause-related UI.
    /// </summary>
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
    
    /// <summary>
    /// Registers pause menu input callbacks.
    /// </summary>
    void OnEnable()
    {
        navigateLeft.performed += ctx => MoveLeft();
        navigateRight.performed += ctx => MoveRight();
        select.performed += ctx => SelectCurrentItem();
    }
    
    /// <summary>
    /// Unregisters input callbacks and disables the UI input map when this component is disabled.
    /// </summary>
    void OnDisable()
    {
        navigateLeft.performed -= ctx => MoveLeft();
        navigateRight.performed -= ctx => MoveRight();
        select.performed -= ctx => SelectCurrentItem();
        
        if (inputActions != null && inputActions.UI.enabled)
            inputActions.UI.Disable();
    }
    
    /// <summary>
    /// Animates selected and unselected button visuals while the pause menu is active.
    /// </summary>
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
    
    /// <summary>
    /// Moves the current menu selection one item to the left when the input cooldown allows it.
    /// </summary>
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
    
    /// <summary>
    /// Moves the current menu selection one item to the right when the input cooldown allows it.
    /// </summary>
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
    
    /// <summary>
    /// Executes the action associated with the currently selected menu button.
    /// </summary>
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
    
    /// <summary>
    /// Fades out the pause menu and immediately resumes gameplay without a countdown.
    /// </summary>
    /// <returns>Coroutine enumerator for the immediate resume sequence.</returns>
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
    
    /// <summary>
    /// Fades out the pause menu, displays a countdown, then resumes gameplay.
    /// </summary>
    /// <returns>Coroutine enumerator for the countdown resume sequence.</returns>
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
    
    /// <summary>
    /// Plays the scale punch animation for the visible countdown number.
    /// </summary>
    /// <returns>Coroutine enumerator for the countdown text animation.</returns>
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
    
    /// <summary>
    /// Fades out the pause menu, restores gameplay input, and invokes the restart event.
    /// </summary>
    /// <returns>Coroutine enumerator for the restart selection sequence.</returns>
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
    
    /// <summary>
    /// Fades out the pause menu, restores gameplay input, and invokes the back-to-menu event.
    /// </summary>
    /// <returns>Coroutine enumerator for the back-to-menu selection sequence.</returns>
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
    
    /// <summary>
    /// Opens the pause menu, freezes gameplay time, and switches control to the UI input map.
    /// </summary>
    /// <param name="manager">Gameplay manager containing the player inputs to disable while paused.</param>
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
    
    /// <summary>
    /// Enables pause menu input and deactivates player gameplay input.
    /// </summary>
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
    
    /// <summary>
    /// Disables pause menu input and reactivates player gameplay input.
    /// </summary>
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
    
    /// <summary>
    /// Starts the close animation when the pause menu can be closed normally.
    /// </summary>
    public void ClosePauseMenu()
    {
        if (!isPaused || isAnimating || isCountingDown) return;
        
        StartCoroutine(CloseAnimation());
    }
    
    /// <summary>
    /// Plays the pause menu opening fade and scale animation, then selects the resume button.
    /// </summary>
    /// <returns>Coroutine enumerator for the opening animation.</returns>
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
    
    /// <summary>
    /// Plays the pause menu closing fade and scale animation, then clears pause state.
    /// </summary>
    /// <returns>Coroutine enumerator for the closing animation.</returns>
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
    
    /// <summary>
    /// Sets all configured pause-only objects active or inactive.
    /// </summary>
    /// <param name="active">Whether the configured pause-only objects should be active.</param>
    private void SetSpecialObjectsActive(bool active)
    {
        foreach (GameObject obj in specialObjectsOnPause)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }
    
    /// <summary>
    /// Gets whether the pause menu currently considers the game paused.
    /// </summary>
    /// <returns>True when paused through this menu; otherwise false.</returns>
    public bool IsPaused()
    {
        return isPaused;
    }
    
    /// <summary>
    /// Gets whether the resume countdown is currently running.
    /// </summary>
    /// <returns>True while the countdown is active; otherwise false.</returns>
    public bool IsCountingDown()
    {
        return isCountingDown;
    }
    
    /// <summary>
    /// Immediately stops pause menu coroutines, hides pause UI, restores time and input, and clears state.
    /// </summary>
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
