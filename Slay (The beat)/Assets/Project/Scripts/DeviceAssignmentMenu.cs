using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Simple menu for device selection
/// </summary>
public class DeviceAssignmentMenu : MonoBehaviour
{
    [Header("Menu References")]
    public MenuSelector menuSelector;
    public CanvasGroup canvasGroup;
    
    [Header("Next Menus")]
    public GameObject calibrationCanvas;
    
    [Header("Player Configuration")]
    public int currentPlayerIndex = 0;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip selectionSound;
    
    [Header("Direct Assignment References")]
    public PlayerInput player1Input;
    public PlayerInput player2Input;
    
    private bool isTransitioning = false;
    private const int BUTTON_ARROW_KEYS = 0;
    private const int BUTTON_CALIBRATION = 1;
    private const int BUTTON_WASD_KEYS = 2;
    
    void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if (menuSelector != null)
        {
            menuSelector.onSelectIndex.RemoveAllListeners();
            menuSelector.onSelectIndex.AddListener(OnMenuItemSelected);
        }
    }
    
    void OnEnable()
    {
        isTransitioning = false;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        UpdateMenuTexts();
    }
    
    void UpdateMenuTexts()
    {
        if (menuSelector == null || menuSelector.selectionTexts == null) return;
        
        string playerPrefix = (currentPlayerIndex == 0) ? "PLAYER 1" : "PLAYER 2";
        
        if (menuSelector.selectionTexts.Length >= 3)
        {
            menuSelector.selectionTexts[0] = $"{playerPrefix}: ARROW KEYS\nUse arrow keys";
            menuSelector.selectionTexts[1] = $"{playerPrefix}: CALIBRATION\nCustomize controls";
            menuSelector.selectionTexts[2] = $"{playerPrefix}: WASD KEYS\nUse WASD keys";
        }
    }
    
    void OnMenuItemSelected(int index)
    {
        if (isTransitioning) return;
        
        if (audioSource != null && selectionSound != null)
            audioSource.PlayOneShot(selectionSound);
        
        StartCoroutine(HandleSelection(index));
    }
    
    IEnumerator HandleSelection(int index)
    {
        isTransitioning = true;
        
        // Disable menu
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        yield return new WaitForSecondsRealtime(0.2f);
        
        if (index == BUTTON_ARROW_KEYS)
        {
            // Assign Arrow Keys and start game
            AssignArrowKeys();
            StartGame();
        }
        else if (index == BUTTON_WASD_KEYS)
        {
            // Assign WASD Keys and start game
            AssignWASDKeys();
            StartGame();
        }
        else if (index == BUTTON_CALIBRATION)
        {
            // Go to calibration
            OpenCalibration();
        }
    }
    
    void AssignArrowKeys()
    {
        PlayerInput targetInput = (currentPlayerIndex == 0) ? player1Input : player2Input;
        if (targetInput == null) return;
        
        var actions = targetInput.actions;
        actions["Up"].ApplyBindingOverride("<Keyboard>/upArrow");
        actions["Down"].ApplyBindingOverride("<Keyboard>/downArrow");
        actions["Left"].ApplyBindingOverride("<Keyboard>/leftArrow");
        actions["Right"].ApplyBindingOverride("<Keyboard>/rightArrow");
        
        string overridesJson = actions.SaveBindingOverridesAsJson();
        if (currentPlayerIndex == 0)
        {
            SessionConfig.P1Bindings = overridesJson;
            SessionConfig.Player1Device = Keyboard.current;
            SessionConfig.P1DeviceType = "Keyboard-ArrowKeys";
        }
        else
        {
            SessionConfig.P2Bindings = overridesJson;
            SessionConfig.Player2Device = Keyboard.current;
            SessionConfig.P2DeviceType = "Keyboard-ArrowKeys";
        }
    }
    
    void AssignWASDKeys()
    {
        PlayerInput targetInput = (currentPlayerIndex == 0) ? player1Input : player2Input;
        if (targetInput == null) return;
        
        var actions = targetInput.actions;
        actions["Up"].ApplyBindingOverride("<Keyboard>/w");
        actions["Down"].ApplyBindingOverride("<Keyboard>/s");
        actions["Left"].ApplyBindingOverride("<Keyboard>/a");
        actions["Right"].ApplyBindingOverride("<Keyboard>/d");
        
        string overridesJson = actions.SaveBindingOverridesAsJson();
        if (currentPlayerIndex == 0)
        {
            SessionConfig.P1Bindings = overridesJson;
            SessionConfig.Player1Device = Keyboard.current;
            SessionConfig.P1DeviceType = "Keyboard-WASD";
        }
        else
        {
            SessionConfig.P2Bindings = overridesJson;
            SessionConfig.Player2Device = Keyboard.current;
            SessionConfig.P2DeviceType = "Keyboard-WASD";
        }
    }
    
    void OpenCalibration()
    {
        gameObject.SetActive(false);
        if (calibrationCanvas != null)
        {
            var setupMenu = calibrationCanvas.GetComponent<DeviceSetupMenu>();
            if (setupMenu != null)
            {
                setupMenu.playerIndexToAssign = currentPlayerIndex;
                setupMenu.ResetCalibration();
            }
            calibrationCanvas.SetActive(true);
        }
    }
    
    void StartGame()
    {
        // Load your game scene
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.LoadScene("SongSelection");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("SongSelection");
        }
    }
}