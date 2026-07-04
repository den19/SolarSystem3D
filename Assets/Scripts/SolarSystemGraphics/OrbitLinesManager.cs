using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Creates and toggles orbit line renderers for planets, moons, and comets in Level1.
/// </summary>
public class OrbitLinesManager : MonoBehaviour
{
    static readonly string[] BodyNames =
    {
        "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune", "Moon", "Titan"
    };

    static readonly Dictionary<string, string> OrbitCenters = new Dictionary<string, string>
    {
        { "Moon", "Earth" },
        { "Titan", "Saturn" }
    };

    [SerializeField] int circleSegments = 96;
    [SerializeField] int ellipseSegments = 128;
    [SerializeField] float lineWidth = 0.08f;

    readonly List<LineRenderer> _lines = new List<LineRenderer>();
    Material _lineMaterial;
    Transform _sun;
    bool _visible;

    void Awake()
    {
        _lineMaterial = OrbitLineUtility.CreateOrbitLineMaterial();
        _sun = GameObject.Find("Sun")?.transform;
        BuildBodyOrbits();
        ApplyVisibility(SimulationViewSettings.ShowOrbitLines);
    }

    void OnEnable()
    {
        SimulationViewSettings.ShowOrbitLinesChanged += ApplyVisibility;
    }

    void OnDisable()
    {
        SimulationViewSettings.ShowOrbitLinesChanged -= ApplyVisibility;
    }

    public void RegisterCometEllipse(float semiMajorAxis, float eccentricity, float inclinationDeg, float phaseOffsetRad)
    {
        if (_sun == null)
            return;

        var lineGo = new GameObject("CometOrbitLine");
        lineGo.transform.SetParent(transform, false);
        var line = lineGo.AddComponent<LineRenderer>();
        ConfigureLine(line);
        line.positionCount = ellipseSegments + 1;
        line.SetPositions(OrbitLineUtility.BuildEllipse(
            ellipseSegments,
            semiMajorAxis,
            eccentricity,
            inclinationDeg,
            _sun.position,
            phaseOffsetRad));
        _lines.Add(line);
        line.enabled = _visible;
    }

    void BuildBodyOrbits()
    {
        if (_sun == null)
            return;

        foreach (string bodyName in BodyNames)
        {
            GameObject bodyGo = GameObject.Find(bodyName);
            if (bodyGo == null)
                continue;

            Transform center = _sun;
            if (OrbitCenters.TryGetValue(bodyName, out string centerName))
            {
                GameObject centerGo = GameObject.Find(centerName);
                if (centerGo != null)
                    center = centerGo.transform;
            }

            float radius = Vector3.ProjectOnPlane(bodyGo.transform.position - center.position, Vector3.up).magnitude;
            if (radius < 0.5f)
                continue;

            var lineGo = new GameObject(bodyName + "_OrbitLine");
            lineGo.transform.SetParent(center, false);
            var line = lineGo.AddComponent<LineRenderer>();
            ConfigureLine(line);
            line.useWorldSpace = false;
            line.positionCount = circleSegments + 1;
            line.SetPositions(OrbitLineUtility.BuildCircle(circleSegments, radius, Vector3.zero, Vector3.up));
            _lines.Add(line);
        }
    }

    void ConfigureLine(LineRenderer line)
    {
        line.useWorldSpace = true;
        line.loop = true;
        line.widthMultiplier = lineWidth;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = _lineMaterial;
        line.startColor = new Color(0.35f, 0.85f, 1f, 0.45f);
        line.endColor = new Color(0.35f, 0.85f, 1f, 0.45f);
    }

    void ApplyVisibility(bool visible)
    {
        _visible = visible;
        for (int i = 0; i < _lines.Count; i++)
        {
            if (_lines[i] != null)
                _lines[i].enabled = visible;
        }
    }
}
