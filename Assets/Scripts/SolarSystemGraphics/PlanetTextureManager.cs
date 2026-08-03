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

    struct RingPlanetConfig
    {
        public string PlanetName;
        public string TextureResourcePath;
        public string FallbackTexturePath;
        public Color SubtleTint;
        public Color VividTint;
        public float OuterFactor;
        public int Segments;
        public float TiltX;
        public bool EnhancedPresentation;
        public float EmissionScale;
        public RingBandDescriptor[] Bands;
    }

    static PlanetTextureManager _instance;

    [SerializeField] bool manageLevelNamed = true;
    [SerializeField] string levelSceneName = "Level1";

    bool _wired;
    bool _bootSceneOk;

    readonly List<SwapEntry> Swaps = new List<SwapEntry>();
    readonly List<OverlayToggle> OverlayToggles = new List<OverlayToggle>();

    Shader _urpLit;
    Shader _planetRingShader;

    Renderer _sunBloomRenderer;

    Mesh _saturnRingMesh;
    Renderer _saturnRingSubtle;
    Renderer _saturnRingHdLayer;

    Mesh _uranusRingMesh;
    Renderer _uranusRingSubtle;
    Renderer _uranusRingHdLayer;

    Mesh _jupiterRingMesh;
    Renderer _jupiterRingSubtle;
    Renderer _jupiterRingHdLayer;

    Mesh _neptuneRingMesh;
    Renderer _neptuneRingSubtle;
    Renderer _neptuneRingHdLayer;

    Material _earthCloudMat;
    Material _venusAtmosphereMat;
    Material _titanHazeMat;

    const float SaturnRingTiltX = 26.7f;
    const float SaturnRingOuterFactor = 1.8f;
    const int SaturnRingSegments = 192;
    const float JupiterRingTiltX = 3f;
    const float JupiterRingOuterFactor = 1.48f;
    const int JupiterRingSegments = 192;
    const float UranusRingTiltX = 25f;
    const float UranusRingOuterFactor = 1.72f;
    const int UranusRingSegments = 192;
    const float NeptuneRingTiltX = 28.3f;
    const float NeptuneRingOuterFactor = 1.55f;
    const int NeptuneRingSegments = 192;

    static readonly RingBandDescriptor[] JupiterRingBands =
    {
        new RingBandDescriptor(1.08f, 1.22f),
        new RingBandDescriptor(1.28f, 1.48f),
    };

    static readonly RingBandDescriptor[] UranusRingBands =
    {
        new RingBandDescriptor(1.06f, 1.14f),
        new RingBandDescriptor(1.16f, 1.28f),
        new RingBandDescriptor(1.30f, 1.75f),
    };

    static readonly RingBandDescriptor[] NeptuneRingBands =
    {
        new RingBandDescriptor(1.08f, 1.12f),
        new RingBandDescriptor(1.14f, 1.18f),
        new RingBandDescriptor(1.20f, 1.26f),
        new RingBandDescriptor(1.28f, 1.36f),
        new RingBandDescriptor(1.38f, 1.48f),
    };

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
        var ringMat = Resources.Load<Material>("PlanetGraphicsHD/PlanetRing");
        _planetRingShader ??= ringMat != null ? ringMat.shader : Shader.Find("Custom/PlanetRing");
        if (_planetRingShader != null && !_planetRingShader.isSupported)
            _planetRingShader = null;

        if (_bootSceneOk)
            BootstrapIfNeeded();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        GraphicsSettings.UseExtraGraphicsChanged -= OnUseExtraGraphicsChanged;
        SunAppearanceSettings.UseRealSunChanged -= OnUseRealSunChanged;

        ScaleSettings.UseRealSizesChanged -= OnScalePresentationChanged;
        ScaleSettings.UseRealDistancesChanged -= OnScalePresentationChanged;

        if (_instance == this) _instance = null;

        if (_earthCloudMat != null) Destroy(_earthCloudMat);
        if (_venusAtmosphereMat != null) Destroy(_venusAtmosphereMat);
        if (_titanHazeMat != null) Destroy(_titanHazeMat);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode _) => EvaluateSceneBoot(scene);

    void EvaluateSceneBoot(Scene scene)
    {
        _bootSceneOk = !manageLevelNamed || scene.name == levelSceneName;
        if (_bootSceneOk)
            BootstrapIfNeeded();
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
        RegisterBodySwap("Titan", "PlanetGraphicsHD/TitanTexture_HD");
        RegisterBodySwap("Io", "PlanetGraphicsHD/IoTexture_HD");
        RegisterBodySwap("Europa", "PlanetGraphicsHD/EuropaTexture_HD");
        RegisterBodySwap("Ganymede", "PlanetGraphicsHD/GanymedeTexture_HD");
        RegisterBodySwap("Callisto", "PlanetGraphicsHD/CallistoTexture_HD");
        RegisterBodySwap("Phobos", "PlanetGraphicsHD/PhobosTexture_HD");
        RegisterBodySwap("Deimos", "PlanetGraphicsHD/DeimosTexture_HD");
        RegisterBodySwap("Triton", "PlanetGraphicsHD/TritonTexture_HD");
        RegisterBodySwap("Pluto", "PlanetGraphicsHD/PlutoTexture_HD");

        EnsureSunBloom();
        EnsureEarthCloudOverlay();
        EnsureVenusAtmosphereOverlay();
        EnsureTitanHazeOverlay();
        EnsurePlanetaryRingLayers();

        GraphicsSettings.UseExtraGraphicsChanged -= OnUseExtraGraphicsChanged;
        GraphicsSettings.UseExtraGraphicsChanged += OnUseExtraGraphicsChanged;

        SunAppearanceSettings.UseRealSunChanged -= OnUseRealSunChanged;
        SunAppearanceSettings.UseRealSunChanged += OnUseRealSunChanged;

        ScaleSettings.UseRealSizesChanged -= OnScalePresentationChanged;
        ScaleSettings.UseRealDistancesChanged -= OnScalePresentationChanged;
        ScaleSettings.UseRealSizesChanged += OnScalePresentationChanged;
        ScaleSettings.UseRealDistancesChanged += OnScalePresentationChanged;

        ApplyAll();
    }

    void OnScalePresentationChanged(bool _) => ApplyAll();

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

        if (!MaterialHasAlbedo(mr.sharedMaterial))
            Debug.LogWarning($"PlanetTextureManager: '{objectName}' standard material has no albedo texture (pink/magenta risk on device).");
        if (!MaterialHasAlbedo(hd))
            Debug.LogWarning($"PlanetTextureManager: '{objectName}' HD material at '{hdMaterialResourcePath}' has no albedo texture (pink/magenta risk on device).");

        Swaps.Add(new SwapEntry { Renderer = mr, StandardShared = mr.sharedMaterial, HdShared = hd });
    }

    static bool MaterialHasAlbedo(Material mat)
    {
        if (mat == null) return false;
        if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null) return true;
        return mat.mainTexture != null;
    }

    void EnsureSunBloom()
    {
        var sun = GameObject.Find("Sun");
        if (!sun) return;

        var tr = sun.transform.Find("ExtraGraphicsSunBloom");
        if (!tr)
        {
            float scaleBoost = SphereWorldRadius(sun) * 2.368f /
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

    void EnsureTitanHazeOverlay()
    {
        var titan = GameObject.Find("Titan");
        var atmosphere = Resources.Load<Texture2D>("PlanetTexturesHD/VenusAtmosphere_8k");
        if (!titan || !atmosphere)
            return;

        if (titan.transform.Find("ExtraGraphicsTitanHaze"))
            return;

        BuildOverlaySphere(titan.transform, "ExtraGraphicsTitanHaze", atmosphere,
            new Color(1f, 0.72f, 0.45f, 0.38f), 1.045f,
            mat => { _titanHazeMat = mat; });
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
        const string saturnFallback = "PlanetTexturesHD/SaturnRing_8k";
        var saturnTex = Resources.Load<Texture2D>(saturnFallback);
        if (!saturnTex)
        {
            Debug.LogWarning("PlanetTextureManager: Resources/PlanetTexturesHD/SaturnRing_8k missing.");
            return;
        }

        SetupRingLayerPair(new RingPlanetConfig
        {
            PlanetName = "Saturn",
            TextureResourcePath = saturnFallback,
            FallbackTexturePath = saturnFallback,
            SubtleTint = new Color(1f, 0.96f, 0.82f, 0.78f),
            VividTint = new Color(1f, 0.98f, 0.9f, 0.98f),
            OuterFactor = SaturnRingOuterFactor,
            Segments = SaturnRingSegments,
            TiltX = SaturnRingTiltX,
            EnhancedPresentation = true,
            EmissionScale = 1f,
            Bands = null,
        }, ref _saturnRingMesh, ref _saturnRingSubtle, ref _saturnRingHdLayer, saturnTex);

        SetupRingLayerPair(new RingPlanetConfig
        {
            PlanetName = "Jupiter",
            TextureResourcePath = "PlanetTexturesHD/JupiterRing_8k",
            FallbackTexturePath = saturnFallback,
            SubtleTint = new Color(0.78f, 0.7f, 0.55f, 0.38f),
            VividTint = new Color(0.88f, 0.78f, 0.62f, 0.62f),
            OuterFactor = JupiterRingOuterFactor,
            Segments = JupiterRingSegments,
            TiltX = JupiterRingTiltX,
            EnhancedPresentation = true,
            EmissionScale = 0.35f,
            Bands = JupiterRingBands,
        }, ref _jupiterRingMesh, ref _jupiterRingSubtle, ref _jupiterRingHdLayer, saturnTex);

        SetupRingLayerPair(new RingPlanetConfig
        {
            PlanetName = "Uranus",
            TextureResourcePath = "PlanetTexturesHD/UranusRing_8k",
            FallbackTexturePath = saturnFallback,
            SubtleTint = new Color(0.52f, 0.84f, 1f, 0.42f),
            VividTint = new Color(0.58f, 0.9f, 1f, 0.78f),
            OuterFactor = UranusRingOuterFactor,
            Segments = UranusRingSegments,
            TiltX = UranusRingTiltX,
            EnhancedPresentation = true,
            EmissionScale = 0.6f,
            Bands = UranusRingBands,
        }, ref _uranusRingMesh, ref _uranusRingSubtle, ref _uranusRingHdLayer, saturnTex);

        SetupRingLayerPair(new RingPlanetConfig
        {
            PlanetName = "Neptune",
            TextureResourcePath = "PlanetTexturesHD/NeptuneRing_8k",
            FallbackTexturePath = saturnFallback,
            SubtleTint = new Color(0.38f, 0.48f, 0.72f, 0.38f),
            VividTint = new Color(0.45f, 0.58f, 0.82f, 0.68f),
            OuterFactor = NeptuneRingOuterFactor,
            Segments = NeptuneRingSegments,
            TiltX = NeptuneRingTiltX,
            EnhancedPresentation = true,
            EmissionScale = 0.4f,
            Bands = NeptuneRingBands,
        }, ref _neptuneRingMesh, ref _neptuneRingSubtle, ref _neptuneRingHdLayer, saturnTex);
    }

    static Texture2D LoadRingTexture(RingPlanetConfig config, Texture2D fallback)
    {
        var tex = Resources.Load<Texture2D>(config.TextureResourcePath);
        if (tex) return tex;

        Debug.LogWarning($"PlanetTextureManager: missing ring texture '{config.TextureResourcePath}', using fallback.");
        return fallback;
    }

    static Mesh BuildRingMesh(GameObject planet, RingPlanetConfig config)
    {
        if (config.Bands != null && config.Bands.Length > 0)
            return RingMeshUtility.BuildAnnulusBands(config.Bands, config.Segments);

        float inner = RingInnerLocal(planet);
        float outer = RingOuterLocal(planet, config.OuterFactor);
        return RingMeshUtility.BuildAnnulus(inner, outer, config.Segments);
    }

    void SetupRingLayerPair(RingPlanetConfig config, ref Mesh cachedMesh,
        ref Renderer subtleRendererStorage,
        ref Renderer vividRendererStorage,
        Texture2D fallbackTex)
    {
        var planet = GameObject.Find(config.PlanetName);
        if (!planet) return;

        var ringTex = LoadRingTexture(config, fallbackTex);

        const string subtleGo = "ExtraGraphicsPlanetRing_Subtle";
        const string vividGo = "ExtraGraphicsPlanetRing_HD";

        subtleRendererStorage = planet.transform.Find(subtleGo)?.GetComponent<MeshRenderer>();
        vividRendererStorage = planet.transform.Find(vividGo)?.GetComponent<MeshRenderer>();

        cachedMesh = BuildRingMesh(planet, config);

        if (!subtleRendererStorage && !vividRendererStorage)
        {
            subtleRendererStorage = CreateRingObject(planet.transform, subtleGo, cachedMesh, ringTex, config.SubtleTint,
                2975, config);
            vividRendererStorage = CreateRingObject(planet.transform, vividGo, cachedMesh, ringTex, config.VividTint,
                2990, config);

            subtleRendererStorage.enabled = false;
            vividRendererStorage.enabled = false;
        }
        else
        {
            RefreshRingMesh(subtleRendererStorage, cachedMesh);
            RefreshRingMesh(vividRendererStorage, cachedMesh);

            if (config.EnhancedPresentation)
            {
                ApplyRingPresentation(subtleRendererStorage, ringTex, config.SubtleTint, 2975, config);
                ApplyRingPresentation(vividRendererStorage, ringTex, config.VividTint, 2990, config);
            }
            else
            {
                ApplyRingTransform(subtleRendererStorage?.transform, config.TiltX);
                ApplyRingTransform(vividRendererStorage?.transform, config.TiltX);
            }
        }

        OverlayToggles.Add(new OverlayToggle { Renderer = subtleRendererStorage, OnlyWhenHd = false });
        OverlayToggles.Add(new OverlayToggle { Renderer = vividRendererStorage, OnlyWhenHd = false });
    }

    static void RefreshRingMesh(Renderer renderer, Mesh mesh)
    {
        if (!renderer || mesh == null) return;
        var mf = renderer.GetComponent<MeshFilter>();
        if (mf) mf.sharedMesh = mesh;
    }

    static void ApplyRingTransform(Transform ringTransform, float tiltX)
    {
        if (!ringTransform) return;
        ringTransform.localPosition = Vector3.zero;
        ringTransform.localScale = Vector3.one;
        ringTransform.localRotation = tiltX != 0f ? Quaternion.Euler(tiltX, 0f, 0f) : Quaternion.identity;
    }

    void ApplyRingPresentation(Renderer renderer, Texture2D ringTex, Color tint, int queue, RingPlanetConfig config)
    {
        if (!renderer) return;
        ApplyRingTransform(renderer.transform, config.TiltX);
        EnsureRingMaterial(renderer, ringTex, tint, queue, config);
    }

    void EnsureRingMaterial(Renderer renderer, Texture2D ringTex, Color tint, int queue, RingPlanetConfig config)
    {
        if (!renderer) return;

        var shader = _planetRingShader != null ? _planetRingShader : _urpLit;
        if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader != shader)
            renderer.sharedMaterial = new Material(shader);

        var mat = renderer.sharedMaterial;
        mat.SetTexture("_BaseMap", ringTex);

        if (shader == _planetRingShader)
        {
            mat.SetColor("_BaseColor", tint);
            mat.SetColor("_EmissionColor", new Color(
                tint.r * 0.2f * config.EmissionScale,
                tint.g * 0.18f * config.EmissionScale,
                tint.b * 0.12f * config.EmissionScale));
            mat.SetFloat("_RimBoost", 1.2f);
            mat.SetFloat("_SunInfluence", 0.65f);
            mat.renderQueue = queue;
        }
        else if (config.EnhancedPresentation)
            ConfigureRingMaterialLit(mat, tint, queue, config.EmissionScale);
        else
        {
            mat.SetFloat("_Smoothness", 0.35f);
            mat.SetColor("_BaseColor", tint);
            ConfigureLitTransparent(mat, queue);
        }
    }

    static void ConfigureRingMaterialLit(Material mat, Color tint, int queue, float emissionScale = 1f)
    {
        if (!mat) return;
        mat.SetColor("_BaseColor", tint);
        mat.SetFloat("_Smoothness", 0.55f);
        mat.SetFloat("_Metallic", 0.08f);
        mat.SetFloat("_Cull", 0f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(
            tint.r * 0.2f * emissionScale,
            tint.g * 0.18f * emissionScale,
            tint.b * 0.12f * emissionScale));
        ConfigureLitTransparent(mat, queue);
    }

    MeshRenderer CreateRingObject(Transform host, string name, Mesh mesh, Texture2D ringTex,
        Color tint, int queue, RingPlanetConfig config)
    {
        var go = new GameObject(name);
        go.transform.SetParent(host, false);
        ApplyRingTransform(go.transform, config.TiltX);

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = go.AddComponent<MeshRenderer>();
        var shader = _planetRingShader != null ? _planetRingShader : _urpLit;
        var mat = new Material(shader);
        mat.SetTexture("_BaseMap", ringTex);

        if (shader == _planetRingShader)
        {
            mat.SetColor("_BaseColor", tint);
            mat.SetColor("_EmissionColor", new Color(
                tint.r * 0.2f * config.EmissionScale,
                tint.g * 0.18f * config.EmissionScale,
                tint.b * 0.12f * config.EmissionScale));
            mat.SetFloat("_RimBoost", 1.2f);
            mat.SetFloat("_SunInfluence", 0.65f);
            mat.renderQueue = queue;
        }
        else if (config.EnhancedPresentation)
            ConfigureRingMaterialLit(mat, tint, queue, config.EmissionScale);
        else
        {
            mat.SetFloat("_Smoothness", 0.35f);
            mat.SetColor("_BaseColor", tint);
            ConfigureLitTransparent(mat, queue);
        }

        mr.sharedMaterial = mat;
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

    static float RingOuterLocal(GameObject planet, float outerFactor = 1.6f)
    {
        var col = planet.GetComponent<SphereCollider>();
        float r = col ? col.radius : 0.5f;
        return r * outerFactor;
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

    void OnUseRealSunChanged(bool _) => ApplyAll();

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

            bool vivid = ot.Renderer == _saturnRingHdLayer || ot.Renderer == _uranusRingHdLayer || ot.Renderer == _jupiterRingHdLayer || ot.Renderer == _neptuneRingHdLayer;
            bool subtle = ot.Renderer == _saturnRingSubtle || ot.Renderer == _uranusRingSubtle || ot.Renderer == _jupiterRingSubtle || ot.Renderer == _neptuneRingSubtle;
            ot.Renderer.enabled = (hq && vivid) || (!hq && subtle);
        }

        if (_sunBloomRenderer)
            _sunBloomRenderer.enabled = hq || SunAppearanceSettings.UseRealSun;
        else
            EnsureSunBloom();

        ConfigureSunPresentation(hq);
    }

    void ConfigureSunPresentation(bool hq)
    {
        var sun = GameObject.Find("Sun");
        var mr = sun != null ? sun.GetComponent<MeshRenderer>() : null;
        if (mr == null)
            return;

        bool realSun = SunAppearanceSettings.UseRealSun;

        if (!hq)
        {
            mr.receiveShadows = true;
            if (!realSun)
                return;

            var lowMat = mr.material;
            if (lowMat.HasProperty("_EmissionColor"))
            {
                lowMat.EnableKeyword("_EMISSION");
                lowMat.SetColor("_BaseColor", new Color(1f, 0.55f, 0.12f));
                lowMat.SetColor("_EmissionColor", new Color(5.1f, 2.04f, 0.102f));
            }

            return;
        }

        var mat = mr.material;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            if (realSun)
            {
                mat.SetColor("_BaseColor", new Color(1f, 0.55f, 0.12f));
                mat.SetColor("_EmissionColor", new Color(5.1f, 2.04f, 0.102f));
            }
            else
            {
                mat.SetColor("_BaseColor", Color.white);
                mat.SetColor("_EmissionColor", new Color(4.2f, 2.9f, 1.55f));
            }
        }

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.96f);

        mr.receiveShadows = false;

        if (_sunBloomRenderer != null)
        {
            var bloomMat = _sunBloomRenderer.material;
            if (realSun)
                bloomMat.SetColor("_EmissionColor", new Color(3.4f, 1.445f, 0.272f));
            else
                bloomMat.SetColor("_EmissionColor", new Color(2.4f, 1.55f, 0.9f));
            _sunBloomRenderer.receiveShadows = false;
        }
    }

    public void SetGraphicsQuality(bool highQuality) => GraphicsSettings.SetUseExtraGraphics(highQuality);
}
