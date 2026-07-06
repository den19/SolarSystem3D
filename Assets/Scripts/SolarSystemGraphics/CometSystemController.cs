using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Spawns the ten periodic comets used in clean view mode.
/// </summary>
public class CometSystemController : MonoBehaviour
{
    Transform _cometsRoot;
    Transform _sun;
    OrbitLinesManager _orbitLinesManager;
    BodyLabelManager _bodyLabelManager;
    float _cachedAuToUnity = 22f;
    readonly List<CometOrbitController> _cometOrbits = new List<CometOrbitController>();
    readonly List<(CometCatalog.CometDefinition definition, float baselineSemiMajorAxis)> _cometDefinitions =
        new List<(CometCatalog.CometDefinition, float)>();

    public Transform CometsRoot => _cometsRoot;

    public void Initialize(OrbitLinesManager orbitLinesManager, BodyLabelManager bodyLabelManager)
    {
        _orbitLinesManager = orbitLinesManager;
        _bodyLabelManager = bodyLabelManager;
        _sun = GameObject.Find("Sun")?.transform;

        var rootGo = new GameObject("CometsRoot");
        _cometsRoot = rootGo.transform;
        _cometsRoot.SetParent(transform, false);
        _cometsRoot.gameObject.SetActive(false);

        var scaleController = GetComponent<SolarSystemScaleController>();
        if (scaleController != null)
            _cachedAuToUnity = scaleController.AuToUnity;

        foreach (CometCatalog.CometDefinition definition in CometCatalog.Comets)
            SpawnComet(definition);

        OrbitSettings.UseRealOrbitsChanged += OnUseRealOrbitsChanged;
        CometMovementSettings.UseCometMovementChanged += OnUseCometMovementChanged;
        ApplyOrbitMode();
    }

    void OnDestroy()
    {
        OrbitSettings.UseRealOrbitsChanged -= OnUseRealOrbitsChanged;
        CometMovementSettings.UseCometMovementChanged -= OnUseCometMovementChanged;
    }

    void OnUseRealOrbitsChanged(bool enabled) => ApplyOrbitMode();

    void OnUseCometMovementChanged(bool enabled) => ApplyOrbitMode();

    static bool GetUseRealCometOrbits()
    {
        return OrbitSettings.UseRealOrbits && CometMovementSettings.UseCometMovement;
    }

    void ApplyOrbitMode()
    {
        bool useRealOrbits = GetUseRealCometOrbits();

        for (int i = 0; i < _cometOrbits.Count; i++)
        {
            CometOrbitController orbit = _cometOrbits[i];
            if (orbit == null)
                continue;

            orbit.SetUseRealOrbits(useRealOrbits);
            orbit.RefreshPosition();
        }

        RebuildCometOrbitLines(_cachedAuToUnity, ScaleSettings.UseRealDistances);
    }

    void SpawnComet(CometCatalog.CometDefinition definition)
    {
        GameObject prefab = !string.IsNullOrEmpty(definition.prefabResourcePath)
            ? Resources.Load<GameObject>(definition.prefabResourcePath)
            : null;

        GameObject cometGo;
        if (prefab != null)
        {
            cometGo = Instantiate(prefab, _cometsRoot, false);
            cometGo.name = definition.objectName;
        }
        else
        {
            CometContentData.ContentEntry content = CometContentData.Get(definition.objectName);
            cometGo = CometPrefabFactory.Build(definition, content);
            cometGo.transform.SetParent(_cometsRoot, false);
        }

        EnsurePickCollider(cometGo);

        var info = cometGo.GetComponent<CometInfo>();
        if (info == null)
            info = cometGo.AddComponent<CometInfo>();
        info.Configure(definition.objectName, definition.labelKey);

        if (string.IsNullOrEmpty(info.GetDescription(Language.English)))
        {
            CometContentData.ContentEntry content = CometContentData.Get(definition.objectName);
            info.SetDescriptions(content.english, content.russian, content.chinese, content.vietnamese, content.uzbek);
        }

        var visual = cometGo.GetComponent<CometVisualController>();
        if (visual == null)
            visual = cometGo.AddComponent<CometVisualController>();
        visual.Initialize(definition, _sun, _cachedAuToUnity);

        var orbit = cometGo.GetComponent<CometOrbitController>();
        if (orbit == null)
            orbit = cometGo.AddComponent<CometOrbitController>();
        orbit.Initialize(definition, _sun, GetUseRealCometOrbits());

        _cometOrbits.Add(orbit);
        _cometDefinitions.Add((definition, definition.semiMajorAxis));

        if (_bodyLabelManager != null)
            _bodyLabelManager.RegisterCometLabel(cometGo.transform, definition.labelKey);
    }

    static void EnsurePickCollider(GameObject cometGo)
    {
        var collider = cometGo.GetComponent<SphereCollider>();
        if (collider == null)
            collider = cometGo.AddComponent<SphereCollider>();
        collider.radius = 1.5f;
        collider.center = Vector3.zero;
    }

    public void RescaleCometOrbits(float auToUnity)
    {
        _cachedAuToUnity = auToUnity;

        for (int i = 0; i < _cometOrbits.Count; i++)
        {
            CometOrbitController orbit = _cometOrbits[i];
            if (orbit == null)
                continue;

            float realAxis = orbit.Definition.semiMajorAxisAu * auToUnity;
            orbit.SetSemiMajorAxis(realAxis);

            var visual = orbit.GetComponent<CometVisualController>();
            if (visual != null)
                visual.Initialize(orbit.Definition, _sun, auToUnity);
        }

        RebuildCometOrbitLines(auToUnity, useRealDistances: true);
    }

    public void RescaleCometOrbitsToSimulation()
    {
        for (int i = 0; i < _cometOrbits.Count; i++)
        {
            CometOrbitController orbit = _cometOrbits[i];
            if (orbit != null)
                orbit.RestoreSimulationSemiMajorAxis();
        }

        RebuildCometOrbitLines(_cachedAuToUnity, useRealDistances: false);
    }

    void RebuildCometOrbitLines(float auToUnity, bool useRealDistances)
    {
        if (_orbitLinesManager == null)
            return;

        _orbitLinesManager.ClearCometOrbitLines();
        bool useRealOrbits = GetUseRealCometOrbits();

        for (int i = 0; i < _cometDefinitions.Count; i++)
        {
            CometCatalog.CometDefinition definition = _cometDefinitions[i].definition;
            float orbitSize = useRealDistances
                ? definition.semiMajorAxisAu * auToUnity
                : _cometDefinitions[i].baselineSemiMajorAxis;

            if (useRealOrbits)
            {
                _orbitLinesManager.RegisterCometEllipse(
                    orbitSize,
                    definition.eccentricity,
                    definition.inclinationDeg,
                    definition.phaseOffsetRad);
            }
            else
            {
                _orbitLinesManager.RegisterCometCircle(
                    orbitSize,
                    definition.phaseOffsetRad);
            }
        }
    }
}
