using UnityEngine;
using System.Collections;

/// <summary>
/// Makes a CanvasGroup pulse its alpha value in a continuous or finite loop.
/// </summary>
/// <remarks>
/// This component creates an attention-grabbing pulsing effect by smoothly fading a UI element
/// between minimum and maximum alpha values. It's ideal for highlighting buttons, notifications,
/// or any UI element that needs to draw the player's attention.
/// 
/// Key Features:
/// - Configurable pulse duration, hold time, and pause between cycles
/// - Smooth step easing for natural animation
/// - Support for finite or infinite pulsing (pulseCount = -1)
/// - Automatic start on Awake (optional)
/// - Public methods to control pulsing from other scripts
/// - Returns to original alpha when stopped or after finite pulses
/// 
/// The script requires a CanvasGroup component on the same GameObject.
/// </remarks>
[RequireComponent(typeof(CanvasGroup))]
public class PulseAttention : MonoBehaviour
{
    [Header("Pulse Settings")]
    /// <summary>Time in seconds for each fade phase (in or out).</summary>
    [SerializeField] private float pulseTime = 0.5f;
    
    /// <summary>Time in seconds to hold at maximum visibility after fading in.</summary>
    [SerializeField] private float holdTime = 0.3f;
    
    /// <summary>Minimum alpha value during the pulse (transparent level).</summary>
    [SerializeField] private float minAlpha = 0.1f;
    
    /// <summary>Maximum alpha value during the pulse (fully visible).</summary>
    [SerializeField] private float maxAlpha = 1f;
    
    /// <summary>Pause duration in seconds between complete pulse cycles.</summary>
    [SerializeField] private float pauseBetween = 0.2f;
    
    /// <summary>If true, starts pulsing automatically when the script starts.</summary>
    [SerializeField] private bool startOnAwake = true;
    
    /// <summary>
    /// Number of pulse cycles to perform. Set to -1 for infinite looping.
    /// </summary>
    [SerializeField] private int pulseCount = -1;
    
    /// <summary>CanvasGroup component controlling the UI element's alpha.</summary>
    private CanvasGroup canvasGroup;
    
    /// <summary>Reference to the active pulsing coroutine.</summary>
    private Coroutine pulseCoroutine;
    
    /// <summary>Original alpha value of the CanvasGroup before pulsing started.</summary>
    private float originalAlpha;

    /// <summary>
    /// Initializes the component, retrieves the CanvasGroup, and stores the original alpha.
    /// </summary>
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

    /// <summary>
    /// Starts pulsing automatically if startOnAwake is true.
    /// </summary>
    void Start()
    {
        if (startOnAwake)
        {
            StartPulsing();
        }
    }

    /// <summary>
    /// Starts the pulsing effect, stopping any existing pulse.
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
    /// Stops the pulsing effect and resets the CanvasGroup alpha to its original value.
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
    /// Stops the pulsing effect without resetting the alpha value.
    /// </summary>
    /// <remarks>
    /// The CanvasGroup remains at whatever alpha it had when pulsing stopped.
    /// Useful for fading out or transitioning to another animation.
    /// </remarks>
    public void StopPulsingMaintainAlpha()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine that performs the pulsing sequence (fade in, hold, fade out, pause).
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    /// <remarks>
    /// The sequence:
    /// 1. Fade from minAlpha to maxAlpha over pulseTime
    /// 2. Hold at maxAlpha for holdTime
    /// 3. Fade from maxAlpha to minAlpha over pulseTime
    /// 4. Pause for pauseBetween
    /// 5. Repeat until pulseCount is reached (or indefinitely if pulseCount == -1)
    /// 
    /// After the final pulse, the alpha is reset to the original value.
    /// </remarks>
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

    /// <summary>
    /// Coroutine that smoothly fades the CanvasGroup alpha between two values.
    /// </summary>
    /// <param name="startAlpha">Starting alpha value.</param>
    /// <param name="endAlpha">Target alpha value.</param>
    /// <param name="duration">Duration of the fade in seconds.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    /// <remarks>
    /// Uses SmoothStep interpolation for a more natural, non-linear fade curve.
    /// </remarks>
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

    /// <summary>Sets the duration of each fade phase.</summary>
    /// <param name="time">New pulse time in seconds.</param>
    public void SetPulseTime(float time)
    {
        pulseTime = time;
    }

    /// <summary>Sets the hold time at maximum alpha.</summary>
    /// <param name="time">New hold time in seconds.</param>
    public void SetHoldTime(float time)
    {
        holdTime = time;
    }

    /// <summary>Sets the minimum alpha value for the pulse.</summary>
    /// <param name="alpha">Clamped between 0 and 1.</param>
    public void SetMinAlpha(float alpha)
    {
        minAlpha = Mathf.Clamp01(alpha);
    }

    /// <summary>Sets the maximum alpha value for the pulse.</summary>
    /// <param name="alpha">Clamped between 0 and 1.</param>
    public void SetMaxAlpha(float alpha)
    {
        maxAlpha = Mathf.Clamp01(alpha);
    }

    /// <summary>Sets the pause time between pulse cycles.</summary>
    /// <param name="pause">New pause duration in seconds.</param>
    public void SetPauseBetween(float pause)
    {
        pauseBetween = pause;
    }

    /// <summary>Sets the number of pulse cycles to perform.</summary>
    /// <param name="count">Use -1 for infinite, or any non-negative integer.</param>
    public void SetPulseCount(int count)
    {
        pulseCount = count;
    }

    /// <summary>Checks if the pulsing coroutine is currently running.</summary>
    /// <returns>True if pulsing, false otherwise.</returns>
    public bool IsPulsing()
    {
        return pulseCoroutine != null;
    }
}