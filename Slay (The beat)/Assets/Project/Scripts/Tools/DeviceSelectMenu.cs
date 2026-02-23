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

    [Header("Visuals")]
    public float fadeDuration = 0.5f;
    public TextMeshProUGUI statusText;
    public string defaultPrompt = "Press any button to join";
    public string confirmPrompt = "Press again to confirm!";

    [Header("Specific Icons")]
    public Image danceMatIcon;
    public Image joystickIcon;
    // KeyboardIcon removed as per request
    public Color standbyColor = Color.black;
    public Color activeColor = Color.white;

    [Header("Animation Settings")]
    public float pulseScale = 1.15f;
    public float pulseSpeed = 5f;

    [Header("Next Steps")]
    public GameObject nextMenuForPlayer2;
    public string gameSceneName = "GameScene";

    // Internal State
    private InputAction joinAction;
    private InputDevice pendingDevice;
    private bool isWaitingForConfirmation = false;
    private float lastInteractionTime;
    private CanvasGroup canvasGroup;
    private bool isTransitioning = false;
    private Coroutine pulseRoutine1;
    private Coroutine pulseRoutine2;

    private void Awake() => canvasGroup = GetComponent<CanvasGroup>();

    private void OnEnable()
    {
        ResetUI();
        
        // Listen for any button on any device
        joinAction = new InputAction(binding: "/*/<button>", type: InputActionType.PassThrough);
        joinAction.performed += OnInputDetected;
        joinAction.Enable();

        canvasGroup.alpha = 0f;
        StartCoroutine(FadeIn());
    }

    private void OnDisable()
    {
        joinAction.performed -= OnInputDetected;
        joinAction.Disable();
        joinAction.Dispose();
        StopAllPulses();
    }

    private void ResetUI()
    {
        if (statusText != null) statusText.text = defaultPrompt;
        
        if (danceMatIcon) danceMatIcon.color = standbyColor;
        if (joystickIcon) joystickIcon.color = standbyColor;
        
        isWaitingForConfirmation = false;
        pendingDevice = null;
        lastInteractionTime = 0f;
        isTransitioning = false;
        StopAllPulses();
    }

    private void OnInputDetected(InputAction.CallbackContext ctx)
    {
        if (isTransitioning) return;
        if (Time.unscaledTime < lastInteractionTime + confirmationDelay) return;

        InputDevice inputDev = ctx.control.device;

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

        // Reset to standby
        if (danceMatIcon) danceMatIcon.color = standbyColor;
        if (joystickIcon) joystickIcon.color = standbyColor;
        StopAllPulses();

        string devName = device.name.ToLower();
        string devProd = device.description.product != null ? device.description.product.ToLower() : "";

        // --- DEVELOPER KEYBOARD LOGIC ---
        if (device is Keyboard || devName.Contains("keyboard"))
        {
            // Highlight BOTH for the developer
            if (danceMatIcon) danceMatIcon.color = activeColor;
            if (joystickIcon) joystickIcon.color = activeColor;
            
            pulseRoutine1 = StartCoroutine(PulseIcon(danceMatIcon));
            pulseRoutine2 = StartCoroutine(PulseIcon(joystickIcon));
        }
        // --- DANCE MAT DETECTION ---
        else if (devName.Contains("mat") || devProd.Contains("mat") || 
                 devProd.Contains("dance") || devProd.Contains("twin usb") || 
                 devProd.Contains("usb gamepad"))
        {
            if (danceMatIcon)
            {
                danceMatIcon.color = activeColor;
                pulseRoutine1 = StartCoroutine(PulseIcon(danceMatIcon));
            }
        }
        // --- GENERIC CONTROLLER ---
        else
        {
            if (joystickIcon)
            {
                joystickIcon.color = activeColor;
                pulseRoutine1 = StartCoroutine(PulseIcon(joystickIcon));
            }
        }

        if (statusText != null) statusText.text = confirmPrompt;
    }

    private void ConfirmSelection(InputDevice device)
    {
        lastInteractionTime = Time.unscaledTime;
        StopAllPulses();

        string deviceType = "Controller";
        
        // Determine type for SessionConfig
        if (device is Keyboard) deviceType = "Debug Keyboard";
        else if (danceMatIcon.color == activeColor && joystickIcon.color == standbyColor) deviceType = "Dance Mat";

        SessionConfig.SetPlayerDevice(playerIndexToAssign, device, deviceType);
        StartCoroutine(FadeOutAndSwitch());
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
            float lerpVal = (Mathf.Sin(timer) + 1f) / 2f; 
            target.transform.localScale = Vector3.Lerp(originalScale, targetScale, lerpVal);
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

    IEnumerator FadeIn()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOutAndSwitch()
    {
        isTransitioning = true;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        if (SessionConfig.PlayerCount > 1 && playerIndexToAssign == 0)
        {
            if (nextMenuForPlayer2 != null)
            {
                nextMenuForPlayer2.SetActive(true);
                gameObject.SetActive(false);
            }
        }
        else
        {
            TransitionManager.Instance.LoadScene(gameSceneName);
        }
    }
}