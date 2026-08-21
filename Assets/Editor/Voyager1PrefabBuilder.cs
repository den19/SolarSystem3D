#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds an unpacked Voyager1Prefab under Resources with baked URP materials.
/// Menu: Solar System / Build Voyager 1 Prefab
/// Batch: -executeMethod Voyager1PrefabBuilder.BuildAndVerify
/// </summary>
public static class Voyager1PrefabBuilder
{
    const string ModelAssetPath = "Assets/Resources/ProbeMeshes/Voyager1/Voyager1.obj";
    const string PrefabAssetPath = "Assets/Resources/ProbeMeshes/Voyager1/Voyager1Prefab.prefab";
    const string MaterialsFolder = "Assets/Resources/ProbeMeshes/Voyager1/Materials";
    const string AlbedoFolder = "Assets/Resources/ProbeMeshes/Voyager1";
    const string PrefabResourcePath = "ProbeMeshes/Voyager1/Voyager1Prefab";
    const string ModelResourcePath = "ProbeMeshes/Voyager1/Voyager1";
    static readonly Vector3 AntennaLocal = new Vector3(-0.007f, 8.12f, 0.54f);

    [MenuItem("Solar System/Build Voyager 1 Prefab")]
    public static void BuildAndVerify()
    {
        if (!File.Exists(ModelAssetPath))
        {
            Debug.LogError($"Voyager1PrefabBuilder: missing model at {ModelAssetPath}");
            EditorApplication.Exit(1);
            return;
        }

        AssetDatabase.ImportAsset(ModelAssetPath, ImportAssetOptions.ForceUpdate);
        var modelRoot = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath);
        if (modelRoot == null)
        {
            Debug.LogError($"Voyager1PrefabBuilder: failed to load GameObject from {ModelAssetPath}");
            EditorApplication.Exit(2);
            return;
        }

        EnsureFolder(MaterialsFolder);
        Dictionary<string, Texture2D> albedos = LoadAlbedoTextures();
        Shader bodyShader = ResolveBodyShader();
        if (bodyShader == null)
        {
            Debug.LogError("Voyager1PrefabBuilder: no suitable body shader found.");
            EditorApplication.Exit(4);
            return;
        }

        var root = new GameObject("Voyager1Prefab");
        try
        {
            GameObject mesh = (GameObject)PrefabUtility.InstantiatePrefab(modelRoot);
            mesh.name = "Voyager1Mesh";
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

            BakeMaterials(mesh, bodyShader, albedos);

            int meshFilterCount = 0;
            foreach (MeshFilter filter in mesh.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter != null && filter.sharedMesh != null)
                    meshFilterCount++;
            }

            if (meshFilterCount == 0)
            {
                Debug.LogError("Voyager1PrefabBuilder: unpacked mesh has no MeshFilters with sharedMesh.");
                EditorApplication.Exit(5);
                return;
            }

            var antenna = new GameObject("Antenna");
            antenna.transform.SetParent(root.transform, false);
            antenna.transform.localPosition = AntennaLocal;
            antenna.transform.localRotation = Quaternion.identity;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Voyager1PrefabBuilder: saved flat prefab with {meshFilterCount} mesh filters.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        var loadedPrefab = Resources.Load<GameObject>(PrefabResourcePath);
        var loadedModel = Resources.Load<GameObject>(ModelResourcePath);
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
            $"Voyager1PrefabBuilder: prefab={PrefabAssetPath} " +
            $"Resources.Load('{PrefabResourcePath}')={(loadedPrefab != null ? "OK" : "NULL")} " +
            $"meshFilters={prefabMeshes} " +
            $"Resources.Load('{ModelResourcePath}')={(loadedModel != null ? "OK" : "NULL")}");

        if (!ok || (loadedPrefab != null && prefabMeshes == 0))
        {
            Debug.LogError("Voyager1PrefabBuilder: Resources load failed or prefab has no meshes.");
            EditorApplication.Exit(3);
            return;
        }

        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    static void BakeMaterials(GameObject meshRoot, Shader bodyShader, Dictionary<string, Texture2D> albedos)
    {
        foreach (MeshRenderer renderer in meshRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Material[] source = renderer.sharedMaterials;
            if (source == null || source.Length == 0)
            {
                string fallbackName = SanitizeFileName(renderer.gameObject.name);
                Material baked = CreateOrUpdateMaterialAsset(fallbackName, bodyShader, FindAlbedo(albedos, fallbackName, renderer.gameObject.name));
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

                rebuilt[i] = CreateOrUpdateMaterialAsset(baseName, bodyShader, albedo);
            }

            renderer.sharedMaterials = rebuilt;
        }
    }

    static Material CreateOrUpdateMaterialAsset(string baseName, Shader shader, Texture2D albedo)
    {
        string assetPath = $"{MaterialsFolder}/{baseName}.mat";
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
            // URP Unlit samples albedo behind _BASEMAP; Lit uses _BaseMap without that keyword.
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

        // MTL names like mat_BODY.040 → BODY.040_albedo.png
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

    static Dictionary<string, Texture2D> LoadAlbedoTextures()
    {
        var map = new Dictionary<string, Texture2D>();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { AlbedoFolder });
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
