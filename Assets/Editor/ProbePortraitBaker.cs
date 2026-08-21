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

    /// <summary>Photo-sourced portraits; bake must not overwrite these PNGs.</summary>
    static readonly ProbeModelKind[] SkipBakeKinds =
    {
        ProbeModelKind.Voyager,
        ProbeModelKind.NewHorizons,
        ProbeModelKind.Juno,
        ProbeModelKind.Luna1,
        ProbeModelKind.Venera7,
        ProbeModelKind.Luna16,
    };

    [MenuItem("Solar System/Bake Probe Portraits")]
    public static void BakeAll()
    {
        EnsureFolder();
        var lightGo = CreateBakeLight();
        var camGo = CreateBakeCamera(out Camera camera, out RenderTexture rt);
        int baked = 0;

        try
        {
            foreach (ProbeModelCatalog.Entry entry in ProbeModelCatalog.Entries)
            {
                if (ShouldSkipBake(entry.Kind))
                    continue;

                BakeOne(entry.Kind, entry.PortraitResourcePath, camera, rt);
                baked++;
            }
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
        Debug.Log($"Probe portraits baked to {OutputFolder} ({baked} files, {SkipBakeKinds.Length} photo portraits preserved).");
    }

    /// <summary>Batch: -executeMethod ProbePortraitBaker.BakeMars3</summary>
    [MenuItem("Solar System/Bake Mars 3 Portrait")]
    public static void BakeMars3()
    {
        EnsureFolder();
        var lightGo = CreateBakeLight();
        var camGo = CreateBakeCamera(out Camera camera, out RenderTexture rt);

        try
        {
            BakeOne(ProbeModelKind.Mars3, "ProbePortraits/Mars3", camera, rt);
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
        Debug.Log($"Mars 3 portrait baked to {OutputFolder}/Mars3.png");

        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    [MenuItem("Solar System/Reimport Probe Portraits")]
    public static void ReimportAll()
    {
        EnsureFolder();
        ConfigureAllImporters();
        AssetDatabase.SaveAssets();
        Debug.Log($"Probe portrait importers refreshed in {OutputFolder} ({ProbeModelCatalog.Count} files).");
    }

    static bool ShouldSkipBake(ProbeModelKind kind)
    {
        for (int i = 0; i < SkipBakeKinds.Length; i++)
        {
            if (SkipBakeKinds[i] == kind)
                return true;
        }

        return false;
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
        camera.farClipPlane = 40f;
        camera.cullingMask = 1 << PreviewLayer;
        camera.enabled = false;
        // Frame craft placed at (0, -500, 0): look along +Z toward origin of craft from front-right.
        go.transform.position = new Vector3(0f, -500f, -4.5f);
        go.transform.rotation = Quaternion.identity;

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

        // Hide minimap blip so portraits show the craft mesh, not the cyan marker.
        Transform blip = craft.transform.Find(ProbePrefabFactory.BlipName);
        if (blip != null)
            blip.gameObject.SetActive(false);

        SetLayerRecursively(craft.gameObject, PreviewLayer);

        // Fit orthographic frustum to craft bounds so mesh/prefab silhouettes fill the card.
        Bounds bounds = ComputeWorldBounds(craft.transform);
        if (bounds.size.sqrMagnitude > 1e-6f)
        {
            Vector3 center = bounds.center;
            float half = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            camera.orthographicSize = Mathf.Max(0.6f, half * 1.15f);
            camera.transform.position = new Vector3(center.x, center.y, center.z - Mathf.Max(4.5f, half * 3f));
            camera.transform.rotation = Quaternion.identity;
        }

        camera.Render();

        string fileName = Path.GetFileName(resourcePath);
        string assetPath = $"{OutputFolder}/{fileName}.png";
        SaveRenderTexture(rt, assetPath);

        Object.DestroyImmediate(craft.gameObject);
    }

    static Bounds ComputeWorldBounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        bool has = false;
        Bounds bounds = new Bounds(root.position, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            if (!has)
            {
                bounds = renderers[i].bounds;
                has = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return bounds;
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
