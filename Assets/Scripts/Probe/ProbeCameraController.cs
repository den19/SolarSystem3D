using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Main-camera chase / cockpit override. Does not write the probe into LookAtTarget.currentTarget.
/// Chase: auto-behind until the user orbits; then keep the angle (default) or ease back behind.
/// </summary>
[DefaultExecutionOrder(250)]
public class ProbeCameraController : MonoBehaviour
{
    public static bool SuppressBodyPicking { get; private set; }

    const float ChaseBackDistance = 4.5f;
    const float ChaseUpDistance = 1.4f;
    const float ChaseFollowSharpness = 8f;
    const float ChaseInspectMinScale = 1.6f;
    const float ChaseInspectMaxDistance = 40f;
    const float ChaseInspectMaxScale = 8f;
    const float ReturnAngleEpsilon = 1.5f;
    const float ReturnDistanceEpsilon = 0.15f;

    MobileOrbitCamera _orbit;
    Camera _main;
    LookAtTarget _lookAt;
    bool _overrideActive;
    GameObject _focusBeforeProbe;

    bool _inspecting;
    bool _returningBehind;
    bool _wasUserControlling;
    bool _chaseLimitsApplied;
    float _savedMinDistance;
    float _savedMaxDistance;
    bool _probeSelfHiddenFromMain;
    int _savedMainCullingMask;

    public static ProbeCameraController EnsureOnHost(GameObject host)
    {
        if (host == null)
            return null;

        var controller = host.GetComponent<ProbeCameraController>();
        if (controller == null)
            controller = host.AddComponent<ProbeCameraController>();
        return controller;
    }

    void Awake()
    {
        _main = Camera.main;
        if (_main != null)
            _orbit = _main.GetComponent<MobileOrbitCamera>();
        _lookAt = FindFirstObjectByType<LookAtTarget>();
    }

    void OnEnable()
    {
        ProbeSettings.KeepChaseInspectAngleChanged += OnKeepChaseInspectAngleChanged;
    }

    void OnDisable()
    {
        ProbeSettings.KeepChaseInspectAngleChanged -= OnKeepChaseInspectAngleChanged;
    }

    void LateUpdate()
    {
        Tick(ProbeSystemController.Instance);
    }

    public void SnapChase(ProbeCraft craft)
    {
        if (craft == null)
            return;

        EnsureMainCamera();
        if (_main == null)
            return;

        if (!_overrideActive)
            BeginOverride();

        EndInspect(keepLock: true);
        ApplyChaseOrbitSetup(craft);
        _orbit?.SetExternalOrbitControl(true);

        Vector3 velocity = craft.Velocity.sqrMagnitude > 1e-5f ? craft.Velocity.normalized : craft.transform.forward;
        Vector3 chasePos = BehindWorldPosition(craft.transform.position, velocity);
        _main.transform.position = chasePos;
        _main.transform.rotation = Quaternion.LookRotation(craft.transform.position - _main.transform.position, Vector3.up);
    }

    public void Tick(ProbeSystemController system)
    {
        EnsureMainCamera();

        bool flying = system != null && system.IsFlying;
        ProbeCameraMode mode = ProbeSettings.CameraMode;
        bool wantOverride = flying && (mode == ProbeCameraMode.Chase || mode == ProbeCameraMode.Cockpit);
        SuppressBodyPicking = wantOverride;

        if (!wantOverride)
        {
            if (_overrideActive)
                ExitProbeCamera(restoreFocus: true);
            return;
        }

        ProbeCraft craft = system.Craft;
        if (craft == null)
            return;

        if (!_overrideActive)
            BeginOverride();

        Vector3 velocity = craft.Velocity.sqrMagnitude > 1e-5f ? craft.Velocity.normalized : craft.transform.forward;

        if (mode == ProbeCameraMode.Cockpit)
        {
            ClearChaseOrbitState();
            ApplyMainProbeSelfCulling(true);
            _main.transform.position = ProbeCameraOffsets.CockpitWorldPosition(craft, velocity);
            _main.transform.rotation = ProbeCameraOffsets.CockpitWorldRotation(craft, velocity);
            _orbit?.SetExternalOrbitControl(true);
            return;
        }

        ApplyMainProbeSelfCulling(false);
        ApplyChaseOrbitSetup(craft);
        TickChase(craft, velocity);
    }

    void TickChase(ProbeCraft craft, Vector3 velocity)
    {
        bool userControlling = _orbit != null && _orbit.IsUserControlling;

        if (!_inspecting)
        {
            if (userControlling)
            {
                BeginInspect(craft);
                return;
            }

            ApplyAutoChase(craft, velocity);
            return;
        }

        if (userControlling)
        {
            _returningBehind = false;
            _wasUserControlling = true;
            return;
        }

        if (_wasUserControlling)
        {
            _wasUserControlling = false;
            if (!ProbeSettings.KeepChaseInspectAngle)
                _returningBehind = true;
        }

        if (_returningBehind)
        {
            TickReturnBehind(craft, velocity);
            return;
        }

        // Keep-angle inspect: orbit must own the camera so locked follow tracks the probe.
        _orbit?.SetExternalOrbitControl(false);
    }

    void BeginInspect(ProbeCraft craft)
    {
        _inspecting = true;
        _returningBehind = false;
        _wasUserControlling = true;
        ApplyChaseOrbitSetup(craft);
        if (_orbit == null)
            return;

        _orbit.SetExternalOrbitControl(false);
        _orbit.SyncOrbitFromTransform();
        _orbit.ApplyOrbitInputFromCurrentFrame();
    }

    void EndInspect(bool keepLock)
    {
        _inspecting = false;
        _returningBehind = false;
        _wasUserControlling = false;
        if (!keepLock)
            _orbit?.SetLockedFollowTarget(null, false);
    }

    void ApplyAutoChase(ProbeCraft craft, Vector3 velocity)
    {
        _orbit?.SetExternalOrbitControl(true);
        Vector3 chasePos = BehindWorldPosition(craft.transform.position, velocity);
        float t = 1f - Mathf.Exp(-ChaseFollowSharpness * Time.unscaledDeltaTime);
        _main.transform.position = Vector3.Lerp(_main.transform.position, chasePos, t);
        _main.transform.rotation = Quaternion.LookRotation(craft.transform.position - _main.transform.position, Vector3.up);
    }

    void TickReturnBehind(ProbeCraft craft, Vector3 velocity)
    {
        if (_orbit == null)
        {
            ApplyAutoChase(craft, velocity);
            EndInspect(keepLock: true);
            return;
        }

        GetBehindOrbit(craft.transform.position, velocity, out float behindX, out float behindY, out float behindDistance);
        _orbit.GetOrbitState(out float x, out float y, out float distance);

        float t = 1f - Mathf.Exp(-ChaseFollowSharpness * Time.unscaledDeltaTime);
        float nextX = Mathf.LerpAngle(x, behindX, t);
        float nextY = Mathf.Lerp(y, behindY, t);
        float nextDistance = Mathf.Lerp(distance, behindDistance, t);
        _orbit.ApplyOrbitState(nextX, nextY, nextDistance);

        bool close =
            Mathf.Abs(Mathf.DeltaAngle(nextX, behindX)) <= ReturnAngleEpsilon &&
            Mathf.Abs(nextY - behindY) <= ReturnAngleEpsilon &&
            Mathf.Abs(nextDistance - behindDistance) <= ReturnDistanceEpsilon;
        if (close)
        {
            EndInspect(keepLock: true);
            _orbit.SetExternalOrbitControl(true);
        }
    }

    void ApplyChaseOrbitSetup(ProbeCraft craft)
    {
        if (_orbit == null || craft == null)
            return;

        _orbit.SetLockedFollowTarget(craft.transform, true);
        if (_chaseLimitsApplied)
            return;

        _savedMinDistance = _orbit.minDistance;
        _savedMaxDistance = _orbit.maxDistance;
        float scale = Mathf.Max(0.01f, craft.transform.lossyScale.x);
        float min = scale * ChaseInspectMinScale;
        float max = Mathf.Max(ChaseInspectMaxDistance, scale * ChaseInspectMaxScale);
        _orbit.SetDistanceLimits(min, max);
        _chaseLimitsApplied = true;
    }

    void ClearChaseOrbitState()
    {
        EndInspect(keepLock: false);
        RestoreOrbitLimits();
    }

    void RestoreOrbitLimits()
    {
        if (!_chaseLimitsApplied || _orbit == null)
            return;

        _orbit.SetDistanceLimits(_savedMinDistance, _savedMaxDistance);
        _chaseLimitsApplied = false;
    }

    void OnKeepChaseInspectAngleChanged(bool keep)
    {
        if (!_inspecting)
            return;

        if (keep)
            _returningBehind = false;
        else if (_orbit == null || !_orbit.IsUserControlling)
            _returningBehind = true;
    }

    static Vector3 BehindWorldPosition(Vector3 craftPosition, Vector3 velocity)
    {
        return craftPosition - velocity * ChaseBackDistance + Vector3.up * ChaseUpDistance;
    }

    void GetBehindOrbit(Vector3 craftPosition, Vector3 velocity, out float yaw, out float pitch, out float dist)
    {
        Vector3 offset = BehindWorldPosition(craftPosition, velocity) - craftPosition;
        dist = offset.magnitude;
        if (dist < 0.001f)
        {
            yaw = 0f;
            pitch = 0f;
            dist = ChaseBackDistance;
            return;
        }

        Vector3 euler = Quaternion.LookRotation(-offset.normalized, Vector3.up).eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
        if (pitch > 180f)
            pitch -= 360f;
        if (_orbit != null)
        {
            pitch = Mathf.Clamp(pitch, _orbit.yMinLimit, _orbit.yMaxLimit);
            dist = Mathf.Clamp(dist, _orbit.minDistance, _orbit.maxDistance);
        }
    }

    void BeginOverride()
    {
        _overrideActive = true;
        if (_lookAt != null)
        {
            _focusBeforeProbe = _lookAt.currentTarget;
            _lookAt.TurnOffAllDetailCameras();
            if (_lookAt.mainCamera != null)
                _lookAt.TurnOnMainCamera();
        }

        ProbePrefabFactory.EnsureMinimapBlipCameraCulling();

        var showcase = _main != null ? _main.GetComponent<BodyShowcaseCameraController>() : null;
        showcase?.StopShowcase();
        _orbit?.SetExternalOrbitControl(true);
    }

    public void ExitProbeCamera(bool restoreFocus)
    {
        SuppressBodyPicking = false;
        if (!_overrideActive)
            return;

        _overrideActive = false;
        ApplyMainProbeSelfCulling(false);
        ClearChaseOrbitState();
        _orbit?.SetExternalOrbitControl(false);

        if (!restoreFocus || _lookAt == null)
            return;

        GameObject focus = _focusBeforeProbe != null ? _focusBeforeProbe : _lookAt.ResolveObservationTarget();
        if (focus != null && !focus.name.StartsWith("Comet_"))
            _lookAt.FocusPlanet(focus.name, useDetailCamera: false, showDescription: false);
        else if (focus != null)
            _lookAt.FocusComet(focus, showDescription: false);

        _focusBeforeProbe = null;
    }

    void EnsureMainCamera()
    {
        if (_main == null)
        {
            _main = Camera.main;
            if (_main != null)
                _orbit = _main.GetComponent<MobileOrbitCamera>();
        }
    }

    void ApplyMainProbeSelfCulling(bool hide)
    {
        EnsureMainCamera();
        if (_main == null)
            return;

        if (hide)
        {
            if (_probeSelfHiddenFromMain)
                return;

            _savedMainCullingMask = _main.cullingMask;
            _main.cullingMask &= ~ProbeCameraOffsets.ProbeSelfLayerBit;
            _probeSelfHiddenFromMain = true;
            return;
        }

        if (!_probeSelfHiddenFromMain)
            return;

        _main.cullingMask = _savedMainCullingMask;
        _probeSelfHiddenFromMain = false;
    }
}
