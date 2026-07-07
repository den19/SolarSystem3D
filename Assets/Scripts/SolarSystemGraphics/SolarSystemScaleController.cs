using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Level1 uses scene-authored real proportions; toggles switch to the compressed educational layout.
/// </summary>
public class SolarSystemScaleController : MonoBehaviour
{
    const float MinPickWorldRadius = 0.3f;
    const float SimulationMaxCameraDistance = 600f;
    const float MainCamMinDistanceScale = 2.5f;
    const float MainCamDefaultDistanceScale = 6.5f;
    const float AbsoluteMinMainCamDistance = 0.4f;

    public struct OrbitSpec
    {
        public string BodyName;
        public Transform Center;
        public float Radius;
        public float PlaneHeight;
        public bool UseWorldSpace;
        public bool UseEllipse;
        public float SemiMajorAxis;
        public float Eccentricity;
        public float InclinationDeg;
        public float PhaseOffsetRad;
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

    public float AuToUnity => _auToUnity;
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

    public void ApplyDistancesOnly()
    {
        ApplyDistances();
    }

    public float GetOrbitSemiMajorAxis(string bodyName)
    {
        for (int i = 0; i < _baselines.Count; i++)
        {
            BodyBaseline baseline = _baselines[i];
            if (baseline.Transform == null || baseline.Transform.name != bodyName)
                continue;

            if (!SolarSystemCatalog.TryGetBody(bodyName, out SolarSystemCatalog.BodyDefinition definition))
                return 0f;

            return GetOrbitRadius(baseline, definition);
        }

        return 0f;
    }

    void CaptureBaselines()
    {
        _baselines.Clear();

        GameObject earthGo = GameObject.Find("Earth");
        if (earthGo != null && _sun != null)
            _auToUnity = Vector3.ProjectOnPlane(earthGo.transform.position - _sun.position, Vector3.up).magnitude;
        else
            _auToUnity = SolarSystemLayout.DefaultAuToUnity;

        if (_auToUnity < 0.001f)
            _auToUnity = SolarSystemLayout.DefaultAuToUnity;

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

            if (!string.IsNullOrEmpty(definition.orbitCenterName))
                orbitDistance = Vector3.ProjectOnPlane(bodyTransform.localPosition, Vector3.up).magnitude;

            var collider = bodyGo.GetComponent<SphereCollider>();
            _baselines.Add(new BodyBaseline
            {
                Transform = bodyTransform,
                LocalPosition = bodyTransform.localPosition,
                LocalScale = bodyTransform.localScale,
                OrbitDistance = orbitDistance,
                ColliderRadius = collider != null ? collider.radius : SolarSystemLayout.StandardColliderRadius,
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
        if (OrbitSettings.UseRealOrbits)
            return;

        for (int i = 0; i < _baselines.Count; i++)
        {
            BodyBaseline baseline = _baselines[i];
            if (baseline.Transform == null)
                continue;

            if (!SolarSystemCatalog.TryGetBody(baseline.Transform.name, out SolarSystemCatalog.BodyDefinition definition))
                continue;

            if (!string.IsNullOrEmpty(definition.orbitCenterName))
            {
                ApplySatelliteDistance(baseline, definition);
                continue;
            }

            if (definition.objectName == "Sun")
                continue;

            ApplyHeliocentricDistance(baseline, definition);
        }
    }

    void ApplyHeliocentricDistance(BodyBaseline baseline, SolarSystemCatalog.BodyDefinition definition)
    {
        if (_sun == null)
            return;

        float targetDistance = GetHeliocentricDistance(baseline, definition);

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

    void ApplySatelliteDistance(BodyBaseline baseline, SolarSystemCatalog.BodyDefinition definition)
    {
        float targetDistance = GetSatelliteDistance(baseline, definition);
        if (targetDistance < 0.0001f)
            targetDistance = baseline.OrbitDistance;

        if (ScaleSettings.UseRealDistances)
        {
            Vector3 flatDir = Vector3.ProjectOnPlane(baseline.LocalPosition, Vector3.up);
            if (flatDir.sqrMagnitude < 0.0001f)
                flatDir = Vector3.forward;

            baseline.Transform.localPosition = flatDir.normalized * targetDistance
                + Vector3.up * baseline.LocalPosition.y;
            return;
        }

        if (SolarSystemLayout.TryGetEducational(definition.objectName, out SolarSystemLayout.EducationalEntry edu))
        {
            baseline.Transform.localPosition = edu.SatelliteLocalPosition;
            return;
        }

        Vector3 direction = Vector3.ProjectOnPlane(baseline.LocalPosition, Vector3.up);
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;

        baseline.Transform.localPosition = direction.normalized * targetDistance
            + Vector3.up * baseline.LocalPosition.y;
    }

    void ApplySizes()
    {
        for (int i = 0; i < _baselines.Count; i++)
        {
            BodyBaseline baseline = _baselines[i];
            if (baseline.Transform == null)
                continue;

            if (!SolarSystemCatalog.TryGetBody(baseline.Transform.name, out SolarSystemCatalog.BodyDefinition definition))
                continue;

            baseline.Transform.localScale = GetBodyScale(baseline, definition);
        }
    }

    Vector3 GetBodyScale(BodyBaseline baseline, SolarSystemCatalog.BodyDefinition definition)
    {
        if (ScaleSettings.UseRealSizes)
            return baseline.LocalScale;

        if (SolarSystemLayout.TryGetEducational(definition.objectName, out SolarSystemLayout.EducationalEntry edu))
            return edu.Scale;

        return baseline.LocalScale;
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

            if (!ScaleSettings.UseRealSizes
                && SolarSystemLayout.TryGetEducational(baseline.Transform.name, out SolarSystemLayout.EducationalEntry edu))
            {
                collider.radius = edu.PickColliderRadius;
            }
            else
            {
                collider.radius = baseline.ColliderRadius;
            }

            float lossy = Mathf.Max(0.0001f, baseline.Transform.lossyScale.x);
            float meshWorldRadius = 0.5f * lossy;
            float colliderWorldRadius = collider.radius * lossy;
            float worldRadius = Mathf.Max(meshWorldRadius, colliderWorldRadius);

            if (worldRadius < MinPickWorldRadius)
                collider.radius = MinPickWorldRadius / lossy;
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

    float GetHeliocentricDistance(BodyBaseline baseline, SolarSystemCatalog.BodyDefinition definition)
    {
        if (ScaleSettings.UseRealDistances)
            return baseline.OrbitDistance;

        if (SolarSystemLayout.TryGetEducational(definition.objectName, out SolarSystemLayout.EducationalEntry edu))
            return edu.OrbitDistance;

        return baseline.OrbitDistance;
    }

    float GetSatelliteDistance(BodyBaseline baseline, SolarSystemCatalog.BodyDefinition definition)
    {
        if (!ScaleSettings.UseRealDistances)
        {
            if (SolarSystemLayout.TryGetEducational(definition.objectName, out SolarSystemLayout.EducationalEntry edu))
                return edu.OrbitDistance;

            return baseline.OrbitDistance;
        }

        float distance = baseline.OrbitDistance;
        float minOrbit = GetMinimumSatelliteOrbitLocal(definition.orbitCenterName);
        if (distance < minOrbit
            && SolarSystemLayout.TryGetEducational(definition.objectName, out SolarSystemLayout.EducationalEntry fallback))
            distance = fallback.OrbitDistance;

        return distance;
    }

    static float GetMinimumSatelliteOrbitLocal(string orbitCenterName)
    {
        const float meshRadius = 0.5f;
        const float margin = 1.08f;
        return meshRadius * margin;
    }

    float GetOrbitRadius(BodyBaseline baseline, SolarSystemCatalog.BodyDefinition definition)
    {
        return !string.IsNullOrEmpty(definition.orbitCenterName)
            ? GetSatelliteDistance(baseline, definition)
            : GetHeliocentricDistance(baseline, definition);
    }

    List<OrbitSpec> BuildOrbitSpecs()
    {
        var specs = new List<OrbitSpec>();
        bool useRealOrbits = OrbitSettings.UseRealOrbits;

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

            float radius = GetOrbitRadius(baseline, definition);
            if (radius <= 0.0001f)
                continue;

            bool isSatellite = !string.IsNullOrEmpty(definition.orbitCenterName);
            float planeHeight = baseline.LocalPosition.y;

            var spec = new OrbitSpec
            {
                BodyName = definition.objectName,
                Center = center,
                Radius = radius,
                PlaneHeight = planeHeight,
                UseWorldSpace = !isSatellite,
                UseEllipse = useRealOrbits,
                SemiMajorAxis = radius,
                Eccentricity = definition.orbitalEccentricity,
                InclinationDeg = definition.orbitalInclinationDeg
            };

            if (useRealOrbits)
            {
                spec.PhaseOffsetRad = ComputeOrbitPhase(
                    baseline.Transform,
                    center,
                    planeHeight,
                    !isSatellite,
                    radius,
                    definition.orbitalEccentricity,
                    definition.orbitalInclinationDeg);
            }

            specs.Add(spec);
        }

        return specs;
    }

    static float ComputeOrbitPhase(
        Transform body,
        Transform center,
        float planeHeight,
        bool useWorldSpace,
        float semiMajorAxis,
        float eccentricity,
        float inclinationDeg)
    {
        Vector3 planeOffset = Vector3.up * planeHeight;
        Vector3 offset = useWorldSpace
            ? body.position - center.position - planeOffset
            : body.localPosition - planeOffset;

        return OrbitLineUtility.ComputePhaseFromOffset(offset, semiMajorAxis, eccentricity, inclinationDeg);
    }

    void RebuildOrbitLines()
    {
        if (_orbitLinesManager != null)
            _orbitLinesManager.RebuildOrbits(BuildOrbitSpecs());
    }

    public void RebuildOrbitLinesOnly()
    {
        RebuildOrbitLines();
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

    public void RefreshMainCameraLimits(bool resetDistance = false)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
            return;

        var orbitCam = mainCam.GetComponent<MobileOrbitCamera>();
        if (orbitCam == null)
            return;

        Transform bodyTransform = ResolveMainCameraTarget();
        float bodyRadius = GetBodyWorldRadius(bodyTransform);
        float minDistance = Mathf.Max(AbsoluteMinMainCamDistance, bodyRadius * MainCamMinDistanceScale);

        float maxDistance = SimulationMaxCameraDistance;
        if (ScaleSettings.UseRealDistances)
            maxDistance = Mathf.Max(SimulationMaxCameraDistance, SolarSystemCatalog.MaxHeliocentricAu() * _auToUnity * 1.15f);

        float defaultDistance = Mathf.Clamp(
            bodyRadius * MainCamDefaultDistanceScale,
            minDistance * 1.5f,
            maxDistance * 0.5f);

        orbitCam.SetMinDistance(minDistance);
        orbitCam.SetMaxDistance(maxDistance);

        if (resetDistance)
            orbitCam.ApplyOrbitState(orbitCam.GetOrbitX(), orbitCam.GetOrbitY(), defaultDistance);
    }

    void UpdateMainCameraLimits()
    {
        RefreshMainCameraLimits(resetDistance: ScaleSettings.UseRealDistances);
    }

    Transform ResolveMainCameraTarget()
    {
        var lookAt = FindFirstObjectByType<LookAtTarget>();
        if (lookAt != null && lookAt.currentTarget != null)
            return lookAt.currentTarget.transform;

        if (_sun != null)
            return _sun;

        return transform;
    }

    static float GetBodyWorldRadius(Transform body)
    {
        if (body == null)
            return 0.5f;

        var col = body.GetComponent<SphereCollider>();
        float r = col != null ? col.radius : SolarSystemLayout.StandardColliderRadius;
        Vector3 ls = body.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
        return r * maxScale;
    }
}
