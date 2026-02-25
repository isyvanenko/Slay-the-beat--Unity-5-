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
        
        // Prevent P2 from stealing P1's device
        if (SessionConfig.IsDeviceUsed(inputDev) && inputDev != pendingDevice) return;

        if (isWaitingForConfirmation && pendingDevice == inputDev)
        {
            ConfirmSelection(inputDev);
        }
        else
        {
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

        if (device is Keyboard || devName.Contains("keyboard"))
        {
            nextCG = confirmControllerCG; 
            pulseRoutine1 = StartCoroutine(PulseIcon(danceMatIcon));
            pulseRoutine2 = StartCoroutine(PulseIcon(joystickIcon));
        }
        else if (devName.Contains("mat") || devProd.Contains("mat") || devProd.Contains("dance") || devProd.Contains("usb gamepad"))
        {
            nextCG = confirmMatCG;
            pulseRoutine1 = StartCoroutine(PulseIcon(danceMatIcon));
        }
        else
        {
            nextCG = confirmControllerCG;
            pulseRoutine1 = StartCoroutine(PulseIcon(joystickIcon));
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

        SessionConfig.SetPlayerDevice(playerIndexToAssign, device, deviceType);
        StartCoroutine(FadeOutAndSwitch());
    }

    // --- TRANSITION LOGIC ---

    IEnumerator FadeOutAndSwitch()
{
    // 1. Wait for the root canvas of THIS player to finish fading out
    float timer = 0f;
    float startAlpha = rootCanvasGroup.alpha;
    
    while (timer < fadeDuration)
    {
        timer += Time.unscaledDeltaTime;
        rootCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
        yield return null;
    }
    rootCanvasGroup.alpha = 0f;

    // 2. Small buffer to ensure rendering has caught up
    yield return new WaitForSecondsRealtime(0.05f);

    // 3. Logic for switching or loading
    if (SessionConfig.PlayerCount == 2 && playerIndexToAssign == 0)
    {
        if (nextMenuForPlayer2 != null)
        {
            // We turn ON P2 first, then turn OFF P1
            nextMenuForPlayer2.SetActive(true); 
            gameObject.SetActive(false); 
        }
        else
        {
            // Fallback if you forgot to assign P2 in the inspector
             TransitionManager.Instance.LoadScene(gameSceneName);
        }
    }
    else
    {
        TransitionManager.Instance.LoadScene(gameSceneName);
        // P1 in Solo mode OR P2 has finished
        
    }
}

    // --- UI HELPERS ---

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