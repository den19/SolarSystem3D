using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Switches celestial bodies between circular RotateAround motion and elliptical BodyOrbitController motion.
/// </summary>
public class BodyOrbitSystemController : MonoBehaviour
{
    struct BodyOrbitEntry
    {
        public Transform Transform;
        public RotateAround RotateAround;
        public BodyOrbitController OrbitController;
        public SolarSystemCatalog.BodyDefinition Definition;
        public float PlaneHeight;
        public bool UseWorldSpace;
    }

    readonly List<BodyOrbitEntry> _entries = new List<BodyOrbitEntry>();
    SolarSystemScaleController _scaleController;
    Transform _sun;

    void Awake()
    {
        _sun = GameObject.Find("Sun")?.transform;
        _scaleController = GetComponent<SolarSystemScaleController>();
        CaptureBodies();
        OrbitSettings.UseRealOrbitsChanged += OnUseRealOrbitsChanged;
        ScaleSettings.UseRealDistancesChanged += OnUseRealDistancesChanged;
    }

    void Start()
    {
        ApplyOrbitMode();
    }

    void OnDestroy()
    {
        OrbitSettings.UseRealOrbitsChanged -= OnUseRealOrbitsChanged;
        ScaleSettings.UseRealDistancesChanged -= OnUseRealDistancesChanged;
    }

    void OnUseRealOrbitsChanged(bool enabled) => ApplyOrbitMode();

    void OnUseRealDistancesChanged(bool enabled)
    {
        if (!OrbitSettings.UseRealOrbits)
            return;

        UpdateOrbitParameters();
    }

    void CaptureBodies()
    {
        _entries.Clear();

        for (int i = 0; i < SolarSystemCatalog.Bodies.Length; i++)
        {
            SolarSystemCatalog.BodyDefinition definition = SolarSystemCatalog.Bodies[i];
            if (definition.objectName == "Sun")
                continue;

            GameObject bodyGo = GameObject.Find(definition.objectName);
            if (bodyGo == null)
                continue;

            var rotateAround = bodyGo.GetComponent<RotateAround>();
            if (rotateAround == null)
                continue;

            var orbitController = bodyGo.GetComponent<BodyOrbitController>();
            if (orbitController == null)
                orbitController = bodyGo.AddComponent<BodyOrbitController>();

            bool isSatellite = !string.IsNullOrEmpty(definition.orbitCenterName);
            _entries.Add(new BodyOrbitEntry
            {
                Transform = bodyGo.transform,
                RotateAround = rotateAround,
                OrbitController = orbitController,
                Definition = definition,
                PlaneHeight = bodyGo.transform.localPosition.y,
                UseWorldSpace = !isSatellite
            });
        }
    }

    void ApplyOrbitMode()
    {
        // Time Machine owns motion: refresh SMA only — do not re-snap circular layout.
        if (SolarSystemApp.SimulationClock.DrivesMotion)
        {
            RefreshTimeMachineOrbitParameters();
            return;
        }

        if (OrbitSettings.UseRealOrbits)
        {
            if (_scaleController != null)
                _scaleController.SyncCircularLayoutBeforeRealOrbits();

            for (int i = 0; i < _entries.Count; i++)
            {
                BodyOrbitEntry entry = _entries[i];
                if (entry.RotateAround != null)
                    entry.RotateAround.enabled = false;
                if (entry.OrbitController != null)
                {
                    ConfigureOrbitController(entry, capturePhaseFromPosition: true);
                    entry.OrbitController.enabled = true;
                }
            }
        }
        else
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                BodyOrbitEntry entry = _entries[i];
                if (entry.OrbitController != null)
                    entry.OrbitController.enabled = false;
                if (entry.RotateAround != null)
                    entry.RotateAround.enabled = true;
            }

            if (_scaleController != null)
                _scaleController.ApplyDistancesOnly();
        }

        RebuildOrbitLinesIfReady();
    }

    void UpdateOrbitParameters()
    {
        if (SolarSystemApp.SimulationClock.DrivesMotion)
        {
            RefreshTimeMachineOrbitParameters();
            return;
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            BodyOrbitEntry entry = _entries[i];
            if (entry.OrbitController == null || !entry.OrbitController.enabled)
                continue;

            ConfigureOrbitController(entry, capturePhaseFromPosition: false);
        }

        RebuildOrbitLinesIfReady();
    }

    void RebuildOrbitLinesIfReady()
    {
        // Avoid SetParent while SimulationViewSystems is activating/deactivating.
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        if (_scaleController != null)
            _scaleController.RebuildOrbitLinesOnly();
    }

    void ConfigureOrbitController(BodyOrbitEntry entry, bool capturePhaseFromPosition, float? overridePhase = null)
    {
        Transform center = ResolveOrbitCenter(entry.Definition);
        if (center == null || entry.OrbitController == null)
            return;

        float semiMajorAxis = _scaleController != null
            ? _scaleController.GetOrbitSemiMajorAxis(entry.Definition.objectName)
            : 0f;

        if (semiMajorAxis <= 0.0001f)
            return;

        float angularSpeed = entry.RotateAround != null ? entry.RotateAround.speed : 5f;
        float phase;
        if (overridePhase.HasValue)
            phase = overridePhase.Value;
        else if (capturePhaseFromPosition)
            phase = ComputePhaseFromTransform(entry, center, semiMajorAxis);
        else
            phase = entry.OrbitController.PhaseRad;

        entry.OrbitController.Configure(
            center,
            semiMajorAxis,
            entry.Definition.orbitalEccentricity,
            entry.Definition.orbitalInclinationDeg,
            entry.PlaneHeight,
            phase,
            angularSpeed,
            entry.UseWorldSpace);
    }

    float ComputePhaseFromTransform(BodyOrbitEntry entry, Transform center, float semiMajorAxis)
    {
        Vector3 planeOffset = Vector3.up * entry.PlaneHeight;
        Vector3 offset = entry.UseWorldSpace
            ? entry.Transform.position - center.position - planeOffset
            : entry.Transform.localPosition - planeOffset;

        return OrbitLineUtility.ComputePhaseFromOffset(
            offset,
            semiMajorAxis,
            entry.Definition.orbitalEccentricity,
            entry.Definition.orbitalInclinationDeg);
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

    public void RestoreSavedMotion(Dictionary<string, float> phases, Dictionary<string, Vector3> positions)
    {
        if (!OrbitSettings.UseRealOrbits)
        {
            if (positions == null)
                return;

            foreach (KeyValuePair<string, Vector3> kvp in positions)
            {
                GameObject bodyGo = GameObject.Find(kvp.Key);
                if (bodyGo != null)
                    bodyGo.transform.position = kvp.Value;
            }

            return;
        }

        if (phases == null)
            return;

        for (int i = 0; i < _entries.Count; i++)
        {
            BodyOrbitEntry entry = _entries[i];
            if (!phases.TryGetValue(entry.Definition.objectName, out float phase))
                continue;

            ConfigureOrbitController(entry, capturePhaseFromPosition: false, overridePhase: phase);
        }
    }

    /// <summary>
    /// One-shot setup when Time Machine turns on: snap layout once, enable orbit controllers.
    /// </summary>
    public void PrepareForTimeMachine()
    {
        if (_scaleController != null)
            _scaleController.SyncCircularLayoutBeforeRealOrbits();

        RefreshTimeMachineOrbitParameters();
    }

    /// <summary>
    /// Keep RotateAround off and refresh SMA without re-snapping circular layout.
    /// </summary>
    public void RefreshTimeMachineOrbitParameters()
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            BodyOrbitEntry entry = _entries[i];
            if (entry.RotateAround != null)
                entry.RotateAround.enabled = false;

            if (entry.OrbitController != null)
            {
                ConfigureOrbitController(entry, capturePhaseFromPosition: false);
                entry.OrbitController.enabled = true;
            }
        }

        RebuildOrbitLinesIfReady();
    }

    public void ApplyKeplerPhases(double daysSinceJ2000)
    {
        // Planets first, then satellites (centers must be placed).
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                BodyOrbitEntry entry = _entries[i];
                bool isSatellite = !string.IsNullOrEmpty(entry.Definition.orbitCenterName);
                if (pass == 0 && isSatellite)
                    continue;
                if (pass == 1 && !isSatellite)
                    continue;

                if (entry.OrbitController == null || !entry.OrbitController.enabled)
                    continue;

                double periodDays = SolarSystemCatalog.GetSiderealPeriodDays(entry.Definition);
                float trueAnomaly = KeplerOrbitMath.TrueAnomalyRad(
                    daysSinceJ2000,
                    periodDays,
                    entry.Definition.orbitalEccentricity);
                entry.OrbitController.SetPhaseRad(trueAnomaly);
            }
        }
    }

    public void RestoreAfterTimeMachine()
    {
        ApplyOrbitMode();
    }
}
