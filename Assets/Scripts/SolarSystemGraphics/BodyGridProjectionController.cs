using System.Collections.Generic;
using SolarSystemApp;
using SolarScaleMode = SolarSystemApp.ScaleMode;
using UnityEngine;

/// <summary>
/// Projects planets and moons onto the gravity grid as soft green glows, drop lines,
/// and a long fading surface trail that stays on the deformed fabric.
/// Default off via ProjectionSettings; independent of GravityGridSettings visibility.
/// </summary>
[DefaultExecutionOrder(50)]
public class BodyGridProjectionController : MonoBehaviour
{
    const string GlowMaterialResourcePath = "GridProjectionGlow";
    const string GlowShaderName = "Custom/GridProjectionGlow";
    const string RootName = "BodyGridProjections";
    const int TrailPointCapacity = 96;
    const int OutsideClearFrames = 120;

    static readonly string[] BodyNames =
    {
        "Mercury", "Venus", "Earth", "Mars", "Phobos", "Deimos", "Jupiter", "Saturn", "Uranus", "Neptune", "Pluto",
        "Moon", "Io", "Europa", "Titan", "Ganymede", "Callisto", "Triton"
    };

    static readonly HashSet<string> MoonNames = new HashSet<string>
    {
        "Moon", "Phobos", "Deimos", "Io", "Europa", "Titan", "Ganymede", "Callisto", "Triton"
    };

    static readonly Color ProjectionGreen = new Color(0.24f, 0.86f, 0.48f, 0.55f);
    static readonly Color LineGreen = new Color(0.3f, 0.9f, 0.55f, 0.42f);
    static readonly Color TrailGreen = new Color(0.24f, 0.86f, 0.48f, 0.72f);

    [SerializeField] float minSpotRadius = 0.9f;
    [SerializeField] float maxSpotRadius = 14f;
    [SerializeField] float bodyRadiusFactor = 2.4f;
    [SerializeField] float moonRadiusScale = 0.65f;
    [SerializeField] float extentSpotFactor = 0.012f;
    [SerializeField] float surfaceLift = 0.04f;
    [SerializeField] float lineWidth = 0.06f;
    [SerializeField] float trailLengthFactor = 0.45f;
    [SerializeField] float trailMinLength = 24f;
    [SerializeField] float moonTrailLengthScale = 0.55f;
    [SerializeField] float trailSpacingFactor = 0.0025f;
    [SerializeField] float trailMinSpacing = 0.35f;

    sealed class ProjectionVisual
    {
        public Transform body;
        public string name;
        public GameObject root;
        public LineRenderer dropLine;
        public LineRenderer trail;
        public Transform glow;
        public MeshRenderer glowRenderer;
        public readonly List<Vector3> trailPoints = new List<Vector3>(TrailPointCapacity);
        public int outsideFrames;
        public bool isMoon;
    }

    readonly List<ProjectionVisual> _visuals = new List<ProjectionVisual>();
    readonly Vector3[] _trailPositionScratch = new Vector3[TrailPointCapacity];

    Transform _root;
    Material _glowMaterial;
    Material _lineMaterial;
    Material _trailMaterial;
    Mesh _quadMesh;
    SpacetimeGridController _grid;
    bool _active;
    bool _built;

    void Awake()
    {
        EnsureMaterials();
        ApplySetting(ProjectionSettings.UseProjection);
    }

    void OnEnable()
    {
        ProjectionSettings.UseProjectionChanged += OnProjectionChanged;
        ScaleSettings.ModeChanged += OnScaleChanged;
        ScaleSettings.UseRealDistancesChanged += OnScaleBoolChanged;
        ScaleSettings.UseRealSizesChanged += OnScaleBoolChanged;
    }

    void OnDisable()
    {
        ProjectionSettings.UseProjectionChanged -= OnProjectionChanged;
        ScaleSettings.ModeChanged -= OnScaleChanged;
        ScaleSettings.UseRealDistancesChanged -= OnScaleBoolChanged;
        ScaleSettings.UseRealSizesChanged -= OnScaleBoolChanged;
    }

    void OnDestroy()
    {
        ClearVisuals();

        if (_glowMaterial != null)
            Destroy(_glowMaterial);
        if (_lineMaterial != null)
            Destroy(_lineMaterial);
        if (_trailMaterial != null)
            Destroy(_trailMaterial);
        if (_quadMesh != null)
            Destroy(_quadMesh);
        if (_root != null)
            Destroy(_root.gameObject);
    }

    void LateUpdate()
    {
        if (!_active)
            return;

        if (!_built || _visuals.Count == 0)
            RebuildVisuals();

        if (_grid == null)
            _grid = FindFirstObjectByType<SpacetimeGridController>();

        UpdateVisuals();
    }

    void OnProjectionChanged(bool enabled) => ApplySetting(enabled);

    void OnScaleChanged(SolarScaleMode _)
    {
        if (_active)
            RebuildVisuals();
    }

    void OnScaleBoolChanged(bool _)
    {
        if (_active)
            RebuildVisuals();
    }

    void ApplySetting(bool enabled)
    {
        _active = enabled;
        if (!enabled)
        {
            ClearVisuals();
            return;
        }

        RebuildVisuals();
    }

    void EnsureMaterials()
    {
        if (_glowMaterial == null)
        {
            var source = Resources.Load<Material>(GlowMaterialResourcePath);
            if (source != null && source.shader != null && source.shader.name == GlowShaderName)
                _glowMaterial = new Material(source);
            else
            {
                var shader = Shader.Find(GlowShaderName);
                if (shader != null)
                {
                    _glowMaterial = new Material(shader);
                    _glowMaterial.SetColor("_Color", ProjectionGreen);
                }
                else
                {
                    _glowMaterial = CreateFallbackUnlit(ProjectionGreen);
                }
            }

            _glowMaterial.renderQueue = 2960;
        }

        if (_lineMaterial == null)
            _lineMaterial = CreateFallbackUnlit(LineGreen);

        if (_trailMaterial == null)
            _trailMaterial = CreateTrailMaterial();

        if (_quadMesh == null)
            _quadMesh = BuildUnitQuad();
    }

    static Material CreateFallbackUnlit(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        var material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        material.renderQueue = 2960;
        return material;
    }

    static Material CreateTrailMaterial()
    {
        // Sprites/Default multiplies vertex colors — needed for LineRenderer length fade.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        var material = new Material(shader);
        material.color = Color.white;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        material.renderQueue = 2960;
        return material;
    }

    static Mesh BuildUnitQuad()
    {
        var mesh = new Mesh { name = "ProjectionGlowQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0.5f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void EnsureRoot()
    {
        if (_root != null)
            return;

        var existing = transform.Find(RootName);
        if (existing != null)
        {
            _root = existing;
            return;
        }

        var go = new GameObject(RootName);
        go.transform.SetParent(transform, false);
        _root = go.transform;
    }

    void RebuildVisuals()
    {
        ClearVisuals();
        if (!_active)
            return;

        EnsureMaterials();
        EnsureRoot();
        _grid = FindFirstObjectByType<SpacetimeGridController>();

        for (int i = 0; i < BodyNames.Length; i++)
        {
            var bodyGo = GameObject.Find(BodyNames[i]);
            if (bodyGo == null)
                continue;

            _visuals.Add(CreateVisual(BodyNames[i], bodyGo.transform));
        }

        _built = true;
        UpdateVisuals();
    }

    ProjectionVisual CreateVisual(string bodyName, Transform body)
    {
        var root = new GameObject("Projection_" + bodyName);
        root.transform.SetParent(_root, false);

        var lineGo = new GameObject("DropLine");
        lineGo.transform.SetParent(root.transform, false);
        var dropLine = lineGo.AddComponent<LineRenderer>();
        ConfigureDropLine(dropLine);

        var trailGo = new GameObject("Trail");
        trailGo.transform.SetParent(root.transform, false);
        var trail = trailGo.AddComponent<LineRenderer>();
        ConfigureTrail(trail);

        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(root.transform, false);
        var filter = glowGo.AddComponent<MeshFilter>();
        filter.sharedMesh = _quadMesh;
        var renderer = glowGo.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _glowMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return new ProjectionVisual
        {
            body = body,
            name = bodyName,
            root = root,
            dropLine = dropLine,
            trail = trail,
            glow = glowGo.transform,
            glowRenderer = renderer,
            isMoon = MoonNames.Contains(bodyName)
        };
    }

    void ConfigureDropLine(LineRenderer line)
    {
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.allowOcclusionWhenDynamic = false;
        line.numCapVertices = 2;
        line.numCornerVertices = 0;
        line.widthMultiplier = 1f;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth * 0.55f;
        line.material = _lineMaterial;
        line.startColor = LineGreen;
        line.endColor = new Color(LineGreen.r, LineGreen.g, LineGreen.b, LineGreen.a * 0.35f);
        line.textureMode = LineTextureMode.Stretch;
    }

    void ConfigureTrail(LineRenderer trail)
    {
        trail.positionCount = 0;
        trail.useWorldSpace = true;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.allowOcclusionWhenDynamic = false;
        trail.numCapVertices = 3;
        trail.numCornerVertices = 2;
        trail.widthMultiplier = 1f;
        trail.material = _trailMaterial;
        trail.textureMode = LineTextureMode.Stretch;
        trail.alignment = LineAlignment.View;
        trail.colorGradient = BuildTrailColorGradient();
        trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.12f);
    }

    static Gradient BuildTrailColorGradient()
    {
        var gradient = new Gradient();
        // Position 0 = trail tail (oldest), 1 = head under the body.
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.18f, 0.7f, 0.4f), 0f),
                new GradientColorKey(TrailGreen, 0.55f),
                new GradientColorKey(TrailGreen, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.12f, 0.25f),
                new GradientAlphaKey(0.45f, 0.65f),
                new GradientAlphaKey(0.78f, 1f)
            });
        return gradient;
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < _visuals.Count; i++)
        {
            ProjectionVisual visual = _visuals[i];
            if (visual.body == null)
            {
                SetDropAndGlowVisible(visual, false);
                ClearTrail(visual);
                if (visual.root != null)
                    visual.root.SetActive(false);
                continue;
            }

            Vector3 bodyPos = visual.body.position;
            bool inside = _grid != null && _grid.ContainsWorldXZ(bodyPos.x, bodyPos.z);

            if (!inside)
            {
                visual.outsideFrames++;
                SetDropAndGlowVisible(visual, false);

                if (visual.outsideFrames >= OutsideClearFrames)
                {
                    ClearTrail(visual);
                    if (visual.root != null)
                        visual.root.SetActive(false);
                    continue;
                }

                if (visual.root != null && !visual.root.activeSelf)
                    visual.root.SetActive(true);

                RestampTrailHeights(visual);
                ApplyTrailRenderer(visual, ComputeSpotRadius(visual));
                continue;
            }

            visual.outsideFrames = 0;
            if (visual.root != null && !visual.root.activeSelf)
                visual.root.SetActive(true);

            float surfaceY = SampleSurfaceY(bodyPos.x, bodyPos.z);
            Vector3 foot = new Vector3(bodyPos.x, surfaceY + surfaceLift, bodyPos.z);

            SetDropAndGlowVisible(visual, true);
            visual.dropLine.SetPosition(0, bodyPos);
            visual.dropLine.SetPosition(1, foot);

            float radius = ComputeSpotRadius(visual);
            visual.glow.position = foot;
            visual.glow.rotation = Quaternion.identity;
            visual.glow.localScale = new Vector3(radius * 2f, 1f, radius * 2f);

            AppendTrailPoint(visual, foot);
            TrimTrailLength(visual);
            RestampTrailHeights(visual);
            ApplyTrailRenderer(visual, radius);
        }
    }

    static void SetDropAndGlowVisible(ProjectionVisual visual, bool visible)
    {
        if (visual.dropLine != null)
            visual.dropLine.enabled = visible;
        if (visual.glowRenderer != null)
            visual.glowRenderer.enabled = visible;
    }

    void AppendTrailPoint(ProjectionVisual visual, Vector3 foot)
    {
        float spacing = GetTrailMinSpacing();
        if (visual.trailPoints.Count > 0)
        {
            Vector3 last = visual.trailPoints[visual.trailPoints.Count - 1];
            float dx = foot.x - last.x;
            float dz = foot.z - last.z;
            if (dx * dx + dz * dz < spacing * spacing)
            {
                // Keep head snug under the body even when spacing gate blocks a new point.
                visual.trailPoints[visual.trailPoints.Count - 1] = foot;
                return;
            }
        }

        visual.trailPoints.Add(foot);
        while (visual.trailPoints.Count > TrailPointCapacity)
            visual.trailPoints.RemoveAt(0);
    }

    void TrimTrailLength(ProjectionVisual visual)
    {
        float maxLength = GetMaxTrailLength(visual);
        float length = 0f;
        for (int i = visual.trailPoints.Count - 1; i > 0; i--)
        {
            Vector3 a = visual.trailPoints[i];
            Vector3 b = visual.trailPoints[i - 1];
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            length += Mathf.Sqrt(dx * dx + dz * dz);
            if (length <= maxLength)
                continue;

            visual.trailPoints.RemoveRange(0, i);
            break;
        }
    }

    void RestampTrailHeights(ProjectionVisual visual)
    {
        for (int i = 0; i < visual.trailPoints.Count; i++)
        {
            Vector3 p = visual.trailPoints[i];
            if (_grid != null && !_grid.ContainsWorldXZ(p.x, p.z))
            {
                // Drop points that left the fabric after extent changes.
                visual.trailPoints.RemoveAt(i);
                i--;
                continue;
            }

            float y = SampleSurfaceY(p.x, p.z) + surfaceLift;
            visual.trailPoints[i] = new Vector3(p.x, y, p.z);
        }
    }

    void ApplyTrailRenderer(ProjectionVisual visual, float spotRadius)
    {
        LineRenderer trail = visual.trail;
        if (trail == null)
            return;

        int count = visual.trailPoints.Count;
        if (count < 2)
        {
            trail.positionCount = 0;
            trail.enabled = false;
            return;
        }

        trail.enabled = true;
        // LineRenderer: index 0 = start (tail), last = end (head under body).
        for (int i = 0; i < count; i++)
            _trailPositionScratch[i] = visual.trailPoints[i];

        trail.positionCount = count;
        trail.SetPositions(_trailPositionScratch);

        float headWidth = Mathf.Max(0.2f, spotRadius * 0.9f);
        trail.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0.12f),
            new Keyframe(0.55f, 0.45f),
            new Keyframe(1f, 1f));
        trail.widthMultiplier = headWidth;
        trail.colorGradient = BuildTrailColorGradient();
    }

    void ClearTrail(ProjectionVisual visual)
    {
        visual.trailPoints.Clear();
        if (visual.trail != null)
        {
            visual.trail.positionCount = 0;
            visual.trail.enabled = false;
        }
    }

    float GetMaxTrailLength(ProjectionVisual visual)
    {
        float halfExtent = _grid != null ? _grid.HalfExtent : 120f;
        float length = Mathf.Max(trailMinLength, halfExtent * trailLengthFactor);
        if (visual.isMoon)
            length *= moonTrailLengthScale;
        return length;
    }

    float GetTrailMinSpacing()
    {
        float halfExtent = _grid != null ? _grid.HalfExtent : 120f;
        return Mathf.Max(trailMinSpacing, halfExtent * trailSpacingFactor);
    }

    float SampleSurfaceY(float x, float z)
    {
        if (_grid != null)
            return _grid.SampleSurfaceWorldY(x, z);

        return -2f;
    }

    float ComputeSpotRadius(ProjectionVisual visual)
    {
        float bodyScale = visual.body != null
            ? Mathf.Max(visual.body.lossyScale.x, visual.body.lossyScale.z)
            : 1f;

        float fromBody = bodyScale * bodyRadiusFactor;
        float fromExtent = 1.5f;
        if (_grid != null)
            fromExtent = Mathf.Max(1.2f, _grid.HalfExtent * extentSpotFactor);

        float radius = Mathf.Max(fromBody, fromExtent * 0.35f);
        if (visual.isMoon)
            radius *= moonRadiusScale;

        return Mathf.Clamp(radius, minSpotRadius, maxSpotRadius);
    }

    void ClearVisuals()
    {
        for (int i = 0; i < _visuals.Count; i++)
        {
            if (_visuals[i].root != null)
                Destroy(_visuals[i].root);
        }

        _visuals.Clear();
        _built = false;
    }
}
