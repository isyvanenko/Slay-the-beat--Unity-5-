using UnityEngine;

/// <summary>
/// Pulses a 3D object's scale in response to bass frequencies from a music source.
/// </summary>
/// <remarks>
/// This component analyzes real-time audio spectrum data and smoothly scales a target object
/// based on the average amplitude of low-frequency sounds (bass range). It's ideal for
/// creating beat-synced visual effects like pulsing pillars, speakers, or rhythm-reactive objects.
/// 
/// Key Features:
/// - Focuses on bass frequencies (first 40 spectrum bins) for rhythmic beat detection
/// - Smooth scale interpolation using Mathf.SmoothDamp
/// - Configurable sensitivity, smoothing, and maximum scale multiplier
/// - Automatic fallback to MusicManager's AudioSource if none assigned
/// - Maintains uniform scaling (X, Y, Z scale equally)
/// 
/// The pulse effect works best with music that has a strong kick drum or bassline.
/// </remarks>
public class SpireMusicPulse : MonoBehaviour
{
    [Header("Music Source")]
    [Tooltip("Leave empty to automatically grab the AudioSource from MusicManager.")]
    /// <summary>
    /// Audio source providing the music for beat detection.
    /// </summary>
    /// <remarks>
    /// If not assigned in the inspector, the component attempts to find the
    /// MusicManager's AudioSource at runtime as a fallback.
    /// </remarks>
    public AudioSource musicSource;

    [Header("Pulse Settings")]
    /// <summary>
    /// The object to scale in response to music beats.
    /// </summary>
    /// <remarks>
    /// If left unassigned, defaults to the GameObject this component is attached to.
    /// Scaling is uniform on all three axes (X, Y, Z).
    /// </remarks>
    public Transform spireObject;          // The object to pulse
    
    /// <summary>
    /// Sensitivity multiplier for converting audio amplitude to scale amount.
    /// </summary>
    /// <remarks>
    /// Higher values make the object react more strongly to quiet sounds.
    /// Typical range: 5-20. Adjust based on your audio levels and desired intensity.
    /// </remarks>
    public float sensitivity = 10f;
    
    /// <summary>
    /// Smoothing time for scale transitions (seconds).
    /// </summary>
    /// <remarks>
    /// Lower values = faster, more responsive pulses (can be jittery).
    /// Higher values = smoother, more gradual movements (less punchy).
    /// Typical range: 0.05 to 0.2 seconds.
    /// </remarks>
    public float smoothTime = 0.1f;
    
    /// <summary>
    /// Maximum scale multiplier relative to the object's original size.
    /// </summary>
    /// <remarks>
    /// For example, a value of 2f means the object can grow up to twice its original size.
    /// The object will never shrink below its base scale.
    /// </remarks>
    public float maxScaleMultiplier = 2f;  // Max scale relative to the base size

    /// <summary>Audio spectrum data array containing 512 frequency bins.</summary>
    private float[] samples = new float[512];
    
    /// <summary>Velocity reference for SmoothDamp interpolation.</summary>
    private float currentScaleVelocity;
    
    /// <summary>Original uniform scale of the spire object (stored at start).</summary>
    private float baseScale;               // The original (starting) scale

    /// <summary>
    /// Initializes the component: assigns references and stores the base scale.
    /// </summary>
    /// <remarks>
    /// Setup steps:
    /// 1. If no spire object is assigned, uses the current GameObject's transform.
    /// 2. Stores the original local scale (assumes uniform scaling).
    /// 3. Attempts to find MusicManager's AudioSource if no music source is assigned.
    /// </remarks>
    void Start()
    {
        // Try to automatically assign the spire and music source
        if (spireObject == null)
            spireObject = transform;

        baseScale = spireObject.localScale.x;

        if (musicSource == null && MusicManager.Instance != null)
            musicSource = MusicManager.Instance.GetComponent<AudioSource>();
    }

    /// <summary>
    /// Updates the object's scale based on real-time audio spectrum analysis.
    /// </summary>
    /// <remarks>
    /// Update sequence:
    /// 1. Verify music source exists and is playing; exit if not.
    /// 2. Get spectrum data using FFT (BlackmanHarris window for good frequency resolution).
    /// 3. Calculate average amplitude of bass frequencies (first 40 bins).
    /// 4. Map the average to a target scale: base + (average * sensitivity * 100).
    /// 5. Clamp the target scale between base and base * maxScaleMultiplier.
    /// 6. Smoothly interpolate the current scale toward the target using SmoothDamp.
    /// 7. Apply the new uniform scale to the spire object.
    /// 
    /// The multiplication by 100 amplifies the raw average (which is typically very small)
    /// into a usable range for scaling.
    /// </remarks>
    void Update()
    {
        if (musicSource == null || !musicSource.isPlaying)
            return;

        // Get frequency spectrum data from the playing audio
        musicSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);

        // Focus on lower frequencies for a more rhythmic "beat" effect
        float sum = 0f;
        for (int i = 0; i < 40; i++) // 0–40 ≈ bass range
            sum += samples[i];

        float average = sum / 40f;

        // Convert loudness to a target scale
        float targetScale = baseScale + (average * sensitivity * 100f);

        // Clamp the scale so it never goes below or above set limits
        targetScale = Mathf.Clamp(targetScale, baseScale, baseScale * maxScaleMultiplier);

        // Smoothly interpolate to target scale
        float newScale = Mathf.SmoothDamp(spireObject.localScale.x, targetScale, ref currentScaleVelocity, smoothTime);
        spireObject.localScale = new Vector3(newScale, newScale, newScale);
    }
}