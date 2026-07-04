using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Applies real-distance and real-size scale modes to celestial bodies in Level1.
/// </summary>
public class SolarSystemScaleController : MonoBehaviour
{
    const float MinPickWorldRadius = 0.12f;
    const float DefaultMainCamDistance = 45f;
    const float SimulationMaxCameraDistance = 600f;

    public struct OrbitSpec
    {
        public string BodyName;
        public Transform Center;
        public float Radius;
        public float PlaneHeight;
        public bool UseWorldSpace;
    }

    struct BodyBaseline
    {
        public Transform Transform;
        public Vector3 LocalPosition;
        public Vector3 LocalScale;
        public float OrbitDistance;
        public float ColliderRadius;
        public bool HasCollider;
        public string OrbitCenterName;
    }

    readonly List<BodyBaseline> _baselines = new List<BodyBaseline>();
    float _auToUnity;
    Vector3 _baselineEarthScale;
    Transform _sun;
    OrbitLinesManager _orbitLinesManager;
    CometSystemController _cometSystemController;

    void Awake()
    {
        _sun = GameObject.Find("Sun")?.transform;
        CaptureBaselines();
        ScaleSettings.UseRealDistancesChanged += OnUseRealDistancesChanged;
        ScaleSettings.UseRealSizesChanged += OnUseRealSizesChanged;
    }

    void Start()
    {
        _orbitLinesManager = GetComponent<OrbitLinesManager>();
        _cometSystemController = GetComponent<CometSystemController>();
        ApplyAll();
    }

    void OnDestroy()
    {
        ScaleSettings.UseRealDistancesChanged -= OnUseRealDistancesChanged;
        ScaleSettings.UseRealSizesChanged -= OnUseRealSizesChanged;
    }

    void OnUseRealDistancesChanged(bool enabled) => ApplyAll();

    void OnUseRealSizesChanged(bool enabled) => ApplyAll();

    void CaptureBaselines()
    {
        _baselines.Clear();

        GameObject earthGo = GameObject.Find("Earth");
        if (earthGo != null && _sun != null)
        {
            _auToUnity = Vector3.ProjectOnPlane(earthGo.transform.position - _sun.position, Vector3.up).magnitude;
            _baselineEarthScale = earthGo.transform.localScale;
        }
        else
        {
            _auToUnity = 22f;
            _baselineEarthScale = Vector3.one;
        }

        if (_auToUnity < 0.001f)
            _auToUnity = 22f;

        for (int i = 0; i < SolarSystemCatalog.Bodies.Length; i++)
        {
            SolarSystemCatalog.BodyDefinition definition = SolarSystemCatalog.Bodies[i];
            GameObject bodyGo = GameObject.Find(definition.objectName);
            if (bodyGo == null)
                continue;

            Transform bodyTransform = bodyGo.transform;
            Transform orbitCenter = ResolveOrbitCenter(definition);
            float orbitDistance = orbitCenter != null
                ? Vector3.ProjectOnPlane(bodyTransform.position - orbitCenter.position, Vector3.up).magnitude
                : 0f;

            var collider = bodyGo.GetComponent<SphereCollider>();
            _baselines.Add(new BodyBaseline
            {
                Transform = bodyTransform,
                LocalPosition = bodyTransform.localPosition,
                LocalScale = bodyTransform.localScale,
                OrbitDistance = orbitDistance,
                ColliderRadius = collider != null ? collider.radius : 0.5f,
                HasCollider = collider != null,
                OrbitCenterName = definition.orbitCenterName
            });
        }
    }

    Transform ResolveOrbitCenter(SolarSystemCatalog.BodyDefinition definition)
    {
        if (!string.IsNullOrEmpty(definition.orbitCenterName))
        {
            GameObject centerGo = GameObject.Find(definition.orbitCenterName);
            if (centerGo != null)
                return centerGo.transform;
        }

        return _sun;
    }

    void ApplyAll()
    {
        ApplyDistances();
        ApplySizes();
        ApplyColliderClamps();
        RebuildOrbitLines();
        RescaleComets();
        UpdateMainCameraLimits();
        UpdateGridExtent();
    }

    void ApplyDistances()
    {
        bool useReal = ScaleSettings.UseRealDistances;

        for (int i = 0; i < _baselines.Count; i++)
        {
            BodyBaseline baseline = _baselines[i];
            if (baseline.Transform == null)
                continue;

            if (!SolarSystemCatalog.TryGetBody(baseline.Transform.name, out SolarSystemCatalog.BodyDefinition definition))
                continue;

            if (!string.IsNullOrEmpty(definition.orbitCenterName))
            {
                ApplySatelliteDistance(baseline, definition, useReal);
                continue;
            }

            if (definition.objectName == "Sun")
                continue;

            ApplyHeliocentricDistance(baseline, definition, useReal);
        }
    }

    void ApplyHeliocentricDistance(BodyBaseline baseline, SolarSystemCatalog.BodyDefinition definition, bool useReal)
    {
        if (_sun == null)
            return;

        float targetDistance = useReal
            ? definition.orbitalRadiusAu * _auToUnity
            : baseline.OrbitDistance;

        Vector3 offset = baseline.Transform.position - _sun.position;
        offset.y = baseline.LocalPosition.y;

        if (offset.sqrMagnitude < 0.0001f)
            offset = new Vector3(targetDistance, offset.y, 0f);

        Vector3 flat = Vector3.ProjectOnPlane(offset, Vector3.up);
        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.right * targetDistance;

        Vector3 direction = flat.normalized;
        baseline.Transform.position = _sun.position + direction * targetDistance + Vector3.up * offset.y;
    }

    void ApplySatelliteDistance(BodyBaseline baseline, SolarSystemCatalog.BodyDefinition definition, bool useReal)
    {
        float targetDistance;
        if (useReal)
            targetDistance = definition.satelliteOrbitKm * _auToUnity / SolarSystemCatalog.AuKm;
        else
            targetDistance = baseline.OrbitDistance;

        if (targetDistance < 0.0001f)
            targetDistance = baseline.OrbitDistance;

        Vector3 flatDir = Vector3.ProjectOnPlane(baseline.LocalPosition, Vector3.up);
        if (flatDir.sqrMagnitude < 0.0001f)
            flatDir = Vector3.forward;

        baseline.Transform.localPosition = flatDir.normalized * targetDistance
            + Vector3.up * baseline.LocalPosition.y;
    }

    void ApplySizes()
    {
        bool useReal = ScaleSettings.UseRealSizes;

        for (int i = 0; i < _baselines.Count; i++)
        {
            BodyBaseline baseline = _baselines[i];
            if (baseline.Transform == null)
                continue;

            if (!SolarSystemCatalog.TryGetBody(baseline.Transform.name, out SolarSystemCatalog.BodyDefinition definition))
                continue;

            if (useReal)
            {
                float scaleFactor = definition.equatorialRadiusKm / SolarSystemCatalog.EarthEquatorialRadiusKm;
                baseline.Transform.localScale = _baselineEarthScale * scaleFactor;
            }
            else
            {
                baseline.Transform.localScale = baseline.LocalScale;
            }
        }
    }

    void ApplyColliderClamps()
    {
        for (int i = 0; i < _baselines.Count; i++)
        {
            BodyBaseline baseline = _baselines[i];
            if (baseline.Transform == null || !baseline.HasCollider)
                continue;

            var collider = baseline.Transform.GetComponent<SphereCollider>();
            if (collider == null)
                continue;

            if (!ScaleSettings.UseRealSizes)
            {
                collider.radius = baseline.ColliderRadius;
                continue;
            }

            float lossy = Mathf.Max(0.0001f, baseline.Transform.lossyScale.x);
            float meshWorldRadius = 0.5f * lossy;
            float colliderWorldRadius = collider.radius * lossy;
            float worldRadius = Mathf.Max(meshWorldRadius, colliderWorldRadius);

            if (worldRadius < MinPickWorldRadius)
                collider.radius = MinPickWorldRadius / lossy;
            else
                collider.radius = baseline.ColliderRadius;
        }
    }

    void UpdateGridExtent()
    {
        var grid = FindFirstObjectByType<SpacetimeGridController>();
        if (grid == null)
            return;

        if (ScaleSettings.UseRealDistances)
            grid.SetHalfExtent(Mathf.Max(120f, SolarSystemCatalog.MaxHeliocentricAu() * _auToUnity * 0.55f));
        else
            grid.SetHalfExtent(120f);
    }

    float GetOrbitRadius(BodyBaseline baseline, SolarSystemCatalog.BodyDefinition definition, bool useReal)
    {
        if (!string.IsNullOrEmpty(definition.orbitCenterName))
        {
            if (useReal)
                return definition.satelliteOrbitKm * _auToUnity / SolarSystemCatalog.AuKm;
            return baseline.OrbitDistance;
        }

        if (useReal)
            return definition.orbitalRadiusAu * _auToUnity;
        return baseline.OrbitDistance;
    }

    List<OrbitSpec> BuildOrbitSpecs()
    {
        var specs = new List<OrbitSpec>();
        bool useReal = ScaleSettings.UseRealDistances;

        for (int i = 0; i < _baselines.Count; i++)
        {
            BodyBaseline baseline = _baselines[i];
            if (baseline.Transform == null)
                continue;

            if (!SolarSystemCatalog.TryGetBody(baseline.Transform.name, out SolarSystemCatalog.BodyDefinition definition))
                continue;

            if (definition.objectName == "Sun")
                continue;

            Transform center = ResolveOrbitCenter(definition);
            if (center == null)
                continue;

            float radius = GetOrbitRadius(baseline, definition, useReal);
            if (radius <= 0.0001f)
                continue;

            bool isSatellite = !string.IsNullOrEmpty(definition.orbitCenterName);
            specs.Add(new OrbitSpec
            {
                BodyName = definition.objectName,
                Center = center,
                Radius = radius,
                PlaneHeight = baseline.LocalPosition.y,
                UseWorldSpace = !isSatellite
            });
        }

        return specs;
    }

    void RebuildOrbitLines()
    {
        if (_orbitLinesManager != null)
            _orbitLinesManager.RebuildOrbits(BuildOrbitSpecs());
    }

    void RescaleComets()
    {
        if (_cometSystemController == null)
            return;

        if (ScaleSettings.UseRealDistances)
            _cometSystemController.RescaleCometOrbits(_auToUnity);
        else
            _cometSystemController.RescaleCometOrbitsToSimulation();
    }

    void UpdateMainCameraLimits()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
            return;

        var orbitCam = mainCam.GetComponent<MobileOrbitCamera>();
        if (orbitCam == null)
            return;

        float maxDistance = SimulationMaxCameraDistance;
        if (ScaleSettings.UseRealDistances)
            maxDistance = Mathf.Max(SimulationMaxCameraDistance, SolarSystemCatalog.MaxHeliocentricAu() * _auToUnity * 1.15f);

        orbitCam.SetMaxDistance(maxDistance);

        if (ScaleSettings.UseRealDistances)
        {
            float safeDistance = Mathf.Clamp(DefaultMainCamDistance, orbitCam.minDistance, maxDistance * 0.5f);
            orbitCam.ApplyOrbitState(orbitCam.GetOrbitX(), orbitCam.GetOrbitY(), safeDistance);
        }
    }
}
