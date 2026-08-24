using System.Text;
using SolarSystemApp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Probe launch HUD, camera modes, PIP views, and telemetry.
/// </summary>
public class ProbeHudController : MonoBehaviour
{
    public const string ObjectName = ProbeSystemController.HudObjectName;

    const float UpdateInterval = 0.1f;
    const float RowHeight = 56f;
    const float RowHeightLandscape = 56f;
    const float ModelRowHeight = 96f;
    const float ModelRowHeightLandscape = 96f;
    const float ButtonFontSize = 26f;
    const float ModelLabelFontSizeMin = 18f;
    const float ModelLabelFontSizeMax = 26f;
    const float TelemetryFontSize = 26f;
    const float TelemetryHeaderFontSize = 27f;
    const float TelemetryStatusFontSize = 22f;
    const float CheckboxSize = 28f;
    const float SliderHeight = 36f;
    const float SliderHeightLandscape = 36f;
    const float TelemetryWidth = 360f;
    const float TelemetryWidthLandscape = 400f;
    const float TelemetryHeight = 392f;
    const float TelemetryHeightLandscape = 220f;
    const float TelemetryHeaderHeight = 52f;
    const float BarWidthLandscape = 960f;
    const float BarHeightStandard = 420f;
    const float BarHeightCustom = 466f;
    // Landscape bar preferred size must not be smaller than Portrait.
    const float BarHeightLandscape = 420f;
    const float BarHeightCustomLandscape = 466f;
    const float BarTopReserve = 24f;
    const float CameraButtonMinWidth = 168f;
    const float ViewsTogglePreferredWidth = 220f;
    const float PipWidthLandscape = 280f;
    const float PipHeightLandscape = 120f;
    const float PipWidthPortrait = 280f;
    const float PipHeightPortrait = 150f;
    const float PipFrameInset = 4f;
    const float PipCaptionHeight = 28f;
    const float ModelScrollArrowWidth = 48f;
    const float ModelButtonWidthPortrait = 148f;
    const float ModelButtonWidthLandscape = 148f;
    const float SafeAreaChangeThresholdPx = 2f;
    const float SafeAreaLayoutDebounceSeconds = 0.25f;

    RectTransform _root;
    RectTransform _bar;
    RectTransform _telemetry;
    HudPanelDrag _telemetryDrag;
    RectTransform _forwardPip;
    RectTransform _rearLeftPip;
    RectTransform _rearRightPip;
    RawImage _forwardImage;
    RawImage _rearLeftImage;
    RawImage _rearRightImage;
    Slider _impulse;
    Slider _heading;
    Toggle _viewsToggle;
    Toggle _antennaToggle;
    Toggle _engineToggle;
    Toggle _shieldToggle;
    TextMeshProUGUI _telemetryText;
    TextMeshProUGUI _telemetryHeader;
    TextMeshProUGUI _telemetryStatus;
    RectTransform _telemetryBody;
    GameObject _customRow;
    Button _burnButton;
    Button _postcardButton;
    Button _helpButton;
    ProbeCoachOverlay _coachOverlay;
    ProbePreviewRig _previewRig;
    ScrollRect _modelScroll;
    RectTransform _modelScrollContent;
    Button _modelPrevBtn;
    Button _modelNextBtn;
    readonly System.Collections.Generic.Dictionary<ProbeModelKind, Image> _modelButtonImages =
        new System.Collections.Generic.Dictionary<ProbeModelKind, Image>();
    float _nextTelemetry;
    int _layoutScreenW = -1;
    int _layoutScreenH = -1;
    Rect _layoutSafeArea;
    ProbeModelKind _layoutModel = (ProbeModelKind)(-1);
    float _layoutModelButtonWidth = -1f;
    bool _layoutPipsVisible;
    float _safeAreaLayoutReadyAt = -1f;
    Rect _pendingSafeArea;

    // Telemetry hot-path scratch (avoids per-tick GC at ~10 Hz).
    readonly ProbeGravityIntegrator.Attractor[] _findScratch =
        new ProbeGravityIntegrator.Attractor[ProbeSystemController.MaxAttractors];
    readonly float[] _findDist = new float[ProbeSystemController.MaxAttractors];
    readonly string[] _findNames = new string[ProbeSystemController.MaxAttractors];
    readonly StringBuilder _telemetrySb = new StringBuilder(256);
    string _lastTelemetryBody;
    string _lastTelemetryHeader;
    string _lastTelemetryStatus;

    public static ProbeHudController EnsureOnCanvas(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return null;

        Transform existing = canvasTransform.Find(ObjectName);
        if (existing != null)
        {
            var controller = existing.GetComponent<ProbeHudController>();
            if (controller == null)
                controller = existing.gameObject.AddComponent<ProbeHudController>();
            controller.EnsureExtensions();
            return controller;
        }

        var go = new GameObject(ObjectName, typeof(RectTransform), typeof(ProbeHudController));
        go.layer = canvasTransform.gameObject.layer;
        go.transform.SetParent(canvasTransform, false);
        var hud = go.GetComponent<ProbeHudController>();
        hud.Build();
        return hud;
    }

    void OnEnable()
    {
        if (ProbeSystemController.Instance != null)
            ProbeSystemController.Instance.StateChanged += RefreshState;
        ProbeSettings.LoadoutChanged += RefreshState;
        ProbeSettings.ShowProbeViewsChanged += RefreshPips;
        ProbeSettings.UseProbeChanged += OnUseProbeChanged;
        ProbeCoachSettings.LaunchCompletedChanged += OnCoachCompleted;
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    void OnDisable()
    {
        if (ProbeSystemController.Instance != null)
            ProbeSystemController.Instance.StateChanged -= RefreshState;
        ProbeSettings.LoadoutChanged -= RefreshState;
        ProbeSettings.ShowProbeViewsChanged -= RefreshPips;
        ProbeSettings.UseProbeChanged -= OnUseProbeChanged;
        ProbeCoachSettings.LaunchCompletedChanged -= OnCoachCompleted;
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    void OnLanguageChanged()
    {
        RefreshLabels();
        RefreshTelemetryHeader();
        RefreshTelemetryNow();
        ApplyHudFontSizes();
        InvalidateLayoutCache();
        Layout();
    }

    void OnUseProbeChanged(bool _) => RefreshRootVisibility();

    void OnCoachCompleted() => RefreshRootVisibility();

    void Start()
    {
        if (_root == null)
            Build();
        else
            EnsureExtensions();
        RefreshState();
    }

    void Update()
    {
        if (NeedsLayout())
            Layout();

        if (Time.unscaledTime < _nextTelemetry)
            return;

        _nextTelemetry = Time.unscaledTime + UpdateInterval;
        RefreshTelemetryNow();
        RefreshPips();
    }

    bool NeedsLayout()
    {
        if (_bar == null)
            return false;
        if (_layoutScreenW != Screen.width || _layoutScreenH != Screen.height)
        {
            ClearSafeAreaLayoutDebounce();
            return true;
        }

        if (_layoutModel != ProbeSettings.Model)
            return true;
        bool pipsVisible = ProbeSettings.ShowProbeViews
            && ProbeSystemController.Instance != null
            && ProbeSystemController.Instance.IsFlying;
        if (pipsVisible != _layoutPipsVisible)
            return true;

        if (HasSignificantSafeAreaChange(Screen.safeArea))
            return IsSafeAreaLayoutDebounceReady();

        ClearSafeAreaLayoutDebounce();
        return false;
    }

    bool HasSignificantSafeAreaChange(Rect safeArea)
    {
        return !SafeAreaApproximatelyEqual(_layoutSafeArea, safeArea, SafeAreaChangeThresholdPx);
    }

    bool IsSafeAreaLayoutDebounceReady()
    {
        Rect safeArea = Screen.safeArea;
        if (_safeAreaLayoutReadyAt < 0f
            || !SafeAreaApproximatelyEqual(_pendingSafeArea, safeArea, SafeAreaChangeThresholdPx))
        {
            _pendingSafeArea = safeArea;
            _safeAreaLayoutReadyAt = Time.unscaledTime + SafeAreaLayoutDebounceSeconds;
            return false;
        }

        return Time.unscaledTime >= _safeAreaLayoutReadyAt;
    }

    void ClearSafeAreaLayoutDebounce()
    {
        _safeAreaLayoutReadyAt = -1f;
    }

    static bool SafeAreaApproximatelyEqual(Rect a, Rect b, float thresholdPx)
    {
        return Mathf.Abs(a.x - b.x) <= thresholdPx
            && Mathf.Abs(a.y - b.y) <= thresholdPx
            && Mathf.Abs(a.width - b.width) <= thresholdPx
            && Mathf.Abs(a.height - b.height) <= thresholdPx;
    }

    void InvalidateLayoutCache()
    {
        _layoutScreenW = -1;
        _layoutScreenH = -1;
        _layoutModelButtonWidth = -1f;
    }

    void Build()
    {
        _root = GetComponent<RectTransform>();
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = Vector2.zero;
        _root.offsetMax = Vector2.zero;

        _bar = CreatePanel("ProbeControlBar", new Color(0.08f, 0.1f, 0.16f, 0.92f));
        var layout = _bar.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        CreateModelRow(_bar);
        _customRow = CreateCustomRow(_bar).gameObject;
        CreateSliderRow(_bar, "ProbeImpulseLabel", "Impulse", out _impulse, 0.15f, 1f, ProbeSystemController.Instance != null ? ProbeSystemController.Instance.ImpulseNormalized : 0.55f);
        CreateSliderRow(_bar, "ProbeHeadingLabel", "Heading", out _heading, -180f, 180f, 0f);
        CreateActionRow(_bar);
        CreateCameraRow(_bar);

        _impulse.onValueChanged.AddListener(v =>
        {
            if (ProbeSystemController.Instance != null)
                ProbeSystemController.Instance.ImpulseNormalized = v;
        });
        _heading.onValueChanged.AddListener(v =>
        {
            if (ProbeSystemController.Instance != null)
                ProbeSystemController.Instance.AimHeadingDeg = v;
        });

        _telemetry = CreatePanel("ProbeTelemetry", new Color(0.02f, 0.05f, 0.1f, 0.78f));
        var telemetryImage = _telemetry.GetComponent<Image>();
        if (telemetryImage != null)
            telemetryImage.raycastTarget = true;
        _telemetryDrag = _telemetry.GetComponent<HudPanelDrag>();
        if (_telemetryDrag == null)
            _telemetryDrag = _telemetry.gameObject.AddComponent<HudPanelDrag>();
        BuildTelemetryHeader(_telemetry);
        _telemetryBody = CreateTelemetryBody(_telemetry);
        _telemetryText = CreateTmp(_telemetryBody, "ProbeTelemetryBody", TelemetryFontSize, TextAlignmentOptions.TopLeft);
        var le = _telemetryText.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 280f;
        _telemetryText.margin = new Vector4(10f, 8f, 10f, 8f);

        _forwardPip = CreatePip("ProbeViewForwardImage", string.Empty, out _forwardImage);
        _rearLeftPip = CreatePip("ProbeViewRearLeftImage", "◀", out _rearLeftImage);
        _rearRightPip = CreatePip("ProbeViewRearRightImage", "▶", out _rearRightImage);

        _coachOverlay = ProbeCoachOverlay.EnsureOnHud(_root);
        _previewRig = ProbePreviewRig.EnsureOnHud(_root);
        _helpButton = CreateHelpButton(_root);
        ApplyHudFontSizes();
        Layout();
    }

    void EnsureExtensions()
    {
        if (_root == null)
            _root = GetComponent<RectTransform>();
        if (_root == null)
            return;

        if (_bar == null)
        {
            Build();
            return;
        }

        if (_coachOverlay == null)
            _coachOverlay = ProbeCoachOverlay.EnsureOnHud(_root);
        if (_previewRig == null)
            _previewRig = ProbePreviewRig.EnsureOnHud(_root);
        if (_helpButton == null)
            _helpButton = CreateHelpButton(_root);
        EnsureModelScroller();
        if (_telemetry != null && _telemetryDrag == null)
        {
            var telemetryImage = _telemetry.GetComponent<Image>();
            if (telemetryImage != null)
                telemetryImage.raycastTarget = true;
            _telemetryDrag = _telemetry.GetComponent<HudPanelDrag>();
            if (_telemetryDrag == null)
                _telemetryDrag = _telemetry.gameObject.AddComponent<HudPanelDrag>();
        }
        if (_telemetryHeader == null && _telemetry != null)
        {
            BuildTelemetryHeader(_telemetry);
            if (_telemetryBody == null)
            {
                _telemetryBody = CreateTelemetryBody(_telemetry);
                if (_telemetryText != null)
                    _telemetryText.transform.SetParent(_telemetryBody, false);
            }
        }
    }

    void BuildTelemetryHeader(RectTransform parent)
    {
        var headerGo = new GameObject("ProbeTelemetryHeaderRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        headerGo.layer = gameObject.layer;
        headerGo.transform.SetParent(parent, false);
        var headerRt = headerGo.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.sizeDelta = new Vector2(0f, TelemetryHeaderHeight);
        headerGo.GetComponent<Image>().color = new Color(0.05f, 0.1f, 0.16f, 0.92f);

        var accentGo = new GameObject("Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        accentGo.layer = gameObject.layer;
        accentGo.transform.SetParent(headerGo.transform, false);
        var accentRt = accentGo.GetComponent<RectTransform>();
        accentRt.anchorMin = new Vector2(0f, 0f);
        accentRt.anchorMax = new Vector2(1f, 0f);
        accentRt.pivot = new Vector2(0.5f, 0f);
        accentRt.sizeDelta = new Vector2(0f, 2f);
        accentGo.GetComponent<Image>().color = new Color(0.35f, 0.85f, 1f, 0.95f);

        _telemetryHeader = CreateTmp(headerGo.GetComponent<RectTransform>(), "ProbeTelemetryHeader", TelemetryHeaderFontSize, TextAlignmentOptions.TopLeft);
        _telemetryHeader.fontStyle = FontStyles.Bold;
        var headerTextRt = _telemetryHeader.GetComponent<RectTransform>();
        headerTextRt.anchorMin = new Vector2(0f, 0.5f);
        headerTextRt.anchorMax = new Vector2(1f, 1f);
        headerTextRt.offsetMin = new Vector2(10f, 2f);
        headerTextRt.offsetMax = new Vector2(-10f, -4f);

        _telemetryStatus = CreateTmp(headerGo.GetComponent<RectTransform>(), "ProbeTelemetryStatus", TelemetryStatusFontSize, TextAlignmentOptions.BottomLeft);
        var statusRt = _telemetryStatus.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0f, 0f);
        statusRt.anchorMax = new Vector2(1f, 0.5f);
        statusRt.offsetMin = new Vector2(10f, 4f);
        statusRt.offsetMax = new Vector2(-10f, -2f);
    }

    static RectTransform CreateTelemetryBody(RectTransform parent)
    {
        var go = new GameObject("ProbeTelemetryBodyPanel", typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(0f, 0f);
        rt.offsetMax = new Vector2(0f, -TelemetryHeaderHeight);
        return rt;
    }

    Button CreateHelpButton(RectTransform parent)
    {
        var go = new GameObject("ProbeCoachHelp", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(52f, 52f);
        go.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.24f, 0.9f);

        var label = CreateTmp(rt, "ProbeCoachHelp_Text", ButtonFontSize, TextAlignmentOptions.Center);
        label.text = "?";
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;

        var button = go.GetComponent<Button>();
        button.onClick.AddListener(() => _coachOverlay?.OpenManual());
        return button;
    }

    void EnsureModelScroller()
    {
        if (_bar == null || _modelScroll != null)
            return;

        Transform oldRow = _bar.Find("ProbeModelRow");
        if (oldRow != null)
            Destroy(oldRow.gameObject);

        _modelButtonImages.Clear();
        var row = CreateModelRow(_bar);
        row.SetAsFirstSibling();
    }

    RectTransform CreateModelRow(RectTransform parent)
    {
        var row = CreateRow(parent, "ProbeModelRow", ModelRowHeight);
        var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.childForceExpandWidth = false;

        _modelPrevBtn = CreateScrollArrow(row, "ProbeModelPrev", "◀", ScrollModelsPrev);
        BuildModelScrollViewport(row);
        _modelNextBtn = CreateScrollArrow(row, "ProbeModelNext", "▶", ScrollModelsNext);

        var entries = ProbeModelCatalog.PickerEntries;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            CreateModeButton(_modelScrollContent, entry.LocalizationKey, entry.Kind);
        }

        if (_modelScroll != null)
            _modelScroll.onValueChanged.AddListener(_ => RefreshModelScrollArrows());

        Canvas.ForceUpdateCanvases();
        ScrollModelIntoView(ProbeSettings.Model, instant: true);
        RefreshModelScrollArrows();
        return row;
    }

    void BuildModelScrollViewport(RectTransform row)
    {
        var viewportGo = new GameObject("ProbeModelViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask), typeof(ScrollRect), typeof(LayoutElement));
        viewportGo.layer = gameObject.layer;
        viewportGo.transform.SetParent(row, false);
        var viewportLe = viewportGo.GetComponent<LayoutElement>();
        viewportLe.flexibleWidth = 1f;
        viewportLe.minHeight = ModelRowHeight;
        viewportLe.preferredHeight = ModelRowHeight;
        viewportGo.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.85f);

        var viewportRt = viewportGo.GetComponent<RectTransform>();
        _modelScroll = viewportGo.GetComponent<ScrollRect>();
        _modelScroll.horizontal = true;
        _modelScroll.vertical = false;
        _modelScroll.movementType = ScrollRect.MovementType.Clamped;
        _modelScroll.inertia = true;
        _modelScroll.decelerationRate = 0.2f;
        _modelScroll.scrollSensitivity = 24f;

        var contentGo = new GameObject("ProbeModelContent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.layer = gameObject.layer;
        contentGo.transform.SetParent(viewportGo.transform, false);
        _modelScrollContent = contentGo.GetComponent<RectTransform>();
        _modelScrollContent.anchorMin = new Vector2(0f, 0.5f);
        _modelScrollContent.anchorMax = new Vector2(0f, 0.5f);
        _modelScrollContent.pivot = new Vector2(0f, 0.5f);
        _modelScrollContent.anchoredPosition = Vector2.zero;
        var contentLayout = contentGo.GetComponent<HorizontalLayoutGroup>();
        contentLayout.spacing = 6f;
        contentLayout.childAlignment = TextAnchor.MiddleLeft;
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = false;
        contentLayout.padding = new RectOffset(4, 4, 0, 0);
        var fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        _modelScroll.content = _modelScrollContent;
        _modelScroll.viewport = viewportRt;
        Stretch(viewportRt);
    }

    Button CreateScrollArrow(RectTransform parent, string name, string glyph, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.14f, 0.18f, 0.28f, 1f);
        var le = go.GetComponent<LayoutElement>();
        le.minWidth = ModelScrollArrowWidth;
        le.preferredWidth = ModelScrollArrowWidth;
        le.minHeight = ModelRowHeight;
        le.preferredHeight = ModelRowHeight;
        le.flexibleWidth = 0f;

        var label = CreateTmp(go.GetComponent<RectTransform>(), name + "_Text", ButtonFontSize, TextAlignmentOptions.Center);
        label.text = glyph;
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;

        var button = go.GetComponent<Button>();
        button.onClick.AddListener(action);
        return button;
    }

    void ScrollModelsPrev() => ScrollModelsByPage(-1);

    void ScrollModelsNext() => ScrollModelsByPage(1);

    void ScrollModelsByPage(int direction)
    {
        if (_modelScroll == null || _modelScrollContent == null || direction == 0)
            return;

        float viewportWidth = _modelScroll.viewport != null ? _modelScroll.viewport.rect.width : 0f;
        if (viewportWidth <= 1f)
            return;

        float maxScroll = Mathf.Max(0f, _modelScrollContent.rect.width - viewportWidth);
        if (maxScroll <= 0f)
            return;

        float target = _modelScrollContent.anchoredPosition.x - direction * viewportWidth * 0.85f;
        target = Mathf.Clamp(target, -maxScroll, 0f);
        _modelScrollContent.anchoredPosition = new Vector2(target, _modelScrollContent.anchoredPosition.y);
        RefreshModelScrollArrows();
    }

    void ScrollModelIntoView(ProbeModelKind kind, bool instant)
    {
        if (_modelScroll == null || _modelScrollContent == null)
            return;

        int index = ProbeModelCatalog.GetDisplayIndex(kind);
        if (index < 0 || index >= _modelScrollContent.childCount)
            return;

        var child = _modelScrollContent.GetChild(index) as RectTransform;
        if (child == null)
            return;

        Canvas.ForceUpdateCanvases();
        float viewportWidth = _modelScroll.viewport != null ? _modelScroll.viewport.rect.width : 0f;
        float contentWidth = _modelScrollContent.rect.width;
        float maxScroll = Mathf.Max(0f, contentWidth - viewportWidth);
        if (maxScroll <= 0f)
            return;

        float childLeft = child.anchoredPosition.x;
        float childRight = childLeft + child.rect.width;
        float current = -_modelScrollContent.anchoredPosition.x;
        float visibleLeft = current;
        float visibleRight = current + viewportWidth;

        float targetScroll = current;
        if (childLeft < visibleLeft)
            targetScroll = childLeft;
        else if (childRight > visibleRight)
            targetScroll = childRight - viewportWidth;

        targetScroll = Mathf.Clamp(targetScroll, 0f, maxScroll);
        _modelScrollContent.anchoredPosition = new Vector2(-targetScroll, _modelScrollContent.anchoredPosition.y);
        RefreshModelScrollArrows();
    }

    void RefreshModelScrollArrows()
    {
        if (_modelScroll == null || _modelScrollContent == null)
            return;

        float viewportWidth = _modelScroll.viewport != null ? _modelScroll.viewport.rect.width : 0f;
        float maxScroll = Mathf.Max(0f, _modelScrollContent.rect.width - viewportWidth);
        float pos = -_modelScrollContent.anchoredPosition.x;
        if (_modelPrevBtn != null)
            _modelPrevBtn.interactable = maxScroll > 1f && pos > 1f;
        if (_modelNextBtn != null)
            _modelNextBtn.interactable = maxScroll > 1f && pos < maxScroll - 1f;
    }

    RectTransform CreateCustomRow(RectTransform parent)
    {
        var row = CreateRow(parent, "ProbeCustomRow");
        _antennaToggle = CreateLabeledToggle(row, "ProbePartAntenna", ProbeSettings.CustomAntenna, ProbeSettings.SetCustomAntenna);
        _engineToggle = CreateLabeledToggle(row, "ProbePartEngine", ProbeSettings.CustomEngine, ProbeSettings.SetCustomEngine);
        _shieldToggle = CreateLabeledToggle(row, "ProbePartShield", ProbeSettings.CustomShield, ProbeSettings.SetCustomShield);
        return row;
    }

    void CreateActionRow(RectTransform parent)
    {
        var row = CreateRow(parent, "ProbeActionRow");
        CreateButton(row, "ProbeLaunchLabel", () => ProbeSystemController.Instance?.Launch());
        CreateButton(row, "ProbeAbortLabel", () => ProbeSystemController.Instance?.Abort("ProbeAbortedManual", "Probe aborted."));
        _burnButton = CreateButton(row, "ProbeBurnLabel", () => ProbeSystemController.Instance?.Burn());
        _postcardButton = CreateButton(row, "ProbePostcardLabel", () => ProbeSystemController.Instance?.RequestPostcard());
    }

    void CreateCameraRow(RectTransform parent)
    {
        var row = CreateRow(parent, "ProbeCameraRow");
        CreateButton(row, "ProbeWorldLabel", () => ProbeSettings.SetCameraMode(ProbeCameraMode.World),
            preferredWidth: null, rowHeight: RowHeight, minWidth: CameraButtonMinWidth, flexibleWidth: 1f);
        CreateButton(row, "ProbeChaseLabel", () => ProbeSettings.SetCameraMode(ProbeCameraMode.Chase),
            preferredWidth: null, rowHeight: RowHeight, minWidth: CameraButtonMinWidth, flexibleWidth: 1f);
        CreateButton(row, "ProbeCockpitLabel", () => ProbeSettings.SetCameraMode(ProbeCameraMode.Cockpit),
            preferredWidth: null, rowHeight: RowHeight, minWidth: CameraButtonMinWidth, flexibleWidth: 1f);
        _viewsToggle = CreateLabeledToggle(row, "ProbeViewsLabel", ProbeSettings.ShowProbeViews, ProbeSettings.SetShowProbeViews);
        var viewsLe = _viewsToggle.GetComponent<LayoutElement>();
        if (viewsLe != null)
        {
            viewsLe.minWidth = ViewsTogglePreferredWidth;
            viewsLe.preferredWidth = ViewsTogglePreferredWidth;
            viewsLe.flexibleWidth = 0f;
        }
    }

    void CreateModeButton(RectTransform parent, string key, ProbeModelKind kind)
    {
        var button = CreateButton(parent, key, () =>
        {
            ProbeSettings.SetModel(kind);
            ScrollModelIntoView(kind, instant: true);
        }, GetModelButtonWidth(), ModelRowHeight);
        _modelButtonImages[kind] = button.GetComponent<Image>();

        var label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.enableWordWrapping = true;
            label.enableAutoSizing = true;
            label.fontSizeMin = ModelLabelFontSizeMin;
            label.fontSizeMax = ModelLabelFontSizeMax;
            label.text = ResolveModelLabel(kind);
        }
    }

    float GetModelButtonWidth()
    {
        return Screen.width > Screen.height ? ModelButtonWidthLandscape : ModelButtonWidthPortrait;
    }

    Button CreateButton(
        RectTransform parent,
        string key,
        UnityEngine.Events.UnityAction action,
        float? preferredWidth = null,
        float rowHeight = RowHeight,
        float? minWidth = null,
        float? flexibleWidth = null)
    {
        var go = new GameObject(key, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.32f, 1f);
        var le = go.GetComponent<LayoutElement>();
        le.minHeight = rowHeight;
        le.preferredHeight = rowHeight;
        if (preferredWidth.HasValue)
        {
            le.minWidth = preferredWidth.Value;
            le.preferredWidth = preferredWidth.Value;
            le.flexibleWidth = 0f;
        }
        else
        {
            if (minWidth.HasValue)
                le.minWidth = minWidth.Value;
            if (flexibleWidth.HasValue)
                le.flexibleWidth = flexibleWidth.Value;
        }
        var label = CreateTmp(go.GetComponent<RectTransform>(), key + "_Text", ButtonFontSize, TextAlignmentOptions.Center);
        label.text = key;
        label.raycastTarget = false;
        // Action / camera labels must stay on one line (e.g. Russian «Слежение»).
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;

        var button = go.GetComponent<Button>();
        button.onClick.AddListener(action);
        ApplyLocalizedName(go, key);
        return button;
    }

    Toggle CreateLabeledToggle(RectTransform parent, string key, bool initial, System.Action<bool> setter)
    {
        var go = new GameObject(key + "_Row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().minHeight = RowHeight;
        go.GetComponent<LayoutElement>().preferredHeight = RowHeight;
        go.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.95f);
        var rowLayout = go.GetComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(8, 8, 6, 6);
        rowLayout.spacing = 8f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        var boxGo = new GameObject("Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        boxGo.layer = gameObject.layer;
        boxGo.transform.SetParent(go.transform, false);
        boxGo.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 1f);
        var boxLe = boxGo.GetComponent<LayoutElement>();
        boxLe.minWidth = CheckboxSize;
        boxLe.minHeight = CheckboxSize;
        boxLe.preferredWidth = CheckboxSize;
        boxLe.preferredHeight = CheckboxSize;
        boxLe.flexibleWidth = 0f;

        var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        checkGo.layer = gameObject.layer;
        checkGo.transform.SetParent(boxGo.transform, false);
        Stretch(checkGo.GetComponent<RectTransform>());
        checkGo.GetComponent<RectTransform>().offsetMin = new Vector2(4f, 4f);
        checkGo.GetComponent<RectTransform>().offsetMax = new Vector2(-4f, -4f);
        checkGo.GetComponent<Image>().color = new Color(0.35f, 0.85f, 1f, 1f);

        var label = CreateTmp(go.GetComponent<RectTransform>(), key, ButtonFontSize, TextAlignmentOptions.MidlineLeft);
        label.enableWordWrapping = false;
        var labelLe = label.gameObject.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1f;
        labelLe.minHeight = RowHeight;

        var toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = go.GetComponent<Image>();
        toggle.graphic = checkGo.GetComponent<Image>();
        toggle.isOn = initial;
        toggle.onValueChanged.AddListener(v => setter(v));
        ApplyLocalizedName(label.gameObject, key);
        return toggle;
    }

    void CreateSliderRow(RectTransform parent, string key, string fallback, out Slider slider, float min, float max, float value)
    {
        var row = CreateRow(parent, key + "_Row");
        CreateTmp(row, key, ButtonFontSize, TextAlignmentOptions.MidlineLeft);
        var sliderGo = new GameObject(key + "_Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        sliderGo.layer = gameObject.layer;
        sliderGo.transform.SetParent(row, false);
        sliderGo.GetComponent<LayoutElement>().minHeight = SliderHeight;
        sliderGo.GetComponent<LayoutElement>().preferredHeight = SliderHeight;
        sliderGo.GetComponent<LayoutElement>().flexibleWidth = 1f;
        slider = sliderGo.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(sliderGo.transform, false);
        Stretch(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.25f, 1f);
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Stretch(fill.GetComponent<RectTransform>());
        fill.GetComponent<Image>().color = new Color(0.35f, 0.75f, 1f, 1f);
        slider.targetGraphic = bg.GetComponent<Image>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        ApplyLocalizedName(row.Find(key)?.gameObject, key);
    }

    RectTransform CreateRow(RectTransform parent, string name, float rowHeight = RowHeight)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        var h = go.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 6f;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlHeight = true;
        h.childControlWidth = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = false;
        go.GetComponent<LayoutElement>().minHeight = rowHeight;
        go.GetComponent<LayoutElement>().preferredHeight = rowHeight;
        return go.GetComponent<RectTransform>();
    }

    RectTransform CreatePanel(string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(_root, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    RectTransform CreatePip(string name, string caption, out RawImage image)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(_root, false);
        go.GetComponent<Image>().color = new Color(0.18f, 0.24f, 0.34f, 0.95f);
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.55f, 0.82f, 1f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        var innerGo = new GameObject("Inner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        innerGo.layer = gameObject.layer;
        innerGo.transform.SetParent(go.transform, false);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(PipFrameInset, PipFrameInset);
        innerRt.offsetMax = new Vector2(-PipFrameInset, -PipFrameInset);
        innerGo.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 1f);

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        viewportGo.layer = gameObject.layer;
        viewportGo.transform.SetParent(innerGo.transform, false);
        Stretch(viewportGo.GetComponent<RectTransform>());
        image = viewportGo.GetComponent<RawImage>();
        image.color = Color.white;
        image.raycastTarget = false;

        var captionTmp = CreateTmp(go.GetComponent<RectTransform>(), name + "_Caption", ButtonFontSize, TextAlignmentOptions.TopLeft);
        captionTmp.enableWordWrapping = false;
        captionTmp.text = caption ?? string.Empty;
        captionTmp.fontSize = ButtonFontSize;
        var capRt = captionTmp.GetComponent<RectTransform>();
        capRt.anchorMin = new Vector2(0f, 1f);
        capRt.anchorMax = new Vector2(1f, 1f);
        capRt.pivot = new Vector2(0.5f, 1f);
        capRt.offsetMin = new Vector2(8f, -PipCaptionHeight);
        capRt.offsetMax = new Vector2(-8f, -4f);

        go.SetActive(false);
        return go.GetComponent<RectTransform>();
    }

    static TextMeshProUGUI CreateTmp(RectTransform parent, string name, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>());
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = new Color(0.92f, 0.95f, 1f, 1f);
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        tmp.text = name;
        var font = LocalizationFontHelper.GetFontForLanguage(LocalizationManager.CurrentLanguage);
        if (font != null)
            tmp.font = font;
        if (go.GetComponent<LocalizedText>() == null && !name.EndsWith("_Text") && !name.EndsWith("Body") && !name.EndsWith("_Caption"))
            go.AddComponent<LocalizedText>();
        return tmp;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void ApplyLocalizedName(GameObject go, string key)
    {
        if (go == null)
            return;
        // Button visual text: copy translation onto child if present.
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null)
            return;
        if (LocalizationManager.Instance != null)
        {
            string t = LocalizationManager.Instance.GetTranslation(key);
            if (!string.IsNullOrEmpty(t))
                tmp.text = t;
        }
    }

    void Layout()
    {
        if (_bar == null)
            return;

        bool landscape = Screen.width > Screen.height;
        Canvas canvas = GetComponentInParent<Canvas>();
        SafeAreaInsets.GetCanvasInsets(canvas, out float left, out float right, out float top, out float bottom);

        float scale = canvas != null && canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
        float canvasWidth = Screen.width / scale;
        float canvasHeight = Screen.height / scale;
        float usableWidth = canvasWidth - left - right - 24f;
        float barWidth = landscape
            ? Mathf.Min(BarWidthLandscape, Mathf.Max(280f, usableWidth))
            : Mathf.Max(280f, usableWidth);
        bool custom = ProbeSettings.Model == ProbeModelKind.Custom;
        float preferredBar = custom ? BarHeightCustom : BarHeightStandard;
        float timeBar = TimeControlUiBootstrap.BarHeight + TimeControlUiBootstrap.BarBottomMargin + 10f;
        float navBottom = top + SidePanelUiBootstrap.BarHeight + 8f;
        float barHeight = preferredBar;
        if (landscape)
        {
            // Prefer Portrait-equivalent height; shrink only when vertical space is short.
            float landscapeCap = custom ? BarHeightCustomLandscape : BarHeightLandscape;
            float available = canvasHeight - bottom - timeBar - navBottom - BarTopReserve;
            barHeight = Mathf.Min(landscapeCap, Mathf.Max(preferredBar * 0.85f, available));
            barHeight = Mathf.Min(barHeight, preferredBar);
        }

        ApplyBarContentHeights(landscape);

        _bar.anchorMin = new Vector2(0.5f, 0f);
        _bar.anchorMax = new Vector2(0.5f, 0f);
        _bar.pivot = new Vector2(0.5f, 0f);
        _bar.sizeDelta = new Vector2(barWidth, barHeight);
        _bar.anchoredPosition = new Vector2(0f, bottom + timeBar);

        float pipW = landscape ? PipWidthLandscape : PipWidthPortrait;
        float pipH = landscape ? PipHeightLandscape : PipHeightPortrait;
        float telemW = landscape ? TelemetryWidthLandscape : TelemetryWidth;
        float preferredTelemH = landscape ? TelemetryHeightLandscape : TelemetryHeight;
        bool pipsVisible = ProbeSettings.ShowProbeViews
            && ProbeSystemController.Instance != null
            && ProbeSystemController.Instance.IsFlying;
        float telemTop = navBottom + (pipsVisible ? pipH + 8f : 0f);
        float barTopY = bottom + timeBar + barHeight;
        float spaceAboveBar = canvasHeight - telemTop - barTopY - 12f;
        float telemH = Mathf.Min(preferredTelemH, Mathf.Max(120f, spaceAboveBar));

        _telemetry.anchorMin = new Vector2(1f, 1f);
        _telemetry.anchorMax = new Vector2(1f, 1f);
        _telemetry.pivot = new Vector2(1f, 1f);
        _telemetry.sizeDelta = new Vector2(telemW, telemH);
        if (_telemetryText != null)
        {
            var telemBodyLe = _telemetryText.GetComponent<LayoutElement>();
            if (telemBodyLe != null)
                telemBodyLe.minHeight = Mathf.Max(80f, telemH - TelemetryHeaderHeight - 16f);
        }
        if (_telemetryDrag != null && _telemetryDrag.HasUserOffset)
            _telemetryDrag.EnsureClamped();
        else
            _telemetry.anchoredPosition = new Vector2(-(right + 10f), -telemTop);

        _forwardPip.anchorMin = new Vector2(0.5f, 1f);
        _forwardPip.anchorMax = new Vector2(0.5f, 1f);
        _forwardPip.pivot = new Vector2(0.5f, 1f);
        _forwardPip.sizeDelta = new Vector2(pipW, pipH);
        _forwardPip.anchoredPosition = new Vector2(0f, -navBottom);

        _rearLeftPip.anchorMin = new Vector2(0f, 1f);
        _rearLeftPip.anchorMax = new Vector2(0f, 1f);
        _rearLeftPip.pivot = new Vector2(0f, 1f);
        _rearLeftPip.sizeDelta = new Vector2(pipW, pipH);
        _rearLeftPip.anchoredPosition = new Vector2(left + (landscape ? 96f : 8f), -navBottom);

        _rearRightPip.anchorMin = new Vector2(1f, 1f);
        _rearRightPip.anchorMax = new Vector2(1f, 1f);
        _rearRightPip.pivot = new Vector2(1f, 1f);
        _rearRightPip.sizeDelta = new Vector2(pipW, pipH);
        _rearRightPip.anchoredPosition = new Vector2(-(right + 8f), -navBottom);

        LayoutHelpButton(bottom, timeBar, barWidth, barHeight);
        LayoutModelScroller();
        Canvas.ForceUpdateCanvases();
        RefreshModelScrollArrows();

        _layoutScreenW = Screen.width;
        _layoutScreenH = Screen.height;
        _layoutSafeArea = Screen.safeArea;
        _layoutModel = ProbeSettings.Model;
        _layoutPipsVisible = pipsVisible;
        ClearSafeAreaLayoutDebounce();
    }

    void ApplyBarContentHeights(bool landscape)
    {
        if (_bar == null)
            return;

        float rowH = landscape ? RowHeightLandscape : RowHeight;
        float modelH = landscape ? ModelRowHeightLandscape : ModelRowHeight;
        float sliderH = landscape ? SliderHeightLandscape : SliderHeight;
        var vlg = _bar.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            int pad = landscape ? 8 : 8;
            vlg.padding = new RectOffset(10, 10, pad, pad);
            vlg.spacing = landscape ? 6f : 6f;
        }

        for (int i = 0; i < _bar.childCount; i++)
        {
            var child = _bar.GetChild(i);
            var le = child.GetComponent<LayoutElement>();
            if (le == null)
                continue;

            bool isModelRow = child.name == "ProbeModelRow";
            float h = isModelRow ? modelH : rowH;
            le.minHeight = h;
            le.preferredHeight = h;

            if (isModelRow)
                ApplyModelRowInnerHeights(child, modelH);

            for (int j = 0; j < child.childCount; j++)
            {
                var nested = child.GetChild(j);
                if (nested.name != null && nested.name.EndsWith("_Slider", System.StringComparison.Ordinal))
                {
                    var sliderLe = nested.GetComponent<LayoutElement>();
                    if (sliderLe != null)
                    {
                        sliderLe.minHeight = sliderH;
                        sliderLe.preferredHeight = sliderH;
                    }
                }
                else if (nested.GetComponent<Toggle>() != null)
                {
                    var toggleLe = nested.GetComponent<LayoutElement>();
                    if (toggleLe != null)
                    {
                        toggleLe.minHeight = rowH;
                        toggleLe.preferredHeight = rowH;
                    }
                }
            }
        }
    }

    void ApplyModelRowInnerHeights(Transform modelRow, float modelH)
    {
        for (int i = 0; i < modelRow.childCount; i++)
        {
            var le = modelRow.GetChild(i).GetComponent<LayoutElement>();
            if (le == null)
                continue;
            le.minHeight = modelH;
            le.preferredHeight = modelH;
        }
    }

    void LayoutModelScroller()
    {
        if (_modelScrollContent == null)
            return;

        float buttonWidth = GetModelButtonWidth();
        if (Mathf.Approximately(buttonWidth, _layoutModelButtonWidth))
            return;

        _layoutModelButtonWidth = buttonWidth;
        for (int i = 0; i < _modelScrollContent.childCount; i++)
        {
            var childLe = _modelScrollContent.GetChild(i).GetComponent<LayoutElement>();
            if (childLe == null)
                continue;
            childLe.minWidth = buttonWidth;
            childLe.preferredWidth = buttonWidth;
        }
    }

    void LayoutHelpButton(float safeBottom, float timeBar, float barWidth, float barHeight)
    {
        if (_helpButton == null)
            return;

        var rt = _helpButton.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(barWidth * 0.5f - 8f, safeBottom + timeBar + barHeight + 8f);
    }

    void ApplyHudFontSizes()
    {
        ApplyFontSize(_bar, ButtonFontSize);
        if (_telemetryText != null)
            _telemetryText.fontSize = TelemetryFontSize;
        if (_telemetryHeader != null)
            _telemetryHeader.fontSize = TelemetryHeaderFontSize;
        if (_telemetryStatus != null)
            _telemetryStatus.fontSize = TelemetryStatusFontSize;
    }

    public void RefreshRootVisibility()
    {
        if (_root == null)
            return;

        bool coachPending = !ProbeCoachSettings.LaunchCompleted;
        bool showProbeUi = ProbeSettings.UseProbe;
        gameObject.SetActive(showProbeUi);

        if (_bar != null)
            _bar.gameObject.SetActive(showProbeUi);
        if (_telemetry != null)
            _telemetry.gameObject.SetActive(showProbeUi);
        if (_helpButton != null)
            _helpButton.gameObject.SetActive(showProbeUi && coachPending);

        if (showProbeUi)
            _coachOverlay?.RefreshVisibility();
    }

    static void ApplyFontSize(RectTransform root, float size)
    {
        if (root == null)
            return;

        var labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].enableAutoSizing)
            {
                labels[i].fontSizeMax = size;
                continue;
            }

            labels[i].fontSize = size;
        }
    }

    void RefreshState()
    {
        bool flying = ProbeSystemController.Instance != null && ProbeSystemController.Instance.IsFlying;
        if (_customRow != null)
            _customRow.SetActive(ProbeSettings.Model == ProbeModelKind.Custom);
        if (_burnButton != null)
            _burnButton.interactable = flying && ProbeSettings.ResolveHasEngine();
        if (_postcardButton != null)
            _postcardButton.interactable = flying;
        if (_viewsToggle != null && _viewsToggle.isOn != ProbeSettings.ShowProbeViews)
            _viewsToggle.SetIsOnWithoutNotify(ProbeSettings.ShowProbeViews);
        RefreshLabels();
        RefreshModelButtonHighlights();
        ScrollModelIntoView(ProbeSettings.Model, instant: true);
        RefreshPips();
        RefreshTelemetryHeader();
        RefreshTelemetryNow();
        RefreshRootVisibility();
        if (NeedsLayout())
            Layout();
    }

    void RefreshModelButtonHighlights()
    {
        var selected = new Color(0.22f, 0.34f, 0.48f, 1f);
        var normal = new Color(0.18f, 0.22f, 0.32f, 1f);
        var outlineColor = new Color(0.35f, 0.85f, 1f, 1f);

        foreach (var pair in _modelButtonImages)
        {
            if (pair.Value == null)
                continue;

            bool active = pair.Key == ProbeSettings.Model;
            pair.Value.color = active ? selected : normal;
            var outline = pair.Value.GetComponent<Outline>();
            if (active)
            {
                if (outline == null)
                    outline = pair.Value.gameObject.AddComponent<Outline>();
                outline.effectColor = outlineColor;
                outline.effectDistance = new Vector2(2f, -2f);
            }
            else if (outline != null)
            {
                Destroy(outline);
            }
        }
    }

    void RefreshTelemetryHeader()
    {
        if (_telemetryHeader == null || _telemetryStatus == null)
            return;

        var system = ProbeSystemController.Instance;
        ProbeModelKind model = ProbeSettings.Model;
        if (system != null && system.IsFlying && system.Craft != null)
            model = system.Craft.Model;

        string modelLabel = ResolveModelLabel(model);
        string titleTemplate = T("ProbeTelemetryTitle", "Probe · {0}");
        SetTmpTextIfChanged(_telemetryHeader, ref _lastTelemetryHeader, string.Format(titleTemplate, modelLabel));

        bool flying = system != null && system.IsFlying;
        SetTmpTextIfChanged(
            _telemetryStatus,
            ref _lastTelemetryStatus,
            flying
                ? T("ProbeTelemetryFlying", "(in flight)")
                : T("ProbeTelemetryAiming", "(aiming)"));
    }

    public static string ResolveModelLabel(ProbeModelKind kind)
    {
        string name = T(ProbeModelCatalog.GetLocalizationKey(kind), kind.ToString());
        string originKey = ProbeModelCatalog.GetOriginKey(kind);
        if (string.IsNullOrEmpty(originKey))
            return name;
        string origin = T(originKey, string.Empty);
        return string.IsNullOrEmpty(origin) ? name : $"{name} ({origin})";
    }

    void RefreshLabels()
    {
        if (_bar == null)
            return;

        var labels = _bar.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            string key = labels[i].gameObject.name;
            if (key.EndsWith("_Text", System.StringComparison.Ordinal))
                key = key.Substring(0, key.Length - 5);
            if (key.EndsWith("Body", System.StringComparison.Ordinal))
                continue;
            if (key.StartsWith("ProbeModel", System.StringComparison.Ordinal)
                && ProbeModelCatalog.TryGetKindFromLocalizationKey(key, out ProbeModelKind kind))
            {
                labels[i].text = ResolveModelLabel(kind);
                continue;
            }
            ApplyLocalizedName(labels[i].gameObject, key);
        }
    }

    void RefreshPips()
    {
        bool show = ProbeSettings.ShowProbeViews
            && ProbeSystemController.Instance != null
            && ProbeSystemController.Instance.IsFlying;
        var rig = ProbeSystemController.Instance != null
            ? ProbeSystemController.Instance.GetComponent<ProbeViewRig>()
            : null;

        if (_forwardPip != null)
            _forwardPip.gameObject.SetActive(show);
        if (_rearLeftPip != null)
            _rearLeftPip.gameObject.SetActive(show);
        if (_rearRightPip != null)
            _rearRightPip.gameObject.SetActive(show);
        if (!show || rig == null)
            return;

        if (rig.ForwardTexture != null && _forwardImage != null)
            _forwardImage.texture = rig.ForwardTexture;
        if (rig.RearLeftTexture != null && _rearLeftImage != null)
            _rearLeftImage.texture = rig.RearLeftTexture;
        if (rig.RearRightTexture != null && _rearRightImage != null)
            _rearRightImage.texture = rig.RearRightTexture;
    }

    void RefreshTelemetryNow()
    {
        if (_telemetryText == null)
            return;

        var system = ProbeSystemController.Instance;
        bool show = system != null && ProbeSettings.UseProbe;
        _telemetry.gameObject.SetActive(show);
        if (!show)
            return;

        RefreshTelemetryHeader();

        Vector3 pos;
        Vector3 vel;
        if (system.IsFlying && system.Craft != null)
        {
            pos = system.Craft.transform.position;
            vel = system.Craft.Velocity;
        }
        else
        {
            pos = system.Sun != null ? system.Sun.position + Vector3.right * 22f : Vector3.zero;
            vel = system.AimVelocity;
        }

        Transform sun = system.Sun;
        Vector3 sunPos = sun != null ? sun.position : Vector3.zero;
        Vector3 rel = pos - sunPos;
        float auToUnity = Mathf.Max(0.01f, system.AuToUnity);
        float rAu = new Vector2(rel.x, rel.z).magnitude / auToUnity;
        float zAu = rel.y / auToUnity;
        float lon = Mathf.Atan2(rel.x, rel.z) * Mathf.Rad2Deg;
        if (lon < 0f)
            lon += 360f;

        Vector3 sunVel = Vector3.zero;
        if (system.TryGetAttractor("Sun", out ProbeGravityIntegrator.Attractor sunA))
            sunVel = sunA.velocity;
        float vSun = (vel - sunVel).magnitude / auToUnity;

        FindTopThree(system, pos, out string n1, out float d1, out string n2, out float d2, out string n3, out float d3);

        float vRel = 0f;
        // Attractors already carry velocity; no GameObject.Find on this ~10 Hz path.
        if (system.TryGetAttractor(n1, out ProbeGravityIntegrator.Attractor nearA))
            vRel = (vel - nearA.velocity).magnitude / auToUnity;

        _telemetrySb.Clear();
        _telemetrySb.Append(T("ProbeTelemetrySunSpeed", "v☉")).Append("  ").Append(FormatSpeed(vSun, rAu)).Append('\n');
        _telemetrySb.Append(T("ProbeTelemetryRelSpeed", "v rel")).Append("  ").Append(FormatSpeed(vRel, d1 / auToUnity)).Append('\n');
        _telemetrySb.Append(T("ProbeTelemetryRadius", "r")).Append("  ").Append(FormatDistance(rAu)).Append('\n');
        _telemetrySb.Append(T("ProbeTelemetryLon", "λ")).Append("  ").Append(lon.ToString("0.0")).Append("°\n");
        _telemetrySb.Append(T("ProbeTelemetryZ", "z")).Append("  ").Append(FormatDistance(Mathf.Abs(zAu))).Append('\n');
        _telemetrySb.Append(T("ProbeNearestLabel", "Near")).Append('\n');
        _telemetrySb.Append("1. ").Append(BodyLabel(n1)).Append("  ").Append(FormatDistance(d1 / auToUnity)).Append('\n');
        _telemetrySb.Append("2. ").Append(BodyLabel(n2)).Append("  ").Append(FormatDistance(d2 / auToUnity)).Append('\n');
        _telemetrySb.Append("3. ").Append(BodyLabel(n3)).Append("  ").Append(FormatDistance(d3 / auToUnity));

        if (SbEquals(_telemetrySb, _lastTelemetryBody))
            return;

        _lastTelemetryBody = _telemetrySb.ToString();
        _telemetryText.text = _lastTelemetryBody;
    }

    void FindTopThree(
        ProbeSystemController system,
        Vector3 pos,
        out string n1, out float d1,
        out string n2, out float d2,
        out string n3, out float d3)
    {
        n1 = n2 = n3 = "—";
        d1 = d2 = d3 = 0f;
        int count = system.CopyAttractors(_findScratch);
        for (int i = 0; i < count; i++)
        {
            _findNames[i] = _findScratch[i].name;
            _findDist[i] = Mathf.Max(0f, Vector3.Distance(pos, _findScratch[i].position) - _findScratch[i].radius);
        }

        for (int pass = 0; pass < 3; pass++)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (_findDist[i] < bestD)
                {
                    bestD = _findDist[i];
                    best = i;
                }
            }

            if (best < 0)
                break;

            if (pass == 0) { n1 = _findNames[best]; d1 = bestD; }
            if (pass == 1) { n2 = _findNames[best]; d2 = bestD; }
            if (pass == 2) { n3 = _findNames[best]; d3 = bestD; }
            _findDist[best] = float.MaxValue;
        }
    }

    static void SetTmpTextIfChanged(TextMeshProUGUI tmp, ref string last, string value)
    {
        if (tmp == null || last == value)
            return;
        last = value;
        tmp.text = value;
    }

    static bool SbEquals(StringBuilder sb, string s)
    {
        if (s == null)
            return sb.Length == 0;
        if (sb.Length != s.Length)
            return false;
        for (int i = 0; i < sb.Length; i++)
        {
            if (sb[i] != s[i])
                return false;
        }

        return true;
    }

    static string FormatDistance(float au)
    {
        if (au < 0.001f)
        {
            float km = au * SolarSystemCatalog.AuKm;
            return km.ToString("0") + " km";
        }

        if (au < 0.1f)
            return au.ToString("0.000") + " AU";
        return au.ToString("0.00") + " AU";
    }

    static string FormatSpeed(float auPerSec, float nearbyAu)
    {
        if (nearbyAu < 0.002f)
        {
            float kmPerSec = auPerSec * SolarSystemCatalog.AuKm;
            return kmPerSec.ToString("0.0") + " km/s";
        }

        return auPerSec.ToString("0.000") + " AU/s";
    }

    public static string ResolveBodyLabel(string objectName) => BodyLabel(objectName);

    static string BodyLabel(string objectName)
    {
        if (string.IsNullOrEmpty(objectName) || objectName == "—")
            return "—";

        string key = objectName + "Header";
        if (objectName.StartsWith("Comet_"))
        {
            if (CometCatalog.TryGetByObjectName(objectName, out CometCatalog.CometDefinition def))
                key = def.labelKey;
        }

        if (LocalizationManager.Instance != null)
        {
            string t = LocalizationManager.Instance.GetTranslation(key);
            if (!string.IsNullOrEmpty(t))
                return t;
        }

        return objectName;
    }

    static string T(string key, string fallback)
    {
        if (LocalizationManager.Instance == null)
            return fallback;
        string t = LocalizationManager.Instance.GetTranslation(key);
        return string.IsNullOrEmpty(t) ? fallback : t;
    }
}
