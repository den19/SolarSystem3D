using System.Collections;
using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MainMenu control for Auto / Balanced / High graphics tier.
/// Creates a settings row under ExtraGraphics when missing from the scene.
/// </summary>
public class GraphicsTierControlController : MonoBehaviour
{
    const string RowName = "GraphicsTier";
    const string LabelName = "GraphicsTierLabel";
    const string DropdownName = "GraphicsTierDropdown";
    const string CpuMonitorRowName = "CpuMonitor";
    const float RowStep = 69f;

    [SerializeField] Dropdown tierDropdown;
    [SerializeField] Text labelText;

    bool isInitializing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedStatic;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedStatic;

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
            EnsureRow();
    }

    static void OnSceneLoadedStatic(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
            EnsureRow();
    }

    public static GraphicsTierControlController EnsureRow()
    {
        var existing = Object.FindFirstObjectByType<GraphicsTierControlController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            existing.ResolveReferences();
            LayoutUnderExtraGraphics(existing.transform as RectTransform);
            return existing;
        }

        Transform extraGraphics = FindNamed("ExtraGraphics");
        if (extraGraphics == null)
            return null;

        Transform parent = extraGraphics.parent;
        if (parent == null)
            return null;

        Transform rowTransform = parent.Find(RowName);
        GameObject rowGo = rowTransform != null
            ? rowTransform.gameObject
            : CreateRow(parent, extraGraphics as RectTransform);

        var controller = rowGo.GetComponent<GraphicsTierControlController>();
        if (controller == null)
            controller = rowGo.AddComponent<GraphicsTierControlController>();

        LayoutUnderExtraGraphics(rowGo.transform as RectTransform);
        controller.ResolveReferences();
        return controller;
    }

    static void LayoutUnderExtraGraphics(RectTransform graphicsTierRect)
    {
        if (graphicsTierRect == null)
            return;

        Transform parent = graphicsTierRect.parent;
        if (parent == null)
            return;

        Transform extraGraphics = parent.Find("ExtraGraphics");
        var extraGraphicsRect = extraGraphics as RectTransform;
        float tierY = extraGraphicsRect != null
            ? extraGraphicsRect.anchoredPosition.y - RowStep
            : -387f;

        graphicsTierRect.anchorMin = new Vector2(0.5f, 1f);
        graphicsTierRect.anchorMax = new Vector2(0.5f, 1f);
        graphicsTierRect.pivot = new Vector2(0.5f, 1f);
        graphicsTierRect.anchoredPosition = new Vector2(0f, tierY);

        if (extraGraphics != null)
            graphicsTierRect.SetSiblingIndex(extraGraphics.GetSiblingIndex() + 1);

        Transform cpuMonitor = parent.Find(CpuMonitorRowName);
        if (cpuMonitor is RectTransform cpuRect)
        {
            cpuRect.anchorMin = new Vector2(0.5f, 1f);
            cpuRect.anchorMax = new Vector2(0.5f, 1f);
            cpuRect.pivot = new Vector2(0.5f, 1f);
            cpuRect.anchoredPosition = new Vector2(0f, tierY - RowStep);
            cpuRect.SetSiblingIndex(graphicsTierRect.GetSiblingIndex() + 1);
        }
    }

    static GameObject CreateRow(Transform parent, RectTransform extraGraphicsRect)
    {
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

        int sibling = extraGraphicsRect != null ? extraGraphicsRect.GetSiblingIndex() + 1 : parent.childCount;
        rowGo.transform.SetSiblingIndex(sibling);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var labelGo = new GameObject(LabelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelGo.layer = rowGo.layer;
        labelGo.transform.SetParent(rowGo.transform, false);
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0.38f, 1f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(10f, 0f);
        labelRect.sizeDelta = Vector2.zero;

        var label = labelGo.GetComponent<Text>();
        label.font = font;
        label.fontSize = 28;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
        label.text = "Graphics quality";
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;

        var dropdownGo = new GameObject(DropdownName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Dropdown));
        dropdownGo.layer = rowGo.layer;
        dropdownGo.transform.SetParent(rowGo.transform, false);
        var dropdownRect = dropdownGo.GetComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0.4f, 0.2f);
        dropdownRect.anchorMax = new Vector2(0.95f, 0.8f);
        dropdownRect.offsetMin = Vector2.zero;
        dropdownRect.offsetMax = Vector2.zero;

        var dropdownImage = dropdownGo.GetComponent<Image>();
        dropdownImage.color = new Color(0.15f, 0.18f, 0.25f, 0.95f);

        var dropdown = dropdownGo.GetComponent<Dropdown>();
        dropdown.targetGraphic = dropdownImage;

        var captionGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        captionGo.layer = rowGo.layer;
        captionGo.transform.SetParent(dropdownGo.transform, false);
        StretchWithPadding(captionGo.GetComponent<RectTransform>(), 8f, 28f, 4f, 4f);
        var caption = captionGo.GetComponent<Text>();
        caption.font = font;
        caption.fontSize = 24;
        caption.alignment = TextAnchor.MiddleLeft;
        caption.color = Color.white;
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
        arrowText.color = Color.white;
        arrowText.text = "v";

        var templateGo = new GameObject("Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        templateGo.layer = rowGo.layer;
        templateGo.transform.SetParent(dropdownGo.transform, false);
        var templateRect = templateGo.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = Vector2.zero;
        templateRect.sizeDelta = new Vector2(0f, 120f);
        templateGo.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.98f);

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
        itemBgGo.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f, 1f);

        var itemCheckGo = new GameObject("Item Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        itemCheckGo.transform.SetParent(itemGo.transform, false);
        var checkRect = itemCheckGo.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0f, 0.5f);
        checkRect.anchorMax = new Vector2(0f, 0.5f);
        checkRect.sizeDelta = new Vector2(16f, 16f);
        checkRect.anchoredPosition = new Vector2(12f, 0f);
        itemCheckGo.GetComponent<Image>().color = new Color(0.45f, 0.85f, 1f, 1f);

        var itemLabelGo = new GameObject("Item Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        itemLabelGo.transform.SetParent(itemGo.transform, false);
        StretchWithPadding(itemLabelGo.GetComponent<RectTransform>(), 28f, 8f, 2f, 2f);
        var itemLabel = itemLabelGo.GetComponent<Text>();
        itemLabel.font = font;
        itemLabel.fontSize = 22;
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

    static Transform FindNamed(string name)
    {
        GameObject found = GameObject.Find(name);
        return found != null ? found.transform : null;
    }

    IEnumerator Start()
    {
        EnsureRow();
        ResolveReferences();
        isInitializing = true;

        GraphicsTierSettings.ModeChanged += OnModeChanged;
        LocalizationManager.OnLanguageChanged += RefreshLocalizedOptions;

        RebuildOptions();
        SyncFromSettings();

        if (tierDropdown != null)
        {
            tierDropdown.onValueChanged.RemoveAllListeners();
            tierDropdown.onValueChanged.AddListener(OnDropdownChanged);
        }

        yield return new WaitForEndOfFrame();
        isInitializing = false;
    }

    void OnDestroy()
    {
        GraphicsTierSettings.ModeChanged -= OnModeChanged;
        LocalizationManager.OnLanguageChanged -= RefreshLocalizedOptions;
    }

    void ResolveReferences()
    {
        if (tierDropdown == null)
            tierDropdown = transform.Find(DropdownName)?.GetComponent<Dropdown>();

        if (labelText == null)
        {
            Transform label = transform.Find(LabelName);
            if (label != null)
                labelText = label.GetComponent<Text>();
        }
    }

    void RebuildOptions()
    {
        if (tierDropdown == null)
            return;

        var options = new List<string>
        {
            Translate("GraphicsTierAuto", "Auto"),
            Translate("GraphicsTierBalanced", "Balanced"),
            Translate("GraphicsTierHigh", "High")
        };

        int previous = tierDropdown.value;
        tierDropdown.ClearOptions();
        tierDropdown.AddOptions(options);
        tierDropdown.SetValueWithoutNotify(Mathf.Clamp(previous, 0, options.Count - 1));

        if (labelText != null)
            labelText.text = Translate("GraphicsTierLabel", "Graphics quality");
    }

    void RefreshLocalizedOptions()
    {
        isInitializing = true;
        RebuildOptions();
        SyncFromSettings();
        isInitializing = false;
    }

    void SyncFromSettings()
    {
        if (tierDropdown == null)
            return;

        tierDropdown.SetValueWithoutNotify((int)GraphicsTierSettings.Mode);
    }

    void OnDropdownChanged(int index)
    {
        if (isInitializing)
            return;

        GraphicsTierSettings.SetMode((GraphicsTierMode)Mathf.Clamp(index, 0, 2));
        MobileUrpQualityApplicator.Apply();
    }

    void OnModeChanged(GraphicsTierMode _)
    {
        SyncFromSettings();
    }

    public void RefreshFromSaved()
    {
        isInitializing = true;
        RebuildOptions();
        SyncFromSettings();
        isInitializing = false;
        MobileUrpQualityApplicator.Apply();
    }

    static string Translate(string key, string fallback)
    {
        if (LocalizationManager.Instance == null)
            return fallback;

        string value = LocalizationManager.Instance.GetTranslation(key);
        return string.IsNullOrEmpty(value) ? fallback : value;
    }
}
