using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class AudioReactiveParticles : MonoBehaviour
{
    [Header("=== AUDIO INPUT ===")]
    public AudioSource audioSource;
    [Range(0.1f, 5f)]
    public float sensitivity = 1.5f;
    [Range(0.001f, 0.1f)]
    public float threshold = 0.01f;
    
    [Header("=== RING WAVE SETTINGS ===")]
    [Range(1, 20)]
    public int ringsPerBeat = 5;
    [Range(0.5f, 5f)]
    public float ringStartRadius = 0.5f;
    [Range(1f, 20f)]
    public float ringMaxRadius = 10f;
    [Range(0.1f, 3f)]
    public float ringSpeed = 2f;
    [Range(0.1f, 2f)]
    public float ringThickness = 0.3f;
    [Range(0.5f, 3f)]
    public float ringLifetime = 1.5f;
    
    [Header("=== VISUAL STYLING ===")]
    [Range(0.01f, 0.5f)]
    public float particleSize = 0.1f;
    [Range(0.1f, 5f)]
    public float sizeVariation = 0.5f;
    public Color ringColor = Color.cyan;
    public Color ringColorEnd = new Color(0.2f, 0.5f, 1f, 0f);
    [Range(0f, 5f)]
    public float glowIntensity = 2f;
    
    [Header("=== PERFORMANCE ===")]
    [Range(1000, 50000)]
    public int maxParticles = 10000;
    
    // Private variables
    private ParticleSystem particleSystem;
    private ParticleSystem.Particle[] particles;
    private List<RingParticle> activeRings = new List<RingParticle>();
    private float[] audioBuffer;
    private float smoothedAmplitude;
    private float time;
    private int particleIndex = 0;
    
    // Ring particle data
    private class RingParticle
    {
        public float radius;
        public float speed;
        public float life;
        public float maxLife;
        public float amplitude;
        public float angleOffset;
        public int particleCount;
        public float thickness;
    }
    
    void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
        SetupParticleSystem();
        SetupAudio();
        
        audioBuffer = new float[512];
        particles = new ParticleSystem.Particle[maxParticles];
    }
    
    void SetupParticleSystem()
    {
        var main = particleSystem.main;
        main.maxParticles = maxParticles;
        main.startLifetime = float.MaxValue;
        main.startSpeed = 0f;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = particleSystem.emission;
        emission.enabled = false;
        
        var shape = particleSystem.shape;
        shape.enabled = false;
        
        // Create a soft glowing material
        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = CreateGlowMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
    }
    
    Material CreateGlowMaterial()
    {
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.SetFloat("_Mode", 2);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        return mat;
    }
    
    void SetupAudio()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        if (audioSource == null)
        {
            Debug.LogWarning("No AudioSource found! Please assign one.");
            return;
        }
    }
    
    void Update()
    {
        time += Time.deltaTime;
        
        if (audioSource == null || !audioSource.isPlaying)
            return;
            
        // Process audio
        audioSource.GetOutputData(audioBuffer, 0);
        
        float amplitude = 0f;
        for (int i = 0; i < audioBuffer.Length; i++)
        {
            amplitude += Mathf.Abs(audioBuffer[i]);
        }
        amplitude = (amplitude / audioBuffer.Length) * sensitivity;
        amplitude = Mathf.Clamp01(amplitude);
        
        smoothedAmplitude = Mathf.Lerp(smoothedAmplitude, amplitude, Time.deltaTime * 15f);
        
        // Spawn rings based on audio
        if (smoothedAmplitude > threshold)
        {
            SpawnRings(smoothedAmplitude);
        }
        
        // Update and render particles
        UpdateRings();
        RenderParticles();
    }
    
    void SpawnRings(float amplitude)
    {
        int ringsToSpawn = Mathf.CeilToInt(ringsPerBeat * amplitude);
        ringsToSpawn = Mathf.Min(ringsToSpawn, 5); // Limit to prevent overload
        
        for (int r = 0; r < ringsToSpawn; r++)
        {
            float delay = r * 0.05f; // Slight delay between rings
            int particlesInRing = Mathf.CeilToInt(60 + (amplitude * 120));
            particlesInRing = Mathf.Min(particlesInRing, 300);
            
            RingParticle ring = new RingParticle
            {
                radius = ringStartRadius + (r * 0.3f),
                speed = ringSpeed * (0.8f + Random.Range(0f, 0.4f)),
                life = -delay, // Start with delay
                maxLife = ringLifetime + Random.Range(-0.2f, 0.2f),
                amplitude = amplitude,
                angleOffset = Random.Range(0f, 360f),
                particleCount = particlesInRing,
                thickness = ringThickness * (0.7f + amplitude * 0.6f)
            };
            
            activeRings.Add(ring);
        }
        
        // Limit active rings
        if (activeRings.Count > 50)
        {
            activeRings.RemoveRange(0, activeRings.Count - 50);
        }
    }
    
    void UpdateRings()
    {
        for (int i = activeRings.Count - 1; i >= 0; i--)
        {
            RingParticle ring = activeRings[i];
            
            // Update life
            ring.life += Time.deltaTime;
            
            // Update radius (expand outward)
            if (ring.life > 0)
            {
                float speedMultiplier = 1f + (ring.life / ring.maxLife) * 0.3f;
                ring.radius += ring.speed * Time.deltaTime * speedMultiplier;
            }
            
            // Remove dead rings
            if (ring.life > ring.maxLife || ring.radius > ringMaxRadius)
            {
                activeRings.RemoveAt(i);
            }
        }
    }
    
    void RenderParticles()
    {
        // Count total particles needed
        int totalParticles = 0;
        foreach (var ring in activeRings)
        {
            if (ring.life > 0)
                totalParticles += ring.particleCount;
        }
        
        if (totalParticles == 0)
        {
            particleSystem.Clear();
            return;
        }
        
        totalParticles = Mathf.Min(totalParticles, maxParticles);
        
        int particleIdx = 0;
        
        foreach (var ring in activeRings)
        {
            if (ring.life <= 0 || particleIdx >= totalParticles)
                continue;
                
            float lifeProgress = ring.life / ring.maxLife;
            float alpha = 1f - lifeProgress;
            float radius = ring.radius;
            float ringSize = particleSize * (1f + ring.amplitude * 2f);
            
            // Calculate how many particles to render for this ring
            int count = Mathf.Min(ring.particleCount, totalParticles - particleIdx);
            
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)ring.particleCount) * 360f + ring.angleOffset;
                
                // Add some randomness to make it look organic
                float randomOffset = Random.Range(-ring.thickness, ring.thickness);
                float currentRadius = radius + randomOffset;
                
                // Calculate position (flat on XZ plane)
                Vector3 position = transform.position + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * currentRadius,
                    0f,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * currentRadius
                );
                
                // Add subtle height variation
                position.y += Mathf.Sin(angle * 3f + time * 2f) * 0.05f * ring.amplitude;
                
                // Set particle properties
                particles[particleIdx].position = position;
                particles[particleIdx].rotation = angle;
                
                // Size - gets smaller as it expands
                float sizeMultiplier = 1f - lifeProgress * 0.5f;
                float sizeVariationFactor = 1f + Random.Range(-sizeVariation, sizeVariation) * 0.3f;
                particles[particleIdx].startSize = ringSize * sizeMultiplier * sizeVariationFactor;
                
                // Color - fade out
                Color color = Color.Lerp(ringColor, ringColorEnd, lifeProgress);
                color *= glowIntensity * (0.5f + ring.amplitude * 0.5f);
                color.a = alpha * (0.8f + Mathf.Sin(angle + time) * 0.2f);
                
                particles[particleIdx].startColor = color;
                particles[particleIdx].remainingLifetime = 0.1f;
                
                particleIdx++;
            }
        }
        
        // Apply to particle system
        particleSystem.SetParticles(particles, particleIdx);
    }
    
    // Public control methods
    public void StartEffect()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
        particleSystem.Play();
    }
    
    public void StopEffect()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        particleSystem.Stop();
        activeRings.Clear();
    }
    
    public void SetAudioClip(AudioClip clip)
    {
        if (audioSource != null)
        {
            audioSource.clip = clip;
        }
    }
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, ringStartRadius);
        Gizmos.DrawWireSphere(transform.position, ringMaxRadius);
        
        // Draw direction arrows
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Gizmos.DrawLine(transform.position + dir * ringStartRadius, 
                           transform.position + dir * ringMaxRadius);
        }
    }
}