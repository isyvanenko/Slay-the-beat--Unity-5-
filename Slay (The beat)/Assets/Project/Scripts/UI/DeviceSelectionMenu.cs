using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

/// <summary>
/// Complete device selection menu with visuals and assignment logic
/// </summary>
public class DeviceSelectionMenu : MonoBehaviour
{
    [Header("Button Components (2 Images + 1 Text per button)")]
    public Image[] buttonMainImages;
    public Image[] buttonTextImages;
    public TMP_Text[] buttonTexts;
    
    [Header("Player Selection Display")]
    public Image player1SelectedImage;
    public Sprite[] optionSprites;
    
    [Header("Selection Description")]
    public TMP_Text selectionText;
    [TextArea]
    public string[] selectionTexts;

    [Header("Colors")]
    public Color activeImageColor = Color.white;
    public Color unavailableImageColor = new Color(0.435f, 0.435f, 0.435f);
    public Color idleImageColor = new Color(0.7f, 0.7f, 0.7f);

    [Header("Visual Settings")]
    public float animSpeed = 8f;

    [Header("Input")]
    public float moveCooldown = 0.15f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip switchSound;
    public AudioClip selectionSound;
    public AudioClip errorSound;

    [Header("Next Menus")]
    public GameObject calibrationCanvas;
    public GameObject deviceSelectionMenuP2;
    
    [Header("Player Configuration")]
    public int currentPlayerIndex = 0;
    
    [Header("Direct Assignment References")]
    public PlayerInput player1Input;
    public PlayerInput player2Input;
    
    [Header("Scene Loading")]
    public string songSelectionScene = "SongSelectionScene";
    
    [Header("Menu Selector Reference (Optional - for visual sync)")]
    public MenuSelector menuSelector;

    [Header("ESC Hold to Return")]
    public float escHoldRequiredTime = 3.0f;
    public Slider escHoldProgressSlider;
    public CanvasGroup mainMenuCanvas;

    private const int BUTTON_ARROW_KEYS = 0;
    private const int BUTTON_CALIBRATION = 1;
    private const int BUTTON_WASD_KEYS = 2;
    
    private bool[] buttonAvailable = { true, true, true };
    private int currentIndex = 0;
    private float lastMoveTime;
    private bool hasSelected = false;
    private CanvasGroup canvasGroup;
    private InputActions input;
    private InputAction left;
    private InputAction right;
    private InputAction select;

    // ESC hold state
    private bool isEscHeld = false;
    private float escHoldTimer = 0f;
    private bool wasEscPressedLastFrame = false;
    private Coroutine escSliderFadeRoutine;

    void Awake()
    {
        input = new InputActions();
        left = input.UI.NavigateLeft;
        right = input.UI.NavigateRight;
        select = input.UI.Select;
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Hide ESC slider initially
        if (escHoldProgressSlider != null)
        {
            escHoldProgressSlider.gameObject.SetActive(true);
            escHoldProgressSlider.value = 0f;
            CanvasGroup sliderCG = escHoldProgressSlider.GetComponent<CanvasGroup>();
            if (sliderCG == null) sliderCG = escHoldProgressSlider.gameObject.AddComponent<CanvasGroup>();
            sliderCG.alpha = 0f;
        }
    }

    void OnEnable()
    {
        left.performed += ctx => Move(-1);
        right.performed += ctx => Move(1);
        select.performed += ctx => SelectCurrent();
        
        left.Enable();
        right.Enable();
        select.Enable();
        
        ResetMenu();
    }

    void OnDisable()
    {
        left.Disable();
        right.Disable();
        select.Disable();
    }

    void Update()
    {
        if (hasSelected) return;
        
        // Update visuals
        for (int i = 0; i < buttonMainImages.Length; i++)
        {
            bool isSelected = (i == currentIndex);
            bool isAvailable = buttonAvailable[i];
            
            Color targetColor = !isAvailable ? unavailableImageColor : (isSelected ? activeImageColor : idleImageColor);
            
            if (buttonMainImages[i] != null)
            {
                buttonMainImages[i].color = Color.Lerp(
                    buttonMainImages[i].color, targetColor, Time.unscaledDeltaTime * animSpeed);
            }
            if (buttonTextImages[i] != null)
            {
                buttonTextImages[i].color = Color.Lerp(
                    buttonTextImages[i].color, targetColor, Time.unscaledDeltaTime * animSpeed);
            }
        }
        
        // Handle ESC hold
        HandleEscInput();
    }

    private void HandleEscInput()
    {
        bool escPressed = false;
        
        if (Keyboard.current != null && Keyboard.current.escapeKey.isPressed)
        {
            escPressed = true;
        }
        
        bool escJustPressed = escPressed && !wasEscPressedLastFrame;
        
        if (escJustPressed)
        {
            // Single press - could be used for something else if needed
        }
        
        if (escPressed)
        {
            if (!isEscHeld)
            {
                isEscHeld = true;
                escHoldTimer = 0f;
            }
            
            escHoldTimer += Time.unscaledDeltaTime;
            
            if (escHoldTimer > 0.3f && escHoldProgressSlider != null)
            {
                CanvasGroup sliderCG = escHoldProgressSlider.GetComponent<CanvasGroup>();
                if (sliderCG != null && sliderCG.alpha < 0.5f)
                {
                    ShowEscSlider();
                }
            }
            
            if (escHoldProgressSlider != null && escHoldTimer > 0.3f)
            {
                escHoldProgressSlider.value = Mathf.Clamp01((escHoldTimer - 0.3f) / (escHoldRequiredTime - 0.3f));
            }
            
            if (escHoldTimer >= escHoldRequiredTime)
            {
                ReturnToMainMenu();
            }
        }
        else
        {
            if (isEscHeld)
            {
                isEscHeld = false;
                escHoldTimer = 0f;
                HideEscSlider();
            }
        }
        
        wasEscPressedLastFrame = escPressed;
    }

    private void ShowEscSlider()
    {
        if (escHoldProgressSlider == null) return;
        
        if (!gameObject.activeInHierarchy)
        {
            CanvasGroup sliderCG = escHoldProgressSlider.GetComponent<CanvasGroup>();
            if (sliderCG != null) sliderCG.alpha = 1f;
            return;
        }
        
        if (escSliderFadeRoutine != null) StopCoroutine(escSliderFadeRoutine);
        escSliderFadeRoutine = StartCoroutine(FadeSliderAlpha(escHoldProgressSlider, 1f));
    }

    private void HideEscSlider()
    {
        if (escHoldProgressSlider == null) return;
        
        if (!gameObject.activeInHierarchy)
        {
            CanvasGroup sliderCG = escHoldProgressSlider.GetComponent<CanvasGroup>();
            if (sliderCG != null) sliderCG.alpha = 0f;
            escHoldProgressSlider.value = 0f;
            return;
        }
        
        if (escSliderFadeRoutine != null) StopCoroutine(escSliderFadeRoutine);
        escSliderFadeRoutine = StartCoroutine(FadeSliderAlpha(escHoldProgressSlider, 0f));
    }

    IEnumerator FadeSliderAlpha(Slider slider, float targetAlpha)
    {
        CanvasGroup sliderCG = slider.GetComponent<CanvasGroup>();
        if (sliderCG == null) yield break;
        
        float startAlpha = sliderCG.alpha;
        float timer = 0f;
        float duration = 1f / 5f; // sliderFadeSpeed
        
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            sliderCG.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }
        
        sliderCG.alpha = targetAlpha;
        
        if (targetAlpha <= 0.01f)
        {
            slider.value = 0f;
        }
    }

    private void ReturnToMainMenu()
    {
        isEscHeld = false;
        escHoldTimer = 0f;
        wasEscPressedLastFrame = false;
        
        if (escHoldProgressSlider != null)
        {
            escHoldProgressSlider.value = 1f;
        }
        
        StartCoroutine(ReturnToMainMenuRoutine());
    }

    IEnumerator ReturnToMainMenuRoutine()
    {
        // Fade out current menu
        float timer = 0f;
        float startAlpha = canvasGroup.alpha;
        float fadeDuration = 0.5f;
        
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        
        // Hide ESC slider
        if (escHoldProgressSlider != null)
        {
            CanvasGroup sliderCG = escHoldProgressSlider.GetComponent<CanvasGroup>();
            if (sliderCG != null) sliderCG.alpha = 0f;
            escHoldProgressSlider.value = 0f;
        }
        
        // Deactivate this menu
        gameObject.SetActive(false);
        
        // Show main menu
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.gameObject.SetActive(true);
            mainMenuCanvas.alpha = 0f;
            
            timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                mainMenuCanvas.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            mainMenuCanvas.alpha = 1f;
        }
    }

    public void ResetMenu()
    {
        StopAllCoroutines();
        hasSelected = false;
        
        // Reset availability based on player
        if (currentPlayerIndex == 0)
        {
            // Player 1 - all buttons available
            buttonAvailable[BUTTON_ARROW_KEYS] = true;
            buttonAvailable[BUTTON_CALIBRATION] = true;
            buttonAvailable[BUTTON_WASD_KEYS] = true;
            HidePlayer1Selection();
        }
        else
        {
            // Player 2 - disable what Player 1 took
            if (SessionConfig.P1DeviceType == "Keyboard-ArrowKeys")
            {
                buttonAvailable[BUTTON_ARROW_KEYS] = false;
                ShowPlayer1Selection(0);
            }
            else if (SessionConfig.P1DeviceType == "Keyboard-WASD")
            {
                buttonAvailable[BUTTON_WASD_KEYS] = false;
                ShowPlayer1Selection(1);
            }
            buttonAvailable[BUTTON_CALIBRATION] = true;
        }
        
        currentIndex = GetFirstAvailableIndex();
        
        // Sync with external MenuSelector if assigned
        if (menuSelector != null && menuSelector.IsAvailableForSelection())
        {
            menuSelector.SetCurrentIndex(currentIndex);
        }
        
        UpdateMenuTexts();
        ResetColors();
        
        // Reset ESC hold state
        isEscHeld = false;
        escHoldTimer = 0f;
        wasEscPressedLastFrame = false;
        
        // Hide ESC slider
        HideEscSlider();
        
        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            StartCoroutine(FadeIn());
        }
    }
    
    void UpdateMenuTexts()
    {
        string playerPrefix = (currentPlayerIndex == 0) ? "PLAYER 1" : "PLAYER 2";
        
        if (selectionTexts.Length >= 3)
        {
            selectionTexts[0] = $"{playerPrefix}: ARROW KEYS\nUse arrow keys (↑ ↓ ← →)";
            selectionTexts[1] = $"{playerPrefix}: CALIBRATION\nCustomize with any device";
            selectionTexts[2] = $"{playerPrefix}: WASD KEYS\nUse WASD keys";
        }
        
        if (selectionText != null && selectionTexts.Length > 0)
        {
            selectionText.text = selectionTexts[currentIndex];
        }
    }
    
    void ResetColors()
    {
        for (int i = 0; i < buttonMainImages.Length; i++)
        {
            Color targetColor = buttonAvailable[i] ? idleImageColor : unavailableImageColor;
            if (buttonMainImages[i] != null)
                buttonMainImages[i].color = targetColor;
            if (buttonTextImages[i] != null)
                buttonTextImages[i].color = targetColor;
        }
    }
    
    void ShowPlayer1Selection(int option)
    {
        if (player1SelectedImage != null && optionSprites != null && option < optionSprites.Length)
        {
            player1SelectedImage.sprite = optionSprites[option];
            player1SelectedImage.gameObject.SetActive(true);
        }
    }
    
    void HidePlayer1Selection()
    {
        if (player1SelectedImage != null)
            player1SelectedImage.gameObject.SetActive(false);
    }

    IEnumerator FadeIn()
    {
        float timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / 0.5f);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
    }

    private int GetFirstAvailableIndex()
    {
        for (int i = 0; i < buttonAvailable.Length; i++)
            if (buttonAvailable[i]) return i;
        return 0;
    }

    private int GetNextAvailable(int direction)
    {
        int newIndex = currentIndex;
        int attempts = 0;
        do
        {
            newIndex = (newIndex + direction + buttonMainImages.Length) % buttonMainImages.Length;
            attempts++;
            if (attempts > buttonMainImages.Length) return currentIndex;
        }
        while (!buttonAvailable[newIndex]);
        return newIndex;
    }

    private void Move(int direction)
    {
        if (hasSelected) return;
        if (Time.unscaledTime - lastMoveTime < moveCooldown) return;
        
        int newIndex = GetNextAvailable(direction);
        if (newIndex != currentIndex)
        {
            lastMoveTime = Time.unscaledTime;
            currentIndex = newIndex;
            
            // Sync with external MenuSelector
            if (menuSelector != null && menuSelector.IsAvailableForSelection())
            {
                menuSelector.SetCurrentIndex(currentIndex);
            }
            
            if (audioSource != null && switchSound != null)
                audioSource.PlayOneShot(switchSound);
            
            if (selectionText != null && selectionTexts.Length > currentIndex)
                selectionText.text = selectionTexts[currentIndex];
        }
    }

    private void SelectCurrent()
    {
        if (hasSelected) return;
        
        if (!buttonAvailable[currentIndex])
        {
            if (audioSource != null && errorSound != null)
                audioSource.PlayOneShot(errorSound);
            StartCoroutine(ShakeButton(currentIndex));
            return;
        }
        
        if (audioSource != null && selectionSound != null)
            audioSource.PlayOneShot(selectionSound);
        
        StartCoroutine(HandleSelection(currentIndex));
    }
    
    IEnumerator HandleSelection(int index)
    {
        hasSelected = true;
        
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        StartCoroutine(FlashButton(index));
        yield return new WaitForSecondsRealtime(0.2f);
        
        if (index == BUTTON_ARROW_KEYS)
        {
            AssignArrowKeys();
            yield return new WaitForSecondsRealtime(0.1f);
            OnPlayerFinished();
        }
        else if (index == BUTTON_WASD_KEYS)
        {
            AssignWASDKeys();
            yield return new WaitForSecondsRealtime(0.1f);
            OnPlayerFinished();
        }
        else if (index == BUTTON_CALIBRATION)
        {
            OpenCalibration();
        }
    }
    
    void AssignArrowKeys()
    {
        PlayerInput targetInput = (currentPlayerIndex == 0) ? player1Input : player2Input;
        if (targetInput == null) return;
        
        var actions = targetInput.actions;
        actions["Up"].ApplyBindingOverride("<Keyboard>/upArrow");
        actions["Down"].ApplyBindingOverride("<Keyboard>/downArrow");
        actions["Left"].ApplyBindingOverride("<Keyboard>/leftArrow");
        actions["Right"].ApplyBindingOverride("<Keyboard>/rightArrow");
        
        string overridesJson = actions.SaveBindingOverridesAsJson();
        if (currentPlayerIndex == 0)
        {
            SessionConfig.P1Bindings = overridesJson;
            SessionConfig.Player1Device = Keyboard.current;
            SessionConfig.P1DeviceType = "Keyboard-ArrowKeys";
        }
        else
        {
            SessionConfig.P2Bindings = overridesJson;
            SessionConfig.Player2Device = Keyboard.current;
            SessionConfig.P2DeviceType = "Keyboard-ArrowKeys";
        }
        
        Debug.Log($"Assigned Arrow Keys to Player {currentPlayerIndex + 1}");
    }
    
    void AssignWASDKeys()
    {
        PlayerInput targetInput = (currentPlayerIndex == 0) ? player1Input : player2Input;
        if (targetInput == null) return;
        
        var actions = targetInput.actions;
        actions["Up"].ApplyBindingOverride("<Keyboard>/w");
        actions["Down"].ApplyBindingOverride("<Keyboard>/s");
        actions["Left"].ApplyBindingOverride("<Keyboard>/a");
        actions["Right"].ApplyBindingOverride("<Keyboard>/d");
        
        string overridesJson = actions.SaveBindingOverridesAsJson();
        if (currentPlayerIndex == 0)
        {
            SessionConfig.P1Bindings = overridesJson;
            SessionConfig.Player1Device = Keyboard.current;
            SessionConfig.P1DeviceType = "Keyboard-WASD";
        }
        else
        {
            SessionConfig.P2Bindings = overridesJson;
            SessionConfig.Player2Device = Keyboard.current;
            SessionConfig.P2DeviceType = "Keyboard-WASD";
        }
        
        Debug.Log($"Assigned WASD Keys to Player {currentPlayerIndex + 1}");
    }
    
    void OpenCalibration()
    {
        gameObject.SetActive(false);
        if (calibrationCanvas != null)
        {
            var setupMenu = calibrationCanvas.GetComponent<DeviceSetupMenu>();
            if (setupMenu != null)
            {
                setupMenu.playerIndexToAssign = currentPlayerIndex;
                setupMenu.gameSceneName = songSelectionScene;
                
                // For 2-player mode, after calibration go to Player 2 menu
                if (SessionConfig.PlayerCount == 2 && currentPlayerIndex == 0)
                {
                    setupMenu.nextMenuForPlayer2 = deviceSelectionMenuP2;
                }
                setupMenu.ResetCalibration();
            }
            calibrationCanvas.SetActive(true);
        }
    }
    
    void OnPlayerFinished()
    {
        // Check if 2-player mode and Player 1 just finished
        if (SessionConfig.PlayerCount == 2 && currentPlayerIndex == 0)
        {
            // Move to Player 2
            gameObject.SetActive(false);
            if (deviceSelectionMenuP2 != null)
            {
                var p2Menu = deviceSelectionMenuP2.GetComponent<DeviceSelectionMenu>();
                if (p2Menu != null)
                {
                    p2Menu.currentPlayerIndex = 1;
                    p2Menu.ResetMenu();
                }
                deviceSelectionMenuP2.SetActive(true);
            }
        }
        else
        {
            // Single player or Player 2 finished - load song selection
            LoadSongSelection();
        }
    }
    
    void LoadSongSelection()
    {
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadScene(songSelectionScene);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(songSelectionScene);
    }

    IEnumerator FlashButton(int index)
    {
        Color originalColor = buttonMainImages[index] != null ? buttonMainImages[index].color : Color.white;
        
        if (buttonMainImages[index] != null)
            buttonMainImages[index].color = Color.yellow;
        if (buttonTextImages[index] != null)
            buttonTextImages[index].color = Color.yellow;
        
        yield return new WaitForSecondsRealtime(0.1f);
        
        if (buttonMainImages[index] != null)
            buttonMainImages[index].color = originalColor;
        if (buttonTextImages[index] != null)
            buttonTextImages[index].color = originalColor;
    }

    IEnumerator ShakeButton(int index)
    {
        if (buttonMainImages[index] == null) yield break;
        
        Vector3 originalPos = buttonMainImages[index].transform.localPosition;
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float offsetX = Random.Range(-5f, 5f);
            buttonMainImages[index].transform.localPosition = new Vector3(originalPos.x + offsetX, originalPos.y, originalPos.z);
            yield return null;
        }
        buttonMainImages[index].transform.localPosition = originalPos;
    }
}