using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class DeviceSetupMenu : MonoBehaviour
{
    [Header("Custom Animations")]
    public GameObject transforplayer2obj;
    private Animator transforplayer2;

    [Header("Arrow GameObjects (Activated by direction)")]
    public GameObject upArrowSprite;
    public GameObject downArrowSprite;
    public GameObject leftArrowSprite;
    public GameObject rightArrowSprite;
    
    [Header("Arrow Alpha Pulse Settings")]
    public float pulseAlphaMin = 0.1f;
    public float pulseAlphaMax = 0.3f;
    public float pulseAlphaSpeed = 8f;
    public Color arrowGoldColor = new Color(1f, 0.84f, 0f, 1f);
    public Color arrowCompleteWhite = Color.white;

    [Header("Configuration")]
    public int playerIndexToAssign = 0;
    public float confirmationDelay = 0.5f;

    [Header("UI Controls")]
    public Button uiResetButton; 
    public TextMeshProUGUI promptText; 
    
    [Header("Player Count Text Objects")]
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI calibrationStatusText;

    [Header("Hardware Controls")]
    public InputActionReference resetActionReference;

    [Header("ESC Hold to Return")]
    public float escHoldRequiredTime = 3.0f;
    public Slider escHoldProgressSlider;
    public CanvasGroup mainMenuCanvas;

    [Header("Animation & Timing")]
    public PlayerInput playerInputToMap; 
    public float requiredHoldTime = 3.0f; 
    public float slipForgivenessTime = 0.35f; 

    [Header("Visual Tuning")]
    public float introWaitDuration = 0.5f;   
    public float introFadeDuration = 0.3f;   
    public float pulseSpeed = 8f;            
    public float pulseScaleMultiplier = 1.15f; 
    
    [Header("Error Shake Settings")]
    public float shakeDuration = 0.3f;
    public float shakeIntensity = 15f; 

    [Header("UI State Colors")]
    public Color normalBlackColor = Color.black; 
    public Color lockedWhiteColor = Color.white;
    public Color flashGoldColor = new Color(1f, 0.8f, 0f); 
    public Color errorRedColor = new Color(1f, 0.2f, 0.2f); 
    public Color confirmedGreenColor = new Color(0.2f, 0.8f, 0.2f);

    [Header("3D Emission Settings")]
    [ColorUsage(true, true)] public Color goldEmissionColor = new Color(1f, 0.8f, 0f, 1f) * 2f; 
    [ColorUsage(true, true)] public Color redEmissionColor = new Color(1f, 0.2f, 0.2f, 1f) * 3f;

    [System.Serializable]
    public class CalibrationAnimsGroup
    {
        public string actionName; 
        public string direction;
        
        [Header("2D UI Elements")]
        public Graphic backgroundBaseImage; 
        public Graphic arrowIconImage;      
        public Graphic lockIconImage;       
        public CanvasGroup containerToPulse; 
        
        [Header("3D Object")]
        public Renderer target3DModel;
        [HideInInspector] public Material matInstance;
        [HideInInspector] public Color originalEmissionColor;
        
        [HideInInspector] public Vector3 originalScale; 
        [HideInInspector] public Vector3 originalPosition; 
    }

    public List<CalibrationAnimsGroup> calibrationAnimations;

    [Header("Audio Setup")]
    public AudioSource globalAudioSource;   
    public AudioSource loopingAudioSource;  
    
    [Space(10)]
    public AudioClip stepPromptSfx;         
    public AudioClip continuousHoldSfx;     
    public AudioClip controllerSelectedSfx; 
    public AudioClip dancematSelectedSfx;   
    public AudioClip selectionConfirmedSfx; 
    public AudioClip resetSfx;              
    public AudioClip errorBuzzSfx;          

    [Header("Transitions & Ducking")]
    public float duckedMusicVolume = 0.2f;   
    public float duckDropSpeed = 0.15f;      
    public float duckRestoreSpeed = 0.5f;    
    public float fadeDuration = 0.5f;
    public float subFadeSpeed = 8f;
    public float sliderFadeSpeed = 5f;
    public string gameSceneName = "GameScene";
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
    
    // Store original arrow positions - simple dictionary
    private Dictionary<GameObject, Vector3> originalArrowPositions = new Dictionary<GameObject, Vector3>();
    
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
        
        // Store original arrow positions while object is active
        if (upArrowSprite != null) originalArrowPositions[upArrowSprite] = upArrowSprite.transform.localPosition;
        if (downArrowSprite != null) originalArrowPositions[downArrowSprite] = downArrowSprite.transform.localPosition;
        if (leftArrowSprite != null) originalArrowPositions[leftArrowSprite] = leftArrowSprite.transform.localPosition;
        if (rightArrowSprite != null) originalArrowPositions[rightArrowSprite] = rightArrowSprite.transform.localPosition;
        
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
        
        // Disable this object - it will be enabled when needed
        gameObject.SetActive(false);
    }

    private void ResetArrowPositions()
    {
        foreach (var kvp in originalArrowPositions)
        {
            if (kvp.Key != null)
                kvp.Key.transform.localPosition = kvp.Value;
        }
    }

    private void Start() 
    {
        if (transforplayer2obj != null) transforplayer2 = transforplayer2obj.GetComponent<Animator>();
        
        UpdatePlayerCountText();
        UpdateCalibrationStatusText(false);
    }

    private void Update()
    {
        HandleEscInput();
    }

    private void HandleEscInput()
    {
        if (isTransitioning) return;
        
        bool escPressed = false;
        
        if (Keyboard.current != null && Keyboard.current.escapeKey.isPressed)
        {
            escPressed = true;
        }
        
        bool escJustPressed = escPressed && !wasEscPressedLastFrame;
        
        if (escJustPressed)
        {
            if (isMapping)
            {
                OnEscPressed();
            }
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

    private void OnEscPressed()
    {
        if (isMapping)
        {
            ResetCalibration();
        }
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
        float duration = 1f / sliderFadeSpeed;
        
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
        isTransitioning = true;
        
        isEscHeld = false;
        escHoldTimer = 0f;
        wasEscPressedLastFrame = false;
        
        StopPulse();
        StopArrowPulse();
        DeactivateAllArrows();
        ResetArrowPositions();
        
        if (loopingAudioSource != null) loopingAudioSource.Stop();
        SetMusicDucked(false, duckRestoreSpeed);
        
        if (globalAudioSource != null && resetSfx != null)
            globalAudioSource.PlayOneShot(resetSfx);
        
        float timer = 0f;
        float startAlpha = (rootCanvasGroup_Internal != null) ? rootCanvasGroup_Internal.alpha : 1f;
        
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            if (rootCanvasGroup_Internal) rootCanvasGroup_Internal.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
            yield return null;
        }
        
        if (rootCanvasGroup_Internal) rootCanvasGroup_Internal.alpha = 0f;
        
        if (escHoldProgressSlider != null)
        {
            CanvasGroup sliderCG = escHoldProgressSlider.GetComponent<CanvasGroup>();
            if (sliderCG != null) sliderCG.alpha = 0f;
            escHoldProgressSlider.value = 0f;
        }
        
        gameObject.SetActive(false);
        
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

    private void DeactivateAllArrows()
    {
        if (upArrowSprite != null) upArrowSprite.SetActive(false);
        if (downArrowSprite != null) downArrowSprite.SetActive(false);
        if (leftArrowSprite != null) leftArrowSprite.SetActive(false);
        if (rightArrowSprite != null) rightArrowSprite.SetActive(false);
    }

    private GameObject GetArrowForDirection(string direction)
    {
        switch (direction.ToLower())
        {
            case "up": return upArrowSprite;
            case "down": return downArrowSprite;
            case "left": return leftArrowSprite;
            case "right": return rightArrowSprite;
            default: return null;
        }
    }

    private void ActivateArrowForDirection(string direction)
    {
        DeactivateAllArrows();
        currentActiveArrow = GetArrowForDirection(direction);
        if (currentActiveArrow != null)
        {
            currentActiveArrow.SetActive(true);
            ResetArrowPositions();
            SetArrowAlpha(currentActiveArrow, pulseAlphaMin);
        }
    }

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

    private void OnEnable()
    {
        ResetArrowPositions();
        
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

    private void GrabBGMReference()
    {
        if (bgmSource == null && MusicManager.Instance != null)
        {
            bgmSource = MusicManager.Instance.GetComponent<AudioSource>();
            if (bgmSource != null) originalBgmVolume = bgmSource.volume;
        }
    }

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

    private void OnHardwareResetPressed(InputAction.CallbackContext ctx)
    {
        if (isTransitioning) return;
        if (isMapping && myLockedDevice != null && ctx.control.device != myLockedDevice) return;
        ResetCalibration();
    }

    public void ResetCalibration()
    {
        if (isTransitioning) return; 

        StopAllCoroutines(); 
        StopPulse();
        StopArrowPulse();
        DeactivateAllArrows();
        ResetArrowPositions();

        if (loopingAudioSource != null) loopingAudioSource.Stop();
        SetMusicDucked(false, duckRestoreSpeed);

        if (globalAudioSource != null && resetSfx != null)
            globalAudioSource.PlayOneShot(resetSfx);

        isMapping = false;
        isWaitingForCalibrationInput = false;
        currentMapStep = 0;
        myLockedDevice = null;
        
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
        
        HideEscSlider();
    }

    private void ResetAllVisuals()
    {
        if (promptText != null) 
        {
            promptText.text = "PRESS ANY BUTTON TO START";
            promptText.transform.localPosition = promptOriginalPos;
        }

        DeactivateAllArrows();
        ResetArrowPositions();

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

    private void OnInputDetected(InputAction.CallbackContext ctx)
    {
        if (isTransitioning) return;
        if (Time.unscaledTime < lastInteractionTime + confirmationDelay) return;

        InputControl control = ctx.control;

        if (control.device is Mouse || control.device is Pointer) return;
        if (control.name == "anyKey" || control.path.Contains("anyKey")) return;

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

    private void SequenceNextAction()
    {
        if (currentMapStep >= calibrationAnimations.Count)
        {
            FinishCalibration();
            return;
        }

        CalibrationAnimsGroup currentGroup = calibrationAnimations[currentMapStep];
        
        if (promptText != null) promptText.text = $"<color=yellow>HOLD {currentGroup.actionName.ToUpper()}</color>";
        
        if (globalAudioSource != null && stepPromptSfx != null)
            globalAudioSource.PlayOneShot(stepPromptSfx);

        ActivateArrowForDirection(currentGroup.direction);
        StartPulse(currentGroup);
        isWaitingForCalibrationInput = true; 
    }

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

                if (currentActiveArrow != null)
                {
                    Color targetColor = Color.Lerp(
                        new Color(arrowGoldColor.r, arrowGoldColor.g, arrowGoldColor.b, pulseAlphaMin), 
                        arrowGoldColor, 
                        progress
                    );
                    SetArrowColor(currentActiveArrow, targetColor);
                    ResetArrowPositions();
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

        if (currentActiveArrow != null)
        {
            SetArrowColor(currentActiveArrow, arrowGoldColor);
            ResetArrowPositions();
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
        
        if (currentActiveArrow != null)
        {
            yield return StartCoroutine(FadeArrowColor(currentActiveArrow, arrowCompleteWhite, new Color(1f, 1f, 1f, 0f), 0.3f));
        }
        
        yield return new WaitForSecondsRealtime(0.3f); 

        currentMapStep++;
        SequenceNextAction(); 
    }

    IEnumerator FadeArrowColor(GameObject arrow, Color fromColor, Color toColor, float duration)
    {
        if (arrow == null) yield break;
        
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;
            SetArrowColor(arrow, Color.Lerp(fromColor, toColor, t));
            ResetArrowPositions();
            yield return null;
        }
        SetArrowColor(arrow, toColor);
    }

    IEnumerator ErrorShakeRoutine(CalibrationAnimsGroup animGroup)
    {
        StopPulse(); 
        StopArrowPulse();
        
        if (globalAudioSource != null && errorBuzzSfx != null) 
            globalAudioSource.PlayOneShot(errorBuzzSfx);

        if (promptText != null) promptText.text = $"<color=red>FOOT SLIPPED!</color>";

        if (currentActiveArrow != null)
        {
            SetArrowColor(currentActiveArrow, errorRedColor);
            ResetArrowPositions();
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
        
        if (animGroup.containerToPulse != null)
        {
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
        }
        else
        {
            yield return new WaitForSecondsRealtime(shakeDuration);
        }
        
        if (promptText != null) promptText.transform.localPosition = promptOriginalPos;
        
        if (currentActiveArrow != null)
        {
            Color resetColor = Color.white;
            resetColor.a = pulseAlphaMin;
            SetArrowColor(currentActiveArrow, resetColor);
            ResetArrowPositions();
            StartArrowPulse();
        }
        
        if (animGroup.matInstance != null)
            StartCoroutine(FadeMaterialEmission(animGroup.matInstance, redEmissionColor, animGroup.originalEmissionColor, 0.2f));

        if (animGroup.backgroundBaseImage != null)
            yield return StartCoroutine(FadeGraphicColor(animGroup.backgroundBaseImage, errorRedColor, normalBlackColor, 0.2f));
    }

    private void FinishCalibration()
    {
        if (promptText != null) promptText.text = "<color=white>CALIBRATION COMPLETE!</color>";
        
        DeactivateAllArrows();
        ResetArrowPositions();
        
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

    private void StartPulse(CalibrationAnimsGroup animGroup)
    {
        StopPulse(); 
        pulseRoutine = StartCoroutine(PulseTarget(animGroup));
        StartArrowPulse();
    }

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

    private void StartArrowPulse()
    {
        StopArrowPulse();
        if (currentActiveArrow != null)
        {
            arrowPulseRoutine = StartCoroutine(PulseArrowAlpha());
        }
    }

    private void StopArrowPulse()
    {
        if (arrowPulseRoutine != null)
        {
            StopCoroutine(arrowPulseRoutine);
            arrowPulseRoutine = null;
        }
    }

    IEnumerator PulseArrowAlpha()
    {
        while (currentActiveArrow != null && currentActiveArrow.activeInHierarchy)
        {
            float lerp = (Mathf.Sin(Time.unscaledTime * pulseAlphaSpeed) + 1f) / 2f;
            float alpha = Mathf.Lerp(pulseAlphaMin, pulseAlphaMax, lerp);
            SetArrowAlpha(currentActiveArrow, alpha);
            ResetArrowPositions();
            yield return null;
        }
    }

    IEnumerator PulseTarget(CalibrationAnimsGroup animGroup)
    {
        if (animGroup.containerToPulse == null) yield break;
        
        isCurrentlyPulsing = true;
        Transform targetTransform = animGroup.containerToPulse.transform;
        
        Vector3 fixedPosition = animGroup.originalPosition;
        Vector3 baseScale = animGroup.originalScale;
        Vector3 targetScale = baseScale * pulseScaleMultiplier; 
        
        while (isCurrentlyPulsing)
        {
            float lerp = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) / 2f;
            targetTransform.localScale = Vector3.Lerp(baseScale, targetScale, lerp);
            targetTransform.localPosition = fixedPosition;
            yield return null;
        }
        targetTransform.localScale = baseScale; 
        targetTransform.localPosition = fixedPosition;
    }

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

    private void SetCGAlpha(CanvasGroup cg, float alpha)
    {
        if (cg == null) return;
        cg.alpha = alpha;
        cg.interactable = alpha > 0.1f;
        cg.blocksRaycasts = alpha > 0.1f;
    }

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