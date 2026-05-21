using UnityEngine;

/// <summary>
/// Continuously rotates the GameObject around specified axes at a constant speed.
/// </summary>
/// <remarks>
/// This component allows independent rotation control for X, Y, and Z axes.
/// Rotation is applied in local Euler angles and accumulates over time,
/// creating continuous rotation based on the object's initial orientation.
/// </remarks>
public class AxisRotator : MonoBehaviour
{
    [Header("Rotation Axes")]
    /// <summary>If true, rotates around the local X-axis.</summary>
    public bool rotateX = false;
    
    /// <summary>If true, rotates around the local Y-axis.</summary>
    public bool rotateY = false;
    
    /// <summary>If true, rotates around the local Z-axis.</summary>
    public bool rotateZ = false;

    [Header("Settings")]
    /// <summary>Rotation speed in degrees per second for all active axes.</summary>
    public float rotationSpeed = 50f;

    /// <summary>Tracks the current Euler rotation angles to maintain continuous rotation.</summary>
    private Vector3 currentRotation;

    /// <summary>
    /// Initializes the rotation tracking with the GameObject's initial Euler angles.
    /// </summary>
    void Start()
    {
        // Start from the object's initial rotation
        currentRotation = transform.eulerAngles;
    }

    /// <summary>
    /// Updates the rotation each frame based on active axes and rotation speed.
    /// </summary>
    /// <remarks>
    /// Rotation is calculated as: angle += rotationSpeed * Time.deltaTime for each enabled axis.
    /// The rotation is applied using Quaternion.Euler to avoid gimbal lock issues.
    /// Time.deltaTime ensures frame-rate independent rotation speed.
    /// </remarks>
    void Update()
    {
        // Add rotation over time (continuous)
        if (rotateX) currentRotation.x += rotationSpeed * Time.deltaTime;
        if (rotateY) currentRotation.y += rotationSpeed * Time.deltaTime;
        if (rotateZ) currentRotation.z += rotationSpeed * Time.deltaTime;

        // Apply the updated rotation
        transform.rotation = Quaternion.Euler(currentRotation);
    }
}