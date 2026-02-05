using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TimeoutRestarter : MonoBehaviour
{
    [Header("Timer Settings")]
    public float totalTimeInSeconds = 30f;
    public string sceneToLoadOnTimeout = "StartScene";

    [Header("UI Components")]
    public TextMeshProUGUI countdownText;
    public Slider timerSlider;
    public Image sliderFillImage;
    public GameObject unlimitedtime;

    [Header("Low Time Feedback")]
    [Tooltip("The image/object that will fade in and out during low time.")]
    public CanvasGroup lowTimeWarningGroup; 
    public Color32 lowTimeColor = new Color32(255, 29, 0, 255);
    public float lowTimeThreshold = 10f;
    public float fadeSpeed = 5f; // Higher is faster pulsing

    [Header("Interval Pulse")]
    public Color32 pulseColor = new Color32(255, 215, 0, 255); // Gold
    public float pulseDuration = 0.6f;

    private float currentTime;
    private bool isTimerRunning = false;
    private bool isPulsingGold = false;
    private int lastPulseTriggerSecond = -1; 
    
    private Color defaultTextColor;
    private Color defaultSliderColor;

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

    private IEnumerator PulseGoldRoutine()
    {
        isPulsingGold = true;
        float elapsed = 0;
        float halfDist = pulseDuration / 2;

        while (elapsed < halfDist)
        {
            elapsed += Time.deltaTime;
            ApplyPulseColor(Color.Lerp(defaultTextColor, pulseColor, elapsed / halfDist));
            yield return null;
        }

        elapsed = 0;
        while (elapsed < halfDist)
        {
            elapsed += Time.deltaTime;
            ApplyPulseColor(Color.Lerp(pulseColor, defaultTextColor, elapsed / halfDist));
            yield return null;
        }

        isPulsingGold = false;
    }

    private void ApplyPulseColor(Color c)
    {
        if (countdownText != null) countdownText.color = c;
        if (sliderFillImage != null) sliderFillImage.color = c;
    }

    private void HandleTimeout()
    {
        currentTime = 0;
        isTimerRunning = false;
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadScene(sceneToLoadOnTimeout);
        else
            SceneManager.LoadScene(sceneToLoadOnTimeout);
    }

    public void ResetAndStartTimer()
    {
        currentTime = totalTimeInSeconds;
        isTimerRunning = true;
    }

    public void StopTimer() => isTimerRunning = false;
}