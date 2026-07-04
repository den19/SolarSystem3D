using UnityEngine;

/// <summary>
/// Moves a celestial body along an inclined elliptical orbit around a center transform.
/// </summary>
public class BodyOrbitController : MonoBehaviour
{
    Transform _center;
    float _semiMajorAxis;
    float _eccentricity;
    float _inclinationDeg;
    float _planeHeight;
    float _phaseRad;
    float _angularSpeedDegPerSec;
    bool _useWorldSpace;

    public void Configure(
        Transform center,
        float semiMajorAxis,
        float eccentricity,
        float inclinationDeg,
        float planeHeight,
        float phaseRad,
        float angularSpeedDegPerSec,
        bool useWorldSpace)
    {
        _center = center;
        _semiMajorAxis = semiMajorAxis;
        _eccentricity = eccentricity;
        _inclinationDeg = inclinationDeg;
        _planeHeight = planeHeight;
        _phaseRad = phaseRad;
        _angularSpeedDegPerSec = angularSpeedDegPerSec;
        _useWorldSpace = useWorldSpace;
        ApplyPosition();
    }

    public void SetSemiMajorAxis(float semiMajorAxis)
    {
        _semiMajorAxis = semiMajorAxis;
        ApplyPosition();
    }

    public void SetPhaseFromCurrentPosition()
    {
        if (_center == null)
            return;

        Vector3 offset = GetOrbitOffset();
        _phaseRad = OrbitLineUtility.ComputePhaseFromOffset(
            offset,
            _semiMajorAxis,
            _eccentricity,
            _inclinationDeg);
        ApplyPosition();
    }

    public float PhaseRad => _phaseRad;

    void Update()
    {
        if (_center == null)
            return;

        _phaseRad += _angularSpeedDegPerSec * Mathf.Deg2Rad * Time.deltaTime;
        ApplyPosition();
    }

    void ApplyPosition()
    {
        if (_center == null)
            return;

        Vector3 orbitalPoint = OrbitLineUtility.EllipsePoint(
            _phaseRad,
            _semiMajorAxis,
            _eccentricity,
            _inclinationDeg);
        Vector3 planeOffset = Vector3.up * _planeHeight;

        if (_useWorldSpace)
            transform.position = _center.position + planeOffset + orbitalPoint;
        else
            transform.localPosition = planeOffset + orbitalPoint;
    }

    Vector3 GetOrbitOffset()
    {
        Vector3 planeOffset = Vector3.up * _planeHeight;
        if (_useWorldSpace)
            return transform.position - _center.position - planeOffset;

        return transform.localPosition - planeOffset;
    }
}
