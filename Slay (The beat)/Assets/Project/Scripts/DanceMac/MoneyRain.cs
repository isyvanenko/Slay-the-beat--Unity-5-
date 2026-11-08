using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Money falling from above: paper bills that tumble and flutter side to side, and gold coins
/// that spin and drop faster.
/// </summary>
/// <remarks>
/// Put it on an empty GameObject above the stage: the money appears across a box at this object
/// and falls down. It builds its own particle systems, textures and materials, and plays in the
/// Scene view without pressing Play. The layers are hidden in the Hierarchy: change the effect
/// with the settings here. It renders in the world, so it doesn't show over a Screen Space - Overlay canvas.
/// </remarks>
[ExecuteAlways]
[DisallowMultipleComponent]
public class MoneyRain : MonoBehaviour
{
    [Header("Area")]
    [Tooltip("Width (X) and depth (Z) of the area the money appears over, at this object's height")]
    public Vector2 spawnArea = new Vector2(12f, 4f);
    [Tooltip("How far it falls before disappearing")]
    public float fallHeight = 10f;
    [Tooltip("Start with money already all the way down instead of the first bills just appearing")]
    public bool startFull = false;

    [Header("Bills")]
    [Range(0, 200)] public float billsPerSecond = 25f;
    [Range(0.05f, 2f)] public float billSize = 0.45f;
    [Tooltip("Top falling speed of a bill")]
    [Range(0.2f, 10f)] public float billFallSpeed = 1.8f;
    [Tooltip("How much bills sway side to side")]
    [Range(0f, 3f)] public float flutter = 1f;
    [Tooltip("Tumbling speed in degrees per second")]
    [Range(0f, 720f)] public float billSpin = 200f;
    [Tooltip("Optional bill picture. A dollar-style bill is drawn when empty")]
    public Texture2D billTexture;
    public Color billTint = Color.white;

    [Header("Coins")]
    [Range(0, 100)] public float coinsPerSecond = 8f;
    [Range(0.02f, 1f)] public float coinSize = 0.2f;
    [Range(0.2f, 15f)] public float coinFallSpeed = 4f;
    [Range(0f, 1440f)] public float coinSpin = 540f;
    [Tooltip("Optional coin picture. A gold coin is drawn when empty")]
    public Texture2D coinTexture;
    public Color coinTint = Color.white;

    [Header("Playback")]
    public bool playOnEnable = true;
    public bool previewInEditor = true;

    const string ShaderName = "Hidden/MoneyRainParticle";
    const string LayerPrefix = "MoneyRain_";

    ParticleSystem bills, coins;
    readonly List<Material> materials = new List<Material>();
    static Texture2D defaultBill, defaultCoin;
    bool dirty;
#if UNITY_EDITOR
    double lastEditorTime;
#endif

    /// <summary>Starts the money falling.</summary>
    public void Play()
    {
        if (bills == null) Build();
        ForEachLayer(ps => ps.Play(false));
    }

    /// <summary>Stops new money; what's already falling keeps falling.</summary>
    public void Stop() => ForEachLayer(ps => ps.Stop(false, ParticleSystemStopBehavior.StopEmitting));

    /// <summary>Drops a big handful at once, for a hit or a jackpot moment.</summary>
    public void MakeItRain(int billCount = 60, int coinCount = 20)
    {
        if (bills == null) Build();
        if (bills != null) bills.Emit(billCount);
        if (coins != null) coins.Emit(coinCount);
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
            ForEachLayer(ps => ps.Simulate(startFull ? BillLifetime() : 0.5f, false, true, false));
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
            if (bills == null) Build(); else Apply();
        }

#if UNITY_EDITOR
        if (!Application.isPlaying && previewInEditor && bills != null)
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
            Debug.LogWarning($"MoneyRain: shader '{ShaderName}' not found (Assets/Resources/Shaders).", this);
            return;
        }
        MakeTextures();

        bills = MakeLayer("Bills", shader);
        coins = MakeLayer("Coins", shader);

        Apply();
        ForEachLayer(ps => ps.gameObject.SetActive(true));
    }

    ParticleSystem MakeLayer(string name, Shader shader)
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
        main.prewarm = startFull;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        materials.Add(mat);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.alignment = ParticleSystemRenderSpace.World; // flat pieces turning in 3D, not facing the camera
        r.maxParticleSize = 20f;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
        {
            ParticleSystemVertexStream.Position,
            ParticleSystemVertexStream.Normal,
            ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV,
        });
        return ps;
    }

    void Teardown()
    {
        bills = coins = null;
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
        if (bills != null) action(bills);
        if (coins != null) action(coins);
    }

    // ---------- settings ----------

    float BillLifetime() => fallHeight / Mathf.Max(0.1f, billFallSpeed) * 1.2f + 0.6f;
    float CoinLifetime() => fallHeight / Mathf.Max(0.1f, coinFallSpeed) * 1.15f + 0.4f;

    void Apply()
    {
        if (bills == null) return;

        materials[0].mainTexture = billTexture != null ? billTexture : defaultBill;
        materials[1].mainTexture = coinTexture != null ? coinTexture : defaultCoin;

        // Bills: a 2.2 : 1 rectangle, so give the picture the same shape.
        Texture billTex = materials[0].mainTexture;
        float billAspect = billTex != null && billTex.width > 0 ? (float)billTex.height / billTex.width : 0.45f;

        SetUp(bills, billsPerSecond, BillLifetime(), billFallSpeed, 0.25f,
            new Vector2(billSize * 0.85f, billSize * 1.15f), billAspect, billTint, billSpin, flutter);
        SetUp(coins, coinsPerSecond, CoinLifetime(), coinFallSpeed, 0.6f,
            new Vector2(coinSize * 0.85f, coinSize * 1.15f), 1f, coinTint, coinSpin, flutter * 0.25f);
    }

    void SetUp(ParticleSystem ps, float rate, float lifetime, float fallSpeed, float gravity,
        Vector2 sizeRange, float aspect, Color tint, float spinDegrees, float sway)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.9f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, fallSpeed * 0.3f);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startSizeY = new ParticleSystem.MinMaxCurve(sizeRange.x * aspect, sizeRange.y * aspect);
        main.startSizeZ = 1f;
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(tint, tint * 0.8f + new Color(0, 0, 0, 0.2f));
        main.gravityModifier = gravity;
        main.maxParticles = Mathf.Clamp(Mathf.CeilToInt(rate * lifetime * 1.5f) + 100, 100, 5000);

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.rotation = new Vector3(90f, 0f, 0f); // emit downwards
        shape.scale = new Vector3(spawnArea.x, spawnArea.y, 0.2f); // turned with the shape: its Y is the world's depth
        shape.randomDirectionAmount = 0.3f;

        // Air drag: they speed up a little and then fall at a steady speed.
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = fallSpeed;
        limit.dampen = 0.25f;

        var noise = ps.noise;
        noise.enabled = sway > 0f;
        noise.strength = sway;
        noise.frequency = 0.45f;
        noise.scrollSpeed = 0.3f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        float spin = spinDegrees * Mathf.Deg2Rad;
        var rot = ps.rotationOverLifetime;
        rot.enabled = spin > 0f;
        rot.separateAxes = true;
        rot.x = new ParticleSystem.MinMaxCurve(-spin, spin);
        rot.y = new ParticleSystem.MinMaxCurve(-spin * 0.6f, spin * 0.6f);
        rot.z = new ParticleSystem.MinMaxCurve(-spin * 0.4f, spin * 0.4f);

        // Cut-out pieces can't fade, so they pop in at the top and shrink away at the end.
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.04f, 1f), new Keyframe(0.9f, 1f), new Keyframe(1f, 0f)));
    }

    // ---------- textures ----------

    static void MakeTextures()
    {
        if (defaultBill == null) defaultBill = DrawBill();
        if (defaultCoin == null) defaultCoin = DrawCoin();
    }

    static Texture2D DrawBill()
    {
        const int w = 160, h = 72;
        var paper = new Color(0.72f, 0.82f, 0.62f);
        var ink = new Color(0.16f, 0.36f, 0.2f);
        var light = new Color(0.86f, 0.92f, 0.78f);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            // Coordinates from the centre, in pixels.
            float fx = x + 0.5f - w / 2f, fy = y + 0.5f - h / 2f;
            float ex = w / 2f - Mathf.Abs(fx), ey = h / 2f - Mathf.Abs(fy); // distance to the edges
            float edge = Mathf.Min(ex, ey);

            Color c = paper;
            if (edge < 2f) c = Color.Lerp(ink, paper, 0.3f);             // outer edge
            else if (edge > 4f && edge < 6f) c = ink;                      // frame line
            else if (edge >= 6f && edge < 8f) c = Color.Lerp(paper, ink, 0.25f);

            // Corner number circles.
            float cx = w / 2f - 16f, cy = h / 2f - 15f;
            float corner = new Vector2(Mathf.Abs(fx) - cx, Mathf.Abs(fy) - cy).magnitude;
            if (corner < 7f) c = corner > 5.5f ? ink : light;

            // Centre oval with a $.
            float ox = fx / 24f, oy = fy / 26f;
            float oval = Mathf.Sqrt(ox * ox + oy * oy);
            if (oval < 1f)
            {
                c = oval > 0.88f ? ink : light;
                if (Dollar(fx, fy, 19f)) c = ink;
            }

            // Faint stripes across the paper.
            if (oval >= 1f && edge >= 8f && corner >= 7f && ((x + y / 3) % 9 == 0)) c = Color.Lerp(c, ink, 0.18f);

            px[y * w + x] = c;
        }
        return Finish(w, h, px, "MoneyRain_Bill");
    }

    static Texture2D DrawCoin()
    {
        const int n = 64;
        var gold = new Color(1f, 0.78f, 0.25f);
        var dark = new Color(0.72f, 0.45f, 0.08f);
        var bright = new Color(1f, 0.93f, 0.6f);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float fx = x + 0.5f - n / 2f, fy = y + 0.5f - n / 2f;
            float r = Mathf.Sqrt(fx * fx + fy * fy) / (n / 2f - 1f);
            if (r > 1f) { px[y * n + x] = new Color(0, 0, 0, 0); continue; }

            Color c = Color.Lerp(gold, bright, Mathf.Clamp01((fy - fx) / n + 0.3f)); // light from the top left
            if (r > 0.86f) c = Color.Lerp(dark, gold, (1f - r) / 0.14f);             // rim
            else if (r > 0.74f && r < 0.79f) c = dark;                                // inner ring
            if (Dollar(fx, fy, 30f)) c = dark;
            px[y * n + x] = c;
        }
        return Finish(n, n, px, "MoneyRain_Coin");
    }

    /// <summary>True where a "$" of the given height, centred at 0,0, covers the point.</summary>
    static bool Dollar(float x, float y, float height)
    {
        float r = height / 4f;          // radius of each loop of the S
        float t = height * 0.15f;       // stroke thickness
        bool bar = Mathf.Abs(x) < t * 0.28f && Mathf.Abs(y) < height * 0.62f;

        // The S is two loops: the top one open at its bottom right, the bottom one open at its top left.
        bool topLoop = Mathf.Abs(new Vector2(x, y - r).magnitude - r) < t * 0.5f && !(x > 0f && y < r);
        bool bottomLoop = Mathf.Abs(new Vector2(x, y + r).magnitude - r) < t * 0.5f && !(x < 0f && y > -r);
        return bar || topLoop || bottomLoop;
    }

    static Texture2D Finish(int w, int h, Color[] px, string name)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 4,
            hideFlags = HideFlags.HideAndDontSave,
        };
        tex.SetPixels(px);
        tex.Apply(true, true);
        return tex;
    }
}
