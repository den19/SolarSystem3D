using System.Collections.Generic;
using SolarSystemApp;
using SolarScaleMode = SolarSystemApp.ScaleMode;
using UnityEngine;

/// <summary>
/// Rubber-sheet spacetime grid deformed by Sun, planets, and major moons in Level1.
/// Educational mode uses two layers: a soft Sun bowl plus boosted local planet/moon wells.
/// </summary>
[DefaultExecutionOrder(-50)]
public class SpacetimeGridController : MonoBehaviour
{
    const string GridMaterialResourcePath = "SpacetimeGrid";
    const string GridShaderName = "Custom/SpacetimeGrid";
    const string LocalLayerObjectName = "SpacetimeGrid_Local";

    static readonly string[] BodyNames =
    {
        "Sun", "Mercury", "Venus", "Earth", "Mars", "Phobos", "Deimos", "Jupiter", "Saturn", "Uranus", "Neptune", "Pluto", "Moon", "Io", "Europa", "Titan", "Ganymede", "Callisto", "Triton"
    };

    static readonly HashSet<string> MoonNames = new HashSet<string>
    {
        "Moon", "Phobos", "Deimos", "Io", "Europa", "Titan", "Ganymede", "Callisto", "Triton"
    };

    static bool _shaderMissingWarningLogged;

    enum BodyFilter
    {
        SunOnly,
        PlanetsAndMoonsOnly,
        All
    }

    struct GridLayer
    {
        public GameObject gameObject;
        public Mesh mesh;
        public MeshFilter filter;
        public MeshRenderer renderer;
        public Material material;
        public Vector3[] workingVertices;
    }

    [SerializeField] float halfExtentX = 120f;
    [SerializeField] float halfExtentZ = 120f;
    [SerializeField] float baseY = -2f;
    [SerializeField] float depthScale = 18f;
    [SerializeField] float depthScaleReferenceExtent = 120f;
    [SerializeField] float softening = 2.5f;
    [SerializeField] float sunMassMultiplier = 8f;
    [SerializeField] int desktopSegments = 96;
    [SerializeField] int mobileSegments = 64;

    [Header("Educational dual layer")]
    [SerializeField] float localMassBoost = 120f;
    [SerializeField] float moonMassBoost = 300f;
    [SerializeField] float localSoftening = 0.8f;
    [SerializeField] float globalFillAlpha = 0.22f;
    [SerializeField] float globalGridAlpha = 0.45f;
    [SerializeField] float localGridAlpha = 0.95f;
    [SerializeField] float localRimBoost = 1.2f;

    readonly List<Transform> _bodies = new List<Transform>();
    readonly List<float> _masses = new List<float>();
    readonly List<string> _bodyNames = new List<string>();

    GridLayer _globalLayer;
    GridLayer _localLayer;
    Vector3[] _baseVertices;
    bool _built;
    bool _dualLayerActive;
    int _segmentsX = 96;
    int _segmentsZ = 96;

    void Awake()
    {
        BuildGrid();
        ApplySetting(GravityGridSettings.UseGravityGrid);
    }

    void OnEnable()
    {
        GravityGridSettings.UseGravityGridChanged += OnUseGravityGridChanged;
        ScaleSettings.ModeChanged += OnScaleModeChanged;
        ScaleSettings.UseRealDistancesChanged += OnScaleSettingsChanged;
        ScaleSettings.UseRealSizesChanged += OnScaleSettingsChanged;
    }

    void OnDisable()
    {
        GravityGridSettings.UseGravityGridChanged -= OnUseGravityGridChanged;
        ScaleSettings.ModeChanged -= OnScaleModeChanged;
        ScaleSettings.UseRealDistancesChanged -= OnScaleSettingsChanged;
        ScaleSettings.UseRealSizesChanged -= OnScaleSettingsChanged;
    }

    void OnDestroy()
    {
        DestroyLocalLayer();

        if (_globalLayer.material != null)
            Destroy(_globalLayer.material);
    }

    void LateUpdate()
    {
        if (!_built || !IsAnyLayerEnabled())
            return;

        DeformMesh();
    }

    void OnUseGravityGridChanged(bool enabled) => ApplySetting(enabled);

    void OnScaleModeChanged(SolarScaleMode mode)
    {
        bool dual = mode == SolarScaleMode.Educational;
        if (dual == _dualLayerActive)
        {
            RefreshBodies();
            return;
        }

        EnsureLocalLayer(dual);
        ApplyLayerMaterials();
        ApplySetting(GravityGridSettings.UseGravityGrid);
        if (_built)
            DeformMesh();
    }

    void OnScaleSettingsChanged(bool _) => RefreshBodies();

    public void RefreshBodies()
    {
        CacheBodies();
        if (_built)
            DeformMesh();
    }

    public void SetHalfExtent(float extent)
    {
        halfExtentX = extent;
        halfExtentZ = extent;
        if (_built)
            RebuildGridMesh();
    }

    /// <summary>
    /// World-space Y of the visible deformed grid surface at (x, z).
    /// Prefers bilinear sample of mesh vertices so projections sit on the well, not below the analytic center.
    /// </summary>
    public float SampleSurfaceWorldY(float x, float z)
    {
        // Live mesh only while the grid is deforming; otherwise analytical with half-cell floor.
        if (IsAnyLayerEnabled() && TrySampleMeshWorldY(x, z, out float meshY))
            return meshY;

        return SampleAnalyticalSurfaceWorldY(x, z);
    }

    /// <summary>Grid half-extent on X/Z (world units).</summary>
    public float HalfExtent => halfExtentX;

    /// <summary>True if (x, z) lies on the grid mesh square (local XZ within half-extents).</summary>
    public bool ContainsWorldXZ(float x, float z)
    {
        float lx = x - transform.position.x;
        float lz = z - transform.position.z;
        return Mathf.Abs(lx) <= halfExtentX && Mathf.Abs(lz) <= halfExtentZ;
    }

    bool TrySampleMeshWorldY(float x, float z, out float worldY)
    {
        worldY = 0f;

        if (!_built || _segmentsX < 1 || _segmentsZ < 1)
            return false;

        Vector3[] verts = null;
        if (_dualLayerActive && _localLayer.workingVertices != null && _localLayer.workingVertices.Length > 0)
            verts = _localLayer.workingVertices;
        else if (_globalLayer.workingVertices != null && _globalLayer.workingVertices.Length > 0)
            verts = _globalLayer.workingVertices;

        if (verts == null)
            return false;

        float localX = x - transform.position.x;
        float localZ = z - transform.position.z;

        float extentX = Mathf.Max(0.0001f, halfExtentX);
        float extentZ = Mathf.Max(0.0001f, halfExtentZ);
        float tx = (localX + extentX) / (2f * extentX);
        float tz = (localZ + extentZ) / (2f * extentZ);
        tx = Mathf.Clamp01(tx);
        tz = Mathf.Clamp01(tz);

        float fx = tx * _segmentsX;
        float fz = tz * _segmentsZ;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, _segmentsX - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, _segmentsZ - 1);
        int x1 = x0 + 1;
        int z1 = z0 + 1;
        float u = fx - x0;
        float v = fz - z0;

        int stride = _segmentsX + 1;
        int i00 = z0 * stride + x0;
        int i10 = z0 * stride + x1;
        int i01 = z1 * stride + x0;
        int i11 = z1 * stride + x1;

        if (i11 >= verts.Length)
            return false;

        float y00 = verts[i00].y;
        float y10 = verts[i10].y;
        float y01 = verts[i01].y;
        float y11 = verts[i11].y;
        float y0 = Mathf.Lerp(y00, y10, u);
        float y1 = Mathf.Lerp(y01, y11, u);
        float localY = Mathf.Lerp(y0, y1, v);

        worldY = transform.position.y + localY;
        return true;
    }

    float SampleAnalyticalSurfaceWorldY(float x, float z)
    {
        if (_bodies.Count == 0)
            CacheBodies();

        float cell = 2f * Mathf.Max(halfExtentX, 0.0001f) / Mathf.Max(1, _segmentsX);
        float minHoriz = cell * 0.5f;
        Vector3 world = new Vector3(x, baseY, z);

        if (IsDualLayerMode())
        {
            float sunDisplacement = ComputeDisplacement(world, BodyFilter.SunOnly, softening, 1f, minHoriz);
            float planetDisplacement = ComputeDisplacement(world, BodyFilter.PlanetsAndMoonsOnly, localSoftening, 1f, minHoriz);
            return baseY + sunDisplacement + planetDisplacement;
        }

        return baseY + ComputeDisplacement(world, BodyFilter.All, softening, 1f, minHoriz);
    }

    bool IsDualLayerMode() => ScaleSettings.Mode == SolarScaleMode.Educational;

    bool IsAnyLayerEnabled()
    {
        if (_globalLayer.renderer != null && _globalLayer.renderer.enabled)
            return true;

        return _dualLayerActive
            && _localLayer.renderer != null
            && _localLayer.renderer.enabled;
    }

    int GetSegmentCount()
    {
#if UNITY_ANDROID || UNITY_IOS
        return mobileSegments;
#else
        return desktopSegments;
#endif
    }

    void RebuildGridMesh()
    {
        DestroyLayerMesh(ref _globalLayer);

        CacheBodies();

        int segments = GetSegmentCount();
        _segmentsX = segments;
        _segmentsZ = segments;
        _globalLayer.mesh = GravityGridMeshUtility.BuildFlatGrid(halfExtentX, halfExtentZ, segments, segments);
        _baseVertices = _globalLayer.mesh.vertices;
        _globalLayer.workingVertices = new Vector3[_baseVertices.Length];

        if (_globalLayer.filter == null)
            _globalLayer.filter = gameObject.GetComponent<MeshFilter>();
        if (_globalLayer.filter == null)
            _globalLayer.filter = gameObject.AddComponent<MeshFilter>();
        _globalLayer.filter.sharedMesh = _globalLayer.mesh;

        if (_dualLayerActive)
        {
            DestroyLayerMesh(ref _localLayer);
            BuildLocalLayerMesh(segments);
        }

        DeformMesh();
    }

    void BuildGrid()
    {
        CacheBodies();

        int segments = GetSegmentCount();
        _segmentsX = segments;
        _segmentsZ = segments;
        _globalLayer.gameObject = gameObject;
        _globalLayer.mesh = GravityGridMeshUtility.BuildFlatGrid(halfExtentX, halfExtentZ, segments, segments);
        _baseVertices = _globalLayer.mesh.vertices;
        _globalLayer.workingVertices = new Vector3[_baseVertices.Length];

        _globalLayer.filter = gameObject.GetComponent<MeshFilter>();
        if (_globalLayer.filter == null)
            _globalLayer.filter = gameObject.AddComponent<MeshFilter>();
        _globalLayer.filter.sharedMesh = _globalLayer.mesh;

        _globalLayer.renderer = gameObject.GetComponent<MeshRenderer>();
        if (_globalLayer.renderer == null)
            _globalLayer.renderer = gameObject.AddComponent<MeshRenderer>();

        _globalLayer.material = CreateGridMaterial();
        _globalLayer.renderer.sharedMaterial = _globalLayer.material;
        _globalLayer.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _globalLayer.renderer.receiveShadows = false;

        transform.position = new Vector3(0f, baseY, 0f);
        _built = true;

        EnsureLocalLayer(IsDualLayerMode());
        ApplyLayerMaterials();
        DeformMesh();
    }

    void EnsureLocalLayer(bool enable)
    {
        if (enable)
        {
            if (_localLayer.gameObject == null)
            {
                var child = transform.Find(LocalLayerObjectName);
                if (child == null)
                {
                    var go = new GameObject(LocalLayerObjectName);
                    go.transform.SetParent(transform, false);
                    child = go.transform;
                }

                _localLayer.gameObject = child.gameObject;
                _localLayer.filter = child.GetComponent<MeshFilter>();
                if (_localLayer.filter == null)
                    _localLayer.filter = child.gameObject.AddComponent<MeshFilter>();

                _localLayer.renderer = child.GetComponent<MeshRenderer>();
                if (_localLayer.renderer == null)
                    _localLayer.renderer = child.gameObject.AddComponent<MeshRenderer>();

                _localLayer.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _localLayer.renderer.receiveShadows = false;
            }

            if (_localLayer.material == null)
                _localLayer.material = CreateGridMaterial();

            _localLayer.renderer.sharedMaterial = _localLayer.material;
            BuildLocalLayerMesh(GetSegmentCount());
            _dualLayerActive = true;
            return;
        }

        DestroyLocalLayer();
        _dualLayerActive = false;
    }

    void BuildLocalLayerMesh(int segments)
    {
        if (_localLayer.gameObject == null)
            return;

        DestroyLayerMesh(ref _localLayer);

        _localLayer.mesh = GravityGridMeshUtility.BuildFlatGrid(halfExtentX, halfExtentZ, segments, segments);
        _localLayer.workingVertices = new Vector3[_baseVertices.Length];
        _localLayer.filter.sharedMesh = _localLayer.mesh;
    }

    void DestroyLocalLayer()
    {
        DestroyLayerMesh(ref _localLayer);

        if (_localLayer.material != null)
        {
            Destroy(_localLayer.material);
            _localLayer.material = null;
        }

        if (_localLayer.gameObject != null)
        {
            Destroy(_localLayer.gameObject);
            _localLayer.gameObject = null;
            _localLayer.filter = null;
            _localLayer.renderer = null;
        }
    }

    static void DestroyLayerMesh(ref GridLayer layer)
    {
        if (layer.mesh == null)
            return;

        Destroy(layer.mesh);
        layer.mesh = null;
        layer.workingVertices = null;

        if (layer.filter != null)
            layer.filter.sharedMesh = null;
    }

    void ApplySetting(bool enabled)
    {
        if (_globalLayer.renderer != null)
            _globalLayer.renderer.enabled = enabled;

        if (_dualLayerActive && _localLayer.renderer != null)
            _localLayer.renderer.enabled = enabled;
    }

    Material CreateGridMaterial()
    {
        var source = Resources.Load<Material>(GridMaterialResourcePath);
        Material mat = null;

        if (source != null && source.shader != null && source.shader.name == GridShaderName)
            mat = new Material(source);

        if (mat == null)
        {
            var shader = Shader.Find(GridShaderName);
            if (shader != null)
            {
                mat = new Material(shader);
                mat.SetColor("_GridColor", new Color(0.45f, 0.78f, 1f, 0.88f));
                mat.SetColor("_FillColor", new Color(0.06f, 0.12f, 0.24f, 0.16f));
                mat.SetColor("_EmissionColor", new Color(0.18f, 0.42f, 0.75f, 0f));
                mat.SetFloat("_GridWorldScale", 0.1f);
                mat.SetFloat("_LineWidth", 0.018f);
                mat.SetFloat("_MaxLineAA", 0.05f);
                mat.SetFloat("_RimBoost", 0.65f);
            }
        }

        if (mat == null)
        {
            LogShaderMissingOnce();
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(lit);
            mat.SetFloat("_Surface", 1f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetColor("_BaseColor", new Color(0.3f, 0.6f, 1f, 0.35f));
            mat.renderQueue = 2950;
            return mat;
        }

        ApplyPlatformMaterialTuning(mat);
        return mat;
    }

    void ApplyLayerMaterials()
    {
        if (_globalLayer.material == null)
            return;

        ApplyPlatformMaterialTuning(_globalLayer.material);

        if (_dualLayerActive)
        {
            Color globalGrid = _globalLayer.material.GetColor("_GridColor");
            globalGrid.a = globalGridAlpha;
            _globalLayer.material.SetColor("_GridColor", globalGrid);

            Color globalFill = _globalLayer.material.GetColor("_FillColor");
            globalFill.a = globalFillAlpha;
            _globalLayer.material.SetColor("_FillColor", globalFill);
            _globalLayer.material.renderQueue = 2950;

            if (_localLayer.material != null)
            {
                ApplyPlatformMaterialTuning(_localLayer.material);

                Color localGrid = _localLayer.material.GetColor("_GridColor");
                localGrid.a = localGridAlpha;
                _localLayer.material.SetColor("_GridColor", localGrid);

                Color localFill = _localLayer.material.GetColor("_FillColor");
                localFill.a = 0.04f;
                _localLayer.material.SetColor("_FillColor", localFill);
                _localLayer.material.SetFloat("_RimBoost", localRimBoost);
                _localLayer.material.renderQueue = 2951;
            }
        }
        else
        {
            var source = Resources.Load<Material>(GridMaterialResourcePath);
            if (source != null)
            {
                _globalLayer.material.SetColor("_GridColor", source.GetColor("_GridColor"));
                _globalLayer.material.SetColor("_FillColor", source.GetColor("_FillColor"));
                _globalLayer.material.SetFloat("_RimBoost", source.GetFloat("_RimBoost"));
            }
            else
            {
                _globalLayer.material.SetColor("_GridColor", new Color(0.45f, 0.78f, 1f, 0.88f));
                _globalLayer.material.SetColor("_FillColor", new Color(0.06f, 0.12f, 0.24f, 0.16f));
                _globalLayer.material.SetFloat("_RimBoost", 0.65f);
            }

            _globalLayer.material.renderQueue = 2950;
        }
    }

    static void ApplyPlatformMaterialTuning(Material mat)
    {
#if UNITY_ANDROID || UNITY_IOS
        if (mat.HasProperty("_FillColor"))
        {
            Color fill = mat.GetColor("_FillColor");
            fill.a = Mathf.Min(fill.a, 0.08f);
            mat.SetColor("_FillColor", fill);
        }

        if (mat.HasProperty("_MaxLineAA"))
            mat.SetFloat("_MaxLineAA", 0.05f);
#else
        if (mat.HasProperty("_MaxLineAA"))
            mat.SetFloat("_MaxLineAA", 0.08f);
#endif
    }

    static void LogShaderMissingOnce()
    {
        if (_shaderMissingWarningLogged)
            return;

        _shaderMissingWarningLogged = true;
        Debug.LogWarning(
            "SpacetimeGridController: Custom/SpacetimeGrid shader/material was not found. " +
            "Gravity grid will render as a solid fallback surface until SpacetimeGrid is included in the build.");
    }

    void CacheBodies()
    {
        _bodies.Clear();
        _masses.Clear();
        _bodyNames.Clear();

        for (int i = 0; i < BodyNames.Length; i++)
        {
            var body = GameObject.Find(BodyNames[i]);
            if (body == null)
                continue;

            _bodies.Add(body.transform);
            _bodyNames.Add(BodyNames[i]);
            _masses.Add(SolarSystemCatalog.GetGravityWellMass(BodyNames[i], sunMassMultiplier));
        }
    }

    float GetEffectiveDepthScale()
    {
        float extent = Mathf.Max(1f, halfExtentX);
        float reference = Mathf.Max(1f, depthScaleReferenceExtent);
        return depthScale * (extent / reference);
    }

    bool ShouldIncludeBody(int bodyIndex, BodyFilter filter)
    {
        bool isSun = _bodyNames[bodyIndex] == "Sun";

        switch (filter)
        {
            case BodyFilter.SunOnly:
                return isSun;
            case BodyFilter.PlanetsAndMoonsOnly:
                return !isSun;
            default:
                return true;
        }
    }

    float GetBodyMassBoost(int bodyIndex)
    {
        if (MoonNames.Contains(_bodyNames[bodyIndex]))
            return moonMassBoost;

        return localMassBoost;
    }

    float ComputeDisplacement(
        Vector3 worldPos,
        BodyFilter filter,
        float softeningRadius,
        float massBoost,
        float minHorizontalDistance = 0f)
    {
        float softeningSq = softeningRadius * softeningRadius;
        float minHorizSq = minHorizontalDistance > 0f
            ? minHorizontalDistance * minHorizontalDistance
            : 0f;
        float effectiveDepthScale = GetEffectiveDepthScale();
        float displacement = 0f;

        for (int i = 0; i < _bodies.Count; i++)
        {
            if (!ShouldIncludeBody(i, filter))
                continue;

            var body = _bodies[i];
            if (body == null)
                continue;

            Vector3 bodyPos = body.position;
            float dx = worldPos.x - bodyPos.x;
            float dz = worldPos.z - bodyPos.z;
            float horizSq = dx * dx + dz * dz;
            if (minHorizSq > 0f && horizSq < minHorizSq)
                horizSq = minHorizSq;
            float distSq = horizSq + softeningSq;
            float boost = filter == BodyFilter.PlanetsAndMoonsOnly ? GetBodyMassBoost(i) : 1f;
            displacement -= effectiveDepthScale * _masses[i] * massBoost * boost / distSq;
        }

        return displacement;
    }

    void DeformMesh()
    {
        if (_bodies.Count == 0)
            CacheBodies();

        if (_baseVertices == null || _globalLayer.mesh == null)
            return;

        if (_dualLayerActive)
            DeformDualLayer();
        else
            DeformSingleLayer();
    }

    void DeformSingleLayer()
    {
        for (int v = 0; v < _baseVertices.Length; v++)
        {
            Vector3 local = _baseVertices[v];
            Vector3 world = transform.TransformPoint(local);
            float displacement = ComputeDisplacement(world, BodyFilter.All, softening, 1f);
            _globalLayer.workingVertices[v] = new Vector3(local.x, local.y + displacement, local.z);
        }

        ApplyMeshDeformation(_globalLayer);
    }

    void DeformDualLayer()
    {
        for (int v = 0; v < _baseVertices.Length; v++)
        {
            Vector3 local = _baseVertices[v];
            Vector3 world = transform.TransformPoint(local);

            float sunDisplacement = ComputeDisplacement(world, BodyFilter.SunOnly, softening, 1f);
            Vector3 globalVertex = new Vector3(local.x, local.y + sunDisplacement, local.z);
            _globalLayer.workingVertices[v] = globalVertex;

            float planetDisplacement = ComputeDisplacement(world, BodyFilter.PlanetsAndMoonsOnly, localSoftening, 1f);
            _localLayer.workingVertices[v] = new Vector3(
                globalVertex.x,
                globalVertex.y + planetDisplacement,
                globalVertex.z);
        }

        ApplyMeshDeformation(_globalLayer);

        if (_localLayer.mesh != null)
            ApplyMeshDeformation(_localLayer);
    }

    static void ApplyMeshDeformation(GridLayer layer)
    {
        layer.mesh.vertices = layer.workingVertices;
        layer.mesh.RecalculateNormals();
        layer.mesh.RecalculateBounds();
    }
}
