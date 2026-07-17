using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Drives SimulationClock, Kepler body/comet positions, and sun activity while Time Machine is on.
/// Enabled only via side-panel toggle — never auto-starts on New/Continue.
/// </summary>
public class TimeMachineController : MonoBehaviour
{
    BodyOrbitSystemController _orbitSystem;
    CometSystemController _cometSystem;
    TimeMachineDateHud _dateHud;
    bool _running;
    string _lastHudDate;

    public static TimeMachineController EnsureOnHost(GameObject host)
    {
        if (host == null)
            return null;

        var controller = host.GetComponent<TimeMachineController>();
        if (controller == null)
            controller = host.AddComponent<TimeMachineController>();
        return controller;
    }

    void OnEnable()
    {
        TimeMachineSettings.UseTimeMachineChanged += OnTimeMachineSettingChanged;
        SimulationViewSettings.ShowSimulationUiChanged += OnShowSimulationUiChanged;
        SimulationTimeController.IsPausedChanged += OnPausedChanged;
        ScaleSettings.ModeChanged += OnScaleModeChanged;
        OrbitSettings.UseRealOrbitsChanged += OnOrbitSettingsChanged;
        SimulationClock.SpeedResetMilestoneReached += OnSpeedResetMilestoneReached;
    }

    void OnDisable()
    {
        TimeMachineSettings.UseTimeMachineChanged -= OnTimeMachineSettingChanged;
        SimulationViewSettings.ShowSimulationUiChanged -= OnShowSimulationUiChanged;
        SimulationTimeController.IsPausedChanged -= OnPausedChanged;
        ScaleSettings.ModeChanged -= OnScaleModeChanged;
        OrbitSettings.UseRealOrbitsChanged -= OnOrbitSettingsChanged;
        SimulationClock.SpeedResetMilestoneReached -= OnSpeedResetMilestoneReached;
        // Never rebuild orbit lines here: parent SimulationViewSystems may be deactivating.
        StopTimeMachine(restoreOrbits: false);
    }

    void OnDestroy()
    {
        _running = false;
        SimulationClock.SetActive(false);
    }

    void Start()
    {
        _orbitSystem = GetComponent<BodyOrbitSystemController>();
        _cometSystem = GetComponent<CometSystemController>();

        // Manual toggle only: ensure clock is inactive on scene load even if prefs were left on.
        // If prefs are on (user left TM enabled last session), begin after systems exist.
        if (TimeMachineSettings.UseTimeMachine)
            BeginTimeMachine(resetClock: true);
        else
            StopTimeMachine(restoreOrbits: false);

        // Hide date HUD unless Time Machine is actively running.
        RefreshDateHud(forceLayout: true);
    }

    void Update()
    {
        if (!_running)
            return;

        // Freeze calendar while paused; SpeedMultiplier is preserved (and can change on pause).
        if (SimulationTimeController.IsPaused)
            return;

        // Keep timeScale pinned while TM drives motion (panel speed only affects the calendar).
        SimulationTimeController.Apply();

        float speed = SimulationTimeController.SpeedMultiplier;
        float dt = Time.unscaledDeltaTime * speed;
        if (dt <= 0f)
            return;

        bool hitMilestone = SimulationClock.Tick(dt);
        if (hitMilestone)
        {
            // Tick clamped the calendar onto the milestone and forced 1×.
            // Do not reuse the pre-reset high-speed dt this frame — next frame reads SpeedMultiplier=1.
            SimulationTimeController.ForceSpeedOneX();
            SimulationTimeController.Apply();
        }

        ApplyMotionAndActivity();
        RefreshDateHud(forceLayout: false);
    }

    void OnTimeMachineSettingChanged(bool enabled)
    {
        if (enabled)
            BeginTimeMachine(resetClock: true);
        else
            StopTimeMachine(restoreOrbits: true);
    }

    void OnShowSimulationUiChanged(bool _)
    {
        RefreshDateHud(forceLayout: true);
    }

    void OnPausedChanged(bool paused)
    {
        if (!_running)
            return;

        if (paused)
        {
            // Freeze only: do not reset SpeedMultiplier (user may change speed while paused).
            SimulationTimeController.Apply();
            return;
        }

        // Resume: apply pinned timeScale, then one Kepler/HUD refresh at the current SpeedMultiplier.
        SimulationTimeController.Apply();
        ApplyMotionAndActivity();
        RefreshDateHud(forceLayout: true);
    }

    void OnScaleModeChanged(SolarSystemApp.ScaleMode _)
    {
        if (!_running)
            return;

        _orbitSystem?.RefreshTimeMachineOrbitParameters();
        ApplyMotionAndActivity();
    }

    void OnOrbitSettingsChanged(bool _)
    {
        if (!_running)
            return;

        _orbitSystem?.RefreshTimeMachineOrbitParameters();
        ApplyMotionAndActivity();
    }

    void OnSpeedResetMilestoneReached()
    {
        if (!_running)
            return;

        SimulationTimeController.ForceSpeedOneX();
    }

    public void BeginTimeMachine(bool resetClock)
    {
        if (_orbitSystem == null)
            _orbitSystem = GetComponent<BodyOrbitSystemController>();
        if (_cometSystem == null)
            _cometSystem = GetComponent<CometSystemController>();

        if (resetClock)
            SimulationClock.ResetToSweepStart();
        else
            SimulationClock.ResetSpeedMilestones();

        SimulationClock.SetActive(true);
        // Pin timeScale=1 while TM is active; calendar rate uses SpeedMultiplier only.
        SimulationTimeController.Apply();
        _orbitSystem?.PrepareForTimeMachine();
        _running = true;
        _lastHudDate = null;

        ApplyMotionAndActivity();
        EnsureDateHud();
        RefreshDateHud(forceLayout: true);
    }

    public void StopTimeMachine(bool restoreOrbits)
    {
        _running = false;
        SimulationClock.SetActive(false);
        // Restore normal timeScale = SpeedMultiplier mapping outside TM.
        SimulationTimeController.Apply();
        SunCoronalVfxController.SetActivityScale(1f);

        if (restoreOrbits)
            _orbitSystem?.RestoreAfterTimeMachine();

        if (_dateHud != null)
            _dateHud.SetVisible(false);
    }

    void ApplyMotionAndActivity()
    {
        double days = SimulationClock.DaysSinceJ2000;
        _orbitSystem?.ApplyKeplerPhases(days);
        _cometSystem?.ApplyKeplerAngles(days);
        SunCoronalVfxController.SetActivityScale(SimulationClock.EvaluateSunActivityScale());
    }

    void EnsureDateHud()
    {
        if (_dateHud != null)
            return;

        GameObject canvasGo = GameObject.Find("MainScreenCanvas");
        if (canvasGo == null)
            return;

        _dateHud = TimeMachineDateHud.EnsureOnCanvas(canvasGo.transform);
    }

    void RefreshDateHud(bool forceLayout)
    {
        bool show = _running
            && TimeMachineSettings.UseTimeMachine
            && SimulationViewSettings.ShowSimulationUi;

        if (!show)
        {
            if (_dateHud != null)
                _dateHud.SetVisible(false);
            return;
        }

        EnsureDateHud();
        if (_dateHud == null)
            return;

        _dateHud.SetVisible(true);

        string dateText = SimulationClock.FormatDateYyyyMmDd();
        if (!forceLayout && dateText == _lastHudDate)
            return;

        _lastHudDate = dateText;
        _dateHud.SetDateText(dateText);
    }
}
