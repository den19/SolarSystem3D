using System.Collections;
using SolarSystemApp;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sun coronal VFX: procedural sunspots, surface flicker overlay, and timed loop-like CME bursts.
/// Gated on Real Sun mode (independent of Extra Graphics).
/// </summary>
public class SunCoronalVfxController : MonoBehaviour
{
    const string LevelSceneName = "Level1";
    const string SunObjectName = "Sun";
    const string SunBloomObjectName = "ExtraGraphicsSunBloom";
    const string VfxRootName = "ExtraGraphicsSunCoronalVfx";
    const string SunspotsObjectName = "ExtraGraphicsSunSunspots";
    const string FlickerObjectName = "ExtraGraphicsSunSurfaceFlicker";
    const string CmeObjectName = "SunCmeLoop";

    const float SunspotsLocalScale = 1.006f;
    const float FlickerLocalScale = 1.018f;
    const float CmeEmitLocalRadius = 0.52f;
    const float ReferenceSunWorldRadius = 5f;
    const string CmeMaterialResourcePath = "SunCmeLoopParticle";

#if UNITY_ANDROID || UNITY_IOS
    const float CmeStartSizeMin = 0.12f;
    const float CmeStartSizeMax = 0.28f;
    const float CmeLengthScale = 1.5f;
    const float CmeBurstIntervalMin = 9f;
    const float CmeBurstIntervalMax = 16f;
    const float CmeInitialDelayMin = 2.5f;
    const float CmeInitialDelayMax = 5f;
    const float CmeContinuousRate = 8f;
#else
    const float CmeStartSizeMin = 0.07f;
    const float CmeStartSizeMax = 0.16f;
    const float CmeLengthScale = 1.2f;
    const float CmeBurstIntervalMin = 14f;
    const float CmeBurstIntervalMax = 26f;
    const float CmeInitialDelayMin = 4f;
    const float CmeInitialDelayMax = 8f;
    const float CmeContinuousRate = 6f;
#endif

    const float CmeBurstIntervalScaleRealSun = 0.45f;
    const float CmeInitialDelayScaleRealSun = 0.5f;

    enum CmeLoopKind
    {
        Compact,
        Arcing,
        Prominence,
        Wispy
    }

    static SunCoronalVfxController _instance;
    static bool _cmeMaterialWarningLogged;
    static Material _cachedCmeMaterial;
    static Texture2D _cachedCmeSoftTexture;

    GameObject _vfxRoot;
    Renderer _sunspotsRenderer;
    Renderer _flickerRenderer;
    Renderer _sunBaseRenderer;
    Renderer _sunBloomRenderer;
    ParticleSystem _cmeParticles;
    Transform _cmeTransform;
    Light _sunLight;
    Transform _sunTransform;
    float _baseLightIntensity = 100f;
    float _cmeSizeScale = 1f;
    Coroutine _cmeRoutine;
    bool _built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (_instance != null)
            return;

        var host = new GameObject("SunCoronalVfxController_Persistent");
        _instance = host.AddComponent<SunCoronalVfxController>();
        DontDestroyOnLoad(host);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GraphicsSettings.UseExtraGraphicsChanged += OnUseExtraGraphicsChanged;
        SunAppearanceSettings.UseRealSunChanged += OnUseRealSunChanged;
        ScaleSettings.UseRealSizesChanged += OnSunScaleSettingsChanged;
        ScaleSettings.UseRealDistancesChanged += OnSunScaleSettingsChanged;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GraphicsSettings.UseExtraGraphicsChanged -= OnUseExtraGraphicsChanged;
        SunAppearanceSettings.UseRealSunChanged -= OnUseRealSunChanged;
        ScaleSettings.UseRealSizesChanged -= OnSunScaleSettingsChanged;
        ScaleSettings.UseRealDistancesChanged -= OnSunScaleSettingsChanged;
        StopCmeRoutine();
    }

    void OnUseRealSunChanged(bool _) => ApplyHdState();

    void OnSunScaleSettingsChanged(bool _) => RefreshSunVfxScale();

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == LevelSceneName)
            TrySetupSunVfx();
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name == LevelSceneName)
            TrySetupSunVfx();
    }

    void OnUseExtraGraphicsChanged(bool _)
    {
        ApplyHdState();
    }

    void TrySetupSunVfx()
    {
        var sun = GameObject.Find(SunObjectName);
        if (!sun)
            return;

        _sunTransform = sun.transform;
        _sunBaseRenderer = sun.GetComponent<Renderer>();
        CacheSunBloomRenderer(sun.transform);

        if (!_built || _vfxRoot == null || _vfxRoot.transform.parent != sun.transform)
            BuildSunVfx(sun.transform);

        RefreshSunVfxScale();
        ApplyHdState();
        StartCoroutine(RefreshSunVfxScaleNextFrame());
    }

    void Update()
    {
        if (!SunAppearanceSettings.UseRealSun || _sunBaseRenderer == null)
            return;

        if (_sunBloomRenderer == null && _sunTransform != null)
            CacheSunBloomRenderer(_sunTransform);

        float t = Time.time;
        float n1 = Mathf.PerlinNoise(t * 0.35f, 0f);
        float n2 = Mathf.PerlinNoise(t * 1.7f, 5.2f);
        float n3 = Mathf.PerlinNoise(t * 4.5f, 11.3f);
        float raw = n1 * 0.5f + n2 * 0.35f + n3 * 0.15f;
        float blend = Mathf.Clamp01((raw - 0.5f) * 2.2f + 0.5f);

        var deep = new Color(1.2f, 0.32f, 0.02f);
        var bright = new Color(3.0f, 1.25f, 0.08f);
        var emission = Color.Lerp(deep, bright, blend);

        var mat = _sunBaseRenderer.material;
        if (mat != null && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", new Color(1f, Mathf.Lerp(0.5f, 0.8f, blend), Mathf.Lerp(0.12f, 0.32f, blend)));
        }

        if (_sunBloomRenderer != null && _sunBloomRenderer.enabled)
        {
            var bloomMat = _sunBloomRenderer.material;
            if (bloomMat != null && bloomMat.HasProperty("_EmissionColor"))
            {
                bloomMat.EnableKeyword("_EMISSION");
                var bloomDeep = new Color(0.9f, 0.35f, 0.06f);
                var bloomBright = new Color(1.8f, 0.8f, 0.18f);
                bloomMat.SetColor("_EmissionColor", Color.Lerp(bloomDeep, bloomBright, blend));
            }
        }
    }

    IEnumerator RefreshSunVfxScaleNextFrame()
    {
        yield return null;
        if (_sunTransform != null)
            CacheSunBloomRenderer(_sunTransform);
        RefreshSunVfxScale();
    }

    void BuildSunVfx(Transform sunTransform)
    {
        if (sunTransform.Find(VfxRootName) != null)
        {
            _vfxRoot = sunTransform.Find(VfxRootName).gameObject;
            _sunspotsRenderer = _vfxRoot.transform.Find(SunspotsObjectName)?.GetComponent<Renderer>();
            if (_sunspotsRenderer == null)
                _sunspotsRenderer = CreateSunspotsOverlay(_vfxRoot.transform);
            _flickerRenderer = _vfxRoot.transform.Find(FlickerObjectName)?.GetComponent<Renderer>();
            _cmeTransform = _vfxRoot.transform.Find(CmeObjectName);
            _cmeParticles = _cmeTransform ? _cmeTransform.GetComponent<ParticleSystem>() : null;
            CacheSunLight(sunTransform);
            CacheSunBloomRenderer(sunTransform);
            _built = true;
            return;
        }

        _vfxRoot = new GameObject(VfxRootName);
        _vfxRoot.transform.SetParent(sunTransform, false);
        _vfxRoot.transform.localPosition = Vector3.zero;
        _vfxRoot.transform.localRotation = Quaternion.identity;
        _vfxRoot.transform.localScale = Vector3.one;

        _sunspotsRenderer = CreateSunspotsOverlay(_vfxRoot.transform);
        _flickerRenderer = CreateFlickerOverlay(_vfxRoot.transform);
        CreateCmeLoop(_vfxRoot.transform, out _cmeTransform, out _cmeParticles);
        CacheSunLight(sunTransform);
        CacheSunBloomRenderer(sunTransform);

        _built = true;
    }

    void CacheSunBloomRenderer(Transform sunTransform)
    {
        var bloomTransform = sunTransform.Find(SunBloomObjectName);
        _sunBloomRenderer = bloomTransform != null ? bloomTransform.GetComponent<Renderer>() : null;
    }

    void CacheSunLight(Transform sunTransform)
    {
        _sunLight = sunTransform.GetComponent<Light>();
        if (_sunLight)
            _baseLightIntensity = _sunLight.intensity;
    }

    static float GetSunWorldRadius(Transform sunTransform)
    {
        if (sunTransform == null)
            return ReferenceSunWorldRadius;

        var col = sunTransform.GetComponent<SphereCollider>();
        float colliderRadius = col != null ? col.radius : 0.5f;
        float lossy = Mathf.Max(Mathf.Abs(sunTransform.lossyScale.x),
            Mathf.Max(Mathf.Abs(sunTransform.lossyScale.y), Mathf.Abs(sunTransform.lossyScale.z)));
        return colliderRadius * lossy;
    }

    void RefreshSunVfxScale()
    {
        if (_sunTransform == null)
            return;

        float worldRadius = GetSunWorldRadius(_sunTransform);
        _cmeSizeScale = Mathf.Clamp(worldRadius / ReferenceSunWorldRadius, 0.05f, 24f);

        if (_sunspotsRenderer != null)
            _sunspotsRenderer.transform.localScale = Vector3.one * SunspotsLocalScale;

        if (_flickerRenderer != null)
            _flickerRenderer.transform.localScale = Vector3.one * FlickerLocalScale;

        if (_cmeParticles != null)
        {
            var renderer = _cmeParticles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.lengthScale = CmeLengthScale * _cmeSizeScale;
                renderer.velocityScale = 0.08f * Mathf.Sqrt(_cmeSizeScale);
            }
        }
    }

    Renderer CreateSunspotsOverlay(Transform parent)
    {
        var sunspots = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sunspots.name = SunspotsObjectName;
        sunspots.transform.SetParent(parent, false);
        sunspots.transform.localPosition = Vector3.zero;
        sunspots.transform.localRotation = Quaternion.identity;
        sunspots.transform.localScale = Vector3.one * SunspotsLocalScale;

        var collider = sunspots.GetComponent<Collider>();
        if (collider)
            Destroy(collider);

        var renderer = sunspots.GetComponent<MeshRenderer>();
        var shader = Shader.Find("Custom/SunSunspots");
        if (shader == null)
        {
            Debug.LogWarning("SunCoronalVfxController: Custom/SunSunspots shader not found.");
            renderer.enabled = false;
            return renderer;
        }

        var material = new Material(shader);
        material.SetColor("_UmbraColor", new Color(0.22f, 0.18f, 0.14f, 1f));
        material.SetColor("_PenumbraColor", new Color(0.50f, 0.42f, 0.32f, 1f));
        material.SetFloat("_SpotStrength", 1f);
        material.SetFloat("_PenumbraNoise", 0.55f);
        material.SetFloat("_PenumbraGrainSpeed", 0.04f);
        material.SetFloat("_BreathingAmount", 0.015f);
        material.SetFloat("_BreathingSpeed", 0.12f);

        material.SetVector("_Spot0Dir", new Vector4(0.7f, 0.3f, 0.64f, 0f));
        material.SetVector("_Spot0Radii", new Vector4(0.045f, 0.095f, 1.35f, 0f));
        material.SetVector("_Spot1Dir", new Vector4(-0.52f, 0.58f, 0.62f, 0f));
        material.SetVector("_Spot1Radii", new Vector4(0.032f, 0.072f, 1.15f, 0f));
        material.SetVector("_Spot2Dir", new Vector4(0.18f, -0.74f, 0.64f, 0f));
        material.SetVector("_Spot2Radii", new Vector4(0.014f, 0.028f, 1f, 0f));

        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = false;
        return renderer;
    }

    Renderer CreateFlickerOverlay(Transform parent)
    {
        var flicker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flicker.name = FlickerObjectName;
        flicker.transform.SetParent(parent, false);
        flicker.transform.localPosition = Vector3.zero;
        flicker.transform.localRotation = Quaternion.identity;
        flicker.transform.localScale = Vector3.one * FlickerLocalScale;

        var collider = flicker.GetComponent<Collider>();
        if (collider)
            Destroy(collider);

        var renderer = flicker.GetComponent<MeshRenderer>();
        var shader = Shader.Find("Custom/SunSurfaceFlicker");
        if (shader == null)
        {
            Debug.LogWarning("SunCoronalVfxController: Custom/SunSurfaceFlicker shader not found.");
            renderer.enabled = false;
            return renderer;
        }

        var material = new Material(shader);
        material.SetColor("_BaseColor", new Color(1f, 0.78f, 0.32f, 0.34f));
        material.SetFloat("_FlickerIntensity", 0.2f);
        material.SetFloat("_SlowSpeed", 0.35f);
        material.SetFloat("_FastSpeed", 4.5f);
        material.SetFloat("_RimPower", 2.4f);
        material.SetFloat("_RimIntensity", 0.42f);

        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = false;
        return renderer;
    }

    void CreateCmeLoop(Transform parent, out Transform cmeTransform, out ParticleSystem cmeParticles)
    {
        var cmeObject = new GameObject(CmeObjectName);
        cmeTransform = cmeObject.transform;
        cmeTransform.SetParent(parent, false);
        cmeTransform.localPosition = Vector3.up * CmeEmitLocalRadius;
        cmeTransform.localRotation = Quaternion.identity;
        cmeTransform.localScale = Vector3.one;

        cmeParticles = cmeObject.AddComponent<ParticleSystem>();
        ConfigureCmeParticleSystem(cmeParticles);

        var renderer = cmeObject.GetComponent<ParticleSystemRenderer>();
        ConfigureCmeRenderer(renderer);
        cmeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    static void ConfigureCmeParticleSystem(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = true;
        main.prewarm = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 280;
        main.gravityModifier = 0f;
        main.duration = 12f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radiusThickness = 1f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var limitVelocity = ps.limitVelocityOverLifetime;
        limitVelocity.enabled = true;
        limitVelocity.dampen = 0.18f;
        limitVelocity.drag = 0.48f;

        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;

        var noise = ps.noise;
        noise.enabled = true;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        ApplyCmeBurstProfile(ps, CmeLoopKind.Arcing, 1f);
    }

    static void ApplyCmeBurstProfile(ParticleSystem ps, CmeLoopKind kind, float sizeScale)
    {
        float sizeMin = CmeStartSizeMin * sizeScale;
        float sizeMax = CmeStartSizeMax * sizeScale;
        float lifetimeMin = 4f;
        float lifetimeMax = 7f;
        float speedMin = 0.08f;
        float speedMax = 0.2f;
        float shapeAngle = 10f;
        float shapeArc = 24f;
        float shapeRadius = 0.028f * sizeScale;
        float velocityScale = 0.08f;
        float lengthScale = CmeLengthScale;
        float burstMin = 55;
        float burstMax = 95;
        float rollDegrees = 360f;
        float emitRadius = CmeEmitLocalRadius * sizeScale;
        Color colorA = new(1f, 0.9f, 0.52f, 1f);
        Color colorB = new(1f, 0.55f, 0.12f, 1f);
        AnimationCurve arcCurve;
        AnimationCurve sizeCurve;
        float noiseStrength = 0.05f;
        float noiseFrequency = 0.4f;
        float noiseScroll = 0.12f;
        float rotationSpeed = 12f;

        switch (kind)
        {
            case CmeLoopKind.Compact:
                lifetimeMin = 3.2f;
                lifetimeMax = 5.2f;
                speedMin = 0.1f;
                speedMax = 0.22f;
                sizeMin *= 0.92f;
                sizeMax *= 0.98f;
                shapeAngle = 7f;
                shapeArc = 16f;
                shapeRadius = 0.02f * sizeScale;
                burstMin = 40;
                burstMax = 68;
                velocityScale = 0.07f;
                lengthScale *= 0.95f;
                arcCurve = CmeArcCurve(0.18f, 0.04f, -0.1f, -0.28f);
                sizeCurve = CmeSizeCurve(0.3f, 0.95f, 0.5f);
                colorA = new Color(1f, 0.86f, 0.42f, 1f);
                colorB = new Color(1f, 0.5f, 0.1f, 1f);
                break;
            case CmeLoopKind.Prominence:
                lifetimeMin = 5.5f;
                lifetimeMax = 9f;
                speedMin = 0.05f;
                speedMax = 0.12f;
                sizeMin *= 1.28f;
                sizeMax *= 1.5f;
                shapeAngle = 14f;
                shapeArc = 38f;
                shapeRadius = 0.04f * sizeScale;
                burstMin = 28;
                burstMax = 48;
                velocityScale = 0.06f;
                lengthScale *= 1.1f;
                emitRadius = CmeEmitLocalRadius * 1.04f * sizeScale;
                arcCurve = CmeArcCurve(0.14f, 0.02f, -0.06f, -0.2f);
                sizeCurve = CmeSizeCurve(0.25f, 1.05f, 0.62f);
                colorA = new Color(1f, 0.82f, 0.38f, 1f);
                colorB = new Color(0.95f, 0.42f, 0.08f, 1f);
                noiseStrength = 0.04f;
                noiseFrequency = 0.28f;
                noiseScroll = 0.08f;
                rotationSpeed = 8f;
                break;
            case CmeLoopKind.Wispy:
                lifetimeMin = 4.5f;
                lifetimeMax = 7.5f;
                speedMin = 0.06f;
                speedMax = 0.14f;
                sizeMin *= 0.78f;
                sizeMax *= 0.92f;
                shapeAngle = 11f;
                shapeArc = 32f;
                shapeRadius = 0.034f * sizeScale;
                burstMin = 85;
                burstMax = 130;
                velocityScale = 0.09f;
                lengthScale *= 1f;
                arcCurve = CmeArcCurve(0.12f, 0.01f, -0.08f, -0.22f);
                sizeCurve = CmeSizeCurve(0.2f, 0.75f, 0.35f);
                colorA = new Color(1f, 0.92f, 0.58f, 1f);
                colorB = new Color(1f, 0.62f, 0.18f, 1f);
                noiseStrength = 0.07f;
                noiseFrequency = 0.52f;
                noiseScroll = 0.16f;
                rotationSpeed = 18f;
                break;
            default:
                lifetimeMin = 4f;
                lifetimeMax = 6.8f;
                speedMin = 0.07f;
                speedMax = 0.17f;
                shapeAngle = 9f;
                shapeArc = 26f;
                shapeRadius = 0.03f * sizeScale;
                burstMin = 58;
                burstMax = 92;
                velocityScale = 0.08f;
                sizeMin *= 1.12f;
                sizeMax *= 1.12f;
                arcCurve = CmeArcCurve(0.16f, 0.03f, -0.08f, -0.24f);
                sizeCurve = CmeSizeCurve(0.28f, 1f, 0.52f);
                break;
        }

        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.startRotation = new ParticleSystem.MinMaxCurve(-rollDegrees * Mathf.Deg2Rad, rollDegrees * Mathf.Deg2Rad);

        var shape = ps.shape;
        shape.angle = shapeAngle;
        shape.arc = shapeArc;
        shape.radius = shapeRadius;

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f, CmeLateralDriftCurve(kind));
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, arcCurve);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, CmeDepthDriftCurve(kind));

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CmeColorGradient(kind);

        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-rotationSpeed, rotationSpeed);

        var noise = ps.noise;
        noise.strength = noiseStrength;
        noise.frequency = noiseFrequency;
        noise.scrollSpeed = noiseScroll;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer)
        {
            renderer.velocityScale = velocityScale;
            renderer.lengthScale = lengthScale;
        }

        _pendingBurstMin = burstMin;
        _pendingBurstMax = burstMax;
        _pendingEmitRadius = emitRadius;
        _pendingLightPulse = kind == CmeLoopKind.Prominence ? 0.1f : kind == CmeLoopKind.Compact ? 0.05f : 0.07f;
        _pendingLightDuration = kind == CmeLoopKind.Prominence ? 0.75f : 0.55f;
    }

    static float _pendingBurstMin;
    static float _pendingBurstMax;
    static float _pendingEmitRadius;
    static float _pendingLightPulse;
    static float _pendingLightDuration;

    static AnimationCurve CmeArcCurve(float rise, float apex, float fallStart, float fallEnd)
    {
        return new AnimationCurve(
            new Keyframe(0f, rise),
            new Keyframe(0.32f, apex),
            new Keyframe(0.58f, apex * 0.65f),
            new Keyframe(0.82f, fallStart),
            new Keyframe(1f, fallEnd));
    }

    static AnimationCurve CmeSizeCurve(float start, float peak, float late)
    {
        return new AnimationCurve(
            new Keyframe(0f, start),
            new Keyframe(0.22f, peak),
            new Keyframe(0.68f, late),
            new Keyframe(1f, 0f));
    }

    static AnimationCurve CmeLateralDriftCurve(CmeLoopKind kind)
    {
        float sway = kind == CmeLoopKind.Wispy ? 0.06f : kind == CmeLoopKind.Prominence ? 0.03f : 0.045f;
        return new AnimationCurve(
            new Keyframe(0f, -sway * 0.5f),
            new Keyframe(0.45f, sway),
            new Keyframe(1f, -sway * 0.35f));
    }

    static AnimationCurve CmeDepthDriftCurve(CmeLoopKind kind)
    {
        float depth = kind == CmeLoopKind.Compact ? 0.02f : 0.035f;
        return new AnimationCurve(
            new Keyframe(0f, depth * 0.4f),
            new Keyframe(0.5f, -depth),
            new Keyframe(1f, depth * 0.25f));
    }

    static Gradient CmeColorGradient(CmeLoopKind kind)
    {
        Color start;
        Color mid;
        Color end;
        switch (kind)
        {
            case CmeLoopKind.Compact:
                start = new Color(1f, 0.88f, 0.48f);
                mid = new Color(1f, 0.5f, 0.1f);
                end = new Color(0.7f, 0.1f, 0.03f);
                break;
            case CmeLoopKind.Prominence:
                start = new Color(1f, 0.84f, 0.4f);
                mid = new Color(1f, 0.48f, 0.08f);
                end = new Color(0.65f, 0.08f, 0.02f);
                break;
            case CmeLoopKind.Wispy:
                start = new Color(1f, 0.94f, 0.62f);
                mid = new Color(1f, 0.6f, 0.16f);
                end = new Color(0.8f, 0.2f, 0.06f);
                break;
            default:
                start = new Color(1f, 0.9f, 0.52f);
                mid = new Color(1f, 0.55f, 0.12f);
                end = new Color(0.75f, 0.12f, 0.04f);
                break;
        }

        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(start, 0f),
                new GradientColorKey(mid, 0.42f),
                new GradientColorKey(end, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.1f),
                new GradientAlphaKey(0.75f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    static Texture2D GetCmeSoftParticleTexture()
    {
        if (_cachedCmeSoftTexture != null)
            return _cachedCmeSoftTexture;

        const int size = 64;
        _cachedCmeSoftTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "SunCmeLoopSoft",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float radial = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - radial);
                alpha *= alpha;
                byte a = (byte)(alpha * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }

        _cachedCmeSoftTexture.SetPixels32(pixels);
        _cachedCmeSoftTexture.Apply(false, true);
        return _cachedCmeSoftTexture;
    }

    static Material LoadCmeParticleMaterial()
    {
        if (_cachedCmeMaterial != null)
            return _cachedCmeMaterial;

        var loaded = Resources.Load<Material>(CmeMaterialResourcePath);
        if (loaded != null)
        {
            _cachedCmeMaterial = new Material(loaded);
            _cachedCmeMaterial.SetTexture("_BaseMap", GetCmeSoftParticleTexture());
            return _cachedCmeMaterial;
        }

        if (!_cmeMaterialWarningLogged)
        {
            Debug.LogWarning(
                $"SunCoronalVfxController: Resources/{CmeMaterialResourcePath} not found; CME particles may be invisible on device.");
            _cmeMaterialWarningLogged = true;
        }

        return null;
    }

    static void ConfigureCmeRenderer(ParticleSystemRenderer renderer)
    {
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.08f;
        renderer.lengthScale = CmeLengthScale;
        renderer.alignment = ParticleSystemRenderSpace.Velocity;
        renderer.freeformStretching = true;
        renderer.rotateWithStretchDirection = true;

        var material = LoadCmeParticleMaterial();
        if (material != null)
            renderer.sharedMaterial = material;

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    static void ApplyRealSunFlickerProfile(Material flickerMat)
    {
        if (flickerMat == null)
            return;

        flickerMat.SetColor("_BaseColor", new Color(1f, 0.7f, 0.25f, 0.5f));
        flickerMat.SetFloat("_FlickerIntensity", 0.45f);
        flickerMat.SetFloat("_RimIntensity", 0.75f);
        flickerMat.SetFloat("_FastSpeed", 6f);
        flickerMat.SetFloat("_SlowSpeed", 0.6f);
    }

    void SetCmeContinuousEmission(bool active)
    {
        if (_cmeParticles == null)
            return;

        var emission = _cmeParticles.emission;
        emission.rateOverTime = active ? CmeContinuousRate : 0f;
    }

    void ApplyHdState()
    {
        if (!_built || _vfxRoot == null)
            return;

        bool realSun = SunAppearanceSettings.UseRealSun;
        _vfxRoot.SetActive(realSun);

        if (!realSun)
        {
            StopCmeRoutine();
            if (_cmeParticles)
            {
                SetCmeContinuousEmission(false);
                _cmeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            RestoreSunLight();
            return;
        }

        if (_sunspotsRenderer)
            _sunspotsRenderer.enabled = true;

        if (_flickerRenderer)
        {
            _flickerRenderer.enabled = true;
            ApplyRealSunFlickerProfile(_flickerRenderer.material);
        }

        if (_cmeParticles)
        {
            SetCmeContinuousEmission(true);
            _cmeParticles.Clear(true);
            _cmeParticles.Play(true);
        }

        StartCmeRoutine();
    }

    void StartCmeRoutine()
    {
        StopCmeRoutine();
        if (_cmeParticles != null && _vfxRoot != null && _vfxRoot.activeInHierarchy)
            _cmeRoutine = StartCoroutine(CmeBurstLoop());
    }

    void StopCmeRoutine()
    {
        if (_cmeRoutine == null)
            return;

        StopCoroutine(_cmeRoutine);
        _cmeRoutine = null;
    }

    IEnumerator CmeBurstLoop()
    {
        float initialMin = CmeInitialDelayMin * CmeInitialDelayScaleRealSun;
        float initialMax = CmeInitialDelayMax * CmeInitialDelayScaleRealSun;
        yield return new WaitForSecondsRealtime(Random.Range(initialMin, initialMax));

        float intervalMin = CmeBurstIntervalMin * CmeBurstIntervalScaleRealSun;
        float intervalMax = CmeBurstIntervalMax * CmeBurstIntervalScaleRealSun;

        while (_vfxRoot != null && _vfxRoot.activeInHierarchy && SunAppearanceSettings.UseRealSun)
        {
            yield return new WaitForSecondsRealtime(Random.Range(intervalMin, intervalMax));
            if (_vfxRoot == null || !_vfxRoot.activeInHierarchy || !SunAppearanceSettings.UseRealSun)
                yield break;

            TriggerCmeBurst();
        }
    }

    void TriggerCmeBurst()
    {
        if (_cmeParticles == null || _cmeTransform == null)
            return;

        var kinds = (CmeLoopKind[])System.Enum.GetValues(typeof(CmeLoopKind));
        var kind = kinds[Random.Range(0, kinds.Length)];
        ApplyCmeBurstProfile(_cmeParticles, kind, _cmeSizeScale);

        Vector3 localNormal = Random.onUnitSphere.normalized;
        _cmeTransform.localPosition = localNormal * _pendingEmitRadius;
        var baseRotation = Quaternion.FromToRotation(Vector3.up, localNormal);
        _cmeTransform.localRotation = baseRotation * Quaternion.AngleAxis(Random.Range(0f, 360f), localNormal);

        int burstCount = Random.Range(Mathf.RoundToInt(_pendingBurstMin), Mathf.RoundToInt(_pendingBurstMax) + 1);
        _cmeParticles.Emit(burstCount);
        StartCoroutine(PulseSunLight(_pendingLightPulse, _pendingLightDuration));
    }

    IEnumerator PulseSunLight(float pulseFraction, float duration)
    {
        if (_sunLight == null)
            yield break;

        float target = _baseLightIntensity * (1f + pulseFraction);
        _sunLight.intensity = target;
        yield return new WaitForSecondsRealtime(duration);

        RestoreSunLight();
    }

    void RestoreSunLight()
    {
        if (_sunLight)
            _sunLight.intensity = _baseLightIntensity;
    }
}
