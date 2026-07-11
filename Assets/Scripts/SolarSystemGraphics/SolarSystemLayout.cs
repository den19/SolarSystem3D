using UnityEngine;

/// <summary>
/// Educational (compressed) layout used when real-distance / real-size toggles are off.
/// Level1 scene baseline is authored with physically consistent proportions at 1 AU = 22 Unity units.
/// </summary>
public static class SolarSystemLayout
{
    public const float DefaultAuToUnity = 22f;
    public const float StandardColliderRadius = 0.5f;

    public struct EducationalEntry
    {
        public Vector3 Scale;
        public float OrbitDistance;
        public Vector3 SatelliteLocalPosition;
        public float PickColliderRadius;
    }

    static readonly System.Collections.Generic.Dictionary<string, EducationalEntry> EducationalByName =
        new System.Collections.Generic.Dictionary<string, EducationalEntry>
        {
            {
                "Sun",
                new EducationalEntry
                {
                    Scale = new Vector3(10f, 10f, 10f),
                    OrbitDistance = 0f,
                    PickColliderRadius = 0.5f
                }
            },
            {
                "Mercury",
                new EducationalEntry
                {
                    Scale = new Vector3(0.3f, 0.3f, 0.3f),
                    OrbitDistance = 10.032f,
                    PickColliderRadius = 6f
                }
            },
            {
                "Venus",
                new EducationalEntry
                {
                    Scale = new Vector3(0.7f, 0.7f, 0.7f),
                    OrbitDistance = 15.021f,
                    PickColliderRadius = 3f
                }
            },
            {
                "Earth",
                new EducationalEntry
                {
                    Scale = Vector3.one,
                    OrbitDistance = 22.034f,
                    PickColliderRadius = 0.7f
                }
            },
            {
                "Mars",
                new EducationalEntry
                {
                    Scale = new Vector3(0.5f, 0.5f, 0.5f),
                    OrbitDistance = 26.002f,
                    PickColliderRadius = 2f
                }
            },
            {
                "Jupiter",
                new EducationalEntry
                {
                    Scale = new Vector3(5f, 5f, 5f),
                    OrbitDistance = 42.042f,
                    PickColliderRadius = 1f
                }
            },
            {
                "Saturn",
                new EducationalEntry
                {
                    Scale = new Vector3(5f, 5f, 5f),
                    OrbitDistance = 67.031f,
                    PickColliderRadius = 1f
                }
            },
            {
                "Uranus",
                new EducationalEntry
                {
                    Scale = new Vector3(4f, 4f, 4f),
                    OrbitDistance = 84.991f,
                    PickColliderRadius = 1f
                }
            },
            {
                "Neptune",
                new EducationalEntry
                {
                    Scale = new Vector3(4f, 4f, 4f),
                    OrbitDistance = 98.021f,
                    PickColliderRadius = 1f
                }
            },
            {
                "Moon",
                new EducationalEntry
                {
                    Scale = new Vector3(0.2f, 0.2f, 0.2f),
                    OrbitDistance = 1.393f,
                    SatelliteLocalPosition = new Vector3(-0.05f, -0.055f, 1.39f),
                    PickColliderRadius = 3f
                }
            },
            {
                "Titan",
                new EducationalEntry
                {
                    Scale = new Vector3(0.28f, 0.28f, 0.28f),
                    OrbitDistance = 2.5f,
                    SatelliteLocalPosition = new Vector3(0f, 0f, 2.5f),
                    PickColliderRadius = 1f
                }
            },
            {
                "Io",
                new EducationalEntry
                {
                    Scale = new Vector3(0.19f, 0.19f, 0.19f),
                    OrbitDistance = 0.79f,
                    SatelliteLocalPosition = new Vector3(0f, 0f, 0.79f),
                    PickColliderRadius = 1f
                }
            },
            {
                "Europa",
                new EducationalEntry
                {
                    Scale = new Vector3(0.17f, 0.17f, 0.17f),
                    OrbitDistance = 1.25f,
                    SatelliteLocalPosition = new Vector3(0f, 0f, 1.25f),
                    PickColliderRadius = 1f
                }
            },
            {
                "Ganymede",
                new EducationalEntry
                {
                    Scale = new Vector3(0.28f, 0.28f, 0.28f),
                    OrbitDistance = 2.0f,
                    SatelliteLocalPosition = new Vector3(0f, 0f, 2.0f),
                    PickColliderRadius = 1f
                }
            },
            {
                "Callisto",
                new EducationalEntry
                {
                    Scale = new Vector3(0.26f, 0.26f, 0.26f),
                    OrbitDistance = 3.5f,
                    SatelliteLocalPosition = new Vector3(0f, 0f, 3.5f),
                    PickColliderRadius = 1f
                }
            },
            {
                "Phobos",
                new EducationalEntry
                {
                    Scale = new Vector3(0.14f, 0.14f, 0.14f),
                    OrbitDistance = 0.95f,
                    SatelliteLocalPosition = new Vector3(0f, 0f, 0.95f),
                    PickColliderRadius = 2.5f
                }
            },
            {
                "Deimos",
                new EducationalEntry
                {
                    Scale = new Vector3(0.08f, 0.08f, 0.08f),
                    OrbitDistance = 2.4f,
                    SatelliteLocalPosition = new Vector3(0f, 0f, 2.4f),
                    PickColliderRadius = 2.0f
                }
            },
            {
                "Triton",
                new EducationalEntry
                {
                    Scale = new Vector3(0.15f, 0.15f, 0.15f),
                    OrbitDistance = 2.2f,
                    SatelliteLocalPosition = new Vector3(0f, 0f, 2.2f),
                    PickColliderRadius = 1f
                }
            }
        };

    public static bool TryGetEducational(string objectName, out EducationalEntry entry)
    {
        return EducationalByName.TryGetValue(objectName, out entry);
    }
}
