using System.Collections.Generic;
using SolarSystemApp;
using SolarScaleMode = SolarSystemApp.ScaleMode;
using UnityEngine;

/// <summary>
/// Projects planets and moons onto the gravity grid as soft green glows and vertical drop lines.
/// Default off via ProjectionSettings; independent of GravityGridSettings visibility.
/// </summary>
[DefaultExecutionOrder(50)]
public class BodyGridProjectionController : MonoBehaviour
{
    const string GlowMaterialResourcePath = "GridProjectionGlow";
    const string GlowShaderName = "Custom/GridProjectionGlow";
    const string RootName = "BodyGridProjections";

    static readonly string[] BodyNames =
    {
        "Mercury", "Venus", "Earth", "Mars", "Phobos", "Deimos", "Jupiter", "Saturn", "Uranus", "Neptune",
        "Moon", "Io", "Europa", "Titan", "Ganymede", "Callisto", "Triton"
    };

    static readonly HashSet<string> MoonNames = new HashSet<string>
    {
        "Moon", "Phobos", "Deimos", "Io", "Europa", "Titan", "Ganymede", "Callisto", "Triton"
    };

    static readonly Color ProjectionGreen = new Color(0.24f, 0.86f, 0.48f, 0.55f);
    static readonly Color LineGreen = new Color(0.3f, 0.9f, 0.55f, 0.42f);

    [SerializeField] float minSpotRadius = 0.9f;
    [SerializeField] float maxSpotRadius = 14f;
    [SerializeField] float bodyRadiusFactor = 2.4f;
    [SerializeField] float moonRadiusScale = 0.65f;
    [SerializeField] float extentSpotFactor = 0.012f;
    [SerializeField] float surfaceLift = 0.04f;
    [SerializeField] float lineWidth = 0.06f;

    struct ProjectionVisual
    {
        public Transform body;
        public string name;
        public GameObject root;
        public LineRenderer line;
        public Transform glow;
        public MeshRenderer glowRenderer;
    }

    readonly List<ProjectionVisual> _visuals = new List<ProjectionVisual>();

    Transform _root;
    Material _glowMaterial;
    Material _lineMaterial;
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

            var visual = CreateVisual(BodyNames[i], bodyGo.transform);
            _visuals.Add(visual);
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
        var line = lineGo.AddComponent<LineRenderer>();
        ConfigureLine(line);

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
            line = line,
            glow = glowGo.transform,
            glowRenderer = renderer
        };
    }

    void ConfigureLine(LineRenderer line)
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

    void UpdateVisuals()
    {
        for (int i = 0; i < _visuals.Count; i++)
        {
            var visual = _visuals[i];
            if (visual.body == null)
            {
                if (visual.root != null)
                    visual.root.SetActive(false);
                continue;
            }

            Vector3 bodyPos = visual.body.position;
            if (_grid == null || !_grid.ContainsWorldXZ(bodyPos.x, bodyPos.z))
            {
                if (visual.root != null)
                    visual.root.SetActive(false);
                continue;
            }

            if (visual.root != null && !visual.root.activeSelf)
                visual.root.SetActive(true);

            float surfaceY = SampleSurfaceY(bodyPos.x, bodyPos.z);
            Vector3 foot = new Vector3(bodyPos.x, surfaceY + surfaceLift, bodyPos.z);

            visual.line.SetPosition(0, bodyPos);
            visual.line.SetPosition(1, foot);

            float radius = ComputeSpotRadius(visual);
            visual.glow.position = foot;
            visual.glow.rotation = Quaternion.identity;
            visual.glow.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
        }
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
        if (MoonNames.Contains(visual.name))
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
