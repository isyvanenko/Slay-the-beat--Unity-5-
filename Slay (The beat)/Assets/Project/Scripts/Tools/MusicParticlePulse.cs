using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class MusicParticlePulse : MonoBehaviour
{
   [Header("Music")]
    public AudioSource musicSource;

    [Header("Beat Detection")]
    public float sensitivity = 15f;
    public float beatThreshold = 0.02f;

    [Header("Laser Emission")]
    public int laserCount = 8;

    [Header("Trail Length")]
    public float idleTrailLifetime = 0.05f;
    public float beatTrailLifetime = 1.5f;
    public float trailReturnSpeed = 12f;

    private ParticleSystem ps;
    private ParticleSystem.TrailModule trails;

    private float[] samples = new float[512];

    private float currentTrailLifetime;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        trails = ps.trails;

        currentTrailLifetime = idleTrailLifetime;

        if (!musicSource && MusicManager.Instance)
            musicSource = MusicManager.Instance.GetComponent<AudioSource>();
    }

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
