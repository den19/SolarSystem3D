using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Procedural stylized probe meshes (no FBX).
/// </summary>
public static class ProbePrefabFactory
{
    public const string RootName = "SlingshotProbe";
    public const string BlipName = "MinimapBlip";

    public static ProbeCraft Create(ProbeModelKind kind, bool highDetail)
    {
        var root = new GameObject(RootName);
        var craft = root.AddComponent<ProbeCraft>();
        craft.Configure(kind);

        Material busMat = CreateLit(new Color(0.78f, 0.76f, 0.7f), 0.35f, 0.45f);
        Material goldMat = CreateLit(new Color(0.85f, 0.68f, 0.28f), 0.55f, 0.5f);
        Material dishMat = CreateLit(new Color(0.92f, 0.93f, 0.96f), 0.2f, 0.65f);
        Material darkMat = CreateLit(new Color(0.18f, 0.18f, 0.2f), 0.1f, 0.3f);
        Material cyanMat = CreateUnlit(new Color(0.2f, 0.9f, 1f, 0.85f));

        switch (kind)
        {
            case ProbeModelKind.NewHorizons:
                BuildNewHorizons(root.transform, busMat, goldMat, dishMat, darkMat, highDetail);
                break;
            case ProbeModelKind.Juno:
                BuildJuno(root.transform, busMat, goldMat, darkMat, highDetail);
                break;
            case ProbeModelKind.Custom:
                BuildCustom(root.transform, busMat, goldMat, dishMat, darkMat, highDetail);
                break;
            case ProbeModelKind.Luna1:
                BuildLuna1(root.transform, busMat, goldMat, darkMat, highDetail);
                break;
            case ProbeModelKind.Venera7:
                BuildVenera7(root.transform, busMat, goldMat, dishMat, darkMat, highDetail);
                break;
            case ProbeModelKind.Luna16:
                BuildLuna16(root.transform, busMat, goldMat, darkMat, highDetail);
                break;
            case ProbeModelKind.Mars3:
                BuildMars3(root.transform, busMat, goldMat, dishMat, darkMat, highDetail);
                break;
            case ProbeModelKind.Change4:
                BuildChange4(root.transform, busMat, goldMat, darkMat, highDetail);
                break;
            case ProbeModelKind.Tianwen1:
                BuildTianwen1(root.transform, busMat, goldMat, dishMat, darkMat, highDetail);
                break;
            case ProbeModelKind.Change5:
                BuildChange5(root.transform, busMat, goldMat, darkMat, highDetail);
                break;
            case ProbeModelKind.Hayabusa2:
                BuildHayabusa2(root.transform, busMat, goldMat, darkMat, highDetail);
                break;
            case ProbeModelKind.Akatsuki:
                BuildAkatsuki(root.transform, busMat, goldMat, dishMat, highDetail);
                break;
            case ProbeModelKind.Chandrayaan3:
                BuildChandrayaan3(root.transform, busMat, goldMat, darkMat, highDetail);
                break;
            default:
                BuildVoyager(root.transform, busMat, goldMat, dishMat, darkMat, highDetail);
                break;
        }

        var blip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        blip.name = BlipName;
        blip.transform.SetParent(root.transform, false);
        blip.transform.localScale = Vector3.one * 1.5f;
        Object.Destroy(blip.GetComponent<Collider>());
        var blipRenderer = blip.GetComponent<MeshRenderer>();
        blipRenderer.sharedMaterial = cyanMat;
        blipRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var sphere = root.AddComponent<SphereCollider>();
        sphere.radius = 0.45f;
        sphere.isTrigger = true;
        SetLayerRecursively(root, 0);

        craft.Antenna = craftAntenna;
        craftAntenna = null;
        return craft;
    }

    static void BuildVoyager(Transform parent, Material bus, Material gold, Material dish, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cube, bus, Vector3.zero, new Vector3(0.55f, 0.35f, 0.55f));
        Transform hga = AddPrimitive(parent, PrimitiveType.Cylinder, dish, new Vector3(0f, 0.12f, -0.55f), new Vector3(1.1f, 0.04f, 1.1f));
        hga.localRotation = Quaternion.Euler(90f, 0f, 0f);
        craftAntenna = hga;
        AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0.55f, 0f, 0.15f), new Vector3(0.12f, 0.28f, 0.12f));
        if (highDetail)
        {
            Transform boom = AddPrimitive(parent, PrimitiveType.Cylinder, gold, new Vector3(1.1f, 0f, 0f), new Vector3(0.04f, 1.1f, 0.04f));
            boom.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
    }

    static Transform craftAntenna;

    static void BuildNewHorizons(Transform parent, Material bus, Material gold, Material dish, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cube, gold, Vector3.zero, new Vector3(0.7f, 0.28f, 0.5f));
        Transform hga = AddPrimitive(parent, PrimitiveType.Cylinder, dish, new Vector3(0f, 0.08f, -0.42f), new Vector3(0.85f, 0.035f, 0.85f));
        hga.localRotation = Quaternion.Euler(90f, 0f, 0f);
        craftAntenna = hga;
        AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0.48f, 0f, 0.05f), new Vector3(0.1f, 0.22f, 0.1f));
        if (highDetail)
            AddPrimitive(parent, PrimitiveType.Cube, bus, new Vector3(0f, 0.22f, 0.05f), new Vector3(0.25f, 0.08f, 0.25f));
    }

    static void BuildJuno(Transform parent, Material bus, Material gold, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cylinder, bus, Vector3.zero, new Vector3(0.45f, 0.22f, 0.45f));
        craftAntenna = AddPrimitive(parent, PrimitiveType.Sphere, gold, new Vector3(0f, 0.28f, 0f), Vector3.one * 0.18f);
        int wings = highDetail ? 3 : 3;
        for (int i = 0; i < wings; i++)
        {
            float yaw = i * 120f;
            var wing = AddPrimitive(parent, PrimitiveType.Cube, dark, Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.95f, 0f, 0f), new Vector3(1.4f, 0.03f, 0.42f));
            wing.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }

    static void BuildCustom(Transform parent, Material bus, Material gold, Material dish, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cube, bus, Vector3.zero, new Vector3(0.5f, 0.32f, 0.5f));
        if (ProbeSettings.ResolveHasAntenna())
        {
            Transform hga = AddPrimitive(parent, PrimitiveType.Cylinder, dish, new Vector3(0f, 0.1f, -0.5f), new Vector3(0.95f, 0.03f, 0.95f));
            hga.localRotation = Quaternion.Euler(90f, 0f, 0f);
            craftAntenna = hga;
        }
        else
        {
            craftAntenna = null;
        }

        if (ProbeSettings.ResolveHasEngine())
            AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0f, 0f, 0.42f), new Vector3(0.18f, 0.16f, 0.18f));

        if (ProbeSettings.ResolveHasShield())
        {
            Transform shield = AddPrimitive(parent, PrimitiveType.Cylinder, gold, new Vector3(0f, 0f, -0.22f), new Vector3(0.7f, 0.02f, 0.7f));
            shield.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        if (highDetail)
            AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(0.32f, 0.18f, 0f), new Vector3(0.12f, 0.12f, 0.12f));
    }

    static void BuildLuna1(Transform parent, Material bus, Material gold, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cylinder, bus, Vector3.zero, new Vector3(0.38f, 0.5f, 0.38f));
        craftAntenna = AddPrimitive(parent, PrimitiveType.Cylinder, gold, new Vector3(0f, 0.38f, 0f), new Vector3(0.06f, 0.55f, 0.06f));
        for (int i = 0; i < 4; i++)
        {
            float yaw = i * 90f;
            var leg = AddPrimitive(parent, PrimitiveType.Cylinder, dark,
                Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.42f, -0.05f, 0f),
                new Vector3(0.03f, 0.35f, 0.03f));
            leg.localRotation = Quaternion.Euler(70f, yaw, 0f);
        }
    }

    static void BuildVenera7(Transform parent, Material bus, Material gold, Material dish, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Sphere, bus, new Vector3(0f, 0.08f, 0f), new Vector3(0.55f, 0.45f, 0.55f));
        Transform hga = AddPrimitive(parent, PrimitiveType.Cylinder, dish, new Vector3(0f, 0.38f, 0f), new Vector3(0.5f, 0.03f, 0.5f));
        hga.localRotation = Quaternion.Euler(90f, 0f, 0f);
        craftAntenna = hga;
        for (int i = 0; i < 3; i++)
        {
            float yaw = i * 120f;
            AddPrimitive(parent, PrimitiveType.Cylinder, dark,
                Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.32f, -0.18f, 0f),
                new Vector3(0.05f, 0.22f, 0.05f));
        }
    }

    static void BuildLuna16(Transform parent, Material bus, Material gold, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cylinder, bus, Vector3.zero, new Vector3(0.62f, 0.38f, 0.62f));
        craftAntenna = AddPrimitive(parent, PrimitiveType.Cylinder, gold, new Vector3(0.38f, 0.12f, 0f), new Vector3(0.08f, 0.45f, 0.08f));
        AddPrimitive(parent, PrimitiveType.Cube, dark, new Vector3(0f, 0.22f, 0.28f), new Vector3(0.22f, 0.12f, 0.18f));
    }

    static void BuildMars3(Transform parent, Material bus, Material gold, Material dish, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cube, bus, Vector3.zero, new Vector3(0.65f, 0.08f, 0.65f));
        for (int i = 0; i < 4; i++)
        {
            float yaw = 45f + i * 90f;
            var petal = AddPrimitive(parent, PrimitiveType.Cube, gold,
                Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.06f, 0.48f),
                new Vector3(0.38f, 0.02f, 0.38f));
            petal.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        craftAntenna = AddPrimitive(parent, PrimitiveType.Cylinder, dish, new Vector3(0f, 0.28f, 0f), new Vector3(0.12f, 0.35f, 0.12f));
        AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0f, 0.14f, 0f), new Vector3(0.1f, 0.14f, 0.1f));
    }

    static void BuildChange4(Transform parent, Material bus, Material gold, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cube, bus, Vector3.zero, new Vector3(0.75f, 0.14f, 0.55f));
        AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(-0.62f, 0.06f, 0f), new Vector3(0.35f, 0.02f, 0.48f));
        AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(0.62f, 0.06f, 0f), new Vector3(0.35f, 0.02f, 0.48f));
        craftAntenna = AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0f, 0.22f, -0.18f), new Vector3(0.08f, 0.2f, 0.08f));
        if (highDetail)
            AddPrimitive(parent, PrimitiveType.Cube, dark, new Vector3(0.28f, 0.1f, 0.18f), new Vector3(0.18f, 0.12f, 0.16f));
    }

    static void BuildTianwen1(Transform parent, Material bus, Material gold, Material dish, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cube, bus, Vector3.zero, new Vector3(0.48f, 0.32f, 0.48f));
        Transform hga = AddPrimitive(parent, PrimitiveType.Cylinder, dish, new Vector3(0f, 0.08f, -0.52f), new Vector3(1.05f, 0.035f, 1.05f));
        hga.localRotation = Quaternion.Euler(90f, 0f, 0f);
        craftAntenna = hga;
        AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0.42f, 0f, 0.12f), new Vector3(0.1f, 0.24f, 0.1f));
        if (highDetail)
            AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(-0.38f, 0.18f, 0.1f), new Vector3(0.28f, 0.06f, 0.22f));
    }

    static void BuildChange5(Transform parent, Material bus, Material gold, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cylinder, bus, Vector3.zero, new Vector3(0.42f, 0.55f, 0.42f));
        AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(-0.55f, 0.15f, 0f), new Vector3(0.42f, 0.02f, 0.32f));
        AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(0.55f, 0.15f, 0f), new Vector3(0.42f, 0.02f, 0.32f));
        craftAntenna = AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0f, 0.42f, 0.15f), new Vector3(0.07f, 0.28f, 0.07f));
    }

    static void BuildHayabusa2(Transform parent, Material bus, Material gold, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cube, bus, Vector3.zero, new Vector3(0.58f, 0.38f, 0.58f));
        craftAntenna = AddPrimitive(parent, PrimitiveType.Cylinder, gold, new Vector3(0f, 0.05f, 0.52f), new Vector3(0.14f, 0.32f, 0.14f));
        AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0f, -0.08f, 0.38f), new Vector3(0.1f, 0.18f, 0.1f));
        if (highDetail)
            AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(0.38f, 0.12f, 0f), new Vector3(0.22f, 0.08f, 0.12f));
    }

    static void BuildAkatsuki(Transform parent, Material bus, Material gold, Material dish, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cube, bus, Vector3.zero, new Vector3(0.52f, 0.42f, 0.52f));
        craftAntenna = AddPrimitive(parent, PrimitiveType.Cylinder, dish, new Vector3(0.55f, 0.18f, 0f), new Vector3(0.04f, 0.95f, 0.04f));
        craftAntenna.localRotation = Quaternion.Euler(0f, 0f, 75f);
        AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(-0.42f, 0.08f, 0f), new Vector3(0.28f, 0.02f, 0.38f));
    }

    static void BuildChandrayaan3(Transform parent, Material bus, Material gold, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cube, bus, Vector3.zero, new Vector3(0.55f, 0.22f, 0.55f));
        for (int i = 0; i < 3; i++)
        {
            float yaw = i * 120f;
            var panel = AddPrimitive(parent, PrimitiveType.Cube, gold,
                Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.12f, 0.58f),
                new Vector3(0.32f, 0.02f, 0.38f));
            panel.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        craftAntenna = AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0f, 0.28f, 0f), new Vector3(0.08f, 0.22f, 0.08f));
        if (highDetail)
            AddPrimitive(parent, PrimitiveType.Cube, dark, new Vector3(0.32f, 0.14f, 0.22f), new Vector3(0.16f, 0.1f, 0.14f));
    }

    static Transform AddPrimitive(Transform parent, PrimitiveType type, Material material, Vector3 localPos, Vector3 localScale)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        Object.Destroy(go.GetComponent<Collider>());
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.transform;
    }

    static Material CreateLit(Color color, float metallic, float smoothness)
    {
        Shader shader = ResolveBodyShader();
        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    static bool _shaderWarningLogged;

    static Shader ResolveUnlitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
            return shader;

        shader = Shader.Find("Mobile/Unlit (Supports Lightmap)");
        if (shader != null)
            return shader;

        shader = Shader.Find("Sprites/Default");
        if (shader != null)
            return shader;

        if (!_shaderWarningLogged)
        {
            _shaderWarningLogged = true;
            Debug.LogWarning("ProbePrefabFactory: no unlit shader found (URP Unlit, Mobile/Unlit, Sprites/Default).");
        }

        return null;
    }

    static Shader ResolveBodyShader()
    {
        bool preferUnlit = Application.isMobilePlatform || !GraphicsTierSettings.IsHighEffective;
        if (preferUnlit)
        {
            Shader unlit = ResolveUnlitShader();
            if (unlit != null)
                return unlit;
        }

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit != null)
            return lit;
        return Shader.Find("Standard");
    }

    static Material CreateUnlit(Color color)
    {
        Shader shader = ResolveUnlitShader();
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        return material;
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
