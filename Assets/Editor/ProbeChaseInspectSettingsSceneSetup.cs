#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Inserts a Keep Chase Inspect Angle settings row (clone of CpuMonitor) into MainMenu SettingsWindow.
/// </summary>
public static class ProbeChaseInspectSettingsSceneSetup
{
    const string MenuPath = "Solar System/Setup Probe Chase Inspect Settings UI";
    const string ScenePath = "Assets/_Scenes/MainMenu.unity";
    const string RowName = "KeepChaseInspectAngle";
    const string LabelName = "KeepChaseInspectAngleLabel";
    const string ToggleName = "Keep Chase Inspect Angle Toggle";
    const string SourceRowName = "CpuMonitor";
    const string SourceLabelName = "CpuMonitorLabel";
    const string SourceToggleName = "CPU Monitor Toggle";
    const float RowStep = 69f;

    [MenuItem(MenuPath)]
    public static void SetupFromMenu()
    {
        if (!EnsureMainMenuOpen())
            return;

        SetupInternal(markDirty: true);
        Debug.Log("Probe Chase Inspect Settings UI setup complete on MainMenu SettingsWindow.");
    }

    public static void ExecuteBatchSetup()
    {
        EditorSceneManager.OpenScene(ScenePath);
        SetupInternal(markDirty: true);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorApplication.Exit(0);
    }

    static bool EnsureMainMenuOpen()
    {
        if (SceneManager.GetActiveScene().path.Replace('\\', '/').EndsWith(ScenePath))
            return true;

        EditorSceneManager.OpenScene(ScenePath);
        return true;
    }

    public static void SetupInternal(bool markDirty)
    {
        Transform settings = FindSettingsWindow();
        if (settings == null)
        {
            Debug.LogError("SettingsWindow not found in MainMenu.");
            return;
        }

        Transform cpu = settings.Find(SourceRowName);
        if (cpu == null)
        {
            Debug.LogError("CpuMonitor row not found under SettingsWindow.");
            return;
        }

        var cpuRect = cpu as RectTransform;
        Transform row = settings.Find(RowName);
        bool created = false;
        if (row == null)
        {
            row = CreateRow(settings, cpu);
            created = true;
        }

        LayoutRow(row as RectTransform, cpuRect);
        WireController(row.gameObject);
        if (created)
        {
            ShiftResetAndBack(settings);
            GrowPanel(settings as RectTransform);
        }

        if (markDirty)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    static Transform FindSettingsWindow()
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t == null || t.name != "SettingsWindow")
                continue;
            if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded)
                continue;
            return t;
        }

        return null;
    }

    static Transform CreateRow(Transform settings, Transform cpu)
    {
        GameObject clone = Object.Instantiate(cpu.gameObject, settings, false);
        clone.name = RowName;
        clone.transform.SetSiblingIndex(cpu.GetSiblingIndex() + 1);

        var oldCtrl = clone.GetComponent<CpuMonitorControlController>();
        if (oldCtrl != null)
            Object.DestroyImmediate(oldCtrl);

        Transform label = FindDeepChild(clone.transform, SourceLabelName);
        if (label != null)
        {
            label.name = LabelName;
            var text = label.GetComponent<Text>();
            if (text != null)
                text.text = "Keep probe view";
        }

        Transform toggle = FindDeepChild(clone.transform, SourceToggleName);
        if (toggle != null)
            toggle.name = ToggleName;

        return clone.transform;
    }

    static void LayoutRow(RectTransform rowRect, RectTransform cpuRect)
    {
        if (rowRect == null || cpuRect == null)
            return;

        rowRect.anchorMin = cpuRect.anchorMin;
        rowRect.anchorMax = cpuRect.anchorMax;
        rowRect.pivot = cpuRect.pivot;
        rowRect.sizeDelta = cpuRect.sizeDelta;
        rowRect.anchoredPosition = new Vector2(
            cpuRect.anchoredPosition.x,
            cpuRect.anchoredPosition.y - RowStep);
        rowRect.SetSiblingIndex(cpuRect.GetSiblingIndex() + 1);
    }

    static void WireController(GameObject row)
    {
        var ctrl = row.GetComponent<ProbeChaseInspectControlController>();
        if (ctrl == null)
            ctrl = row.AddComponent<ProbeChaseInspectControlController>();

        Toggle toggle = null;
        Transform toggleTf = FindDeepChild(row.transform, ToggleName);
        if (toggleTf != null)
            toggle = toggleTf.GetComponent<Toggle>();

        var so = new SerializedObject(ctrl);
        so.FindProperty("keepChaseInspectAngleToggle").objectReferenceValue = toggle;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ShiftResetAndBack(Transform settings)
    {
        ShiftNamed(settings, "Reset");
        ShiftNamed(settings, "Back");
    }

    static void ShiftNamed(Transform settings, string name)
    {
        Transform row = FindDirectOrPrefixed(settings, name);
        if (row is not RectTransform rect)
            return;

        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y - RowStep);
    }

    static void GrowPanel(RectTransform settingsRect)
    {
        if (settingsRect == null)
            return;

        settingsRect.sizeDelta = new Vector2(settingsRect.sizeDelta.x, settingsRect.sizeDelta.y + RowStep);
    }

    static Transform FindDirectOrPrefixed(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name || child.name.StartsWith(name))
                return child;
        }

        return FindDeepChild(parent, name);
    }

    static Transform FindDeepChild(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
#endif
