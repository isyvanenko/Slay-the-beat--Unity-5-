using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class DeviceSetupMenu : MonoBehaviour
{
    [Header("Configuration")]
    public int playerIndexToAssign = 0;
    public float confirmationDelay = 0.5f;

    [Header("🚨 Animation & Timing 🚨")]
    public PlayerInput playerInputToMap; 
    public TextMeshProUGUI promptText;   
    public float requiredHoldTime = 3.0f; // Exactly 3 seconds
    
    [Tooltip("How long the player can accidentally lift their foot before it fails them (in seconds)")]
    public float slipForgivenessTime = 0.25f; // THE FIX: Hardware Debounce Timer

    [Header("Visual Tuning")]
    public float introWaitDuration = 0.5f;   
    public float introFadeDuration = 0.3f;   
    public float pulseSpeed = 8f;            
    public float pulseScaleMultiplier = 1.15f; 

    [Header("State Colors")]
    public Color normalBlackColor = Color.black; 
    public Color lockedWhiteColor = Color.white;
    public Color flashGoldColor = new Color(1f, 0.8f, 0f); // Bright Gold

    [System.Serializable]
    public class CalibrationAnimsGroup
    {
        public string actionName; 
        
        public Graphic arrowMainImage;      
        public Graphic secondaryElement;    
        public CanvasGroup containerToPulse; 
        
        [HideInInspector] public Vector3 originalScale; 
    }

    public List<CalibrationAnimsGroup> calibrationAnimations;

    [Header("Audio Setup")]
    public AudioSource globalAudioSource; 
    public AudioClip controllerSelectedSfx;
    public AudioClip dancematSelectedSfx;
    public AudioClip selectionConfirmedSfx;

    [Header("Transitions")]
    public float duckedMusicVolume = 0.2f;   
    public float duckDropSpeed = 0.3f;       
    public float duckRestoreSpeed = 1.0f;    
    public float fadeDuration = 0.5f;
    public float subFadeSpeed = 8f;
    public string gameSceneName = "GameScene";
    public GameObject nextMenuForPlayer2; 

    // Internal State
    private CanvasGroup rootCanvasGroup_Internal; 
    private InputAction joinAction;
    private float lastInteractionTime;
    private bool isTransitioning = false;
    private bool isMapping = false;
    private int currentMapStep = 0;
    private InputActionRebindingExtensions.RebindingOperation rebindOp;
    
    private Coroutine pulseRoutine;
    private bool isCurrentlyPulsing = false;

    private void Awake()
    {
        rootCanvasGroup_Internal = GetComponent<CanvasGroup>();
        
        foreach (var animGroup in calibrationAnimations)
        {
            if (animGroup.containerToPulse != null)
                animGroup.originalScale = animGroup.containerToPulse.transform.localScale;
        }
    }

    private void OnEnable()
    {
        if (rootCanvasGroup_Internal) rootCanvasGroup_Internal.alpha = 0f;
        if (promptText != null) promptText.text = "";
        isCurrentlyPulsing = false;
        
        foreach (var animGroup in calibrationAnimations)
        {
            if (animGroup.containerToPulse != null)
            {
                animGroup.containerToPulse.alpha = 0f; 
                animGroup.containerToPulse.transform.localScale = animGroup.originalScale; 
            }
            if (animGroup.arrowMainImage != null) animGroup.arrowMainImage.color = normalBlackColor;
            
            if (animGroup.secondaryElement != null)
            {
                Color c = animGroup.secondaryElement.color;
                c.a = 1f;
                animGroup.secondaryElement.color = c;
            }
        }

        isMapping = false;
        lastInteractionTime = 0f;
        isTransitioning = false;

        joinAction = new InputAction(binding: "/*/<button>", type: InputActionType.PassThrough);
        joinAction.performed += OnInputDetected;
        joinAction.Enable();

        if (rootCanvasGroup_Internal) StartCoroutine(FadeIn(rootCanvasGroup_Internal, 1f));
    }

    private void OnDisable()
    {
        if (joinAction != null) joinAction.performed -= OnInputDetected;
        if (rebindOp != null) rebindOp.Dispose();
        StopPulse();
    }

    private void OnInputDetected(InputAction.CallbackContext ctx)
    {
        if (isTransitioning || isMapping) return;
        if (Time.unscaledTime < lastInteractionTime + confirmationDelay) return;

        InputDevice inputDev = ctx.control.device;
        bool isKeyboard = inputDev is Keyboard || inputDev.name.ToLower().Contains("keyboard");
        
        if (!isKeyboard)
        {
            if (playerIndexToAssign == 1 && SessionConfig.Player1Device == inputDev) return;
            if (playerIndexToAssign == 0 && SessionConfig.Player2Device == inputDev) return;
        }

        StartMappingSequence(inputDev);
    }

    private void StartMappingSequence(InputDevice device)
    {
        isMapping = true;
        joinAction.Disable(); 
        
        SessionConfig.SetPlayerDevice(playerIndexToAssign, device, "Custom");

        if (globalAudioSource != null && dancematSelectedSfx != null)
            globalAudioSource.PlayOneShot(dancematSelectedSfx);

        if (playerInputToMap != null)
        {
            playerInputToMap.actions.Disable();
            playerInputToMap.actions.RemoveAllBindingOverrides(); 
        }

        currentMapStep = 0;
        StartCoroutine(IntroPopUpAndInitialize(device));
    }

    IEnumerator IntroPopUpAndInitialize(InputDevice lockedDevice)
    {
        if (promptText != null) promptText.text = $"Player {playerIndexToAssign + 1}\nPREPARE CALIBRATION!";
        yield return new WaitForSecondsRealtime(introWaitDuration);

        foreach (var animGroup in calibrationAnimations)
        {
            if (animGroup.containerToPulse != null) 
                StartCoroutine(FadeIn(animGroup.containerToPulse, 1f, introFadeDuration));
            yield return new WaitForSecondsRealtime(0.1f); 
        }

        yield return new WaitForSecondsRealtime(introFadeDuration);

        currentMapStep = 0;
        SequenceNextAction(lockedDevice);
    }

    private void SequenceNextAction(InputDevice lockedDevice)
    {
        if (currentMapStep >= calibrationAnimations.Count)
        {
            FinishCalibration();
            return;
        }

        CalibrationAnimsGroup currentGroup = calibrationAnimations[currentMapStep];
        string actionName = currentGroup.actionName;

        if (promptText != null) promptText.text = $"STEP ON: <color=yellow>{actionName.ToUpper()}</color>";
        
        StartPulse(currentGroup);

        rebindOp = playerInputToMap.actions[actionName].PerformInteractiveRebinding()
            .WithControlsHavingToMatchPath(lockedDevice.path) 
            .OnMatchWaitForAnother(0.1f) // ADDED SAFETY: Ignores physical "bounce" when first pressing
            .OnComplete(operation => 
            {
                InputControl control = operation.selectedControl;
                operation.Dispose();
                
                StartCoroutine(VerifyHoldAndCrossFadeRoutine(currentGroup, control, lockedDevice, actionName));
            })
            .Start();
    }

    // 🚨 THE NEW BOUNCE-PROOF HOLD COROUTINE 🚨
    IEnumerator VerifyHoldAndCrossFadeRoutine(CalibrationAnimsGroup animGroup, InputControl control, InputDevice lockedDevice, string actionName)
    {
        float holdTimer = 0f;
        float currentSlipTime = 0f; // Tracks how long the foot has been off the pad

        while (holdTimer < requiredHoldTime)
        {
            if (control.IsPressed())
            {
                // FOOT IS ON THE SENSOR!
                currentSlipTime = 0f; // Reset slip forgiveness
                holdTimer += Time.unscaledDeltaTime; 
                
                float progress = holdTimer / requiredHoldTime; 

                // Progress animations forward
                animGroup.arrowMainImage.color = Color.Lerp(normalBlackColor, lockedWhiteColor, progress);

                if (animGroup.secondaryElement != null)
                {
                    Color secColor = animGroup.secondaryElement.color;
                    secColor.a = Mathf.Lerp(1f, 0f, progress);
                    animGroup.secondaryElement.color = secColor;
                }
            }
            else
            {
                // FOOT SLIPPED OR SENSOR FLICKERED!
                currentSlipTime += Time.unscaledDeltaTime;

                // Only fail them if the sensor has been dead longer than the forgiveness time
                if (currentSlipTime >= slipForgivenessTime)
                {
                    promptText.text = $"<color=red>FOOT SLIPPED!</color>\nHOLD <color=yellow>{actionName.ToUpper()}</color>";
                    
                    StartCoroutine(FadeGraphicColor(animGroup.arrowMainImage, animGroup.arrowMainImage.color, normalBlackColor, 0.2f));
                    StartCoroutine(FadeGraphicAlpha(animGroup.secondaryElement, animGroup.secondaryElement.color.a, 1f, 0.2f));

                    yield return new WaitForSecondsRealtime(0.3f);
                    SequenceNextAction(lockedDevice); 
                    yield break; // Kill the routine
                }
                // If currentSlipTime is LESS than forgiveness, we do nothing. 
                // The progress bar just pauses, waiting for the foot to reconnect!
            }

            yield return null;
        }

        // --- SUCCESS: 3 SECONDS REACHED ---
        StopPulse(); 

        if (animGroup.arrowMainImage != null) animGroup.arrowMainImage.color = lockedWhiteColor;
        if (animGroup.secondaryElement != null)
        {
            Color c = animGroup.secondaryElement.color;
            c.a = 0f;
            animGroup.secondaryElement.color = c;
        }

        if (globalAudioSource != null && controllerSelectedSfx != null) 
            globalAudioSource.PlayOneShot(controllerSelectedSfx);

        promptText.text = "<color=green>LOCKED!</color>";
        
        animGroup.arrowMainImage.color = flashGoldColor;
        yield return new WaitForSecondsRealtime(0.15f); 

        yield return StartCoroutine(FadeGraphicColor(animGroup.arrowMainImage, flashGoldColor, lockedWhiteColor, 0.3f));
        yield return new WaitForSecondsRealtime(0.3f); 

        currentMapStep++;
        SequenceNextAction(lockedDevice); 
    }

    private void FinishCalibration()
    {
        if (promptText != null) promptText.text = "<color=green>CALIBRATION COMPLETE!</color>";
        
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

        StartCoroutine(FadeOutAndSwitch(selectionConfirmedSfx));
    }

    // --- ANIMATION HELPERS ---
    private void StartPulse(CalibrationAnimsGroup animGroup)
    {
        StopPulse(); 
        pulseRoutine = StartCoroutine(PulseTarget(animGroup));
    }

    private void StopPulse()
    {
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        isCurrentlyPulsing = false;
        
        foreach (var animGroup in calibrationAnimations)
        {
            if (animGroup.containerToPulse != null && animGroup.originalScale != Vector3.zero) 
                animGroup.containerToPulse.transform.localScale = animGroup.originalScale;
        }
    }

    IEnumerator PulseTarget(CalibrationAnimsGroup animGroup)
    {
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

    // --- TRANSITIONS ---
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
        AudioSource musicSource = null;
        float originalMusicVolume = 1f;
        if (MusicManager.Instance != null) 
        {
            musicSource = MusicManager.Instance.GetComponent<AudioSource>();
            if (musicSource != null) originalMusicVolume = musicSource.volume;
        }
        if (musicSource != null) StartCoroutine(FadeMusicVolume(musicSource, musicSource.volume, duckedMusicVolume, duckDropSpeed));

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
        if (musicSource != null) yield return StartCoroutine(FadeMusicVolume(musicSource, musicSource.volume, originalMusicVolume, duckRestoreSpeed));

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