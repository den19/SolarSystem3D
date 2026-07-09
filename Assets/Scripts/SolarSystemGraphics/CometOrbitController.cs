using UnityEngine;

/// <summary>
/// Moves a comet along a circular or inclined elliptical orbit around the Sun.
/// </summary>
public class CometOrbitController : MonoBehaviour
{
    CometCatalog.CometDefinition _definition;
    Transform _sun;
    float _angle;
    float _semiMinorAxis;
    float _simulationSemiMajorAxis;
    bool _useRealOrbits;

    public CometCatalog.CometDefinition Definition => _definition;

    public float OrbitAngle => _angle;

    public float PerihelionAu => _definition.semiMajorAxisAu * (1f - _definition.eccentricity);

    public float GetHeliocentricDistanceAu()
    {
        float a = _definition.semiMajorAxisAu;
        float e = _definition.eccentricity;
        if (e < 0.001f)
            return a;

        return a * (1f - e * e) / (1f + e * Mathf.Cos(_angle));
    }

    public void Initialize(CometCatalog.CometDefinition definition, Transform sun, bool useRealOrbits)
    {
        _definition = definition;
        _sun = sun;
        _useRealOrbits = useRealOrbits;
        _simulationSemiMajorAxis = definition.semiMajorAxis;
        _angle = definition.phaseOffsetRad;
        RecalculateSemiMinorAxis();
        UpdatePosition();
    }

    public void SetUseRealOrbits(bool useRealOrbits)
    {
        if (_useRealOrbits == useRealOrbits)
            return;

        _useRealOrbits = useRealOrbits;
        UpdatePosition();
    }

    public void SetSemiMajorAxis(float semiMajorAxis)
    {
        _definition.semiMajorAxis = semiMajorAxis;
        RecalculateSemiMinorAxis();
        UpdatePosition();
    }

    public void RestoreSimulationSemiMajorAxis()
    {
        _definition.semiMajorAxis = _simulationSemiMajorAxis;
        RecalculateSemiMinorAxis();
        UpdatePosition();
    }

    public void RefreshPosition() => UpdatePosition();

    public void SetOrbitAngle(float angle)
    {
        _angle = angle;
        UpdatePosition();
    }

    void RecalculateSemiMinorAxis()
    {
        _semiMinorAxis = _definition.semiMajorAxis *
            Mathf.Sqrt(Mathf.Max(0f, 1f - _definition.eccentricity * _definition.eccentricity));
    }

    void Update()
    {
        if (_sun == null)
            return;

        _angle += (Mathf.PI * 2f / _definition.simPeriodSec) * Time.deltaTime;
        UpdatePosition();
    }

    void UpdatePosition()
    {
        if (_sun == null)
            return;

        if (_useRealOrbits)
            UpdateEllipticalPosition();
        else
            UpdateCircularPosition();
    }

    void UpdateEllipticalPosition()
    {
        float inclRad = _definition.inclinationDeg * Mathf.Deg2Rad;
        float cosIncl = Mathf.Cos(inclRad);
        float sinIncl = Mathf.Sin(inclRad);

        float x = Mathf.Cos(_angle) * _definition.semiMajorAxis;
        float zFlat = Mathf.Sin(_angle) * _semiMinorAxis;
        float y = zFlat * sinIncl;
        float z = zFlat * cosIncl;

        transform.position = _sun.position + new Vector3(x, y, z);

        Vector3 tangent = new Vector3(
            -Mathf.Sin(_angle) * _definition.semiMajorAxis,
            zFlat * cosIncl * sinIncl,
            Mathf.Cos(_angle) * _semiMinorAxis * cosIncl);
        if (tangent.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(tangent.normalized, Vector3.up);
    }

    void UpdateCircularPosition()
    {
        float radius = _definition.semiMajorAxis;
        float x = Mathf.Cos(_angle) * radius;
        float z = Mathf.Sin(_angle) * radius;

        transform.position = _sun.position + new Vector3(x, 0f, z);

        Vector3 tangent = new Vector3(-Mathf.Sin(_angle), 0f, Mathf.Cos(_angle));
        if (tangent.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(tangent.normalized, Vector3.up);
    }
}
