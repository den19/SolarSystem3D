using UnityEngine;

/// <summary>
/// Catalog of named asteroids (belt + near-Sun demonstrators).
/// Educational semi-major axes sit between Mars (~26) and Jupiter (~42).
/// </summary>
public static class AsteroidCatalog
{
    public struct AsteroidDefinition
    {
        public string objectName;
        public string labelKey;
        public string prefabResourcePath;
        public float semiMajorAxis;
        public float semiMajorAxisAu;
        public float eccentricity;
        public float inclinationDeg;
        public float simPeriodSec;
        public float phaseOffsetRad;
        public float visualScale;
        public float equatorialRadiusKm;

        public double SiderealPeriodDays => KeplerOrbitMath.PeriodDaysFromAu(semiMajorAxisAu);
    }

    public static bool TryGetByObjectName(string objectName, out AsteroidDefinition definition)
    {
        for (int i = 0; i < Asteroids.Length; i++)
        {
            if (Asteroids[i].objectName == objectName)
            {
                definition = Asteroids[i];
                return true;
            }
        }

        definition = default;
        return false;
    }

    public static readonly AsteroidDefinition[] Asteroids =
    {
        new AsteroidDefinition
        {
            objectName = "Asteroid_Vesta",
            labelKey = "AsteroidVestaLabel",
            prefabResourcePath = "Asteroids/Asteroid_Vesta",
            semiMajorAxis = 29.7f,
            semiMajorAxisAu = 2.362f,
            eccentricity = 0.089f,
            inclinationDeg = 7.14f,
            simPeriodSec = 40f,
            phaseOffsetRad = 0.6f,
            visualScale = 0.38f,
            equatorialRadiusKm = 262.7f
        },
        new AsteroidDefinition
        {
            objectName = "Asteroid_Ceres",
            labelKey = "AsteroidCeresLabel",
            prefabResourcePath = "Asteroids/Asteroid_Ceres",
            semiMajorAxis = 31.5f,
            semiMajorAxisAu = 2.767f,
            eccentricity = 0.076f,
            inclinationDeg = 10.6f,
            simPeriodSec = 48f,
            phaseOffsetRad = 2.1f,
            visualScale = 0.48f,
            equatorialRadiusKm = 473f
        },
        new AsteroidDefinition
        {
            objectName = "Asteroid_Pallas",
            labelKey = "AsteroidPallasLabel",
            prefabResourcePath = "Asteroids/Asteroid_Pallas",
            semiMajorAxis = 31.6f,
            semiMajorAxisAu = 2.772f,
            eccentricity = 0.230f,
            inclinationDeg = 34.8f,
            simPeriodSec = 46f,
            phaseOffsetRad = 4.0f,
            visualScale = 0.36f,
            equatorialRadiusKm = 256f
        },
        // Near-Sun asteroid so Hot/Warm material variants are visible in-sim.
        new AsteroidDefinition
        {
            objectName = "Asteroid_Icarus",
            labelKey = "AsteroidIcarusLabel",
            prefabResourcePath = "Asteroids/Asteroid_Icarus",
            semiMajorAxis = 23.2f,
            semiMajorAxisAu = 1.078f,
            eccentricity = 0.827f,
            inclinationDeg = 22.8f,
            simPeriodSec = 28f,
            phaseOffsetRad = 1.2f,
            visualScale = 0.22f,
            equatorialRadiusKm = 0.7f
        }
    };
}
