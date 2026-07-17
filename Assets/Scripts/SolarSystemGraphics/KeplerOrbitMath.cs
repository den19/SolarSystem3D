using System;
using UnityEngine;

/// <summary>
/// Two-body Kepler helpers: mean anomaly → eccentric anomaly → true anomaly.
/// </summary>
public static class KeplerOrbitMath
{
    const int MaxNewtonIterations = 12;
    const float NewtonEpsilon = 1e-6f;

    /// <summary>Sidereal period in days from semi-major axis in AU (Kepler's third law, Sun-centered).</summary>
    public static double PeriodDaysFromAu(double semiMajorAxisAu)
    {
        double a = Math.Max(1e-8, semiMajorAxisAu);
        return SimulationClockDaysPerYear() * Math.Pow(a, 1.5);
    }

    static double SimulationClockDaysPerYear()
    {
        return SolarSystemApp.SimulationClock.DaysPerYear;
    }

    public static float MeanAnomalyRad(double daysSinceEpoch, double periodDays, float phaseOffsetRad = 0f)
    {
        if (periodDays < 1e-8)
            return phaseOffsetRad;

        double orbits = daysSinceEpoch / periodDays;
        double m = (orbits - Math.Floor(orbits)) * (Math.PI * 2.0) + phaseOffsetRad;
        return WrapTwoPi((float)m);
    }

    public static float SolveEccentricAnomaly(float meanAnomalyRad, float eccentricity)
    {
        float e = Mathf.Clamp01(eccentricity);
        float m = WrapTwoPi(meanAnomalyRad);
        if (m > Mathf.PI)
            m -= Mathf.PI * 2f;

        float E = e < 0.8f ? m : Mathf.PI;
        for (int i = 0; i < MaxNewtonIterations; i++)
        {
            float f = E - e * Mathf.Sin(E) - m;
            float fp = 1f - e * Mathf.Cos(E);
            if (Mathf.Abs(fp) < 1e-8f)
                break;

            float dE = f / fp;
            E -= dE;
            if (Mathf.Abs(dE) < NewtonEpsilon)
                break;
        }

        return WrapTwoPi(E);
    }

    public static float TrueAnomalyFromEccentric(float eccentricAnomalyRad, float eccentricity)
    {
        float e = Mathf.Clamp01(eccentricity);
        float E = eccentricAnomalyRad;
        float cosE = Mathf.Cos(E);
        float sinE = Mathf.Sin(E);
        float cosNu = (cosE - e) / Mathf.Max(1e-6f, 1f - e * cosE);
        float sinNu = (Mathf.Sqrt(Mathf.Max(0f, 1f - e * e)) * sinE) / Mathf.Max(1e-6f, 1f - e * cosE);
        return Mathf.Atan2(sinNu, cosNu);
    }

    public static float TrueAnomalyRad(double daysSinceEpoch, double periodDays, float eccentricity, float phaseOffsetRad = 0f)
    {
        float m = MeanAnomalyRad(daysSinceEpoch, periodDays, phaseOffsetRad);
        float E = SolveEccentricAnomaly(m, eccentricity);
        return TrueAnomalyFromEccentric(E, eccentricity);
    }

    public static float WrapTwoPi(float angle)
    {
        const float twoPi = Mathf.PI * 2f;
        angle %= twoPi;
        if (angle < 0f)
            angle += twoPi;
        return angle;
    }
}
