using System.Collections;
using SolarSystemApp;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HD-only Saturn Great White Spot VFX: procedural equatorial storm that erupts at random
/// longitudes, expands to wrap the planet, then dissipates.
/// </summary>
public class SaturnGreatWhiteSpotVfxController : MonoBehaviour
{
    const string LevelSceneName = "Level1";
    const string SaturnObjectName = "Saturn";
    const string VfxRootName = "ExtraGraphicsSaturnStormVfx";
    const string StormObjectName = "ExtraGraphicsSaturnGreatWhiteSpot";

    const float StormLocalScale = 1.005f;

    const float DormantMinSeconds = 120f;
    const float DormantMaxSeconds = 300f;
    const float FormingMinSeconds = 8f;
    const float FormingMaxSeconds = 12f;
    const float ExpandingMinSeconds = 25f;
    const float ExpandingMaxSeconds = 45f;
    const float WrappedMinSeconds = 5f;
    const float WrappedMaxSeconds = 8f;
    const float DissipatingMinSeconds = 12f;
    const float DissipatingMaxSeconds = 18f;

    const float InitialLongitudeHalfExtent = 0.15f;
    const float WrappedLongitudeHalfExtent = 3.14159f;
    const float FormingLatitudeHalfExtent = 0.10f;
    const float WrappedLatitudeHalfExtent = 0.18f;

    static readonly int StormCenterLongitudeId = Shader.PropertyToID("_StormCenterLongitude");
    static readonly int StormLongitudeHalfExtentId = Shader.PropertyToID("_StormLongitudeHalfExtent");
    static readonly int StormLatitudeHalfExtentId = Shader.PropertyToID("_StormLatitudeHalfExtent");
    static readonly int SpotStrengthId = Shader.PropertyToID("_SpotStrength");
    static readonly int DissipationId = Shader.PropertyToID("_Dissipation");

    static SaturnGreatWhiteSpotVfxController _instance;

    GameObject _vfxRoot;
    Renderer _stormRenderer;
    Material _stormMaterial;
    Coroutine _stormRoutine;
    bool _built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (_instance != null)
            return;

        var host = new GameObject("SaturnGreatWhiteSpotVfxController_Persistent");
        _instance = host.AddComponent<SaturnGreatWhiteSpotVfxController>();
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
        StopStormRoutine();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == LevelSceneName)
            TrySetupSaturnVfx();
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name == LevelSceneName)
            TrySetupSaturnVfx();
    }

    void OnUseExtraGraphicsChanged(bool _)
    {
        ApplyHdState();
    }

    void TrySetupSaturnVfx()
    {
        var saturn = GameObject.Find(SaturnObjectName);
        if (!saturn)
            return;

        if (!_built || _vfxRoot == null || _vfxRoot.transform.parent != saturn.transform)
            BuildSaturnStormVfx(saturn.transform);

        ApplyHdState();
    }

    void BuildSaturnStormVfx(Transform saturnTransform)
    {
        var existingRoot = saturnTransform.Find(VfxRootName);
        if (existingRoot != null)
        {
            _vfxRoot = existingRoot.gameObject;
            _stormRenderer = _vfxRoot.transform.Find(StormObjectName)?.GetComponent<Renderer>();
            if (_stormRenderer == null)
                _stormRenderer = CreateStormOverlay(_vfxRoot.transform);
            _stormMaterial = _stormRenderer.sharedMaterial;
            _built = true;
            return;
        }

        _vfxRoot = new GameObject(VfxRootName);
        _vfxRoot.transform.SetParent(saturnTransform, false);
        _vfxRoot.transform.localPosition = Vector3.zero;
        _vfxRoot.transform.localRotation = Quaternion.identity;
        _vfxRoot.transform.localScale = Vector3.one;

        _stormRenderer = CreateStormOverlay(_vfxRoot.transform);
        _stormMaterial = _stormRenderer.sharedMaterial;
        _built = true;
    }

    Renderer CreateStormOverlay(Transform parent)
    {
        var storm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        storm.name = StormObjectName;
        storm.transform.SetParent(parent, false);
        storm.transform.localPosition = Vector3.zero;
        storm.transform.localRotation = Quaternion.identity;
        storm.transform.localScale = Vector3.one * StormLocalScale;

        var collider = storm.GetComponent<Collider>();
        if (collider)
            Destroy(collider);

        var renderer = storm.GetComponent<MeshRenderer>();
        var template = Resources.Load<Material>("PlanetGraphicsHD/SaturnGreatWhiteSpot");
        if (template == null || template.shader == null || !template.shader.isSupported)
        {
            Debug.LogWarning("SaturnGreatWhiteSpotVfxController: SaturnGreatWhiteSpot material or shader not available.");
            renderer.enabled = false;
            return renderer;
        }

        var material = new Material(template);
        ResetStormMaterial(material, 0f);

        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = false;
        return renderer;
    }

    void ApplyHdState()
    {
        if (!_built || _vfxRoot == null)
            return;

        bool hd = GraphicsSettings.UseExtraGraphics;
        _vfxRoot.SetActive(hd);

        if (!hd)
        {
            StopStormRoutine();
            if (_stormRenderer)
                _stormRenderer.enabled = false;
            if (_stormMaterial)
                ResetStormMaterial(_stormMaterial, 0f);
            return;
        }

        StartStormRoutine();
    }

    void StartStormRoutine()
    {
        StopStormRoutine();
        if (_stormRenderer != null && _stormMaterial != null && _vfxRoot != null && _vfxRoot.activeInHierarchy)
            _stormRoutine = StartCoroutine(StormLifecycleLoop());
    }

    void StopStormRoutine()
    {
        if (_stormRoutine == null)
            return;

        StopCoroutine(_stormRoutine);
        _stormRoutine = null;
    }

    IEnumerator StormLifecycleLoop()
    {
        while (_vfxRoot != null && _vfxRoot.activeInHierarchy && GraphicsSettings.UseExtraGraphics)
        {
            if (_stormRenderer)
                _stormRenderer.enabled = false;
            if (_stormMaterial)
                ResetStormMaterial(_stormMaterial, 0f);

            yield return new WaitForSeconds(Random.Range(DormantMinSeconds, DormantMaxSeconds));
            if (!IsActiveAndHd())
                yield break;

            float centerLongitude = Random.Range(0f, Mathf.PI * 2f);
            SetStormMaterial(
                centerLongitude,
                InitialLongitudeHalfExtent,
                FormingLatitudeHalfExtent,
                0f,
                0f);

            if (_stormRenderer)
                _stormRenderer.enabled = true;

            float formingDuration = Random.Range(FormingMinSeconds, FormingMaxSeconds);
            yield return AnimateStormPhase(
                formingDuration,
                InitialLongitudeHalfExtent,
                InitialLongitudeHalfExtent,
                FormingLatitudeHalfExtent,
                FormingLatitudeHalfExtent,
                0f,
                1f,
                0f,
                0f,
                centerLongitude);

            if (!IsActiveAndHd())
                yield break;

            float expandingDuration = Random.Range(ExpandingMinSeconds, ExpandingMaxSeconds);
            yield return AnimateStormPhase(
                expandingDuration,
                InitialLongitudeHalfExtent,
                WrappedLongitudeHalfExtent,
                FormingLatitudeHalfExtent,
                WrappedLatitudeHalfExtent,
                1f,
                1f,
                0f,
                0f,
                centerLongitude);

            if (!IsActiveAndHd())
                yield break;

            SetStormMaterial(
                centerLongitude,
                WrappedLongitudeHalfExtent,
                WrappedLatitudeHalfExtent,
                1f,
                0f);

            yield return new WaitForSeconds(Random.Range(WrappedMinSeconds, WrappedMaxSeconds));
            if (!IsActiveAndHd())
                yield break;

            float dissipatingDuration = Random.Range(DissipatingMinSeconds, DissipatingMaxSeconds);
            yield return AnimateStormPhase(
                dissipatingDuration,
                WrappedLongitudeHalfExtent,
                WrappedLongitudeHalfExtent,
                WrappedLatitudeHalfExtent,
                WrappedLatitudeHalfExtent,
                1f,
                0f,
                0f,
                1f,
                centerLongitude);

            if (_stormRenderer)
                _stormRenderer.enabled = false;
            if (_stormMaterial)
                ResetStormMaterial(_stormMaterial, 0f);
        }
    }

    bool IsActiveAndHd()
    {
        return _vfxRoot != null
            && _vfxRoot.activeInHierarchy
            && GraphicsSettings.UseExtraGraphics
            && _stormMaterial != null;
    }

    IEnumerator AnimateStormPhase(
        float duration,
        float fromLonHalf,
        float toLonHalf,
        float fromLatHalf,
        float toLatHalf,
        float fromStrength,
        float toStrength,
        float fromDissipation,
        float toDissipation,
        float centerLongitude)
    {
        if (duration <= 0f || _stormMaterial == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!IsActiveAndHd())
                yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            SetStormMaterial(
                centerLongitude,
                Mathf.Lerp(fromLonHalf, toLonHalf, smooth),
                Mathf.Lerp(fromLatHalf, toLatHalf, smooth),
                Mathf.Lerp(fromStrength, toStrength, smooth),
                Mathf.Lerp(fromDissipation, toDissipation, smooth));

            yield return null;
        }

        SetStormMaterial(centerLongitude, toLonHalf, toLatHalf, toStrength, toDissipation);
    }

    void SetStormMaterial(
        float centerLongitude,
        float longitudeHalfExtent,
        float latitudeHalfExtent,
        float strength,
        float dissipation)
    {
        if (_stormMaterial == null)
            return;

        _stormMaterial.SetFloat(StormCenterLongitudeId, centerLongitude);
        _stormMaterial.SetFloat(StormLongitudeHalfExtentId, longitudeHalfExtent);
        _stormMaterial.SetFloat(StormLatitudeHalfExtentId, latitudeHalfExtent);
        _stormMaterial.SetFloat(SpotStrengthId, strength);
        _stormMaterial.SetFloat(DissipationId, dissipation);
    }

    static void ResetStormMaterial(Material material, float centerLongitude)
    {
        material.SetFloat(StormCenterLongitudeId, centerLongitude);
        material.SetFloat(StormLongitudeHalfExtentId, InitialLongitudeHalfExtent);
        material.SetFloat(StormLatitudeHalfExtentId, FormingLatitudeHalfExtent);
        material.SetFloat(SpotStrengthId, 0f);
        material.SetFloat(DissipationId, 0f);
    }
}
