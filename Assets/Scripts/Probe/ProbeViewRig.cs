using SolarSystemApp;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Forward + two rear-view probe cameras rendered into HUD RawImages.
/// </summary>
[DefaultExecutionOrder(120)]
public class ProbeViewRig : MonoBehaviour
{
    public const string MarkerName = "ProbePipCamera";

    static ProbeViewRig _instance;

    Camera _forward;
    Camera _rearLeft;
    Camera _rearRight;
    RenderTexture _forwardRt;
    RenderTexture _rearLeftRt;
    RenderTexture _rearRightRt;
    bool _active;

    public static ProbeViewRig EnsureOnHost(GameObject host)
    {
        if (host == null)
            return null;

        var rig = host.GetComponent<ProbeViewRig>();
        if (rig == null)
            rig = host.AddComponent<ProbeViewRig>();
        return rig;
    }

    void Awake()
    {
        _instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
        Release();
    }

    public static void SetEnabledForCapture(bool enabled)
    {
        if (_instance == null)
            return;

        if (!enabled)
        {
            _instance.SetCamerasEnabled(false);
            return;
        }

        _instance.SetCamerasEnabled(_instance._active);
    }

    public void SetActive(bool active)
    {
        _active = active;
        if (!active)
            SetCamerasEnabled(false);
    }

    public void Tick(ProbeSystemController system)
    {
        bool want = system != null
            && system.IsFlying
            && ProbeSettings.ShowProbeViews;

        _active = want;
        if (!want)
        {
            SetCamerasEnabled(false);
            return;
        }

        EnsureCameras();
        SetCamerasEnabled(true);
        PoseCameras(system.Craft);
    }

    void PoseCameras(ProbeCraft craft)
    {
        if (craft == null)
            return;

        Vector3 forward = craft.Velocity.sqrMagnitude > 1e-5f ? craft.Velocity.normalized : craft.transform.forward;
        Vector3 up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.95f)
            up = craft.transform.up;

        Vector3 pos = craft.transform.position;
        _forward.transform.position = pos + forward * 0.7f;
        _forward.transform.rotation = Quaternion.LookRotation(forward, up);

        Vector3 back = -forward;
        Quaternion yawL = Quaternion.AngleAxis(-35f, up);
        Quaternion yawR = Quaternion.AngleAxis(35f, up);
        _rearLeft.transform.position = pos + yawL * back * 0.7f;
        _rearLeft.transform.rotation = Quaternion.LookRotation(yawL * back, up);
        _rearRight.transform.position = pos + yawR * back * 0.7f;
        _rearRight.transform.rotation = Quaternion.LookRotation(yawR * back, up);
    }

    void EnsureCameras()
    {
        if (_forward != null)
            return;

        bool balanced = !GraphicsTierSettings.IsHighEffective;
        int fw = balanced ? 256 : 320;
        int fh = balanced ? 144 : 180;
        int rw = balanced ? 192 : 256;
        int rh = balanced ? 108 : 144;

        _forwardRt = CreateRt(fw, fh);
        _rearLeftRt = CreateRt(rw, rh);
        _rearRightRt = CreateRt(rw, rh);
        _forward = CreatePipCamera("ProbeViewForward", _forwardRt);
        _rearLeft = CreatePipCamera("ProbeViewRearLeft", _rearLeftRt);
        _rearRight = CreatePipCamera("ProbeViewRearRight", _rearRightRt);
    }

    static RenderTexture CreateRt(int w, int h)
    {
        var rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32);
        rt.Create();
        return rt;
    }

    Camera CreatePipCamera(string objectName, RenderTexture rt)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 55f;
        cam.nearClipPlane = 0.15f;
        cam.farClipPlane = 800f;
        cam.depth = -20f;
        cam.cullingMask = ~(1 << 5);
        cam.targetTexture = rt;
        cam.enabled = false;
        cam.allowHDR = false;
        cam.allowMSAA = false;

        var data = go.GetComponent<UniversalAdditionalCameraData>();
        if (data == null)
            data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = false;
        data.renderType = CameraRenderType.Base;
        return cam;
    }

    void SetCamerasEnabled(bool enabled)
    {
        if (_forward != null)
            _forward.enabled = enabled;
        if (_rearLeft != null)
            _rearLeft.enabled = enabled;
        if (_rearRight != null)
            _rearRight.enabled = enabled;
    }

    public RenderTexture ForwardTexture => _forwardRt;
    public RenderTexture RearLeftTexture => _rearLeftRt;
    public RenderTexture RearRightTexture => _rearRightRt;

    void Release()
    {
        SetCamerasEnabled(false);
        ReleaseRt(ref _forwardRt);
        ReleaseRt(ref _rearLeftRt);
        ReleaseRt(ref _rearRightRt);
    }

    static void ReleaseRt(ref RenderTexture rt)
    {
        if (rt == null)
            return;
        rt.Release();
        Object.Destroy(rt);
        rt = null;
    }
}
