using UnityEngine;
using UnityEngine.UI;

public class UIMusicVisualPulse : MonoBehaviour
{
    [Header("Music")]
    public AudioSource musicSource;

    [Header("UI Image")]
    public Image targetImage;

    [Header("Pulse Settings")]
    public float sensitivity = 10f;
    public float smoothTime = 0.1f;

    [Header("Alpha Limits")]
    [Range(0f, 1f)] public float minAlpha = 0.2f;
    [Range(0f, 1f)] public float maxAlpha = 1f;

    [Header("Color Pulse")]
    public Gradient colorGradient;
    [Range(0f, 1f)] public float colorStrength = 0.25f;

    private float[] samples = new float[512];

    private float beatValue;
    private float beatVelocity;

    private float currentAlpha;
    private float alphaVelocity;

    private Color baseColor;

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

        // Color pulse
        float gradientT = Mathf.Clamp01(beatValue * 10f);
        Color gradientColor = colorGradient.Evaluate(gradientT);

        Color finalColor = Color.Lerp(baseColor, gradientColor, colorStrength);
        finalColor.a = currentAlpha;

        targetImage.color = finalColor;
    }
}
