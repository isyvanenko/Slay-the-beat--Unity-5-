using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes a UI image pulse its alpha and color in response to music beats.
/// </summary>
/// <remarks>
/// This component analyzes the bass frequencies of an audio source and dynamically adjusts
/// the alpha (transparency) and color of a target UI Image. It's ideal for rhythm game UI
/// elements such as backgrounds, borders, or visualizers that react to the music.
/// 
/// Key Features:
/// - Bass frequency focus for beat detection (first 40 spectrum bins)
/// - Smooth alpha interpolation between configurable min/max values
/// - Optional color pulsing using a gradient with configurable strength
/// - Smooth damping for both beat value and alpha transitions
/// - Automatic fallback to MusicManager's AudioSource if none assigned
/// 
/// The effect works best with music that has a strong kick drum or bassline.
/// </remarks>
public class UIMusicVisualPulse : MonoBehaviour
{
    [Header("Music")]
    /// <summary>
    /// Audio source providing the music for beat detection.
    /// </summary>
    /// <remarks>
    /// If not assigned in the inspector, the component attempts to find the
    /// MusicManager's AudioSource at runtime as a fallback.
    /// </remarks>
    public AudioSource musicSource;

    [Header("UI Image")]
    /// <summary>
    /// The UI Image component to animate.
    /// </summary>
    /// <remarks>
    /// If not assigned, the component tries to get an Image on the same GameObject.
    /// If no Image is found, the component disables itself and logs an error.
    /// </remarks>
    public Image targetImage;

    [Header("Pulse Settings")]
    /// <summary>
    /// Sensitivity multiplier for converting audio amplitude to pulse intensity.
    /// </summary>
    /// <remarks>
    /// Higher values make the effect more reactive. Typical range: 5-20.
    /// </remarks>
    public float sensitivity = 10f;
    
    /// <summary>
    /// Smoothing time for beat value and alpha transitions (seconds).
    /// </summary>
    /// <remarks>
    /// Lower values = faster, more responsive pulses (may be jittery).
    /// Higher values = smoother, more gradual movement.
    /// Typical range: 0.05 to 0.2 seconds.
    /// </remarks>
    public float smoothTime = 0.1f;

    [Header("Alpha Limits")]
    /// <summary>Minimum alpha value when music is quiet (0 = transparent, 1 = opaque).</summary>
    [Range(0f, 1f)] public float minAlpha = 0.2f;
    
    /// <summary>Maximum alpha value during strong beats.</summary>
    [Range(0f, 1f)] public float maxAlpha = 1f;

    [Header("Color Pulse")]
    /// <summary>
    /// Gradient that defines the color progression based on beat intensity.
    /// </summary>
    /// <remarks>
    /// The gradient is sampled from 0 to 1, where 0 = quiet, 1 = peak beat.
    /// If null, color pulsing will have no effect (only alpha changes).
    /// </remarks>
    public Gradient colorGradient;
    
    /// <summary>
    /// Blend strength between the image's base color and the gradient color.
    /// </summary>
    /// <remarks>
    /// 0 = only base color, 1 = only gradient color. 0.25 provides subtle color shifts.
    /// </remarks>
    [Range(0f, 1f)] public float colorStrength = 0.25f;

    /// <summary>Audio spectrum data array containing 512 frequency bins.</summary>
    private float[] samples = new float[512];

    /// <summary>Current smoothed beat intensity value.</summary>
    private float beatValue;
    
    /// <summary>Velocity reference for beat value SmoothDamp.</summary>
    private float beatVelocity;

    /// <summary>Current alpha value (used for smoothing).</summary>
    private float currentAlpha;
    
    /// <summary>Velocity reference for alpha SmoothDamp.</summary>
    private float alphaVelocity;

    /// <summary>Base color of the image (stored at start).</summary>
    private Color baseColor;

    /// <summary>
    /// Initializes the component: finds required references and stores base color.
    /// </summary>
    /// <remarks>
    /// Setup steps:
    /// 1. Attempt to get an Image component if not assigned.
    /// 2. If no Image found, disable the component and log an error.
    /// 3. Try to get MusicManager's AudioSource if no music source assigned.
    /// 4. Store the image's original color and alpha.
    /// </remarks>
    void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (targetImage == null)
        {
            Debug.LogError("❌ UIMusicVisualPulse: No Image component found.");
            enabled = false;
            return;
        }

        if (musicSource == null && MusicManager.Instance != null)
            musicSource = MusicManager.Instance.GetComponent<AudioSource>();

        baseColor = targetImage.color;
        currentAlpha = baseColor.a;
    }

    /// <summary>
    /// Updates the image's alpha and color based on real-time audio spectrum analysis.
    /// </summary>
    /// <remarks>
    /// Update sequence:
    /// 1. Verify music source exists and is playing; exit if not.
    /// 2. Get spectrum data using FFT (BlackmanHarris window).
    /// 3. Calculate average amplitude of bass frequencies (first 40 bins).
    /// 4. Smooth the beat value using SmoothDamp.
    /// 5. Map beat value to target alpha (between minAlpha and maxAlpha) with smoothing.
    /// 6. If a color gradient is assigned, sample it at (beatValue * 10f, clamped 0-1).
    /// 7. Blend gradient color with base color using colorStrength.
    /// 8. Apply final color and alpha to the UI Image.
    /// 
    /// The multiplication by 10 amplifies the beat value to make better use of the gradient range.
    /// </remarks>
    void Update()
    {
        if (musicSource == null || !musicSource.isPlaying)
            return;

        // Get spectrum data
        musicSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);

        // Bass focus
        float sum = 0f;
        for (int i = 0; i < 40; i++)
            sum += samples[i];

        float average = sum / 40f;

        // Smooth beat value
        beatValue = Mathf.SmoothDamp(
            beatValue,
            average * sensitivity,
            ref beatVelocity,
            smoothTime
        );

        // Alpha pulse
        float targetAlpha = Mathf.Lerp(minAlpha, maxAlpha, beatValue * 10f);
        targetAlpha = Mathf.Clamp(targetAlpha, minAlpha, maxAlpha);

        currentAlpha = Mathf.SmoothDamp(
            currentAlpha,
            targetAlpha,
            ref alphaVelocity,
            smoothTime
        );

        // Color pulse (only if gradient is assigned)
        Color finalColor = baseColor;
        if (colorGradient != null)
        {
            float gradientT = Mathf.Clamp01(beatValue * 10f);
            Color gradientColor = colorGradient.Evaluate(gradientT);
            finalColor = Color.Lerp(baseColor, gradientColor, colorStrength);
        }

        finalColor.a = currentAlpha;
        targetImage.color = finalColor;
    }
}