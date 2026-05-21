using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Automatically enables an Image component when the GameObject awakens.
/// </summary>
/// <remarks>
/// This component searches for an Image component on the same GameObject during the Awake phase
/// and enables it. If no Image component is found, it logs a warning message.
/// This is useful for ensuring UI elements are visible by default after initialization.
/// </remarks>
public class EnableImageOnAwake : MonoBehaviour
{
    /// <summary>Reference to the Image component on this GameObject.</summary>
    private Image imageComponent;

    /// <summary>
    /// Called when the GameObject is initialized, before Start.
    /// </summary>
    /// <remarks>
    /// This method attempts to retrieve the Image component from the current GameObject.
    /// If successful, it enables the component and logs a confirmation message.
    /// If no Image component exists, it logs a warning to aid in debugging.
    /// </remarks>
    void Awake()
    {
        // Try to get the Image component on this GameObject
        imageComponent = GetComponent<Image>();

        if (imageComponent != null)
        {
            // Enable the Image component
            imageComponent.enabled = true;
            Debug.Log("Image component enabled on Awake.");
        }
        else
        {
            Debug.LogWarning("No Image component found on this GameObject.");
        }
    }
}