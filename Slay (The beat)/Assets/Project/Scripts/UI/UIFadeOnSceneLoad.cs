using UnityEngine;

/// <summary>
/// Listens to the scene‑load event and fades out a UI element (CanvasGroup) when a transition starts.
/// </summary>
/// <remarks>
/// This component automatically attaches to the <see cref="TransitionManager.OnSceneLoadStarted"/> event.
/// When the event fires, it smoothly reduces the alpha of its CanvasGroup to zero over a configurable duration.
/// 
/// Typical use: fading out a logo, background, or overlay right before a scene change.
/// </remarks>
public class UIFadeOnSceneLoad : MonoBehaviour
{
    /// <summary>Duration (in seconds) of the fade‑out animation.</summary>
    public float fadeDuration = 0.5f;

    /// <summary>Reference to the CanvasGroup component that controls transparency.</summary>
    private CanvasGroup canvasGroup;

    /// <summary>
    /// Initialises the CanvasGroup and subscribes to the scene‑load event.
    /// </summary>
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Subscribe to the TransitionManager event
        TransitionManager.OnSceneLoadStarted += StartFade;
    }

    /// <summary>
    /// Unsubscribes from the event when the object is destroyed to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        TransitionManager.OnSceneLoadStarted -= StartFade;
    }

    /// <summary>
    /// Called when the scene load event is raised. Starts the fade‑out coroutine.
    /// </summary>
    private void StartFade()
    {
        StartCoroutine(FadeOut());
    }

    /// <summary>
    /// Coroutine that smoothly reduces the CanvasGroup alpha from its current value to zero.
    /// </summary>
    /// <returns>IEnumerator for the fade sequence.</returns>
    private System.Collections.IEnumerator FadeOut()
    {
        float time = 0f;
        float startAlpha = canvasGroup.alpha;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}