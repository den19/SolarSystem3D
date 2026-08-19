using System;
using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Restricted N-body slingshot: launch, integrate, abort on layout teleports.
/// </summary>
[DefaultExecutionOrder(100)]
public class ProbeSystemController : MonoBehaviour
{
    public const string HudObjectName = "ProbeHud";
    public const int MaxAttractors = 32;

    public static ProbeSystemController Instance { get; private set; }

    public ProbeCraft Craft { get; private set; }
    public bool IsFlying => Craft != null && !Craft.Impacted;
    public bool IsAiming => ProbeSettings.UseProbe && !IsFlying;
    public Vector3 AimVelocity { get; private set; }
    public float AimHeadingDeg = 0f;
    public float ImpulseNormalized = 0.55f;
    public string TelemetryShareLine { get; private set; } = string.Empty;

    public event Action StateChanged;

    readonly ProbeGravityIntegrator.Attractor[] _attractors = new ProbeGravityIntegrator.Attractor[MaxAttractors];
    readonly Vector3[] _ghostPoints = new Vector3[ProbeTrajectoryPredictor.PointCount];
    readonly Vector3[] _prevBodyPos = new Vector3[MaxAttractors];
    readonly string[] _prevBodyNames = new string[MaxAttractors];

    int _attractorCount;
    float _gmSun = 1f;
    bool _cappedToastShown;
    LineRenderer _ghostLine;
    Transform _sun;
    Transform _earth;
    SolarSystemScaleController _scale;
    ProbeCameraController _cameraController;
    ProbeViewRig _viewRig;
    ProbeGridProjection _gridProjection;
    LookAtTarget _lookAt;

    public static ProbeSystemController EnsureOnHost(GameObject host)
    {
        if (host == null)
            return null;

        var controller = host.GetComponent<ProbeSystemController>();
        if (controller == null)
            controller = host.AddComponent<ProbeSystemController>();
        return controller;
    }

    void Awake()
    {
        Instance = this;
        _sun = GameObject.Find("Sun")?.transform;
        _earth = GameObject.Find("Earth")?.transform;
        _scale = GetComponent<SolarSystemScaleController>();
        _lookAt = FindFirstObjectByType<LookAtTarget>();
        EnsureGhostLine();
    }

    void OnEnable()
    {
        ProbeSettings.UseProbeChanged += OnUseProbeChanged;
        ProbeSettings.LoadoutChanged += OnLoadoutChanged;
        ScaleSettings.ModeChanged += OnLayoutTeleport;
        OrbitSettings.UseRealOrbitsChanged += OnLayoutTeleport;
        TimeMachineSettings.UseTimeMachineChanged += OnTimeMachineChanged;
    }

    void OnDisable()
    {
        ProbeSettings.UseProbeChanged -= OnUseProbeChanged;
        ProbeSettings.LoadoutChanged -= OnLoadoutChanged;
        ScaleSettings.ModeChanged -= OnLayoutTeleport;
        OrbitSettings.UseRealOrbitsChanged -= OnLayoutTeleport;
        TimeMachineSettings.UseTimeMachineChanged -= OnTimeMachineChanged;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        AbortInternal(restoreCamera: false, toastKey: null, toastFallback: null);
    }

    void Start()
    {
        Canvas canvas = GameObject.Find("MainScreenCanvas")?.GetComponent<Canvas>();
        if (canvas != null)
            ProbeHudController.EnsureOnCanvas(canvas.transform);

        _cameraController = ProbeCameraController.EnsureOnHost(gameObject);
        _viewRig = ProbeViewRig.EnsureOnHost(gameObject);
        _gridProjection = ProbeGridProjection.EnsureOnHost(gameObject);

        if (!ProbeSettings.UseProbe)
            SetHudVisible(false);
    }

    void LateUpdate()
    {
        if (!ProbeSettings.UseProbe)
        {
            SetGhostVisible(false);
            return;
        }

        RecalibrateGm();
        SampleAttractors();

        if (IsFlying)
            TickFlight();
        else
            TickAim();

        _cameraController?.Tick(this);
        _viewRig?.Tick(this);
        _gridProjection?.Tick(this);
    }

    void TickAim()
    {
        if (!TryGetLaunchPose(out Vector3 position, out Vector3 prograde, out Vector3 originVelocity, out float originRadius, out string originName))
        {
            SetGhostVisible(false);
            return;
        }

        AimVelocity = originVelocity + prograde * ResolveImpulseSpeed(position) * ProbeSettings.ResolveLaunchSpeedScale();
        int n = ProbeTrajectoryPredictor.Predict(position, AimVelocity, _attractors, _attractorCount, _ghostPoints);
        DrawGhost(n);
    }

    void TickFlight()
    {
        float rawDt = ProbeGravityIntegrator.GetSimulationDeltaTime();
        if (rawDt <= 0f)
        {
            Craft.AlignToVelocity();
            UpdateTelemetry();
            SetGhostVisible(false);
            return;
        }

        float dt = rawDt;
        if (dt > ProbeGravityIntegrator.MaxPhysicsDelta)
        {
            dt = ProbeGravityIntegrator.MaxPhysicsDelta;
            if (!_cappedToastShown)
            {
                _cappedToastShown = true;
                TransientMessageController.ShowLocalized(
                    "ProbePhysicsCappedMessage",
                    "Probe physics is capped while the calendar runs this fast.");
            }
        }

        Vector3 pos = Craft.transform.position;
        Vector3 vel = Craft.Velocity;
        ProbeGravityIntegrator.IntegrateVerlet(ref pos, ref vel, _attractors, _attractorCount, dt);
        Craft.transform.position = pos;
        Craft.Velocity = vel;
        Craft.AlignToVelocity();

        Transform earth = _earth != null ? _earth : GameObject.Find("Earth")?.transform;
        if (earth != null)
            Craft.PointAntennaAt(earth.position);

        if (CheckImpact(pos) || CheckOverheat(pos))
            return;

        int n = ProbeTrajectoryPredictor.Predict(pos, vel, _attractors, _attractorCount, _ghostPoints);
        DrawGhost(n);
        UpdateTelemetry();
    }

    bool CheckImpact(Vector3 pos)
    {
        for (int i = 0; i < _attractorCount; i++)
        {
            float hit = _attractors[i].radius * 0.92f;
            if (hit < 0.05f)
                continue;

            if ((pos - _attractors[i].position).sqrMagnitude <= hit * hit)
            {
                Craft.Impacted = true;
                TransientMessageController.ShowLocalized(
                    "ProbeImpactMessage",
                    "Probe impacted " + _attractors[i].name + ".");
                AbortInternal(restoreCamera: true, toastKey: null, toastFallback: null);
                return true;
            }
        }

        return false;
    }

    bool CheckOverheat(Vector3 pos)
    {
        if (Craft.HasShield || _sun == null)
            return false;

        Transform mercury = GameObject.Find("Mercury")?.transform;
        float limit = mercury != null
            ? Vector3.Distance(mercury.position, _sun.position) * 0.72f
            : 8f;

        if (Vector3.Distance(pos, _sun.position) < limit)
        {
            Craft.Overheated = true;
            TransientMessageController.ShowLocalized(
                "ProbeOverheatMessage",
                "Probe overheated near the Sun. Add a heat shield.");
            AbortInternal(restoreCamera: true, toastKey: null, toastFallback: null);
            return true;
        }

        return false;
    }

    public void Launch()
    {
        if (!ProbeSettings.UseProbe)
            return;

        AbortInternal(restoreCamera: false, toastKey: null, toastFallback: null);

        if (!TryGetLaunchPose(out Vector3 position, out Vector3 prograde, out Vector3 originVelocity, out float originRadius, out string originName))
        {
            TransientMessageController.ShowLocalized("ProbeLaunchFailedMessage", "No launch body in focus.");
            return;
        }

        if (_lookAt != null)
        {
            if (originName.StartsWith("Comet_", StringComparison.Ordinal))
            {
                GameObject comet = GameObject.Find(originName);
                if (comet != null)
                    _lookAt.FocusComet(comet, showDescription: false);
            }
            else
            {
                _lookAt.FocusPlanet(originName, useDetailCamera: false, showDescription: false);
            }

            _lookAt.TurnOffAllDetailCameras();
        }

        bool highDetail = SolarSystemApp.GraphicsSettings.UseExtraGraphics && GraphicsTierSettings.IsHighEffective;
        Craft = ProbePrefabFactory.Create(ProbeSettings.Model, highDetail);
        Craft.OriginName = originName;
        Craft.transform.position = position;
        Craft.Velocity = originVelocity + prograde * ResolveImpulseSpeed(position) * ProbeSettings.ResolveLaunchSpeedScale();
        Craft.AlignToVelocity();
        _cappedToastShown = false;
        StateChanged?.Invoke();
    }

    public void Abort(string toastKey, string toastFallback)
    {
        AbortInternal(restoreCamera: true, toastKey, toastFallback);
    }

    public void Burn()
    {
        if (!IsFlying || Craft.BurnsRemaining <= 0)
            return;

        Vector3 dir = Craft.Velocity.sqrMagnitude > 1e-6f ? Craft.Velocity.normalized : transform.forward;
        float impulse = ResolveImpulseSpeed(Craft.transform.position) * 0.35f;
        Craft.Velocity += dir * impulse;
        Craft.BurnsRemaining--;
        StateChanged?.Invoke();
    }

    public void RequestPostcard()
    {
        if (!IsFlying)
            return;

        if (Craft == null || !Craft.HasAntenna)
        {
            TransientMessageController.ShowLocalized(
                "ProbeNeedAntennaMessage",
                "Antenna required to send a postcard.");
            return;
        }

        if (SimulationShareController.Instance != null)
            SimulationShareController.Instance.RequestShare();
    }

    void AbortInternal(bool restoreCamera, string toastKey, string toastFallback)
    {
        if (Craft != null)
        {
            Destroy(Craft.gameObject);
            Craft = null;
        }

        _cameraController?.ExitProbeCamera(restoreCamera);
        _viewRig?.SetActive(false);
        SetGhostVisible(false);
        _cappedToastShown = false;

        if (!string.IsNullOrEmpty(toastKey))
            TransientMessageController.ShowLocalized(toastKey, toastFallback);

        StateChanged?.Invoke();
    }

    void OnUseProbeChanged(bool enabled)
    {
        if (!enabled)
            AbortInternal(restoreCamera: true, toastKey: null, toastFallback: null);

        SetHudVisible(enabled);
        StateChanged?.Invoke();
    }

    void OnLoadoutChanged()
    {
        if (IsFlying)
            return;
        StateChanged?.Invoke();
    }

    void OnLayoutTeleport(SolarSystemApp.ScaleMode mode) => AbortForTeleport("ProbeAbortedScale", "Probe aborted: scale mode changed.");

    void OnLayoutTeleport(bool _) => AbortForTeleport("ProbeAbortedOrbits", "Probe aborted: orbit mode changed.");

    void OnTimeMachineChanged(bool _) => AbortForTeleport("ProbeAbortedTimeMachine", "Probe aborted: Time Machine changed.");

    void AbortForTeleport(string key, string fallback)
    {
        if (IsFlying)
            AbortInternal(restoreCamera: true, key, fallback);
    }

    void SetHudVisible(bool visible)
    {
        Transform canvas = GameObject.Find("MainScreenCanvas")?.transform;
        if (canvas == null)
            return;

        Transform hud = canvas.Find(HudObjectName);
        if (hud != null)
            hud.gameObject.SetActive(visible);
    }

    bool TryGetLaunchPose(out Vector3 position, out Vector3 prograde, out Vector3 originVelocity, out float originRadius, out string originName)
    {
        position = Vector3.zero;
        prograde = Vector3.forward;
        originVelocity = Vector3.zero;
        originRadius = 0.5f;
        originName = "Earth";

        GameObject origin = null;
        if (_lookAt != null)
        {
            origin = _lookAt.currentTarget;
            if (origin == null || origin.name == "SlingshotProbe")
                origin = _lookAt.ResolveObservationTarget();
        }

        if (origin == null)
            origin = GameObject.Find("Earth");
        if (origin == null)
            return false;

        originName = origin.name;
        originRadius = EstimateRadius(origin);
        Vector3 outward = origin.transform.position;
        if (_sun != null)
        {
            Vector3 fromSun = origin.transform.position - _sun.position;
            if (fromSun.sqrMagnitude > 1e-4f)
                outward = fromSun.normalized;
            else
                outward = origin.transform.right;
        }

        position = origin.transform.position + outward * (originRadius * 1.45f + 0.35f);

        Vector3 orbitRadial = _sun != null ? origin.transform.position - _sun.position : Vector3.right;
        orbitRadial.y = 0f;
        if (orbitRadial.sqrMagnitude < 1e-4f)
            orbitRadial = Vector3.right;
        prograde = Vector3.Cross(Vector3.up, orbitRadial.normalized).normalized;
        prograde = Quaternion.AngleAxis(AimHeadingDeg, Vector3.up) * prograde;

        originVelocity = EstimateBodyVelocity(originName, origin.transform.position);
        return true;
    }

    float ResolveImpulseSpeed(Vector3 position)
    {
        float r = _sun != null ? Vector3.Distance(position, _sun.position) : 22f;
        float circular = Mathf.Sqrt(Mathf.Max(0.01f, _gmSun / Mathf.Max(0.5f, r)));
        float t = Mathf.Clamp01(ImpulseNormalized);
        return circular * Mathf.Lerp(0.35f, 2.4f, t);
    }

    void SampleAttractors()
    {
        _attractorCount = 0;
        AddAttractor(_sun != null ? _sun.gameObject : GameObject.Find("Sun"));

        for (int i = 0; i < SolarSystemCatalog.Bodies.Length && _attractorCount < MaxAttractors; i++)
        {
            string name = SolarSystemCatalog.Bodies[i].objectName;
            if (name == "Sun")
                continue;

            AddAttractor(GameObject.Find(name));
        }
    }

    void AddAttractor(GameObject go)
    {
        if (go == null || _attractorCount >= MaxAttractors)
            return;

        float mass = ProbeMassCatalog.MassRatioToSun(go.name);
        if (mass <= 0f)
            return;

        Vector3 pos = go.transform.position;
        Vector3 vel = EstimateBodyVelocity(go.name, pos);

        _attractors[_attractorCount] = new ProbeGravityIntegrator.Attractor
        {
            position = pos,
            velocity = vel,
            gm = go.name == "Sun" ? _gmSun : _gmSun * mass,
            radius = EstimateRadius(go),
            name = go.name,
            transform = go.transform
        };
        _attractorCount++;
        RememberBody(go.name, pos);
    }

    Vector3 EstimateBodyVelocity(string name, Vector3 pos)
    {
        for (int i = 0; i < _prevBodyNames.Length; i++)
        {
            if (_prevBodyNames[i] != name)
                continue;

            float dt = ProbeGravityIntegrator.GetSimulationDeltaTime();
            if (dt > 1e-5f)
                return (pos - _prevBodyPos[i]) / dt;

            break;
        }

        RememberBody(name, pos);
        if (_sun == null)
            return Vector3.zero;

        Vector3 radial = pos - _sun.position;
        radial.y = 0f;
        if (radial.sqrMagnitude < 1e-4f)
            return Vector3.zero;

        float r = radial.magnitude;
        float circular = Mathf.Sqrt(Mathf.Max(0f, _gmSun / r));
        return Vector3.Cross(Vector3.up, radial.normalized) * circular;
    }

    void RememberBody(string name, Vector3 pos)
    {
        for (int i = 0; i < _prevBodyNames.Length; i++)
        {
            if (_prevBodyNames[i] == name || string.IsNullOrEmpty(_prevBodyNames[i]))
            {
                _prevBodyNames[i] = name;
                _prevBodyPos[i] = pos;
                return;
            }
        }
    }

    void RecalibrateGm()
    {
        if (_sun == null || _earth == null)
            return;

        Vector3 earthVel = EstimateBodyVelocity("Earth", _earth.position);
        float gm = ProbeGravityIntegrator.CalibrateGmSun(_sun.position, _earth.position, earthVel);
        if (gm > 0.01f)
            _gmSun = gm;
    }

    static float EstimateRadius(GameObject go)
    {
        var sphere = go.GetComponent<SphereCollider>();
        if (sphere != null)
        {
            float scale = Mathf.Max(go.transform.lossyScale.x, go.transform.lossyScale.y, go.transform.lossyScale.z);
            return Mathf.Max(0.08f, sphere.radius * scale);
        }

        var renderer = go.GetComponentInChildren<Renderer>();
        if (renderer != null)
            return Mathf.Max(0.08f, renderer.bounds.extents.magnitude);

        return 0.5f;
    }

    void EnsureGhostLine()
    {
        var go = new GameObject("ProbeGhostPath");
        go.transform.SetParent(transform, false);
        _ghostLine = go.AddComponent<LineRenderer>();
        _ghostLine.positionCount = 0;
        _ghostLine.useWorldSpace = true;
        _ghostLine.loop = false;
        _ghostLine.widthMultiplier = 0.12f;
        _ghostLine.numCapVertices = 2;
        _ghostLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _ghostLine.receiveShadows = false;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        var mat = new Material(shader);
        mat.color = new Color(0.35f, 0.9f, 1f, 0.7f);
        _ghostLine.sharedMaterial = mat;
        _ghostLine.enabled = false;
    }

    void DrawGhost(int count)
    {
        if (_ghostLine == null)
            return;

        _ghostLine.enabled = count > 1;
        _ghostLine.positionCount = count;
        for (int i = 0; i < count; i++)
            _ghostLine.SetPosition(i, _ghostPoints[i]);
    }

    void SetGhostVisible(bool visible)
    {
        if (_ghostLine != null)
            _ghostLine.enabled = visible;
    }

    void UpdateTelemetry()
    {
        if (Craft == null || _sun == null)
        {
            TelemetryShareLine = string.Empty;
            return;
        }

        Vector3 relSun = Craft.Velocity;
        if (TryGetAttractor("Sun", out ProbeGravityIntegrator.Attractor sunA))
            relSun = Craft.Velocity - sunA.velocity;

        string nearest = FindNearestName(Craft.transform.position, out float nearestDist);
        TelemetryShareLine = string.Format(
            "v☉ {0:0.00}  {1} {2:0.00}",
            relSun.magnitude,
            nearest,
            nearestDist);
    }

    public bool TryGetAttractor(string name, out ProbeGravityIntegrator.Attractor attractor)
    {
        for (int i = 0; i < _attractorCount; i++)
        {
            if (_attractors[i].name == name)
            {
                attractor = _attractors[i];
                return true;
            }
        }

        attractor = default;
        return false;
    }

    public int CopyAttractors(ProbeGravityIntegrator.Attractor[] dest)
    {
        int n = Mathf.Min(_attractorCount, dest.Length);
        Array.Copy(_attractors, dest, n);
        return n;
    }

    public string FindNearestName(Vector3 pos, out float distance)
    {
        distance = float.MaxValue;
        string name = "—";
        for (int i = 0; i < _attractorCount; i++)
        {
            float d = Vector3.Distance(pos, _attractors[i].position) - _attractors[i].radius;
            if (d < distance)
            {
                distance = Mathf.Max(0f, d);
                name = _attractors[i].name;
            }
        }

        return name;
    }

    public float AuToUnity
    {
        get
        {
            if (_scale != null && _scale.AuToUnity > 0.01f)
                return _scale.AuToUnity;
            return SolarSystemLayout.DefaultAuToUnity;
        }
    }

    public Transform Sun => _sun;
}
