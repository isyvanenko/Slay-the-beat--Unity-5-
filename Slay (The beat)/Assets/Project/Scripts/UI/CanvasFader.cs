using UnityEngine;
using System.Collections;

/// <summary>
/// Smoothly fades in a CanvasGroup using a cosine easing curve.
/// </summary>
/// <remarks>
/// This component automatically fades a CanvasGroup from alpha 0 to 1 over a specified duration.
/// It's ideal for UI panels, menus, or overlays that should appear with a smooth, professional
/// fade-in effect when they become active.
/// 
/// Key Features:
/// - Configurable fade duration
/// - Cosine easing for natural, non-linear fade curve
/// - Optional enabling of raycast blocking and interactivity after fade completes
/// - Uses unscaledDeltaTime for reliable fading even during time scale changes
/// - Automatically disables interaction/raycasts during fade
/// 
/// The component is designed to work with Unity's CanvasGroup component, which must be attached
/// to the same GameObject. The fade starts automatically in Start().
/// </remarks>
[RequireComponent(typeof(CanvasGroup))]
public class CanvasFader : MonoBehaviour
{
    [Header("Fade Settings")]
    /// <summary>
    /// Duration of the fade-in animation in seconds.
    /// </summary>
    public float fadeInDuration = 1.5f;

    /// <summary>
    /// If true, enables CanvasGroup.blocksRaycasts after fade completes.
    /// </summary>
    /// <remarks>
    /// Useful for preventing UI interaction until the fade finishes.
    /// </remarks>
    public bool setBlocksRaycastsOnEnd = true;
    
    /// <summary>
    /// If true, enables CanvasGroup.interactable after fade completes.
    /// </summary>
    public bool setInteractableOnEnd = true;

    /// <summary>Reference to the CanvasGroup component on this GameObject.</summary>
    private CanvasGroup canvasGroup;

    /// <summary>
    /// Initializes the component and caches the CanvasGroup reference.
    /// </summary>
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            Debug.LogError("CanvasFader: No CanvasGroup found!", this);
        }
    }

    /// <summary>
    /// Starts the fade-in coroutine after resetting the CanvasGroup to an invisible, non-interactable state.
    /// </summary>
    /// <remarks>
    /// The CanvasGroup is explicitly reset to alpha 0, interactable false, and blocksRaycasts false
    /// before the fade begins, ensuring a clean start even if the UI was previously visible.
    /// </remarks>
    private void Start()
    {
        // Reset state cleanly
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        StartCoroutine(FadeInCoroutine());
    }

    /// <summary>
    /// Coroutine that performs the actual fade-in animation.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    /// <remarks>
    /// The fade uses a cosine easing function: (1 - cos(π * t)) / 2.
    /// This provides a smooth start and end, unlike linear interpolation.
    /// 
    /// The animation runs based on Time.unscaledDeltaTime, so it won't be affected by
    /// time scale changes (e.g., pause menus or slow-motion effects).
    /// 
    /// After the duration elapses, the alpha is set exactly to 1 and the interaction
    /// and raycast settings are applied based on the public boolean fields.
    /// </remarks>
    private IEnumerator FadeInCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            // Normalized 0 → 1 time
            float t = Mathf.Clamp01(elapsed / fadeInDuration);

            // Ultra-smooth cosine fade
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