using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class DeviceSetupMenu : MonoBehaviour
{
    [Header("Configuration")]
    public int playerIndexToAssign = 0;
    public float confirmationDelay = 0.5f;

    [Header("Audio Setup")]
    public AudioSource globalAudioSource; 
    public AudioClip controllerSelectedSfx;
    public AudioClip dancematSelectedSfx;
    public AudioClip selectionConfirmedSfx;

    [Header("Audio Ducking (Smooth Fades)")]
    public float duckedMusicVolume = 0.2f;   
    public float duckDropSpeed = 0.3f;       
    public float duckRestoreSpeed = 1.0f;    

    [Header("Canvases (CanvasGroups)")]
    public CanvasGroup mainPromptCG;    
    public CanvasGroup confirmControllerCG; 
    public CanvasGroup confirmMatCG;    
    public float subFadeSpeed = 8f;

    [Header("Visuals")]
    public float fadeDuration = 0.5f;
    public string gameSceneName = "GameScene";

    [Header("Icons to Pulse")]
    public Image danceMatIcon;
    public Image joystickIcon;
    public float pulseScale = 1.15f;
    public float pulseSpeed = 5f;

    [Header("Next Steps")]
    public GameObject nextMenuForPlayer2; // DRAG PLAYER 2 UI HERE

    // Internal State
    private InputAction joinAction;
    private InputDevice pendingDevice;
    private bool isWaitingForConfirmation = false;
    private float lastInteractionTime;
    private CanvasGroup rootCanvasGroup;
    private bool isTransitioning = false;
    
    private Coroutine pulseRoutine1;
    private Coroutine pulseRoutine2;
    private CanvasGroup activeConfirmCG;

    private void Awake() => rootCanvasGroup = GetComponent<CanvasGroup>();

    private void OnEnable()
    {
        ResetUI();
        
        joinAction = new InputAction(binding: "/*/<button>", type: InputActionType.PassThrough);
        joinAction.performed += OnInputDetected;
        joinAction.Enable();

        rootCanvasGroup.alpha = 0f;
        StartCoroutine(FadeIn(rootCanvasGroup, 1f));
    }

    private void OnDisable()
    {
        if (joinAction != null)
        {
            joinAction.performed -= OnInputDetected;
            joinAction.Disable();
        }
        StopAllPulses();
    }

    private void ResetUI()
    {
        SetCGAlpha(mainPromptCG, 1f);
        SetCGAlpha(confirmControllerCG, 0f);
        SetCGAlpha(confirmMatCG, 0f);
        
        isWaitingForConfirmation = false;
        pendingDevice = null;
        lastInteractionTime = 0f;
        isTransitioning = false;
        activeConfirmCG = null;
        StopAllPulses();
    }

    private void OnInputDetected(InputAction.CallbackContext ctx)
    {
        if (isTransitioning) return;
        if (Time.unscaledTime < lastInteractionTime + confirmationDelay) return;

        InputDevice inputDev = ctx.control.device;

        // =========================================================
        // 🚨 STRICT DEVICE LOCKOUT (NO STEALING ALLOWED) 🚨
        // =========================================================
        // If this menu is setting up Player 2, reject Player 1's hardware
        if (playerIndexToAssign == 1 && SessionConfig.Player1Device == inputDev)
        {
            Debug.LogWarning($"<color=red>[REJECTED]</color> Player 2 tried to press a button on Player 1's locked device ({inputDev.deviceId}).");
            
            // Pro-Tip: If you add an "errorBuzzSfx" AudioClip to this script later, 
            // you can uncomment the line below to play a buzzer sound so they know they messed up!
            // if (globalAudioSource != null && errorBuzzSfx != null) globalAudioSource.PlayOneShot(errorBuzzSfx);
            
            return; // Stop the code dead in its tracks. Ignore the input.
        }

        // If this menu is setting up Player 1, reject Player 2's hardware (just in case they go backwards)
        if (playerIndexToAssign == 0 && SessionConfig.Player2Device == inputDev)
        {
            return; 
        }
        // =========================================================

        // If they pressed the same safe device twice, confirm it
        if (isWaitingForConfirmation && pendingDevice == inputDev)
        {
            ConfirmSelection(inputDev);
        }
        else
        {
            // Otherwise, start the prompt for this new safe device
            StartConfirmationPhase(inputDev);
        }
    }

    private void StartConfirmationPhase(InputDevice device)
    {
        pendingDevice = device;
        isWaitingForConfirmation = true;
        lastInteractionTime = Time.unscaledTime;
        StopAllPulses();

        string devName = device.name.ToLower();
        string devProd = device.description.product?.ToLower() ?? "";

        CanvasGroup nextCG = null;
        AudioClip clipToPlay = null;

        if (device is Keyboard || devName.Contains("keyboard"))
        {
            nextCG = confirmControllerCG; 
            pulseRoutine1 = StartCoroutine(PulseIcon(danceMatIcon));
            pulseRoutine2 = StartCoroutine(PulseIcon(joystickIcon));
            clipToPlay = controllerSelectedSfx;
        }
        else if (devName.Contains("mat") || devProd.Contains("mat") || devProd.Contains("dance") || devProd.Contains("usb gamepad"))
        {
            nextCG = confirmMatCG;
            pulseRoutine1 = StartCoroutine(PulseIcon(danceMatIcon));
            clipToPlay = dancematSelectedSfx;
        }
        else
        {
            nextCG = confirmControllerCG;
            pulseRoutine1 = StartCoroutine(PulseIcon(joystickIcon));
            clipToPlay = controllerSelectedSfx;
        }

        // Play the "Selected" SFX
        if (globalAudioSource != null && clipToPlay != null)
        {
            globalAudioSource.PlayOneShot(clipToPlay);
        }

        StartCoroutine(FadeOut(mainPromptCG));
        StartCoroutine(FadeIn(nextCG, 1f));
        activeConfirmCG = nextCG;
    }

    private void ConfirmSelection(InputDevice device)
    {
        lastInteractionTime = Time.unscaledTime;
        isTransitioning = true;
        StopAllPulses();

        string deviceType = "Controller";
        if (device is Keyboard) deviceType = "Debug Keyboard";
        else if (activeConfirmCG == confirmMatCG) deviceType = "Dance Mat";

        // === X-RAY DEBUG LOGS ===
        Debug.Log($"<color=orange>[MENU SYSTEM]</color> Menu for Player Index {playerIndexToAssign} is trying to save device: {device.deviceId}");

        SessionConfig.SetPlayerDevice(playerIndexToAssign, device, deviceType);

        // Immediate verification to see if SessionConfig actually saved it!
        Debug.Log($"<color=orange>[MENU SYSTEM]</color> VERIFICATION: SessionConfig.Player1Device is now: {(SessionConfig.Player1Device != null ? "SAVED" : "NULL")}");
        Debug.Log($"<color=orange>[MENU SYSTEM]</color> VERIFICATION: SessionConfig.Player2Device is now: {(SessionConfig.Player2Device != null ? "SAVED" : "NULL")}");
        // ========================

        if (globalAudioSource != null && selectionConfirmedSfx != null)
        {
            globalAudioSource.PlayOneShot(selectionConfirmedSfx);
        }

        StartCoroutine(FadeOutAndSwitch(selectionConfirmedSfx));
    }

    // --- TRANSITION & AUDIO DUCKING LOGIC ---

    IEnumerator FadeOutAndSwitch(AudioClip playedClip)
    {
        AudioSource musicSource = null;
        float originalMusicVolume = 1f;

        if (MusicManager.Instance != null) 
        {
            musicSource = MusicManager.Instance.GetComponent<AudioSource>();
            if (musicSource != null)
            {
                originalMusicVolume = musicSource.volume;
            }
        }

        // 1. Smoothly duck music volume
        if (musicSource != null)
        {
            StartCoroutine(FadeMusicVolume(musicSource, musicSource.volume, duckedMusicVolume, duckDropSpeed));
        }

        // 2. Fade out UI
        float timer = 0f;
        float startAlpha = rootCanvasGroup.alpha;
        
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            rootCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
            yield return null;
        }
        rootCanvasGroup.alpha = 0f;

        // 3. Wait for Confirmation Audio to finish playing
        if (playedClip != null)
        {
            float remainingWait = playedClip.length - fadeDuration;
            if (remainingWait > 0) 
            {
                yield return new WaitForSecondsRealtime(remainingWait);
            }
        }

        // 4. Restore Music Volume completely
        if (musicSource != null)
        {
            yield return StartCoroutine(FadeMusicVolume(musicSource, musicSource.volume, originalMusicVolume, duckRestoreSpeed));
        }

        // 5. Logic for switching UI or loading the next scene
        if (SessionConfig.PlayerCount == 2 && playerIndexToAssign == 0)
        {
            if (nextMenuForPlayer2 != null)
            {
                nextMenuForPlayer2.SetActive(true); 
                gameObject.SetActive(false); 
            }
            else
            {
                 TransitionManager.Instance.LoadScene(gameSceneName);
            }
        }
        else
        {
            // === ADD THESE TWO LIE DETECTOR LINES HERE ===
            Debug.Log($"<color=magenta>LEAVING MENU! P1 Device is: {(SessionConfig.Player1Device != null ? "SAVED" : "NULL")}</color>");
            Debug.Log($"<color=magenta>LEAVING MENU! P2 Device is: {(SessionConfig.Player2Device != null ? "SAVED" : "NULL")}</color>");
            // =============================================

            TransitionManager.Instance.LoadScene(gameSceneName);
        }
    }

    // --- HELPER METHODS ---

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

    private void SetCGAlpha(CanvasGroup cg, float alpha)
    {
        if (cg == null) return;
        cg.alpha = alpha;
        cg.interactable = alpha > 0.1f;
        cg.blocksRaycasts = alpha > 0.1f;
    }

    IEnumerator FadeIn(CanvasGroup cg, float targetAlpha)
    {
        if (cg == null) yield break;
        while (cg.alpha < targetAlpha)
        {
            cg.alpha += Time.unscaledDeltaTime * subFadeSpeed;
            yield return null;
        }
        SetCGAlpha(cg, targetAlpha);
    }

    IEnumerator FadeOut(CanvasGroup cg)
    {
        if (cg == null) yield break;
        while (cg.alpha > 0)
        {
            cg.alpha -= Time.unscaledDeltaTime * subFadeSpeed;
            yield return null;
        }
        SetCGAlpha(cg, 0f);
    }

    IEnumerator PulseIcon(Image target)
    {
        if (target == null) yield break;
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = Vector3.one * pulseScale;
        float timer = 0f;
        while (isWaitingForConfirmation)
        {
            timer += Time.unscaledDeltaTime * pulseSpeed;
            target.transform.localScale = Vector3.Lerp(originalScale, targetScale, (Mathf.Sin(timer) + 1f) / 2f);
            yield return null;
        }
        target.transform.localScale = Vector3.one;
    }

    private void StopAllPulses()
    {
        if (pulseRoutine1 != null) StopCoroutine(pulseRoutine1);
        if (pulseRoutine2 != null) StopCoroutine(pulseRoutine2);
        if (danceMatIcon) danceMatIcon.transform.localScale = Vector3.one;
        if (joystickIcon) joystickIcon.transform.localScale = Vector3.one;
    }
}