using UnityEngine;

public class UIFadeOnSceneLoad : MonoBehaviour
{
    public float fadeDuration = 0.5f;     // time to fade out
    private CanvasGroup canvasGroup;

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

    private void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        TransitionManager.OnSceneLoadStarted -= StartFade;
    }

    private void StartFade()
    {
        StartCoroutine(FadeOut());
    }

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