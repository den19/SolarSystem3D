using System.Collections.Generic;
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
    const float DescNavHeight = 32f;
    const float DescNavGap = 16f;
    const float DescNavArrowWidth = 36f;
    const float CloseButtonSize = 32f;
    const float Padding = 8f;

    RectTransform _panel;
    HudPanelDrag _drag;
    Image _frameImage;
    Image _portraitImage;
    TextMeshProUGUI _title;
    TextMeshProUGUI _description;
    RectTransform _descNavRow;
    Button _descPrevBtn;
    Button _descNextBtn;
    TextMeshProUGUI _descPageIndicator;
    Button _closeBtn;
    bool _userDismissed;
    ProbeModelKind _dismissedForModel;
    readonly List<string> _descPages = new List<string>(3);
    int _descPageIndex;
    ProbeModelKind _pagedModel = (ProbeModelKind)(-1);
    Language _pagedLanguage;
    float _lastPaginateWidth = -1f;
    float _lastPaginateHeight = -1f;
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
        ProbeSettings.LoadoutChanged += OnLoadoutChanged;
        ProbeSettings.UseProbeChanged += OnUseProbeChanged;
        ProbeSettings.UseProbeChanged += RefreshVisibility;
        if (ProbeSystemController.Instance != null)
            ProbeSystemController.Instance.StateChanged += OnStateChanged;
        LocalizationManager.OnLanguageChanged += RefreshContent;
        RefreshContent();
        RefreshVisibility(ProbeSettings.UseProbe);
    }

    void OnDisable()
    {
        ProbeSettings.LoadoutChanged -= OnLoadoutChanged;
        ProbeSettings.UseProbeChanged -= OnUseProbeChanged;
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

    void OnLoadoutChanged()
    {
        if (_userDismissed && ProbeSettings.Model != _dismissedForModel)
            _userDismissed = false;
        RefreshContent();
        RefreshVisibility(ProbeSettings.UseProbe);
    }

    void OnUseProbeChanged(bool enabled)
    {
        if (enabled)
        {
            _userDismissed = false;
            RefreshVisibility(enabled);
        }
    }

    void DismissPreview()
    {
        _userDismissed = true;
        _dismissedForModel = ProbeSettings.Model;
        RefreshVisibility(ProbeSettings.UseProbe);
    }

    void EnsureUiBuilt()
    {
        if (_portraitImage != null && _descNavRow != null && _description != null && _closeBtn != null)
        {
            // Keep runtime rects in sync with layout constants (Play Mode may keep old children).
            ApplyCompactLayout(_compactMode);
            return;
        }

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
        _description.overflowMode = TextOverflowModes.Overflow;
        _description.margin = new Vector4(0f, 0f, 0f, 2f);
        var descRt = _description.rectTransform;
        descRt.anchorMin = new Vector2(0f, 0f);
        descRt.anchorMax = new Vector2(1f, 1f);
        descRt.offsetMin = new Vector2(Padding, Padding + DescNavHeight + DescNavGap);
        descRt.offsetMax = new Vector2(-Padding, -(Padding + PortraitImageHeight + TitleHeight));

        BuildDescNavRow(frameGo.transform);
        BuildCloseButton(frameGo.transform);

        _panel.gameObject.SetActive(false);
    }

    void BuildCloseButton(Transform parent)
    {
        var go = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-Padding, -Padding);
        rt.sizeDelta = new Vector2(CloseButtonSize, CloseButtonSize);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.25f, 0.3f, 0.4f, 0.95f);
        image.raycastTarget = true;

        var label = CreateText(go.transform, "Label", 26f, TextAlignmentOptions.Center, false);
        label.text = "×";
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        Stretch(label.rectTransform);

        _closeBtn = go.GetComponent<Button>();
        _closeBtn.onClick.AddListener(DismissPreview);
    }

    void BuildDescNavRow(Transform parent)
    {
        var rowGo = new GameObject("DescNav", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowGo.layer = gameObject.layer;
        rowGo.transform.SetParent(parent, false);
        _descNavRow = rowGo.GetComponent<RectTransform>();
        _descNavRow.anchorMin = new Vector2(0f, 0f);
        _descNavRow.anchorMax = new Vector2(1f, 0f);
        _descNavRow.pivot = new Vector2(0.5f, 0f);
        _descNavRow.offsetMin = new Vector2(Padding, Padding);
        _descNavRow.offsetMax = new Vector2(-Padding, Padding + DescNavHeight);

        var layout = rowGo.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;

        _descPrevBtn = CreateDescNavArrow(rowGo.transform, "DescPrev", "◀", ShowPrevDescPage);
        _descPageIndicator = CreateText(rowGo.transform, "DescPage", 16f, TextAlignmentOptions.Center, false);
        var indicatorLe = _descPageIndicator.gameObject.AddComponent<LayoutElement>();
        indicatorLe.flexibleWidth = 1f;
        indicatorLe.minHeight = DescNavHeight;
        indicatorLe.preferredHeight = DescNavHeight;
        _descNextBtn = CreateDescNavArrow(rowGo.transform, "DescNext", "▶", ShowNextDescPage);
    }

    Button CreateDescNavArrow(Transform parent, string name, string glyph, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.14f, 0.18f, 0.28f, 1f);
        var le = go.GetComponent<LayoutElement>();
        le.minWidth = DescNavArrowWidth;
        le.preferredWidth = DescNavArrowWidth;
        le.minHeight = DescNavHeight;
        le.preferredHeight = DescNavHeight;
        le.flexibleWidth = 0f;

        var label = CreateText(go.transform, name + "_Text", 18f, TextAlignmentOptions.Center, false);
        label.text = glyph;
        label.fontStyle = FontStyles.Bold;
        Stretch(label.rectTransform);

        var button = go.GetComponent<Button>();
        button.onClick.AddListener(action);
        return button;
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
        if (_descPageIndicator != null)
            _descPageIndicator.font = font;
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

        var descRt = _description.rectTransform;
        descRt.offsetMin = new Vector2(Padding, Padding + DescNavHeight + DescNavGap);
        descRt.offsetMax = new Vector2(-Padding, -(Padding + portraitH + TitleHeight));
        _description.overflowMode = TextOverflowModes.Overflow;
        _description.margin = new Vector4(0f, 0f, 0f, 2f);

        _description.gameObject.SetActive(!compact);
        if (_descNavRow != null)
        {
            _descNavRow.offsetMin = new Vector2(Padding, Padding);
            _descNavRow.offsetMax = new Vector2(-Padding, Padding + DescNavHeight);
            _descNavRow.gameObject.SetActive(!compact);
        }
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

        RebuildDescPagesIfNeeded(model);
        ApplyCurrentDescPage();
    }

    void RebuildDescPagesIfNeeded(ProbeModelKind model)
    {
        Language language = LocalizationManager.CurrentLanguage;
        bool resetPage = _pagedModel != model || _pagedLanguage != language;
        _pagedModel = model;
        _pagedLanguage = language;
        if (resetPage)
            _descPageIndex = 0;

        PaginateRawPages(BuildRawPagesForCurrentModel(model));

        if (_descPageIndex >= _descPages.Count)
            _descPageIndex = 0;
    }

    void PaginateRawPages(List<string> rawPages)
    {
        _descPages.Clear();
        GetDescViewportSize(out float width, out float height);

        for (int i = 0; i < rawPages.Count; i++)
            AppendSplitPages(rawPages[i], width, height);

        if (_descPages.Count == 0)
            _descPages.Add(string.Empty);

        _lastPaginateWidth = width;
        _lastPaginateHeight = height;
    }

    void AppendSplitPages(string text, float width, float maxHeight)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        int start = 0;
        while (start < text.Length)
        {
            while (start < text.Length && char.IsWhiteSpace(text[start]))
                start++;
            if (start >= text.Length)
                break;

            int fit = MeasureFitLength(text, start, width, maxHeight);
            fit = RefinePageBreak(text, start, fit);
            _descPages.Add(text.Substring(start, fit).Trim());
            start += fit;
        }
    }

    int MeasureFitLength(string text, int start, float width, float maxHeight)
    {
        int remaining = text.Length - start;
        if (remaining <= 0)
            return 0;

        int lo = 1;
        int hi = remaining;
        int best = 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            if (TextFits(text.Substring(start, mid), width, maxHeight))
            {
                best = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return Mathf.Max(1, best);
    }

    bool TextFits(string slice, float width, float maxHeight)
    {
        if (_description == null || string.IsNullOrEmpty(slice))
            return true;

        Vector2 size = _description.GetPreferredValues(slice, width, 0f);
        return size.y <= maxHeight + 0.5f;
    }

    static int RefinePageBreak(string text, int start, int maxLen)
    {
        if (maxLen <= 0)
            return 1;

        int remaining = text.Length - start;
        if (maxLen >= remaining)
            return remaining;

        int searchFrom = start + Mathf.Max(1, maxLen - 48);
        int end = start + maxLen;
        for (int i = end - 1; i >= searchFrom; i--)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c) || c == '-' || c == '—' || c == '.' || c == ',' || c == ';' || c == ':')
                return i - start + 1;
        }

        return maxLen;
    }

    void GetDescViewportSize(out float width, out float height)
    {
        bool landscape = Screen.width > Screen.height;
        bool compact = IsCompactMode();
        float panelW = compact
            ? (landscape ? CompactWidthLandscape : CompactWidthPortrait)
            : (landscape ? PanelWidthLandscape : PanelWidthPortrait);
        float panelH = compact
            ? (landscape ? CompactHeightLandscape : CompactHeightPortrait)
            : (landscape ? PanelHeightLandscape : PanelHeightPortrait);
        float portraitH = compact ? CompactPortraitHeight : PortraitImageHeight;

        width = panelW - Padding * 2f;
        height = panelH - portraitH - TitleHeight - DescNavHeight - DescNavGap - Padding * 2f;

        if (_description != null)
        {
            Canvas.ForceUpdateCanvases();
            Rect rect = _description.rectTransform.rect;
            if (rect.width > 1f)
                width = rect.width;
            if (rect.height > 1f)
                height = rect.height;
        }

        width = Mathf.Max(80f, width);
        height = Mathf.Max(48f, height);
    }

    void RepaginateIfViewportChanged()
    {
        if (_description == null || _compactMode || !_panel.gameObject.activeSelf)
            return;

        GetDescViewportSize(out float width, out float height);
        if (Mathf.Approximately(width, _lastPaginateWidth)
            && Mathf.Approximately(height, _lastPaginateHeight))
        {
            return;
        }

        int savedIndex = _descPageIndex;
        PaginateRawPages(BuildRawPagesForCurrentModel(_pagedModel));
        _descPageIndex = Mathf.Clamp(savedIndex, 0, Mathf.Max(0, _descPages.Count - 1));
        ApplyCurrentDescPage();
    }

    List<string> BuildRawPagesForCurrentModel(ProbeModelKind model)
    {
        var rawPages = new List<string>(3);
        string[] keys = ProbeModelCatalog.GetDescriptionPageKeys(model);
        for (int i = 0; i < keys.Length; i++)
        {
            string text = T(keys[i], string.Empty);
            if (string.IsNullOrEmpty(text))
                continue;

            if (i == 0 && model == ProbeModelKind.Custom)
            {
                string parts = BuildCustomPartsLine();
                if (!string.IsNullOrEmpty(parts))
                    text = text + "\n" + parts;
            }

            rawPages.Add(text);
        }

        if (rawPages.Count == 0)
            rawPages.Add(string.Empty);
        return rawPages;
    }

    void ApplyCurrentDescPage()
    {
        if (_description == null)
            return;

        if (_descPages.Count == 0)
        {
            _description.text = string.Empty;
            RefreshDescNav();
            return;
        }

        _descPageIndex = Mathf.Clamp(_descPageIndex, 0, _descPages.Count - 1);
        _description.text = _descPages[_descPageIndex];
        RefreshDescNav();
    }

    void RefreshDescNav()
    {
        int pageCount = Mathf.Max(1, _descPages.Count);
        int pageNumber = _descPageIndex + 1;
        string format = T("ProbeDescPageFormat", "{0}/{1}");
        if (_descPageIndicator != null)
            _descPageIndicator.text = string.Format(format, pageNumber, pageCount);

        bool multi = _descPages.Count > 1;
        if (_descPrevBtn != null)
            _descPrevBtn.interactable = multi && _descPageIndex > 0;
        if (_descNextBtn != null)
            _descNextBtn.interactable = multi && _descPageIndex < _descPages.Count - 1;
        if (_descNavRow != null && !_compactMode)
            _descNavRow.gameObject.SetActive(true);
    }

    void ShowPrevDescPage()
    {
        if (_descPageIndex <= 0)
            return;
        _descPageIndex--;
        ApplyCurrentDescPage();
    }

    void ShowNextDescPage()
    {
        if (_descPageIndex >= _descPages.Count - 1)
            return;
        _descPageIndex++;
        ApplyCurrentDescPage();
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
        if (!useProbe || _userDismissed)
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
        else
            RepaginateIfViewportChanged();
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
