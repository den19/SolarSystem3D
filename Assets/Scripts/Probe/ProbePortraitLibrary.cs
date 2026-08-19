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

        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
            return GetFallback();

        Cache[path] = sprite;
        return sprite;
    }

    static Sprite GetFallback()
    {
        if (_fallback != null)
            return _fallback;

        _fallback = Resources.Load<Sprite>(FallbackResourcePath);
        return _fallback;
    }

    public static void ClearCache()
    {
        Cache.Clear();
        _fallback = null;
    }
}
