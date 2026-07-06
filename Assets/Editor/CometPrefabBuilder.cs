#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to bake comet prefabs and shared materials into Resources.
/// </summary>
public static class CometPrefabBuilder
{
    const string CometsFolder = "Assets/Resources/Comets";
    const string GraphicsFolder = "Assets/Resources/CometGraphics";

    [MenuItem("Tools/Comets/Rebuild All Comet Prefabs And Materials")]
    public static void RebuildAll()
    {
        EnsureFolders();
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
    }

    static void CreateSharedMaterials()
    {
        SaveMaterial("CometNucleus_Dark", CreateNucleusMat(new Color(0.06f, 0.06f, 0.07f)));
        SaveMaterial("CometNucleus_Ice", CreateNucleusMat(new Color(0.12f, 0.15f, 0.18f)));
        SaveMaterial("CometNucleus_Dusty", CreateNucleusMat(new Color(0.14f, 0.1f, 0.07f)));

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
        return mat;
    }

    static Material CreateNucleusMat(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.08f);
        return mat;
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
        ApplyBakedMaterials(instance);

        string path = $"{CometsFolder}/{definition.objectName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
    }

    static void ApplyBakedMaterials(GameObject instance)
    {
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
