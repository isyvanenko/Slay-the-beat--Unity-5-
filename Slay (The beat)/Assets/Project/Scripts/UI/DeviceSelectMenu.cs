using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

/// <summary>
/// Complete standalone device selection menu - no MenuSelector needed
/// </summary>
public class DeviceSelectMenu : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button[] buttons;  // Assign your 3 buttons in inspector
    public TMP_Text[] buttonTexts;
    public TMP_Text descriptionText;
    public string[] descriptions;
    
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    
    [Header("Navigation")]
    public float inputCooldown = 0.15f;
    
    [Header("References")]
    public GameObject calibrationCanvas;
    public PlayerInput playerInput;
    public int currentPlayerIndex = 0;
    public string songSelectionScene = "SongSelectionScene";
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip selectSound;
    public AudioClip navigateSound;
    
    private int currentIndex = 0;
    private float lastInputTime;
    private bool isProcessing = false;
    private InputActions input;
    private InputAction left;
    private InputAction right;
    private InputAction select;
    
    void Awake()
    {
        input = new InputActions();
        left = input.UI.NavigateLeft;
        right = input.UI.NavigateRight;
        select = input.UI.Select;
    }
    
    void OnEnable()
    {
        left.performed += OnNavigate;
        right.performed += OnNavigate;
        select.performed += OnSelect;
        
        left.Enable();
        right.Enable();
        select.Enable();
        
        currentIndex = 0;
        isProcessing = false;
        UpdateButtonHighlights();
        UpdateDescription();
    }
    
    void OnDisable()
    {
        left.performed -= OnNavigate;
        right.performed -= OnNavigate;
        select.performed -= OnSelect;
        
        left.Disable();
        right.Disable();
        select.Disable();
    }
    
    void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (isProcessing) return;
        if (Time.unscaledTime - lastInputTime < inputCooldown) return;
        
        float direction = ctx.ReadValue<float>();
        if (direction > 0)
            currentIndex = (currentIndex + 1) % buttons.Length;
        else if (direction < 0)
            currentIndex = (currentIndex - 1 + buttons.Length) % buttons.Length;
        else return;
        
        lastInputTime = Time.unscaledTime;
        
        if (audioSource != null && navigateSound != null)
            audioSource.PlayOneShot(navigateSound);
        
        UpdateButtonHighlights();
        UpdateDescription();
    }
    
    void OnSelect(InputAction.CallbackContext ctx)
    {
        if (isProcessing) return;
        
        if (audioSource != null && selectSound != null)
            audioSource.PlayOneShot(selectSound);
        
        StartCoroutine(ProcessSelection(currentIndex));
    }
    
    void UpdateButtonHighlights()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            var colors = buttons[i].colors;
            colors.normalColor = (i == currentIndex) ? selectedColor : normalColor;
            buttons[i].colors = colors;
        }
    }
    
    void UpdateDescription()
    {
        if (descriptionText != null && descriptions.Length > currentIndex)
        {
            descriptionText.text = descriptions[currentIndex];
        }
    }
    
    IEnumerator ProcessSelection(int index)
    {
        isProcessing = true;
        
        // Button 0 = Arrow Keys
        if (index == 0)
        {
            AssignArrowKeys();
            yield return new WaitForSecondsRealtime(0.1f);
            LoadSongSelection();
        }
        // Button 1 = Calibration
        else if (index == 1)
        {
            OpenCalibration();
        }
        // Button 2 = WASD Keys
        else if (index == 2)
        {
            AssignWASDKeys();
            yield return new WaitForSecondsRealtime(0.1f);
            LoadSongSelection();
        }
        
        isProcessing = false;
    }
    
    void AssignArrowKeys()
    {
        if (playerInput == null) return;
        
        var actions = playerInput.actions;
        actions["Up"].ApplyBindingOverride("<Keyboard>/upArrow");
        actions["Down"].ApplyBindingOverride("<Keyboard>/downArrow");
        actions["Left"].ApplyBindingOverride("<Keyboard>/leftArrow");
        actions["Right"].ApplyBindingOverride("<Keyboard>/rightArrow");
        
        string json = actions.SaveBindingOverridesAsJson();
        if (currentPlayerIndex == 0) SessionConfig.P1Bindings = json;
        else SessionConfig.P2Bindings = json;
        
        Debug.Log("Assigned Arrow Keys to Player " + (currentPlayerIndex + 1));
    }
    
    void AssignWASDKeys()
    {
        if (playerInput == null) return;
        
        var actions = playerInput.actions;
        actions["Up"].ApplyBindingOverride("<Keyboard>/w");
        actions["Down"].ApplyBindingOverride("<Keyboard>/s");
        actions["Left"].ApplyBindingOverride("<Keyboard>/a");
        actions["Right"].ApplyBindingOverride("<Keyboard>/d");
        
        string json = actions.SaveBindingOverridesAsJson();
        if (currentPlayerIndex == 0) SessionConfig.P1Bindings = json;
        else SessionConfig.P2Bindings = json;
        
        Debug.Log("Assigned WASD Keys to Player " + (currentPlayerIndex + 1));
    }
    
    void OpenCalibration()
    {
        gameObject.SetActive(false);
        if (calibrationCanvas != null)
        {
            var setup = calibrationCanvas.GetComponent<DeviceSetupMenu>();
            if (setup != null)
            {
                setup.playerIndexToAssign = currentPlayerIndex;
                setup.ResetCalibration();
            }
            calibrationCanvas.SetActive(true);
        }
    }
    
    void LoadSongSelection()
    {
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadScene(songSelectionScene);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(songSelectionScene);
    }
}