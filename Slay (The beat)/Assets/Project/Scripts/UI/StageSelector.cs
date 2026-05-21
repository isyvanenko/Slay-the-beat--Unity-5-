using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Allows the player to select the number of stages in an arcade session using a 3D mesh spinner and UI feedback.
/// </summary>
/// <remarks>
/// This component presents a stage selection interface where the player can choose between 1 and 6 stages.
/// It features:
/// - A 3D object that rotates to reflect the selected stage (60° per stage).
/// - UI buttons and input actions (left/right arrows, submit) for navigation.
/// - Visual feedback: color flash, scale punch on arrows and stage text, gold on success, red on error.
/// - Integration with a timeout restarter to reset inactivity timer.
/// - Persistence of the selected stage count via SessionConfig.MaxStages.
/// - Smooth rotation and scale return animations.
/// 
/// The UI updates in real time, and confirming the selection loads the next scene (e.g., CharacterSelect).
/// </remarks>
public class StageSelector : MonoBehaviour
{
    [Header("Input Actions")]
    /// <summary>Input action for moving left (previous stage).</summary>
    public InputActionReference navigateLeft;
    
    /// <summary>Input action for moving right (next stage).</summary>
    public InputActionReference navigateRight;
    
    /// <summary>Input action for confirming the current stage selection.</summary>
    public InputActionReference submitAction;

    [Header("3D Mesh Settings")]
    /// <summary>Transform of the 3D object that rotates to indicate stage number.</summary>
    public Transform meshTransform;
    
    /// <summary>Speed of the smooth rotation interpolation (degrees per second).</summary>
    public float spinSpeed = 10f;
    
    /// <summary>Target rotation for the mesh (calculated from current stage).</summary>
    private Quaternion targetRotation;
    
    /// <summary>Initial Euler angles of the mesh at startup.</summary>
    private Vector3 initialEulerAngles;

    [Header("UI References")]
    /// <summary>Text displaying the current stage number.</summary>
    public TextMeshProUGUI stageText;
    
    /// <summary>Image used for the main display (e.g., background or panel).</summary>
    public Image mainDisplayImage;
    
    /// <summary>Image for the left arrow button.</summary>
    public Image leftArrowImage;
    
    /// <summary>Image for the right arrow button.</summary>
    public Image rightArrowImage;

    [Header("Settings")]
    /// <summary>Currently selected number of stages (range 1–6).</summary>
    public int currentStages = 1;
    
    /// <summary>Minimum allowed stage count.</summary>
    private const int MIN_STAGES = 1;
    
    /// <summary>Maximum allowed stage count.</summary>
    private const int MAX_STAGES = 6;
    
    /// <summary>Name of the scene to load after confirming the stage selection.</summary>
    public string nextSceneName = "CharacterSelect";

    [Header("Visual Effects")]
    /// <summary>Gold color used for successful navigation feedback.</summary>
    public Color32 goldColor = new Color32(255, 215, 0, 255);
    
    /// <summary>Red color used for error feedback (attempting to go below min or above max).</summary>
    public Color32 errorColor = new Color32(255, 40, 40, 255);
    
    /// <summary>Duration of the color flash effect.</summary>
    public float flashDuration = 0.6f;
    
    /// <summary>Multiplier for the scale punch effect (e.g., 1.2 = 20% larger).</summary>
    public float scalePunchAmount = 1.2f;
    
    /// <summary>Speed at which the scale returns to normal after a punch.</summary>
    public float returnSpeed = 12f;
    
    /// <summary>Reference to a TimeoutRestarter script to reset inactivity timer on navigation.</summary>
    public TimeoutRestarter timeoutScript;

    // Cached original colors and scales
    private Color originalMainColor, originalLeftColor, originalRightColor;
    private Vector3 leftArrowOriginalScale, rightArrowOriginalScale, textOriginalScale;
    private Coroutine mainFlashCr, leftArrowCr, rightArrowCr;

    /// <summary>
    /// Enables input actions and subscribes to their performed events.
    /// </summary>
    private void OnEnable()
    {
        navigateLeft.action.Enable();
        navigateRight.action.Enable();
        submitAction.action.Enable();
        navigateLeft.action.performed += OnNavigateLeft;
        navigateRight.action.performed += OnNavigateRight;
        submitAction.action.performed += OnSubmit;
    }

    /// <summary>
    /// Unsubscribes from input events and disables the actions.
    /// </summary>
    private void OnDisable()
    {
        navigateLeft.action.performed -= OnNavigateLeft;
        navigateRight.action.performed -= OnNavigateRight;
        submitAction.action.performed -= OnSubmit;
    }

    /// <summary>
    /// Initializes component: caches original colors and scales, records initial mesh rotation.
    /// </summary>
    void Start()
    {
        if (mainDisplayImage != null) originalMainColor = mainDisplayImage.color;
        if (leftArrowImage != null) originalLeftColor = leftArrowImage.color;
        if (rightArrowImage != null) originalRightColor = rightArrowImage.color;
        
        if (leftArrowImage != null) leftArrowOriginalScale = leftArrowImage.transform.localScale;
        if (rightArrowImage != null) rightArrowOriginalScale = rightArrowImage.transform.localScale;
        if (stageText != null) textOriginalScale = stageText.transform.localScale;

        if (meshTransform != null)
        {
            initialEulerAngles = meshTransform.eulerAngles;
            targetRotation = meshTransform.rotation;
        }

        UpdateStageUI();
    }

    /// <summary>
    /// Smoothly rotates the mesh toward the target rotation each frame.
    /// </summary>
    void Update()
    {
        if (meshTransform != null)
        {
            meshTransform.rotation = Quaternion.Slerp(meshTransform.rotation, targetRotation, Time.deltaTime * spinSpeed);
        }
    }

    // --- Input System Listeners ---
    private void OnNavigateLeft(InputAction.CallbackContext context) { if (context.ReadValue<float>() > 0) HandleNavigation(true); }
    private void OnNavigateRight(InputAction.CallbackContext context) { if (context.ReadValue<float>() > 0) HandleNavigation(false); }
    private void OnSubmit(InputAction.CallbackContext context) { if (context.ReadValue<float>() > 0) ConfirmSelection(); }

    // --- UI Button Listeners (Assign these in the Inspector) ---
    /// <summary>Called by UI left button click.</summary>
    public void OnLeftClick() => HandleNavigation(true);
    
    /// <summary>Called by UI right button click.</summary>
    public void OnRightClick() => HandleNavigation(false);
    
    /// <summary>Called by UI confirm button click.</summary>
    public void OnSubmitClick() => ConfirmSelection();

    /// <summary>
    /// Handles stage navigation (increase or decrease).
    /// </summary>
    /// <param name="isGoingLeft">True to move left (decrease stage count), false to move right (increase).</param>
    /// <remarks>
    /// If the move would go outside the valid range, an error flash is shown and the stage count does not change.
    /// Otherwise, updates stage count, UI text, target mesh rotation, and applies a success flash.
    /// Also resets the timeout restarter if present.
    /// </remarks>
    void HandleNavigation(bool isGoingLeft)
    {
        if (timeoutScript != null) timeoutScript.StopTimer();

        int amount = isGoingLeft ? -1 : 1;
        int targetValue = currentStages + amount;

        bool isError = (isGoingLeft && targetValue < MIN_STAGES) || (!isGoingLeft && targetValue > MAX_STAGES);
        Color flashColor = isError ? (Color)errorColor : (Color)goldColor;

        if (!isError)
        {
            currentStages = targetValue;
            UpdateStageUI();
            
            float targetY = initialEulerAngles.y + ((currentStages - 1) * 60f);
            targetRotation = Quaternion.Euler(initialEulerAngles.x, targetY, initialEulerAngles.z);

            if (stageText != null) stageText.transform.localScale = textOriginalScale * scalePunchAmount;
        }

        ApplyVisualFeedback(isGoingLeft, flashColor);
    }

    /// <summary>
    /// Applies visual feedback for a navigation action (scale punch and color flash).
    /// </summary>
    /// <param name="isLeft">True if the feedback is for the left arrow.</param>
    /// <param name="color">Color to flash (gold on success, red on error).</param>
    void ApplyVisualFeedback(bool isLeft, Color color)
    {
        Image arrow = isLeft ? leftArrowImage : rightArrowImage;
        Vector3 arrowOrig = isLeft ? leftArrowOriginalScale : rightArrowOriginalScale;
        
        if (arrow != null) arrow.transform.localScale = arrowOrig * scalePunchAmount;

        ResetColors();

        if (mainDisplayImage != null) 
            mainFlashCr = StartCoroutine(FadeColor(mainDisplayImage, originalMainColor, color));
        
        if (arrow != null)
        {
            if (isLeft) leftArrowCr = StartCoroutine(FadeColor(leftArrowImage, originalLeftColor, color));
            else rightArrowCr = StartCoroutine(FadeColor(rightArrowImage, originalRightColor, color));
        }
    }

    /// <summary>
    /// Stops any ongoing color flash coroutines and resets all affected images to their original colors.
    /// </summary>
    private void ResetColors()
    {
        if (mainFlashCr != null) StopCoroutine(mainFlashCr);
        if (leftArrowCr != null) StopCoroutine(leftArrowCr);
        if (rightArrowCr != null) StopCoroutine(rightArrowCr);

        if (mainDisplayImage != null) mainDisplayImage.color = originalMainColor;
        if (leftArrowImage != null) leftArrowImage.color = originalLeftColor;
        if (rightArrowImage != null) rightArrowImage.color = originalRightColor;
    }

    /// <summary>
    /// Coroutine that smoothly fades an image from a flash color back to its original color.
    /// </summary>
    /// <param name="img">The Image component to fade.</param>
    /// <param name="original">The original color to return to.</param>
    /// <param name="flash">The flash color to start from.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private IEnumerator FadeColor(Image img, Color original, Color flash)
    {
        float elapsed = 0;
        img.color = flash;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            img.color = Color.Lerp(flash, original, elapsed / flashDuration);
            yield return null;
        }
        img.color = original;
    }

    /// <summary>Updates the stage text UI to show the current stage count.</summary>
    void UpdateStageUI() => stageText.text = currentStages.ToString();

    /// <summary>
    /// Smoothly returns the stage text and arrow scales to their original sizes after a punch.
    /// </summary>
    void LateUpdate()
    {
        float step = Time.deltaTime * returnSpeed;
        
        if (stageText != null) 
            stageText.transform.localScale = Vector3.Lerp(stageText.transform.localScale, textOriginalScale, step);
        
        if (leftArrowImage != null) 
            leftArrowImage.transform.localScale = Vector3.Lerp(leftArrowImage.transform.localScale, leftArrowOriginalScale, step);
        if (rightArrowImage != null) 
            rightArrowImage.transform.localScale = Vector3.Lerp(rightArrowImage.transform.localScale, rightArrowOriginalScale, step);
    }

    /// <summary>
    /// Confirms the selected stage count, saves it to SessionConfig, and loads the next scene.
    /// </summary>
    public void ConfirmSelection()
    {
        SessionConfig.MaxStages = currentStages;
        SceneManager.LoadScene(nextSceneName);
    }
}