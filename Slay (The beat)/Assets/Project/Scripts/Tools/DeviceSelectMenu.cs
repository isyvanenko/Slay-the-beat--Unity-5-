using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class DeviceSetupMenu : MonoBehaviour
{
    [Header("Configuration")]
    public int playerIndexToAssign = 0;

    [Header("Timing")]
    public float confirmationDelay = 0.5f;

    [Header("UI Feedback")]
    public TextMeshProUGUI statusText;
    public string defaultPrompt = "Press any button to join";
    public string confirmPrompt = "Press again to confirm!";

    [Header("Next Steps")]
    public GameObject nextMenuForPlayer2;
    public string gameSceneName = "GameScene";

    // Internal State
    private InputAction joinAction;
    private InputDevice pendingDevice;
    private bool isWaitingForConfirmation = false;
    private float lastInteractionTime;

    // --- FIX: Setup input ONCE in Awake ---
    private void Awake()
    {
        joinAction = new InputAction(binding: "/*/<button>", type: InputActionType.PassThrough);
        joinAction.AddBinding("<Joystick>/trigger");
        joinAction.AddBinding("<Gamepad>/start");

        // Subscribe here, but don't enable yet
        joinAction.performed += OnInputDetected;
    }

    private void OnEnable()
    {
        // Reset state
        if (statusText != null) statusText.text = defaultPrompt;
        isWaitingForConfirmation = false;
        pendingDevice = null;
        lastInteractionTime = 0f;

        // SAFE: Just turn it on
        joinAction.Enable();
    }

    private void OnDisable()
    {
        // SAFE: Just turn it off (Do NOT Dispose here)
        joinAction.Disable();
    }

    private void OnDestroy()
    {
        // SAFE: Only destroy when the object is truly gone
        joinAction.Dispose();
    }
    // --------------------------------------

    private void OnInputDetected(InputAction.CallbackContext ctx)
    {
        if (Time.unscaledTime < lastInteractionTime + confirmationDelay)
            return;

        InputDevice inputDev = ctx.control.device;

        if (SessionConfig.IsDeviceUsed(inputDev) && inputDev != pendingDevice)
            return;

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

        // Disabling the action is safe here, but we rely on OnDisable doing it for us
        GoToNextStep();
    }

    private void GoToNextStep()
    {
        if (SessionConfig.PlayerCount > 1 && playerIndexToAssign == 0)
        {
            if (nextMenuForPlayer2 != null)
            {
                nextMenuForPlayer2.SetActive(true);
                gameObject.SetActive(false); // Triggers OnDisable()
            }
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}