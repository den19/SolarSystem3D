using System.Collections.Generic;

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
    public const float GanymedeOrbitKm = 1070400f;

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
            objectName = "Ganymede",
            orbitalRadiusAu = 0f,
            equatorialRadiusKm = 2634.1f,
            orbitCenterName = "Jupiter",
            satelliteOrbitKm = GanymedeOrbitKm,
            orbitalEccentricity = 0.0013f,
            orbitalInclinationDeg = 0.2f
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
}
