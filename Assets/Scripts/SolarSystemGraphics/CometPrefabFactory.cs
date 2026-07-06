using UnityEngine;

/// <summary>
/// Builds comet visual hierarchies at runtime or for editor prefab export.
/// </summary>
public static class CometPrefabFactory
{
    const float VisualScale = 2.8f;

    public static GameObject Build(CometCatalog.CometDefinition definition, CometContentData.ContentEntry content)
    {
        var root = new GameObject(definition.objectName);

        var info = root.AddComponent<CometInfo>();
        info.Configure(definition.objectName, definition.labelKey);
        ApplyContent(info, content);

        var visual = root.AddComponent<CometVisualController>();

        var collider = root.AddComponent<SphereCollider>();
        collider.radius = 1.5f;
        collider.center = Vector3.zero;

        var nucleus = CreateNucleus(root.transform, content.nucleusScale, content.nucleusMaterialVariant);
        var coma = CreateComa(root.transform);
        var dustTail = CreateDustTail(root.transform);
        var ionTail = CreateIonTail(root.transform);

        WireVisualReferences(visual, nucleus, coma, dustTrail: dustTail, ionTail);

        return root;
    }

    static void ApplyContent(CometInfo info, CometContentData.ContentEntry content)
    {
        var so = new SerializedCometInfo(info);
        so.SetDescriptions(
            content.english,
            content.russian,
            content.chinese,
            content.vietnamese,
            content.uzbek);
    }

    static Transform CreateNucleus(Transform parent, Vector3 scale, string materialVariant)
    {
        var nucleus = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nucleus.name = "Nucleus";
        nucleus.transform.SetParent(parent, false);
        nucleus.transform.localScale = scale * VisualScale;

        var col = nucleus.GetComponent<Collider>();
        if (col != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(col);
            else
#endif
                Object.Destroy(col);
        }

        var renderer = nucleus.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = LoadNucleusMaterial(materialVariant);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return nucleus.transform;
    }

    static Material LoadComaMaterialForBuild()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var editorMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/CometGraphics/CometComa.mat");
            if (editorMat != null)
                return editorMat;
        }
#endif
        return CometGraphicsLibrary.LoadComaMaterial();
    }

    static Material LoadTrailMaterialForBuild()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var editorMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/CometGraphics/CometDustTrail.mat");
            if (editorMat != null)
                return editorMat;
        }
#endif
        return CometGraphicsLibrary.LoadDustTrailMaterial();
    }

    static Material LoadIonMaterialForBuild()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var editorMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/CometGraphics/CometIonTailParticle.mat");
            if (editorMat != null)
                return editorMat;
        }
#endif
        return CometGraphicsLibrary.LoadIonMaterial();
    }

    static Transform CreateComa(Transform parent)
    {
        var coma = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coma.name = "Coma";
        coma.transform.SetParent(parent, false);
        coma.transform.localScale = Vector3.one * 1.35f * VisualScale;

        var col = coma.GetComponent<Collider>();
        if (col != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(col);
            else
#endif
                Object.Destroy(col);
        }

        var renderer = coma.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = LoadComaMaterialForBuild();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return coma.transform;
    }

    static TrailRenderer CreateDustTail(Transform parent)
    {
        var dustGo = new GameObject("DustTail");
        dustGo.transform.SetParent(parent, false);
        dustGo.transform.localPosition = Vector3.zero;

        var trail = dustGo.AddComponent<TrailRenderer>();
        trail.time = 2.5f;
        trail.startWidth = 0.18f * VisualScale;
        trail.endWidth = 0.01f;
        trail.minVertexDistance = 0.05f;
        trail.numCapVertices = 4;
        trail.sharedMaterial = LoadTrailMaterialForBuild();
        trail.startColor = new Color(0.92f, 0.95f, 0.15f, 0.85f);
        trail.endColor = new Color(0.98f, 0.97f, 0.28f, 0f);
        trail.emitting = false;
        return trail;
    }

    static LineRenderer CreateIonTail(Transform parent)
    {
        var ionGo = new GameObject("IonTail");
        ionGo.transform.SetParent(parent, false);
        ionGo.transform.localPosition = Vector3.zero;

        var line = ionGo.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.loop = false;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = LoadIonMaterialForBuild();
        line.startWidth = 0.28f * VisualScale;
        line.endWidth = 0.02f * VisualScale;
        line.startColor = new Color(0.35f, 0.72f, 1f, 0.9f);
        line.endColor = new Color(0.5f, 0.85f, 1f, 0f);
        line.SetPosition(0, parent.position);
        line.SetPosition(1, parent.position + Vector3.right * 5f * VisualScale);
        line.enabled = false;
        return line;
    }

    static Material LoadNucleusMaterial(string variant)
    {
        var mat = CometGraphicsLibrary.LoadNucleusMaterial(variant);
        if (mat != null)
            return mat;

        return CreateFallbackNucleusMaterial(variant);
    }

    static Material CreateFallbackNucleusMaterial(string variant)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader);
        Color color = variant switch
        {
            "ice" => new Color(0.14f, 0.16f, 0.18f, 1f),
            "dusty" => new Color(0.1f, 0.09f, 0.08f, 1f),
            _ => new Color(0.08f, 0.08f, 0.09f, 1f)
        };
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.08f);
        return mat;
    }

    static void WireVisualReferences(
        CometVisualController visual,
        Transform nucleus,
        Transform coma,
        TrailRenderer dustTrail,
        LineRenderer ionTail)
    {
        var so = new SerializedCometVisual(visual);
        so.SetReferences(nucleus, coma, dustTrail, ionTail, coma != null ? coma.GetComponent<Renderer>() : null);
    }

    /// <summary>Helper to set private serialized fields without reflection in player builds.</summary>
    internal sealed class SerializedCometInfo
    {
        readonly CometInfo _target;

        public SerializedCometInfo(CometInfo target) => _target = target;

        public void SetDescriptions(string en, string ru, string zh, string vi, string uz)
        {
            _target.SetDescriptions(en, ru, zh, vi, uz);
        }
    }

    internal sealed class SerializedCometVisual
    {
        readonly CometVisualController _target;

        public SerializedCometVisual(CometVisualController target) => _target = target;

        public void SetReferences(Transform nucleus, Transform coma, TrailRenderer dust, LineRenderer ion, Renderer comaRenderer)
        {
            _target.SetReferences(nucleus, coma, dust, ion, comaRenderer);
        }
    }
}
