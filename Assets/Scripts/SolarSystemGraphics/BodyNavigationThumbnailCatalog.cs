using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves navigation list thumbnails from HD planet materials or lightweight fallbacks.
/// </summary>
public static class BodyNavigationThumbnailCatalog
{
    static readonly Dictionary<string, string> MaterialResourcePaths = new Dictionary<string, string>
    {
        { "Sun", "PlanetGraphicsHD/SunTexture_HD" },
        { "Mercury", "PlanetGraphicsHD/MercuryTexture_HD" },
        { "Venus", "PlanetGraphicsHD/VenusTexture_HD" },
        { "Earth", "PlanetGraphicsHD/EarthTexture_HD" },
        { "Moon", "PlanetGraphicsHD/MoonTexture_HD" },
        { "Mars", "PlanetGraphicsHD/MarsTexture_HD" },
        { "Phobos", "PlanetGraphicsHD/PhobosTexture_HD" },
        { "Deimos", "PlanetGraphicsHD/DeimosTexture_HD" },
        { "Jupiter", "PlanetGraphicsHD/JupiterTexture_HD" },
        { "Io", "PlanetGraphicsHD/IoTexture_HD" },
        { "Europa", "PlanetGraphicsHD/EuropaTexture_HD" },
        { "Ganymede", "PlanetGraphicsHD/GanymedeTexture_HD" },
        { "Callisto", "PlanetGraphicsHD/CallistoTexture_HD" },
        { "Saturn", "PlanetGraphicsHD/SaturnTexture_HD" },
        { "Titan", "PlanetGraphicsHD/TitanTexture_HD" },
        { "Uranus", "PlanetGraphicsHD/UranusTexture_HD" },
        { "Neptune", "PlanetGraphicsHD/NeptuneTexture_HD" },
        { "Triton", "PlanetGraphicsHD/TritonTexture_HD" }
    };

    static readonly Dictionary<string, Texture> TextureCache = new Dictionary<string, Texture>();
    static Texture _cometFallbackTexture;
    static Texture _defaultFallbackTexture;

    public static Texture GetThumbnail(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return GetDefaultFallback();

        if (TextureCache.TryGetValue(objectName, out Texture cached))
            return cached;

        Texture resolved = ResolveThumbnail(objectName);
        TextureCache[objectName] = resolved;
        return resolved;
    }

    static Texture ResolveThumbnail(string objectName)
    {
        if (objectName.StartsWith("Comet_"))
            return GetCometFallback();

        if (MaterialResourcePaths.TryGetValue(objectName, out string materialPath))
        {
            var material = Resources.Load<Material>(materialPath);
            if (material != null && material.mainTexture != null)
                return material.mainTexture;
        }

        return GetDefaultFallback();
    }

    static Texture GetCometFallback()
    {
        if (_cometFallbackTexture != null)
            return _cometFallbackTexture;

        var loaded = Resources.Load<Texture2D>("CometTextures/CometNucleus_Dusty_2k");
        if (loaded != null)
        {
            _cometFallbackTexture = loaded;
            return _cometFallbackTexture;
        }

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "CometThumbnailFallback";
        var center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.34f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x + 6f;
                float dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= radius)
                {
                    float t = 1f - dist / radius;
                    texture.SetPixel(x, y, Color.Lerp(new Color(0.45f, 0.72f, 0.95f, 1f), new Color(0.92f, 0.97f, 1f, 1f), t));
                }
                else if (x < center.x && Mathf.Abs(y - center.y) < radius * 0.35f)
                {
                    float tail = Mathf.Clamp01(1f - (center.x - x) / (size * 0.42f));
                    texture.SetPixel(x, y, new Color(0.55f, 0.78f, 1f, tail * 0.55f));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        _cometFallbackTexture = texture;
        return _cometFallbackTexture;
    }

    static Texture GetDefaultFallback()
    {
        if (_defaultFallbackTexture != null)
            return _defaultFallbackTexture;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "BodyThumbnailFallback";
        var center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.38f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    float t = 1f - dist / radius;
                    texture.SetPixel(x, y, Color.Lerp(new Color(0.18f, 0.24f, 0.36f, 1f), new Color(0.55f, 0.72f, 0.95f, 1f), t));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        _defaultFallbackTexture = texture;
        return _defaultFallbackTexture;
    }
}
