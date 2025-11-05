using UnityEngine;

public class SpireMusicPulse : MonoBehaviour
{
    [Header("Music Source")]
    [Tooltip("Leave empty to automatically grab the AudioSource from MusicManager.")]
    public AudioSource musicSource;

    [Header("Pulse Settings")]
    public Transform spireObject;          // The object to pulse
    public float sensitivity = 10f;        // How reactive it is to loudness
    public float smoothTime = 0.1f;        // How smooth the pulse transition is
    public float maxScaleMultiplier = 2f;  // Max scale relative to the base size

    private float[] samples = new float[512];
    private float currentScaleVelocity;
    private float baseScale;               // The original (starting) scale

    void Start()
    {
        // Try to automatically assign the spire and music source
        if (spireObject == null)
            spireObject = transform;

        baseScale = spireObject.localScale.x;

        if (musicSource == null && MusicManager.instance != null)
            musicSource = MusicManager.instance.GetComponent<AudioSource>();
    }

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
