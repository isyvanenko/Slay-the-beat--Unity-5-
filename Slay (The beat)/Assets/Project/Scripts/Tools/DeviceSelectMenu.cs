using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls the player device selection and dance mat calibration menu.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class DeviceSetupMenu : MonoBehaviour
{
    [Header("Custom Animations")]
    /// <summary>Object containing the transition animator used before player two setup.</summary>
    public GameObject transforplayer2obj;
    private Animator transforplayer2;

    [Header("Arrow GameObjects (Activated by direction)")]
    /// <summary>Arrow object shown while calibrating the upward direction.</summary>
    public GameObject upArrowSprite;
    /// <summary>Arrow object shown while calibrating the downward direction.</summary>
    public GameObject downArrowSprite;
    /// <summary>Arrow object shown while calibrating the left direction.</summary>
    public GameObject leftArrowSprite;
    /// <summary>Arrow object shown while calibrating the right direction.</summary>
    public GameObject rightArrowSprite;
    
    [Header("Arrow Alpha Pulse Settings")]
    /// <summary>Minimum alpha used during arrow pulse animation.</summary>
    public float pulseAlphaMin = 0.1f;
    /// <summary>Maximum alpha used during arrow pulse animation.</summary>
    public float pulseAlphaMax = 0.3f;
    /// <summary>Speed multiplier for the arrow alpha pulse.</summary>
    public float pulseAlphaSpeed = 8f;
    /// <summary>Color used while the player is successfully holding a calibration input.</summary>
    public Color arrowGoldColor = new Color(1f, 0.84f, 0f, 1f);
    /// <summary>Color used when a calibration arrow has completed successfully.</summary>
    public Color arrowCompleteWhite = Color.white;

    [Header("Configuration")]
    /// <summary>Zero-based player index whose device and bindings should be assigned.</summary>
    public int playerIndexToAssign = 0;
    /// <summary>Minimum delay before another input can be accepted after a transition.</summary>
    public float confirmationDelay = 0.5f;

    [Header("UI Controls")]
    /// <summary>Button that resets the current calibration process.</summary>
    public Button uiResetButton; 
    /// <summary>Main prompt text used to guide the player through setup.</summary>
    public TextMeshProUGUI promptText; 
    
    [Header("Player Count Text Objects")]
    /// <summary>Text label that displays two-player mode status.</summary>
    public TextMeshProUGUI playerCountText;
    /// <summary>Text label that displays player calibration confirmation status.</summary>
    public TextMeshProUGUI calibrationStatusText;

    [Header("Hardware Controls")]
    /// <summary>Input action used for hardware reset controls.</summary>
    public InputActionReference resetActionReference;

    [Header("ESC Hold to Return")]
    /// <summary>Time in seconds Escape must be held to return to the main menu.</summary>
    public float escHoldRequiredTime = 3.0f;
    /// <summary>Progress slider shown while Escape is held.</summary>
    public Slider escHoldProgressSlider;
    /// <summary>Main menu CanvasGroup to fade in when returning from setup.</summary>
    public CanvasGroup mainMenuCanvas;

    [Header("Animation & Timing")]
    /// <summary>PlayerInput asset whose action bindings receive calibration overrides.</summary>
    public PlayerInput playerInputToMap; 
    /// <summary>Time in seconds each calibration input must be held.</summary>
    public float requiredHoldTime = 3.0f; 
    /// <summary>Grace period in seconds before a released input counts as a slip.</summary>
    public float slipForgivenessTime = 0.35f; 

    [Header("Visual Tuning")]
    /// <summary>Delay before the calibration intro elements begin appearing.</summary>
    public float introWaitDuration = 0.5f;   
    /// <summary>Duration for fading in each intro calibration element.</summary>
    public float introFadeDuration = 0.3f;   
    /// <summary>Speed multiplier for calibration target pulsing.</summary>
    public float pulseSpeed = 8f;            
    /// <summary>Scale multiplier applied at the peak of target pulsing.</summary>
    public float pulseScaleMultiplier = 1.15f; 
    
    [Header("Error Shake Settings")]
    /// <summary>Duration of the failed-hold shake animation.</summary>
    public float shakeDuration = 0.3f;
    /// <summary>Horizontal displacement range used during the failed-hold shake.</summary>
    public float shakeIntensity = 15f; 

    [Header("UI State Colors")]
    /// <summary>Default color for unconfirmed calibration backgrounds.</summary>
    public Color normalBlackColor = Color.black; 
    /// <summary>Color for locked or completed calibration backgrounds.</summary>
    public Color lockedWhiteColor = Color.white;
    /// <summary>Flash color used when a calibration input locks successfully.</summary>
    public Color flashGoldColor = new Color(1f, 0.8f, 0f); 
    /// <summary>Error color used when the player releases an input too long.</summary>
    public Color errorRedColor = new Color(1f, 0.2f, 0.2f); 
    /// <summary>Text color used when a player calibration is confirmed.</summary>
    public Color confirmedGreenColor = new Color(0.2f, 0.8f, 0.2f);

    [Header("3D Emission Settings")]
    /// <summary>Emission color applied to 3D targets during successful holds.</summary>
    [ColorUsage(true, true)] public Color goldEmissionColor = new Color(1f, 0.8f, 0f, 1f) * 2f; 
    /// <summary>Emission color applied to 3D targets during error feedback.</summary>
    [ColorUsage(true, true)] public Color redEmissionColor = new Color(1f, 0.2f, 0.2f, 1f) * 3f;

    /// <summary>
    /// Stores the UI and 3D objects that represent one calibration direction.
    /// </summary>
    [System.Serializable]
    public class CalibrationAnimsGroup
    {
        /// <summary>Name of the Input System action that this step binds.</summary>
        public string actionName; 
        /// <summary>Direction keyword that chooses the matching arrow object.</summary>
        public string direction; // "up", "down", "left", "right"
        
        [Header("2D UI Elements")]
        /// <summary>Background graphic that changes color during calibration.</summary>
        public Graphic backgroundBaseImage; 
        /// <summary>Arrow icon graphic that fades as the input is held.</summary>
        public Graphic arrowIconImage;      
        /// <summary>Lock icon graphic shown after successful calibration.</summary>
        public Graphic lockIconImage;       
        /// <summary>Container CanvasGroup used for fade and pulse animations.</summary>
        public CanvasGroup containerToPulse; 
        
        [Header("3D Object")]
        /// <summary>Optional 3D model that receives emission feedback for this step.</summary>
        public Renderer target3DModel;
        /// <summary>Instanced material cached from the target renderer.</summary>
        [HideInInspector] public Material matInstance;
        /// <summary>Original emission color restored when this step resets.</summary>
        [HideInInspector] public Color originalEmissionColor;
        
        /// <summary>Original local scale restored after pulsing finishes.</summary>
        [HideInInspector] public Vector3 originalScale; 
        /// <summary>Original local position restored after shake feedback finishes.</summary>
        [HideInInspector] public Vector3 originalPosition; 
    }

    /// <summary>Ordered list of calibration actions and their matching visuals.</summary>
    public List<CalibrationAnimsGroup> calibrationAnimations;

    [Header("Audio Setup")]
    /// <summary>Audio source used for one-shot menu sound effects.</summary>
    public AudioSource globalAudioSource;   
    /// <summary>Audio source used for looping hold feedback.</summary>
    public AudioSource loopingAudioSource;  
    
    [Space(10)]
    /// <summary>Sound played when prompting the next calibration step.</summary>
    public AudioClip stepPromptSfx;         
    /// <summary>Looping sound played while an input is held.</summary>
    public AudioClip continuousHoldSfx;     
    /// <summary>Sound played when a calibration input is accepted.</summary>
    public AudioClip controllerSelectedSfx; 
    /// <summary>Sound played when a dance mat or non-keyboard device is selected.</summary>
    public AudioClip dancematSelectedSfx;   
    /// <summary>Sound played after the full calibration sequence completes.</summary>
    public AudioClip selectionConfirmedSfx; 
    /// <summary>Sound played when calibration is reset.</summary>
    public AudioClip resetSfx;              
    /// <summary>Sound played when the hold verification fails.</summary>
    public AudioClip errorBuzzSfx;          

    [Header("Transitions & Ducking")]
    /// <summary>Background music volume while calibration feedback is active.</summary>
    public float duckedMusicVolume = 0.2f;   
    /// <summary>Duration for lowering background music volume.</summary>
    public float duckDropSpeed = 0.15f;      
    /// <summary>Duration for restoring background music volume.</summary>
    public float duckRestoreSpeed = 0.5f;    
    /// <summary>Duration used by menu fade transitions.</summary>
    public float fadeDuration = 0.5f;
    /// <summary>Default speed for CanvasGroup fade helpers.</summary>
    public float subFadeSpeed = 8f;
    /// <summary>Speed used when fading the Escape hold slider.</summary>
    public float sliderFadeSpeed = 5f;
    /// <summary>Name of the gameplay scene to load after calibration.</summary>
    public string gameSceneName = "GameScene";
    /// <summary>Optional setup menu activated for player two before loading the game scene.</summary>
    public GameObject nextMenuForPlayer2; 

    // Internal State
    private CanvasGroup rootCanvasGroup_Internal; 
    private InputAction joinAction;
    private float lastInteractionTime;
    private bool isTransitioning = false;
    private bool isMapping = false;
    private bool isWaitingForCalibrationInput = false;
    private int currentMapStep = 0;
    private InputDevice myLockedDevice; 
    private Coroutine pulseRoutine;
    private bool isCurrentlyPulsing = false;
    private Vector3 promptOriginalPos;
    
    // Arrow alpha pulsing
    private Coroutine arrowPulseRoutine;
    private GameObject currentActiveArrow;

    // ESC hold state
    private bool isEscHeld = false;
    private float escHoldTimer = 0f;
    private bool wasEscPressedLastFrame = false;
    private Coroutine escSliderFadeRoutine;

    // Music Ducking State
    private AudioSource bgmSource;
    private float originalBgmVolume = 1f;
    private Coroutine duckingRoutine;

    /// <summary>
    /// Caches component references and stores the initial visual state for calibration elements.
    /// </summary>
    private void Awake()
    {
        rootCanvasGroup_Internal = GetComponent<CanvasGroup>();
        if (promptText != null) promptOriginalPos = promptText.transform.localPosition;

        foreach (var animGroup in calibrationAnimations)
        {
            if (animGroup.containerToPulse != null)
            {
                animGroup.originalScale = animGroup.containerToPulse.transform.localScale;
                animGroup.originalPosition = animGroup.containerToPulse.transform.localPosition;
            }

            if (animGroup.target3DModel != null)
            {
                animGroup.matInstance = animGroup.target3DModel.material; 
                animGroup.matInstance.EnableKeyword("_EMISSION"); 
                animGroup.originalEmissionColor = animGroup.matInstance.GetColor("_EmissionColor"); 
            }
        }
        
        // Deactivate all arrow sprites initially
        DeactivateAllArrows();
        
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

    /// <summary>
    /// Initializes optional animation references and refreshes player status labels.
    /// </summary>
    private void Start() 
    {
        if (transforplayer2obj != null) transforplayer2 = transforplayer2obj.GetComponent<Animator>();
        
        UpdatePlayerCountText();
        UpdateCalibrationStatusText(false);
    }

    /// <summary>
    /// Polls per-frame escape input for reset and return-to-menu behavior.
    /// </summary>
    private void Update()
    {
        // Handle ESC press and hold
        HandleEscInput();
    }

    /// <summary>
    /// Handles short and held Escape key input while the menu is active.
    /// </summary>
    private void HandleEscInput()
    {
        if (isTransitioning) return;
        
        bool escPressed = false;
        
        // Check keyboard ESC
        if (Keyboard.current != null && Keyboard.current.escapeKey.isPressed)
        {
            escPressed = true;
        }
        
        // Check if ESC was just pressed this frame (single press)
        bool escJustPressed = escPressed && !wasEscPressedLastFrame;
        
        if (escJustPressed)
        {
            // Single press - reset calibration if we're mapping
            if (isMapping)
            {
                OnEscPressed();
            }
        }
        
        if (escPressed)
        {
            if (!isEscHeld)
            {
                // Started holding ESC
                isEscHeld = true;
                escHoldTimer = 0f;
            }
            
            escHoldTimer += Time.unscaledDeltaTime;
            
            // Show slider after a short delay to distinguish from single press
            if (escHoldTimer > 0.3f && escHoldProgressSlider != null)
            {
                CanvasGroup sliderCG = escHoldProgressSlider.GetComponent<CanvasGroup>();
                if (sliderCG != null && sliderCG.alpha < 0.5f)
                {
                    ShowEscSlider();
                }
            }
            
            // Update slider
            if (escHoldProgressSlider != null && escHoldTimer > 0.3f)
            {
                escHoldProgressSlider.value = Mathf.Clamp01((escHoldTimer - 0.3f) / (escHoldRequiredTime - 0.3f));
            }
            
            // Check if held long enough
            if (escHoldTimer >= escHoldRequiredTime)
            {
                ReturnToMainMenu();
            }
        }
        else
        {
            if (isEscHeld)
            {
                // Released ESC before completing
                isEscHeld = false;
                escHoldTimer = 0f;
                HideEscSlider();
            }
        }
        
        wasEscPressedLastFrame = escPressed;
    }

    /// <summary>
    /// Resets the current calibration when Escape is tapped during mapping.
    /// </summary>
    private void OnEscPressed()
    {
        // Single ESC press - reset calibration if mapping
        if (isMapping)
        {
            ResetCalibration();
        }
    }

    /// <summary>
    /// Fades in the Escape hold progress slider.
    /// </summary>
    private void ShowEscSlider()
    {
        if (escHoldProgressSlider == null) return;
        
        if (escSliderFadeRoutine != null) StopCoroutine(escSliderFadeRoutine);
        escSliderFadeRoutine = StartCoroutine(FadeSliderAlpha(escHoldProgressSlider, 1f));
    }

    /// <summary>
    /// Fades out the Escape hold progress slider.
    /// </summary>
    private void HideEscSlider()
    {
        if (escHoldProgressSlider == null) return;
        
        if (escSliderFadeRoutine != null) StopCoroutine(escSliderFadeRoutine);
        escSliderFadeRoutine = StartCoroutine(FadeSliderAlpha(escHoldProgressSlider, 0f));
    }

    /// <summary>
    /// Animates a slider CanvasGroup alpha to the requested value.
    /// </summary>
    /// <param name="slider">Slider whose CanvasGroup should be faded.</param>
    /// <param name="targetAlpha">Alpha value to fade toward.</param>
    /// <returns>Coroutine enumerator for the fade animation.</returns>
    IEnumerator FadeSliderAlpha(Slider slider, float targetAlpha)
    {
        CanvasGroup sliderCG = slider.GetComponent<CanvasGroup>();
        if (sliderCG == null) yield break;
        
        float startAlpha = sliderCG.alpha;
        float timer = 0f;
        float duration = 1f / sliderFadeSpeed;
        
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            sliderCG.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }
        
        sliderCG.alpha = targetAlpha;
        
        // Reset slider value if hiding
        if (targetAlpha <= 0.01f)
        {
            slider.value = 0f;
        }
    }

    /// <summary>
    /// Starts the transition back to the main menu after Escape is held long enough.
    /// </summary>
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

    /// <summary>
    /// Fades out this menu, resets active calibration effects, and fades in the main menu.
    /// </summary>
    /// <returns>Coroutine enumerator for the menu transition.</returns>
    IEnumerator ReturnToMainMenuRoutine()
    {
        isTransitioning = true;
        
        // Reset ESC state
        isEscHeld = false;
        escHoldTimer = 0f;
        wasEscPressedLastFrame = false;
        
        // Stop everything
        StopPulse();
        StopArrowPulse();
        DeactivateAllArrows();
        
        if (loopingAudioSource != null) loopingAudioSource.Stop();
        SetMusicDucked(false, duckRestoreSpeed);
        
        if (globalAudioSource != null && resetSfx != null)
            globalAudioSource.PlayOneShot(resetSfx);
        
        // Fade out current menu
        float timer = 0f;
        float startAlpha = (rootCanvasGroup_Internal != null) ? rootCanvasGroup_Internal.alpha : 1f;
        
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            if (rootCanvasGroup_Internal) rootCanvasGroup_Internal.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
            yield return null;
        }
        
        if (rootCanvasGroup_Internal) rootCanvasGroup_Internal.alpha = 0f;
        
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
        
        isTransitioning = false;
    }

    /// <summary>
    /// Disables every directional arrow object.
    /// </summary>
    private void DeactivateAllArrows()
    {
        if (upArrowSprite != null) upArrowSprite.SetActive(false);
        if (downArrowSprite != null) downArrowSprite.SetActive(false);
        if (leftArrowSprite != null) leftArrowSprite.SetActive(false);
        if (rightArrowSprite != null) rightArrowSprite.SetActive(false);
    }

    /// <summary>
    /// Gets the arrow GameObject assigned to a calibration direction.
    /// </summary>
    /// <param name="direction">Direction name: up, down, left, or right.</param>
    /// <returns>The matching arrow object, or null when the direction is unknown.</returns>
    private GameObject GetArrowForDirection(string direction)
    {
        switch (direction.ToLower())
        {
            case "up":
                return upArrowSprite;
            case "down":
                return downArrowSprite;
            case "left":
                return leftArrowSprite;
            case "right":
                return rightArrowSprite;
            default:
                return null;
        }
    }

    /// <summary>
    /// Shows the arrow for a direction and prepares it for alpha pulsing.
    /// </summary>
    /// <param name="direction">Direction whose arrow should be activated.</param>
    private void ActivateArrowForDirection(string direction)
    {
        DeactivateAllArrows();
        currentActiveArrow = GetArrowForDirection(direction);
        if (currentActiveArrow != null)
        {
            currentActiveArrow.SetActive(true);
            SetArrowAlpha(currentActiveArrow, pulseAlphaMin);
        }
    }

    /// <summary>
    /// Sets the alpha channel on an arrow rendered by either SpriteRenderer or UI Image.
    /// </summary>
    /// <param name="arrow">Arrow object to update.</param>
    /// <param name="alpha">New alpha value.</param>
    private void SetArrowAlpha(GameObject arrow, float alpha)
    {
        if (arrow == null) return;
        
        SpriteRenderer spriteRenderer = arrow.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
        
        Image image = arrow.GetComponent<Image>();
        if (image != null)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }

    /// <summary>
    /// Sets the full color on an arrow rendered by either SpriteRenderer or UI Image.
    /// </summary>
    /// <param name="arrow">Arrow object to update.</param>
    /// <param name="color">New arrow color.</param>
    private void SetArrowColor(GameObject arrow, Color color)
    {
        if (arrow == null) return;
        
        SpriteRenderer spriteRenderer = arrow.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
        
        Image image = arrow.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }
    }

    /// <summary>
    /// Updates the UI label that indicates whether two-player mode is active.
    /// </summary>
    private void UpdatePlayerCountText()
    {
        if (playerCountText == null) return;
        
        if (SessionConfig.PlayerCount == 2)
        {
            playerCountText.text = "2 PLAYER";
        }
        else
        {
            playerCountText.text = "";
        }
    }
    
    /// <summary>
    /// Updates the calibration confirmation label for the currently assigned player.
    /// </summary>
    /// <param name="isConfirmed">True when calibration has completed successfully.</param>
    private void UpdateCalibrationStatusText(bool isConfirmed)
    {
        if (calibrationStatusText == null) return;
        
        if (isConfirmed)
        {
            if (playerIndexToAssign == 0)
            {
                calibrationStatusText.text = "PLAYER 1 CONFIRMED";
            }
            else
            {
                calibrationStatusText.text = "PLAYER 2 CONFIRMED";
            }
            calibrationStatusText.color = confirmedGreenColor;
        }
        else
        {
            calibrationStatusText.text = "";
        }
    }

    /// <summary>
    /// Subscribes input handlers, resets menu state, and starts the menu fade-in.
    /// </summary>
    private void OnEnable()
    {
        if (rootCanvasGroup_Internal) rootCanvasGroup_Internal.alpha = 0f;
        if (uiResetButton != null) uiResetButton.onClick.AddListener(ResetCalibration);

        if (resetActionReference != null)
        {
            resetActionReference.action.Enable();
            resetActionReference.action.performed += OnHardwareResetPressed;
        }

        isCurrentlyPulsing = false;
        isMapping = false;
        isWaitingForCalibrationInput = false;
        lastInteractionTime = 0f;
        isTransitioning = false;
        isEscHeld = false;
        escHoldTimer = 0f;
        wasEscPressedLastFrame = false;
        
        GrabBGMReference();
        ResetAllVisuals(); 

        joinAction = new InputAction(binding: "/*/<button>", type: InputActionType.PassThrough);
        joinAction.performed += OnInputDetected;
        joinAction.Enable();

        if (rootCanvasGroup_Internal) StartCoroutine(FadeIn(rootCanvasGroup_Internal, 1f));
        
        UpdateCalibrationStatusText(false);
    }

    /// <summary>
    /// Unsubscribes input handlers and stops active audio, pulsing, and hold state.
    /// </summary>
    private void OnDisable()
    {
        if (joinAction != null)
        {
            joinAction.performed -= OnInputDetected;
            joinAction.Disable();
        }
        
        if (uiResetButton != null) uiResetButton.onClick.RemoveListener(ResetCalibration);

        if (resetActionReference != null)
        {
            resetActionReference.action.performed -= OnHardwareResetPressed;
            resetActionReference.action.Disable();
        }

        StopPulse();
        StopArrowPulse();
        if (loopingAudioSource != null) loopingAudioSource.Stop();
        SetMusicDucked(false, duckRestoreSpeed);
        
        isEscHeld = false;
        escHoldTimer = 0f;
        wasEscPressedLastFrame = false;
    }

    /// <summary>
    /// Finds the music manager audio source and records its original volume.
    /// </summary>
    private void GrabBGMReference()
    {
        if (bgmSource == null && MusicManager.Instance != null)
        {
            bgmSource = MusicManager.Instance.GetComponent<AudioSource>();
            if (bgmSource != null) originalBgmVolume = bgmSource.volume;
        }
    }

    /// <summary>
    /// Fades background music between normal and ducked volume.
    /// </summary>
    /// <param name="isDucked">True to lower the music volume; false to restore it.</param>
    /// <param name="duration">Fade duration in seconds.</param>
    private void SetMusicDucked(bool isDucked, float duration)
    {
        if (bgmSource == null) return;

        if (!gameObject.activeInHierarchy)
        {
            bgmSource.volume = isDucked ? duckedMusicVolume : originalBgmVolume;
            return;
        }

        if (duckingRoutine != null) StopCoroutine(duckingRoutine);

        float targetVol = isDucked ? duckedMusicVolume : originalBgmVolume;
        duckingRoutine = StartCoroutine(FadeMusicVolume(bgmSource, bgmSource.volume, targetVol, duration));
    }

    /// <summary>
    /// Handles reset input from the configured hardware reset action.
    /// </summary>
    /// <param name="ctx">Input callback context for the reset action.</param>
    private void OnHardwareResetPressed(InputAction.CallbackContext ctx)
    {
        // This is for the reset action reference in inspector
        // Don't double-handle ESC if it's bound to reset action
        if (isTransitioning) return;
        if (isMapping && myLockedDevice != null && ctx.control.device != myLockedDevice) return;
        ResetCalibration();
    }

    /// <summary>
    /// Clears the current calibration, restores visuals, and returns to the initial prompt.
    /// </summary>
    public void ResetCalibration()
    {
        if (isTransitioning) return; 

        StopAllCoroutines(); 
        StopPulse();
        StopArrowPulse();
        DeactivateAllArrows();

        if (loopingAudioSource != null) loopingAudioSource.Stop();
        SetMusicDucked(false, duckRestoreSpeed);

        if (globalAudioSource != null && resetSfx != null)
            globalAudioSource.PlayOneShot(resetSfx);

        isMapping = false;
        isWaitingForCalibrationInput = false;
        currentMapStep = 0;
        myLockedDevice = null;
        
        // Reset ESC hold state
        isEscHeld = false;
        escHoldTimer = 0f;
        wasEscPressedLastFrame = false;

        if (playerInputToMap != null)
        {
            playerInputToMap.actions.Disable();
            playerInputToMap.actions.RemoveAllBindingOverrides();
        }

        ResetAllVisuals();
        
        if (rootCanvasGroup_Internal != null) rootCanvasGroup_Internal.alpha = 1f;
        if (promptText != null) promptText.text = "PRESS ANY BUTTON TO START";
        if (joinAction != null && !joinAction.enabled) joinAction.Enable();
        
        UpdateCalibrationStatusText(false);
        
        // Hide ESC slider
        HideEscSlider();
    }

    /// <summary>
    /// Restores all calibration UI, arrows, and emission colors to their idle state.
    /// </summary>
    private void ResetAllVisuals()
    {
        if (promptText != null) 
        {
            promptText.text = "PRESS ANY BUTTON TO START";
            promptText.transform.localPosition = promptOriginalPos;
        }

        DeactivateAllArrows();

        foreach (var animGroup in calibrationAnimations)
        {
            if (animGroup.containerToPulse != null)
            {
                animGroup.containerToPulse.alpha = 0f; 
                if (animGroup.originalScale != Vector3.zero)
                    animGroup.containerToPulse.transform.localScale = animGroup.originalScale; 
                animGroup.containerToPulse.transform.localPosition = animGroup.originalPosition;
            }
            
            if (animGroup.backgroundBaseImage != null) 
                animGroup.backgroundBaseImage.color = normalBlackColor;
            
            if (animGroup.arrowIconImage != null)
            {
                Color c = animGroup.arrowIconImage.color;
                c.a = 1f;
                animGroup.arrowIconImage.color = c;
            }
            
            if (animGroup.lockIconImage != null)
            {
                Color c = animGroup.lockIconImage.color;
                c.a = 0f;
                animGroup.lockIconImage.color = c;
            }

            if (animGroup.matInstance != null)
                animGroup.matInstance.SetColor("_EmissionColor", animGroup.originalEmissionColor);
        }
    }

    /// <summary>
    /// Detects the device to map and captures calibration inputs from that locked device.
    /// </summary>
    /// <param name="ctx">Input callback context containing the actuated control.</param>
    private void OnInputDetected(InputAction.CallbackContext ctx)
    {
        if (isTransitioning) return;
        if (Time.unscaledTime < lastInteractionTime + confirmationDelay) return;

        InputControl control = ctx.control;

        if (control.device is Mouse || control.device is Pointer) return;
        if (control.name == "anyKey" || control.path.Contains("anyKey")) return;

        // Don't process ESC in join action (handled by Update)
        if (control is KeyControl keyControl)
        {
            if (keyControl.keyCode == Key.Escape) return;
            if (keyControl.keyCode == Key.L)
            {
                if (isMapping) ResetCalibration();
                return; 
            }
            if (keyControl.keyCode == Key.P) return;
        }
        
        bool isPressed = false;
        if (control is ButtonControl btn) isPressed = btn.isPressed;
        else isPressed = control.IsActuated(0.1f);
        
        if (!isPressed) return; 

        InputDevice inputDev = control.device;

        if (!isMapping)
        {
            if (resetActionReference != null && ctx.action.name == resetActionReference.action.name) return;

            bool isKeyboard = inputDev is Keyboard || inputDev.name.ToLower().Contains("keyboard");
            if (!isKeyboard)
            {
                if (playerIndexToAssign == 1 && SessionConfig.Player1Device == inputDev) return;
                if (playerIndexToAssign == 0 && SessionConfig.Player2Device == inputDev) return;
            }

            myLockedDevice = inputDev;
            StartMappingSequence(inputDev);
        }
        else if (isWaitingForCalibrationInput)
        {
            if (inputDev != myLockedDevice) return; 
            
            if (resetActionReference != null && ctx.action.name == resetActionReference.action.name) return;

            isWaitingForCalibrationInput = false; 
            
            CalibrationAnimsGroup currentGroup = calibrationAnimations[currentMapStep];
            StartCoroutine(VerifyHoldAndCrossFadeRoutine(currentGroup, control));
        }
    }

    /// <summary>
    /// Locks a device to the selected player and begins the calibration sequence.
    /// </summary>
    /// <param name="device">Input device being assigned to the player.</param>
    private void StartMappingSequence(InputDevice device)
    {
        isMapping = true;
        SessionConfig.SetPlayerDevice(playerIndexToAssign, device, "Custom");

        if (globalAudioSource != null && dancematSelectedSfx != null)
            globalAudioSource.PlayOneShot(dancematSelectedSfx);

        if (playerInputToMap != null)
        {
            playerInputToMap.actions.Disable();
            playerInputToMap.actions.RemoveAllBindingOverrides(); 
        }

        currentMapStep = 0;
        StartCoroutine(IntroPopUpAndInitialize());
    }

    /// <summary>
    /// Displays the calibration intro and fades in each calibration target.
    /// </summary>
    /// <returns>Coroutine enumerator for the intro animation.</returns>
    IEnumerator IntroPopUpAndInitialize()
    {
        if (promptText != null) promptText.text = "CALIBRATION";
        yield return new WaitForSecondsRealtime(introWaitDuration);

        foreach (var animGroup in calibrationAnimations)
        {
            if (animGroup.containerToPulse != null) 
                StartCoroutine(FadeIn(animGroup.containerToPulse, 1f, introFadeDuration));
            yield return new WaitForSecondsRealtime(0.1f); 
        }

        yield return new WaitForSecondsRealtime(introFadeDuration);
        currentMapStep = 0;
        SequenceNextAction();
    }

    /// <summary>
    /// Advances to the next calibration action or finishes when all actions are mapped.
    /// </summary>
    private void SequenceNextAction()
    {
        if (currentMapStep >= calibrationAnimations.Count)
        {
            FinishCalibration();
            return;
        }

        CalibrationAnimsGroup currentGroup = calibrationAnimations[currentMapStep];
        
        // Single line prompt
        if (promptText != null) promptText.text = $"<color=yellow>HOLD {currentGroup.actionName.ToUpper()}</color>";
        
        if (globalAudioSource != null && stepPromptSfx != null)
            globalAudioSource.PlayOneShot(stepPromptSfx);

        // Activate the correct arrow and start pulsing
        ActivateArrowForDirection(currentGroup.direction);
        StartPulse(currentGroup);
        isWaitingForCalibrationInput = true; 
    }

    /// <summary>
    /// Verifies that the selected control is held long enough and animates progress feedback.
    /// </summary>
    /// <param name="animGroup">Visual group associated with the current calibration step.</param>
    /// <param name="control">Input control being verified for this action.</param>
    /// <returns>Coroutine enumerator for the hold verification and success animation.</returns>
    IEnumerator VerifyHoldAndCrossFadeRoutine(CalibrationAnimsGroup animGroup, InputControl control)
    {
        float holdTimer = 0f;
        float currentSlipTime = 0f; 
        bool wasStepping = false; 

        while (holdTimer < requiredHoldTime)
        {
            bool isStepping = false;
            if (control is ButtonControl btn) isStepping = btn.isPressed;
            else isStepping = control.IsActuated(0.1f);

            if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
            {
                ResetCalibration();
                yield break;
            }

            if (isStepping)
            {
                if (!wasStepping)
                {
                    if (promptText != null) promptText.text = $"<color=white>KEEP HOLDING...</color>";
                    wasStepping = true;

                    SetMusicDucked(true, duckDropSpeed);
                    if (loopingAudioSource != null && continuousHoldSfx != null)
                    {
                        loopingAudioSource.clip = continuousHoldSfx;
                        loopingAudioSource.Play();
                    }
                }

                currentSlipTime = 0f; 
                holdTimer += Time.unscaledDeltaTime; 
                float progress = holdTimer / requiredHoldTime; 

                // Arrow goes from pulsing alpha to full gold
                if (currentActiveArrow != null)
                {
                    Color targetColor = Color.Lerp(
                        new Color(arrowGoldColor.r, arrowGoldColor.g, arrowGoldColor.b, pulseAlphaMin), 
                        arrowGoldColor, 
                        progress
                    );
                    SetArrowColor(currentActiveArrow, targetColor);
                }

                if (animGroup.backgroundBaseImage != null)
                    animGroup.backgroundBaseImage.color = Color.Lerp(normalBlackColor, lockedWhiteColor, progress);

                if (animGroup.arrowIconImage != null)
                {
                    Color arrowColor = animGroup.arrowIconImage.color;
                    arrowColor.a = Mathf.Lerp(1f, 0f, progress);
                    animGroup.arrowIconImage.color = arrowColor;
                }

                if (animGroup.matInstance != null)
                {
                    Color currentEmission = Color.Lerp(animGroup.originalEmissionColor, goldEmissionColor, progress);
                    animGroup.matInstance.SetColor("_EmissionColor", currentEmission);
                }
            }
            else
            {
                if (wasStepping)
                {
                    wasStepping = false;
                    SetMusicDucked(false, duckRestoreSpeed);
                    if (loopingAudioSource != null) loopingAudioSource.Stop();
                    
                    // Reset arrow to pulsing state
                    StartArrowPulse();
                }

                currentSlipTime += Time.unscaledDeltaTime;

                if (currentSlipTime >= slipForgivenessTime)
                {
                    yield return StartCoroutine(ErrorShakeRoutine(animGroup));
                    SequenceNextAction(); 
                    yield break; 
                }
            }

            yield return null;
        }

        StopPulse(); 
        StopArrowPulse();
        
        if (loopingAudioSource != null) loopingAudioSource.Stop();
        SetMusicDucked(false, duckRestoreSpeed);
        
        if (globalAudioSource != null && controllerSelectedSfx != null) 
            globalAudioSource.PlayOneShot(controllerSelectedSfx);

        if (playerInputToMap != null)
            playerInputToMap.actions[animGroup.actionName].ApplyBindingOverride(control.path);

        if (animGroup.matInstance != null) animGroup.matInstance.SetColor("_EmissionColor", goldEmissionColor);

        // Arrow is gold now, then fade to white
        if (currentActiveArrow != null)
        {
            SetArrowColor(currentActiveArrow, arrowGoldColor);
        }

        if (animGroup.backgroundBaseImage != null) animGroup.backgroundBaseImage.color = lockedWhiteColor;
        if (animGroup.arrowIconImage != null)
        {
            Color c = animGroup.arrowIconImage.color;
            c.a = 0f;
            animGroup.arrowIconImage.color = c;
        }

        if (promptText != null) promptText.text = $"<color=white>LOCKED!</color>";
        
        if (animGroup.backgroundBaseImage != null) animGroup.backgroundBaseImage.color = flashGoldColor;
        yield return new WaitForSecondsRealtime(0.15f); 

        // Fade arrow from gold to white
        if (currentActiveArrow != null)
        {
            StartCoroutine(FadeArrowColor(currentActiveArrow, arrowGoldColor, arrowCompleteWhite, 0.3f));
        }

        Coroutine bgFade = null;
        if (animGroup.backgroundBaseImage != null)
            bgFade = StartCoroutine(FadeGraphicColor(animGroup.backgroundBaseImage, flashGoldColor, lockedWhiteColor, 0.3f));
        
        if (animGroup.lockIconImage != null)
            StartCoroutine(FadeGraphicAlpha(animGroup.lockIconImage, 0f, 1f, 0.3f));
        
        if (bgFade != null) yield return bgFade;
        else yield return new WaitForSecondsRealtime(0.3f);
        
        // Fade arrow from white to transparent
        if (currentActiveArrow != null)
        {
            yield return StartCoroutine(FadeArrowColor(currentActiveArrow, arrowCompleteWhite, new Color(1f, 1f, 1f, 0f), 0.3f));
        }
        
        yield return new WaitForSecondsRealtime(0.3f); 

        currentMapStep++;
        SequenceNextAction(); 
    }

    /// <summary>
    /// Fades an arrow from one color to another.
    /// </summary>
    /// <param name="arrow">Arrow object to fade.</param>
    /// <param name="fromColor">Starting color.</param>
    /// <param name="toColor">Target color.</param>
    /// <param name="duration">Fade duration in seconds.</param>
    /// <returns>Coroutine enumerator for the color fade.</returns>
    IEnumerator FadeArrowColor(GameObject arrow, Color fromColor, Color toColor, float duration)
    {
        if (arrow == null) yield break;
        
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;
            SetArrowColor(arrow, Color.Lerp(fromColor, toColor, t));
            yield return null;
        }
        SetArrowColor(arrow, toColor);
    }

    /// <summary>
    /// Plays the failed-hold feedback animation and restores the current step visuals.
    /// </summary>
    /// <param name="animGroup">Visual group to shake and reset.</param>
    /// <returns>Coroutine enumerator for the error animation.</returns>
    IEnumerator ErrorShakeRoutine(CalibrationAnimsGroup animGroup)
    {
        StopPulse(); 
        StopArrowPulse();
        
        if (globalAudioSource != null && errorBuzzSfx != null) 
            globalAudioSource.PlayOneShot(errorBuzzSfx);

        if (promptText != null) promptText.text = $"<color=red>FOOT SLIPPED!</color>";

        // Flash arrow red briefly
        if (currentActiveArrow != null)
        {
            SetArrowColor(currentActiveArrow, errorRedColor);
        }

        if (animGroup.backgroundBaseImage != null) 
            animGroup.backgroundBaseImage.color = errorRedColor;
            
        if (animGroup.arrowIconImage != null)
        {
            Color arrowColor = animGroup.arrowIconImage.color;
            arrowColor.a = 1f;
            animGroup.arrowIconImage.color = arrowColor;
        }

        if (animGroup.matInstance != null) animGroup.matInstance.SetColor("_EmissionColor", redEmissionColor);

        float timer = 0f;
        Transform target = animGroup.containerToPulse.transform;
        Vector3 startPos = animGroup.originalPosition;

        while (timer < shakeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
            target.localPosition = new Vector3(startPos.x + offsetX, startPos.y, startPos.z);
            
            if (promptText != null) 
                promptText.transform.localPosition = new Vector3(promptOriginalPos.x + offsetX, promptOriginalPos.y, promptOriginalPos.z);
            
            yield return null;
        }

        target.localPosition = startPos;
        if (promptText != null) promptText.transform.localPosition = promptOriginalPos;
        
        // Reset arrow color back to white with pulsing alpha, not staying red
        if (currentActiveArrow != null)
        {
            Color resetColor = Color.white;
            resetColor.a = pulseAlphaMin;
            SetArrowColor(currentActiveArrow, resetColor);
            // Restart the pulsing
            StartArrowPulse();
        }
        
        if (animGroup.matInstance != null)
            StartCoroutine(FadeMaterialEmission(animGroup.matInstance, redEmissionColor, animGroup.originalEmissionColor, 0.2f));

        if (animGroup.backgroundBaseImage != null)
            yield return StartCoroutine(FadeGraphicColor(animGroup.backgroundBaseImage, errorRedColor, normalBlackColor, 0.2f));
    }

    /// <summary>
    /// Saves binding overrides and transitions to the next player menu or game scene.
    /// </summary>
    private void FinishCalibration()
    {
        if (promptText != null) promptText.text = "<color=white>CALIBRATION COMPLETE!</color>";
        
        DeactivateAllArrows();
        
        UpdateCalibrationStatusText(true);
        
        if (playerInputToMap != null)
        {
            string overridesJson = playerInputToMap.actions.SaveBindingOverridesAsJson();
            if (playerIndexToAssign == 0) SessionConfig.P1Bindings = overridesJson;
            else SessionConfig.P2Bindings = overridesJson;
            
            playerInputToMap.actions.Enable(); 
        }

        isTransitioning = true;
        if (globalAudioSource != null && selectionConfirmedSfx != null)
            globalAudioSource.PlayOneShot(selectionConfirmedSfx);

        SetMusicDucked(true, duckDropSpeed);
        StartCoroutine(FadeOutAndSwitch(selectionConfirmedSfx));
    }

    /// <summary>
    /// Starts the pulsing animation for the active calibration target and arrow.
    /// </summary>
    /// <param name="animGroup">Visual group to pulse.</param>
    private void StartPulse(CalibrationAnimsGroup animGroup)
    {
        StopPulse(); 
        pulseRoutine = StartCoroutine(PulseTarget(animGroup));
        StartArrowPulse();
    }

    /// <summary>
    /// Stops calibration target pulsing and restores cached transforms and emission colors.
    /// </summary>
    private void StopPulse()
    {
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        isCurrentlyPulsing = false;
        
        foreach (var animGroup in calibrationAnimations)
        {
            if (animGroup.containerToPulse != null)
            {
                if (animGroup.originalScale != Vector3.zero) 
                    animGroup.containerToPulse.transform.localScale = animGroup.originalScale;
                animGroup.containerToPulse.transform.localPosition = animGroup.originalPosition;
            }
            if (animGroup.matInstance != null)
                animGroup.matInstance.SetColor("_EmissionColor", animGroup.originalEmissionColor);
        }
    }

    /// <summary>
    /// Starts alpha pulsing on the current active arrow.
    /// </summary>
    private void StartArrowPulse()
    {
        StopArrowPulse();
        if (currentActiveArrow != null)
        {
            arrowPulseRoutine = StartCoroutine(PulseArrowAlpha());
        }
    }

    /// <summary>
    /// Stops the active arrow alpha pulse coroutine.
    /// </summary>
    private void StopArrowPulse()
    {
        if (arrowPulseRoutine != null)
        {
            StopCoroutine(arrowPulseRoutine);
            arrowPulseRoutine = null;
        }
    }

    /// <summary>
    /// Continuously pulses the active arrow alpha between configured limits.
    /// </summary>
    /// <returns>Coroutine enumerator for the arrow pulse.</returns>
    IEnumerator PulseArrowAlpha()
    {
        while (currentActiveArrow != null && currentActiveArrow.activeInHierarchy)
        {
            float lerp = (Mathf.Sin(Time.unscaledTime * pulseAlphaSpeed) + 1f) / 2f;
            float alpha = Mathf.Lerp(pulseAlphaMin, pulseAlphaMax, lerp);
            SetArrowAlpha(currentActiveArrow, alpha);
            yield return null;
        }
    }

    /// <summary>
    /// Continuously scales the current calibration target for attention feedback.
    /// </summary>
    /// <param name="animGroup">Visual group whose container should pulse.</param>
    /// <returns>Coroutine enumerator for the target pulse.</returns>
    IEnumerator PulseTarget(CalibrationAnimsGroup animGroup)
    {
        if (animGroup.containerToPulse == null) yield break;
        
        isCurrentlyPulsing = true;
        Transform targetTransform = animGroup.containerToPulse.transform;
        
        Vector3 baseScale = animGroup.originalScale;
        Vector3 targetScale = baseScale * pulseScaleMultiplier; 
        
        while (isCurrentlyPulsing)
        {
            float lerp = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) / 2f;
            targetTransform.localScale = Vector3.Lerp(baseScale, targetScale, lerp);
            yield return null;
        }
        targetTransform.localScale = baseScale; 
    }

    /// <summary>
    /// Fades a UI Graphic from one color to another.
    /// </summary>
    /// <param name="g">Graphic to fade.</param>
    /// <param name="startCol">Starting color.</param>
    /// <param name="endCol">Target color.</param>
    /// <param name="duration">Fade duration in seconds.</param>
    /// <returns>Coroutine enumerator for the color fade.</returns>
    IEnumerator FadeGraphicColor(Graphic g, Color startCol, Color endCol, float duration)
    {
        if (g == null) yield break;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            g.color = Color.Lerp(startCol, endCol, t);
            yield return null;
        }
        g.color = endCol;
    }

    /// <summary>
    /// Fades only the alpha channel of a UI Graphic.
    /// </summary>
    /// <param name="g">Graphic to fade.</param>
    /// <param name="startAlpha">Starting alpha value.</param>
    /// <param name="endAlpha">Target alpha value.</param>
    /// <param name="duration">Fade duration in seconds.</param>
    /// <returns>Coroutine enumerator for the alpha fade.</returns>
    IEnumerator FadeGraphicAlpha(Graphic g, float startAlpha, float endAlpha, float duration)
    {
        if (g == null) yield break;
        float t = 0f;
        Color c = g.color;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            c.a = Mathf.Lerp(startAlpha, endAlpha, t);
            g.color = c;
            yield return null;
        }
        c.a = endAlpha;
        g.color = c;
    }

    /// <summary>
    /// Fades the emission color of a material.
    /// </summary>
    /// <param name="mat">Material whose emission color should change.</param>
    /// <param name="startCol">Starting emission color.</param>
    /// <param name="endCol">Target emission color.</param>
    /// <param name="duration">Fade duration in seconds.</param>
    /// <returns>Coroutine enumerator for the emission fade.</returns>
    IEnumerator FadeMaterialEmission(Material mat, Color startCol, Color endCol, float duration)
    {
        if (mat == null) yield break;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            mat.SetColor("_EmissionColor", Color.Lerp(startCol, endCol, t));
            yield return null;
        }
        mat.SetColor("_EmissionColor", endCol);
    }

    /// <summary>
    /// Sets a CanvasGroup alpha and updates its interaction flags to match visibility.
    /// </summary>
    /// <param name="cg">CanvasGroup to update.</param>
    /// <param name="alpha">Target alpha value.</param>
    private void SetCGAlpha(CanvasGroup cg, float alpha)
    {
        if (cg == null) return;
        cg.alpha = alpha;
        cg.interactable = alpha > 0.1f;
        cg.blocksRaycasts = alpha > 0.1f;
    }

    /// <summary>
    /// Fades a CanvasGroup from transparent to a target alpha.
    /// </summary>
    /// <param name="cg">CanvasGroup to fade.</param>
    /// <param name="targetAlpha">Target alpha value.</param>
    /// <param name="duration">Fade duration in seconds, or automatic duration when zero.</param>
    /// <returns>Coroutine enumerator for the fade.</returns>
    IEnumerator FadeIn(CanvasGroup cg, float targetAlpha, float duration = 0f)
    {
        if (cg == null) yield break;
        if (duration <= 0) duration = 1f / subFadeSpeed;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            cg.alpha = Mathf.Lerp(0f, targetAlpha, t);
            yield return null;
        }
        SetCGAlpha(cg, targetAlpha);
    }

    /// <summary>
    /// Fades an AudioSource volume between two levels.
    /// </summary>
    /// <param name="source">Audio source to adjust.</param>
    /// <param name="startVol">Starting volume.</param>
    /// <param name="endVol">Target volume.</param>
    /// <param name="duration">Fade duration in seconds.</param>
    /// <returns>Coroutine enumerator for the volume fade.</returns>
    IEnumerator FadeMusicVolume(AudioSource source, float startVol, float endVol, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVol, endVol, t / duration);
            yield return null;
        }
        source.volume = endVol; 
    }

    /// <summary>
    /// Fades out this menu after successful calibration and switches to the next destination.
    /// </summary>
    /// <param name="playedClip">Confirmation clip used to time the transition delay.</param>
    /// <returns>Coroutine enumerator for the fade and scene/menu switch.</returns>
    IEnumerator FadeOutAndSwitch(AudioClip playedClip)
    {
        if (transforplayer2 != null) transforplayer2.SetTrigger("TransForPlayer2");

        float timer = 0f;
        float startAlpha = (rootCanvasGroup_Internal != null) ? rootCanvasGroup_Internal.alpha : 1f;
        
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            if (rootCanvasGroup_Internal) rootCanvasGroup_Internal.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
            yield return null;
        }
        if (rootCanvasGroup_Internal) rootCanvasGroup_Internal.alpha = 0f;

        if (playedClip != null)
        {
            float remainingWait = playedClip.length - fadeDuration;
            if (remainingWait > 0) yield return new WaitForSecondsRealtime(remainingWait);
        }
        
        SetMusicDucked(false, duckRestoreSpeed);
        yield return new WaitForSecondsRealtime(duckRestoreSpeed);

        if (SessionConfig.PlayerCount == 2 && playerIndexToAssign == 0)
        {
            if (nextMenuForPlayer2 != null)
            {
                nextMenuForPlayer2.SetActive(true); 
                gameObject.SetActive(false); 
            }
            else TransitionManager.Instance.LoadScene(gameSceneName);
        }
        else
        {
            TransitionManager.Instance.LoadScene(gameSceneName);
        }
    }
}
