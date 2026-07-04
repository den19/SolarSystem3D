using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Creates and toggles orbit line renderers for planets, moons, and comets in Level1.
/// </summary>
public class OrbitLinesManager : MonoBehaviour
{
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

    public void RebuildOrbits(IReadOnlyList<SolarSystemScaleController.OrbitSpec> specs)
    {
        ClearBodyOrbitLines();

        if (specs == null)
        {
            ApplyVisibility(_visible);
            return;
        }

        for (int i = 0; i < specs.Count; i++)
            BuildOrbitLine(specs[i]);

        ApplyVisibility(_visible);
    }

    public void ClearCometOrbitLines()
    {
        for (int i = _lines.Count - 1; i >= 0; i--)
        {
            if (_lines[i] == null)
                continue;

            if (_lines[i].gameObject.name == "CometOrbitLine")
            {
                Destroy(_lines[i].gameObject);
                _lines.RemoveAt(i);
            }
        }
    }

    void ClearBodyOrbitLines()
    {
        for (int i = _lines.Count - 1; i >= 0; i--)
        {
            if (_lines[i] == null)
                continue;

            if (_lines[i].gameObject.name.EndsWith("_OrbitLine"))
            {
                Destroy(_lines[i].gameObject);
                _lines.RemoveAt(i);
            }
        }
    }

    void BuildOrbitLine(SolarSystemScaleController.OrbitSpec spec)
    {
        if (spec.Center == null || spec.Radius <= 0.0001f)
            return;

        var lineGo = new GameObject(spec.BodyName + "_OrbitLine");
        var line = lineGo.AddComponent<LineRenderer>();
        ConfigureLine(line);
        line.positionCount = circleSegments + 1;

        if (spec.UseWorldSpace)
        {
            lineGo.transform.SetParent(transform, false);
            line.useWorldSpace = true;
            Vector3 center = spec.Center.position + Vector3.up * spec.PlaneHeight;
            line.SetPositions(OrbitLineUtility.BuildCircle(circleSegments, spec.Radius, center, Vector3.up));
        }
        else
        {
            lineGo.transform.SetParent(spec.Center, false);
            line.useWorldSpace = false;
            Vector3 center = new Vector3(0f, spec.PlaneHeight, 0f);
            line.SetPositions(OrbitLineUtility.BuildCircle(circleSegments, spec.Radius, center, Vector3.up));
        }

        _lines.Add(line);
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
