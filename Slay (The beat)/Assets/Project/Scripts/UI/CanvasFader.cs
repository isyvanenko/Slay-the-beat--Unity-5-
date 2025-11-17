using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class CanvasFader : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeInDuration = 1.5f;

    public bool setBlocksRaycastsOnEnd = true;
    public bool setInteractableOnEnd = true;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            Debug.LogError("CanvasFader: No CanvasGroup found!", this);
        }
    }

    private void Start()
    {
        // Reset state cleanly
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        StartCoroutine(FadeInCoroutine());
    }

    private IEnumerator FadeInCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            // Normalized 0 → 1 time
            float t = Mathf.Clamp01(elapsed / fadeInDuration);

            // ⭐ Ultra-smooth cosine fade
            float eased = (1f - Mathf.Cos(t * Mathf.PI)) * 0.5f;

            canvasGroup.alpha = eased;

            yield return null;
        }

        // Ensure exact final values
        canvasGroup.alpha = 1f;

        canvasGroup.blocksRaycasts = setBlocksRaycastsOnEnd;
        canvasGroup.interactable = setInteractableOnEnd;
    }
}