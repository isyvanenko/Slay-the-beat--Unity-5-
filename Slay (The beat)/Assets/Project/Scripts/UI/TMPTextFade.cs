using UnityEngine;
using TMPro;
using System.Collections;

public class TMPTextPulse : MonoBehaviour
{
    [Header("Text Settings")]
    [SerializeField] private TextMeshProUGUI tmpText; // Assign your TMP Text
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Animation Curve (optional)")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine pulseRoutine;

    void Awake()
    {
        if (tmpText == null)
            tmpText = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        pulseRoutine = StartCoroutine(FadeLoop());
    }

    void OnDisable()
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);
    }

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
