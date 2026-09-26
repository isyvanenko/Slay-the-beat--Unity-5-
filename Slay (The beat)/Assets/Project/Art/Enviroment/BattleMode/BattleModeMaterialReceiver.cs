using UnityEngine;

/// <summary>
/// Applies and syncs HDR emission material from BattleModeUI to renderers and particle systems.
/// </summary>
public class BattleModeMaterialReceiver : MonoBehaviour
{
    [Header("Material Source")]
    public BattleModeUI battleModeUI;
    
    [Header("Settings")]
    public bool applyOnStart = true;
    public bool syncMaterialToRenderer = true;
    public bool syncMaterialToParticles = true;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    // Private variables
    private Renderer objectRenderer;
    private ParticleSystem particleSystem;
    private Material sharedMaterial;
    private bool isInitialized = false;
    
    void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        particleSystem = GetComponent<ParticleSystem>();
    }
    
    void Start()
    {
        if (battleModeUI == null)
        {
            battleModeUI = FindObjectOfType<BattleModeUI>();
            if (battleModeUI == null)
            {
                Debug.LogWarning($"No BattleModeUI found in scene for {gameObject.name}!");
                enabled = false;
                return;
            }
        }
        
        if (applyOnStart)
        {
            ApplyMaterial();
        }
    }
    
    void Update()
    {
        // Sync continuously if material is shared
        if (isInitialized && sharedMaterial != null)
        {
            // Material updates automatically through the shared instance
        }
    }
    
    /// <summary>
    /// Applies the material from BattleModeUI to this object.
    /// </summary>
    public void ApplyMaterial()
    {
        if (battleModeUI == null) return;
        
        // Get the material from BattleModeUI
        sharedMaterial = battleModeUI.GetMaterial();
        
        if (sharedMaterial == null)
        {
            Debug.LogWarning($"No material available from BattleModeUI for {gameObject.name}");
            return;
        }
        
        // Apply to renderer if it exists
        if (objectRenderer != null && syncMaterialToRenderer)
        {
            objectRenderer.sharedMaterial = sharedMaterial;
            isInitialized = true;
            
            if (showDebugLogs)
                Debug.Log($"Applied shared material to renderer on {gameObject.name}");
        }
        
        // Apply to particle system if it exists
        if (particleSystem != null && syncMaterialToParticles)
        {
            ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null)
            {
                particleRenderer.sharedMaterial = sharedMaterial;
                isInitialized = true;
                
                if (showDebugLogs)
                    Debug.Log($"Applied shared material to particle system on {gameObject.name}");
            }
        }
    }
    
    /// <summary>
    /// Force refresh the material.
    /// </summary>
    public void RefreshMaterial()
    {
        if (battleModeUI != null)
        {
            sharedMaterial = battleModeUI.GetMaterial();
            
            if (objectRenderer != null && syncMaterialToRenderer)
                objectRenderer.sharedMaterial = sharedMaterial;
                
            if (particleSystem != null && syncMaterialToParticles)
            {
                ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                if (particleRenderer != null)
                    particleRenderer.sharedMaterial = sharedMaterial;
            }
        }
    }
    
    /// <summary>
    /// Gets the current shared material.
    /// </summary>
    public Material GetCurrentMaterial()
    {
        return sharedMaterial;
    }
}