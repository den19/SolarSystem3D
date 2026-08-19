using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Cached Resources.Load for probe portrait sprites.
/// </summary>
public static class ProbePortraitLibrary
{
    const string FallbackResourcePath = "ProbePortraits/Custom";

    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
    static Sprite _fallback;

    public static Sprite Get(ProbeModelKind kind)
    {
        string path = ProbeModelCatalog.GetPortraitResourcePath(kind);
        if (string.IsNullOrEmpty(path))
            return GetFallback();

        if (Cache.TryGetValue(path, out Sprite cached) && cached != null)
            return cached;

        Sprite sprite = LoadSprite(path);
        if (sprite == null)
            return GetFallback();

        Cache[path] = sprite;
        return sprite;
    }

    static Sprite LoadSprite(string resourcePath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
            return sprite;

        // Hand-authored .meta files may import as Texture2D before Unity assigns a sub-sprite.
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
            return null;

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    static Sprite GetFallback()
    {
        if (_fallback != null)
            return _fallback;

        _fallback = LoadSprite(FallbackResourcePath);
        return _fallback;
    }

    public static void ClearCache()
    {
        Cache.Clear();
        _fallback = null;
    }
}
