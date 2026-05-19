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
    [Tooltip("Leave empty to use the gradient from the current song")]
    public Gradient colorGradient;
    [Tooltip("How strong the color shift is (lower = more subtle)")]
    [Range(0f, 1f)] public float colorStrength = 0.25f;
    [Tooltip("Automatically update gradient from song data when song changes")]
    public bool autoUpdateGradientFromSong = true;

    private float[] samples = new float[512];
    private float currentAlpha;
    private float alphaVelocity;

    private float beatValue;
    private float beatVelocity;

    private Material runtimeMaterial;
    private Color baseColor;
    private SongGradeData currentSongData;

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

    // New method to update gradient from current song
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

    private void TryUpdateGradientFromSong()
    {
        // Try to get current song from GameDataBridge
        if (GameDataBridge.SelectedSong != null && GameDataBridge.SelectedSong != currentSongData)
        {
            UpdateGradientFromSong(GameDataBridge.SelectedSong);
        }
        
        // Alternative: Try to get from MusicManager if it has song data reference
        if (currentSongData == null && MusicManager.Instance != null)
        {
            // You might need to add a CurrentSong property to MusicManager
            // For now, this is a placeholder
        }
    }

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

    // Manual method to force gradient update
    public void SetGradient(Gradient newGradient)
    {
        colorGradient = newGradient;
    }

    // Reset to base color
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