#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Inserts a Milky Way settings row (clone of ExtraGraphics) into MainMenu SettingsWindow.
/// </summary>
public static class MilkyWaySettingsSceneSetup
{
    const string MenuPath = "Solar System/Setup Milky Way Settings UI";
    const string ScenePath = "Assets/_Scenes/MainMenu.unity";
    const string RowName = "MilkyWay";
    const string LabelName = "MilkyWayLabel";
    const string ToggleName = "Milky Way Toggle";
    const string SourceRowName = "ExtraGraphics";
    const string SourceLabelName = "ExtraGraphicsLabel";
    const string SourceToggleName = "Extra Graphics Toggle";
    const float RowStep = 69f;

    [MenuItem(MenuPath)]
    public static void SetupFromMenu()
    {
        if (!EnsureMainMenuOpen())
            return;

        SetupInternal(markDirty: true);
        Debug.Log("Milky Way Settings UI setup complete on MainMenu SettingsWindow.");
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

        Transform extra = settings.Find(SourceRowName);
        if (extra == null)
        {
            Debug.LogError("ExtraGraphics row not found under SettingsWindow.");
            return;
        }

        var extraRect = extra as RectTransform;
        Transform milky = settings.Find(RowName);
        bool created = false;
        if (milky == null)
        {
            milky = CreateMilkyWayRow(settings, extra);
            created = true;
        }

        LayoutMilkyWayRow(milky as RectTransform, extraRect);
        WireMilkyWayController(milky.gameObject);
        ShiftRowsBelowExtra(settings, extraRect, created || !WasAlreadyShifted(settings, extraRect));
        GrowPanel(settings as RectTransform);
        ShiftBackButton(settings);

        if (markDirty)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    static bool WasAlreadyShifted(Transform settings, RectTransform extraRect)
    {
        Transform tier = settings.Find("GraphicsTier");
        if (tier is not RectTransform tierRect || extraRect == null)
            return false;

        float delta = extraRect.anchoredPosition.y - tierRect.anchoredPosition.y;
        // After Milky Way insertion, GraphicsTier sits ~2 row steps below ExtraGraphics.
        return delta > RowStep * 1.5f;
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

    static Transform CreateMilkyWayRow(Transform settings, Transform extra)
    {
        GameObject clone = Object.Instantiate(extra.gameObject, settings, false);
        clone.name = RowName;
        clone.transform.SetSiblingIndex(extra.GetSiblingIndex() + 1);

        var oldCtrl = clone.GetComponent<ExtraGraphicsControlController>();
        if (oldCtrl != null)
            Object.DestroyImmediate(oldCtrl);

        Transform label = FindDeepChild(clone.transform, SourceLabelName);
        if (label != null)
        {
            label.name = LabelName;
            var text = label.GetComponent<Text>();
            if (text != null)
                text.text = "Milky Way";
        }

        Transform toggle = FindDeepChild(clone.transform, SourceToggleName);
        if (toggle != null)
            toggle.name = ToggleName;

        return clone.transform;
    }

    static void LayoutMilkyWayRow(RectTransform milkyRect, RectTransform extraRect)
    {
        if (milkyRect == null || extraRect == null)
            return;

        milkyRect.anchorMin = extraRect.anchorMin;
        milkyRect.anchorMax = extraRect.anchorMax;
        milkyRect.pivot = extraRect.pivot;
        milkyRect.sizeDelta = extraRect.sizeDelta;
        milkyRect.anchoredPosition = new Vector2(
            extraRect.anchoredPosition.x,
            extraRect.anchoredPosition.y - RowStep);
        milkyRect.SetSiblingIndex(extraRect.GetSiblingIndex() + 1);
    }

    static void WireMilkyWayController(GameObject milkyRow)
    {
        var ctrl = milkyRow.GetComponent<MilkyWayControlController>();
        if (ctrl == null)
            ctrl = milkyRow.AddComponent<MilkyWayControlController>();

        Toggle toggle = null;
        Transform toggleTf = FindDeepChild(milkyRow.transform, ToggleName);
        if (toggleTf != null)
            toggle = toggleTf.GetComponent<Toggle>();

        var so = new SerializedObject(ctrl);
        so.FindProperty("milkyWayToggle").objectReferenceValue = toggle;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ShiftRowsBelowExtra(Transform settings, RectTransform extraRect, bool doShift)
    {
        if (!doShift || extraRect == null)
            return;

        string[] names = { "GraphicsTier", "CpuMonitor" };
        for (int i = 0; i < names.Length; i++)
        {
            Transform row = settings.Find(names[i]);
            if (row is not RectTransform rect)
                continue;

            // Extra + MilkyWay + i => offset (i + 2) steps from Extra.
            float y = extraRect.anchoredPosition.y - RowStep * (i + 2);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }
    }

    static void GrowPanel(RectTransform settingsRect)
    {
        if (settingsRect == null)
            return;

        // Music setup grew 940 -> 1009; add one more row.
        float targetHeight = 1009f + RowStep;
        if (Mathf.Abs(settingsRect.sizeDelta.y - targetHeight) < 0.5f)
            return;

        if (Mathf.Abs(settingsRect.sizeDelta.y - 1009f) < 0.5f)
        {
            settingsRect.sizeDelta = new Vector2(settingsRect.sizeDelta.x, targetHeight);
            return;
        }

        if (Mathf.Abs(settingsRect.sizeDelta.y - 940f) < 0.5f)
            settingsRect.sizeDelta = new Vector2(settingsRect.sizeDelta.x, 940f + RowStep * 2f);
    }

    static void ShiftBackButton(Transform settings)
    {
        Transform back = FindDirectOrPrefixed(settings, "Back");
        if (back is not RectTransform backRect)
            return;

        // After Music: -428.57144; after Milky Way: one more step.
        const float afterMusicY = -428.57144f;
        const float afterMilkyY = afterMusicY - RowStep;
        if (Mathf.Abs(backRect.anchoredPosition.y - afterMusicY) < 0.5f)
            backRect.anchoredPosition = new Vector2(backRect.anchoredPosition.x, afterMilkyY);
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
