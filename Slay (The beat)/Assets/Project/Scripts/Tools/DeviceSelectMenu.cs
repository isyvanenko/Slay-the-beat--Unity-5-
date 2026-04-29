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

    [Header("🚨 NEW: Mapping Setup 🚨")]
    public PlayerInput playerInputToMap; // Drag a PlayerInput component here
    public TextMeshProUGUI promptText;   // Drag your main text here (e.g., "Press to Join")

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
    public float subFadeSpeed = 8f;
    public float fadeDuration = 0.5f;
    public string gameSceneName = "GameScene";

    [Header("Next Steps")]
    public GameObject nextMenuForPlayer2; 

    // Internal State
    private InputAction joinAction;
    private float lastInteractionTime;
    private CanvasGroup rootCanvasGroup;
    private bool isTransitioning = false;
    
    // Mapping Variables
    private string[] actionsToMap = { "Left", "Down", "Up", "Right" };
    private int currentMapStep = 0;
    private InputActionRebindingExtensions.RebindingOperation rebindOp;
    private bool isMapping = false;

    private void Awake() => rootCanvasGroup = GetComponent<CanvasGroup>();

    private void OnEnable()
    {
        SetCGAlpha(mainPromptCG, 1f);
        if (promptText != null) promptText.text = $"Player {playerIndexToAssign + 1}\nPress any button on your controller/mat to begin.";
        
        isMapping = false;
        lastInteractionTime = 0f;
        isTransitioning = false;

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
        if (rebindOp != null) rebindOp.Dispose();
    }

    private void OnInputDetected(InputAction.CallbackContext ctx)
    {
        if (isTransitioning || isMapping) return;
        if (Time.unscaledTime < lastInteractionTime + confirmationDelay) return;

        InputDevice inputDev = ctx.control.device;

        // 🚨 THE KEYBOARD EXCEPTION 🚨
        // Check if the device is a keyboard. If it is, we ALLOW sharing!
        bool isKeyboard = inputDev is Keyboard || inputDev.name.ToLower().Contains("keyboard");

        if (!isKeyboard)
        {
            // STRICT DEVICE LOCKOUT (Only applies to Gamepads/Dance Mats)
            if (playerIndexToAssign == 1 && SessionConfig.Player1Device == inputDev) return;
            if (playerIndexToAssign == 0 && SessionConfig.Player2Device == inputDev) return;
        }

        // Start the Mapping Sequence!
        StartMappingPhase(inputDev);
    }

    private void StartMappingPhase(InputDevice device)
    {
        isMapping = true;
        joinAction.Disable(); // Stop listening for random buttons
        
        // Lock the device to the player so they can't be stolen
        SessionConfig.SetPlayerDevice(playerIndexToAssign, device, "Custom");

        if (globalAudioSource != null && dancematSelectedSfx != null)
        {
            globalAudioSource.PlayOneShot(dancematSelectedSfx);
        }

        // Prep the Action Map
        if (playerInputToMap != null)
        {
            playerInputToMap.actions.Disable();
            playerInputToMap.actions.RemoveAllBindingOverrides(); // Clear old memory
        }

        currentMapStep = 0;
        MapNextAction(device);
    }

    private void MapNextAction(InputDevice lockedDevice)
    {
        if (currentMapStep >= actionsToMap.Length)
        {
            FinishMapping();
            return;
        }

        string actionName = actionsToMap[currentMapStep];
        
        if (promptText != null) 
        {
            promptText.text = $"<color=yellow>STEP ON: {actionName.ToUpper()}</color>";
        }

        // The magic Unity code that waits for the user to press a button
        rebindOp = playerInputToMap.actions[actionName].PerformInteractiveRebinding()
            .WithControlsHavingToMatchPath(lockedDevice.path) // 🚨 Only listen to the mat they just locked in!
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => 
            {
                operation.Dispose();
                
                if (globalAudioSource != null && controllerSelectedSfx != null) 
                    globalAudioSource.PlayOneShot(controllerSelectedSfx);
                
                currentMapStep++;
                MapNextAction(lockedDevice); // Loop to the next button
            })
            .Start();
    }

    private void FinishMapping()
    {
        if (promptText != null) promptText.text = "<color=green>ALL SET!</color>";
        
        // Save the custom map into JSON for the GameplayManager
        if (playerInputToMap != null)
        {
            string overridesJson = playerInputToMap.actions.SaveBindingOverridesAsJson();
            if (playerIndexToAssign == 0) SessionConfig.P1Bindings = overridesJson;
            else SessionConfig.P2Bindings = overridesJson;

            playerInputToMap.actions.Enable();
        }

        isTransitioning = true;

        if (globalAudioSource != null && selectionConfirmedSfx != null)
        {
            globalAudioSource.PlayOneShot(selectionConfirmedSfx);
        }

        StartCoroutine(FadeOutAndSwitch(selectionConfirmedSfx));
    }

    // --- TRANSITION LOGIC (Unchanged) ---
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
        float startAlpha = rootCanvasGroup.alpha;
        
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            rootCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
            yield return null;
        }
        rootCanvasGroup.alpha = 0f;

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
}