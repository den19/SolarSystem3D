using System.Collections.Generic;
using SolarSystemApp;
using SolarScaleMode = SolarSystemApp.ScaleMode;
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

    // True-scale presentation: sizes use catalog radius ratios softened by this exponent
    // (1 = literal proportions, lower = small bodies boosted for visibility).
    const float TrueScaleSizeExponent = 0.4f;
    const float TrueScalePlanetMinPickWorldRadius = 0.6f;
    const float TrueScaleSatelliteMinPickWorldRadius = 0.3f;
    const float BodyMeshRadius = 0.5f;

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
    Light _sunLight;
    OrbitLinesManager _orbitLinesManager;
    CometSystemController _cometSystemController;

    void Awake()
    {
        _sun = GameObject.Find("Sun")?.transform;
        if (_sun != null)
            _sunLight = _sun.GetComponent<Light>();
        CaptureBaselines();
        ScaleSettings.ModeChanged += OnScaleModeChanged;
    }

    void Start()
    {
        _orbitLinesManager = GetComponent<OrbitLinesManager>();
        _cometSystemController = GetComponent<CometSystemController>();
        ApplyAll();
    }

    void OnDestroy()
    {
        ScaleSettings.ModeChanged -= OnScaleModeChanged;
    }

    void OnScaleModeChanged(SolarScaleMode mode) => ApplyAll();

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
        UpdateSunLightRange();
        RefreshGravityGridBodies();
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
        if (ScaleSettings.Mode == SolarScaleMode.TrueScale)
        {
            float uniform = ComputeTrueScaleUniform(definition);
            return new Vector3(uniform, uniform, uniform);
        }

        if (SolarSystemLayout.TryGetEducational(definition.objectName, out SolarSystemLayout.EducationalEntry edu))
            return edu.Scale;

        return baseline.LocalScale;
    }

    /// <summary>
    /// True-scale body scale: catalog radius ratio softened by <see cref="TrueScaleSizeExponent"/>,
    /// then clamped so no body overlaps the Sun or its neighbouring orbit.
    /// </summary>
    float ComputeTrueScaleUniform(SolarSystemCatalog.BodyDefinition definition)
    {
        float earthRef = GetEarthReferenceScale();
        float ratio = SolarSystemCatalog.GetRadiusRatioToEarth(definition.objectName);
        float uniform = earthRef * Mathf.Pow(Mathf.Max(0.0001f, ratio), TrueScaleSizeExponent);

        float maxWorldRadius = GetTrueScaleMaxWorldRadius(definition);
        if (maxWorldRadius > 0f)
        {
            float worldRadius = BodyMeshRadius * uniform;
            if (worldRadius > maxWorldRadius)
                uniform = maxWorldRadius / BodyMeshRadius;
        }

        return Mathf.Max(0.0001f, uniform);
    }

    /// <summary>
    /// Safety cap on a body's world radius in true-scale mode so overlaps are impossible:
    /// Sun stays inside Mercury's perihelion, planets/moons below a fraction of their own orbit.
    /// </summary>
    float GetTrueScaleMaxWorldRadius(SolarSystemCatalog.BodyDefinition definition)
    {
        if (definition.objectName == "Sun")
        {
            if (SolarSystemCatalog.TryGetBody("Mercury", out SolarSystemCatalog.BodyDefinition mercury))
            {
                float perihelion = mercury.orbitalRadiusAu * (1f - mercury.orbitalEccentricity) * _auToUnity;
                return 0.48f * perihelion;
            }
            return 3.5f;
        }

        if (string.IsNullOrEmpty(definition.orbitCenterName))
        {
            float distance = definition.orbitalRadiusAu * _auToUnity;
            return distance > 0f ? 0.45f * distance : 0f;
        }

        float satelliteOrbit = GetBaselineOrbitDistance(definition.objectName);
        return satelliteOrbit > 0f ? 0.40f * satelliteOrbit : 0f;
    }

    float GetBaselineOrbitDistance(string bodyName)
    {
        for (int i = 0; i < _baselines.Count; i++)
        {
            if (_baselines[i].Transform != null && _baselines[i].Transform.name == bodyName)
                return _baselines[i].OrbitDistance;
        }

        return 0f;
    }

    float GetEarthReferenceScale()
    {
        for (int i = 0; i < _baselines.Count; i++)
        {
            BodyBaseline baseline = _baselines[i];
            if (baseline.Transform != null && baseline.Transform.name == "Earth")
                return Mathf.Max(0.0001f, baseline.LocalScale.x);
        }

        return 1f;
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
            float meshWorldRadius = BodyMeshRadius * lossy;
            float colliderWorldRadius = collider.radius * lossy;
            float worldRadius = Mathf.Max(meshWorldRadius, colliderWorldRadius);

            float minPick = GetMinPickWorldRadius(baseline.Transform.name);
            if (worldRadius < minPick)
                collider.radius = minPick / lossy;
        }
    }

    float GetMinPickWorldRadius(string bodyName)
    {
        if (ScaleSettings.Mode != SolarScaleMode.TrueScale)
            return MinPickWorldRadius;

        if (SolarSystemCatalog.TryGetBody(bodyName, out SolarSystemCatalog.BodyDefinition definition)
            && !string.IsNullOrEmpty(definition.orbitCenterName))
            return TrueScaleSatelliteMinPickWorldRadius;

        return TrueScalePlanetMinPickWorldRadius;
    }

    void UpdateGridExtent()
    {
        var grid = FindFirstObjectByType<SpacetimeGridController>();
        if (grid == null)
            return;

        if (ScaleSettings.UseRealDistances)
        {
            float outerPlanetDistance = SolarSystemCatalog.MaxHeliocentricAu() * _auToUnity;
            grid.SetHalfExtent(Mathf.Max(120f, outerPlanetDistance * 1.08f));
        }
        else if (SolarSystemLayout.TryGetEducational("Neptune", out SolarSystemLayout.EducationalEntry neptune))
        {
            grid.SetHalfExtent(Mathf.Max(120f, neptune.OrbitDistance * 1.12f));
        }
        else
        {
            grid.SetHalfExtent(120f);
        }
    }

    void RefreshGravityGridBodies()
    {
        var grid = FindFirstObjectByType<SpacetimeGridController>();
        if (grid != null)
            grid.RefreshBodies();
    }

    void UpdateSunLightRange()
    {
        if (_sunLight == null && _sun != null)
            _sunLight = _sun.GetComponent<Light>();
        if (_sunLight == null)
            return;

        if (ScaleSettings.UseRealDistances)
        {
            float outerReach = SolarSystemCatalog.MaxHeliocentricAu() * _auToUnity * 1.2f;
            _sunLight.range = Mathf.Max(500f, outerReach);
        }
        else
        {
            _sunLight.range = 500f;
        }
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

        float minOrbit = GetMinimumSatelliteOrbitLocal(definition.orbitCenterName);
        float innermost = GetInnermostSatelliteBaseline(definition.orbitCenterName);
        if (innermost > 0.0001f && innermost < minOrbit)
            return baseline.OrbitDistance * (minOrbit / innermost);

        return baseline.OrbitDistance;
    }

    /// <summary>
    /// Earth–Moon educational clearance ratio used as the true-scale satellite orbit template.
    /// </summary>
    float GetEarthMoonClearanceRatio()
    {
        if (SolarSystemLayout.TryGetEducational("Moon", out SolarSystemLayout.EducationalEntry moon)
            && SolarSystemLayout.TryGetEducational("Earth", out SolarSystemLayout.EducationalEntry earth))
        {
            float earthRadius = BodyMeshRadius * earth.Scale.x;
            if (earthRadius > 0.0001f)
                return moon.OrbitDistance / earthRadius;
        }

        return 2.78f;
    }

    float GetTrueScaleParentWorldRadius(string parentName)
    {
        if (!SolarSystemCatalog.TryGetBody(parentName, out SolarSystemCatalog.BodyDefinition parentDef))
            return BodyMeshRadius;

        return BodyMeshRadius * ComputeTrueScaleUniform(parentDef);
    }

    float GetInnermostSatelliteBaseline(string parentName)
    {
        float min = float.MaxValue;
        for (int i = 0; i < _baselines.Count; i++)
        {
            BodyBaseline entry = _baselines[i];
            if (entry.OrbitCenterName != parentName)
                continue;

            if (entry.OrbitDistance < min)
                min = entry.OrbitDistance;
        }

        return min < float.MaxValue ? min : 0f;
    }

    float GetMinimumSatelliteOrbitLocal(string orbitCenterName)
    {
        if (string.IsNullOrEmpty(orbitCenterName))
            return BodyMeshRadius * 1.08f;

        return GetTrueScaleParentWorldRadius(orbitCenterName) * GetEarthMoonClearanceRatio();
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
            maxDistance = Mathf.Max(SimulationMaxCameraDistance, SolarSystemCatalog.MaxHeliocentricAu() * _auToUnity * 1.25f);

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
