using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class LocalizationFontHelper
{
    const string DefaultFontPath = "Fonts & Materials/LiberationSans SDF";
    const string DefaultMaterialPath = "Fonts & Materials/LiberationSans SDF - Overlay";
    const string LiberationSansSourceFontPath = "Fonts/LiberationSans/LiberationSans-Regular";
    const string ChineseFontResourcePath = "Fonts & Materials/NotoSansSC SDF";
    const string ChineseSourceFontPath = "Fonts/NotoSansSC/NotoSansSC-Regular";

    static TMP_FontAsset defaultFont;
    static Material defaultOverlayMaterial;
    static TMP_FontAsset chineseFont;
    static bool symbolFallbackEnsured;

    public static TMP_FontAsset GetFontForLanguage(Language language)
    {
        return language == Language.Chinese ? GetChineseFont() : GetDefaultFont();
    }

    public static Material GetOverlayMaterialForLanguage(Language language)
    {
        // Dynamic runtime LiberationSans uses its own material; Chinese likewise.
        if (language == Language.Chinese)
            return null;
        if (defaultFont != null && defaultFont.name.Contains("Runtime"))
            return null;
        return GetDefaultOverlayMaterial();
    }

    public static void ApplyFontsForLanguage(Language language)
    {
        TMP_FontAsset font = GetFontForLanguage(language);
        if (font == null)
            return;

        Material material = GetOverlayMaterialForLanguage(language);

        var tmpTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tmp in tmpTexts)
        {
            tmp.font = font;
            if (material != null)
                tmp.fontSharedMaterial = material;
        }
    }

    static TMP_FontAsset GetDefaultFont()
    {
        if (defaultFont != null)
        {
            EnsureSymbolFallback(defaultFont);
            return defaultFont;
        }

        // Static LiberationSans SDF atlas lacks Cyrillic / Tatar extended letters.
        // Prefer a dynamic runtime asset from the TTF so Russian, Belarusian, and Tatar render.
        defaultFont = CreateDynamicLiberationSans();
        if (defaultFont == null)
            defaultFont = Resources.Load<TMP_FontAsset>(DefaultFontPath);

        EnsureSymbolFallback(defaultFont);
        return defaultFont;
    }

    static TMP_FontAsset CreateDynamicLiberationSans()
    {
        var sourceFont = Resources.Load<Font>(LiberationSansSourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogWarning(
                "LocalizationFontHelper: LiberationSans TTF not found in Resources; " +
                "Cyrillic / Tatar glyphs may be missing from the static SDF atlas.");
            return null;
        }

        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic);
        font.name = "LiberationSans SDF (Runtime)";
        return font;
    }

    static Material GetDefaultOverlayMaterial()
    {
        if (defaultOverlayMaterial == null)
            defaultOverlayMaterial = Resources.Load<Material>(DefaultMaterialPath);
        return defaultOverlayMaterial;
    }

    /// <summary>
    /// LiberationSans lacks astronomical glyphs (☉ U+2609, ⊙ U+2299).
    /// NotoSansSC has them — attach it as a TMP fallback so descriptions/titles render correctly.
    /// </summary>
    static void EnsureSymbolFallback(TMP_FontAsset font)
    {
        if (symbolFallbackEnsured || font == null)
            return;

        symbolFallbackEnsured = true;

        TMP_FontAsset symbolFont = GetChineseFont();
        if (symbolFont == null || symbolFont == font)
            return;

        if (font.fallbackFontAssetTable == null)
            font.fallbackFontAssetTable = new List<TMP_FontAsset>();

        if (font.fallbackFontAssetTable.Contains(symbolFont))
            return;

        font.fallbackFontAssetTable.Add(symbolFont);
    }

    static TMP_FontAsset GetChineseFont()
    {
        if (chineseFont != null)
            return chineseFont;

        chineseFont = Resources.Load<TMP_FontAsset>(ChineseFontResourcePath);
        if (chineseFont != null)
            return chineseFont;

        var sourceFont = Resources.Load<Font>(ChineseSourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogWarning("LocalizationFontHelper: Chinese font not found; CJK text may not render.");
            return GetDefaultFont();
        }

        chineseFont = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic);
        chineseFont.name = "NotoSansSC SDF (Runtime)";
        return chineseFont;
    }
}
