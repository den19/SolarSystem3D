#if UNITY_EDITOR
using System.IO;
using SolarSystemApp;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bakes orthographic probe portraits from procedural meshes into Resources/ProbePortraits/.
/// </summary>
public static class ProbePortraitBaker
{
    const string OutputFolder = "Assets/Resources/ProbePortraits";
    const int Size = 256;
    const int PreviewLayer = 31;

    [MenuItem("Solar System/Bake Probe Portraits")]
    public static void BakeAll()
    {
        EnsureFolder();
        var lightGo = CreateBakeLight();
        var camGo = CreateBakeCamera(out Camera camera, out RenderTexture rt);

        try
        {
            foreach (ProbeModelCatalog.Entry entry in ProbeModelCatalog.Entries)
                BakeOne(entry.Kind, entry.PortraitResourcePath, camera, rt);
        }
        finally
        {
            Object.DestroyImmediate(lightGo);
            Object.DestroyImmediate(camGo);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ConfigureAllImporters();
        AssetDatabase.SaveAssets();
        Debug.Log($"Probe portraits baked to {OutputFolder} ({ProbeModelCatalog.Count} files).");
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "ProbePortraits");
    }

    static GameObject CreateBakeLight()
    {
        var go = new GameObject("ProbePortraitBakeLight");
        go.hideFlags = HideFlags.HideAndDontSave;
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.color = new Color(0.95f, 0.97f, 1f);
        go.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
        return go;
    }

    static GameObject CreateBakeCamera(out Camera camera, out RenderTexture rt)
    {
        var go = new GameObject("ProbePortraitBakeCamera");
        go.hideFlags = HideFlags.HideAndDontSave;
        camera = go.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.07f, 0.14f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 1.35f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 20f;
        camera.cullingMask = 1 << PreviewLayer;
        camera.enabled = false;

        rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        camera.targetTexture = rt;
        return go;
    }

    static void BakeOne(ProbeModelKind kind, string resourcePath, Camera camera, RenderTexture rt)
    {
        ProbeCraft craft = ProbePrefabFactory.Create(kind, highDetail: true);
        craft.transform.position = new Vector3(0f, -500f, 0f);
        craft.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
        SetLayerRecursively(craft.gameObject, PreviewLayer);

        camera.Render();

        string fileName = Path.GetFileName(resourcePath);
        string assetPath = $"{OutputFolder}/{fileName}.png";
        SaveRenderTexture(rt, assetPath);

        Object.DestroyImmediate(craft.gameObject);
    }

    static void SaveRenderTexture(RenderTexture rt, string assetPath)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        File.WriteAllBytes(assetPath, png);
    }

    static void ConfigureAllImporters()
    {
        foreach (ProbeModelCatalog.Entry entry in ProbeModelCatalog.Entries)
        {
            string fileName = Path.GetFileName(entry.PortraitResourcePath);
            string assetPath = $"{OutputFolder}/{fileName}.png";
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsToUnits = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = Size;
            importer.SaveAndReimport();
        }
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
#endif
