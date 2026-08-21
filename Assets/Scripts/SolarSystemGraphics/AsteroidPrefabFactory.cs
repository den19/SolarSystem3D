using UnityEngine;

/// <summary>
/// Builds asteroid visual roots from the shared Asteroid Ball mesh + heat materials.
/// </summary>
public static class AsteroidPrefabFactory
{
    public static GameObject Build(AsteroidCatalog.AsteroidDefinition definition, AsteroidContentData.ContentEntry content)
    {
        var root = new GameObject(definition.objectName);

        var info = root.AddComponent<AsteroidInfo>();
        info.Configure(definition.objectName, definition.labelKey);
        if (!string.IsNullOrEmpty(content.id))
        {
            info.SetDescriptions(
                content.english,
                content.russian,
                content.chinese,
                content.vietnamese,
                content.uzbek,
                content.tatar,
                content.belarusian);
        }

        root.AddComponent<AsteroidVisualController>();
        root.AddComponent<AsteroidOrbitController>();

        var collider = root.AddComponent<SphereCollider>();
        collider.radius = 0.75f;
        collider.center = Vector3.zero;

        CreateMeshChild(root.transform, definition.visualScale);

        return root;
    }

    static void CreateMeshChild(Transform parent, float visualScale)
    {
        var meshGo = new GameObject("AsteroidMesh");
        meshGo.transform.SetParent(parent, false);
        meshGo.transform.localPosition = Vector3.zero;
        meshGo.transform.localRotation = Quaternion.identity;
        meshGo.transform.localScale = Vector3.one * Mathf.Max(0.05f, visualScale);

        var filter = meshGo.AddComponent<MeshFilter>();
        Mesh mesh = AsteroidGraphicsLibrary.LoadSharedMesh();
        if (mesh != null)
            filter.sharedMesh = mesh;
        else
        {
            // Fallback if OBJ not imported yet.
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            filter.sharedMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(sphere);
            else
#endif
                Object.Destroy(sphere);
        }

        var renderer = meshGo.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = AsteroidGraphicsLibrary.LoadVariantMaterial(AsteroidHeatVariant.Cold);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
