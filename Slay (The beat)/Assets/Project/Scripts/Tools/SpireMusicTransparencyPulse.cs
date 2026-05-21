using UnityEngine;

/// <summary>
/// Pulses the color and alpha of a material in response to music beats, with optional gradient support.
/// </summary>
/// <remarks>
/// This component analyzes real-time audio spectrum data to drive visual effects on a renderer's material.
/// It focuses on bass frequencies to create rhythmic pulsing, adjusting both alpha transparency and color.
/// 
/// Key Features:
/// - Bass frequency detection for beat-synced pulses
/// - Smooth alpha interpolation between configurable min/max values
/// - Color pulsing using gradients (custom or song-specific)
/// - Optional auto-update of gradient from currently selected song's visualGradient
/// - Fallback default rainbow gradient when none is assigned
/// - Material instance creation to avoid modifying shared materials
/// 
/// The pulse intensity is calculated from the average amplitude of the first 40 spectrum bins,
/// which represent low-frequency sounds (kick drum, bassline).
/// </remarks>
public class SpireMusicVisualPulse : MonoBehaviour
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

    [Header("Renderer / Material")]
    /// <summary>
    /// Renderer whose material will be animated.
    /// </summary>
    /// <remarks>
    /// If not assigned, the component tries to get a Renderer on this GameObject.
    /// A material instance is created at runtime to prevent asset modification.
    /// </remarks>
    public Renderer targetRenderer;

    [Header("Pulse Settings")]
    /// <summary>
    /// Sensitivity multiplier for converting audio amplitude to pulse intensity.
    /// </summary>
    /// <remarks>
    /// Higher values make the effect more reactive. Typical range: 5-20.
    /// </remarks>
    public float sensitivity = 10f;
    
    /// <summary>
    /// Smoothing time for both alpha and color transitions (seconds).
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
    [Tooltip("Leave empty to use the gradient from the current song")]
    /// <summary>
    /// Gradient that defines the color progression based on beat intensity.
    /// </summary>
    /// <remarks>
    /// The gradient is sampled from 0 to 1, where 0 = quiet, 1 = peak beat.
    /// If null and autoUpdateGradientFromSong is true, tries to use the song's visualGradient.
    /// If none available, falls back to a default rainbow gradient.
    /// </remarks>
    public Gradient colorGradient;
    
    [Tooltip("How strong the color shift is (lower = more subtle)")]
    [Range(0f, 1f)]
    /// <summary>
    /// Blend strength between the material's base color and the gradient color.
    /// </summary>
    /// <remarks>
    /// 0 = only base color, 1 = only gradient color. 0.25 provides subtle color shifts.
    /// </remarks>
    public float colorStrength = 0.25f;
    
    [Tooltip("Automatically update gradient from song data when song changes")]
    /// <summary>
    /// If true, the component will check GameDataBridge for the selected song and
    /// update the gradient from the song's visualGradient field.
    /// </summary>
    public bool autoUpdateGradientFromSong = true;

    /// <summary>Audio spectrum data array containing 512 frequency bins.</summary>
    private float[] samples = new float[512];
    
    /// <summary>Current alpha value (used for smoothing).</summary>
    private float currentAlpha;
    
    /// <summary>Velocity reference for alpha SmoothDamp.</summary>
    private float alphaVelocity;

    /// <summary>Current smoothed beat intensity value.</summary>
    private float beatValue;
    
    /// <summary>Velocity reference for beat value SmoothDamp.</summary>
    private float beatVelocity;

    /// <summary>Material instance created at runtime (prevents shared material changes).</summary>
    private Material runtimeMaterial;
    
    /// <summary>Base color of the material (stored at start).</summary>
    private Color baseColor;
    
    /// <summary>Currently active song data (used to track changes for auto-update).</summary>
    private SongGradeData currentSongData;

    /// <summary>
    /// Initializes component references, creates material instance, and stores base color.
    /// </summary>
    void Start()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material; // Instance
            baseColor = runtimeMaterial.color;
            currentAlpha = baseColor.a;
        }

        if (musicSource == null && MusicManager.Instance != null)
            musicSource = MusicManager.Instance.GetComponent<AudioSource>();
    }

    /// <summary>
    /// Updates material alpha and color based on real-time audio spectrum analysis.
    /// </summary>
    /// <remarks>
    /// Update sequence:
    /// 1. Verify music source exists and is playing; exit if not.
    /// 2. Auto-update gradient from song if enabled.
    /// 3. Get spectrum data using FFT (BlackmanHarris window).
    /// 4. Calculate average amplitude of bass frequencies (first 40 bins).
    /// 5. Smooth the beat value.
    /// 6. Map beat value to target alpha (between minAlpha and maxAlpha).
    /// 7. Sample gradient at position (beatValue * 10f, clamped 0-1).
    /// 8. Blend gradient color with base color using colorStrength.
    /// 9. Apply final color and alpha to the material instance.
    /// 
    /// The multiplication by 10 amplifies the beat value to make better use of the gradient range.
    /// </remarks>
    void Update()
    {
        if (musicSource == null || !musicSource.isPlaying || runtimeMaterial == null)
            return;

        // Auto-update gradient from current song if needed
        if (autoUpdateGradientFromSong)
        {
            TryUpdateGradientFromSong();
        }

        // Get spectrum data
        musicSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);

        // Bass focus
        float sum = 0f;
        for (int i = 0; i < 40; i++)
            sum += samples[i];

        float average = sum / 40f;

        // Smooth beat value (used for both alpha & color)
        beatValue = Mathf.SmoothDamp(
            beatValue,
            average * sensitivity,
            ref beatVelocity,
            smoothTime
        );

        // ---- ALPHA ----
        float targetAlpha = Mathf.Lerp(minAlpha, maxAlpha, beatValue * 10f);
        targetAlpha = Mathf.Clamp(targetAlpha, minAlpha, maxAlpha);

        currentAlpha = Mathf.SmoothDamp(
            currentAlpha,
            targetAlpha,
            ref alphaVelocity,
            smoothTime
        );

        // ---- COLOR ----
        float gradientT = Mathf.Clamp01(beatValue * 10f);
        
        // Use either the assigned gradient or fallback to a default rainbow gradient
        Gradient activeGradient = colorGradient;
        if (activeGradient == null)
        {
            activeGradient = GetDefaultGradient();
        }
        
        Color gradientColor = activeGradient.Evaluate(gradientT);

        // Subtle blend between base color and gradient color
        Color finalColor = Color.Lerp(
            baseColor,
            gradientColor,
            colorStrength
        );

        finalColor.a = currentAlpha;
        runtimeMaterial.color = finalColor;
    }

    /// <summary>
    /// Updates the color gradient from a SongGradeData asset's visualGradient field.
    /// </summary>
    /// <param name="songData">Song data containing the gradient to use.</param>
    /// <remarks>
    /// If the song data has a valid visualGradient, it replaces the current gradient.
    /// If the song data exists but has no gradient, a default gradient is assigned.
    /// Logs the update for debugging purposes.
    /// </remarks>
    public void UpdateGradientFromSong(SongGradeData songData)
    {
        if (songData != null && songData.visualGradient != null)
        {
            colorGradient = songData.visualGradient;
            currentSongData = songData;
            Debug.Log($"Updated visual gradient from song: {songData.songName}");
        }
        else if (songData != null && songData.visualGradient == null)
        {
            // Use default gradient if song doesn't have one
            colorGradient = GetDefaultGradient();
            Debug.Log($"Song {songData.songName} has no gradient, using default");
        }
    }

    /// <summary>
    /// Attempts to update the gradient from the currently selected song in GameDataBridge.
    /// </summary>
    /// <remarks>
    /// Called automatically during Update if autoUpdateGradientFromSong is true.
    /// Compares the current song with the last known song and updates if changed.
    /// </remarks>
    private void TryUpdateGradientFromSong()
    {
        // Try to get current song from GameDataBridge
        if (GameDataBridge.SelectedSong != null && GameDataBridge.SelectedSong != currentSongData)
        {
            UpdateGradientFromSong(GameDataBridge.SelectedSong);
        }
        
        // Alternative: Try to get from MusicManager if it has song data reference
        // (Placeholder for future expansion - requires MusicManager to expose CurrentSong)
        if (currentSongData == null && MusicManager.Instance != null)
        {
            // You might need to add a CurrentSong property to MusicManager
            // For now, this is a placeholder
        }
    }

    /// <summary>
    /// Creates and returns a default rainbow gradient for fallback use.
    /// </summary>
    /// <returns>A gradient with red, yellow, green, and blue color keys.</returns>
    private Gradient GetDefaultGradient()
    {
        // Create a default rainbow-ish gradient
        Gradient defaultGrad = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[4];
        colorKeys[0] = new GradientColorKey(Color.red, 0f);
        colorKeys[1] = new GradientColorKey(Color.yellow, 0.33f);
        colorKeys[2] = new GradientColorKey(Color.green, 0.66f);
        colorKeys[3] = new GradientColorKey(Color.blue, 1f);
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);
        
        defaultGrad.SetKeys(colorKeys, alphaKeys);
        return defaultGrad;
    }

    /// <summary>
    /// Manually sets a new gradient for the visual pulse effect.
    /// </summary>
    /// <param name="newGradient">The gradient to use for color pulsing.</param>
    public void SetGradient(Gradient newGradient)
    {
        colorGradient = newGradient;
    }

    /// <summary>
    /// Resets the material color to its base color (preserving current alpha).
    /// </summary>
    public void ResetToBaseColor()
    {
        if (runtimeMaterial != null)
        {
            Color resetColor = baseColor;
            resetColor.a = currentAlpha;
            runtimeMaterial.color = resetColor;
        }
    }
}