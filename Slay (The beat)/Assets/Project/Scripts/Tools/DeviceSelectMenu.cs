using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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

    [Header("Next Steps")]
    public GameObject nextMenuForPlayer2;
    public string gameSceneName = "GameScene";

    // Internal
    private InputAction joinAction;
    private InputDevice pendingDevice;
    private bool isWaitingForConfirmation = false;
    private float lastInteractionTime;
    private CanvasGroup canvasGroup;
    private bool isTransitioning = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (statusText != null) statusText.text = defaultPrompt;
        
        // Reset State
        isWaitingForConfirmation = false;
        pendingDevice = null;
        lastInteractionTime = 0f;
        isTransitioning = false;

        // Start Input
        joinAction = new InputAction(binding: "/*/<button>", type: InputActionType.PassThrough);
        joinAction.AddBinding("<Joystick>/trigger");
        joinAction.AddBinding("<Gamepad>/start");
        joinAction.performed += OnInputDetected;
        joinAction.Enable();

        // FADE IN START
        canvasGroup.alpha = 0f;
        StartCoroutine(FadeIn());
    }

    private void OnDisable()
    {
        joinAction.performed -= OnInputDetected;
        joinAction.Disable();
        joinAction.Dispose();
    }

    // --- FADE IN LOGIC ---
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

    private void OnInputDetected(InputAction.CallbackContext ctx)
    {
        // Don't accept input if we are fading out
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

        if (statusText != null)
        {
            string cleanName = device.name.Replace("InputDevice", "").Trim();
            statusText.text = $"<color=yellow>{cleanName} Detected!</color>\n{confirmPrompt}";
        }
    }

    private void ConfirmSelection(InputDevice device)
    {
        lastInteractionTime = Time.unscaledTime;
        
        string deviceType = "Controller";
        if (device.name.ToLower().Contains("mat") || device is Joystick)
            deviceType = "Dance Mat";

        Debug.Log($"CONFIRMED: Player {playerIndexToAssign + 1} uses {device.name}");
        SessionConfig.SetPlayerDevice(playerIndexToAssign, device, deviceType);

        // Instead of switching immediately, we Fade Out first
        StartCoroutine(FadeOutAndSwitch());
    }

    // --- FADE OUT LOGIC ---
    IEnumerator FadeOutAndSwitch()
    {
        isTransitioning = true; // Block input
        joinAction.Disable();   // Stop listening

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        // NOW we switch
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
            SceneManager.LoadScene(gameSceneName);
        }
    }
}