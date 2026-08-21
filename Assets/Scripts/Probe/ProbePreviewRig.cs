using SolarSystemApp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Probe info card on HUD: portrait sprite, title, and localized description.
/// </summary>
public class ProbePreviewRig : MonoBehaviour
{
    const float PanelWidthPortrait = 300f;
    const float PanelHeightPortrait = 400f;
    const float PanelWidthLandscape = 260f;
    const float PanelHeightLandscape = 340f;
    const float CompactWidthPortrait = 240f;
    const float CompactHeightPortrait = 268f;
    const float CompactWidthLandscape = 220f;
    const float CompactHeightLandscape = 248f;
    const float PortraitImageHeight = 192f;
    const float CompactPortraitHeight = 160f;
    const float TitleHeight = 32f;
    const float DescHeight = 96f;
    const float Padding = 8f;

    RectTransform _panel;
    HudPanelDrag _drag;
    Image _frameImage;
    Image _portraitImage;
    TextMeshProUGUI _title;
    TextMeshProUGUI _description;
    ProbeModelKind _shownModel = (ProbeModelKind)(-1);
    bool _customAntenna;
    bool _customEngine;
    bool _customShield;
    bool _compactMode;

    public static ProbePreviewRig EnsureOnHud(Transform hudRoot)
    {
        if (hudRoot == null)
            return null;

        Transform existing = hudRoot.Find("ProbePreview");
        if (existing != null)
        {
            var rig = existing.GetComponent<ProbePreviewRig>();
            if (rig == null)
                rig = existing.gameObject.AddComponent<ProbePreviewRig>();
            rig.EnsureUiBuilt();
            return rig;
        }

        var go = new GameObject("ProbePreview", typeof(RectTransform), typeof(ProbePreviewRig));
        go.layer = hudRoot.gameObject.layer;
        go.transform.SetParent(hudRoot, false);
        var preview = go.GetComponent<ProbePreviewRig>();
        preview.BuildUi();
        return preview;
    }

    void OnEnable()
    {
        EnsureUiBuilt();
        ProbeSettings.LoadoutChanged += RefreshContent;
        ProbeSettings.UseProbeChanged += RefreshVisibility;
        if (ProbeSystemController.Instance != null)
            ProbeSystemController.Instance.StateChanged += OnStateChanged;
        LocalizationManager.OnLanguageChanged += RefreshContent;
        RefreshContent();
        RefreshVisibility(ProbeSettings.UseProbe);
    }

    void OnDisable()
    {
        ProbeSettings.LoadoutChanged -= RefreshContent;
        ProbeSettings.UseProbeChanged -= RefreshVisibility;
        if (ProbeSystemController.Instance != null)
            ProbeSystemController.Instance.StateChanged -= OnStateChanged;
        LocalizationManager.OnLanguageChanged -= RefreshContent;
    }

    void OnStateChanged()
    {
        RefreshVisibility(ProbeSettings.UseProbe);
        RefreshContent();
    }

    void Update()
    {
        LayoutPanel();
    }

    void EnsureUiBuilt()
    {
        if (_portraitImage != null)
            return;

        Transform root = _panel != null ? _panel : transform;
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);

        BuildUi();
    }

    void BuildUi()
    {
        _panel = GetComponent<RectTransform>();
        _panel.anchorMin = new Vector2(0f, 1f);
        _panel.anchorMax = new Vector2(0f, 1f);
        _panel.pivot = new Vector2(0f, 1f);
        _drag = _panel.GetComponent<HudPanelDrag>();
        if (_drag == null)
            _drag = _panel.gameObject.AddComponent<HudPanelDrag>();

        var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frameGo.layer = gameObject.layer;
        frameGo.transform.SetParent(_panel, false);
        var frameRt = frameGo.GetComponent<RectTransform>();
        Stretch(frameRt);
        _frameImage = frameGo.GetComponent<Image>();
        _frameImage.color = new Color(0.12f, 0.16f, 0.24f, 0.94f);
        _frameImage.raycastTarget = true;
        var outline = frameGo.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.85f, 1f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        var portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        portraitGo.layer = gameObject.layer;
        portraitGo.transform.SetParent(frameGo.transform, false);
        var portraitRt = portraitGo.GetComponent<RectTransform>();
        portraitRt.anchorMin = new Vector2(0f, 1f);
        portraitRt.anchorMax = new Vector2(1f, 1f);
        portraitRt.pivot = new Vector2(0.5f, 1f);
        portraitRt.offsetMin = new Vector2(Padding, -(Padding + PortraitImageHeight));
        portraitRt.offsetMax = new Vector2(-Padding, -Padding);
        _portraitImage = portraitGo.GetComponent<Image>();
        _portraitImage.raycastTarget = false;
        _portraitImage.preserveAspect = true;
        _portraitImage.type = Image.Type.Simple;
        ApplyPortraitSprite(null);

        _title = CreateText(frameGo.transform, "Title", 23f, TextAlignmentOptions.Midline, false);
        var titleRt = _title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.offsetMin = new Vector2(Padding, -(Padding + PortraitImageHeight + TitleHeight));
        titleRt.offsetMax = new Vector2(-Padding, -(Padding + PortraitImageHeight));

        _description = CreateText(frameGo.transform, "Description", 19f, TextAlignmentOptions.TopLeft, true);
        var descRt = _description.rectTransform;
        descRt.anchorMin = new Vector2(0f, 0f);
        descRt.anchorMax = new Vector2(1f, 1f);
        descRt.offsetMin = new Vector2(Padding, Padding);
        descRt.offsetMax = new Vector2(-Padding, -(Padding + PortraitImageHeight + TitleHeight));

        _panel.gameObject.SetActive(false);
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, float fontSize, TextAlignmentOptions align, bool wrap)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = new Color(0.88f, 0.94f, 1f, 1f);
        tmp.enableWordWrapping = wrap;
        tmp.raycastTarget = false;
        var font = LocalizationFontHelper.GetFontForLanguage(LocalizationManager.CurrentLanguage);
        if (font != null)
            tmp.font = font;
        return tmp;
    }

    void RefreshContent()
    {
        ProbeModelKind model = ResolveDisplayModel();
        bool antenna = ProbeSettings.CustomAntenna;
        bool engine = ProbeSettings.CustomEngine;
        bool shield = ProbeSettings.CustomShield;
        bool compact = IsCompactMode();

        Sprite portrait = _portraitImage != null ? ProbePortraitLibrary.Get(model) : null;
        bool portraitReady = portrait != null;

        if (_shownModel == model
            && _customAntenna == antenna
            && _customEngine == engine
            && _customShield == shield
            && _compactMode == compact
            && _portraitImage != null
            && _portraitImage.sprite == portrait
            && (_portraitImage.enabled == portraitReady))
        {
            RefreshTexts(model, compact);
            return;
        }

        _shownModel = model;
        _customAntenna = antenna;
        _customEngine = engine;
        _customShield = shield;
        _compactMode = compact;

        if (_portraitImage != null)
            ApplyPortraitSprite(portrait);

        ApplyCompactLayout(compact);
        ApplyFonts();
        RefreshTexts(model, compact);
    }

    void ApplyFonts()
    {
        var font = LocalizationFontHelper.GetFontForLanguage(LocalizationManager.CurrentLanguage);
        if (font == null)
            return;
        if (_title != null)
            _title.font = font;
        if (_description != null)
            _description.font = font;
    }

    ProbeModelKind ResolveDisplayModel()
    {
        var system = ProbeSystemController.Instance;
        if (system != null && system.IsFlying && system.Craft != null)
            return system.Craft.Model;
        return ProbeSettings.Model;
    }

    bool IsCompactMode()
    {
        var system = ProbeSystemController.Instance;
        return system != null && system.IsFlying && system.FlightPreviewGraceRemaining > 0f;
    }

    static void ApplyPortraitSprite(Image portraitImage, Sprite sprite)
    {
        portraitImage.sprite = sprite;
        if (sprite != null)
        {
            portraitImage.enabled = true;
            portraitImage.color = Color.white;
            return;
        }

        portraitImage.enabled = false;
        portraitImage.color = new Color(1f, 1f, 1f, 0f);
    }

    void ApplyPortraitSprite(Sprite sprite)
    {
        if (_portraitImage == null)
            return;

        ApplyPortraitSprite(_portraitImage, sprite);
    }

    void ApplyCompactLayout(bool compact)
    {
        if (_portraitImage == null || _title == null || _description == null)
            return;

        float portraitH = compact ? CompactPortraitHeight : PortraitImageHeight;
        var portraitRt = _portraitImage.rectTransform;
        portraitRt.offsetMin = new Vector2(Padding, -(Padding + portraitH));
        portraitRt.offsetMax = new Vector2(-Padding, -Padding);

        var titleRt = _title.rectTransform;
        titleRt.offsetMin = new Vector2(Padding, -(Padding + portraitH + TitleHeight));
        titleRt.offsetMax = new Vector2(-Padding, -(Padding + portraitH));

        _description.gameObject.SetActive(!compact);
    }

    void RefreshTexts(ProbeModelKind model, bool compact)
    {
        if (_title == null)
            return;

        string modelName = ProbeHudController.ResolveModelLabel(model);
        if (model == ProbeModelKind.Custom)
        {
            string a = ProbeSettings.CustomAntenna ? "A" : "—";
            string e = ProbeSettings.CustomEngine ? "E" : "—";
            string s = ProbeSettings.CustomShield ? "S" : "—";
            _title.text = modelName + "  " + a + " · " + e + " · " + s;
        }
        else
        {
            _title.text = modelName;
        }

        if (_description == null || compact)
            return;

        string descKey = ProbeModelCatalog.GetDescriptionKey(model);
        _description.text = T(descKey, string.Empty);

        if (model == ProbeModelKind.Custom)
        {
            string parts = BuildCustomPartsLine();
            if (!string.IsNullOrEmpty(parts))
                _description.text = _description.text + "\n" + parts;
        }
    }

    static string BuildCustomPartsLine()
    {
        string a = ProbeSettings.CustomAntenna ? T("ProbePartAntenna", "Antenna") : "—";
        string e = ProbeSettings.CustomEngine ? T("ProbePartEngine", "Engine") : "—";
        string s = ProbeSettings.CustomShield ? T("ProbePartShield", "Shield") : "—";
        return a + " · " + e + " · " + s;
    }

    void RefreshVisibility(bool useProbe)
    {
        bool show = ShouldShowPreview(useProbe);
        if (_panel != null)
            _panel.gameObject.SetActive(show);
    }

    bool ShouldShowPreview(bool useProbe)
    {
        if (!useProbe)
            return false;

        var system = ProbeSystemController.Instance;
        if (system == null)
            return true;

        if (!system.IsFlying)
            return true;

        return system.FlightPreviewGraceRemaining > 0f;
    }

    void LayoutPanel()
    {
        if (_panel == null || !_panel.gameObject.activeSelf)
            return;

        bool landscape = Screen.width > Screen.height;
        bool compact = IsCompactMode();
        float width;
        float height;
        if (compact)
        {
            width = landscape ? CompactWidthLandscape : CompactWidthPortrait;
            height = landscape ? CompactHeightLandscape : CompactHeightPortrait;
        }
        else
        {
            width = landscape ? PanelWidthLandscape : PanelWidthPortrait;
            height = landscape ? PanelHeightLandscape : PanelHeightPortrait;
        }

        _panel.sizeDelta = new Vector2(width, height);

        Canvas canvas = GetComponentInParent<Canvas>();
        SafeAreaInsets.GetCanvasInsets(canvas, out float left, out _, out float top, out _);
        float navBottom = top + SidePanelUiBootstrap.BarHeight + 8f;
        if (_drag != null && _drag.HasUserOffset)
            _drag.EnsureClamped();
        else
            _panel.anchoredPosition = new Vector2(left + 8f, -(navBottom + 8f));

        if (_compactMode != compact)
            RefreshContent();
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static string T(string key, string fallback)
    {
        if (LocalizationManager.Instance == null)
            return fallback;
        string t = LocalizationManager.Instance.GetTranslation(key);
        return string.IsNullOrEmpty(t) ? fallback : t;
    }
}
