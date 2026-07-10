using System.Collections;
using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Cinematic camera: four orbits around a body, then follow framing
/// (sun-facing when free observation is on, contextual side view when off).
/// </summary>
public class BodyShowcaseCameraController : MonoBehaviour
{
    const float OrbitDurationSec = 20f;
    const float OrbitPitchDeg = 35f;
    const float OrbitRevolutions = 4f;
    const float TargetScreenWidthFraction = 0.5f;
    const float GuidedTargetScreenWidthFraction = 0.38f;
    const float GuidedContextViewportFraction = 0.82f;
    const float GuidedNeighborRadiusScale = 0.35f;
    const int DistanceSolveIterations = 6;

    static readonly string[] SunContextBodyNames = { "Mercury", "Venus", "Earth" };

    struct ContextBody
    {
        public Vector3 center;
        public float radius;
    }

    [SerializeField] float orbitDurationSec = OrbitDurationSec;
    [SerializeField] float orbitPitchDeg = OrbitPitchDeg;
    [SerializeField] float targetScreenWidthFraction = TargetScreenWidthFraction;

    MobileOrbitCamera _orbitCamera;
    Camera _camera;
    Transform _sun;
    Coroutine _showcaseRoutine;
    bool _followMode;
    Transform _followTarget;
    readonly List<ContextBody> _contextBodies = new List<ContextBody>();

    public bool IsShowcaseActive { get; private set; }

    void Awake()
    {
        _camera = GetComponent<Camera>();
        _orbitCamera = GetComponent<MobileOrbitCamera>();
    }

    void OnEnable()
    {
        SimulationViewSettings.UseFreeObservationChanged += OnFreeObservationChanged;
    }

    void OnDisable()
    {
        SimulationViewSettings.UseFreeObservationChanged -= OnFreeObservationChanged;
    }

    void Start()
    {
        if (_sun == null)
        {
            GameObject sunGo = GameObject.Find("Sun");
            if (sunGo != null)
                _sun = sunGo.transform;
        }
    }

    void LateUpdate()
    {
        if (!_followMode || _followTarget == null || _camera == null || !_camera.enabled)
            return;

        if (_orbitCamera != null && _orbitCamera.IsUserControlling)
        {
            StopShowcase();
            return;
        }

        ApplyFollowFraming(_followTarget);
    }

    public void StartShowcase(Transform target)
    {
        if (target == null)
            return;

        EnsureOrbitCamera();

        if (_showcaseRoutine != null)
            StopCoroutine(_showcaseRoutine);

        _showcaseRoutine = StartCoroutine(ShowcaseRoutine(target));
    }

    public void StopShowcase()
    {
        if (_showcaseRoutine != null)
        {
            StopCoroutine(_showcaseRoutine);
            _showcaseRoutine = null;
        }

        _followMode = false;
        _followTarget = null;
        IsShowcaseActive = false;

        if (_orbitCamera != null)
            _orbitCamera.SetExternalOrbitControl(false);
    }

    void OnFreeObservationChanged(bool _)
    {
        // Follow reframes on the next LateUpdate when the toggle changes mid-showcase.
    }

    void EnsureOrbitCamera()
    {
        if (_orbitCamera == null)
            _orbitCamera = GetComponent<MobileOrbitCamera>();

        if (_camera == null)
            _camera = GetComponent<Camera>();
    }

    IEnumerator ShowcaseRoutine(Transform target)
    {
        EnsureOrbitCamera();
        if (_orbitCamera == null)
            yield break;

        IsShowcaseActive = true;
        _followMode = false;
        _followTarget = target;
        _orbitCamera.SetExternalOrbitControl(true);
        _orbitCamera.target = target;

        var scaleController = FindFirstObjectByType<SolarSystemScaleController>();
        if (scaleController != null)
            scaleController.RefreshMainCameraLimits(resetDistance: true);

        float startYaw = _orbitCamera.GetOrbitX();
        float startDistance = GetDefaultDistance();
        float endYaw = startYaw + 360f * OrbitRevolutions;
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, orbitDurationSec);

        while (elapsed < duration)
        {
            if (_orbitCamera.IsUserControlling)
            {
                StopShowcase();
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float yaw = Mathf.Lerp(startYaw, endYaw, t);
            _orbitCamera.ApplyOrbitState(yaw, orbitPitchDeg, startDistance);
            yield return null;
        }

        _followMode = true;
        _showcaseRoutine = null;
    }

    float GetDefaultDistance()
    {
        if (_orbitCamera == null)
            return 8f;

        _orbitCamera.GetOrbitState(out _, out _, out float currentDistance);
        return currentDistance;
    }

    void ApplyFollowFraming(Transform target)
    {
        if (SimulationViewSettings.UseFreeObservation)
            ApplySunFacingFollowFraming(target);
        else
            ApplyGuidedFollowFraming(target);
    }

    void ApplySunFacingFollowFraming(Transform target)
    {
        EnsureSunReference();
        ComputeFramingVectors(target.position, out Vector3 bodyPos, out Vector3 toSun, out Vector3 side);

        float bodyRadius = EstimateBodyRadius(target);
        float distance = SolveDistanceForScreenWidth(
            bodyPos, toSun, side, bodyRadius, sunFacing: true, targetScreenWidthFraction);
        distance = Mathf.Clamp(distance, _orbitCamera.minDistance, _orbitCamera.maxDistance);

        Vector3 cameraPos = ComputeCameraPosition(bodyPos, toSun, side, distance, sunFacing: true);
        _camera.transform.position = cameraPos;
        _camera.transform.rotation = Quaternion.LookRotation((bodyPos - cameraPos).normalized, Vector3.up);

        _orbitCamera.SyncOrbitFromTransform();
    }

    void ApplyGuidedFollowFraming(Transform target)
    {
        EnsureSunReference();
        ComputeFramingVectors(target.position, out Vector3 bodyPos, out Vector3 toSun, out Vector3 side);

        float bodyRadius = EstimateBodyRadius(target);
        float baseDistance = SolveDistanceForScreenWidth(
            bodyPos, toSun, side, bodyRadius, sunFacing: false, GuidedTargetScreenWidthFraction);

        CollectContextBodies(target, bodyPos, bodyRadius);
        float contextDistance = SolveDistanceForContext(bodyPos, toSun, side, _contextBodies);
        float distance = Mathf.Max(baseDistance, contextDistance);
        distance = Mathf.Clamp(distance, _orbitCamera.minDistance, _orbitCamera.maxDistance);

        Vector3 cameraPos = ComputeCameraPosition(bodyPos, toSun, side, distance, sunFacing: false);
        _camera.transform.position = cameraPos;
        _camera.transform.rotation = Quaternion.LookRotation((bodyPos - cameraPos).normalized, Vector3.up);

        _orbitCamera.SyncOrbitFromTransform();
    }

    void EnsureSunReference()
    {
        if (_sun == null)
        {
            GameObject sunGo = GameObject.Find("Sun");
            if (sunGo != null)
                _sun = sunGo.transform;
        }
    }

    static void ComputeFramingVectors(Vector3 bodyPos, out Vector3 resolvedBodyPos, out Vector3 toSun, out Vector3 side)
    {
        resolvedBodyPos = bodyPos;

        GameObject sunGo = GameObject.Find("Sun");
        Transform sun = sunGo != null ? sunGo.transform : null;
        toSun = sun != null ? (sun.position - bodyPos) : Vector3.forward;
        if (toSun.sqrMagnitude < 0.0001f)
            toSun = Vector3.forward;
        toSun.Normalize();

        Vector3 up = Vector3.up;
        side = Vector3.Cross(up, toSun);
        if (side.sqrMagnitude < 0.0001f)
            side = Vector3.right;
        side.Normalize();
    }

    static Vector3 ComputeCameraPosition(Vector3 bodyPos, Vector3 toSun, Vector3 side, float distance, bool sunFacing)
    {
        Vector3 up = Vector3.up;
        if (sunFacing)
            return bodyPos - toSun * distance * 0.55f + side * distance * 0.2f + up * distance * 0.35f;

        return bodyPos + side * distance * 0.85f + up * distance * 0.3f;
    }

    static float EstimateBodyRadius(Transform target)
    {
        var renderer = target.GetComponentInChildren<Renderer>();
        if (renderer != null)
            return Mathf.Max(renderer.bounds.extents.magnitude, 0.5f);

        return Mathf.Max(target.lossyScale.x, 0.5f);
    }

    float SolveDistanceForScreenWidth(
        Vector3 bodyPos,
        Vector3 toSun,
        Vector3 side,
        float bodyRadius,
        bool sunFacing,
        float desiredScreenWidthFraction)
    {
        float distance = Mathf.Max(_orbitCamera.minDistance, bodyRadius * 4f);
        float desiredWidth = Screen.width * desiredScreenWidthFraction;

        for (int i = 0; i < DistanceSolveIterations; i++)
        {
            Vector3 cameraPos = ComputeCameraPosition(bodyPos, toSun, side, distance, sunFacing);
            _camera.transform.position = cameraPos;
            _camera.transform.rotation = Quaternion.LookRotation((bodyPos - cameraPos).normalized, Vector3.up);

            Vector3 left = bodyPos - side * bodyRadius;
            Vector3 right = bodyPos + side * bodyRadius;
            Vector3 screenLeft = _camera.WorldToScreenPoint(left);
            Vector3 screenRight = _camera.WorldToScreenPoint(right);
            float screenWidth = Mathf.Abs(screenRight.x - screenLeft.x);

            if (screenWidth < 1f)
            {
                distance *= 0.5f;
                continue;
            }

            float ratio = desiredWidth / screenWidth;
            distance *= ratio;
            distance = Mathf.Clamp(distance, _orbitCamera.minDistance, _orbitCamera.maxDistance);
        }

        return distance;
    }

    float SolveDistanceForContext(Vector3 bodyPos, Vector3 toSun, Vector3 side, List<ContextBody> contextBodies)
    {
        if (contextBodies == null || contextBodies.Count == 0)
            return _orbitCamera.minDistance;

        float distance = Mathf.Max(_orbitCamera.minDistance, EstimateBodyRadius(_followTarget) * 4f);
        float maxViewportWidth = Screen.width * GuidedContextViewportFraction;
        float maxViewportHeight = Screen.height * GuidedContextViewportFraction;
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        for (int i = 0; i < DistanceSolveIterations; i++)
        {
            Vector3 cameraPos = ComputeCameraPosition(bodyPos, toSun, side, distance, sunFacing: false);
            _camera.transform.position = cameraPos;
            _camera.transform.rotation = Quaternion.LookRotation((bodyPos - cameraPos).normalized, Vector3.up);

            Vector3 cameraRight = _camera.transform.right;
            Vector3 cameraUp = _camera.transform.up;
            GetScreenBounds(_camera, contextBodies, cameraRight, cameraUp, out float minX, out float maxX, out float minY, out float maxY);

            float boundsWidth = Mathf.Max(1f, maxX - minX);
            float boundsHeight = Mathf.Max(1f, maxY - minY);
            float boundsCenterX = (minX + maxX) * 0.5f;
            float boundsCenterY = (minY + maxY) * 0.5f;

            float widthRatio = boundsWidth / maxViewportWidth;
            float heightRatio = boundsHeight / maxViewportHeight;
            float scaleRatio = Mathf.Max(widthRatio, heightRatio, 1f);

            if (scaleRatio <= 1.02f &&
                Mathf.Abs(boundsCenterX - screenCenter.x) <= Screen.width * 0.08f &&
                Mathf.Abs(boundsCenterY - screenCenter.y) <= Screen.height * 0.08f)
                break;

            distance *= Mathf.Max(scaleRatio, 1.05f);
            distance = Mathf.Clamp(distance, _orbitCamera.minDistance, _orbitCamera.maxDistance);
        }

        return distance;
    }

    static void GetScreenBounds(
        Camera camera,
        List<ContextBody> contextBodies,
        Vector3 cameraRight,
        Vector3 cameraUp,
        out float minX,
        out float maxX,
        out float minY,
        out float maxY)
    {
        minX = float.MaxValue;
        maxX = float.MinValue;
        minY = float.MaxValue;
        maxY = float.MinValue;

        for (int i = 0; i < contextBodies.Count; i++)
        {
            ContextBody body = contextBodies[i];
            IncludeScreenBounds(camera, body.center, body.radius, cameraRight, cameraUp, ref minX, ref maxX, ref minY, ref maxY);
            IncludeScreenBounds(camera, body.center, body.radius, -cameraRight, cameraUp, ref minX, ref maxX, ref minY, ref maxY);
            IncludeScreenBounds(camera, body.center, body.radius, cameraRight, -cameraUp, ref minX, ref maxX, ref minY, ref maxY);
            IncludeScreenBounds(camera, body.center, body.radius, -cameraRight, -cameraUp, ref minX, ref maxX, ref minY, ref maxY);
        }
    }

    static void IncludeScreenBounds(
        Camera camera,
        Vector3 center,
        float radius,
        Vector3 axisA,
        Vector3 axisB,
        ref float minX,
        ref float maxX,
        ref float minY,
        ref float maxY)
    {
        if (camera == null)
            return;

        Vector3 worldPoint = center + axisA.normalized * radius + axisB.normalized * radius;
        Vector3 screenPoint = camera.WorldToScreenPoint(worldPoint);

        if (screenPoint.z <= 0f)
            return;

        minX = Mathf.Min(minX, screenPoint.x);
        maxX = Mathf.Max(maxX, screenPoint.x);
        minY = Mathf.Min(minY, screenPoint.y);
        maxY = Mathf.Max(maxY, screenPoint.y);
    }

    void CollectContextBodies(Transform target, Vector3 bodyPos, float targetRadius)
    {
        _contextBodies.Clear();
        _contextBodies.Add(new ContextBody { center = bodyPos, radius = targetRadius });

        string targetName = target.name;
        if (!SolarSystemCatalog.TryGetBody(targetName, out SolarSystemCatalog.BodyDefinition definition))
        {
            CollectNearbyCatalogBodies(bodyPos, ResolveNeighborRadius(bodyPos), targetName);
            return;
        }

        if (targetName == "Sun")
        {
            AddContextBodyByName("Sun");
            for (int i = 0; i < SunContextBodyNames.Length; i++)
                AddContextBodyByName(SunContextBodyNames[i]);
            return;
        }

        if (!string.IsNullOrEmpty(definition.orbitCenterName))
        {
            AddContextBodyByName(definition.orbitCenterName);
            for (int i = 0; i < SolarSystemCatalog.Bodies.Length; i++)
            {
                SolarSystemCatalog.BodyDefinition candidate = SolarSystemCatalog.Bodies[i];
                if (candidate.orbitCenterName == definition.orbitCenterName)
                    AddContextBodyByName(candidate.objectName);
            }

            return;
        }

        float neighborRadius = ResolveNeighborRadius(bodyPos);
        CollectNearbyCatalogBodies(bodyPos, neighborRadius, targetName);
    }

    void CollectNearbyCatalogBodies(Vector3 bodyPos, float neighborRadius, string excludeName)
    {
        float radiusSq = neighborRadius * neighborRadius;
        for (int i = 0; i < SolarSystemCatalog.Bodies.Length; i++)
        {
            string objectName = SolarSystemCatalog.Bodies[i].objectName;
            if (objectName == excludeName)
                continue;

            GameObject bodyGo = GameObject.Find(objectName);
            if (bodyGo == null || !bodyGo.activeInHierarchy)
                continue;

            Vector3 offset = bodyGo.transform.position - bodyPos;
            offset.y = 0f;
            if (offset.sqrMagnitude <= radiusSq)
                AddContextBody(bodyGo.transform);
        }
    }

    float ResolveNeighborRadius(Vector3 bodyPos)
    {
        float referenceDistance = 2f;
        if (_sun != null)
            referenceDistance = Vector3.Distance(bodyPos, _sun.position);

        return Mathf.Max(referenceDistance * GuidedNeighborRadiusScale, 2f);
    }

    void AddContextBodyByName(string objectName)
    {
        GameObject bodyGo = GameObject.Find(objectName);
        if (bodyGo == null || !bodyGo.activeInHierarchy)
            return;

        AddContextBody(bodyGo.transform);
    }

    void AddContextBody(Transform bodyTransform)
    {
        Vector3 center = bodyTransform.position;
        float radius = EstimateBodyRadius(bodyTransform);

        for (int i = 0; i < _contextBodies.Count; i++)
        {
            if ((_contextBodies[i].center - center).sqrMagnitude < 0.0001f)
                return;
        }

        _contextBodies.Add(new ContextBody { center = center, radius = radius });
    }
}
