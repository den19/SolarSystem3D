using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Probe meshes: Voyager 1 / Mars 3 / New Horizons / Juno / Venera 7 / Luna 1 / Hayabusa2 / Chang'e 4
/// from Resources when available; other kinds are procedural primitives.
/// </summary>
public static class ProbePrefabFactory
{
    public const string RootName = "SlingshotProbe";
    public const string BlipName = "MinimapBlip";
    /// <summary>User layer for minimap-only blip spheres (must match TagManager).</summary>
    public const int MinimapBlipLayer = 8;
    public const string MinimapBlipLayerName = "MinimapBlip";

    const float DefaultMeshFitSize = 1.6f;

    /// <summary>Resources path for Voyager 1 prefab (Editor-built) or imported OBJ root.</summary>
    public const string Voyager1PrefabResourcePath = "ProbeMeshes/Voyager1/Voyager1";
    public const string Voyager1PrefabAltResourcePath = "ProbeMeshes/Voyager1/Voyager1Prefab";
    /// <summary>VTAD model HGA tip in mesh-local space (Y-up dish); Visual wrapper rotates to craft frame.</summary>
    static readonly Vector3 Voyager1AntennaLocal = new Vector3(-0.007f, 8.12f, 0.54f);
    static readonly Vector3 Voyager1VisualEuler = new Vector3(90f, 0f, 0f);
    static bool _voyagerMeshFallbackWarned;

    /// <summary>Resources path for Mars 3 prefab (Editor-built) or imported OBJ root.</summary>
    public const string Mars3PrefabResourcePath = "ProbeMeshes/Mars3/Mars3";
    public const string Mars3PrefabAltResourcePath = "ProbeMeshes/Mars3/Mars3Prefab";
    /// <summary>HGA tip in mesh-local space (Y-up); from Tools/_mars3_import mesh_info.json.</summary>
    static readonly Vector3 Mars3AntennaLocal = new Vector3(0.01f, 0.699f, -0.097f);
    static readonly Vector3 Mars3VisualEuler = new Vector3(0f, 0f, 0f);
    static bool _mars3MeshFallbackWarned;

    public const string NewHorizonsPrefabResourcePath = "ProbeMeshes/NewHorizons/NewHorizons";
    public const string NewHorizonsPrefabAltResourcePath = "ProbeMeshes/NewHorizons/NewHorizonsPrefab";
    static readonly Vector3 NewHorizonsAntennaLocal = new Vector3(-0.018f, 1.516f, -0.029f);
    static bool _newHorizonsMeshFallbackWarned;

    public const string JunoPrefabResourcePath = "ProbeMeshes/Juno/Juno";
    public const string JunoPrefabAltResourcePath = "ProbeMeshes/Juno/JunoPrefab";
    static readonly Vector3 JunoAntennaLocal = new Vector3(-0.075f, 1.681f, 0.043f);
    static bool _junoMeshFallbackWarned;

    public const string Venera7PrefabResourcePath = "ProbeMeshes/Venera7/Venera7";
    public const string Venera7PrefabAltResourcePath = "ProbeMeshes/Venera7/Venera7Prefab";
    static readonly Vector3 Venera7AntennaLocal = new Vector3(0.004f, 0.827f, 0.003f);
    static bool _venera7MeshFallbackWarned;

    public const string Luna1PrefabResourcePath = "ProbeMeshes/Luna1/Luna1";
    public const string Luna1PrefabAltResourcePath = "ProbeMeshes/Luna1/Luna1Prefab";
    static readonly Vector3 Luna1AntennaLocal = new Vector3(0f, 1.144f, 0f);
    static bool _luna1MeshFallbackWarned;

    public const string Hayabusa2PrefabResourcePath = "ProbeMeshes/Hayabusa2/Hayabusa2";
    public const string Hayabusa2PrefabAltResourcePath = "ProbeMeshes/Hayabusa2/Hayabusa2Prefab";
    static readonly Vector3 Hayabusa2AntennaLocal = new Vector3(0f, 0.385f, 0.013f);
    static bool _hayabusa2MeshFallbackWarned;

    public const string Change4PrefabResourcePath = "ProbeMeshes/Change4/Change4";
    public const string Change4PrefabAltResourcePath = "ProbeMeshes/Change4/Change4Prefab";
    static readonly Vector3 Change4AntennaLocal = new Vector3(-0.001f, 0.307f, 0.011f);
    static bool _change4MeshFallbackWarned;

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
        blip.layer = ResolveMinimapBlipLayer();
        EnsureMinimapBlipCameraCulling();

        craft.Antenna = craftAntenna;
        craftAntenna = null;
        return craft;
    }

    static void BuildVoyager(Transform parent, Material bus, Material gold, Material dish, Material dark, bool highDetail)
    {
        if (TryBuildVoyagerFromMesh(parent))
            return;

        if (!_voyagerMeshFallbackWarned)
        {
            _voyagerMeshFallbackWarned = true;
            Debug.LogWarning(
                "ProbePrefabFactory: Voyager 1 mesh unavailable or empty; using primitive fallback. " +
                $"Tried Resources '{Voyager1PrefabAltResourcePath}' then '{Voyager1PrefabResourcePath}'.");
        }

        AddPrimitive(parent, PrimitiveType.Cube, bus, Vector3.zero, new Vector3(0.55f, 0.35f, 0.55f));
        Transform hga = AddPrimitive(parent, PrimitiveType.Cylinder, dish, new Vector3(0f, 0.12f, -0.55f), new Vector3(1.1f, 0.04f, 1.1f));
        hga.localRotation = Quaternion.Euler(90f, 0f, 0f);
        craftAntenna = hga;
        AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0.55f, 0f, 0.15f), new Vector3(0.12f, 0.28f, 0.12f));
        AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(-0.38f, 0.05f, 0.12f), new Vector3(0.08f, 0.22f, 0.08f));
        if (highDetail)
        {
            Transform boom = AddPrimitive(parent, PrimitiveType.Cylinder, gold, new Vector3(1.1f, 0f, 0f), new Vector3(0.04f, 1.1f, 0.04f));
            boom.localRotation = Quaternion.Euler(0f, 0f, 90f);
            AddPrimitive(parent, PrimitiveType.Cube, dark, new Vector3(0f, 0.22f, 0.18f), new Vector3(0.18f, 0.06f, 0.12f));
            AddPrimitive(parent, PrimitiveType.Cylinder, gold, new Vector3(-1.05f, 0f, 0f), new Vector3(0.03f, 0.85f, 0.03f)).localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
    }

    static bool TryBuildVoyagerFromMesh(Transform parent)
    {
        // Prefer Editor-built flat prefab, then fall back to imported OBJ root.
        GameObject prefab = Resources.Load<GameObject>(Voyager1PrefabAltResourcePath);
        if (prefab == null)
            prefab = Resources.Load<GameObject>(Voyager1PrefabResourcePath);
        if (prefab == null)
            return false;

        var wrap = new GameObject("Voyager1Visual");
        wrap.transform.SetParent(parent, false);
        wrap.transform.localRotation = Quaternion.Euler(Voyager1VisualEuler);

        GameObject instance = Object.Instantiate(prefab, wrap.transform, false);
        instance.name = "Voyager1Mesh";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        if (!HasValidMeshFilters(instance))
        {
            Object.Destroy(wrap);
            return false;
        }

        foreach (Collider col in instance.GetComponentsInChildren<Collider>(true))
            Object.Destroy(col);

        Transform antenna = FindNamedChild(instance.transform, "Antenna");
        if (antenna == null)
        {
            var antennaGo = new GameObject("Antenna");
            antennaGo.transform.SetParent(wrap.transform, false);
            antennaGo.transform.localPosition = Voyager1AntennaLocal;
            antennaGo.transform.localRotation = Quaternion.identity;
            antenna = antennaGo.transform;
        }
        else if (antenna.parent != wrap.transform)
        {
            // Keep dish pivot under the rotated visual root so PointAntennaAt tracks Earth.
            Vector3 world = antenna.position;
            antenna.SetParent(wrap.transform, true);
            antenna.position = world;
        }

        craftAntenna = antenna;
        ApplyProbeBodyMaterials(wrap.transform);
        FitToBounds(wrap.transform, DefaultMeshFitSize);
        return true;
    }

    static bool TryBuildImportedMesh(
        Transform parent,
        string visualName,
        string meshChildName,
        string prefabAltPath,
        string prefabPath,
        Vector3 antennaLocal,
        Vector3 visualEuler)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabAltPath);
        if (prefab == null)
            prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
            return false;

        var wrap = new GameObject(visualName);
        wrap.transform.SetParent(parent, false);
        wrap.transform.localRotation = Quaternion.Euler(visualEuler);

        GameObject instance = Object.Instantiate(prefab, wrap.transform, false);
        instance.name = meshChildName;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        if (!HasValidMeshFilters(instance))
        {
            Object.Destroy(wrap);
            return false;
        }

        foreach (Collider col in instance.GetComponentsInChildren<Collider>(true))
            Object.Destroy(col);

        Transform antenna = FindNamedChild(instance.transform, "Antenna");
        if (antenna == null)
        {
            var antennaGo = new GameObject("Antenna");
            antennaGo.transform.SetParent(wrap.transform, false);
            antennaGo.transform.localPosition = antennaLocal;
            antennaGo.transform.localRotation = Quaternion.identity;
            antenna = antennaGo.transform;
        }
        else if (antenna.parent != wrap.transform)
        {
            Vector3 world = antenna.position;
            antenna.SetParent(wrap.transform, true);
            antenna.position = world;
        }

        craftAntenna = antenna;
        ApplyProbeBodyMaterials(wrap.transform);
        FitToBounds(wrap.transform, DefaultMeshFitSize);
        return true;
    }

    static void WarnMeshFallbackOnce(ref bool warned, string label, string altPath, string path)
    {
        if (warned)
            return;
        warned = true;
        Debug.LogWarning(
            $"ProbePrefabFactory: {label} mesh unavailable or empty; using primitive fallback. " +
            $"Tried Resources '{altPath}' then '{path}'.");
    }

    static bool HasValidMeshFilters(GameObject root)
    {
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i] != null && filters[i].sharedMesh != null)
                return true;
        }

        return false;
    }

    static Transform FindNamedChild(Transform root, string name)
    {
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamedChild(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    static void FitToBounds(Transform root, float targetSize)
    {
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        if (filters == null || filters.Length == 0)
            return;

        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter == null || filter.sharedMesh == null)
                continue;

            Bounds meshBounds = filter.sharedMesh.bounds;
            Vector3 c = meshBounds.center;
            Vector3 e = meshBounds.extents;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 localCorner = c + new Vector3(e.x * x, e.y * y, e.z * z);
                Vector3 worldCorner = filter.transform.TransformPoint(localCorner);
                Vector3 rootLocal = root.InverseTransformPoint(worldCorner);
                if (!hasBounds)
                {
                    bounds = new Bounds(rootLocal, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(rootLocal);
                }
            }
        }

        if (!hasBounds)
            return;

        float maxExtent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (maxExtent < 1e-5f)
            return;

        float scale = targetSize / maxExtent;
        root.localScale *= scale;
    }

    static void ApplyProbeBodyMaterials(Transform root)
    {
        Shader shader = ResolveBodyShader();
        if (shader == null)
            return;

        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            MeshRenderer renderer = renderers[r];
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Material[] source = renderer.sharedMaterials;
            if (source == null || source.Length == 0)
                continue;

            var rebuilt = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                Material src = source[i];
                var mat = new Material(shader);
                Texture albedo = null;
                Color color = new Color(0.78f, 0.76f, 0.7f, 1f);
                if (src != null)
                {
                    mat.name = src.name + "_Probe";
                    if (src.HasProperty("_BaseMap"))
                        albedo = src.GetTexture("_BaseMap");
                    if (albedo == null && src.HasProperty("_MainTex"))
                        albedo = src.GetTexture("_MainTex");
                    if (src.HasProperty("_BaseColor"))
                        color = src.GetColor("_BaseColor");
                    else if (src.HasProperty("_Color"))
                        color = src.GetColor("_Color");
                }
                else
                {
                    mat.name = "ProbeBody";
                }

                color.a = 1f;

                if (albedo != null)
                {
                    if (mat.HasProperty("_BaseMap"))
                        mat.SetTexture("_BaseMap", albedo);
                    if (mat.HasProperty("_MainTex"))
                        mat.SetTexture("_MainTex", albedo);
                    if (shader.name.IndexOf("Unlit", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        mat.EnableKeyword("_BASEMAP");
                }

                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", color);
                if (mat.HasProperty("_Metallic"))
                    mat.SetFloat("_Metallic", 0.25f);
                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", 0.35f);

                rebuilt[i] = mat;
            }

            renderer.sharedMaterials = rebuilt;
        }
    }

    public static int ResolveMinimapBlipLayer()
    {
        int named = LayerMask.NameToLayer(MinimapBlipLayerName);
        if (named >= 0)
            return named;
        return MinimapBlipLayer;
    }

    /// <summary>
    /// Keep MinimapBlip world size stable when the craft root is scaled for launch.
    /// </summary>
    public static void NormalizeBlipLocalScale(Transform craftRoot, float craftScale)
    {
        if (craftRoot == null)
            return;
        Transform blip = craftRoot.Find(BlipName);
        if (blip == null)
            return;
        float scale = Mathf.Max(craftScale, 0.01f);
        blip.localScale = Vector3.one * (1.5f / scale);
    }

    /// <summary>
    /// Minimap-only cyan sphere: exclude from every camera except Minimap Camera.
    /// </summary>
    public static void EnsureMinimapBlipCameraCulling()
    {
        int layer = ResolveMinimapBlipLayer();
        int bit = 1 << layer;

        Camera minimap = null;
        GameObject minimapGo = GameObject.Find("Minimap Camera");
        if (minimapGo != null)
            minimap = minimapGo.GetComponent<Camera>();

        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null)
                continue;
            if (minimap != null && cam == minimap)
                continue;
            cam.cullingMask &= ~bit;
        }

        if (minimap != null)
            minimap.cullingMask |= bit;

        Camera main = Camera.main;
        if (main == null)
        {
            GameObject mainGo = GameObject.Find("Main Camera");
            if (mainGo != null)
                main = mainGo.GetComponent<Camera>();
        }

        if (main != null)
            main.cullingMask &= ~bit;
    }

    static Transform craftAntenna;

    static void BuildNewHorizons(Transform parent, Material bus, Material gold, Material dish, Material dark, bool highDetail)
    {
        if (TryBuildImportedMesh(
                parent,
                "NewHorizonsVisual",
                "NewHorizonsMesh",
                NewHorizonsPrefabAltResourcePath,
                NewHorizonsPrefabResourcePath,
                NewHorizonsAntennaLocal,
                Vector3.zero))
            return;

        WarnMeshFallbackOnce(ref _newHorizonsMeshFallbackWarned, "New Horizons", NewHorizonsPrefabAltResourcePath, NewHorizonsPrefabResourcePath);

        AddPrimitive(parent, PrimitiveType.Cube, gold, Vector3.zero, new Vector3(0.7f, 0.28f, 0.5f));
        Transform hga = AddPrimitive(parent, PrimitiveType.Cylinder, dish, new Vector3(0f, 0.08f, -0.42f), new Vector3(0.85f, 0.035f, 0.85f));
        hga.localRotation = Quaternion.Euler(90f, 0f, 0f);
        craftAntenna = hga;
        AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0.48f, 0f, 0.05f), new Vector3(0.1f, 0.22f, 0.1f));
        AddPrimitive(parent, PrimitiveType.Cube, bus, new Vector3(0.22f, 0.12f, 0.08f), new Vector3(0.14f, 0.1f, 0.14f));
        if (highDetail)
        {
            AddPrimitive(parent, PrimitiveType.Cube, bus, new Vector3(0f, 0.22f, 0.05f), new Vector3(0.25f, 0.08f, 0.25f));
            AddPrimitive(parent, PrimitiveType.Cube, dark, new Vector3(-0.32f, -0.02f, 0.12f), new Vector3(0.1f, 0.08f, 0.1f));
        }
    }

    static void BuildJuno(Transform parent, Material bus, Material gold, Material dark, bool highDetail)
    {
        if (TryBuildImportedMesh(
                parent,
                "JunoVisual",
                "JunoMesh",
                JunoPrefabAltResourcePath,
                JunoPrefabResourcePath,
                JunoAntennaLocal,
                Vector3.zero))
            return;

        WarnMeshFallbackOnce(ref _junoMeshFallbackWarned, "Juno", JunoPrefabAltResourcePath, JunoPrefabResourcePath);

        AddPrimitive(parent, PrimitiveType.Cylinder, bus, Vector3.zero, new Vector3(0.45f, 0.22f, 0.45f));
        craftAntenna = AddPrimitive(parent, PrimitiveType.Sphere, gold, new Vector3(0f, 0.28f, 0f), Vector3.one * 0.18f);
        int wings = highDetail ? 3 : 3;
        for (int i = 0; i < wings; i++)
        {
            float yaw = i * 120f;
            var wing = AddPrimitive(parent, PrimitiveType.Cube, dark, Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.95f, 0f, 0f), new Vector3(1.4f, 0.03f, 0.42f));
            wing.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (highDetail)
                AddPrimitive(parent, PrimitiveType.Cube, gold, Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.72f, 0.04f, 0f), new Vector3(0.55f, 0.02f, 0.28f)).localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
        if (highDetail)
            AddPrimitive(parent, PrimitiveType.Cylinder, bus, new Vector3(0f, -0.18f, 0f), new Vector3(0.32f, 0.08f, 0.32f));
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
        {
            AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(0.32f, 0.18f, 0f), new Vector3(0.12f, 0.12f, 0.12f));
            AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(-0.28f, 0f, 0.12f), new Vector3(0.06f, 0.14f, 0.06f));
        }
    }

    static void BuildLuna1(Transform parent, Material bus, Material gold, Material dark, bool highDetail)
    {
        if (TryBuildImportedMesh(
                parent,
                "Luna1Visual",
                "Luna1Mesh",
                Luna1PrefabAltResourcePath,
                Luna1PrefabResourcePath,
                Luna1AntennaLocal,
                Vector3.zero))
            return;

        WarnMeshFallbackOnce(ref _luna1MeshFallbackWarned, "Luna 1", Luna1PrefabAltResourcePath, Luna1PrefabResourcePath);

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
        if (highDetail)
        {
            AddPrimitive(parent, PrimitiveType.Sphere, gold, new Vector3(0f, 0.52f, 0f), Vector3.one * 0.12f);
            AddPrimitive(parent, PrimitiveType.Cube, dark, new Vector3(0f, -0.28f, 0f), new Vector3(0.22f, 0.08f, 0.22f));
        }
    }

    static void BuildVenera7(Transform parent, Material bus, Material gold, Material dish, Material dark, bool highDetail)
    {
        if (TryBuildImportedMesh(
                parent,
                "Venera7Visual",
                "Venera7Mesh",
                Venera7PrefabAltResourcePath,
                Venera7PrefabResourcePath,
                Venera7AntennaLocal,
                Vector3.zero))
            return;

        WarnMeshFallbackOnce(ref _venera7MeshFallbackWarned, "Venera 7", Venera7PrefabAltResourcePath, Venera7PrefabResourcePath);

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
        if (highDetail)
        {
            AddPrimitive(parent, PrimitiveType.Cylinder, gold, new Vector3(0f, -0.22f, 0f), new Vector3(0.28f, 0.12f, 0.28f));
            AddPrimitive(parent, PrimitiveType.Cube, dark, new Vector3(0.18f, 0.12f, 0.12f), new Vector3(0.1f, 0.08f, 0.1f));
        }
    }

    static void BuildLuna16(Transform parent, Material bus, Material gold, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cylinder, bus, Vector3.zero, new Vector3(0.62f, 0.38f, 0.62f));
        craftAntenna = AddPrimitive(parent, PrimitiveType.Cylinder, gold, new Vector3(0.38f, 0.12f, 0f), new Vector3(0.08f, 0.45f, 0.08f));
        AddPrimitive(parent, PrimitiveType.Cube, dark, new Vector3(0f, 0.22f, 0.28f), new Vector3(0.22f, 0.12f, 0.18f));
        if (highDetail)
        {
            AddPrimitive(parent, PrimitiveType.Cylinder, gold, new Vector3(-0.28f, -0.08f, 0.18f), new Vector3(0.14f, 0.18f, 0.14f));
            AddPrimitive(parent, PrimitiveType.Cube, bus, new Vector3(0.28f, 0.08f, -0.12f), new Vector3(0.12f, 0.1f, 0.12f));
        }
    }

    static void BuildMars3(Transform parent, Material bus, Material gold, Material dish, Material dark, bool highDetail)
    {
        if (TryBuildMars3FromMesh(parent))
            return;

        if (!_mars3MeshFallbackWarned)
        {
            _mars3MeshFallbackWarned = true;
            Debug.LogWarning(
                "ProbePrefabFactory: Mars 3 mesh unavailable or empty; using primitive fallback. " +
                $"Tried Resources '{Mars3PrefabAltResourcePath}' then '{Mars3PrefabResourcePath}'.");
        }

        // Enriched procedural: cylinder bus, 4 gold petals, spherical lander, HGA dish.
        AddPrimitive(parent, PrimitiveType.Cylinder, bus, Vector3.zero, new Vector3(0.7f, 0.28f, 0.7f));
        for (int i = 0; i < 4; i++)
        {
            float yaw = 45f + i * 90f;
            var petal = AddPrimitive(parent, PrimitiveType.Cube, gold,
                Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.12f, 0.55f),
                new Vector3(0.42f, 0.025f, 0.55f));
            petal.localRotation = Quaternion.Euler(18f, yaw, 0f);
        }

        AddPrimitive(parent, PrimitiveType.Sphere, dark, new Vector3(0f, -0.42f, 0f), new Vector3(0.48f, 0.42f, 0.48f));
        AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0f, 0.28f, 0f), new Vector3(0.08f, 0.28f, 0.08f));
        Transform hga = AddPrimitive(parent, PrimitiveType.Cylinder, dish, new Vector3(0f, 0.58f, 0f), new Vector3(0.55f, 0.035f, 0.55f));
        craftAntenna = hga;
        if (highDetail)
        {
            AddPrimitive(parent, PrimitiveType.Cube, bus, new Vector3(0.28f, 0.06f, 0.18f), new Vector3(0.14f, 0.1f, 0.12f));
            AddPrimitive(parent, PrimitiveType.Cube, dark, new Vector3(-0.26f, 0.08f, -0.16f), new Vector3(0.12f, 0.08f, 0.12f));
        }
    }

    static bool TryBuildMars3FromMesh(Transform parent)
    {
        GameObject prefab = Resources.Load<GameObject>(Mars3PrefabAltResourcePath);
        if (prefab == null)
            prefab = Resources.Load<GameObject>(Mars3PrefabResourcePath);
        if (prefab == null)
            return false;

        var wrap = new GameObject("Mars3Visual");
        wrap.transform.SetParent(parent, false);
        wrap.transform.localRotation = Quaternion.Euler(Mars3VisualEuler);

        GameObject instance = Object.Instantiate(prefab, wrap.transform, false);
        instance.name = "Mars3Mesh";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        if (!HasValidMeshFilters(instance))
        {
            Object.Destroy(wrap);
            return false;
        }

        foreach (Collider col in instance.GetComponentsInChildren<Collider>(true))
            Object.Destroy(col);

        Transform antenna = FindNamedChild(instance.transform, "Antenna");
        if (antenna == null)
        {
            var antennaGo = new GameObject("Antenna");
            antennaGo.transform.SetParent(wrap.transform, false);
            antennaGo.transform.localPosition = Mars3AntennaLocal;
            antennaGo.transform.localRotation = Quaternion.identity;
            antenna = antennaGo.transform;
        }
        else if (antenna.parent != wrap.transform)
        {
            Vector3 world = antenna.position;
            antenna.SetParent(wrap.transform, true);
            antenna.position = world;
        }

        craftAntenna = antenna;
        ApplyProbeBodyMaterials(wrap.transform);
        FitToBounds(wrap.transform, DefaultMeshFitSize);
        return true;
    }

    static void BuildChange4(Transform parent, Material bus, Material gold, Material dark, bool highDetail)
    {
        if (TryBuildImportedMesh(
                parent,
                "Change4Visual",
                "Change4Mesh",
                Change4PrefabAltResourcePath,
                Change4PrefabResourcePath,
                Change4AntennaLocal,
                Vector3.zero))
            return;

        WarnMeshFallbackOnce(ref _change4MeshFallbackWarned, "Chang'e 4", Change4PrefabAltResourcePath, Change4PrefabResourcePath);

        AddPrimitive(parent, PrimitiveType.Cube, bus, Vector3.zero, new Vector3(0.75f, 0.14f, 0.55f));
        AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(-0.62f, 0.06f, 0f), new Vector3(0.35f, 0.02f, 0.48f));
        AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(0.62f, 0.06f, 0f), new Vector3(0.35f, 0.02f, 0.48f));
        craftAntenna = AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0f, 0.22f, -0.18f), new Vector3(0.08f, 0.2f, 0.08f));
        AddPrimitive(parent, PrimitiveType.Cube, dark, new Vector3(0.28f, 0.1f, 0.18f), new Vector3(0.18f, 0.12f, 0.16f));
        if (highDetail)
            AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(-0.18f, -0.04f, 0.28f), new Vector3(0.14f, 0.08f, 0.12f));
    }

    static void BuildTianwen1(Transform parent, Material bus, Material gold, Material dish, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cube, bus, Vector3.zero, new Vector3(0.48f, 0.32f, 0.48f));
        Transform hga = AddPrimitive(parent, PrimitiveType.Cylinder, dish, new Vector3(0f, 0.08f, -0.52f), new Vector3(1.05f, 0.035f, 1.05f));
        hga.localRotation = Quaternion.Euler(90f, 0f, 0f);
        craftAntenna = hga;
        AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0.42f, 0f, 0.12f), new Vector3(0.1f, 0.24f, 0.1f));
        AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(-0.38f, 0.18f, 0.1f), new Vector3(0.28f, 0.06f, 0.22f));
        if (highDetail)
        {
            AddPrimitive(parent, PrimitiveType.Sphere, dark, new Vector3(0f, -0.28f, 0.22f), new Vector3(0.28f, 0.2f, 0.28f));
            AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(0.32f, 0.08f, 0.22f), new Vector3(0.12f, 0.08f, 0.12f));
        }
    }

    static void BuildChange5(Transform parent, Material bus, Material gold, Material dark, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cylinder, bus, Vector3.zero, new Vector3(0.42f, 0.55f, 0.42f));
        AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(-0.55f, 0.15f, 0f), new Vector3(0.42f, 0.02f, 0.32f));
        AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(0.55f, 0.15f, 0f), new Vector3(0.42f, 0.02f, 0.32f));
        craftAntenna = AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0f, 0.42f, 0.15f), new Vector3(0.07f, 0.28f, 0.07f));
        if (highDetail)
        {
            AddPrimitive(parent, PrimitiveType.Cube, dark, new Vector3(0f, -0.22f, 0.18f), new Vector3(0.18f, 0.12f, 0.16f));
            AddPrimitive(parent, PrimitiveType.Cylinder, gold, new Vector3(0.22f, 0.28f, 0f), new Vector3(0.08f, 0.14f, 0.08f));
        }
    }

    static void BuildHayabusa2(Transform parent, Material bus, Material gold, Material dark, bool highDetail)
    {
        if (TryBuildImportedMesh(
                parent,
                "Hayabusa2Visual",
                "Hayabusa2Mesh",
                Hayabusa2PrefabAltResourcePath,
                Hayabusa2PrefabResourcePath,
                Hayabusa2AntennaLocal,
                Vector3.zero))
            return;

        WarnMeshFallbackOnce(ref _hayabusa2MeshFallbackWarned, "Hayabusa2", Hayabusa2PrefabAltResourcePath, Hayabusa2PrefabResourcePath);

        AddPrimitive(parent, PrimitiveType.Cube, bus, Vector3.zero, new Vector3(0.58f, 0.38f, 0.58f));
        craftAntenna = AddPrimitive(parent, PrimitiveType.Cylinder, gold, new Vector3(0f, 0.05f, 0.52f), new Vector3(0.14f, 0.32f, 0.14f));
        AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(0f, -0.08f, 0.38f), new Vector3(0.1f, 0.18f, 0.1f));
        AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(0.38f, 0.12f, 0f), new Vector3(0.22f, 0.08f, 0.12f));
        if (highDetail)
        {
            AddPrimitive(parent, PrimitiveType.Cylinder, dark, new Vector3(-0.28f, -0.12f, 0.32f), new Vector3(0.08f, 0.12f, 0.08f));
            AddPrimitive(parent, PrimitiveType.Cube, bus, new Vector3(-0.32f, 0.08f, -0.12f), new Vector3(0.12f, 0.1f, 0.1f));
        }
    }

    static void BuildAkatsuki(Transform parent, Material bus, Material gold, Material dish, bool highDetail)
    {
        AddPrimitive(parent, PrimitiveType.Cube, bus, Vector3.zero, new Vector3(0.52f, 0.42f, 0.52f));
        craftAntenna = AddPrimitive(parent, PrimitiveType.Cylinder, dish, new Vector3(0.55f, 0.18f, 0f), new Vector3(0.04f, 0.95f, 0.04f));
        craftAntenna.localRotation = Quaternion.Euler(0f, 0f, 75f);
        AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(-0.42f, 0.08f, 0f), new Vector3(0.28f, 0.02f, 0.38f));
        AddPrimitive(parent, PrimitiveType.Cube, gold, new Vector3(0.42f, 0.08f, 0f), new Vector3(0.28f, 0.02f, 0.38f));
        if (highDetail)
            AddPrimitive(parent, PrimitiveType.Cylinder, bus, new Vector3(0f, -0.22f, 0f), new Vector3(0.28f, 0.1f, 0.28f));
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
        AddPrimitive(parent, PrimitiveType.Cube, dark, new Vector3(0.32f, 0.14f, 0.22f), new Vector3(0.16f, 0.1f, 0.14f));
        if (highDetail)
        {
            AddPrimitive(parent, PrimitiveType.Cylinder, gold, new Vector3(-0.22f, -0.12f, 0.18f), new Vector3(0.12f, 0.08f, 0.12f));
            AddPrimitive(parent, PrimitiveType.Cube, bus, new Vector3(0f, -0.18f, -0.12f), new Vector3(0.2f, 0.08f, 0.16f));
        }
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
