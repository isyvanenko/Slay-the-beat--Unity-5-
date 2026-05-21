using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages a timeout countdown that restarts the scene after a period of inactivity.
/// </summary>
/// <remarks>
/// This component tracks a countdown timer and provides visual feedback:
/// - A countdown text (MM:SS or seconds) and a slider.
/// - Low‑time warning with a pulsing alpha effect.
/// - Gold colour pulses every 5 seconds (optional).
/// - Resets the timer when activity is detected (e.g., user input via external calls).
/// If the timer reaches zero, it loads a specified scene (e.g., the start menu).
/// </remarks>
public class TimeoutRestarter : MonoBehaviour
{
    [Header("Timer Settings")]
    /// <summary>Total duration of the timeout countdown (seconds).</summary>
    public float totalTimeInSeconds = 30f;
    /// <summary>Scene to load when the timeout expires.</summary>
    public string sceneToLoadOnTimeout = "StartScene";

    [Header("UI Components")]
    /// <summary>Text component that displays the remaining time.</summary>
    public TextMeshProUGUI countdownText;
    /// <summary>Slider that visualises the remaining time.</summary>
    public Slider timerSlider;
    /// <summary>Image used as the fill of the slider (for colour feedback).</summary>
    public Image sliderFillImage;
    /// <summary>Optional GameObject shown when time is unlimited (e.g., totalTimeInSeconds > 1000).</summary>
    public GameObject unlimitedtime;

    [Header("Low Time Feedback")]
    [Tooltip("The image/object that will fade in and out during low time.")]
    /// <summary>CanvasGroup that pulses its alpha when the remaining time is below lowTimeThreshold.</summary>
    public CanvasGroup lowTimeWarningGroup; 
    /// <summary>Colour used for text and slider fill during low‑time warning.</summary>
    public Color32 lowTimeColor = new Color32(255, 29, 0, 255);
    /// <summary>Time (seconds) below which the low‑time warning becomes active.</summary>
    public float lowTimeThreshold = 10f;
    /// <summary>Speed of the low‑time pulsing effect (higher = faster pulse).</summary>
    public float fadeSpeed = 5f; // Higher is faster pulsing

    [Header("Interval Pulse")]
    /// <summary>Colour used for the gold pulse that occurs every 5 seconds while above lowTimeThreshold.</summary>
    public Color32 pulseColor = new Color32(255, 215, 0, 255); // Gold
    /// <summary>Duration of the gold pulse effect.</summary>
    public float pulseDuration = 0.6f;

    private float currentTime;
    private bool isTimerRunning = false;
    private bool isPulsingGold = false;
    private int lastPulseTriggerSecond = -1; 
    
    private Color defaultTextColor;
    private Color defaultSliderColor;

    /// <summary>
    /// Initialises the timer, UI elements, and colour defaults.
    /// </summary>
    void Start()
    {
        if (countdownText != null) defaultTextColor = countdownText.color;
        if (sliderFillImage != null) defaultSliderColor = sliderFillImage.color;

        if (timerSlider != null)
        {
            timerSlider.maxValue = totalTimeInSeconds;
            timerSlider.value = totalTimeInSeconds;
        }

        // Ensure the warning starts invisible
        if (lowTimeWarningGroup != null) lowTimeWarningGroup.alpha = 0;

        ResetAndStartTimer();

        if (unlimitedtime != null)
            unlimitedtime.SetActive(totalTimeInSeconds > 1000);
    }

    /// <summary>
    /// Updates the countdown, handles interval pulses, low‑time visuals, and timeout.
    /// </summary>
    void Update()
    {
        if (!isTimerRunning) return;

        currentTime -= Time.deltaTime;

        // 1. INTERVAL PULSE (Every 5 seconds, only if NOT in low time)
        int currentSecond = Mathf.CeilToInt(currentTime);
        if (currentTime > lowTimeThreshold && currentSecond % 5 == 0 && currentSecond != lastPulseTriggerSecond)
        {
            lastPulseTriggerSecond = currentSecond;
            StartCoroutine(PulseGoldRoutine());
        }

        // 2. LOW TIME FADE (The "Heartbeat" effect)
        HandleLowTimeVisuals();

        if (currentTime <= 0)
        {
            HandleTimeout();
        }

        UpdateUI();
    }

    /// <summary>
    /// Handles the pulsing alpha of the low‑time warning CanvasGroup.
    /// </summary>
    /// <remarks>
    /// Uses a sine wave (with absolute value) to create a smooth fade‑in/fade‑out loop.
    /// </remarks>
    private void HandleLowTimeVisuals()
    {
        if (currentTime < lowTimeThreshold && currentTime > 0)
        {
            if (lowTimeWarningGroup != null)
            {
                // Sine wave oscillates between -1 and 1. 
                // We use Abs to make it stay between 0 and 1 for alpha.
                float alpha = Mathf.Abs(Mathf.Sin(Time.time * fadeSpeed));
                lowTimeWarningGroup.alpha = alpha;
            }
        }
        else
        {
            // Ensure it's hidden if we aren't in low time
            if (lowTimeWarningGroup != null) lowTimeWarningGroup.alpha = 0;
        }
    }

    /// <summary>
    /// Updates the countdown text, slider value, and colours.
    /// </summary>
    private void UpdateUI()
    {
        if (countdownText != null)
        {
            int timeToDisplay = Mathf.CeilToInt(currentTime);
            countdownText.text = Mathf.Clamp(timeToDisplay, 0, 99).ToString("D2");
            
            if (!isPulsingGold)
                countdownText.color = (currentTime < lowTimeThreshold) ? (Color)lowTimeColor : defaultTextColor;
        }

        if (timerSlider != null) timerSlider.value = currentTime;

        if (sliderFillImage != null && !isPulsingGold)
            sliderFillImage.color = (currentTime < lowTimeThreshold) ? (Color)lowTimeColor : defaultSliderColor;
    }

    /// <summary>
    /// Coroutine that performs a gold pulse effect on the countdown text and slider fill.
    /// </summary>
    /// <returns>IEnumerator for the pulse sequence.</returns>
    private IEnumerator PulseGoldRoutine()
    {
        isPulsingGold = true;
        float elapsed = 0;
        float halfDist = pulseDuration / 2;

        // Fade from default to gold
        while (elapsed < halfDist)
        {
            elapsed += Time.deltaTime;
            ApplyPulseColor(Color.Lerp(defaultTextColor, pulseColor, elapsed / halfDist));
            yield return null;
        }

        // Fade from gold back to default
        elapsed = 0;
        while (elapsed < halfDist)
        {
            elapsed += Time.deltaTime;
            ApplyPulseColor(Color.Lerp(pulseColor, defaultTextColor, elapsed / halfDist));
            yield return null;
        }

        isPulsingGold = false;
    }

    /// <summary>
    /// Applies a colour to both the countdown text and the slider fill image.
    /// </summary>
    /// <param name="c">The colour to apply.</param>
    private void ApplyPulseColor(Color c)
    {
        if (countdownText != null) countdownText.color = c;
        if (sliderFillImage != null) sliderFillImage.color = c;
    }

    /// <summary>
    /// Called when the timer expires. Stops the timer and loads the timeout scene.
    /// </summary>
    private void HandleTimeout()
    {
        currentTime = 0;
        isTimerRunning = false;
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadScene(sceneToLoadOnTimeout);
        else
            SceneManager.LoadScene(sceneToLoadOnTimeout);
    }

    /// <summary>
    /// Resets the timer to its full duration and starts it (if it was stopped).
    /// </summary>
    public void ResetAndStartTimer()
    {
        currentTime = totalTimeInSeconds;
        isTimerRunning = true;
    }

    /// <summary>
    /// Stops the timer without resetting it.
    /// </summary>
    public void StopTimer() => isTimerRunning = false;
}