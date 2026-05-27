using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies HD Resources materials to planet bodies when Extra Graphics is on,
/// restores the scene's shared materials otherwise, manages HD-only overlays, and swaps ring LODs (subtle vs rich).
/// </summary>
[DefaultExecutionOrder(-20)]
public class PlanetTextureManager : MonoBehaviour
{
    struct SwapEntry
    {
        public Renderer Renderer;
        public Material StandardShared;
        public Material HdShared;
    }

    struct OverlayToggle
    {
        public Renderer Renderer;
        /// <summary>When true the renderer stays disabled whenever Extra Graphics is off.</summary>
        public bool OnlyWhenHd;
    }

    static PlanetTextureManager _instance;

    [SerializeField] bool manageLevelNamed = true;
    [SerializeField] string levelSceneName = "Level1";

    bool _wired;
    bool _bootSceneOk;

    readonly List<SwapEntry> Swaps = new List<SwapEntry>();
    readonly List<OverlayToggle> OverlayToggles = new List<OverlayToggle>();

    Shader _urpLit;

    Renderer _sunBloomRenderer;

    Mesh _saturnRingMesh;
    Renderer _saturnRingSubtle;
    Renderer _saturnRingHdLayer;

    Mesh _uranusRingMesh;
    Renderer _uranusRingSubtle;
    Renderer _uranusRingHdLayer;

    Material _earthCloudMat;
    Material _venusAtmosphereMat;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EvaluateSceneBoot(SceneManager.GetActiveScene());

        _urpLit ??= Shader.Find("Universal Render Pipeline/Lit");

        if (_bootSceneOk)
            BootstrapIfNeeded();

        SpawnUiIfPossible();
    }

    void Start() => SpawnUiIfPossible();

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        GraphicsSettings.UseExtraGraphicsChanged -= OnUseExtraGraphicsChanged;

        if (_instance == this) _instance = null;

        if (_earthCloudMat != null) Destroy(_earthCloudMat);
        if (_venusAtmosphereMat != null) Destroy(_venusAtmosphereMat);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode _) => EvaluateSceneBoot(scene);

    void EvaluateSceneBoot(Scene scene)
    {
        _bootSceneOk = !manageLevelNamed || scene.name == levelSceneName;
        if (_bootSceneOk)
            BootstrapIfNeeded();

        SpawnUiIfPossible();
    }

    void BootstrapIfNeeded()
    {
        if (!_bootSceneOk || _wired || _urpLit == null) return;
        _wired = true;

        RegisterBodySwap("Sun", "PlanetGraphicsHD/SunTexture_HD");
        RegisterBodySwap("Mercury", "PlanetGraphicsHD/MercuryTexture_HD");
        RegisterBodySwap("Venus", "PlanetGraphicsHD/VenusTexture_HD");
        RegisterBodySwap("Earth", "PlanetGraphicsHD/EarthTexture_HD");
        RegisterBodySwap("Mars", "PlanetGraphicsHD/MarsTexture_HD");
        RegisterBodySwap("Jupiter", "PlanetGraphicsHD/JupiterTexture_HD");
        RegisterBodySwap("Saturn", "PlanetGraphicsHD/SaturnTexture_HD");
        RegisterBodySwap("Uranus", "PlanetGraphicsHD/UranusTexture_HD");
        RegisterBodySwap("Neptune", "PlanetGraphicsHD/NeptuneTexture_HD");
        RegisterBodySwap("Moon", "PlanetGraphicsHD/MoonTexture_HD");

        EnsureSunBloom();
        EnsureEarthCloudOverlay();
        EnsureVenusAtmosphereOverlay();
        EnsurePlanetaryRingLayers();

        GraphicsSettings.UseExtraGraphicsChanged -= OnUseExtraGraphicsChanged;
        GraphicsSettings.UseExtraGraphicsChanged += OnUseExtraGraphicsChanged;

        ApplyAll();
    }

    void RegisterBodySwap(string objectName, string hdMaterialResourcePath)
    {
        var go = GameObject.Find(objectName);
        if (!go)
        {
            Debug.LogWarning($"PlanetTextureManager: GameObject '{objectName}' missing.");
            return;
        }

        var mr = go.GetComponent<MeshRenderer>();
        if (!mr || !mr.sharedMaterial)
        {
            Debug.LogWarning($"PlanetTextureManager: mesh renderer/material missing on '{objectName}'.");
            return;
        }

        var hd = Resources.Load<Material>(hdMaterialResourcePath);
        if (!hd)
        {
            Debug.LogWarning($"PlanetTextureManager: missing HD Material at Resources '{hdMaterialResourcePath}'.");
            return;
        }

        Swaps.Add(new SwapEntry { Renderer = mr, StandardShared = mr.sharedMaterial, HdShared = hd });
    }

    void EnsureSunBloom()
    {
        var sun = GameObject.Find("Sun");
        if (!sun) return;

        var tr = sun.transform.Find("ExtraGraphicsSunBloom");
        if (!tr)
        {
            float scaleBoost = SphereWorldRadius(sun) * 2.06f /
                               Mathf.Max(0.0001f, MaxLossyScalar(sun.transform.lossyScale));

            var bloom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bloom.name = "ExtraGraphicsSunBloom";
            bloom.transform.SetParent(sun.transform, false);
            bloom.transform.localPosition = Vector3.zero;
            bloom.transform.localRotation = Quaternion.identity;
            bloom.transform.localScale = Vector3.one * scaleBoost;

            var col = bloom.GetComponent<Collider>();
            if (col) Destroy(col);

            var mr = bloom.GetComponent<MeshRenderer>();
            var mat = new Material(_urpLit);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_EmissionColor", new Color(1.6f, 1.1f, 0.65f));
            mat.SetFloat("_Smoothness", 1f);
            mat.SetFloat("_Metallic", 0f);

            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            _sunBloomRenderer = mr;
        }
        else
        {
            _sunBloomRenderer = tr.GetComponent<MeshRenderer>();
        }
    }

    static float MaxLossyScalar(Vector3 ls) =>
        Mathf.Max(Mathf.Abs(ls.x), Mathf.Max(Mathf.Abs(ls.y), Mathf.Abs(ls.z)));

    static float SphereWorldRadius(GameObject go)
    {
        var col = go.GetComponent<SphereCollider>();
        float r = col ? col.radius : 0.5f;
        return r * MaxLossyScalar(go.transform.lossyScale);
    }

    void EnsureEarthCloudOverlay()
    {
        var earth = GameObject.Find("Earth");
        var atlas = Resources.Load<Texture2D>("PlanetTexturesHD/EarthClouds_8k");
        if (!earth || !atlas) return;

        if (earth.transform.Find("ExtraGraphicsEarthClouds"))
            return;

        BuildOverlaySphere(earth.transform, "ExtraGraphicsEarthClouds", atlas,
            new Color(1f, 1f, 1f, 0.7f), 1.022f,
            mat => { _earthCloudMat = mat; });
    }

    void EnsureVenusAtmosphereOverlay()
    {
        var venus = GameObject.Find("Venus");
        var atmosphere = Resources.Load<Texture2D>("PlanetTexturesHD/VenusAtmosphere_8k");
        if (!venus || !atmosphere)
            return;

        if (venus.transform.Find("ExtraGraphicsVenusAtmosphere"))
            return;

        BuildOverlaySphere(venus.transform, "ExtraGraphicsVenusAtmosphere", atmosphere,
            new Color(1f, 0.93f, 0.65f, 0.43f), 1.019f,
            mat => { _venusAtmosphereMat = mat; });
    }

    void BuildOverlaySphere(Transform parent, string childName, Texture2D baseMap,
        Color tint, float uniformScaleVsParent, System.Action<Material> registerMaterialCallback)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = childName;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localRotation = Quaternion.identity;
        sphere.transform.localScale = Vector3.one * uniformScaleVsParent;

        var col = sphere.GetComponent<Collider>();
        if (col) Destroy(col);

        var mr = sphere.GetComponent<MeshRenderer>();
        var mat = new Material(_urpLit);
        mat.SetTexture("_BaseMap", baseMap);
        mat.SetColor("_BaseColor", tint);
        ConfigureLitTransparent(mat, 3005);
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.enabled = false;

        registerMaterialCallback?.Invoke(mat);

        OverlayToggles.Add(new OverlayToggle { Renderer = mr, OnlyWhenHd = true });
    }

    void EnsurePlanetaryRingLayers()
    {
        var ringAtlas = Resources.Load<Texture2D>("PlanetTexturesHD/SaturnRing_8k");
        if (!ringAtlas)
        {
            Debug.LogWarning("PlanetTextureManager: Resources/PlanetTexturesHD/SaturnRing_8k missing.");
            return;
        }

        SetupRingLayerPair("Saturn", ringAtlas, new Color(1f, 0.96f, 0.82f, 0.62f),
            new Color(1f, 0.98f, 0.9f, 0.93f),
            ref _saturnRingMesh, ref _saturnRingSubtle, ref _saturnRingHdLayer);

        SetupRingLayerPair("Uranus", ringAtlas, new Color(0.5f, 0.82f, 1f, 0.28f),
            new Color(0.55f, 0.88f, 1f, 0.58f),
            ref _uranusRingMesh, ref _uranusRingSubtle, ref _uranusRingHdLayer);
    }

    void SetupRingLayerPair(string planetName, Texture2D ringTex, Color subtleTint, Color vividTint,
        ref Mesh cachedMesh,
        ref Renderer subtleRendererStorage,
        ref Renderer vividRendererStorage)
    {
        var planet = GameObject.Find(planetName);
        if (!planet) return;

        const string subtleGo = "ExtraGraphicsPlanetRing_Subtle";
        const string vividGo = "ExtraGraphicsPlanetRing_HD";

        subtleRendererStorage = planet.transform.Find(subtleGo)?.GetComponent<MeshRenderer>();
        vividRendererStorage = planet.transform.Find(vividGo)?.GetComponent<MeshRenderer>();

        if (!subtleRendererStorage && !vividRendererStorage)
        {
            float inner = RingInnerLocal(planet);
            float outer = RingOuterLocal(planet);
            cachedMesh = RingMeshUtility.BuildAnnulus(inner, outer, 144);

            subtleRendererStorage = CreateRingObject(planet.transform, subtleGo, cachedMesh, ringTex, subtleTint, 2975);
            vividRendererStorage = CreateRingObject(planet.transform, vividGo, cachedMesh, ringTex, vividTint, 2990);

            ConfigureLitTransparent(subtleRendererStorage.sharedMaterial, 2975);
            ConfigureLitTransparent(vividRendererStorage.sharedMaterial, 2990);

            subtleRendererStorage.enabled = false;
            vividRendererStorage.enabled = false;
        }

        OverlayToggles.Add(new OverlayToggle { Renderer = subtleRendererStorage, OnlyWhenHd = false });
        OverlayToggles.Add(new OverlayToggle { Renderer = vividRendererStorage, OnlyWhenHd = false });
    }

    static MeshRenderer CreateRingObject(Transform host, string name, Mesh mesh, Texture2D ringTex,
        Color tint, int queue)
    {
        var go = new GameObject(name);
        go.transform.SetParent(host, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = go.AddComponent<MeshRenderer>();

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(lit);
        mat.SetTexture("_BaseMap", ringTex);
        mat.SetFloat("_Smoothness", 0.35f);
        mat.SetFloat("_Metallic", 0.08f);
        mat.SetColor("_BaseColor", tint);
        mr.sharedMaterial = mat;
        ConfigureLitTransparent(mat, queue);

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;
        return mr;
    }

    static float RingInnerLocal(GameObject planet)
    {
        var col = planet.GetComponent<SphereCollider>();
        float r = col ? col.radius : 0.5f;
        return r * 1.05f;
    }

    static float RingOuterLocal(GameObject planet)
    {
        var col = planet.GetComponent<SphereCollider>();
        float r = col ? col.radius : 0.5f;
        return r * 1.6f;
    }

    static void ConfigureLitTransparent(Material mat, int renderQueueOverride)
    {
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_AlphaClip", 0f);

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.renderQueue = renderQueueOverride;
    }

    void OnUseExtraGraphicsChanged(bool _) => ApplyAll();

    void ApplyAll()
    {
        if (!_wired || !_bootSceneOk) return;

        bool hq = GraphicsSettings.UseExtraGraphics;

        for (var i = 0; i < Swaps.Count; i++)
        {
            var e = Swaps[i];
            if (e.Renderer != null && e.StandardShared != null && e.HdShared != null)
                e.Renderer.sharedMaterial = hq ? e.HdShared : e.StandardShared;
        }

        for (var i = 0; i < OverlayToggles.Count; i++)
        {
            var ot = OverlayToggles[i];
            if (!ot.Renderer) continue;

            if (ot.OnlyWhenHd)
            {
                ot.Renderer.enabled = hq;
                continue;
            }

            bool vivid = ot.Renderer == _saturnRingHdLayer || ot.Renderer == _uranusRingHdLayer;
            bool subtle = ot.Renderer == _saturnRingSubtle || ot.Renderer == _uranusRingSubtle;
            ot.Renderer.enabled = (hq && vivid) || (!hq && subtle);
        }

        if (_sunBloomRenderer)
            _sunBloomRenderer.enabled = hq;
        else EnsureSunBloom();
    }

    public void SetGraphicsQuality(bool highQuality) => GraphicsSettings.SetUseExtraGraphics(highQuality);

    void SpawnUiIfPossible()
    {
        var canvasRt = GameObject.Find("MainScreenCanvas")?.GetComponent<RectTransform>();
        if (!canvasRt || !_bootSceneOk) return;
        if (canvasRt.Find("ExtraGraphicsPanel_Runtime")) return;

        ExtraGraphicsHud.SpawnHud(canvasRt);
    }
}
