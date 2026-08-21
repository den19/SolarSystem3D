#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds unpacked probe mesh prefabs under Resources with baked URP materials.
/// Menu: Solar System / Build Probe Mesh Prefabs (batch)
/// Also: Solar System / Build New Horizons Prefab, … per model.
/// Batch: -executeMethod ProbeMeshPrefabBuilder.BuildAllAndVerify
/// </summary>
public static class ProbeMeshPrefabBuilder
{
    struct Spec
    {
        public string FolderName;
        public string MenuLabel;
        public Vector3 AntennaLocal;
    }

    static readonly Spec[] Specs =
    {
        new Spec { FolderName = "NewHorizons", MenuLabel = "New Horizons", AntennaLocal = new Vector3(-0.018f, 1.516f, -0.029f) },
        new Spec { FolderName = "Juno", MenuLabel = "Juno", AntennaLocal = new Vector3(-0.075f, 1.681f, 0.043f) },
        new Spec { FolderName = "Venera7", MenuLabel = "Venera 7", AntennaLocal = new Vector3(0.004f, 0.827f, 0.003f) },
        new Spec { FolderName = "Luna1", MenuLabel = "Luna 1", AntennaLocal = new Vector3(0f, 1.144f, 0f) },
        new Spec { FolderName = "Hayabusa2", MenuLabel = "Hayabusa2", AntennaLocal = new Vector3(0f, 0.385f, 0.013f) },
        new Spec { FolderName = "Change4", MenuLabel = "Chang'e 4", AntennaLocal = new Vector3(-0.001f, 0.307f, 0.011f) },
        new Spec { FolderName = "Luna16", MenuLabel = "Luna 16", AntennaLocal = new Vector3(0.018f, 0.66f, 0.014f) },
        new Spec { FolderName = "Tianwen1", MenuLabel = "Tianwen-1", AntennaLocal = new Vector3(0.007f, 0.572f, -0.203f) },
        new Spec { FolderName = "Change5", MenuLabel = "Chang'e 5", AntennaLocal = new Vector3(0.01f, 0.676f, 0.013f) },
        new Spec { FolderName = "Akatsuki", MenuLabel = "Akatsuki", AntennaLocal = new Vector3(0.046f, 0.298f, 0.006f) },
        new Spec { FolderName = "Chandrayaan3", MenuLabel = "Chandrayaan-3", AntennaLocal = new Vector3(0.002f, 0.377f, 0.007f) },
    };

    [MenuItem("Solar System/Build Probe Mesh Prefabs")]
    public static void BuildAllAndVerify()
    {
        int failures = 0;
        for (int i = 0; i < Specs.Length; i++)
        {
            if (!BuildOne(Specs[i], exitOnError: false))
                failures++;
        }

        if (failures > 0)
        {
            Debug.LogError($"ProbeMeshPrefabBuilder: {failures} prefab(s) failed.");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
            return;
        }

        Debug.Log("ProbeMeshPrefabBuilder: all batch prefabs OK.");
        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    [MenuItem("Solar System/Build New Horizons Prefab")]
    public static void BuildNewHorizons() => BuildOneByName("NewHorizons");

    [MenuItem("Solar System/Build Juno Prefab")]
    public static void BuildJuno() => BuildOneByName("Juno");

    [MenuItem("Solar System/Build Venera 7 Prefab")]
    public static void BuildVenera7() => BuildOneByName("Venera7");

    [MenuItem("Solar System/Build Luna 1 Prefab")]
    public static void BuildLuna1() => BuildOneByName("Luna1");

    [MenuItem("Solar System/Build Hayabusa2 Prefab")]
    public static void BuildHayabusa2() => BuildOneByName("Hayabusa2");

    [MenuItem("Solar System/Build Chang'e 4 Prefab")]
    public static void BuildChange4() => BuildOneByName("Change4");

    [MenuItem("Solar System/Build Luna 16 Prefab")]
    public static void BuildLuna16() => BuildOneByName("Luna16");

    [MenuItem("Solar System/Build Tianwen-1 Prefab")]
    public static void BuildTianwen1() => BuildOneByName("Tianwen1");

    [MenuItem("Solar System/Build Chang'e 5 Prefab")]
    public static void BuildChange5() => BuildOneByName("Change5");

    [MenuItem("Solar System/Build Akatsuki Prefab")]
    public static void BuildAkatsuki() => BuildOneByName("Akatsuki");

    [MenuItem("Solar System/Build Chandrayaan-3 Prefab")]
    public static void BuildChandrayaan3() => BuildOneByName("Chandrayaan3");

    static void BuildOneByName(string folder)
    {
        for (int i = 0; i < Specs.Length; i++)
        {
            if (Specs[i].FolderName == folder)
            {
                bool ok = BuildOne(Specs[i], exitOnError: Application.isBatchMode);
                if (Application.isBatchMode)
                    EditorApplication.Exit(ok ? 0 : 1);
                return;
            }
        }

        Debug.LogError($"ProbeMeshPrefabBuilder: unknown folder {folder}");
        if (Application.isBatchMode)
            EditorApplication.Exit(1);
    }

    static bool BuildOne(Spec spec, bool exitOnError)
    {
        string modelAssetPath = $"Assets/Resources/ProbeMeshes/{spec.FolderName}/{spec.FolderName}.obj";
        string prefabAssetPath = $"Assets/Resources/ProbeMeshes/{spec.FolderName}/{spec.FolderName}Prefab.prefab";
        string materialsFolder = $"Assets/Resources/ProbeMeshes/{spec.FolderName}/Materials";
        string albedoFolder = $"Assets/Resources/ProbeMeshes/{spec.FolderName}";
        string prefabResourcePath = $"ProbeMeshes/{spec.FolderName}/{spec.FolderName}Prefab";
        string modelResourcePath = $"ProbeMeshes/{spec.FolderName}/{spec.FolderName}";

        if (!File.Exists(modelAssetPath))
        {
            Debug.LogError($"ProbeMeshPrefabBuilder: missing model at {modelAssetPath}");
            if (exitOnError)
                EditorApplication.Exit(1);
            return false;
        }

        AssetDatabase.ImportAsset(modelAssetPath, ImportAssetOptions.ForceUpdate);
        var modelRoot = AssetDatabase.LoadAssetAtPath<GameObject>(modelAssetPath);
        if (modelRoot == null)
        {
            Debug.LogError($"ProbeMeshPrefabBuilder: failed to load GameObject from {modelAssetPath}");
            if (exitOnError)
                EditorApplication.Exit(2);
            return false;
        }

        EnsureFolder(materialsFolder);
        Dictionary<string, Texture2D> albedos = LoadAlbedoTextures(albedoFolder);
        Shader bodyShader = ResolveBodyShader();
        if (bodyShader == null)
        {
            Debug.LogError("ProbeMeshPrefabBuilder: no suitable body shader found.");
            if (exitOnError)
                EditorApplication.Exit(4);
            return false;
        }

        var root = new GameObject(spec.FolderName + "Prefab");
        try
        {
            GameObject mesh = (GameObject)PrefabUtility.InstantiatePrefab(modelRoot);
            mesh.name = spec.FolderName + "Mesh";
            mesh.transform.SetParent(root.transform, false);
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

            BakeMaterials(mesh, bodyShader, albedos, materialsFolder);

            int meshFilterCount = 0;
            foreach (MeshFilter filter in mesh.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter != null && filter.sharedMesh != null)
                    meshFilterCount++;
            }

            if (meshFilterCount == 0)
            {
                Debug.LogError($"ProbeMeshPrefabBuilder ({spec.FolderName}): no MeshFilters with sharedMesh.");
                if (exitOnError)
                    EditorApplication.Exit(5);
                return false;
            }

            var antenna = new GameObject("Antenna");
            antenna.transform.SetParent(root.transform, false);
            antenna.transform.localPosition = spec.AntennaLocal;
            antenna.transform.localRotation = Quaternion.identity;

            PrefabUtility.SaveAsPrefabAsset(root, prefabAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"ProbeMeshPrefabBuilder: saved {prefabAssetPath} ({meshFilterCount} mesh filters).");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        var loadedPrefab = Resources.Load<GameObject>(prefabResourcePath);
        var loadedModel = Resources.Load<GameObject>(modelResourcePath);
        bool ok = loadedPrefab != null || loadedModel != null;

        int prefabMeshes = 0;
        if (loadedPrefab != null)
        {
            foreach (MeshFilter filter in loadedPrefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter != null && filter.sharedMesh != null)
                    prefabMeshes++;
            }
        }

        Debug.Log(
            $"ProbeMeshPrefabBuilder ({spec.MenuLabel}): " +
            $"Resources.Load('{prefabResourcePath}')={(loadedPrefab != null ? "OK" : "NULL")} " +
            $"meshFilters={prefabMeshes} " +
            $"Resources.Load('{modelResourcePath}')={(loadedModel != null ? "OK" : "NULL")}");

        if (!ok || (loadedPrefab != null && prefabMeshes == 0))
        {
            Debug.LogError($"ProbeMeshPrefabBuilder ({spec.FolderName}): Resources load failed or prefab has no meshes.");
            if (exitOnError)
                EditorApplication.Exit(3);
            return false;
        }

        return true;
    }

    static void BakeMaterials(GameObject meshRoot, Shader bodyShader, Dictionary<string, Texture2D> albedos, string materialsFolder)
    {
        foreach (MeshRenderer renderer in meshRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Material[] source = renderer.sharedMaterials;
            if (source == null || source.Length == 0)
            {
                string fallbackName = SanitizeFileName(renderer.gameObject.name);
                Material baked = CreateOrUpdateMaterialAsset(materialsFolder, fallbackName, bodyShader, FindAlbedo(albedos, fallbackName, renderer.gameObject.name));
                renderer.sharedMaterial = baked;
                continue;
            }

            var rebuilt = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                Material src = source[i];
                string baseName = src != null ? src.name : $"{renderer.gameObject.name}_{i}";
                baseName = SanitizeFileName(baseName);
                Texture2D albedo = null;
                if (src != null)
                {
                    if (src.HasProperty("_BaseMap"))
                        albedo = src.GetTexture("_BaseMap") as Texture2D;
                    if (albedo == null && src.HasProperty("_MainTex"))
                        albedo = src.GetTexture("_MainTex") as Texture2D;
                }

                if (albedo == null)
                    albedo = FindAlbedo(albedos, baseName, renderer.gameObject.name);

                rebuilt[i] = CreateOrUpdateMaterialAsset(materialsFolder, baseName, bodyShader, albedo);
            }

            renderer.sharedMaterials = rebuilt;
        }
    }

    static Material CreateOrUpdateMaterialAsset(string materialsFolder, string baseName, Shader shader, Texture2D albedo)
    {
        string assetPath = $"{materialsFolder}/{baseName}.mat";
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

        Color color = Color.white;
        color.a = 1f;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0.25f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.35f);

        if (albedo != null)
        {
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", albedo);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", albedo);
            if (shader.name.IndexOf("Unlit", System.StringComparison.OrdinalIgnoreCase) >= 0)
                mat.EnableKeyword("_BASEMAP");
            else
                mat.DisableKeyword("_BASEMAP");
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Texture2D FindAlbedo(Dictionary<string, Texture2D> albedos, string materialName, string objectName)
    {
        if (albedos.TryGetValue(NormalizeKey(materialName), out Texture2D byMat))
            return byMat;

        string stripped = materialName;
        if (stripped.StartsWith("mat_"))
            stripped = stripped.Substring(4);
        if (albedos.TryGetValue(NormalizeKey(stripped), out Texture2D byStripped))
            return byStripped;

        if (albedos.TryGetValue(NormalizeKey(objectName), out Texture2D byObj))
            return byObj;

        foreach (KeyValuePair<string, Texture2D> pair in albedos)
        {
            if (NormalizeKey(materialName).Contains(pair.Key) || pair.Key.Contains(NormalizeKey(stripped)))
                return pair.Value;
        }

        return null;
    }

    static Dictionary<string, Texture2D> LoadAlbedoTextures(string albedoFolder)
    {
        var map = new Dictionary<string, Texture2D>();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { albedoFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path) || path.IndexOf("/Materials/", System.StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            if (!path.EndsWith("_albedo.png", System.StringComparison.OrdinalIgnoreCase))
                continue;

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
                continue;

            string file = Path.GetFileNameWithoutExtension(path);
            string key = file;
            const string suffix = "_albedo";
            if (key.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
                key = key.Substring(0, key.Length - suffix.Length);

            map[NormalizeKey(key)] = tex;
            map[NormalizeKey(file)] = tex;
        }

        return map;
    }

    static string NormalizeKey(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Replace(" ", string.Empty).Replace(".", string.Empty).ToLowerInvariant();
    }

    static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "ProbeBody";
        char[] invalid = Path.GetInvalidFileNameChars();
        var chars = name.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            for (int j = 0; j < invalid.Length; j++)
            {
                if (chars[i] == invalid[j])
                {
                    chars[i] = '_';
                    break;
                }
            }
        }

        return new string(chars);
    }

    static Shader ResolveBodyShader()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit != null)
            return lit;
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit != null)
            return unlit;
        return Shader.Find("Standard");
    }

    static void EnsureFolder(string assetFolder)
    {
        assetFolder = assetFolder.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;

        string[] parts = assetFolder.Split('/');
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
