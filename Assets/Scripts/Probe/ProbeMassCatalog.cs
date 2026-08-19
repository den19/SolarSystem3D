/// <summary>
/// Heliocentric mass ratios (Sun = 1) for restricted N-body probe physics.
/// Not the visual R³ proxies used by the gravity-grid mesh.
/// </summary>
public static class ProbeMassCatalog
{
    public static float MassRatioToSun(string objectName)
    {
        switch (objectName)
        {
            case "Sun": return 1f;
            case "Jupiter": return 9.5479e-4f;
            case "Saturn": return 2.8584e-4f;
            case "Neptune": return 5.1514e-5f;
            case "Uranus": return 4.3658e-5f;
            case "Earth": return 3.0035e-6f;
            case "Venus": return 2.4478e-6f;
            case "Mars": return 3.2272e-7f;
            case "Mercury": return 1.6601e-7f;
            case "Ganymede": return 7.8046e-8f;
            case "Titan": return 7.0866e-8f;
            case "Callisto": return 5.6673e-8f;
            case "Io": return 4.7047e-8f;
            case "Moon": return 3.6943e-8f;
            case "Europa": return 2.5280e-8f;
            case "Triton": return 1.0751e-8f;
            case "Pluto": return 7.3100e-9f;
            case "Phobos": return 5.32e-15f;
            case "Deimos": return 7.4e-16f;
            default: return 0f;
        }
    }
}
