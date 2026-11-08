using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lights up the edges of the screen in gold with drifting sparkles, shared by everyone in the current level.
/// </summary>
/// <remarks>
/// Builds itself the first time it's needed. It draws under the other overlay canvases, so the
/// pause menu and scene fades stay on top, and it freezes with the game when paused. The sparkles
/// are UI images rather than a ParticleSystem, because overlay canvases would cover a ParticleSystem.
/// </remarks>
public class GoldenEdgeGlow : MonoBehaviour
{
    private const int CanvasSortingOrder = -1;
    private const string AdditiveShaderPath = "Shaders/GoldenAdditive";

    private const int GlowTextureWidth = 256;
    private const int GlowTextureHeight = 144;
    private const float GlowFalloffPower = 2f;
    private const float RimReachShare = 0.45f;
    private const float RimFalloffPower = 3f;
    private const float RimFlashBrightness = 0.5f;
    private const float RimRestBrightness = 0.25f;
    private const float RimWhiteMix = 0.1f;

    private const float ShimmerSpeed = 7f;
    private const float ShimmerAmount = 0.12f;
    private const float RushInScale = 1.22f;
    private const float RushInTime = 0.35f;
    private const float RetriggerAlpha = 0.05f;

    private const int SparklePoolSize = 140;
    private const int SparkleTextureSize = 64;
    private const float SparkleEdgeInset = 0.12f;
    private const float SparkleBottomEdgeChance = 0.4f;
    private const float SparklesPerSecondPerCount = 0.7f;
    private const float SparkleMinLife = 0.7f;
    private const float SparkleMaxLife = 1.4f;
    private const float SparkleMinInwardSpeed = 0.03f;
    private const float SparkleMaxInwardSpeed = 0.12f;
    private const float SparkleMinRiseSpeed = 0.05f;
    private const float SparkleMaxRiseSpeed = 0.2f;
    private const float SparkleDrag = 0.8f;
    private const float SparkleMaxSpin = 90f;
    private const float SparkleMinTwinkleSpeed = 8f;
    private const float SparkleMaxTwinkleSpeed = 16f;
    private const float SparklePopTime = 0.15f;
    private const float SparkleFadeShare = 0.35f;
    private const float SparkleWhiteMix = 0.6f;
    private const float SparkleMinScale = 0.45f;

    private sealed class Sparkle
    {
        public RawImage Image;
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Tint;
        public float Age;
        public float Life;
        public float Size;
        public float Spin;
        public float Angle;
        public float TwinkleSpeed;
        public float TwinkleOffset;
    }

    private static GoldenEdgeGlow instance;

    private RawImage glowImage;
    private RawImage rimImage;
    private Texture2D glowTexture;
    private Texture2D rimTexture;
    private Texture2D sparkleTexture;
    private Material additiveMaterial;
    private Sparkle[] sparkles;
    private float textureWidthShare = -1f;

    private Color color;
    private float strength;
    private float fadeInTime;
    private float stayTime;
    private float fadeOutTime;
    private float sparkleSizeShare;
    private float sparklesPerSecond;

    private float elapsed;
    private float startAlpha;
    private float currentAlpha;
    private float rushInElapsed = RushInTime;
    private float sparkleBudget;
    private bool glowFinished = true;

    /// <summary>
    /// Plays the effect. If it's already showing, it carries on from its current brightness.
    /// </summary>
    /// <param name="glowColor">Colour of the glow and sparkles.</param>
    /// <param name="widthShare">How far the glow reaches in from the edges, as a share of the screen height.</param>
    /// <param name="glowStrength">Opacity at full brightness, from 0 to 1.</param>
    /// <param name="fadeIn">Seconds to reach full brightness.</param>
    /// <param name="stay">Seconds to stay at full brightness.</param>
    /// <param name="fadeOut">Seconds to fade away.</param>
    /// <param name="sparkleCount">Sparkles released in the first burst, with more following while the glow stays.</param>
    /// <param name="sparkleSize">Size of a sparkle, as a share of the screen height.</param>
    public static void Play(Color glowColor, float widthShare, float glowStrength, float fadeIn, float stay, float fadeOut, int sparkleCount, float sparkleSize)
    {
        if (instance == null) instance = Create();
        instance.Begin(glowColor, widthShare, glowStrength, fadeIn, stay, fadeOut, sparkleCount, sparkleSize);
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        UpdateGlow();
        UpdateSparkleSpawning();
        bool sparklesAlive = UpdateSparkles();

        if (glowFinished && !sparklesAlive) enabled = false;
    }

    void OnDestroy()
    {
        if (glowTexture != null) Destroy(glowTexture);
        if (rimTexture != null) Destroy(rimTexture);
        if (sparkleTexture != null) Destroy(sparkleTexture);
        if (additiveMaterial != null) Destroy(additiveMaterial);
    }

    private void Begin(Color glowColor, float widthShare, float glowStrength, float fadeIn, float stay, float fadeOut, int sparkleCount, float sparkleSize)
    {
        color = glowColor;
        strength = glowStrength;
        fadeInTime = fadeIn;
        stayTime = stay;
        fadeOutTime = fadeOut;
        sparkleSizeShare = sparkleSize;
        sparklesPerSecond = sparkleCount * SparklesPerSecondPerCount;

        // A glow that's already showing keeps its place rather than rushing in again.
        if (currentAlpha < RetriggerAlpha) rushInElapsed = 0f;

        elapsed = 0f;
        startAlpha = currentAlpha;
        sparkleBudget = 0f;
        glowFinished = false;

        UpdateGlowTextures(widthShare);
        ApplyGlow(0f);
        glowImage.enabled = true;
        rimImage.enabled = true;
        enabled = true;

        for (int i = 0; i < sparkleCount; i++) SpawnSparkle();
    }

    private void UpdateGlow()
    {
        if (glowFinished) return;

        rushInElapsed += Time.deltaTime;
        float rimFlash;

        if (elapsed < fadeInTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeInTime);
            currentAlpha = Mathf.Lerp(startAlpha, strength, t);
            rimFlash = RimFlashBrightness * t;
        }
        else if (elapsed < fadeInTime + stayTime)
        {
            float stayed = elapsed - fadeInTime;
            currentAlpha = GetShimmer(stayed);
            rimFlash = Mathf.Lerp(RimFlashBrightness, RimRestBrightness, stayed / Mathf.Max(stayTime, 0.001f));
        }
        else if (elapsed < fadeInTime + stayTime + fadeOutTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, (elapsed - fadeInTime - stayTime) / fadeOutTime);
            currentAlpha = Mathf.Lerp(GetShimmer(stayTime), 0f, t);
            rimFlash = Mathf.Lerp(RimRestBrightness, 0f, t);
        }
        else
        {
            currentAlpha = 0f;
            rimFlash = 0f;
            glowFinished = true;
            glowImage.enabled = false;
            rimImage.enabled = false;
        }

        ApplyGlow(rimFlash);

        float rush = Mathf.Clamp01(rushInElapsed / RushInTime);
        float eased = 1f - Mathf.Pow(1f - rush, 3f);
        Vector3 scale = Vector3.one * Mathf.Lerp(RushInScale, 1f, eased);
        glowImage.rectTransform.localScale = scale;
        rimImage.rectTransform.localScale = scale;
    }

    private float GetShimmer(float stayed)
    {
        return strength * (1f - ShimmerAmount + ShimmerAmount * Mathf.Cos(stayed * ShimmerSpeed));
    }

    private void ApplyGlow(float rimBrightness)
    {
        Color glow = color;
        glow.a = currentAlpha;
        glowImage.color = glow;

        Color rim = Color.Lerp(color, Color.white, RimWhiteMix);
        rim.a = rimBrightness;
        rimImage.color = rim;
    }

    private void UpdateSparkleSpawning()
    {
        if (glowFinished || elapsed > fadeInTime + stayTime) return;

        sparkleBudget += sparklesPerSecond * Time.deltaTime;
        while (sparkleBudget >= 1f)
        {
            sparkleBudget -= 1f;
            SpawnSparkle();
        }
    }

    private void SpawnSparkle()
    {
        Sparkle sparkle = System.Array.Find(sparkles, s => !s.Image.enabled);
        if (sparkle == null) return;

        float width = Screen.width;
        float height = Screen.height;
        float inset = Random.Range(0f, SparkleEdgeInset) * height;
        float inward = Random.Range(SparkleMinInwardSpeed, SparkleMaxInwardSpeed) * height;
        float rise = Random.Range(SparkleMinRiseSpeed, SparkleMaxRiseSpeed) * height;

        // Positions are measured from the centre of the screen. Sparkles favour the bottom, as they rise.
        Vector2 position;
        Vector2 direction;
        if (Random.value < SparkleBottomEdgeChance)
        {
            position = new Vector2(Random.Range(-width, width) * 0.5f, -height * 0.5f + inset);
            direction = Vector2.up;
        }
        else if (Random.value < width / (width + height * 2f))
        {
            position = new Vector2(Random.Range(-width, width) * 0.5f, height * 0.5f - inset);
            direction = Vector2.down;
        }
        else
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            position = new Vector2(side * (width * 0.5f - inset), Random.Range(-height, height) * 0.5f);
            direction = new Vector2(-side, 0f);
        }

        sparkle.Position = position;
        sparkle.Velocity = direction * inward + Vector2.up * rise;
        sparkle.Tint = Color.Lerp(color, Color.white, Random.Range(0f, SparkleWhiteMix));
        sparkle.Age = 0f;
        sparkle.Life = Random.Range(SparkleMinLife, SparkleMaxLife);
        sparkle.Size = sparkleSizeShare * height * Random.Range(SparkleMinScale, 1f);
        sparkle.Spin = Random.Range(-SparkleMaxSpin, SparkleMaxSpin);
        sparkle.Angle = Random.Range(0f, 90f);
        sparkle.TwinkleSpeed = Random.Range(SparkleMinTwinkleSpeed, SparkleMaxTwinkleSpeed);
        sparkle.TwinkleOffset = Random.Range(0f, Mathf.PI * 2f);

        sparkle.Image.color = Color.clear;
        sparkle.Image.enabled = true;
    }

    /// <summary>Moves and fades the sparkles.</summary>
    /// <returns>True if any sparkles are still alive.</returns>
    private bool UpdateSparkles()
    {
        bool anyAlive = false;
        float delta = Time.deltaTime;

        foreach (Sparkle sparkle in sparkles)
        {
            if (!sparkle.Image.enabled) continue;

            sparkle.Age += delta;
            if (sparkle.Age >= sparkle.Life)
            {
                sparkle.Image.enabled = false;
                continue;
            }

            anyAlive = true;
            sparkle.Velocity *= Mathf.Max(0f, 1f - SparkleDrag * delta);
            sparkle.Position += sparkle.Velocity * delta;
            sparkle.Angle += sparkle.Spin * delta;

            float pop = Mathf.Clamp01(sparkle.Age / SparklePopTime);
            float life = sparkle.Age / sparkle.Life;
            float fade = Mathf.Clamp01((1f - life) / SparkleFadeShare);
            float twinkle = 0.65f + 0.35f * Mathf.Sin(sparkle.Age * sparkle.TwinkleSpeed + sparkle.TwinkleOffset);

            Color tint = sparkle.Tint;
            tint.a = pop * fade * twinkle;
            sparkle.Image.color = tint;

            RectTransform rect = sparkle.Image.rectTransform;
            rect.anchoredPosition = sparkle.Position;
            rect.localRotation = Quaternion.Euler(0f, 0f, sparkle.Angle);
            rect.sizeDelta = Vector2.one * sparkle.Size * pop * Mathf.Lerp(0.7f, 1f, fade);
        }

        return anyAlive;
    }

    private static GoldenEdgeGlow Create()
    {
        GameObject glowObject = new GameObject("Golden Edge Glow", typeof(Canvas));

        Canvas canvas = glowObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortingOrder;

        GoldenEdgeGlow glow = glowObject.AddComponent<GoldenEdgeGlow>();

        Shader additive = Resources.Load<Shader>(AdditiveShaderPath);
        if (additive != null) glow.additiveMaterial = new Material(additive);

        glow.glowImage = glow.CreateFullScreenImage("Glow", null);
        glow.rimImage = glow.CreateFullScreenImage("Rim", glow.additiveMaterial);
        glow.CreateSparkles();
        return glow;
    }

    private RawImage CreateFullScreenImage(string objectName, Material material)
    {
        RawImage image = CreateImage(objectName, material);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return image;
    }

    private RawImage CreateImage(string objectName, Material material)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform));
        imageObject.transform.SetParent(transform, false);

        RawImage image = imageObject.AddComponent<RawImage>();
        image.raycastTarget = false;
        image.maskable = false;
        image.enabled = false;
        if (material != null) image.material = material;
        return image;
    }

    private void CreateSparkles()
    {
        sparkleTexture = CreateSparkleTexture();
        sparkles = new Sparkle[SparklePoolSize];

        for (int i = 0; i < sparkles.Length; i++)
        {
            RawImage image = CreateImage("Sparkle", additiveMaterial);
            image.texture = sparkleTexture;
            sparkles[i] = new Sparkle { Image = image };
        }
    }

    /// <summary>Draws a sparkle: a soft glowing core with four thin points.</summary>
    private static Texture2D CreateSparkleTexture()
    {
        Texture2D texture = new Texture2D(SparkleTextureSize, SparkleTextureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[SparkleTextureSize * SparkleTextureSize];
        float half = (SparkleTextureSize - 1) * 0.5f;
        for (int y = 0; y < SparkleTextureSize; y++)
        {
            for (int x = 0; x < SparkleTextureSize; x++)
            {
                float dx = Mathf.Abs(x - half) / half;
                float dy = Mathf.Abs(y - half) / half;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                float core = Mathf.Exp(-distance * distance * 22f);
                float halo = 0.35f * Mathf.Exp(-distance * distance * 9f);
                float horizontal = Mathf.Exp(-dy * 22f) * Mathf.Pow(Mathf.Clamp01(1f - dx), 2f);
                float vertical = Mathf.Exp(-dx * 22f) * Mathf.Pow(Mathf.Clamp01(1f - dy), 2f);
                float edgeFade = Mathf.Clamp01((1f - distance) * 4f);
                float alpha = Mathf.Clamp01(core + (halo + horizontal + vertical) * edgeFade);

                pixels[y * SparkleTextureSize + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private void UpdateGlowTextures(float widthShare)
    {
        if (Mathf.Approximately(widthShare, textureWidthShare)) return;
        textureWidthShare = widthShare;

        DrawEdgeTexture(ref glowTexture, widthShare, GlowFalloffPower);
        DrawEdgeTexture(ref rimTexture, widthShare * RimReachShare, RimFalloffPower);
        glowImage.texture = glowTexture;
        rimImage.texture = rimTexture;
    }

    /// <summary>Draws a frame shape: solid at the screen edges, fading to nothing towards the middle.</summary>
    private static void DrawEdgeTexture(ref Texture2D texture, float widthShare, float falloffPower)
    {
        if (texture == null)
        {
            texture = new Texture2D(GlowTextureWidth, GlowTextureHeight, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
        }

        float reach = Mathf.Max(widthShare, 0.001f) * GlowTextureHeight;
        Color32[] pixels = new Color32[GlowTextureWidth * GlowTextureHeight];
        for (int y = 0; y < GlowTextureHeight; y++)
        {
            float fromVerticalEdge = Mathf.Min(y, GlowTextureHeight - 1 - y);
            for (int x = 0; x < GlowTextureWidth; x++)
            {
                float fromSideEdge = Mathf.Min(x, GlowTextureWidth - 1 - x);

                // Combining the sides this way rounds off the inner corners.
                float side = Mathf.Pow(Mathf.Clamp01(1f - fromSideEdge / reach), falloffPower);
                float vertical = Mathf.Pow(Mathf.Clamp01(1f - fromVerticalEdge / reach), falloffPower);
                float alpha = 1f - (1f - side) * (1f - vertical);

                pixels[y * GlowTextureWidth + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
    }
}
