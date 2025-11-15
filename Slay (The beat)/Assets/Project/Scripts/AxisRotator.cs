using UnityEngine;

public class AxisRotator : MonoBehaviour
{
[Header("Rotation Axes")]
    public bool rotateX = false;
    public bool rotateY = false;
    public bool rotateZ = false;

    [Header("Settings")]
    public float rotationSpeed = 50f;

    private Vector3 currentRotation;

    void Start()
    {
        // Start from the object's initial rotation
        currentRotation = transform.eulerAngles;
    }

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
