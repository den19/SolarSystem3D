using System.Collections;
using SolarSystemApp;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HD-only Sun coronal VFX: procedural surface flicker overlay and timed loop-like CME bursts.
/// </summary>
public class SunCoronalVfxController : MonoBehaviour
{
    const string LevelSceneName = "Level1";
    const string SunObjectName = "Sun";
    const string VfxRootName = "ExtraGraphicsSunCoronalVfx";
    const string FlickerObjectName = "ExtraGraphicsSunSurfaceFlicker";
    const string CmeObjectName = "SunCmeLoop";

    const float FlickerLocalScale = 1.012f;
    const float CmeEmitLocalRadius = 0.52f;
    const float LightPulseFraction = 0.08f;
    const float LightPulseDuration = 0.4f;

    static SunCoronalVfxController _instance;

    GameObject _vfxRoot;
    Renderer _flickerRenderer;
    ParticleSystem _cmeParticles;
    Transform _cmeTransform;
    Light _sunLight;
    float _baseLightIntensity = 100f;
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
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GraphicsSettings.UseExtraGraphicsChanged -= OnUseExtraGraphicsChanged;
        StopCmeRoutine();
    }

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

        if (!_built || _vfxRoot == null || _vfxRoot.transform.parent != sun.transform)
            BuildSunVfx(sun.transform);

        ApplyHdState();
    }

    void BuildSunVfx(Transform sunTransform)
    {
        if (sunTransform.Find(VfxRootName) != null)
        {
            _vfxRoot = sunTransform.Find(VfxRootName).gameObject;
            _flickerRenderer = _vfxRoot.transform.Find(FlickerObjectName)?.GetComponent<Renderer>();
            _cmeTransform = _vfxRoot.transform.Find(CmeObjectName);
            _cmeParticles = _cmeTransform ? _cmeTransform.GetComponent<ParticleSystem>() : null;
            CacheSunLight(sunTransform);
            _built = true;
            return;
        }

        _vfxRoot = new GameObject(VfxRootName);
        _vfxRoot.transform.SetParent(sunTransform, false);
        _vfxRoot.transform.localPosition = Vector3.zero;
        _vfxRoot.transform.localRotation = Quaternion.identity;
        _vfxRoot.transform.localScale = Vector3.one;

        _flickerRenderer = CreateFlickerOverlay(_vfxRoot.transform);
        CreateCmeLoop(_vfxRoot.transform, out _cmeTransform, out _cmeParticles);
        CacheSunLight(sunTransform);

        _built = true;
    }

    void CacheSunLight(Transform sunTransform)
    {
        _sunLight = sunTransform.GetComponent<Light>();
        if (_sunLight)
            _baseLightIntensity = _sunLight.intensity;
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
        material.SetColor("_BaseColor", new Color(1f, 0.72f, 0.35f, 0.12f));
        material.SetFloat("_FlickerIntensity", 0.12f);
        material.SetFloat("_SlowSpeed", 0.35f);
        material.SetFloat("_FastSpeed", 4.5f);
        material.SetFloat("_RimPower", 3.2f);
        material.SetFloat("_RimIntensity", 0.22f);

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
        main.maxParticles = 220;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.98f, 0.82f, 1f),
            new Color(1f, 0.72f, 0.28f, 1f));
        main.gravityModifier = 0f;
        main.duration = 5f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.035f;
        shape.radiusThickness = 1f;
        shape.arc = 28f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f, AnimationCurve.Linear(0f, -0.08f, 1f, 0.12f));
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.55f),
            new Keyframe(0.35f, 0.15f),
            new Keyframe(0.65f, -0.25f),
            new Keyframe(1f, -0.55f)));
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, AnimationCurve.Linear(0f, 0.04f, 1f, -0.04f));

        var limitVelocity = ps.limitVelocityOverLifetime;
        limitVelocity.enabled = true;
        limitVelocity.dampen = 0.12f;
        limitVelocity.drag = 0.35f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.75f, 0.55f),
            new Keyframe(1f, 0f)));

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.98f, 0.85f), 0f),
                new GradientColorKey(new Color(1f, 0.62f, 0.18f), 0.45f),
                new GradientColorKey(new Color(0.85f, 0.18f, 0.05f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.08f),
                new GradientAlphaKey(0.85f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-25f, 25f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 0.65f;
        noise.scrollSpeed = 0.35f;
        noise.damping = true;
    }

    static Material CreateCmeParticleMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");

        var material = new Material(shader);
        material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 2f);
        if (material.HasProperty("_BaseColorAddSubDiff"))
            material.SetColor("_BaseColorAddSubDiff", new Color(1f, 0f, 0f, 0f));
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_BLENDMODE_ADD");
        return material;
    }

    static void ConfigureCmeRenderer(ParticleSystemRenderer renderer)
    {
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.35f;
        renderer.lengthScale = 2.4f;
        renderer.alignment = ParticleSystemRenderSpace.Velocity;
        renderer.material = CreateCmeParticleMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    void ApplyHdState()
    {
        if (!_built || _vfxRoot == null)
            return;

        bool hd = GraphicsSettings.UseExtraGraphics;
        _vfxRoot.SetActive(hd);

        if (!hd)
        {
            StopCmeRoutine();
            if (_cmeParticles)
                _cmeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            RestoreSunLight();
            return;
        }

        if (_flickerRenderer)
            _flickerRenderer.enabled = true;

        if (_cmeParticles)
        {
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
        yield return new WaitForSeconds(Random.Range(1.5f, 3f));

        while (_vfxRoot != null && _vfxRoot.activeInHierarchy && GraphicsSettings.UseExtraGraphics)
        {
            yield return new WaitForSeconds(Random.Range(5f, 10f));
            if (_vfxRoot == null || !_vfxRoot.activeInHierarchy || !GraphicsSettings.UseExtraGraphics)
                yield break;

            TriggerCmeBurst();
        }
    }

    void TriggerCmeBurst()
    {
        if (_cmeParticles == null || _cmeTransform == null)
            return;

        Vector3 localNormal = Random.onUnitSphere.normalized;
        _cmeTransform.localPosition = localNormal * CmeEmitLocalRadius;
        _cmeTransform.localRotation = Quaternion.FromToRotation(Vector3.up, localNormal);

        int burstCount = Random.Range(80, 141);
        _cmeParticles.Emit(burstCount);
        StartCoroutine(PulseSunLight());
    }

    IEnumerator PulseSunLight()
    {
        if (_sunLight == null)
            yield break;

        float target = _baseLightIntensity * (1f + LightPulseFraction);
        _sunLight.intensity = target;
        yield return new WaitForSeconds(LightPulseDuration);

        RestoreSunLight();
    }

    void RestoreSunLight()
    {
        if (_sunLight)
            _sunLight.intensity = _baseLightIntensity;
    }
}
