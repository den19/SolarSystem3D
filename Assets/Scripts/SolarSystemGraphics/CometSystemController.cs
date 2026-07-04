using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns the five periodic comets used in clean view mode.
/// </summary>
public class CometSystemController : MonoBehaviour
{
    [SerializeField] Material cometSurfaceMaterial;

    Transform _cometsRoot;
    Transform _sun;
    OrbitLinesManager _orbitLinesManager;
    BodyLabelManager _bodyLabelManager;
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

        foreach (CometCatalog.CometDefinition definition in CometCatalog.Comets)
            SpawnComet(definition);
    }

    void SpawnComet(CometCatalog.CometDefinition definition)
    {
        var cometGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cometGo.name = definition.objectName;
        cometGo.transform.SetParent(_cometsRoot, false);
        cometGo.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        var collider = cometGo.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        var renderer = cometGo.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            if (cometSurfaceMaterial == null)
                cometSurfaceMaterial = CreateDefaultCometMaterial();
            renderer.sharedMaterial = cometSurfaceMaterial;
        }

        var trail = cometGo.AddComponent<TrailRenderer>();
        trail.time = 4f;
        trail.startWidth = 0.35f;
        trail.endWidth = 0.02f;
        trail.minVertexDistance = 0.05f;
        trail.numCapVertices = 4;
        trail.material = CreateTrailMaterial();
        trail.startColor = new Color(0.92f, 0.95f, 0.15f, 0.85f);
        trail.endColor = new Color(0.98f, 0.97f, 0.28f, 0f);

        var orbit = cometGo.AddComponent<CometOrbitController>();
        orbit.Initialize(definition, _sun);
        _cometOrbits.Add(orbit);
        _cometDefinitions.Add((definition, definition.semiMajorAxis));

        if (_orbitLinesManager != null)
        {
            _orbitLinesManager.RegisterCometEllipse(
                definition.semiMajorAxis,
                definition.eccentricity,
                definition.inclinationDeg,
                definition.phaseOffsetRad);
        }

        if (_bodyLabelManager != null)
            _bodyLabelManager.RegisterCometLabel(cometGo.transform, definition.labelKey);
    }

    public void RescaleCometOrbits(float auToUnity)
    {
        for (int i = 0; i < _cometOrbits.Count; i++)
        {
            CometOrbitController orbit = _cometOrbits[i];
            if (orbit == null)
                continue;

            float realAxis = orbit.Definition.semiMajorAxisAu * auToUnity;
            orbit.SetSemiMajorAxis(realAxis);
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

        RebuildCometOrbitLines(0f, useRealDistances: false);
    }

    void RebuildCometOrbitLines(float auToUnity, bool useRealDistances)
    {
        if (_orbitLinesManager == null)
            return;

        _orbitLinesManager.ClearCometOrbitLines();

        for (int i = 0; i < _cometDefinitions.Count; i++)
        {
            CometCatalog.CometDefinition definition = _cometDefinitions[i].definition;
            float semiMajorAxis = useRealDistances
                ? definition.semiMajorAxisAu * auToUnity
                : _cometDefinitions[i].baselineSemiMajorAxis;

            _orbitLinesManager.RegisterCometEllipse(
                semiMajorAxis,
                definition.eccentricity,
                definition.inclinationDeg,
                definition.phaseOffsetRad);
        }
    }

    static Material CreateDefaultCometMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        var material = new Material(shader);
        material.color = new Color(0.75f, 0.78f, 0.82f, 1f);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(0.75f, 0.78f, 0.82f, 1f));
        return material;
    }

    static Material CreateTrailMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        var material = new Material(shader);
        material.color = Color.white;
        return material;
    }
}
