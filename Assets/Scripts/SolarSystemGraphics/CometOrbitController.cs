using UnityEngine;

/// <summary>
/// Moves a comet along an inclined elliptical orbit around the Sun.
/// </summary>
public class CometOrbitController : MonoBehaviour
{
    CometCatalog.CometDefinition _definition;
    Transform _sun;
    float _angle;
    float _semiMinorAxis;
    float _simulationSemiMajorAxis;

    public CometCatalog.CometDefinition Definition => _definition;

    public void Initialize(CometCatalog.CometDefinition definition, Transform sun)
    {
        _definition = definition;
        _sun = sun;
        _simulationSemiMajorAxis = definition.semiMajorAxis;
        _angle = definition.phaseOffsetRad;
        RecalculateSemiMinorAxis();
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
}
