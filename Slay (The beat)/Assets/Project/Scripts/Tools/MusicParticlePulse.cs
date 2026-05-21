using UnityEngine;

/// <summary>
/// Triggers particle system bursts and dynamically adjusts trail length based on detected music beats.
/// </summary>
/// <remarks>
/// This component analyzes the bass frequencies of an audio source and emits laser-like particles
/// when a beat is detected. It also stretches the particle trails on beat and smoothly returns them
/// to an idle length between beats.
/// 
/// Key Features:
/// - Real-time bass frequency analysis using Unity's GetSpectrumData
/// - Configurable sensitivity and threshold for beat detection
/// - Instant particle emission on beat for visual impact
/// - Dynamic trail length scaling (long on beat, short at idle)
/// - Smooth interpolation between trail states
/// 
/// The system focuses on the first 40 spectrum bins to capture kick drum and bassline frequencies,
/// making it ideal for rhythm game effects or music visualization.
/// </remarks>
[RequireComponent(typeof(ParticleSystem))]
public class MusicParticlePulse : MonoBehaviour
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

    [Header("Beat Detection")]
    /// <summary>
    /// Sensitivity multiplier that scales the detected audio amplitude.
    /// </summary>
    /// <remarks>
    /// Higher values make beat detection more responsive. Typical range: 5-20.
    /// Adjust based on your audio levels and desired reactivity.
    /// </remarks>
    public float sensitivity = 15f;
    
    /// <summary>
    /// Minimum scaled amplitude required to trigger a beat.
    /// </summary>
    /// <remarks>
    /// Lower values make detection more sensitive (more false positives).
    /// Higher values make detection stricter (fewer triggers).
    /// Recommended range: 0.01 to 0.05.
    /// </remarks>
    public float beatThreshold = 0.02f;

    [Header("Laser Emission")]
    /// <summary>
    /// Number of particles emitted instantly on each beat.
    /// </summary>
    /// <remarks>
    /// Higher values create more intense laser bursts. Typical range: 4-20.
    /// </remarks>
    public int laserCount = 8;

    [Header("Trail Length")]
    /// <summary>
    /// Trail lifetime in seconds when no beat is occurring.
    /// </summary>
    /// <remarks>
    /// Short trails (0.05-0.1s) create a crisp, dot-like appearance.
    /// </remarks>
    public float idleTrailLifetime = 0.05f;
    
    /// <summary>
    /// Trail lifetime in seconds immediately after a beat.
    /// </summary>
    /// <remarks>
    /// Longer trails (1-2s) create a streaking laser effect during beats.
    /// </remarks>
    public float beatTrailLifetime = 1.5f;
    
    /// <summary>
    /// Speed at which the trail lifetime returns to idle after a beat.
    /// </summary>
    /// <remarks>
    /// Higher values cause rapid trail shortening; lower values create a
    /// lingering effect. Measured in seconds^-1 (units of 1/time).
    /// </remarks>
    public float trailReturnSpeed = 12f;

    /// <summary>Reference to the ParticleSystem component.</summary>
    private ParticleSystem ps;
    
    /// <summary>Reference to the ParticleSystem's trail module for runtime adjustment.</summary>
    private ParticleSystem.TrailModule trails;

    /// <summary>Audio spectrum data array containing 512 frequency bins.</summary>
    private float[] samples = new float[512];

    /// <summary>Current trail lifetime value (interpolated between idle and beat).</summary>
    private float currentTrailLifetime;

    /// <summary>
    /// Initializes component references and caches the trail module.
    /// </summary>
    /// <remarks>
    /// Gets the ParticleSystem component, stores its trail module for runtime
    /// modification, and attempts to find MusicManager's AudioSource if no
    /// music source is assigned.
    /// </remarks>
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        trails = ps.trails;

        currentTrailLifetime = idleTrailLifetime;

        if (!musicSource && MusicManager.Instance)
            musicSource = MusicManager.Instance.GetComponent<AudioSource>();
    }

    /// <summary>
    /// Analyzes audio spectrum each frame, triggers particle emission on beat,
    /// and smoothly adjusts trail lifetime.
    /// </summary>
    /// <remarks>
    /// Update sequence:
    /// 1. Verify music source exists and is playing
    /// 2. Get spectrum data using FFT (BlackmanHarris window for good frequency resolution)
    /// 3. Calculate average amplitude of bass frequencies (first 40 bins)
    /// 4. Scale average by sensitivity to get beat value
    /// 5. If beat value exceeds threshold, emit particles and extend trail lifetime
    /// 6. Smoothly interpolate trail lifetime back to idle value
    /// 7. Apply the updated lifetime to the trail module
    /// 
    /// The trail module's lifetime property controls how long particle trails persist.
    /// Longer trails create streaking effects; shorter trails produce dots.
    /// </remarks>
    void Update()
    {
        if (!musicSource || !musicSource.isPlaying)
            return;

        // Bass detection
        musicSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);

        float sum = 0f;
        for (int i = 0; i < 40; i++)
            sum += samples[i];

        float average = sum / 40f;
        float beatValue = average * sensitivity;

        // ----- BEAT HIT -----
        if (beatValue > beatThreshold)
        {
            // Emit laser particles instantly
            ps.Emit(laserCount);

            // Stretch trails hard
            currentTrailLifetime = beatTrailLifetime;
        }

        // Smooth return to idle (lasers disappear)
        currentTrailLifetime = Mathf.Lerp(
            currentTrailLifetime,
            idleTrailLifetime,
            Time.deltaTime * trailReturnSpeed
        );

        trails.lifetime = currentTrailLifetime;
    }
}