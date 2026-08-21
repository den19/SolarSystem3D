using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Spawns named asteroids with shared Asteroid Ball mesh; materials follow sun distance.
/// Always visible (unlike comets which gate on CometMovementSettings).
/// </summary>
public class AsteroidSystemController : MonoBehaviour
{
    Transform _asteroidsRoot;
    Transform _sun;
    OrbitLinesManager _orbitLinesManager;
    BodyLabelManager _bodyLabelManager;
    float _cachedAuToUnity = 22f;
    readonly List<AsteroidOrbitController> _orbits = new List<AsteroidOrbitController>();
    readonly List<(AsteroidCatalog.AsteroidDefinition definition, float baselineSemiMajorAxis)> _definitions =
        new List<(AsteroidCatalog.AsteroidDefinition, float)>();

    public Transform AsteroidsRoot => _asteroidsRoot;

    public void Initialize(OrbitLinesManager orbitLinesManager, BodyLabelManager bodyLabelManager)
    {
        _orbitLinesManager = orbitLinesManager;
        _bodyLabelManager = bodyLabelManager;
        _sun = GameObject.Find("Sun")?.transform;

        var rootGo = new GameObject("AsteroidsRoot");
        _asteroidsRoot = rootGo.transform;
        _asteroidsRoot.SetParent(transform, false);

        var scaleController = GetComponent<SolarSystemScaleController>();
        if (scaleController != null)
            _cachedAuToUnity = scaleController.AuToUnity;

        foreach (AsteroidCatalog.AsteroidDefinition definition in AsteroidCatalog.Asteroids)
            SpawnAsteroid(definition);

        OrbitSettings.UseRealOrbitsChanged += OnUseRealOrbitsChanged;
        ApplyOrbitMode();
    }

    void OnDestroy()
    {
        OrbitSettings.UseRealOrbitsChanged -= OnUseRealOrbitsChanged;
    }

    void OnUseRealOrbitsChanged(bool enabled) => ApplyOrbitMode();

    void ApplyOrbitMode()
    {
        bool useRealOrbits = OrbitSettings.UseRealOrbits;

        for (int i = 0; i < _orbits.Count; i++)
        {
            AsteroidOrbitController orbit = _orbits[i];
            if (orbit == null)
                continue;

            orbit.SetUseRealOrbits(useRealOrbits);
            orbit.RefreshPosition();
        }

        RebuildOrbitLines(_cachedAuToUnity, ScaleSettings.UseRealDistances);
    }

    void SpawnAsteroid(AsteroidCatalog.AsteroidDefinition definition)
    {
        GameObject prefab = !string.IsNullOrEmpty(definition.prefabResourcePath)
            ? Resources.Load<GameObject>(definition.prefabResourcePath)
            : null;

        AsteroidContentData.ContentEntry content = AsteroidContentData.Get(definition.objectName);

        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab, _asteroidsRoot, false);
            go.name = definition.objectName;
        }
        else
        {
            go = AsteroidPrefabFactory.Build(definition, content);
            go.transform.SetParent(_asteroidsRoot, false);
        }

        EnsurePickCollider(go);

        var info = go.GetComponent<AsteroidInfo>();
        if (info == null)
            info = go.AddComponent<AsteroidInfo>();
        info.Configure(definition.objectName, definition.labelKey);

        if (!string.IsNullOrEmpty(content.id))
        {
            info.SetDescriptions(
                content.english,
                content.russian,
                content.chinese,
                content.vietnamese,
                content.uzbek,
                content.tatar,
                content.belarusian);
        }

        var visual = go.GetComponent<AsteroidVisualController>();
        if (visual == null)
            visual = go.AddComponent<AsteroidVisualController>();
        visual.Initialize(definition, _sun, _cachedAuToUnity);

        var orbit = go.GetComponent<AsteroidOrbitController>();
        if (orbit == null)
            orbit = go.AddComponent<AsteroidOrbitController>();
        orbit.Initialize(definition, _sun, OrbitSettings.UseRealOrbits);

        _orbits.Add(orbit);
        _definitions.Add((definition, definition.semiMajorAxis));

        if (_bodyLabelManager != null)
            _bodyLabelManager.RegisterCometLabel(go.transform, definition.labelKey, verticalOffset: 1.4f);
    }

    static void EnsurePickCollider(GameObject go)
    {
        var collider = go.GetComponent<SphereCollider>();
        if (collider == null)
            collider = go.AddComponent<SphereCollider>();
        collider.radius = 0.75f;
        collider.center = Vector3.zero;
    }

    public void RescaleAsteroidOrbits(float auToUnity)
    {
        _cachedAuToUnity = auToUnity;

        for (int i = 0; i < _orbits.Count; i++)
        {
            AsteroidOrbitController orbit = _orbits[i];
            if (orbit == null)
                continue;

            float realAxis = orbit.Definition.semiMajorAxisAu * auToUnity;
            orbit.SetSemiMajorAxis(realAxis);

            var visual = orbit.GetComponent<AsteroidVisualController>();
            if (visual != null)
                visual.Initialize(orbit.Definition, _sun, auToUnity);
        }

        RebuildOrbitLines(auToUnity, useRealDistances: true);
    }

    public void RescaleAsteroidOrbitsToSimulation()
    {
        for (int i = 0; i < _orbits.Count; i++)
        {
            AsteroidOrbitController orbit = _orbits[i];
            if (orbit != null)
                orbit.RestoreSimulationSemiMajorAxis();
        }

        RebuildOrbitLines(_cachedAuToUnity, useRealDistances: false);
    }

    public void RestoreSavedAngles(Dictionary<string, float> angles)
    {
        if (angles == null)
            return;

        for (int i = 0; i < _orbits.Count; i++)
        {
            AsteroidOrbitController orbit = _orbits[i];
            if (orbit == null)
                continue;

            if (angles.TryGetValue(orbit.gameObject.name, out float angle))
                orbit.SetOrbitAngle(angle);
        }
    }

    public void ApplyKeplerAngles(double daysSinceJ2000)
    {
        for (int i = 0; i < _orbits.Count; i++)
        {
            AsteroidOrbitController orbit = _orbits[i];
            if (orbit == null)
                continue;

            AsteroidCatalog.AsteroidDefinition definition = orbit.Definition;
            float trueAnomaly = KeplerOrbitMath.TrueAnomalyRad(
                daysSinceJ2000,
                definition.SiderealPeriodDays,
                definition.eccentricity,
                definition.phaseOffsetRad);
            orbit.SetOrbitAngle(trueAnomaly);
        }
    }

    void RebuildOrbitLines(float auToUnity, bool useRealDistances)
    {
        if (_orbitLinesManager == null)
            return;

        _orbitLinesManager.ClearAsteroidOrbitLines();
        bool useRealOrbits = OrbitSettings.UseRealOrbits;

        for (int i = 0; i < _definitions.Count; i++)
        {
            AsteroidCatalog.AsteroidDefinition definition = _definitions[i].definition;
            float orbitSize = useRealDistances
                ? definition.semiMajorAxisAu * auToUnity
                : _definitions[i].baselineSemiMajorAxis;

            if (useRealOrbits)
            {
                _orbitLinesManager.RegisterAsteroidEllipse(
                    orbitSize,
                    definition.eccentricity,
                    definition.inclinationDeg,
                    definition.phaseOffsetRad);
            }
            else
            {
                _orbitLinesManager.RegisterAsteroidCircle(
                    orbitSize,
                    definition.phaseOffsetRad);
            }
        }
    }
}
