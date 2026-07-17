using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Astronomical reference data and educational layout helpers for Level1 scale modes.
/// </summary>
public static class SolarSystemCatalog
{
    public const float EarthOrbitalRadiusAu = 1f;
    public const float EarthEquatorialRadiusKm = 6371f;
    public const float AuKm = 149597870.7f;
    public const float MoonOrbitKm = 384400f;
    public const float TitanOrbitKm = 1221870f;
    public const float IoOrbitKm = 421700f;
    public const float EuropaOrbitKm = 671100f;
    public const float GanymedeOrbitKm = 1070400f;
    public const float CallistoOrbitKm = 1882700f;
    public const float PhobosOrbitKm = 9376f;
    public const float DeimosOrbitKm = 23463f;
    public const float TritonOrbitKm = 354759f;

    public struct BodyDefinition
    {
        public string objectName;
        public float orbitalRadiusAu;
        public float equatorialRadiusKm;
        public string orbitCenterName;
        public float satelliteOrbitKm;
        public float orbitalEccentricity;
        public float orbitalInclinationDeg;
    }

    public static readonly BodyDefinition[] Bodies =
    {
        new BodyDefinition { objectName = "Sun", orbitalRadiusAu = 0f, equatorialRadiusKm = 696340f },
        new BodyDefinition
        {
            objectName = "Mercury",
            orbitalRadiusAu = 0.387f,
            equatorialRadiusKm = 2439.7f,
            orbitalEccentricity = 0.205f,
            orbitalInclinationDeg = 7.0f
        },
        new BodyDefinition
        {
            objectName = "Venus",
            orbitalRadiusAu = 0.723f,
            equatorialRadiusKm = 6051.8f,
            orbitalEccentricity = 0.007f,
            orbitalInclinationDeg = 3.4f
        },
        new BodyDefinition
        {
            objectName = "Earth",
            orbitalRadiusAu = 1f,
            equatorialRadiusKm = 6371f,
            orbitalEccentricity = 0.017f,
            orbitalInclinationDeg = 0f
        },
        new BodyDefinition
        {
            objectName = "Mars",
            orbitalRadiusAu = 1.524f,
            equatorialRadiusKm = 3389.5f,
            orbitalEccentricity = 0.093f,
            orbitalInclinationDeg = 1.85f
        },
        new BodyDefinition
        {
            objectName = "Jupiter",
            orbitalRadiusAu = 5.203f,
            equatorialRadiusKm = 69911f,
            orbitalEccentricity = 0.049f,
            orbitalInclinationDeg = 1.3f
        },
        new BodyDefinition
        {
            objectName = "Saturn",
            orbitalRadiusAu = 9.537f,
            equatorialRadiusKm = 58232f,
            orbitalEccentricity = 0.057f,
            orbitalInclinationDeg = 2.49f
        },
        new BodyDefinition
        {
            objectName = "Uranus",
            orbitalRadiusAu = 19.19f,
            equatorialRadiusKm = 25362f,
            orbitalEccentricity = 0.046f,
            orbitalInclinationDeg = 0.77f
        },
        new BodyDefinition
        {
            objectName = "Neptune",
            orbitalRadiusAu = 30.07f,
            equatorialRadiusKm = 24622f,
            orbitalEccentricity = 0.009f,
            orbitalInclinationDeg = 1.77f
        },
        new BodyDefinition
        {
            objectName = "Moon",
            orbitalRadiusAu = 0f,
            equatorialRadiusKm = 1737.4f,
            orbitCenterName = "Earth",
            satelliteOrbitKm = MoonOrbitKm,
            orbitalEccentricity = 0.055f,
            orbitalInclinationDeg = 5.15f
        },
        new BodyDefinition
        {
            objectName = "Titan",
            orbitalRadiusAu = 0f,
            equatorialRadiusKm = 2574.7f,
            orbitCenterName = "Saturn",
            satelliteOrbitKm = TitanOrbitKm,
            orbitalEccentricity = 0.029f,
            orbitalInclinationDeg = 0.33f
        },
        new BodyDefinition
        {
            objectName = "Io",
            orbitalRadiusAu = 0f,
            equatorialRadiusKm = 1821.6f,
            orbitCenterName = "Jupiter",
            satelliteOrbitKm = IoOrbitKm,
            orbitalEccentricity = 0.0041f,
            orbitalInclinationDeg = 0.05f
        },
        new BodyDefinition
        {
            objectName = "Europa",
            orbitalRadiusAu = 0f,
            equatorialRadiusKm = 1560.8f,
            orbitCenterName = "Jupiter",
            satelliteOrbitKm = EuropaOrbitKm,
            orbitalEccentricity = 0.009f,
            orbitalInclinationDeg = 0.47f
        },
        new BodyDefinition
        {
            objectName = "Ganymede",
            orbitalRadiusAu = 0f,
            equatorialRadiusKm = 2634.1f,
            orbitCenterName = "Jupiter",
            satelliteOrbitKm = GanymedeOrbitKm,
            orbitalEccentricity = 0.0013f,
            orbitalInclinationDeg = 0.2f
        },
        new BodyDefinition
        {
            objectName = "Callisto",
            orbitalRadiusAu = 0f,
            equatorialRadiusKm = 2410.3f,
            orbitCenterName = "Jupiter",
            satelliteOrbitKm = CallistoOrbitKm,
            orbitalEccentricity = 0.0074f,
            orbitalInclinationDeg = 0.19f
        },
        new BodyDefinition
        {
            objectName = "Phobos",
            orbitalRadiusAu = 0f,
            equatorialRadiusKm = 11.27f,
            orbitCenterName = "Mars",
            satelliteOrbitKm = PhobosOrbitKm,
            orbitalEccentricity = 0.0151f,
            orbitalInclinationDeg = 1.08f
        },
        new BodyDefinition
        {
            objectName = "Deimos",
            orbitalRadiusAu = 0f,
            equatorialRadiusKm = 6.2f,
            orbitCenterName = "Mars",
            satelliteOrbitKm = DeimosOrbitKm,
            orbitalEccentricity = 0.00033f,
            orbitalInclinationDeg = 1.79f
        },
        new BodyDefinition
        {
            objectName = "Triton",
            orbitalRadiusAu = 0f,
            equatorialRadiusKm = 1353.4f,
            orbitCenterName = "Neptune",
            satelliteOrbitKm = TritonOrbitKm,
            orbitalEccentricity = 0.000016f,
            orbitalInclinationDeg = 0.67f
        }
    };

    static readonly Dictionary<string, BodyDefinition> ByName = BuildLookup();

    static Dictionary<string, BodyDefinition> BuildLookup()
    {
        var lookup = new Dictionary<string, BodyDefinition>();
        for (int i = 0; i < Bodies.Length; i++)
            lookup[Bodies[i].objectName] = Bodies[i];
        return lookup;
    }

    public static bool TryGetBody(string objectName, out BodyDefinition definition)
    {
        return ByName.TryGetValue(objectName, out definition);
    }

    /// <summary>
    /// Sidereal orbital period in days. Planets use Kepler a^(3/2); moons use catalog values.
    /// </summary>
    public static double GetSiderealPeriodDays(BodyDefinition definition)
    {
        if (!string.IsNullOrEmpty(definition.orbitCenterName))
            return GetSatellitePeriodDays(definition.objectName);

        if (definition.orbitalRadiusAu <= 0f)
            return 365.25;

        return KeplerOrbitMath.PeriodDaysFromAu(definition.orbitalRadiusAu);
    }

    public static double GetSatellitePeriodDays(string objectName)
    {
        switch (objectName)
        {
            case "Moon": return 27.321661;
            case "Phobos": return 0.318910;
            case "Deimos": return 1.26244;
            case "Io": return 1.769137786;
            case "Europa": return 3.551181;
            case "Ganymede": return 7.15455296;
            case "Callisto": return 16.6890184;
            case "Titan": return 15.945;
            case "Triton": return 5.876854;
            default: return 30.0;
        }
    }

    public static float MaxHeliocentricAu()
    {
        float max = 0f;
        for (int i = 0; i < Bodies.Length; i++)
        {
            if (string.IsNullOrEmpty(Bodies[i].orbitCenterName) && Bodies[i].orbitalRadiusAu > max)
                max = Bodies[i].orbitalRadiusAu;
        }

        return max;
    }

    /// <summary>Equatorial radius relative to Earth (1.0 = Earth size).</summary>
    public static float GetRadiusRatioToEarth(string objectName)
    {
        if (!TryGetBody(objectName, out BodyDefinition definition) || definition.equatorialRadiusKm <= 0f)
            return 1f;

        return definition.equatorialRadiusKm / EarthEquatorialRadiusKm;
    }

    /// <summary>
    /// Uniform local scale for a body when real-size mode uses catalog proportions.
    /// Earth reference scale is the scene baseline for Earth (typically 1).
    /// </summary>
    public static float GetCatalogUniformScale(float earthReferenceScale, string objectName)
    {
        float ratio = GetRadiusRatioToEarth(objectName);
        return earthReferenceScale * ratio;
    }

    /// <summary>
    /// Mass proxy for the spacetime grid, normalized so Jupiter ≈ 1 and the Sun dominates via multiplier.
    /// Uses physical radius ratios, not scene mesh scale.
    /// </summary>
    public static float GetGravityWellMass(string objectName, float sunMassMultiplier = 8f)
    {
        float radiusRatio = GetRadiusRatioToEarth(objectName);
        float mass = radiusRatio * radiusRatio * radiusRatio;

        const float jupiterRadiusRatio = 69911f / EarthEquatorialRadiusKm;
        float normalization = jupiterRadiusRatio * jupiterRadiusRatio * jupiterRadiusRatio;
        mass /= Mathf.Max(0.0001f, normalization);

        if (objectName == "Sun")
            mass *= sunMassMultiplier;

        return Mathf.Max(0.0001f, mass);
    }
}
