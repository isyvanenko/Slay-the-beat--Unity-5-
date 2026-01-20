using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class DeviceSetupMenu : MonoBehaviour
{
    [Header("Configuration")]
    public int playerIndexToAssign = 0;

    [Header("Timing")]
    public float confirmationDelay = 0.5f; // Prevents accidental double-clicks

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
    private float lastInteractionTime; // Tracks when we last pressed a button

    private void OnEnable()
    {
        if (statusText != null) statusText.text = defaultPrompt;
        
        isWaitingForConfirmation = false;
        pendingDevice = null;
        lastInteractionTime = 0f; // Reset timer

        // Listen for ANY button
        joinAction = new InputAction(binding: "/*/<button>", type: InputActionType.PassThrough);
        joinAction.AddBinding("<Joystick>/trigger");
        joinAction.AddBinding("<Gamepad>/start");
        
        joinAction.performed += OnInputDetected;
        joinAction.Enable();
    }

    private void OnDisable()
    {
        joinAction.performed -= OnInputDetected;
        joinAction.Disable();
        joinAction.Dispose();
    }

    private void OnInputDetected(InputAction.CallbackContext ctx)
    {
        // 1. GLOBAL COOLDOWN
        // If we just pressed a button, ignore everything for a split second
        if (Time.unscaledTime < lastInteractionTime + confirmationDelay)
            return;

        InputDevice inputDev = ctx.control.device;

        // 2. IGNORE IF ALREADY TAKEN (Unless it's this player re-confirming)
        if (SessionConfig.IsDeviceUsed(inputDev) && inputDev != pendingDevice)
            return;

        // 3. LOGIC BRANCH
        if (isWaitingForConfirmation && pendingDevice == inputDev)
        {
            // --- STEP 2: CONFIRMATION ---
            ConfirmSelection(inputDev);
        }
        else
        {
            // --- STEP 1: DETECTION ---
            StartConfirmationPhase(inputDev);
        }
    }

    private void StartConfirmationPhase(InputDevice device)
    {
        pendingDevice = device;
        isWaitingForConfirmation = true;
        lastInteractionTime = Time.unscaledTime; // Start the cooldown timer

        // Update UI
        if (statusText != null)
        {
            string cleanName = device.name.Replace("InputDevice", "").Trim();
            statusText.text = $"<color=yellow>{cleanName} Detected!</color>\n{confirmPrompt}";
        }
        
        // Optional: Play a sound here if you have an AudioSource
        // GetComponent<AudioSource>()?.PlayOneShot(detectSound);
    }

    private void ConfirmSelection(InputDevice device)
    {
        lastInteractionTime = Time.unscaledTime; // Update timer just in case

        string deviceType = "Controller";
        if (device.name.ToLower().Contains("mat") || device is Joystick)
            deviceType = "Dance Mat";

        Debug.Log($"CONFIRMED: Player {playerIndexToAssign + 1} uses {device.name}");

        SessionConfig.SetPlayerDevice(playerIndexToAssign, device, deviceType);

        joinAction.Disable();
        GoToNextStep();
    }

    private void GoToNextStep()
    {
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