using UnityEngine;

/// <summary>
/// Automatically activates a specified GameObject when the component awakens.
/// </summary>
/// <remarks>
/// This simple utility component is useful for ensuring certain UI panels, effects,
/// or gameplay elements are enabled at startup without requiring manual scene setup.
/// 
/// Common use cases:
/// - Activating UI panels that should be visible by default
/// - Enabling visual effects that should play on scene load
/// - Turning on gameplay systems that might be disabled in prefabs
/// - Initializing disabled objects that need to be active immediately
/// 
/// The activation happens during Awake() rather than Start(), ensuring the object
/// is active before any other components initialize or Start() methods run.
/// </remarks>
public class ActivateOnAwake : MonoBehaviour
{
    [Header("Object to Activate")]
    /// <summary>
    /// Reference to the GameObject that will be activated when this component awakens.
    /// </summary>
    /// <remarks>
    /// This can be any GameObject in the scene, including:
    /// - Child objects of this GameObject
    /// - Objects elsewhere in the scene hierarchy
    /// - Prefab instances
    /// 
    /// If left unassigned in the inspector, a warning will be logged but the script
    /// will not throw an error.
    /// </remarks>
    public GameObject objectToActivate;

    /// <summary>
    /// Called when the GameObject is initialized, before any Start() methods.
    /// </summary>
    /// <remarks>
    /// This method attempts to activate the assigned GameObject. If successful,
    /// it logs a confirmation message. If the reference is null, it logs a warning
    /// to assist in debugging.
    /// 
    /// Using Awake() instead of Start() ensures that:
    /// - The object is active before other components run their initialization
    /// - Any components on the activated object can initialize properly
    /// - There's no one-frame delay before the object becomes active
    /// </remarks>
    void Awake()
    {
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
            Debug.Log(objectToActivate.name + " activated on Awake.");
        }
        else
        {
            Debug.LogWarning("No GameObject assigned to ActivateOnAwake.");
        }
    }
}