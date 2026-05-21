using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Continuously pulses a TextMeshProUGUI component by fading it in and out in a loop.
/// </summary>
/// <remarks>
/// This component cycles the alpha of a TMP text between 0 and 1 using configurable fade durations
/// and an optional animation curve. It's useful for attention‑grabbing UI elements like
/// prompts, countdowns, or notifications.
/// 
/// The fading loop starts when the GameObject becomes active (OnEnable) and stops when disabled.
/// </remarks>
public class TMPTextPulse : MonoBehaviour
{
    [Header("Text Settings")]
    /// <summary>The TextMeshProUGUI component to animate. If unassigned, tries to get one on Awake.</summary>
    [SerializeField] private TextMeshProUGUI tmpText;
    
    /// <summary>Duration (seconds) of the fade‑in phase (from transparent to opaque).</summary>
    [SerializeField] private float fadeInDuration = 1f;
    
    /// <summary>Duration (seconds) of the fade‑out phase (from opaque to transparent).</summary>
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Animation Curve (optional)")]
    /// <summary>Optional animation curve to control the fade progression (over time).</summary>
    /// <remarks>
    /// The curve is evaluated on the x‑axis from 0 to 1, where 0 = start of fade, 1 = end.
    /// The default curve is a standard ease‑in‑out.
    /// </remarks>
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine pulseRoutine;

    /// <summary>
    /// Caches the TextMeshProUGUI component if not already assigned.
    /// </summary>
    void Awake()
    {
        if (tmpText == null)
            tmpText = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// Starts the fading loop when the object becomes active.
    /// </summary>
    void OnEnable()
    {
        pulseRoutine = StartCoroutine(FadeLoop());
    }

    /// <summary>
    /// Stops the fading loop when the object becomes inactive.
    /// </summary>
    void OnDisable()
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);
    }

    /// <summary>
    /// Main coroutine that alternates between fade‑in and fade‑out forever.
    /// </summary>
    /// <returns>IEnumerator for the infinite loop.</returns>
    private IEnumerator FadeLoop()
    {
        while (true)
        {
            // Fade in
            yield return FadeToAlpha(1f, fadeInDuration);

            // Fade out
            yield return FadeToAlpha(0f, fadeOutDuration);
        }
    }

    /// <summary>
    /// Fades the text alpha to a target value over a specified duration using the animation curve.
    /// </summary>
    /// <param name="targetAlpha">Target alpha (0 = transparent, 1 = opaque).</param>
    /// <param name="duration">Fade duration in seconds.</param>
    /// <returns>IEnumerator for the fade coroutine.</returns>
    private IEnumerator FadeToAlpha(float targetAlpha, float duration)
    {
        Color color = tmpText.color;
        float startAlpha = color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float curveValue = fadeCurve.Evaluate(t);
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            tmpText.color = new Color(color.r, color.g, color.b, newAlpha);
            yield return null;
        }

        tmpText.color = new Color(color.r, color.g, color.b, targetAlpha);
    }
}