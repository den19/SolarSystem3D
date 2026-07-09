using System.Collections;
using UnityEngine;

/// <summary>
/// Cinematic camera: three orbits around a body, then sun-facing follow framing.
/// </summary>
public class BodyShowcaseCameraController : MonoBehaviour
{
    const float OrbitDurationSec = 10f;
    const float OrbitPitchDeg = 35f;
    const float OrbitRevolutions = 3f;
    const float TargetScreenWidthFraction = 0.5f;
    const int DistanceSolveIterations = 6;

    [SerializeField] float orbitDurationSec = OrbitDurationSec;
    [SerializeField] float orbitPitchDeg = OrbitPitchDeg;
    [SerializeField] float targetScreenWidthFraction = TargetScreenWidthFraction;

    MobileOrbitCamera _orbitCamera;
    Camera _camera;
    Transform _sun;
    Coroutine _showcaseRoutine;
    bool _followMode;
    Transform _followTarget;

    public bool IsShowcaseActive { get; private set; }

    void Awake()
    {
        _camera = GetComponent<Camera>();
        _orbitCamera = GetComponent<MobileOrbitCamera>();
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
        if (_sun == null)
        {
            GameObject sunGo = GameObject.Find("Sun");
            if (sunGo != null)
                _sun = sunGo.transform;
        }

        Vector3 bodyPos = target.position;
        Vector3 toSun = _sun != null ? (_sun.position - bodyPos) : Vector3.forward;
        if (toSun.sqrMagnitude < 0.0001f)
            toSun = Vector3.forward;
        toSun.Normalize();

        Vector3 up = Vector3.up;
        Vector3 side = Vector3.Cross(up, toSun);
        if (side.sqrMagnitude < 0.0001f)
            side = Vector3.right;
        side.Normalize();

        float bodyRadius = EstimateBodyRadius(target);
        float distance = SolveDistanceForScreenWidth(target, bodyPos, toSun, side, bodyRadius);
        distance = Mathf.Clamp(distance, _orbitCamera.minDistance, _orbitCamera.maxDistance);

        Vector3 cameraPos = bodyPos - toSun * distance * 0.55f + side * distance * 0.2f + up * distance * 0.35f;
        _camera.transform.position = cameraPos;
        _camera.transform.rotation = Quaternion.LookRotation((bodyPos - cameraPos).normalized, up);

        _orbitCamera.SyncOrbitFromTransform();
    }

    static float EstimateBodyRadius(Transform target)
    {
        var renderer = target.GetComponentInChildren<Renderer>();
        if (renderer != null)
            return Mathf.Max(renderer.bounds.extents.magnitude, 0.5f);

        return Mathf.Max(target.lossyScale.x, 0.5f);
    }

    float SolveDistanceForScreenWidth(Transform target, Vector3 bodyPos, Vector3 toSun, Vector3 side, float bodyRadius)
    {
        float distance = Mathf.Max(_orbitCamera.minDistance, bodyRadius * 4f);
        float desiredWidth = Screen.width * targetScreenWidthFraction;

        for (int i = 0; i < DistanceSolveIterations; i++)
        {
            Vector3 cameraPos = bodyPos - toSun * distance * 0.55f + side * distance * 0.2f + Vector3.up * distance * 0.35f;
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
}
