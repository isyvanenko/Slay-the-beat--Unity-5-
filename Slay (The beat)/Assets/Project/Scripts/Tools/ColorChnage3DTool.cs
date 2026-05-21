using UnityEngine;

/// <summary>
/// Creates dynamic emission pulse effects on 3D objects based on audio spectrum data.
/// </summary>
/// <remarks>
/// This component analyzes real-time audio from a music source and drives emission intensity
/// and color on a target renderer's material. It's designed for rhythm game visual effects,
/// music visualization, or any scenario where 3D objects should pulse to the beat.
/// 
/// Key Features:
/// - Analyzes bass frequencies (first 40 spectrum bins) for beat detection
/// - Smooths beat values using Mathf.SmoothDamp for natural movement
/// - Controls emission intensity with configurable min/max range
/// - Blends between base material color and gradient-based colors
/// - Uses MaterialPropertyBlock for efficient per-instance rendering
/// - Automatically enables emission on materials if disabled
/// 
/// The system focuses on bass frequencies (0-40 spectrum bins) which typically
/// contain the kick drum and bassline, making it ideal for beat-synced effects.
/// </remarks>
public class ColorChnage3DTool : MonoBehaviour
{
    [Header("Music Source")]
    /// <summary>
    /// AudioSource providing the music to analyze for beat detection.
    /// </summary>
    /// <remarks>
    /// If not assigned in the inspector, the component will attempt to find
    /// the MusicManager's AudioSource at runtime as a fallback.
    /// </remarks>
    public AudioSource musicSource;

    [Header("Target Mesh Renderer")]
    /// <summary>
    /// Renderer whose material emission will be animated.
    /// </summary>
    /// <remarks>
    /// If not assigned, the component will try to get a Renderer on this GameObject.
    /// The target material must support emission (Standard shader with emission enabled).
    /// </remarks>
    public Renderer targetRenderer;

    [Header("Pulse Settings")]
    /// <summary>
    /// Sensitivity multiplier for converting audio amplitude to emission intensity.
    /// </summary>
    /// <remarks>
    /// Higher values make the effect more reactive to audio. Adjust based on
    /// your audio levels and desired intensity range. Typical range: 5-20.
    /// </remarks>
    public float sensitivity = 10f;
    
    /// <summary>
    /// Smoothing time for beat value and emission transitions.
    /// </summary>
    /// <remarks>
    /// Lower values = faster response (more jitter). Higher values = smoother
    /// movement (less responsive). 0.08 provides a good balance for most music.
    /// Measured in seconds.
    /// </remarks>
    public float smoothTime = 0.08f;

    [Header("Emission Strength")]
    /// <summary>
    /// Minimum emission intensity when audio is silent or low.
    /// </summary>
    [Tooltip("Minimum emission intensity")]
    public float minEmission = 0f;

    /// <summary>
    /// Maximum emission intensity at peak audio levels.
    /// </summary>
    [Tooltip("Maximum emission intensity")]
    public float maxEmission = 3f;

    [Header("Emission Color Pulse")]
    /// <summary>
    /// Gradient defining the emission color progression based on audio intensity.
    /// </summary>
    /// <remarks>
    /// The gradient is sampled from 0 to 1 where:
    /// - 0 = Low audio intensity (subtle beat)
    /// - 1 = High audio intensity (strong beat)
    /// 
    /// Common patterns:
    /// - Single color: White for clean white pulses
    /// - Two-color: Dark to bright (e.g., dark red to bright orange)
    /// - Rainbow: Multiple colors for dynamic visual effects
    /// </remarks>
    public Gradient emissionGradient;

    /// <summary>
    /// How strongly the gradient overrides the material's base emission color.
    /// </summary>
    /// <remarks>
    /// 0 = Only base emission color, no gradient influence
    /// 0.5 = Equal mix of base color and gradient color
    /// 1 = Full gradient color, no base color influence
    /// 
    /// This allows subtle color shifts (low values) or dramatic color changes (high values).
    /// </remarks>
    [Tooltip("How strongly the gradient affects emission color")]
    [Range(0f, 1f)]
    public float colorStrength = 1f;

    /// <summary>Audio spectrum data array containing 512 frequency bins.</summary>
    private float[] samples = new float[512];

    /// <summary>Current smoothed beat intensity value (0-1 scale approximately).</summary>
    private float beatValue;
    
    /// <summary>Velocity reference for SmoothDamp on beat value.</summary>
    private float beatVelocity;

    /// <summary>Current emission intensity value.</summary>
    private float emissionValue;
    
    /// <summary>Velocity reference for SmoothDamp on emission value.</summary>
    private float emissionVelocity;

    /// <summary>MaterialPropertyBlock for efficient material property changes.</summary>
    private MaterialPropertyBlock mpb;

    /// <summary>Original emission color from the material (used for blending).</summary>
    private Color baseEmissionColor;

    /// <summary>Cached shader property ID for emission color (performance optimization).</summary>
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    /// <summary>
    /// Initializes component references and material emission settings.
    /// </summary>
    /// <remarks>
    /// Setup sequence:
    /// 1. Get or find target Renderer if not assigned
    /// 2. Create MaterialPropertyBlock for efficient rendering
    /// 3. Enable _EMISSION keyword on material if disabled
    /// 4. Cache the material's base emission color
    /// 5. Attempt to find MusicManager's AudioSource if no music source assigned
    /// 
    /// The MaterialPropertyBlock approach allows multiple objects to use the same
    /// material with different properties without creating material instances.
    /// </remarks>
    void Start()
    {
        if (!targetRenderer)
            targetRenderer = GetComponent<Renderer>();

        mpb = new MaterialPropertyBlock();

        if (targetRenderer && targetRenderer.sharedMaterial)
        {
            Material mat = targetRenderer.sharedMaterial;

            if (!mat.IsKeywordEnabled("_EMISSION"))
                mat.EnableKeyword("_EMISSION");

            baseEmissionColor = mat.GetColor(EmissionColorID);
        }

        if (!musicSource && MusicManager.Instance)
            musicSource = MusicManager.Instance.GetComponent<AudioSource>();
    }

    /// <summary>
    /// Updates emission effect based on real-time audio spectrum analysis.
    /// </summary>
    /// <remarks>
    /// Per-frame update process:
    /// 1. Get spectrum data from the music source (FFT of 512 bins)
    /// 2. Calculate average amplitude of bass frequencies (first 40 bins)
    /// 3. Smooth the beat value to prevent jitter
    /// 4. Map beat value to emission intensity (minEmission to maxEmission)
    /// 5. Sample gradient color based on intensity
    /// 6. Blend with base emission color using colorStrength
    /// 7. Apply final emission color to material via MaterialPropertyBlock
    /// 
    /// The effect only runs when music is playing and all required
    /// references are valid, otherwise it silently exits.
    /// </remarks>
    void Update()
    {
        if (!musicSource || !musicSource.isPlaying || targetRenderer == null)
            return;

        // Get spectrum data (bass)
        musicSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);

        float sum = 0f;
        for (int i = 0; i < 40; i++)
            sum += samples[i];

        float average = sum / 40f;

        // Smooth beat
        beatValue = Mathf.SmoothDamp(
            beatValue,
            average * sensitivity,
            ref beatVelocity,
            smoothTime
        );

        // Emission intensity
        float targetEmission = Mathf.Lerp(minEmission, maxEmission, beatValue * 10f);
        targetEmission = Mathf.Clamp(targetEmission, minEmission, maxEmission);

        emissionValue = Mathf.SmoothDamp(
            emissionValue,
            targetEmission,
            ref emissionVelocity,
            smoothTime
        );

        // Emission color
        float t = Mathf.Clamp01(beatValue * 10f);
        Color gradientColor = emissionGradient.Evaluate(t);

        Color finalEmission = Color.Lerp(
            baseEmissionColor,
            gradientColor,
            colorStrength
        ) * emissionValue;

        // Apply
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(EmissionColorID, finalEmission);
        targetRenderer.SetPropertyBlock(mpb);
    }
}