using UnityEngine;

public class SpireMusicVisualPulse : MonoBehaviour
{
    [Header("Music Source")]
    [Tooltip("Leave empty to automatically grab the AudioSource from MusicManager.")]
    public AudioSource musicSource;

    [Header("Renderer / Material")]
    public Renderer targetRenderer;

    [Header("Pulse Settings")]
    public float sensitivity = 10f;
    public float smoothTime = 0.1f;

    [Header("Alpha Limits")]
    [Range(0f, 1f)] public float minAlpha = 0.2f;
    [Range(0f, 1f)] public float maxAlpha = 1f;

    [Header("Color Pulse")]
    public Gradient colorGradient;
    [Tooltip("How strong the color shift is (lower = more subtle)")]
    [Range(0f, 1f)] public float colorStrength = 0.25f;

    private float[] samples = new float[512];
    private float currentAlpha;
    private float alphaVelocity;

    private float beatValue;
    private float beatVelocity;

    private Material runtimeMaterial;
    private Color baseColor;

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

    void Update()
    {
        if (musicSource == null || !musicSource.isPlaying || runtimeMaterial == null)
            return;

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
        Color gradientColor = colorGradient.Evaluate(gradientT);

        // Subtle blend between base color and gradient color
        Color finalColor = Color.Lerp(
            baseColor,
            gradientColor,
            colorStrength
        );

        finalColor.a = currentAlpha;
        runtimeMaterial.color = finalColor;
    }
}
