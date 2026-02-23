using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class PulseAttention : MonoBehaviour
{
    [Header("Pulse Settings")]
    [SerializeField] private float pulseTime = 0.5f;      // Time for each pulse phase
    [SerializeField] private float holdTime = 0.3f;       // Time to hold at peak visibility
    [SerializeField] private float minAlpha = 0.1f;       // Minimum transparency (fade to)
    [SerializeField] private float maxAlpha = 1f;          // Maximum transparency (fully visible)
    [SerializeField] private float pauseBetween = 0.2f;    // Pause between pulse cycles
    [SerializeField] private bool startOnAwake = true;     // Start pulsing automatically
    [SerializeField] private int pulseCount = -1;          // -1 for infinite, or set number of pulses
    
    private CanvasGroup canvasGroup;
    private Coroutine pulseCoroutine;
    private float originalAlpha;

    void Awake()
    {
        // Get the CanvasGroup component
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            Debug.LogError("PulseAttention script requires a CanvasGroup component!");
            enabled = false;
            return;
        }
        
        // Store the original alpha value
        originalAlpha = canvasGroup.alpha;
    }

    void Start()
    {
        if (startOnAwake)
        {
            StartPulsing();
        }
    }

    /// <summary>
    /// Start the pulsing effect
    /// </summary>
    public void StartPulsing()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        
        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    /// <summary>
    /// Stop the pulsing effect and reset to original alpha
    /// </summary>
    public void StopPulsing()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        // Reset to original alpha
        canvasGroup.alpha = originalAlpha;
    }

    /// <summary>
    /// Stop pulsing but maintain current alpha
    /// </summary>
    public void StopPulsingMaintainAlpha()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    private IEnumerator PulseRoutine()
    {
        int pulsesDone = 0;
        
        while (pulseCount == -1 || pulsesDone < pulseCount)
        {
            // Pulse in (fade to max alpha)
            yield return StartCoroutine(FadeAlpha(minAlpha, maxAlpha, pulseTime));
            
            // Hold at max alpha
            yield return new WaitForSeconds(holdTime);
            
            // Pulse out (fade to min alpha)
            yield return StartCoroutine(FadeAlpha(maxAlpha, minAlpha, pulseTime));
            
            // Pause before next pulse
            yield return new WaitForSeconds(pauseBetween);
            
            pulsesDone++;
        }
        
        // Reset to original alpha when done
        canvasGroup.alpha = originalAlpha;
        pulseCoroutine = null;
    }

    private IEnumerator FadeAlpha(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Smooth step for more natural animation
            t = Mathf.SmoothStep(0, 1, t);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            
            yield return null;
        }
        
        // Ensure we end at the exact target value
        canvasGroup.alpha = endAlpha;
    }

    // Optional: Public methods to control the pulse from other scripts
    public void SetPulseTime(float time)
    {
        pulseTime = time;
    }

    public void SetHoldTime(float time)
    {
        holdTime = time;
    }

    public void SetMinAlpha(float alpha)
    {
        minAlpha = Mathf.Clamp01(alpha);
    }

    public void SetMaxAlpha(float alpha)
    {
        maxAlpha = Mathf.Clamp01(alpha);
    }

    public void SetPauseBetween(float pause)
    {
        pauseBetween = pause;
    }

    public void SetPulseCount(int count)
    {
        pulseCount = count;
    }

    public bool IsPulsing()
    {
        return pulseCoroutine != null;
    }
}