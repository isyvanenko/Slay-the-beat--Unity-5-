using UnityEngine;

/// <summary>
/// Pulses the emission color and intensity of a specific material in response to music beats.
/// </summary>
public class SpireMusicParticleEmissionPulse : MonoBehaviour
{
    [Header("Music Source")]
    public AudioSource musicSource;

    [Header("Target Material")]
    [Tooltip("Drag the material you want to modify here. This will create an instance.")]
    public Material targetMaterial;
    
    [Tooltip("Optional: Particle system to get renderer from if targetMaterial is not set")]
    public ParticleSystem particleSystem;

    [Header("Emission Pulse Settings")]
    public float sensitivity = 10f;
    public float smoothTime = 0.1f;

    [Header("Emission Intensity")]
    [Range(0f, 10f)] public float minEmissionIntensity = 0f;
    [Range(0f, 20f)] public float maxEmissionIntensity = 5f;

    [Header("Emission Color")]
    public Gradient emissionColorGradient;
    [Range(0f, 1f)] public float colorStrength = 0.25f;

    [Header("Shader Property Names")]
    public string emissionColorProperty = "_EmissionColor";
    public string emissionIntensityProperty = "_EmissionIntensity";

    // Runtime variables
    private Material materialInstance;
    private Color baseEmissionColor;
    private float baseEmissionIntensity;
    
    private float[] samples = new float[512];
    private float beatValue;
    private float beatVelocity;
    private float currentIntensity;
    private float intensityVelocity;
    private SongGradeData currentSongData;

    void Start()
    {
        // Get the material instance
        if (targetMaterial != null)
        {
            // Create an instance of the assigned material
            materialInstance = new Material(targetMaterial);
            Debug.Log($"Created material instance from: {targetMaterial.name}");
        }
        else if (particleSystem != null)
        {
            // Get material from particle system renderer
            Renderer renderer = particleSystem.GetComponent<Renderer>();
            if (renderer != null)
            {
                materialInstance = renderer.material; // This creates an instance
                Debug.Log($"Created material instance from particle system: {materialInstance.name}");
            }
        }
        else
        {
            Debug.LogError("No target material or particle system assigned!");
            return;
        }

        // Store base values
        if (materialInstance.HasProperty(emissionColorProperty))
        {
            baseEmissionColor = materialInstance.GetColor(emissionColorProperty);
            Debug.Log($"Base emission color: {baseEmissionColor}");
        }
        else
        {
            baseEmissionColor = Color.white;
            Debug.LogWarning($"Property '{emissionColorProperty}' not found. Using white.");
        }

        if (materialInstance.HasProperty(emissionIntensityProperty))
        {
            baseEmissionIntensity = materialInstance.GetFloat(emissionIntensityProperty);
            Debug.Log($"Base emission intensity: {baseEmissionIntensity}");
        }
        else
        {
            baseEmissionIntensity = 0f;
            Debug.LogWarning($"Property '{emissionIntensityProperty}' not found. Using 0.");
        }

        currentIntensity = baseEmissionIntensity;

        // Get music source
        if (musicSource == null && MusicManager.Instance != null)
            musicSource = MusicManager.Instance.GetComponent<AudioSource>();

        // Default gradient
        if (emissionColorGradient == null)
            emissionColorGradient = GetDefaultGradient();

        // Apply the material instance to the renderer if we have a particle system
        if (particleSystem != null)
        {
            Renderer renderer = particleSystem.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = materialInstance;
                Debug.Log("Applied material instance to particle system renderer");
            }
        }
    }

    void Update()
    {
        if (musicSource == null || !musicSource.isPlaying || materialInstance == null)
            return;

        // Get audio spectrum
        musicSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);

        // Bass focus (first 40 bins)
        float sum = 0f;
        for (int i = 0; i < 40; i++)
            sum += samples[i];
        float average = sum / 40f;

        // Smooth beat value
        beatValue = Mathf.SmoothDamp(beatValue, average * sensitivity, ref beatVelocity, smoothTime);

        // Calculate emission intensity
        float targetIntensity = Mathf.Lerp(minEmissionIntensity, maxEmissionIntensity, Mathf.Clamp01(beatValue * 10f));
        currentIntensity = Mathf.SmoothDamp(currentIntensity, targetIntensity, ref intensityVelocity, smoothTime);

        // Calculate emission color from gradient
        float gradientT = Mathf.Clamp01(beatValue * 10f);
        Color gradientColor = emissionColorGradient.Evaluate(gradientT);
        Color finalEmissionColor = Color.Lerp(baseEmissionColor, gradientColor, colorStrength);

        // ---- MODIFY THE MATERIAL ----
        // This directly modifies the material instance we created
        
        // Set the emission color
        if (materialInstance.HasProperty(emissionColorProperty))
        {
            materialInstance.SetColor(emissionColorProperty, finalEmissionColor);
        }
        
        // Set the emission intensity
        if (materialInstance.HasProperty(emissionIntensityProperty))
        {
            materialInstance.SetFloat(emissionIntensityProperty, currentIntensity);
        }
        
        // For Standard shader, also set _EmissionColor with multiplied intensity
        if (materialInstance.HasProperty("_EmissionColor"))
        {
            materialInstance.SetColor("_EmissionColor", finalEmissionColor * currentIntensity);
        }
        
        // Ensure emission is enabled
        materialInstance.EnableKeyword("_EMISSION");
        materialInstance.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }

    public void SetTargetMaterial(Material newMaterial)
    {
        // Clean up old material instance
        if (materialInstance != null && materialInstance != targetMaterial)
        {
            DestroyImmediate(materialInstance);
        }
        
        targetMaterial = newMaterial;
        
        if (targetMaterial != null)
        {
            materialInstance = new Material(targetMaterial);
            
            // Update base values
            if (materialInstance.HasProperty(emissionColorProperty))
                baseEmissionColor = materialInstance.GetColor(emissionColorProperty);
            
            if (materialInstance.HasProperty(emissionIntensityProperty))
                baseEmissionIntensity = materialInstance.GetFloat(emissionIntensityProperty);
            
            currentIntensity = baseEmissionIntensity;
            
            // Apply to renderer if we have one
            if (particleSystem != null)
            {
                Renderer renderer = particleSystem.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = materialInstance;
                }
            }
        }
    }

    private Gradient GetDefaultGradient()
    {
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

    void OnDestroy()
    {
        // Clean up the material instance
        if (materialInstance != null && materialInstance != targetMaterial)
        {
            DestroyImmediate(materialInstance);
        }
    }
}