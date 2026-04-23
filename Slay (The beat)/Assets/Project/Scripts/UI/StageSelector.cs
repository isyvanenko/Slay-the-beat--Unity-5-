using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class StageSelector : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference navigateLeft;
    public InputActionReference navigateRight;
    public InputActionReference submitAction;

    [Header("3D Mesh Settings")]
    public Transform meshTransform;
    public float spinSpeed = 10f;
    private Quaternion targetRotation;
    private Vector3 initialEulerAngles;

    [Header("UI References")]
    public TextMeshProUGUI stageText;
    public Image mainDisplayImage; 
    public Image leftArrowImage; 
    public Image rightArrowImage;

    [Header("Settings")]
    public int currentStages = 1;
    private const int MIN_STAGES = 1;
    private const int MAX_STAGES = 6;
    public string nextSceneName = "CharacterSelect";

    [Header("Visual Effects")]
    public Color32 goldColor = new Color32(255, 215, 0, 255);
    public Color32 errorColor = new Color32(255, 40, 40, 255);
    public float flashDuration = 0.6f;
    public float scalePunchAmount = 1.2f;
    public float returnSpeed = 12f;
    
    public TimeoutRestarter timeoutScript;

    private Color originalMainColor, originalLeftColor, originalRightColor;
    private Vector3 leftArrowOriginalScale, rightArrowOriginalScale, textOriginalScale;
    private Coroutine mainFlashCr, leftArrowCr, rightArrowCr;

    private void OnEnable()
    {
        navigateLeft.action.Enable();
        navigateRight.action.Enable();
        submitAction.action.Enable();
        navigateLeft.action.performed += OnNavigateLeft;
        navigateRight.action.performed += OnNavigateRight;
        submitAction.action.performed += OnSubmit;
    }

    private void OnDisable()
    {
        navigateLeft.action.performed -= OnNavigateLeft;
        navigateRight.action.performed -= OnNavigateRight;
        submitAction.action.performed -= OnSubmit;
    }

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

    // --- NEW: UI Button Listeners (Assign these in the Inspector) ---
    public void OnLeftClick() => HandleNavigation(true);
    public void OnRightClick() => HandleNavigation(false);
    public void OnSubmitClick() => ConfirmSelection();

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

    private void ResetColors()
    {
        if (mainFlashCr != null) StopCoroutine(mainFlashCr);
        if (leftArrowCr != null) StopCoroutine(leftArrowCr);
        if (rightArrowCr != null) StopCoroutine(rightArrowCr);

        if (mainDisplayImage != null) mainDisplayImage.color = originalMainColor;
        if (leftArrowImage != null) leftArrowImage.color = originalLeftColor;
        if (rightArrowImage != null) rightArrowImage.color = originalRightColor;
    }

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

    void UpdateStageUI() => stageText.text = currentStages.ToString();

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

    public void ConfirmSelection()
    {
        SessionConfig.MaxStages = currentStages;
        SceneManager.LoadScene(nextSceneName);
    }
}