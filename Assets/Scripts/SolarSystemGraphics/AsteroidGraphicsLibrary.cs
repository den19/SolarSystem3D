using UnityEngine;

/// <summary>
/// Sun-distance visual stages for the shared Asteroid Ball mesh (3 Sketchfab materials).
/// </summary>
public enum AsteroidHeatVariant
{
    Cold = 0,
    Warm = 1,
    Hot = 2
}

/// <summary>
/// Loads / builds URP Lit materials for asteroid heat variants from Resources textures.
/// </summary>
public static class AsteroidGraphicsLibrary
{
    public const string MeshResourcePath = "AsteroidMeshes/AsteroidBall/AsteroidBall";
    public const string PrefabResourcePath = "AsteroidMeshes/AsteroidBall/AsteroidBallPrefab";
    public const string TextureRoot = "AsteroidMeshes/AsteroidBall/";

    /// <summary>Below this heliocentric AU → Hot (Asteroid_3).</summary>
    public const float HotBelowAu = 0.8f;

    /// <summary>Below this (and ≥ HotBelowAu) → Warm (Asteroid_2); otherwise Cold.</summary>
    public const float WarmBelowAu = 2.2f;

    static Material _cold;
    static Material _warm;
    static Material _hot;
    static Mesh _sharedMesh;

    public static AsteroidHeatVariant Classify(float distanceAu)
    {
        if (distanceAu < HotBelowAu)
            return AsteroidHeatVariant.Hot;
        if (distanceAu < WarmBelowAu)
            return AsteroidHeatVariant.Warm;
        return AsteroidHeatVariant.Cold;
    }

    public static Material LoadVariantMaterial(AsteroidHeatVariant variant)
    {
        EnsureMaterials();
        return variant switch
        {
            AsteroidHeatVariant.Hot => _hot,
            AsteroidHeatVariant.Warm => _warm,
            _ => _cold
        };
    }

    public static Mesh LoadSharedMesh()
    {
        if (_sharedMesh != null)
            return _sharedMesh;

        var prefab = Resources.Load<GameObject>(PrefabResourcePath);
        if (prefab != null)
        {
            var filter = prefab.GetComponentInChildren<MeshFilter>(true);
            if (filter != null && filter.sharedMesh != null)
            {
                _sharedMesh = filter.sharedMesh;
                return _sharedMesh;
            }
        }

        var model = Resources.Load<GameObject>(MeshResourcePath);
        if (model != null)
        {
            var filter = model.GetComponentInChildren<MeshFilter>(true);
            if (filter != null && filter.sharedMesh != null)
            {
                _sharedMesh = filter.sharedMesh;
                return _sharedMesh;
            }
        }

        return null;
    }

    static void EnsureMaterials()
    {
        if (_cold != null && _warm != null && _hot != null)
            return;

        _cold = Resources.Load<Material>(TextureRoot + "Materials/mat_Asteroid_Cold")
                ?? BuildMaterial("Asteroid_Cold", emissiveName: null, ormName: "Asteroid_1_orm");
        _warm = Resources.Load<Material>(TextureRoot + "Materials/mat_Asteroid_Warm")
                ?? BuildMaterial("Asteroid_Warm", emissiveName: "Asteroid_2_emissive", ormName: "Asteroid_2_orm");
        _hot = Resources.Load<Material>(TextureRoot + "Materials/mat_Asteroid_Hot")
               ?? BuildMaterial("Asteroid_Hot", emissiveName: "Asteroid_3_emissive", ormName: "Asteroid_2_orm");
    }

    static Material BuildMaterial(string name, string emissiveName, string ormName)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                        ?? Shader.Find("Standard");
        var mat = new Material(shader) { name = name };

        Texture2D albedo = Resources.Load<Texture2D>(TextureRoot + "Asteroid_albedo");
        Texture2D normal = Resources.Load<Texture2D>(TextureRoot + "Asteroid_normal");
        Texture2D orm = Resources.Load<Texture2D>(TextureRoot + ormName);
        Texture2D emissive = string.IsNullOrEmpty(emissiveName)
            ? null
            : Resources.Load<Texture2D>(TextureRoot + emissiveName);

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);

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
            if (mat.HasProperty("_BumpScale"))
                mat.SetFloat("_BumpScale", 1f);
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

        return mat;
    }
}
