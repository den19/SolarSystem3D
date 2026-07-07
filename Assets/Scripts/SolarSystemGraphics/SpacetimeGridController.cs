using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Rubber-sheet spacetime grid deformed by Sun, planets, and major moons in Level1.
/// </summary>
public class SpacetimeGridController : MonoBehaviour
{
    const string GridMaterialResourcePath = "SpacetimeGrid";
    const string GridShaderName = "Custom/SpacetimeGrid";

    static readonly string[] BodyNames =
    {
        "Sun", "Mercury", "Venus", "Earth", "Mars", "Phobos", "Deimos", "Jupiter", "Saturn", "Uranus", "Neptune", "Moon", "Titan", "Ganymede"
    };

    static bool _shaderMissingWarningLogged;

    [SerializeField] float halfExtentX = 120f;
    [SerializeField] float halfExtentZ = 120f;
    [SerializeField] float baseY = -2f;
    [SerializeField] float depthScale = 18f;
    [SerializeField] float softening = 2.5f;
    [SerializeField] float sunMassMultiplier = 8f;
    [SerializeField] int desktopSegments = 96;
    [SerializeField] int mobileSegments = 64;

    readonly List<Transform> _bodies = new List<Transform>();
    readonly List<float> _masses = new List<float>();

    Mesh _mesh;
    MeshFilter _meshFilter;
    MeshRenderer _meshRenderer;
    Vector3[] _baseVertices;
    Vector3[] _workingVertices;
    bool _built;

    void Awake()
    {
        BuildGrid();
        ApplySetting(GravityGridSettings.UseGravityGrid);
    }

    void OnEnable()
    {
        GravityGridSettings.UseGravityGridChanged += OnUseGravityGridChanged;
    }

    void OnDisable()
    {
        GravityGridSettings.UseGravityGridChanged -= OnUseGravityGridChanged;
    }

    void LateUpdate()
    {
        if (!_built || _meshRenderer == null || !_meshRenderer.enabled)
            return;

        DeformMesh();
    }

    void OnUseGravityGridChanged(bool enabled) => ApplySetting(enabled);

    public void SetHalfExtent(float extent)
    {
        halfExtentX = extent;
        halfExtentZ = extent;
        if (_built)
            RebuildGridMesh();
    }

    void RebuildGridMesh()
    {
        if (_mesh != null)
            Destroy(_mesh);

        CacheBodies();

        int segments = mobileSegments;
#if !UNITY_ANDROID && !UNITY_IOS
        segments = desktopSegments;
#endif

        _mesh = GravityGridMeshUtility.BuildFlatGrid(halfExtentX, halfExtentZ, segments, segments);
        _baseVertices = _mesh.vertices;
        _workingVertices = new Vector3[_baseVertices.Length];

        if (_meshFilter == null)
            _meshFilter = gameObject.GetComponent<MeshFilter>();
        if (_meshFilter == null)
            _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshFilter.sharedMesh = _mesh;

        DeformMesh();
    }

    void BuildGrid()
    {
        CacheBodies();

        int segments = mobileSegments;
#if !UNITY_ANDROID && !UNITY_IOS
        segments = desktopSegments;
#endif

        _mesh = GravityGridMeshUtility.BuildFlatGrid(halfExtentX, halfExtentZ, segments, segments);
        _baseVertices = _mesh.vertices;
        _workingVertices = new Vector3[_baseVertices.Length];

        _meshFilter = gameObject.GetComponent<MeshFilter>();
        if (_meshFilter == null)
            _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshFilter.sharedMesh = _mesh;

        _meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (_meshRenderer == null)
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();

        _meshRenderer.sharedMaterial = CreateGridMaterial();
        _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _meshRenderer.receiveShadows = false;

        transform.position = new Vector3(0f, baseY, 0f);
        _built = true;
        DeformMesh();
    }

    void ApplySetting(bool enabled)
    {
        if (_meshRenderer != null)
            _meshRenderer.enabled = enabled;
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
        mat.renderQueue = 2950;
        return mat;
    }

    static void ApplyPlatformMaterialTuning(Material mat)
    {
#if UNITY_ANDROID || UNITY_IOS
        mat.SetColor("_FillColor", new Color(0.06f, 0.12f, 0.24f, 0.08f));
        mat.SetFloat("_MaxLineAA", 0.05f);
#else
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

        for (int i = 0; i < BodyNames.Length; i++)
        {
            var body = GameObject.Find(BodyNames[i]);
            if (body == null)
                continue;

            _bodies.Add(body.transform);
            float scale = Mathf.Max(0.01f, body.transform.lossyScale.x);
            float mass = scale * scale * scale;
            if (BodyNames[i] == "Sun")
                mass *= sunMassMultiplier;
            _masses.Add(mass);
        }
    }

    void DeformMesh()
    {
        if (_bodies.Count == 0)
            CacheBodies();

        float softeningSq = softening * softening;

        for (int v = 0; v < _baseVertices.Length; v++)
        {
            Vector3 local = _baseVertices[v];
            Vector3 world = transform.TransformPoint(local);
            float displacement = 0f;

            for (int i = 0; i < _bodies.Count; i++)
            {
                var body = _bodies[i];
                if (body == null)
                    continue;

                Vector3 bodyPos = body.position;
                float dx = world.x - bodyPos.x;
                float dz = world.z - bodyPos.z;
                float distSq = dx * dx + dz * dz + softeningSq;
                displacement -= depthScale * _masses[i] / distSq;
            }

            _workingVertices[v] = new Vector3(local.x, local.y + displacement, local.z);
        }

        _mesh.vertices = _workingVertices;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }
}
