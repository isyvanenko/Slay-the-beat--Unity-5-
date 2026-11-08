using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Gold dust with soft light rays: glowing specks drifting and twinkling, blurred motion streaks,
/// slowly turning rays fanning out from the centre and a soft haze behind them.
/// </summary>
/// <remarks>
/// Put it on an empty GameObject. It builds its own particle systems, textures and materials,
/// so there is nothing else to set up, and it plays in the Scene view without pressing Play.
/// The layers are hidden in the Hierarchy: change the effect with the settings here.
/// It renders in the world, so it doesn't show over a Screen Space - Overlay canvas.
/// </remarks>
[ExecuteAlways]
[DisallowMultipleComponent]
public class GoldDustRays : MonoBehaviour
{
    [Header("Colour")]
    [ColorUsage(false)] public Color gold = new Color(1f, 0.76f, 0.28f);
    [ColorUsage(false)] public Color highlight = new Color(1f, 0.95f, 0.75f);
    [Tooltip("Brightness. Above 1 it blooms if the camera has Bloom")]
    [Range(0f, 5f)] public float glow = 1.6f;

    [Header("Dust")]
    [Tooltip("Size of the box the dust floats in")]
    public Vector3 area = new Vector3(6f, 4f, 2f);
    [Tooltip("Roughly how many specks are visible at once")]
    [Range(0, 600)] public int dustAmount = 140;
    [Range(0.01f, 0.5f)] public float dustSize = 0.09f;
    [Tooltip("How much the dust drifts around")]
    [Range(0f, 2f)] public float drift = 0.35f;
    [Tooltip("Upward float speed. Negative makes it fall")]
    [Range(-1f, 1f)] public float rise = 0.12f;

    [Header("Blur Streaks")]
    [Tooltip("Fast motion-blurred sparkles per second")]
    [Range(0, 60)] public float streaksPerSecond = 6f;
    [Range(0f, 1f)] public float streakLength = 0.3f;

    [Header("Rays")]
    [Tooltip("Roughly how many rays are visible at once. Each ray crosses the centre, so it makes two spokes")]
    [Range(0, 30)] public int rayCount = 7;
    public float rayLength = 9f;
    public float rayWidth = 0.5f;
    [Tooltip("Turning speed in degrees per second")]
    [Range(0f, 90f)] public float raySpin = 8f;
    [Range(0f, 1f)] public float rayStrength = 0.3f;

    [Header("Haze")]
    [Range(0f, 1f)] public float hazeStrength = 0.18f;
    [Tooltip("Haze size compared to the dust area")]
    [Range(0.2f, 3f)] public float hazeSize = 1f;

    [Header("Rendering")]
    [Tooltip("Draw order against sprites and other transparent things")]
    public int sortingOrder = 10;
    public bool playOnEnable = true;
    public bool previewInEditor = true;

    const string ShaderName = "Hidden/GoldDustAdditive";
    const string LayerPrefix = "GoldDust_";

    ParticleSystem dust, streaks, rays, haze;
    readonly List<Material> materials = new List<Material>();
    static Texture2D dotTexture, rayTexture;
    bool dirty;
#if UNITY_EDITOR
    double lastEditorTime;
#endif

    /// <summary>Starts the effect (it fades in).</summary>
    public void Play()
    {
        if (dust == null) Build();
        ForEachLayer(ps => ps.Play(false));
    }

    /// <summary>Stops emitting; what's already there fades out.</summary>
    public void Stop() => ForEachLayer(ps => ps.Stop(false, ParticleSystemStopBehavior.StopEmitting));

    /// <summary>Throws out a burst of blurred sparkles, for a hit or a golden moment.</summary>
    public void Burst(int count = 25)
    {
        if (streaks == null) Build();
        streaks.Emit(count);
    }

    void OnEnable()
    {
        Build();
        if (Application.isPlaying)
        {
            if (playOnEnable) Play();
        }
#if UNITY_EDITOR
        else if (previewInEditor)
        {
            // Start the preview already full instead of growing from nothing.
            ForEachLayer(ps => ps.Simulate(4f, false, true, false));
            lastEditorTime = EditorApplication.timeSinceStartup;
        }
#endif
    }

    void OnDisable() => Teardown();

    void OnValidate() => dirty = true; // Building isn't allowed inside OnValidate, so Update applies it.

    void Update()
    {
        if (dirty)
        {
            dirty = false;
            if (dust == null) Build(); else Apply();
        }

#if UNITY_EDITOR
        if (!Application.isPlaying && previewInEditor && dust != null)
        {
            double now = EditorApplication.timeSinceStartup;
            float dt = Mathf.Clamp((float)(now - lastEditorTime), 0f, 0.1f);
            lastEditorTime = now;
            ForEachLayer(ps => ps.Simulate(dt, false, false, false));
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    // ---------- building ----------

    void Build()
    {
        Teardown();

        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogWarning($"GoldDustRays: shader '{ShaderName}' not found (Assets/Resources/Shaders).", this);
            return;
        }
        MakeTextures();

        haze = MakeLayer("Haze", shader, dotTexture);
        rays = MakeLayer("Rays", shader, rayTexture);
        dust = MakeLayer("Dust", shader, dotTexture);
        streaks = MakeLayer("Streaks", shader, dotTexture);

        Apply();
        ForEachLayer(ps => ps.gameObject.SetActive(true));
    }

    ParticleSystem MakeLayer(string name, Shader shader, Texture2D texture)
    {
        var go = new GameObject(LayerPrefix + name);
        go.hideFlags = HideFlags.HideAndDontSave;
        go.SetActive(false); // configure before it starts playing
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 5f;
        main.prewarm = true;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, mainTexture = texture };
        materials.Add(mat);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.maxParticleSize = 20f; // the default would shrink big rays and haze on screen
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return ps;
    }

    void Teardown()
    {
        dust = streaks = rays = haze = null;
        // Remove every hidden layer, including ones left behind by a script reload.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith(LayerPrefix) && (child.hideFlags & HideFlags.DontSave) != 0)
                Kill(child.gameObject);
        }
        foreach (var m in materials) if (m != null) Kill(m);
        materials.Clear();
    }

    static void Kill(Object o)
    {
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }

    void ForEachLayer(System.Action<ParticleSystem> action)
    {
        if (haze != null) action(haze);
        if (rays != null) action(rays);
        if (dust != null) action(dust);
        if (streaks != null) action(streaks);
    }

    // ---------- settings ----------

    void Apply()
    {
        if (dust == null) return;
        foreach (var m in materials) m.SetFloat("_Glow", glow);

        ApplyDust();
        ApplyStreaks();
        ApplyRays();
        ApplyHaze();

        ForEachLayer(ps =>
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sortingOrder = sortingOrder;
        });
    }

    void ApplyDust()
    {
        var main = dust.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(dustSize * 0.35f, dustSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(gold, highlight);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(1, dustAmount * 2);

        var emission = dust.emission;
        emission.rateOverTime = dustAmount / 4.5f; // amount / average lifetime

        var shape = dust.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = area;
        shape.randomDirectionAmount = 1f;

        var velocity = dust.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(rise * 0.5f, rise * 1.5f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var noise = dust.noise;
        noise.enabled = drift > 0f;
        noise.strength = drift;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.25f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.sizeAmount = new ParticleSystem.MinMaxCurve(0.9f); // twinkle

        var col = dust.colorOverLifetime;
        col.enabled = true;
        col.color = Fade(1f, 0.2f, 0.75f);

        dust.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Billboard;
    }

    void ApplyStreaks()
    {
        var main = streaks.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(dustSize * 0.3f, dustSize * 0.6f);
        main.startRotation = 0f;
        main.startColor = new ParticleSystem.MinMaxGradient(highlight, Color.white);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;

        var emission = streaks.emission;
        emission.rateOverTime = streaksPerSecond;

        var shape = streaks.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = area * 0.8f;
        shape.randomDirectionAmount = 1f;

        var limit = streaks.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 0.4f;
        limit.dampen = 0.08f; // they shoot out and slow down

        var size = streaks.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

        var col = streaks.colorOverLifetime;
        col.enabled = true;
        col.color = Fade(1f, 0.05f, 0.5f);

        var r = streaks.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = streakLength;
        r.lengthScale = 1.5f;
        r.cameraVelocityScale = 0f;
    }

    void ApplyRays()
    {
        var main = rays.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSpeed = 0f;
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(rayWidth * 0.4f, rayWidth * 1.4f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(rayLength * 0.6f, rayLength);
        main.startSizeZ = 1f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            WithAlpha(gold, rayStrength), WithAlpha(highlight, rayStrength * 0.7f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = Mathf.Max(1, rayCount * 3);

        var emission = rays.emission;
        emission.rateOverTime = rayCount / 4.5f;

        var shape = rays.shape;
        shape.enabled = false; // all rays come from the centre

        float spin = raySpin * Mathf.Deg2Rad;
        var rot = rays.rotationOverLifetime;
        rot.enabled = spin > 0f;
        rot.z = new ParticleSystem.MinMaxCurve(-spin, spin);

        var col = rays.colorOverLifetime;
        col.enabled = true;
        col.color = Fade(1f, 0.35f, 0.65f);

        rays.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Billboard;
    }

    void ApplyHaze()
    {
        float s = Mathf.Max(area.x, area.y) * hazeSize;
        var main = haze.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(s * 0.6f, s);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(WithAlpha(gold, hazeStrength));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 8;

        var emission = haze.emission;
        emission.rateOverTime = hazeStrength > 0f ? 0.6f : 0f;

        var shape = haze.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = s * 0.15f;

        var col = haze.colorOverLifetime;
        col.enabled = true;
        col.color = Fade(1f, 0.4f, 0.6f);
    }

    static Color WithAlpha(Color c, float a) { c.a = a; return c; }

    static ParticleSystem.MinMaxGradient Fade(float peak, float fadeInEnd, float fadeOutStart)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peak, fadeInEnd),
                new GradientAlphaKey(peak, fadeOutStart),
                new GradientAlphaKey(0f, 1f),
            });
        return new ParticleSystem.MinMaxGradient(g);
    }

    // ---------- textures ----------

    static void MakeTextures()
    {
        if (dotTexture == null)
        {
            // Soft round speck: a bright core inside a wide blurry halo.
            const int n = 64;
            dotTexture = NewTexture(n, n, "GoldDust_Dot");
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float halo = Mathf.Pow(Mathf.Clamp01(1f - r), 2.5f);
                float core = Mathf.Exp(-r * r * 40f);
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(halo * 0.55f + core * 0.6f));
            }
            dotTexture.SetPixels(px);
            dotTexture.Apply(true, true);
        }

        if (rayTexture == null)
        {
            // Light ray through the middle: blurry sides, brightest at the centre, fading out at both ends.
            const int w = 32, h = 256;
            rayTexture = NewTexture(w, h, "GoldDust_Ray");
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w * 2f - 1f, v = Mathf.Abs((y + 0.5f) / h * 2f - 1f);
                float width = Mathf.Lerp(0.35f, 1f, v); // narrow at the centre, spreading outwards
                float across = Mathf.Exp(-(u * u) / (width * width) * 6f) * Mathf.Clamp01(1f - Mathf.Abs(u));
                float along = Mathf.Pow(1f - v, 1.6f);
                px[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(across * along));
            }
            rayTexture.SetPixels(px);
            rayTexture.Apply(true, true);
        }
    }

    static Texture2D NewTexture(int w, int h, string name) =>
        new Texture2D(w, h, TextureFormat.RGBA32, true, true)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };
}
