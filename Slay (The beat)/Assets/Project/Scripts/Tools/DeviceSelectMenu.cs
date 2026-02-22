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
    public Image keyboardIcon; // Added Keyboard Slot
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
    private Coroutine pulseRoutine;
    private Image currentlyHighlighting;

    private void Awake() => canvasGroup = GetComponent<CanvasGroup>();

    private void OnEnable()
    {
        ResetUI();
        
        joinAction = new InputAction(binding: "/*/<button>", type: InputActionType.PassThrough);
        joinAction.AddBinding("<Joystick>/trigger");
        joinAction.AddBinding("<Gamepad>/start");
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
        StopPulse();
    }

    private void ResetUI()
    {
        if (statusText != null) statusText.text = defaultPrompt;
        
        // Set all icons to standby
        if (danceMatIcon) danceMatIcon.color = standbyColor;
        if (joystickIcon) joystickIcon.color = standbyColor;
        if (keyboardIcon) keyboardIcon.color = standbyColor;
        
        isWaitingForConfirmation = false;
        pendingDevice = null;
        lastInteractionTime = 0f;
        isTransitioning = false;
        StopPulse();
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

        // Reset all to standby before highlighting new selection
        if (danceMatIcon) danceMatIcon.color = standbyColor;
        if (joystickIcon) joystickIcon.color = standbyColor;
        if (keyboardIcon) keyboardIcon.color = standbyColor;

        string devName = device.name.ToLower();
        string devProd = device.description.product.ToLower();

        // Check Device Type
        if (devName.Contains("keyboard") || device is Keyboard)
        {
            currentlyHighlighting = keyboardIcon;
        }
        else if (devName.Contains("mat") || devProd.Contains("mat"))
        {
            currentlyHighlighting = danceMatIcon;
        }
        else
        {
            currentlyHighlighting = joystickIcon;
        }

        if (currentlyHighlighting != null)
        {
            currentlyHighlighting.color = activeColor;
            StopPulse();
            pulseRoutine = StartCoroutine(PulseIcon(currentlyHighlighting));
        }

        if (statusText != null) statusText.text = confirmPrompt;
    }

    private void ConfirmSelection(InputDevice device)
    {
        lastInteractionTime = Time.unscaledTime;
        StopPulse();

        string deviceType = "Controller";
        if (currentlyHighlighting == danceMatIcon) deviceType = "Dance Mat";
        if (currentlyHighlighting == keyboardIcon) deviceType = "Keyboard";

        SessionConfig.SetPlayerDevice(playerIndexToAssign, device, deviceType);
        StartCoroutine(FadeOutAndSwitch());
    }

    // --- ANIMATIONS ---

    IEnumerator PulseIcon(Image target)
    {
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

    private void StopPulse()
    {
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        if (danceMatIcon) danceMatIcon.transform.localScale = Vector3.one;
        if (joystickIcon) joystickIcon.transform.localScale = Vector3.one;
        if (keyboardIcon) keyboardIcon.transform.localScale = Vector3.one;
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