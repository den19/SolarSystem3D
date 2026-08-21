#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bakes Asteroid Ball materials + optional per-asteroid prefabs under Resources.
/// Menu: Solar System / Build Asteroid Ball Prefabs
/// Batch: -executeMethod AsteroidBallPrefabBuilder.BuildAndVerify
/// </summary>
public static class AsteroidBallPrefabBuilder
{
    const string ModelAssetPath = "Assets/Resources/AsteroidMeshes/AsteroidBall/AsteroidBall.obj";
    const string PrefabAssetPath = "Assets/Resources/AsteroidMeshes/AsteroidBall/AsteroidBallPrefab.prefab";
    const string MaterialsFolder = "Assets/Resources/AsteroidMeshes/AsteroidBall/Materials";
    const string AsteroidsFolder = "Assets/Resources/Asteroids";
    const string TextureRoot = "Assets/Resources/AsteroidMeshes/AsteroidBall";

    [MenuItem("Solar System/Build Asteroid Ball Prefabs")]
    public static void BuildAndVerify()
    {
        if (!File.Exists(ModelAssetPath))
        {
            Debug.LogError($"AsteroidBallPrefabBuilder: missing model at {ModelAssetPath}");
            EditorApplication.Exit(1);
            return;
        }

        AssetDatabase.ImportAsset(ModelAssetPath, ImportAssetOptions.ForceUpdate);
        var modelRoot = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath);
        if (modelRoot == null)
        {
            Debug.LogError($"AsteroidBallPrefabBuilder: failed to load GameObject from {ModelAssetPath}");
            EditorApplication.Exit(2);
            return;
        }

        EnsureFolder(MaterialsFolder);
        EnsureFolder(AsteroidsFolder);

        Shader bodyShader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Universal Render Pipeline/Simple Lit");
        if (bodyShader == null)
        {
            Debug.LogError("AsteroidBallPrefabBuilder: URP Lit shader not found.");
            EditorApplication.Exit(4);
            return;
        }

        Texture2D albedo = LoadTex("Asteroid_albedo.png");
        Texture2D normal = LoadTex("Asteroid_normal.png");
        Texture2D ormCold = LoadTex("Asteroid_1_orm.png");
        Texture2D ormWarm = LoadTex("Asteroid_2_orm.png");
        Texture2D emisWarm = LoadTex("Asteroid_2_emissive.png");
        Texture2D emisHot = LoadTex("Asteroid_3_emissive.png");

        Material cold = BakeMaterial("mat_Asteroid_Cold", bodyShader, albedo, normal, ormCold, null);
        Material warm = BakeMaterial("mat_Asteroid_Warm", bodyShader, albedo, normal, ormWarm, emisWarm);
        Material hot = BakeMaterial("mat_Asteroid_Hot", bodyShader, albedo, normal, ormWarm, emisHot);

        var sharedRoot = new GameObject("AsteroidBallPrefab");
        try
        {
            GameObject mesh = (GameObject)PrefabUtility.InstantiatePrefab(modelRoot);
            mesh.name = "AsteroidMesh";
            mesh.transform.SetParent(sharedRoot.transform, false);
            mesh.transform.localPosition = Vector3.zero;
            mesh.transform.localRotation = Quaternion.identity;
            mesh.transform.localScale = Vector3.one;

            if (PrefabUtility.IsPartOfPrefabInstance(mesh))
            {
                PrefabUtility.UnpackPrefabInstance(
                    mesh,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }

            foreach (Collider col in mesh.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);

            foreach (MeshRenderer renderer in mesh.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.sharedMaterial = cold;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            PrefabUtility.SaveAsPrefabAsset(sharedRoot, PrefabAssetPath);
        }
        finally
        {
            Object.DestroyImmediate(sharedRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        foreach (AsteroidCatalog.AsteroidDefinition definition in AsteroidCatalog.Asteroids)
            BakeNamedPrefab(definition, cold);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var loaded = Resources.Load<GameObject>("AsteroidMeshes/AsteroidBall/AsteroidBallPrefab");
        bool ok = loaded != null;
        Debug.Log(
            $"AsteroidBallPrefabBuilder: prefab={PrefabAssetPath} " +
            $"Resources.Load={(ok ? "OK" : "NULL")} cold={cold != null} warm={warm != null} hot={hot != null}");

        if (!ok)
        {
            Debug.LogError("AsteroidBallPrefabBuilder: Resources load failed.");
            EditorApplication.Exit(3);
            return;
        }

        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    static void BakeNamedPrefab(AsteroidCatalog.AsteroidDefinition definition, Material cold)
    {
        string path = $"{AsteroidsFolder}/{definition.objectName}.prefab";
        GameObject built = AsteroidPrefabFactory.Build(definition, AsteroidContentData.Get(definition.objectName));
        try
        {
            var renderer = built.GetComponentInChildren<MeshRenderer>(true);
            if (renderer != null)
                renderer.sharedMaterial = cold;
            PrefabUtility.SaveAsPrefabAsset(built, path);
        }
        finally
        {
            Object.DestroyImmediate(built);
        }
    }

    static Material BakeMaterial(
        string name,
        Shader shader,
        Texture2D albedo,
        Texture2D normal,
        Texture2D orm,
        Texture2D emissive)
    {
        string assetPath = $"{MaterialsFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, assetPath);
        }
        else
        {
            mat.shader = shader;
        }

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);
        if (albedo != null)
        {
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", albedo);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", albedo);
        }

        if (normal != null && mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (orm != null)
        {
            if (mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", orm);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            if (mat.HasProperty("_OcclusionMap"))
                mat.SetTexture("_OcclusionMap", orm);
        }

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0.05f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.22f);

        if (emissive != null)
        {
            if (mat.HasProperty("_EmissionMap"))
                mat.SetTexture("_EmissionMap", emissive);
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", Color.white);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", Color.black);
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Texture2D LoadTex(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureRoot}/{fileName}");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
