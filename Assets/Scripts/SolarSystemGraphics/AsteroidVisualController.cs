using UnityEngine;

/// <summary>
/// Swaps the shared asteroid-ball material by heliocentric distance (Cold / Warm / Hot).
/// </summary>
public class AsteroidVisualController : MonoBehaviour
{
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] float semiMajorAxisAu = 2.5f;
    [SerializeField] float eccentricity = 0.1f;

    Transform _sun;
    float _auToUnity = 22f;
    AsteroidOrbitController _orbit;
    AsteroidHeatVariant _applied = (AsteroidHeatVariant)(-1);

    public void Initialize(AsteroidCatalog.AsteroidDefinition definition, Transform sun, float auToUnity)
    {
        semiMajorAxisAu = definition.semiMajorAxisAu;
        eccentricity = definition.eccentricity;
        _sun = sun;
        _auToUnity = Mathf.Max(0.001f, auToUnity);
        _orbit = GetComponent<AsteroidOrbitController>();

        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>(true);

        ApplyVariant(AsteroidGraphicsLibrary.Classify(GetHeliocentricDistanceAu()), force: true);
    }

    void LateUpdate()
    {
        if (_orbit == null)
            _orbit = GetComponent<AsteroidOrbitController>();

        if (_sun == null)
        {
            var sunGo = GameObject.Find("Sun");
            if (sunGo != null)
                _sun = sunGo.transform;
            if (_sun == null)
                return;
        }

        var scaleController = FindFirstObjectByType<SolarSystemScaleController>();
        if (scaleController != null)
            _auToUnity = scaleController.AuToUnity;

        ApplyVariant(AsteroidGraphicsLibrary.Classify(GetHeliocentricDistanceAu()), force: false);
    }

    float GetHeliocentricDistanceAu()
    {
        if (_orbit != null)
            return _orbit.GetHeliocentricDistanceAu();

        if (_sun == null)
            return semiMajorAxisAu;

        return Vector3.Distance(transform.position, _sun.position) / _auToUnity;
    }

    void ApplyVariant(AsteroidHeatVariant variant, bool force)
    {
        if (!force && variant == _applied)
            return;

        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>(true);
        if (meshRenderer == null)
            return;

        Material mat = AsteroidGraphicsLibrary.LoadVariantMaterial(variant);
        if (mat == null)
            return;

        meshRenderer.sharedMaterial = mat;
        _applied = variant;
    }
}
