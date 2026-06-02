#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class CreateNotoSansScFontAsset
{
    const string SourceFontPath = "Assets/Resources/Fonts/NotoSansSC/NotoSansSC-Regular.ttf";
    const string OutputPath = "Assets/Resources/Fonts & Materials/NotoSansSC SDF.asset";

    [MenuItem("Tools/Localization/Create NotoSansSC TMP Font Asset")]
    public static void Create()
    {
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError($"Source font not found: {SourceFontPath}");
            return;
        }

        var fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic);

        fontAsset.name = "NotoSansSC SDF";

        var dir = System.IO.Path.GetDirectoryName(OutputPath);
        if (!AssetDatabase.IsValidFolder(dir))
            System.IO.Directory.CreateDirectory(dir);

        AssetDatabase.CreateAsset(fontAsset, OutputPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created TMP font asset at {OutputPath}");
    }
}
#endif
