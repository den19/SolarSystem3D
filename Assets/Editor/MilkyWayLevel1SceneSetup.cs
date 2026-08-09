#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds MilkyWaySkyboxApplier to Level1 with Space 5 / Space 3 material refs.
/// </summary>
public static class MilkyWayLevel1SceneSetup
{
    const string MenuPath = "Solar System/Setup Milky Way Level1 Applier";
    const string ScenePath = "Assets/_Scenes/Level1.unity";
    const string HostName = "MilkyWaySkybox";
    const string Space5Guid = "57a03e42a58e1e242b3b60b258a9229b";
    const string Space3Guid = "412f5114ffdfb224090e321b3492cdd6";

    [MenuItem(MenuPath)]
    public static void SetupFromMenu()
    {
        if (!EnsureLevel1Open())
            return;

        SetupInternal(markDirty: true);
        Debug.Log("Milky Way Level1 applier setup complete.");
    }

    public static void ExecuteBatchSetup()
    {
        EditorSceneManager.OpenScene(ScenePath);
        SetupInternal(markDirty: true);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorApplication.Exit(0);
    }

    public static void ExecuteBatchSetupBoth()
    {
        EditorSceneManager.OpenScene("Assets/_Scenes/MainMenu.unity");
        MilkyWaySettingsSceneSetup.SetupInternal(markDirty: true);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        EditorSceneManager.OpenScene(ScenePath);
        SetupInternal(markDirty: true);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorApplication.Exit(0);
    }

    static bool EnsureLevel1Open()
    {
        if (SceneManager.GetActiveScene().path.Replace('\\', '/').EndsWith(ScenePath))
            return true;

        EditorSceneManager.OpenScene(ScenePath);
        return true;
    }

    static void SetupInternal(bool markDirty)
    {
        Material space5 = LoadMaterial(Space5Guid);
        Material space3 = LoadMaterial(Space3Guid);
        if (space5 == null || space3 == null)
        {
            Debug.LogError("Could not load Space 5 / Space 3 skybox materials.");
            return;
        }

        GameObject host = GameObject.Find(HostName);
        if (host == null)
        {
            host = new GameObject(HostName);
            Undo.RegisterCreatedObjectUndo(host, "Create MilkyWaySkybox");
        }

        var applier = host.GetComponent<MilkyWaySkyboxApplier>();
        if (applier == null)
            applier = host.AddComponent<MilkyWaySkyboxApplier>();

        var so = new SerializedObject(applier);
        so.FindProperty("starfieldSkybox").objectReferenceValue = space5;
        so.FindProperty("milkyWaySkybox").objectReferenceValue = space3;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (markDirty)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    static Material LoadMaterial(string guid)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
            return null;
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }
}
#endif
