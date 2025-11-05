using UnityEngine;
using UnityEngine.UI;

public class EnableImageOnAwake : MonoBehaviour
{
    private Image imageComponent;

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

