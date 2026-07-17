#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Bakes Graphics quality row into MainMenu Settings (scene object, not runtime-created).
/// </summary>
public static class GraphicsTierSceneSetup
{
    const string MenuPath = "Solar System/Setup Graphics Tier UI";
    const string ScenePath = "Assets/_Scenes/MainMenu.unity";
    const string JupiterFontGuid = "7cb2912222469634ba17a77055919ea8";
    const string RowName = "GraphicsTier";
    const string LabelName = "GraphicsTierLabel";
    const string DropdownName = "GraphicsTierDropdown";
    const float RowStep = 69f;

    static readonly Color PanelBlue = new Color(0.3754f, 0.5526f, 0.6981f, 1f);
    static readonly Color TemplateBlue = new Color(0.28f, 0.42f, 0.55f, 0.98f);
    static readonly Color ItemBlue = new Color(0.32f, 0.48f, 0.62f, 1f);
    static readonly Color CheckBlue = new Color(0.85f, 0.93f, 1f, 1f);
    static readonly Color CaptionDark = new Color(0.12f, 0.18f, 0.28f, 1f);

    [MenuItem(MenuPath)]
    public static void SetupFromMenu()
    {
        if (!EnsureMainMenuOpen())
            return;

        SetupInternal(markDirty: true);
        Debug.Log("Graphics Tier UI setup complete on MainMenu SettingsWindow.");
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

        Transform extraGraphics = settings.Find("ExtraGraphics");
        if (extraGraphics == null)
        {
            Debug.LogError("ExtraGraphics not found under SettingsWindow.");
            return;
        }

        Transform row = settings.Find(RowName);
        if (row == null)
            row = CreateRow(settings, extraGraphics as RectTransform).transform;
        else
            ApplyOrganicStyle(row);

        LayoutRow(row as RectTransform, extraGraphics as RectTransform);
        WireController(row.gameObject);

        if (markDirty)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    static Transform FindSettingsWindow()
    {
        // SettingsWindow is often inactive on MainMenu load — search inactive too.
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

    static GameObject CreateRow(Transform parent, RectTransform extraGraphicsRect)
    {
        Font jupiter = LoadJupiterFont();
        Font fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        Font font = jupiter != null ? jupiter : fallback;

        var rowGo = new GameObject(RowName, typeof(RectTransform));
        rowGo.layer = parent.gameObject.layer;
        rowGo.transform.SetParent(parent, false);

        var rowRect = rowGo.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(360f, 80f);
        float y = extraGraphicsRect != null ? extraGraphicsRect.anchoredPosition.y - RowStep : -387f;
        rowRect.anchoredPosition = new Vector2(0f, y);
        rowGo.transform.SetSiblingIndex(extraGraphicsRect != null ? extraGraphicsRect.GetSiblingIndex() + 1 : parent.childCount);

        var labelGo = new GameObject(LabelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelGo.layer = rowGo.layer;
        labelGo.transform.SetParent(rowGo.transform, false);
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0.38f, 1f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(10f, 0f);
        labelRect.sizeDelta = new Vector2(0f, 0f);

        var label = labelGo.GetComponent<Text>();
        label.font = font;
        label.fontSize = 33;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
        label.text = "Graphics quality";
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        labelGo.AddComponent<LocalizedText>();

        var dropdownGo = new GameObject(DropdownName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Dropdown));
        dropdownGo.layer = rowGo.layer;
        dropdownGo.transform.SetParent(rowGo.transform, false);
        var dropdownRect = dropdownGo.GetComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0.4f, 0.22f);
        dropdownRect.anchorMax = new Vector2(0.95f, 0.78f);
        dropdownRect.offsetMin = Vector2.zero;
        dropdownRect.offsetMax = Vector2.zero;

        var dropdownImage = dropdownGo.GetComponent<Image>();
        dropdownImage.color = PanelBlue;

        var dropdown = dropdownGo.GetComponent<Dropdown>();
        dropdown.targetGraphic = dropdownImage;

        var captionGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        captionGo.layer = rowGo.layer;
        captionGo.transform.SetParent(dropdownGo.transform, false);
        StretchWithPadding(captionGo.GetComponent<RectTransform>(), 10f, 28f, 4f, 4f);
        var caption = captionGo.GetComponent<Text>();
        caption.font = font;
        caption.fontSize = 26;
        caption.alignment = TextAnchor.MiddleLeft;
        caption.color = CaptionDark;
        dropdown.captionText = caption;

        var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        arrowGo.layer = rowGo.layer;
        arrowGo.transform.SetParent(dropdownGo.transform, false);
        var arrowRect = arrowGo.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0f);
        arrowRect.anchorMax = new Vector2(1f, 1f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.sizeDelta = new Vector2(28f, 0f);
        arrowRect.anchoredPosition = new Vector2(-4f, 0f);
        var arrowText = arrowGo.GetComponent<Text>();
        arrowText.font = font;
        arrowText.fontSize = 20;
        arrowText.alignment = TextAnchor.MiddleCenter;
        arrowText.color = CaptionDark;
        arrowText.text = "▼";

        var templateGo = new GameObject("Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        templateGo.layer = rowGo.layer;
        templateGo.transform.SetParent(dropdownGo.transform, false);
        var templateRect = templateGo.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = Vector2.zero;
        templateRect.sizeDelta = new Vector2(0f, 120f);
        templateGo.GetComponent<Image>().color = TemplateBlue;

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewportGo.layer = rowGo.layer;
        viewportGo.transform.SetParent(templateGo.transform, false);
        StretchFull(viewportGo.GetComponent<RectTransform>());
        viewportGo.GetComponent<Image>().color = Color.white;
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.layer = rowGo.layer;
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 28f);

        var itemGo = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
        itemGo.layer = rowGo.layer;
        itemGo.transform.SetParent(contentGo.transform, false);
        var itemRect = itemGo.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 0.5f);
        itemRect.anchorMax = new Vector2(1f, 0.5f);
        itemRect.sizeDelta = new Vector2(0f, 28f);

        var itemBgGo = new GameObject("Item Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        itemBgGo.transform.SetParent(itemGo.transform, false);
        StretchFull(itemBgGo.GetComponent<RectTransform>());
        itemBgGo.GetComponent<Image>().color = ItemBlue;

        var itemCheckGo = new GameObject("Item Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        itemCheckGo.transform.SetParent(itemGo.transform, false);
        var checkRect = itemCheckGo.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0f, 0.5f);
        checkRect.anchorMax = new Vector2(0f, 0.5f);
        checkRect.sizeDelta = new Vector2(16f, 16f);
        checkRect.anchoredPosition = new Vector2(12f, 0f);
        itemCheckGo.GetComponent<Image>().color = CheckBlue;

        var itemLabelGo = new GameObject("Item Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        itemLabelGo.transform.SetParent(itemGo.transform, false);
        StretchWithPadding(itemLabelGo.GetComponent<RectTransform>(), 28f, 8f, 2f, 2f);
        var itemLabel = itemLabelGo.GetComponent<Text>();
        itemLabel.font = font;
        itemLabel.fontSize = 24;
        itemLabel.color = Color.white;
        itemLabel.alignment = TextAnchor.MiddleLeft;

        var itemToggle = itemGo.GetComponent<Toggle>();
        itemToggle.targetGraphic = itemBgGo.GetComponent<Image>();
        itemToggle.graphic = itemCheckGo.GetComponent<Image>();

        var scroll = templateGo.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = viewportGo.GetComponent<RectTransform>();
        scroll.horizontal = false;
        scroll.vertical = true;

        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;
        templateGo.SetActive(false);

        return rowGo;
    }

    static void ApplyOrganicStyle(Transform row)
    {
        Font jupiter = LoadJupiterFont();
        Transform labelT = row.Find(LabelName);
        if (labelT != null && labelT.TryGetComponent(out Text label))
        {
            if (jupiter != null)
                label.font = jupiter;
            label.fontSize = 33;
            label.color = Color.white;
        }

        Transform dropdownT = row.Find(DropdownName);
        if (dropdownT == null)
            return;

        if (dropdownT.TryGetComponent(out Image image))
            image.color = PanelBlue;

        Transform caption = dropdownT.Find("Label");
        if (caption != null && caption.TryGetComponent(out Text captionText))
        {
            if (jupiter != null)
                captionText.font = jupiter;
            captionText.fontSize = 26;
            captionText.color = CaptionDark;
        }

        Transform arrow = dropdownT.Find("Arrow");
        if (arrow != null && arrow.TryGetComponent(out Text arrowText))
            arrowText.color = CaptionDark;

        Transform template = dropdownT.Find("Template");
        if (template != null && template.TryGetComponent(out Image templateImage))
            templateImage.color = TemplateBlue;
    }

    static void LayoutRow(RectTransform graphicsTierRect, RectTransform extraGraphicsRect)
    {
        if (graphicsTierRect == null)
            return;

        float tierY = extraGraphicsRect != null
            ? extraGraphicsRect.anchoredPosition.y - RowStep
            : -387f;

        graphicsTierRect.anchorMin = new Vector2(0.5f, 1f);
        graphicsTierRect.anchorMax = new Vector2(0.5f, 1f);
        graphicsTierRect.pivot = new Vector2(0.5f, 1f);
        graphicsTierRect.sizeDelta = new Vector2(360f, 80f);
        graphicsTierRect.anchoredPosition = new Vector2(0f, tierY);

        if (extraGraphicsRect != null)
            graphicsTierRect.SetSiblingIndex(extraGraphicsRect.GetSiblingIndex() + 1);

        Transform cpuMonitor = graphicsTierRect.parent.Find("CpuMonitor");
        if (cpuMonitor is RectTransform cpuRect)
        {
            cpuRect.anchorMin = new Vector2(0.5f, 1f);
            cpuRect.anchorMax = new Vector2(0.5f, 1f);
            cpuRect.pivot = new Vector2(0.5f, 1f);
            cpuRect.anchoredPosition = new Vector2(0f, tierY - RowStep);
            cpuRect.SetSiblingIndex(graphicsTierRect.GetSiblingIndex() + 1);
        }
    }

    static void WireController(GameObject rowGo)
    {
        var controller = rowGo.GetComponent<GraphicsTierControlController>();
        if (controller == null)
            controller = rowGo.AddComponent<GraphicsTierControlController>();

        var so = new SerializedObject(controller);
        so.FindProperty("tierDropdown").objectReferenceValue =
            rowGo.transform.Find(DropdownName)?.GetComponent<Dropdown>();
        so.FindProperty("labelText").objectReferenceValue =
            rowGo.transform.Find(LabelName)?.GetComponent<Text>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Font LoadJupiterFont()
    {
        string path = AssetDatabase.GUIDToAssetPath(JupiterFontGuid);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Font>(path);
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void StretchWithPadding(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
#endif
