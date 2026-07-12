using UnityEngine;

/// <summary>
/// Builds circular and elliptical orbit line geometry for LineRenderer.
/// </summary>
public static class OrbitLineUtility
{
    public static Vector3[] BuildCircle(int segments, float radius, Vector3 center, Vector3 normal)
    {
        segments = Mathf.Max(8, segments);
        var points = new Vector3[segments + 1];
        Vector3 axis = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
        Vector3 tangent = Vector3.Cross(axis, Vector3.forward);
        if (tangent.sqrMagnitude < 0.0001f)
            tangent = Vector3.Cross(axis, Vector3.right);
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(axis, tangent).normalized;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            points[i] = center + (tangent * cos + bitangent * sin) * radius;
        }

        return points;
    }

    public static float SemiMinorAxis(float semiMajorAxis, float eccentricity)
    {
        return semiMajorAxis * Mathf.Sqrt(Mathf.Max(0f, 1f - eccentricity * eccentricity));
    }

    public static Vector3 EllipsePoint(float angleRad, float semiMajorAxis, float eccentricity, float inclinationDeg)
    {
        float e = Mathf.Clamp01(eccentricity);
        float a = semiMajorAxis;
        float oneMinusESq = 1f - e * e;
        float denom = 1f + e * Mathf.Cos(angleRad);
        float r = denom > 0.0001f ? a * oneMinusESq / denom : a;

        float x = r * Mathf.Cos(angleRad);
        float zFlat = r * Mathf.Sin(angleRad);
        return ApplyOrbitalInclination(x, zFlat, inclinationDeg);
    }

    static Vector3 ApplyOrbitalInclination(float x, float zFlat, float inclinationDeg)
    {
        float inclRad = inclinationDeg * Mathf.Deg2Rad;
        float cosIncl = Mathf.Cos(inclRad);
        float sinIncl = Mathf.Sin(inclRad);
        float y = zFlat * sinIncl;
        float z = zFlat * cosIncl;
        return new Vector3(x, y, z);
    }

    static float RemoveOrbitalInclinationZFlat(Vector3 offset, float inclinationDeg)
    {
        float inclRad = inclinationDeg * Mathf.Deg2Rad;
        float cosIncl = Mathf.Cos(inclRad);
        float sinIncl = Mathf.Sin(inclRad);
        return offset.y * sinIncl + offset.z * cosIncl;
    }

    public static float ComputePhaseFromOffset(Vector3 offset, float semiMajorAxis, float eccentricity, float inclinationDeg)
    {
        if (semiMajorAxis < 0.0001f)
            return 0f;

        float zFlat = RemoveOrbitalInclinationZFlat(offset, inclinationDeg);
        return Mathf.Atan2(zFlat, offset.x);
    }

    public static Vector3[] BuildEllipse(int segments, float semiMajorAxis, float eccentricity, float inclinationDeg, Vector3 center, float phaseOffsetRad)
    {
        segments = Mathf.Max(16, segments);
        var points = new Vector3[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            float angle = phaseOffsetRad + i / (float)segments * Mathf.PI * 2f;
            points[i] = center + EllipsePoint(angle, semiMajorAxis, eccentricity, inclinationDeg);
        }

        return points;
    }

    public static Material CreateOrbitLineMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        var material = new Material(shader);
        material.color = new Color(0.35f, 0.85f, 1f, 0.45f);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(0.35f, 0.85f, 1f, 0.45f));
        return material;
    }
}
