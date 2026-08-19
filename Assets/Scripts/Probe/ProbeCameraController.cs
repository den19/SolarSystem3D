using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Main-camera chase / cockpit override. Does not write the probe into LookAtTarget.currentTarget.
/// </summary>
[DefaultExecutionOrder(110)]
public class ProbeCameraController : MonoBehaviour
{
    public static bool SuppressBodyPicking { get; private set; }

    MobileOrbitCamera _orbit;
    Camera _main;
    LookAtTarget _lookAt;
    bool _overrideActive;
    GameObject _focusBeforeProbe;

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

    public void Tick(ProbeSystemController system)
    {
        if (_main == null)
        {
            _main = Camera.main;
            if (_main != null)
                _orbit = _main.GetComponent<MobileOrbitCamera>();
        }

        bool flying = system != null && system.IsFlying;
        ProbeCameraMode mode = ProbeSettings.CameraMode;
        bool wantOverride = flying && (mode == ProbeCameraMode.Chase || mode == ProbeCameraMode.Cockpit);
        SuppressBodyPicking = wantOverride && mode == ProbeCameraMode.Cockpit;

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
            _main.transform.position = craft.transform.position + velocity * 0.55f;
            _main.transform.rotation = Quaternion.LookRotation(velocity, Vector3.up);
            if (_orbit != null)
                _orbit.SetExternalOrbitControl(true);
            return;
        }

        Vector3 chasePos = craft.transform.position - velocity * 4.5f + Vector3.up * 1.4f;
        _main.transform.position = Vector3.Lerp(_main.transform.position, chasePos, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        _main.transform.rotation = Quaternion.LookRotation(craft.transform.position - _main.transform.position, Vector3.up);
        if (_orbit != null)
            _orbit.SetExternalOrbitControl(true);
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
}
