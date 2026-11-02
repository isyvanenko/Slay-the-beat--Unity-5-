using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Cheap planar reflection for a flat floor. A hidden camera mirrors the main camera below the floor,
/// renders into a reduced-resolution texture, and shares it with the PlanarReflectionFloor shader.
/// Put this on the floor object and add M_PlanarReflectionFloor as a second material on its renderer.
/// </summary>
[DisallowMultipleComponent]
public class PlanarReflection : MonoBehaviour
{
    /// <summary>Camera whose view gets reflected. Uses Camera.main when empty.</summary>
    [Tooltip("Camera whose view gets reflected. Uses Camera.main when empty.")]
    public Camera targetCamera;

    /// <summary>Reflection texture size relative to the screen. 0.5 renders a quarter of the pixels.</summary>
    [Tooltip("Reflection texture size relative to the screen. 0.5 = a quarter of the pixels.")]
    [Range(0.1f, 1f)] public float resolutionScale = 0.5f;

    /// <summary>Layers that appear in the reflection.</summary>
    [Tooltip("Layers that appear in the reflection. Leave out anything you don't need to save performance.")]
    public LayerMask reflectLayers = ~(1 << 5);

    /// <summary>Whether reflected objects receive shadows. Off is much cheaper.</summary>
    [Tooltip("Whether reflected objects receive shadows. Off is much cheaper.")]
    public bool renderShadows = false;

    /// <summary>Floor surface direction in the object's local space.</summary>
    [Tooltip("Floor surface direction in local space. (0,1,0) for a Plane, (0,0,-1) for a Quad.")]
    public Vector3 localNormal = Vector3.up;

    /// <summary>Small lift of the clip plane so the floor itself never shows up in its own reflection.</summary>
    [Tooltip("Small lift of the clip plane so the floor itself never shows up in its own reflection.")]
    public float clipPlaneOffset = 0.02f;

    private static readonly int ReflectionTexId = Shader.PropertyToID("_PlanarReflectionTex");
    private static readonly int ReflectionTexelSizeId = Shader.PropertyToID("_PlanarReflectionTex_TexelSize");
    private static readonly int ReflectionActiveId = Shader.PropertyToID("_PlanarReflectionActive");

    /// <summary>Hidden camera that renders the mirrored view.</summary>
    private Camera reflectionCamera;
    /// <summary>Texture the mirrored view is rendered into.</summary>
    private RenderTexture reflectionTexture;

    private void OnEnable()
    {
        CreateReflectionCamera();
        Application.onBeforeRender += UpdateReflection;
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= UpdateReflection;
        Shader.SetGlobalFloat(ReflectionActiveId, 0f);

        if (reflectionCamera != null) Destroy(reflectionCamera.gameObject);
        reflectionCamera = null;
        ReleaseTexture();
    }

    /// <summary>
    /// Creates the hidden reflection camera with every expensive URP feature turned off.
    /// </summary>
    private void CreateReflectionCamera()
    {
        GameObject go = new GameObject("[PlanarReflectionCamera]");
        go.hideFlags = HideFlags.HideAndDontSave;
        reflectionCamera = go.AddComponent<Camera>();
        reflectionCamera.allowMSAA = false;

        UniversalAdditionalCameraData data = reflectionCamera.GetUniversalAdditionalCameraData();
        data.renderShadows = renderShadows;
        data.renderPostProcessing = false;
        data.requiresDepthTexture = false;
        data.requiresColorTexture = false;
        data.antialiasing = AntialiasingMode.None;
    }

    /// <summary>
    /// Mirrors the target camera across the floor right before rendering, after all LateUpdate camera movement.
    /// </summary>
    private void UpdateReflection()
    {
        if (reflectionCamera == null) return;

        Camera target = targetCamera != null ? targetCamera : Camera.main;
        Vector3 normal = transform.TransformDirection(localNormal).normalized;
        Vector3 point = transform.position;

        // Nothing to reflect when the camera is missing or below the floor.
        float height = target != null ? Vector3.Dot(target.transform.position - point, normal) : -1f;
        bool active = height > 0f;
        reflectionCamera.enabled = active;
        Shader.SetGlobalFloat(ReflectionActiveId, active ? 1f : 0f);
        if (!active) return;

        EnsureTexture(target);

        reflectionCamera.depth = target.depth - 1f;
        reflectionCamera.cullingMask = reflectLayers;
        reflectionCamera.clearFlags = target.clearFlags;
        reflectionCamera.backgroundColor = target.backgroundColor;
        reflectionCamera.allowHDR = target.allowHDR;
        reflectionCamera.orthographic = target.orthographic;
        reflectionCamera.orthographicSize = target.orthographicSize;
        reflectionCamera.fieldOfView = target.fieldOfView;
        reflectionCamera.aspect = target.aspect;
        reflectionCamera.nearClipPlane = target.nearClipPlane;
        reflectionCamera.farClipPlane = target.farClipPlane;
        reflectionCamera.GetUniversalAdditionalCameraData().renderShadows = renderShadows;

        // Place the camera at the mirrored position looking along the mirrored direction.
        // This keeps a normal (non-inverted) camera, so the image is just flipped horizontally;
        // the floor shader flips it back when sampling.
        Transform t = target.transform;
        Vector3 forward = Vector3.Reflect(t.forward, normal);
        Vector3 up = Vector3.Reflect(t.up, normal);
        reflectionCamera.transform.SetPositionAndRotation(t.position - 2f * height * normal, Quaternion.LookRotation(forward, up));

        // Clip everything below the floor so it can't leak into the reflection.
        reflectionCamera.ResetProjectionMatrix();
        Matrix4x4 worldToCamera = reflectionCamera.worldToCameraMatrix;
        Vector3 planePoint = worldToCamera.MultiplyPoint(point + normal * clipPlaneOffset);
        Vector3 planeNormal = worldToCamera.MultiplyVector(normal).normalized;
        Vector4 clipPlane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, -Vector3.Dot(planePoint, planeNormal));
        reflectionCamera.projectionMatrix = reflectionCamera.CalculateObliqueMatrix(clipPlane);
    }

    /// <summary>
    /// Creates or resizes the reflection texture to match the target camera at the chosen resolution scale.
    /// </summary>
    private void EnsureTexture(Camera target)
    {
        int width = Mathf.Max(1, Mathf.RoundToInt(target.pixelWidth * resolutionScale));
        int height = Mathf.Max(1, Mathf.RoundToInt(target.pixelHeight * resolutionScale));
        RenderTextureFormat format = target.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

        if (reflectionTexture != null && reflectionTexture.width == width && reflectionTexture.height == height && reflectionTexture.format == format)
            return;

        ReleaseTexture();
        reflectionTexture = new RenderTexture(width, height, 24, format)
        {
            name = "PlanarReflection",
            hideFlags = HideFlags.HideAndDontSave
        };
        reflectionTexture.Create();

        reflectionCamera.targetTexture = reflectionTexture;
        Shader.SetGlobalTexture(ReflectionTexId, reflectionTexture);
        Shader.SetGlobalVector(ReflectionTexelSizeId, new Vector4(1f / width, 1f / height, width, height));
    }

    /// <summary>Frees the reflection texture.</summary>
    private void ReleaseTexture()
    {
        if (reflectionCamera != null) reflectionCamera.targetTexture = null;
        if (reflectionTexture == null) return;
        reflectionTexture.Release();
        Destroy(reflectionTexture);
        reflectionTexture = null;
    }
}
