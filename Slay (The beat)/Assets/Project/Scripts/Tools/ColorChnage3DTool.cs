using UnityEngine;

public class ColorChnage3DTool : MonoBehaviour
{
   [Header("Music Source")]
    public AudioSource musicSource;

    [Header("Target Mesh Renderer")]
    public Renderer targetRenderer;

    [Header("Pulse Settings")]
    public float sensitivity = 10f;
    public float smoothTime = 0.08f;

    [Header("Emission Strength")]
    [Tooltip("Minimum emission intensity")]
    public float minEmission = 0f;

    [Tooltip("Maximum emission intensity")]
    public float maxEmission = 3f;

    [Header("Emission Color Pulse")]
    public Gradient emissionGradient;

    [Tooltip("How strongly the gradient affects emission color")]
    [Range(0f, 1f)]
    public float colorStrength = 1f;

    private float[] samples = new float[512];

    private float beatValue;
    private float beatVelocity;

    private float emissionValue;
    private float emissionVelocity;

    private MaterialPropertyBlock mpb;

    private Color baseEmissionColor;

    // Shader property IDs
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

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