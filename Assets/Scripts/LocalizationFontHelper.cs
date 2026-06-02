using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class LocalizationFontHelper
{
    const string DefaultFontPath = "Fonts & Materials/LiberationSans SDF";
    const string DefaultMaterialPath = "Fonts & Materials/LiberationSans SDF - Overlay";
    const string ChineseFontResourcePath = "Fonts & Materials/NotoSansSC SDF";
    const string ChineseSourceFontPath = "Fonts/NotoSansSC/NotoSansSC-Regular";

    static TMP_FontAsset defaultFont;
    static Material defaultOverlayMaterial;
    static TMP_FontAsset chineseFont;

    public static TMP_FontAsset GetFontForLanguage(Language language)
    {
        return language == Language.Chinese ? GetChineseFont() : GetDefaultFont();
    }

    public static Material GetOverlayMaterialForLanguage(Language language)
    {
        return language == Language.Chinese ? null : GetDefaultOverlayMaterial();
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
        if (defaultFont == null)
            defaultFont = Resources.Load<TMP_FontAsset>(DefaultFontPath);
        return defaultFont;
    }

    static Material GetDefaultOverlayMaterial()
    {
        if (defaultOverlayMaterial == null)
            defaultOverlayMaterial = Resources.Load<Material>(DefaultMaterialPath);
        return defaultOverlayMaterial;
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
