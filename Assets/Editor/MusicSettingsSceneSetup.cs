#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Inserts a Music settings row (clone of Volume) into MainMenu SettingsWindow
/// and shifts rows below it so nothing overlaps.
/// </summary>
public static class MusicSettingsSceneSetup
{
    const string MenuPath = "Solar System/Setup Music Settings UI";
    const string ScenePath = "Assets/_Scenes/MainMenu.unity";
    const string RowName = "Music";
    const string LabelName = "MusicLabel";
    const string ToggleName = "Music Toggle";
    const float RowStep = 69f;
    const float PanelHeightDelta = 69f;

    [MenuItem(MenuPath)]
    public static void SetupFromMenu()
    {
        if (!EnsureMainMenuOpen())
            return;

        SetupInternal(markDirty: true);
        Debug.Log("Music Settings UI setup complete on MainMenu SettingsWindow.");
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

    static void SetupInternal(bool markDirty)
    {
        Transform settings = FindSettingsWindow();
        if (settings == null)
        {
            Debug.LogError("SettingsWindow not found in MainMenu.");
            return;
        }

        Transform volume = settings.Find("Volume");
        if (volume == null)
        {
            Debug.LogError("Volume row not found under SettingsWindow.");
            return;
        }

        var volumeRect = volume as RectTransform;
        Transform music = settings.Find(RowName);
        bool created = false;
        if (music == null)
        {
            music = CreateMusicRow(settings, volume);
            created = true;
        }

        LayoutMusicRow(music as RectTransform, volumeRect);
        WireMusicController(music.gameObject);
        ShiftRowsBelowVolume(settings, volumeRect, created || !WasAlreadyShifted(settings));
        GrowPanel(settings as RectTransform);
        ShiftBackButton(settings);

        if (markDirty)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    static bool WasAlreadyShifted(Transform settings)
    {
        Transform extra = settings.Find("ExtraGraphics");
        Transform volume = settings.Find("Volume");
        if (extra == null || volume == null)
            return false;

        var extraRect = extra as RectTransform;
        var volumeRect = volume as RectTransform;
        if (extraRect == null || volumeRect == null)
            return false;

        // After Music insertion ExtraGraphics sits ~2 row steps below Volume.
        float delta = volumeRect.anchoredPosition.y - extraRect.anchoredPosition.y;
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

    static Transform CreateMusicRow(Transform settings, Transform volume)
    {
        GameObject clone = Object.Instantiate(volume.gameObject, settings, false);
        clone.name = RowName;
        clone.transform.SetSiblingIndex(volume.GetSiblingIndex() + 1);

        // Remove VolumeControlController; replace with MusicControlController.
        var oldCtrl = clone.GetComponent<VolumeControlController>();
        if (oldCtrl != null)
            Object.DestroyImmediate(oldCtrl);

        Transform label = FindDeepChild(clone.transform, "VolumeLabel");
        if (label != null)
        {
            label.name = LabelName;
            var text = label.GetComponent<Text>();
            if (text != null)
                text.text = "Music";
        }

        Transform toggle = FindDeepChild(clone.transform, "Volume Toggle");
        if (toggle != null)
            toggle.name = ToggleName;

        Transform slider = FindDeepChild(clone.transform, "VolumeSlider");
        if (slider != null)
            slider.name = "MusicSlider";

        return clone.transform;
    }

    static void LayoutMusicRow(RectTransform musicRect, RectTransform volumeRect)
    {
        if (musicRect == null || volumeRect == null)
            return;

        musicRect.anchorMin = volumeRect.anchorMin;
        musicRect.anchorMax = volumeRect.anchorMax;
        musicRect.pivot = volumeRect.pivot;
        musicRect.sizeDelta = volumeRect.sizeDelta;
        musicRect.anchoredPosition = new Vector2(
            volumeRect.anchoredPosition.x,
            volumeRect.anchoredPosition.y - RowStep);
        musicRect.SetSiblingIndex(volumeRect.GetSiblingIndex() + 1);
    }

    static void WireMusicController(GameObject musicRow)
    {
        var ctrl = musicRow.GetComponent<MusicControlController>();
        if (ctrl == null)
            ctrl = musicRow.AddComponent<MusicControlController>();

        Toggle toggle = null;
        Transform toggleTf = FindDeepChild(musicRow.transform, ToggleName);
        if (toggleTf != null)
            toggle = toggleTf.GetComponent<Toggle>();

        Slider slider = musicRow.GetComponentInChildren<Slider>(true);

        var so = new SerializedObject(ctrl);
        so.FindProperty("musicToggle").objectReferenceValue = toggle;
        so.FindProperty("musicSlider").objectReferenceValue = slider;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ShiftRowsBelowVolume(Transform settings, RectTransform volumeRect, bool doShift)
    {
        if (!doShift || volumeRect == null)
            return;

        string[] names = { "ExtraGraphics", "GraphicsTier", "CpuMonitor" };
        for (int i = 0; i < names.Length; i++)
        {
            Transform row = settings.Find(names[i]);
            if (row is not RectTransform rect)
                continue;

            // Place relative to Volume so re-running stays idempotent.
            float y = volumeRect.anchoredPosition.y - RowStep * (i + 2);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }
    }

    static void GrowPanel(RectTransform settingsRect)
    {
        if (settingsRect == null)
            return;

        // Idempotent: portrait baseline should be 940 + 69 once Music exists.
        float targetHeight = 940f + PanelHeightDelta;
        if (Mathf.Abs(settingsRect.sizeDelta.y - targetHeight) < 0.5f)
            return;

        // If landscape already applied a scaled height, prefer +69 over current.
        if (settingsRect.sizeDelta.y >= targetHeight - 0.5f &&
            Mathf.Abs(settingsRect.sizeDelta.y - 940f) > 0.5f &&
            Mathf.Abs(settingsRect.sizeDelta.y - targetHeight) > 0.5f)
        {
            // Likely landscape scaled; bump by RowStep once if not already bumped from 940*1.5.
            float portraitScaled = 940f * 1.5f;
            float targetLandscape = targetHeight * 1.5f;
            if (Mathf.Abs(settingsRect.sizeDelta.y - portraitScaled) < 1f)
                settingsRect.sizeDelta = new Vector2(settingsRect.sizeDelta.x, targetLandscape);
            return;
        }

        if (Mathf.Abs(settingsRect.sizeDelta.y - 940f) < 0.5f)
            settingsRect.sizeDelta = new Vector2(settingsRect.sizeDelta.x, targetHeight);
    }

    static void ShiftBackButton(Transform settings)
    {
        Transform back = FindDirectOrPrefixed(settings, "Back");
        if (back is not RectTransform backRect)
            return;

        // Idempotent marker: store expected offset via comparing to known pre-shift value.
        const float originalY = -359.57144f;
        const float shiftedY = originalY - RowStep;
        if (Mathf.Abs(backRect.anchoredPosition.y - originalY) < 0.5f)
            backRect.anchoredPosition = new Vector2(backRect.anchoredPosition.x, shiftedY);
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
