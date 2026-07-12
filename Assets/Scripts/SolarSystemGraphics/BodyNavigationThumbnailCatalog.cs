using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves navigation list thumbnails from HD planet materials or lightweight fallbacks.
/// </summary>
public static class BodyNavigationThumbnailCatalog
{
    const int CometThumbSize = 64;

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
            return GetCometThumbnail(objectName);

        if (MaterialResourcePaths.TryGetValue(objectName, out string materialPath))
        {
            var material = Resources.Load<Material>(materialPath);
            if (material != null && material.mainTexture != null)
                return material.mainTexture;
        }

        return GetDefaultFallback();
    }

    static Texture GetCometThumbnail(string objectName)
    {
        CometContentData.ContentEntry profile = CometContentData.Get(objectName);
        bool hasProfile = !string.IsNullOrEmpty(profile.id);

        string variant = hasProfile ? profile.nucleusMaterialVariant : "dusty";
        Color comaColor = hasProfile ? profile.comaColor : new Color(0.55f, 0.92f, 0.78f, 1f);
        Vector3 nucleusScale = hasProfile ? profile.nucleusScale : new Vector3(0.4f, 0.3f, 0.5f);

        Color nucleusDark = GetVariantDarkColor(variant);
        Color nucleusLight = GetVariantLightColor(variant);

        // Stable per-comet variation so shared coma colors still look distinct.
        float seed = Hash01(objectName);
        float hueShift = (seed - 0.5f) * 0.08f;
        nucleusDark = ShiftHue(nucleusDark, hueShift);
        nucleusLight = ShiftHue(nucleusLight, hueShift * 0.7f);

        var texture = new Texture2D(CometThumbSize, CometThumbSize, TextureFormat.RGBA32, false);
        texture.name = $"CometThumb_{objectName}";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float centerX = CometThumbSize * 0.58f;
        float centerY = CometThumbSize * 0.5f;
        float baseRadius = CometThumbSize * 0.28f;

        float maxAxis = Mathf.Max(nucleusScale.x, Mathf.Max(nucleusScale.y, nucleusScale.z));
        float minAxis = Mathf.Max(0.05f, Mathf.Min(nucleusScale.x, Mathf.Min(nucleusScale.y, nucleusScale.z)));
        float elongation = Mathf.Clamp(maxAxis / minAxis, 1f, 2.2f);
        float radiusX = baseRadius * Mathf.Lerp(1f, 0.72f, (elongation - 1f) / 1.2f);
        float radiusY = baseRadius * Mathf.Lerp(1f, 1.35f, (elongation - 1f) / 1.2f);

        for (int y = 0; y < CometThumbSize; y++)
        {
            for (int x = 0; x < CometThumbSize; x++)
            {
                float dx = (x - centerX) / radiusX;
                float dy = (y - centerY) / radiusY;
                float ellipseDist = Mathf.Sqrt(dx * dx + dy * dy);

                if (ellipseDist <= 1f)
                {
                    float t = 1f - ellipseDist;
                    float noise = Mathf.PerlinNoise(x * 0.18f + seed * 17f, y * 0.18f + seed * 9f);
                    Color baseColor = Color.Lerp(nucleusDark, nucleusLight, Mathf.Clamp01(t * 0.85f + noise * 0.25f));
                    baseColor *= Mathf.Lerp(0.55f, 1.12f, t);
                    baseColor.a = 1f;
                    texture.SetPixel(x, y, baseColor);
                }
                else
                {
                    float halo = Mathf.Clamp01(1f - (ellipseDist - 1f) / 0.55f);
                    Color haloColor = comaColor;
                    haloColor.a = halo * 0.42f;

                    float tailDx = centerX - x;
                    float tailDy = Mathf.Abs(y - centerY);
                    float tail = 0f;
                    if (tailDx > 0f && tailDy < radiusY * 1.15f)
                    {
                        float along = Mathf.Clamp01(tailDx / (CometThumbSize * 0.52f));
                        float width = 1f - Mathf.Clamp01(tailDy / (radiusY * 1.15f));
                        tail = along * width * width;
                    }

                    Color tailColor = Color.Lerp(comaColor, Color.white, 0.25f);
                    tailColor.a = tail * 0.55f;

                    texture.SetPixel(x, y, BlendPremultiplied(haloColor, tailColor));
                }
            }
        }

        texture.Apply(false, true);
        return texture;
    }

    static Color GetVariantDarkColor(string variant)
    {
        return variant switch
        {
            "ice" => new Color(0.28f, 0.34f, 0.40f, 1f),
            "dusty" => new Color(0.28f, 0.20f, 0.12f, 1f),
            _ => new Color(0.14f, 0.14f, 0.16f, 1f)
        };
    }

    static Color GetVariantLightColor(string variant)
    {
        return variant switch
        {
            "ice" => new Color(0.62f, 0.72f, 0.80f, 1f),
            "dusty" => new Color(0.58f, 0.46f, 0.32f, 1f),
            _ => new Color(0.32f, 0.32f, 0.36f, 1f)
        };
    }

    static Color ShiftHue(Color color, float amount)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        h = Mathf.Repeat(h + amount, 1f);
        Color shifted = Color.HSVToRGB(h, s, v);
        shifted.a = color.a;
        return shifted;
    }

    static Color BlendPremultiplied(Color a, Color b)
    {
        float outA = Mathf.Clamp01(a.a + b.a * (1f - a.a));
        if (outA <= 0.0001f)
            return Color.clear;

        Color result;
        result.r = (a.r * a.a + b.r * b.a * (1f - a.a)) / outA;
        result.g = (a.g * a.a + b.g * b.a * (1f - a.a)) / outA;
        result.b = (a.b * a.a + b.b * b.a * (1f - a.a)) / outA;
        result.a = outA;
        return result;
    }

    static float Hash01(string value)
    {
        unchecked
        {
            int hash = 23;
            for (int i = 0; i < value.Length; i++)
                hash = hash * 31 + value[i];
            return (hash & 0x7fffffff) / (float)int.MaxValue;
        }
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
