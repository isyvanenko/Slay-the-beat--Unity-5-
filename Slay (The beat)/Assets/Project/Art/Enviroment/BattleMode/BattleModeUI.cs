using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Manages the battle mode UI carousel with flip animation and HDR emission control.
/// </summary>
public class BattleModeUI : MonoBehaviour
{
    [System.Serializable]
    public class ImageData
    {
        public Sprite imageSprite;
        public SongGradeData songData;
        
        [Header("HDR Emission Settings (Material)")]
        [ColorUsage(true, true)]
        public Color emissionColor = Color.white;
        [Range(0f, 10f)]
        public float emissionIntensity = 1f;
        
        [Header("Particle System Settings")]
        [ColorUsage(true, true)]
        public Color particleColor = Color.white;
        [Range(0f, 10f)]
        public float particleIntensity = 1f;
        [Range(0f, 5f)]
        public float particleSizeMultiplier = 1f;
        [Range(0f, 5f)]
        public float particleSpeedMultiplier = 1f;
    }
    
    [Header("Image Data")]
    public List<ImageData> imageList = new List<ImageData>();
    
    [Header("UI Display")]
    public Image mainImageDisplay;
    public TextMeshProUGUI imageInfoText;
    public TextMeshProUGUI counterText;
    public CanvasGroup canvasGroup;
    
    [Header("Material Emission (HDR)")]
    public Material targetMaterial;
    public string emissionColorProperty = "_EmissionColor";
    public float emissionTransitionSpeed = 5f;
    
    [Header("Particle System")]
    public ParticleSystem targetParticleSystem;
    public Material particleMaterial;
    public bool syncParticleMaterial = true;
    
    [Header("Particle System Controls")]
    public bool controlParticleColor = true;
    public bool controlParticleSize = false;
    public bool controlParticleSpeed = false;
    public bool controlParticleEmissionRate = false;
    public float particleTransitionSpeed = 5f;
    
    [Header("Emission Color Previews")]
    public Image materialPreviewImage;
    public Image particlePreviewImage;
    
    [Header("Flip Animation")]
    public AnimationCurve flipAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float flipDuration = 0.6f;
    public bool use3DFlip = true;
    
    [Header("Card Flip Visuals")]
    public Image backsideImage;
    public Sprite backsideSprite;
    public Color backsideColor = Color.gray;
    
    [Header("Navigation")]
    public bool loopImages = true;
    
    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip navigateSound;
    public AudioClip selectSound;
    public AudioClip flipSound;
    
    // Private variables
    private int currentIndex = 0;
    private bool isTransitioning = false;
    private InputActions input;
    
    // Material emission variables
    private Color targetEmissionColor;
    private float targetEmissionIntensity;
    
    // Particle system variables
    private Material runtimeParticleMaterial;
    private ParticleSystem.MainModule particleMain;
    private ParticleSystem.EmissionModule particleEmission;
    private bool hasParticleSystem = false;
    private float originalEmissionRate;
    private Color targetParticleColor;
    private float targetParticleIntensity;
    private float targetParticleSize;
    private float targetParticleSpeed;
    private float targetEmissionRate;
    
    // Events
    public System.Action<int> OnImageChanged;
    public System.Action<int> OnImageSelected;
    public System.Action<Color, float> OnEmissionChanged;
    public System.Action<Color, float, float, float> OnParticlePropertiesChanged;
    
    void Awake()
    {
        input = new InputActions();
        input.UI.NavigateLeft.performed += _ => NavigateLeft();
        input.UI.NavigateRight.performed += _ => NavigateRight();
        input.UI.Select.performed += _ => SelectCurrentImage();
    }
    
    void Start()
    {
        if (imageList.Count == 0)
        {
            Debug.LogWarning("No images assigned to BattleModeUI!");
            return;
        }
        
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        if (mainImageDisplay == null)
        {
            mainImageDisplay = GetComponent<Image>();
        }
        
        // Setup main material for HDR emission
        if (targetMaterial != null)
        {
            targetMaterial.EnableKeyword("_EMISSION");
            
            ImageData firstImage = imageList[0];
            targetEmissionColor = firstImage.emissionColor;
            targetEmissionIntensity = firstImage.emissionIntensity;
            
            ApplyEmissionToMaterial(targetMaterial, targetEmissionColor, targetEmissionIntensity);
            
            Debug.Log($"Initial HDR emission set: Color={targetEmissionColor}, Intensity={targetEmissionIntensity}");
        }
        else
        {
            Debug.LogError("Target Material is not assigned!");
        }
        
        // Setup particle system
        if (targetParticleSystem != null)
        {
            hasParticleSystem = true;
            particleMain = targetParticleSystem.main;
            particleEmission = targetParticleSystem.emission;
            originalEmissionRate = particleEmission.rateOverTime.constant;
            
            // Setup particle material
            if (syncParticleMaterial)
            {
                ParticleSystemRenderer particleRenderer = targetParticleSystem.GetComponent<ParticleSystemRenderer>();
                
                if (particleRenderer != null)
                {
                    if (particleMaterial != null)
                    {
                        runtimeParticleMaterial = new Material(particleMaterial);
                        particleRenderer.material = runtimeParticleMaterial;
                        runtimeParticleMaterial.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        runtimeParticleMaterial = particleRenderer.material;
                        runtimeParticleMaterial.EnableKeyword("_EMISSION");
                    }
                    
                    Debug.Log($"Particle material initialized: {runtimeParticleMaterial.name}");
                }
            }
            
            // Set initial particle properties from first image
            ImageData firstImage = imageList[0];
            targetParticleColor = firstImage.particleColor;
            targetParticleIntensity = firstImage.particleIntensity;
            targetParticleSize = firstImage.particleSizeMultiplier;
            targetParticleSpeed = firstImage.particleSpeedMultiplier;
            targetEmissionRate = originalEmissionRate * targetParticleIntensity;
            
            // Apply initial particle properties
            ApplyParticleProperties(
                targetParticleColor,
                targetParticleIntensity,
                targetParticleSize,
                targetParticleSpeed
            );
            
            Debug.Log($"Initial particle properties set: Color={targetParticleColor}, Intensity={targetParticleIntensity}, Size={targetParticleSize}, Speed={targetParticleSpeed}");
        }
        else
        {
            Debug.LogWarning("Target Particle System is not assigned!");
        }
        
        // Setup backside image
        if (backsideImage != null)
        {
            backsideImage.sprite = backsideSprite;
            backsideImage.color = backsideColor;
            backsideImage.gameObject.SetActive(false);
        }
        
        // Update previews
        UpdateMaterialPreview();
        UpdateParticlePreview();
        
        DisplayCurrentImage();
        UpdateUIInfo();
        
        // Fire initial events
        OnEmissionChanged?.Invoke(targetEmissionColor * targetEmissionIntensity, targetEmissionIntensity);
        OnParticlePropertiesChanged?.Invoke(targetParticleColor, targetParticleIntensity, targetParticleSize, targetParticleSpeed);
    }
    
    void Update()
    {
        // Handle HDR emission transition for main material
        if (targetMaterial != null)
        {
            Color currentColor = targetMaterial.GetColor(emissionColorProperty);
            Color targetColorWithIntensity = targetEmissionColor * targetEmissionIntensity;
            
            if (currentColor != targetColorWithIntensity)
            {
                Color newColor = Color.Lerp(currentColor, targetColorWithIntensity, Time.deltaTime * emissionTransitionSpeed);
                targetMaterial.SetColor(emissionColorProperty, newColor);
            }
        }
        
        // Handle HDR emission transition for particle material
        if (runtimeParticleMaterial != null && syncParticleMaterial)
        {
            Color currentColor = runtimeParticleMaterial.GetColor(emissionColorProperty);
            Color targetColorWithIntensity = targetParticleColor * targetParticleIntensity;
            
            if (currentColor != targetColorWithIntensity)
            {
                Color newColor = Color.Lerp(currentColor, targetColorWithIntensity, Time.deltaTime * particleTransitionSpeed);
                runtimeParticleMaterial.SetColor(emissionColorProperty, newColor);
            }
        }
        
        // Handle particle system property transitions
        if (hasParticleSystem)
        {
            // Transition particle color
            if (controlParticleColor)
            {
                Color currentColor = particleMain.startColor.color;
                Color targetColor = targetParticleColor * targetParticleIntensity;
                
                if (currentColor != targetColor)
                {
                    Color newColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * particleTransitionSpeed);
                    particleMain.startColor = newColor;
                }
            }
            
            // Transition particle size
            if (controlParticleSize)
            {
                float currentSize = particleMain.startSize.constant;
                float targetSize = targetParticleSize;
                
                if (Mathf.Abs(currentSize - targetSize) > 0.001f)
                {
                    float newSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * particleTransitionSpeed);
                    particleMain.startSize = newSize;
                }
            }
            
            // Transition particle speed
            if (controlParticleSpeed)
            {
                float currentSpeed = particleMain.startSpeed.constant;
                float targetSpeed = targetParticleSpeed;
                
                if (Mathf.Abs(currentSpeed - targetSpeed) > 0.001f)
                {
                    float newSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * particleTransitionSpeed);
                    particleMain.startSpeed = newSpeed;
                }
            }
            
            // Transition emission rate
            if (controlParticleEmissionRate)
            {
                float currentRate = particleEmission.rateOverTime.constant;
                float targetRate = originalEmissionRate * targetParticleIntensity;
                
                if (Mathf.Abs(currentRate - targetRate) > 0.001f)
                {
                    float newRate = Mathf.Lerp(currentRate, targetRate, Time.deltaTime * particleTransitionSpeed);
                    
                    ParticleSystem.MinMaxCurve rateCurve = particleEmission.rateOverTime;
                    rateCurve.constant = newRate;
                    particleEmission.rateOverTime = rateCurve;
                }
            }
        }
    }
    
    void OnEnable() => input?.Enable();
    void OnDisable() => input?.Disable();
    
    public void NavigateLeft()
    {
        if (isTransitioning || imageList.Count == 0) return;
        
        int newIndex = currentIndex - 1;
        if (newIndex < 0)
        {
            if (loopImages)
                newIndex = imageList.Count - 1;
            else
                return;
        }
        
        if (newIndex != currentIndex)
        {
            PlaySound(navigateSound);
            StartFlipAnimation(newIndex);
        }
    }
    
    public void NavigateRight()
    {
        if (isTransitioning || imageList.Count == 0) return;
        
        int newIndex = currentIndex + 1;
        if (newIndex >= imageList.Count)
        {
            if (loopImages)
                newIndex = 0;
            else
                return;
        }
        
        if (newIndex != currentIndex)
        {
            PlaySound(navigateSound);
            StartFlipAnimation(newIndex);
        }
    }
    
    public void SelectCurrentImage()
    {
        if (isTransitioning || imageList.Count == 0) return;
        
        PlaySound(selectSound);
        OnImageSelected?.Invoke(currentIndex);
        
        SongGradeData songData = GetCurrentSongData();
        if (songData != null)
        {
            Debug.Log($"Selected: {songData.songName}");
        }
    }
    
    private void StartFlipAnimation(int newIndex)
    {
        isTransitioning = true;
        PlaySound(flipSound);
        
        // Get the new image data
        ImageData newImageData = imageList[newIndex];
        
        // Update material emission
        targetEmissionColor = newImageData.emissionColor;
        targetEmissionIntensity = newImageData.emissionIntensity;
        ApplyEmissionToMaterial(targetMaterial, targetEmissionColor, targetEmissionIntensity);
        
        // Update particle properties
        targetParticleColor = newImageData.particleColor;
        targetParticleIntensity = newImageData.particleIntensity;
        targetParticleSize = newImageData.particleSizeMultiplier;
        targetParticleSpeed = newImageData.particleSpeedMultiplier;
        targetEmissionRate = originalEmissionRate * targetParticleIntensity;
        
        // Apply particle properties immediately
        ApplyParticleProperties(
            targetParticleColor,
            targetParticleIntensity,
            targetParticleSize,
            targetParticleSpeed
        );
        
        // Update material previews
        UpdateMaterialPreview();
        UpdateParticlePreview();
        
        // Fire events
        OnEmissionChanged?.Invoke(targetEmissionColor * targetEmissionIntensity, targetEmissionIntensity);
        OnParticlePropertiesChanged?.Invoke(targetParticleColor, targetParticleIntensity, targetParticleSize, targetParticleSpeed);
        
        StartCoroutine(AnimateFlip(newIndex));
    }
    
    private System.Collections.IEnumerator AnimateFlip(int newIndex)
    {
        float elapsed = 0f;
        float duration = flipDuration;
        
        if (backsideImage != null)
        {
            backsideImage.gameObject.SetActive(true);
            backsideImage.transform.localScale = new Vector3(1f, 1f, 1f);
        }
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float curveValue = flipAnimationCurve.Evaluate(progress);
            
            float flipAngle = Mathf.Lerp(0f, 180f, curveValue);
            
            if (use3DFlip)
            {
                Vector3 rotation = mainImageDisplay.transform.eulerAngles;
                rotation.y = flipAngle;
                mainImageDisplay.transform.eulerAngles = rotation;
                
                if (backsideImage != null && backsideImage.gameObject.activeSelf)
                {
                    Vector3 backRotation = backsideImage.transform.eulerAngles;
                    backRotation.y = flipAngle + 180f;
                    backsideImage.transform.eulerAngles = backRotation;
                }
            }
            else
            {
                Vector3 scale = mainImageDisplay.transform.localScale;
                scale.x = Mathf.Lerp(1f, -1f, curveValue);
                mainImageDisplay.transform.localScale = scale;
            }
            
            if (progress >= 0.5f && !isTransitioning)
            {
                currentIndex = newIndex;
                DisplayCurrentImage();
                UpdateUIInfo();
                OnImageChanged?.Invoke(currentIndex);
                
                if (backsideImage != null)
                {
                    backsideImage.gameObject.SetActive(false);
                }
            }
            
            yield return null;
        }
        
        if (use3DFlip)
        {
            Vector3 finalRotation = mainImageDisplay.transform.eulerAngles;
            finalRotation.y = 0f;
            mainImageDisplay.transform.eulerAngles = finalRotation;
        }
        else
        {
            Vector3 finalScale = mainImageDisplay.transform.localScale;
            finalScale.x = 1f;
            mainImageDisplay.transform.localScale = finalScale;
        }
        
        currentIndex = newIndex;
        DisplayCurrentImage();
        UpdateUIInfo();
        OnImageChanged?.Invoke(currentIndex);
        
        if (backsideImage != null)
        {
            backsideImage.gameObject.SetActive(false);
        }
        
        isTransitioning = false;
    }
    
    private void ApplyEmissionToMaterial(Material material, Color color, float intensity)
    {
        if (material == null) return;
        
        Color finalColor = color * intensity;
        material.SetColor(emissionColorProperty, finalColor);
        
        Debug.Log($"Applied emission to {material.name}: Color={color}, Intensity={intensity}, Final={finalColor}");
    }
    
    private void ApplyParticleProperties(Color color, float intensity, float sizeMultiplier, float speedMultiplier)
    {
        if (!hasParticleSystem) return;
        
        // Apply to particle material
        if (runtimeParticleMaterial != null && syncParticleMaterial)
        {
            Color finalColor = color * intensity;
            runtimeParticleMaterial.SetColor(emissionColorProperty, finalColor);
        }
        
        // Apply to particle system properties
        if (controlParticleColor)
        {
            Color finalColor = color * intensity;
            particleMain.startColor = finalColor;
        }
        
        if (controlParticleSize)
        {
            particleMain.startSize = sizeMultiplier;
        }
        
        if (controlParticleSpeed)
        {
            particleMain.startSpeed = speedMultiplier;
        }
        
        if (controlParticleEmissionRate)
        {
            float newRate = originalEmissionRate * intensity;
            ParticleSystem.MinMaxCurve rateCurve = particleEmission.rateOverTime;
            rateCurve.constant = newRate;
            particleEmission.rateOverTime = rateCurve;
        }
        
        Debug.Log($"Applied particle properties: Color={color}, Intensity={intensity}, Size={sizeMultiplier}, Speed={speedMultiplier}");
    }
    
    private void UpdateMaterialPreview()
    {
        if (materialPreviewImage != null)
        {
            Color previewColor = targetEmissionColor * targetEmissionIntensity;
            previewColor.r = Mathf.Clamp01(previewColor.r);
            previewColor.g = Mathf.Clamp01(previewColor.g);
            previewColor.b = Mathf.Clamp01(previewColor.b);
            previewColor.a = 1f;
            
            materialPreviewImage.color = previewColor;
        }
    }
    
    private void UpdateParticlePreview()
    {
        if (particlePreviewImage != null)
        {
            Color previewColor = targetParticleColor * targetParticleIntensity;
            previewColor.r = Mathf.Clamp01(previewColor.r);
            previewColor.g = Mathf.Clamp01(previewColor.g);
            previewColor.b = Mathf.Clamp01(previewColor.b);
            previewColor.a = 1f;
            
            particlePreviewImage.color = previewColor;
        }
    }
    
    public SongGradeData GetCurrentSongData()
    {
        if (currentIndex < 0 || currentIndex >= imageList.Count)
            return null;
        return imageList[currentIndex].songData;
    }
    
    public ImageData GetCurrentImageData()
    {
        if (currentIndex < 0 || currentIndex >= imageList.Count)
            return null;
        return imageList[currentIndex];
    }
    
    public Sprite GetCurrentSprite()
    {
        if (currentIndex < 0 || currentIndex >= imageList.Count)
            return null;
        return imageList[currentIndex].imageSprite;
    }
    
    public void SetImageIndex(int newIndex)
    {
        if (newIndex < 0 || newIndex >= imageList.Count || isTransitioning)
            return;
            
        if (newIndex != currentIndex)
        {
            StartFlipAnimation(newIndex);
        }
    }
    
    public int GetCurrentIndex()
    {
        return currentIndex;
    }
    
    public int GetImageCount()
    {
        return imageList.Count;
    }
    
    private void DisplayCurrentImage()
    {
        if (mainImageDisplay != null && currentIndex < imageList.Count)
        {
            mainImageDisplay.sprite = imageList[currentIndex].imageSprite;
            mainImageDisplay.transform.eulerAngles = Vector3.zero;
            mainImageDisplay.transform.localScale = Vector3.one;
        }
    }
    
    private void UpdateUIInfo()
    {
        if (imageInfoText != null && currentIndex < imageList.Count)
        {
            SongGradeData songData = imageList[currentIndex].songData;
            if (songData != null)
                imageInfoText.text = songData.songName;
            else
                imageInfoText.text = $"Image {currentIndex + 1}";
        }
        
        if (counterText != null)
        {
            counterText.text = $"{currentIndex + 1} / {imageList.Count}";
        }
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }
    
    /// <summary>
    /// Gets the main material.
    /// </summary>
    public Material GetMaterial()
    {
        return targetMaterial;
    }
    
    /// <summary>
    /// Gets the particle material.
    /// </summary>
    public Material GetParticleMaterial()
    {
        return runtimeParticleMaterial;
    }
    
    /// <summary>
    /// Gets the current particle color.
    /// </summary>
    public Color GetCurrentParticleColor()
    {
        return targetParticleColor;
    }
    
    /// <summary>
    /// Gets the current particle intensity.
    /// </summary>
    public float GetCurrentParticleIntensity()
    {
        return targetParticleIntensity;
    }
    
    /// <summary>
    /// Manually set particle properties.
    /// </summary>
    public void SetParticleProperties(Color color, float intensity, float sizeMultiplier = 1f, float speedMultiplier = 1f)
    {
        targetParticleColor = color;
        targetParticleIntensity = intensity;
        targetParticleSize = sizeMultiplier;
        targetParticleSpeed = speedMultiplier;
        
        ApplyParticleProperties(color, intensity, sizeMultiplier, speedMultiplier);
        UpdateParticlePreview();
        OnParticlePropertiesChanged?.Invoke(color, intensity, sizeMultiplier, speedMultiplier);
    }
}