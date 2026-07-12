#if UNITY_EDITOR
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to bake comet prefabs and shared materials into Resources.
/// </summary>
public static class CometPrefabBuilder
{
    const string CometsFolder = "Assets/Resources/Comets";
    const string GraphicsFolder = "Assets/Resources/CometGraphics";
    const string TexturesFolder = "Assets/Resources/CometTextures";

    static readonly string DustyTextureUrl =
        "https://www.esa.int/var/esa/storage/images/esa_multimedia/images/2015/01/comet_goose_bumps_a/15206852-1-eng-GB/Comet_goose_bumps_a.jpg";

    [MenuItem("Tools/Comets/Download Comet Textures")]
    public static void DownloadCometTextures()
    {
        EnsureFolders();
        bool downloaded = TryDownloadSourceTexture();
        if (!downloaded)
            GenerateProceduralTextures();

        ConfigureTextureImporter($"{TexturesFolder}/CometNucleus_Dusty_2k.jpg");
        ConfigureTextureImporter($"{TexturesFolder}/CometNucleus_Ice_2k.jpg");
        ConfigureTextureImporter($"{TexturesFolder}/CometNucleus_Dark_2k.jpg");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(downloaded
            ? "Comet textures downloaded and configured."
            : "Comet textures generated procedurally and configured.");
    }

    [MenuItem("Tools/Comets/Rebuild All Comet Prefabs And Materials")]
    public static void RebuildAll()
    {
        EnsureFolders();
        EnsureTexturesExist();
        CreateSharedMaterials();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        foreach (CometCatalog.CometDefinition definition in CometCatalog.Comets)
            BakePrefab(definition);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Comet prefabs and materials rebuilt.");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Comets"))
            AssetDatabase.CreateFolder("Assets/Resources", "Comets");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/CometGraphics"))
            AssetDatabase.CreateFolder("Assets/Resources", "CometGraphics");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/CometTextures"))
            AssetDatabase.CreateFolder("Assets/Resources", "CometTextures");
    }

    static void EnsureTexturesExist()
    {
        string dustyPath = $"{TexturesFolder}/CometNucleus_Dusty_2k.jpg";
        if (File.Exists(dustyPath))
            return;

        if (!TryDownloadSourceTexture())
            GenerateProceduralTextures();
    }

    static bool TryDownloadSourceTexture()
    {
        string dustyPath = $"{TexturesFolder}/CometNucleus_Dusty_2k.jpg";
        string icePath = $"{TexturesFolder}/CometNucleus_Ice_2k.jpg";
        string darkPath = $"{TexturesFolder}/CometNucleus_Dark_2k.jpg";

        try
        {
            string sourcePath = $"{TexturesFolder}/_source_67p.jpg";
            using (var client = new WebClient())
                client.DownloadFile(DustyTextureUrl, sourcePath);

            var source = LoadImageFromDisk(sourcePath);
            if (source == null)
                return false;

            var dusty = TileToSquare(source, 2048);
            var ice = TintTexture(dusty, new Color(0.85f, 0.92f, 1.08f), 1.08f);
            var dark = TintTexture(dusty, new Color(0.55f, 0.55f, 0.58f), 0.55f);

            SaveJpeg(dusty, dustyPath);
            SaveJpeg(ice, icePath);
            SaveJpeg(dark, darkPath);

            if (File.Exists(sourcePath))
                File.Delete(sourcePath);

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Comet texture download failed: {ex.Message}");
            return false;
        }
    }

    static void GenerateProceduralTextures()
    {
        var dusty = GenerateProceduralTexture(2048, new Color(0.22f, 0.20f, 0.16f), new Color(0.50f, 0.44f, 0.36f));
        var ice = TintTexture(dusty, new Color(0.82f, 0.90f, 1.05f), 1.05f);
        var dark = TintTexture(dusty, new Color(0.45f, 0.45f, 0.48f), 0.50f);

        SaveJpeg(dusty, $"{TexturesFolder}/CometNucleus_Dusty_2k.jpg");
        SaveJpeg(ice, $"{TexturesFolder}/CometNucleus_Ice_2k.jpg");
        SaveJpeg(dark, $"{TexturesFolder}/CometNucleus_Dark_2k.jpg");
    }

    static Texture2D LoadImageFromDisk(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        if (!texture.LoadImage(bytes))
            return null;
        return texture;
    }

    static Texture2D TileToSquare(Texture2D source, int size)
    {
        var canvas = new Texture2D(size, size, TextureFormat.RGB24, false);
        int tileW = source.width;
        int tileH = source.height;

        for (int y = 0; y < size; y += tileH)
        {
            for (int x = 0; x < size; x += tileW)
                canvas.SetPixels(x, y, tileW, tileH, source.GetPixels());
        }

        canvas.Apply();
        return canvas;
    }

    static Texture2D TintTexture(Texture2D source, Color tint, float brightness)
    {
        var result = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
        Color[] pixels = source.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            c.r *= tint.r * brightness;
            c.g *= tint.g * brightness;
            c.b *= tint.b * brightness;
            pixels[i] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
        }

        result.SetPixels(pixels);
        result.Apply();
        return result;
    }

    static Texture2D GenerateProceduralTexture(int size, Color low, Color high)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        var pixels = new Color[size * size];
        var random = new System.Random(67);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float n = (float)random.NextDouble() * 0.12f;
                n += Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 0.45f;
                n += Mathf.PerlinNoise(x * 0.08f + 17f, y * 0.08f + 31f) * 0.28f;
                n += Mathf.PerlinNoise(x * 0.2f + 53f, y * 0.2f + 71f) * 0.15f;
                pixels[y * size + x] = Color.Lerp(low, high, Mathf.Clamp01(n));
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    static void SaveJpeg(Texture2D texture, string assetPath)
    {
        byte[] bytes = texture.EncodeToJPG(88);
        string fullPath = Path.GetFullPath(assetPath);
        File.WriteAllBytes(fullPath, bytes);
        Object.DestroyImmediate(texture);
    }

    static void ConfigureTextureImporter(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }

    static void CreateSharedMaterials()
    {
        SaveMaterial("CometNucleus_Dark", CreateNucleusMat("dark", new Color(0.10f, 0.10f, 0.11f)));
        SaveMaterial("CometNucleus_Ice", CreateNucleusMat("ice", new Color(0.20f, 0.24f, 0.28f)));
        SaveMaterial("CometNucleus_Dusty", CreateNucleusMat("dusty", new Color(0.24f, 0.18f, 0.12f)));

        var ion = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"));
        ion.SetColor("_BaseColor", new Color(0.4f, 0.78f, 1f, 1f));
        ion.color = new Color(0.4f, 0.78f, 1f, 1f);
        if (ion.HasProperty("_Surface"))
            ion.SetFloat("_Surface", 1f);
        if (ion.HasProperty("_Blend"))
            ion.SetFloat("_Blend", 1f);
        SaveMaterial("CometIonTailParticle", ion);

        var coma = CreateComaMat();
        SaveMaterial("CometComa", coma);

        var dust = CreateDustTrailMat();
        SaveMaterial("CometDustTrail", dust);
    }

    static Material CreateComaMat()
    {
        var shader = Shader.Find("Custom/AtmosphereRim") ?? Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader);
        mat.SetColor("_AtmosphereColor", new Color(0.55f, 0.92f, 0.78f, 0.28f));
        mat.SetFloat("_RimPower", 3.2f);
        mat.SetFloat("_SunInfluence", 0.75f);
        return mat;
    }

    static Material CreateDustTrailMat()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        mat.SetColor("_BaseColor", new Color(1f, 0.95f, 0.7f, 0.9f));
        mat.color = new Color(1f, 0.95f, 0.7f, 0.9f);
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);
        mat.renderQueue = 3000;
        return mat;
    }

    static Material CreateNucleusMat(string variant, Color tint)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader);
        mat.SetColor("_BaseColor", tint);
        mat.color = tint;

        Texture2D texture = LoadNucleusTexture(variant);
        if (texture != null)
            mat.SetTexture("_BaseMap", texture);

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.12f);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", new Color(0.04f, 0.04f, 0.05f, 1f));
            mat.EnableKeyword("_EMISSION");
        }

        return mat;
    }

    static Texture2D LoadNucleusTexture(string variant)
    {
        string suffix = variant switch
        {
            "ice" => "Ice",
            "dusty" => "Dusty",
            _ => "Dark"
        };

        return AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/CometNucleus_{suffix}_2k.jpg");
    }

    static void SaveMaterial(string name, Material material)
    {
        string path = $"{GraphicsFolder}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            EditorUtility.CopySerialized(material, existing);
        else
            AssetDatabase.CreateAsset(material, path);
    }

    static void BakePrefab(CometCatalog.CometDefinition definition)
    {
        CometContentData.ContentEntry content = CometContentData.Get(definition.objectName);
        GameObject instance = CometPrefabFactory.Build(definition, content);
        instance.name = definition.objectName;
        ApplyBakedMaterials(instance, content);

        string path = $"{CometsFolder}/{definition.objectName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
    }

    static void ApplyBakedMaterials(GameObject instance, CometContentData.ContentEntry content)
    {
        var nucleus = instance.transform.Find("Nucleus")?.GetComponent<Renderer>();
        if (nucleus != null)
        {
            string matName = content.nucleusMaterialVariant switch
            {
                "ice" => "CometNucleus_Ice",
                "dusty" => "CometNucleus_Dusty",
                _ => "CometNucleus_Dark"
            };
            nucleus.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{GraphicsFolder}/{matName}.mat");
        }

        var coma = instance.transform.Find("Coma")?.GetComponent<Renderer>();
        if (coma != null)
            coma.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{GraphicsFolder}/CometComa.mat");

        var dust = instance.transform.Find("DustTail")?.GetComponent<TrailRenderer>();
        if (dust != null)
            dust.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{GraphicsFolder}/CometDustTrail.mat");

        var ion = instance.transform.Find("IonTail")?.GetComponent<LineRenderer>();
        if (ion != null)
            ion.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{GraphicsFolder}/CometIonTailParticle.mat");
    }
}
#endif
