using System;
using System.Collections;
using SolarSystemApp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Map-style distance scale bar (AU / km) for Level1. Portrait: bottom-right above CPU.
/// Tap opens a localized AU hint with kilometre equivalent.
/// Runs after MobileOrbitCamera so pinch/scroll distance is current when measuring.
/// </summary>
[DefaultExecutionOrder(50)]
public class ScaleBarController : MonoBehaviour
{
    public const string ObjectName = "ScaleBar";

    const string KeyAuFormat = "ScaleBarAuFormat";
    const string KeyKmFormat = "ScaleBarKmFormat";
    const string KeyHintTitle = "ScaleBarAuHintTitle";
    const string KeyHintBody = "ScaleBarAuHintBody";

    const string FallbackAuFormat = "{0} AU";
    const string FallbackKmFormat = "{0} km";
    const string FallbackHintTitle = "Astronomical unit (AU)";
    const string FallbackHintBody =
        "1 AU is the average Earth–Sun distance, about 149 597 871 km.";

    const float GapAboveCpu = 8f;
    const float PanelHeight = 36f;
    const float TargetBarSegmentWidth = 100f;
    const float MinBarSegmentWidth = 48f;
    const float MaxBarSegmentWidth = 140f;
    const float PanelPaddingH = 10f;
    const float LabelGap = 6f;
    const float LabelMinWidth = 72f;
    const float UpdateInterval = 0.15f;
    const float HintAutoHideSeconds = 5f;
    const float HintWidth = 260f;
    const float HintGap = 8f;
    const float MinAuThreshold = 0.001f;

    static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.65f);
    static readonly Color LineColor = new Color(0.92f, 0.95f, 1f, 0.92f);
    static readonly Color LabelColor = new Color(0.92f, 0.95f, 1f, 0.95f);
    static readonly Color HintColor = new Color(0.02f, 0.05f, 0.12f, 0.94f);

    RectTransform _root;
    RectTransform _barSegment;
    Image _barImage;
    TextMeshProUGUI _label;
    Button _button;
    RectTransform _hintRoot;
    TextMeshProUGUI _hintTitle;
    TextMeshProUGUI _hintBody;
    Canvas _canvas;
    SolarSystemScaleController _scaleController;
    MobileOrbitCamera _orbitCamera;

    float _nextUpdateTime;
    Rect _lastSafeArea;
    int _lastScreenWidth;
    int _lastScreenHeight;
    bool _cpuEnabled = true;
    bool _hintVisible;
    Coroutine _hintHideRoutine;
    float _currentSegmentWidth = TargetBarSegmentWidth;

    public static ScaleBarController EnsureOnCanvas(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return null;

        Transform existing = canvasTransform.Find(ObjectName);
        if (existing != null)
        {
            var controller = existing.GetComponent<ScaleBarController>();
            if (controller == null)
                controller = existing.gameObject.AddComponent<ScaleBarController>();
            return controller;
        }

        var go = new GameObject(ObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ScaleBarController));
        go.layer = canvasTransform.gameObject.layer;
        go.transform.SetParent(canvasTransform, false);
        return go.GetComponent<ScaleBarController>();
    }

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        BuildUi();
        transform.SetAsLastSibling();
        _cpuEnabled = CpuMonitorSettings.UseCpuMonitor;
        RefreshSafeAreaLayout();
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        CpuMonitorSettings.UseCpuMonitorChanged += OnCpuMonitorChanged;
        ScaleSettings.ModeChanged += OnScaleModeChanged;
        RefreshFonts();
        RefreshScale();
        RefreshSafeAreaLayout();
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        CpuMonitorSettings.UseCpuMonitorChanged -= OnCpuMonitorChanged;
        ScaleSettings.ModeChanged -= OnScaleModeChanged;
        HideHintImmediate();
    }

    void Update()
    {
        Rect safeArea = Screen.safeArea;
        if (safeArea != _lastSafeArea
            || Screen.width != _lastScreenWidth
            || Screen.height != _lastScreenHeight)
        {
            RefreshSafeAreaLayout();
        }
    }

    void LateUpdate()
    {
        // After MobileOrbitCamera updates distance in LateUpdate so pinch/scroll is current.
        EnsureOrbitCamera();
        bool userZooming = _orbitCamera != null && _orbitCamera.IsUserControlling;
        if (!userZooming && Time.unscaledTime < _nextUpdateTime)
            return;

        _nextUpdateTime = Time.unscaledTime + UpdateInterval;
        RefreshScale();
    }

    void BuildUi()
    {
        _root = GetComponent<RectTransform>();
        if (_root == null)
            _root = gameObject.AddComponent<RectTransform>();

        _root.anchorMin = new Vector2(1f, 0f);
        _root.anchorMax = new Vector2(1f, 0f);
        _root.pivot = new Vector2(1f, 0f);
        _root.sizeDelta = new Vector2(TargetBarSegmentWidth + LabelGap + LabelMinWidth + PanelPaddingH * 2f, PanelHeight);

        var panel = GetComponent<Image>();
        if (panel == null)
            panel = gameObject.AddComponent<Image>();
        panel.color = PanelColor;
        panel.raycastTarget = true;

        _button = GetComponent<Button>();
        if (_button == null)
            _button = gameObject.AddComponent<Button>();
        _button.targetGraphic = panel;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(ToggleHint);
        var colors = _button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.75f, 0.85f, 0.95f, 1f);
        colors.selectedColor = colors.highlightedColor;
        _button.colors = colors;

        _barSegment = CreateChildRect("BarSegment", transform);
        _barSegment.anchorMin = new Vector2(0f, 0.5f);
        _barSegment.anchorMax = new Vector2(0f, 0.5f);
        _barSegment.pivot = new Vector2(0f, 0.5f);
        _barSegment.sizeDelta = new Vector2(TargetBarSegmentWidth, 3f);
        _barSegment.anchoredPosition = new Vector2(PanelPaddingH, 0f);

        _barImage = _barSegment.gameObject.GetComponent<Image>();
        if (_barImage == null)
            _barImage = _barSegment.gameObject.AddComponent<Image>();
        _barImage.color = LineColor;
        _barImage.raycastTarget = false;

        // End ticks
        CreateTick(_barSegment, "TickLeft", 0f);
        CreateTick(_barSegment, "TickRight", 1f);

        var labelRect = CreateChildRect("Label", transform);
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(PanelPaddingH + TargetBarSegmentWidth + LabelGap, 4f);
        labelRect.offsetMax = new Vector2(-PanelPaddingH, -4f);

        _label = labelRect.gameObject.GetComponent<TextMeshProUGUI>();
        if (_label == null)
            _label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        _label.fontSize = 14f;
        _label.color = LabelColor;
        _label.alignment = TextAlignmentOptions.MidlineLeft;
        _label.textWrappingMode = TextWrappingModes.NoWrap;
        _label.raycastTarget = false;
        _label.text = string.Format(FallbackAuFormat, 1);

        BuildHintUi();
    }

    void BuildHintUi()
    {
        var hintGo = new GameObject("AuHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        hintGo.layer = gameObject.layer;
        hintGo.transform.SetParent(transform, false);

        _hintRoot = hintGo.GetComponent<RectTransform>();
        _hintRoot.anchorMin = new Vector2(1f, 1f);
        _hintRoot.anchorMax = new Vector2(1f, 1f);
        _hintRoot.pivot = new Vector2(1f, 0f);
        _hintRoot.sizeDelta = new Vector2(HintWidth, 96f);
        _hintRoot.anchoredPosition = new Vector2(0f, HintGap);

        var hintImage = hintGo.GetComponent<Image>();
        hintImage.color = HintColor;
        hintImage.raycastTarget = true;

        var titleRect = CreateChildRect("Title", hintGo.transform);
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-20f, 28f);
        titleRect.anchoredPosition = new Vector2(0f, -8f);

        _hintTitle = titleRect.gameObject.AddComponent<TextMeshProUGUI>();
        _hintTitle.fontSize = 15f;
        _hintTitle.fontStyle = FontStyles.Bold;
        _hintTitle.color = LabelColor;
        _hintTitle.alignment = TextAlignmentOptions.TopLeft;
        _hintTitle.textWrappingMode = TextWrappingModes.NoWrap;
        _hintTitle.raycastTarget = false;

        var bodyRect = CreateChildRect("Body", hintGo.transform);
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(10f, 10f);
        bodyRect.offsetMax = new Vector2(-10f, -36f);

        _hintBody = bodyRect.gameObject.AddComponent<TextMeshProUGUI>();
        _hintBody.fontSize = 13f;
        _hintBody.color = new Color(0.85f, 0.9f, 1f, 0.95f);
        _hintBody.alignment = TextAlignmentOptions.TopLeft;
        _hintBody.textWrappingMode = TextWrappingModes.Normal;
        _hintBody.raycastTarget = false;

        hintGo.SetActive(false);
        _hintVisible = false;
    }

    static void CreateTick(RectTransform bar, string name, float anchorX)
    {
        var tick = CreateChildRect(name, bar);
        tick.anchorMin = new Vector2(anchorX, 0.5f);
        tick.anchorMax = new Vector2(anchorX, 0.5f);
        tick.pivot = new Vector2(0.5f, 0.5f);
        tick.sizeDelta = new Vector2(2f, 10f);
        tick.anchoredPosition = Vector2.zero;

        var image = tick.gameObject.AddComponent<Image>();
        image.color = LineColor;
        image.raycastTarget = false;
    }

    static RectTransform CreateChildRect(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.GetComponent<RectTransform>();

        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    void RefreshSafeAreaLayout()
    {
        if (_root == null)
            _root = GetComponent<RectTransform>();

        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        SafeAreaInsets.GetCanvasInsets(_canvas, out _, out float safeRight, out _, out _);

        float x = -(safeRight + CpuLoadMonitor.HorizontalMargin);
        float y = ComputeBottomOffset();

        _root.anchorMin = new Vector2(1f, 0f);
        _root.anchorMax = new Vector2(1f, 0f);
        _root.pivot = new Vector2(1f, 0f);
        _root.anchoredPosition = new Vector2(x, y);
        ApplyPanelWidth(_currentSegmentWidth);

        if (_hintRoot != null)
        {
            _hintRoot.anchoredPosition = new Vector2(0f, HintGap);
            ClampHintInsideScreen();
        }

        _lastSafeArea = Screen.safeArea;
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
    }

    float ComputeBottomOffset()
    {
        // Match time bar: fixed bottom stack so Y stays stable on rotate.
        float y = TimeControlUiBootstrap.BarBottomMargin
            + TimeControlUiBootstrap.BarHeight
            + CpuLoadMonitor.GapAboveTimeBar;

        if (_cpuEnabled)
            y += CpuLoadMonitor.PanelHeight + GapAboveCpu;

        return y;
    }

    void ApplyPanelWidth(float segmentWidth)
    {
        _currentSegmentWidth = Mathf.Clamp(segmentWidth, MinBarSegmentWidth, MaxBarSegmentWidth);

        if (_barSegment != null)
        {
            _barSegment.sizeDelta = new Vector2(_currentSegmentWidth, 3f);
            _barSegment.anchoredPosition = new Vector2(PanelPaddingH, 0f);
        }

        float panelWidth = PanelPaddingH * 2f + _currentSegmentWidth + LabelGap + LabelMinWidth;
        if (_root != null)
            _root.sizeDelta = new Vector2(panelWidth, PanelHeight);

        if (_label != null)
        {
            var labelRect = _label.rectTransform;
            labelRect.offsetMin = new Vector2(PanelPaddingH + _currentSegmentWidth + LabelGap, 4f);
            labelRect.offsetMax = new Vector2(-PanelPaddingH, -4f);
        }
    }

    void RefreshScale()
    {
        if (_label == null)
            return;

        float auToUnity = ResolveAuToUnity();

        if (!TryMeasureWorldLength(TargetBarSegmentWidth, out float worldLength) || worldLength <= 0f)
        {
            _label.text = string.Format(Translate(KeyAuFormat, FallbackAuFormat), "—");
            return;
        }

        float auForTarget = worldLength / auToUnity;

        if (auForTarget >= MinAuThreshold)
        {
            float niceAu = ChooseNiceNumber(auForTarget);
            float segmentWidth = TargetBarSegmentWidth * (niceAu / Mathf.Max(auForTarget, 1e-8f));
            ApplyPanelWidth(segmentWidth);
            _label.text = FormatValue(Translate(KeyAuFormat, FallbackAuFormat), niceAu);
        }
        else
        {
            float kmForTarget = auForTarget * SolarSystemCatalog.AuKm;
            float niceKm = ChooseNiceNumber(Mathf.Max(kmForTarget, 1f));
            float segmentWidth = TargetBarSegmentWidth * (niceKm / Mathf.Max(kmForTarget, 1e-8f));
            ApplyPanelWidth(segmentWidth);
            _label.text = FormatValue(Translate(KeyKmFormat, FallbackKmFormat), niceKm);
        }
    }

    /// <summary>
    /// 1 AU = current Earth–Sun horizontal distance when available.
    /// Prefers ScaleController baseline, then live scene measure, then default.
    /// </summary>
    float ResolveAuToUnity()
    {
        if (_scaleController == null)
            _scaleController = FindFirstObjectByType<SolarSystemScaleController>();

        if (_scaleController != null && _scaleController.AuToUnity > 0.001f)
            return _scaleController.AuToUnity;

        float live = MeasureLiveEarthSunAuToUnity();
        if (live > 0.001f)
            return live;

        return SolarSystemLayout.DefaultAuToUnity;
    }

    static float MeasureLiveEarthSunAuToUnity()
    {
        GameObject earthGo = GameObject.Find("Earth");
        GameObject sunGo = GameObject.Find("Sun");
        if (earthGo == null || sunGo == null)
            return 0f;

        return Vector3.ProjectOnPlane(
            earthGo.transform.position - sunGo.transform.position,
            Vector3.up).magnitude;
    }

    void EnsureOrbitCamera()
    {
        if (_orbitCamera != null)
            return;

        Camera cam = Camera.main;
        if (cam != null)
            _orbitCamera = cam.GetComponent<MobileOrbitCamera>();
    }

    bool TryMeasureWorldLength(float canvasBarWidth, out float worldLength)
    {
        worldLength = 0f;
        Camera cam = Camera.main;
        if (cam == null || !cam.enabled)
            return false;

        EnsureOrbitCamera();

        float distance;
        if (_orbitCamera != null && _orbitCamera.distance > 0.01f)
            distance = _orbitCamera.distance;
        else
            distance = Vector3.Distance(cam.transform.position, GetFallbackFocusPoint());

        distance = Mathf.Max(0.05f, distance);

        float scaleFactor = _canvas != null ? Mathf.Max(0.001f, _canvas.scaleFactor) : 1f;
        float screenPixels = canvasBarWidth * scaleFactor;
        if (Screen.width <= 0)
            return false;

        float viewportWidth = screenPixels / Screen.width;
        Vector3 left = cam.ViewportToWorldPoint(new Vector3(0.5f - viewportWidth * 0.5f, 0.5f, distance));
        Vector3 right = cam.ViewportToWorldPoint(new Vector3(0.5f + viewportWidth * 0.5f, 0.5f, distance));
        worldLength = Vector3.Distance(left, right);
        return worldLength > 1e-8f;
    }

    static Vector3 GetFallbackFocusPoint()
    {
        var lookAt = FindFirstObjectByType<LookAtTarget>();
        if (lookAt != null)
        {
            if (lookAt.currentTarget != null)
                return lookAt.currentTarget.transform.position;
            if (lookAt.defaultTarget != null)
                return lookAt.defaultTarget.transform.position;
        }

        GameObject sun = GameObject.Find("Sun");
        return sun != null ? sun.transform.position : Vector3.zero;
    }

    static float ChooseNiceNumber(float value)
    {
        if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            return 1f;

        float exp = Mathf.Floor(Mathf.Log10(value));
        float magnitude = Mathf.Pow(10f, exp);
        float normalized = value / magnitude;

        float nice;
        if (normalized >= 5f)
            nice = 5f;
        else if (normalized >= 2f)
            nice = 2f;
        else
            nice = 1f;

        return nice * magnitude;
    }

    static string FormatValue(string format, float value)
    {
        string text;
        if (value >= 100f)
            text = Mathf.RoundToInt(value).ToString();
        else if (value >= 10f)
            text = value.ToString("0.#");
        else if (value >= 1f)
            text = value.ToString("0.##");
        else
            text = value.ToString("0.###");

        try
        {
            return string.Format(format, text);
        }
        catch (FormatException)
        {
            return text;
        }
    }

    void ToggleHint()
    {
        if (_hintVisible)
            HideHintImmediate();
        else
            ShowHint();
    }

    void ShowHint()
    {
        if (_hintRoot == null)
            return;

        ApplyHintText();
        _hintRoot.gameObject.SetActive(true);
        _hintVisible = true;
        ClampHintInsideScreen();

        if (_hintHideRoutine != null)
            StopCoroutine(_hintHideRoutine);
        _hintHideRoutine = StartCoroutine(HideHintAfterDelay());
    }

    void HideHintImmediate()
    {
        if (_hintHideRoutine != null)
        {
            StopCoroutine(_hintHideRoutine);
            _hintHideRoutine = null;
        }

        if (_hintRoot != null)
            _hintRoot.gameObject.SetActive(false);

        _hintVisible = false;
    }

    IEnumerator HideHintAfterDelay()
    {
        yield return new WaitForSecondsRealtime(HintAutoHideSeconds);
        _hintHideRoutine = null;
        if (_hintRoot != null)
            _hintRoot.gameObject.SetActive(false);
        _hintVisible = false;
    }

    void ApplyHintText()
    {
        if (_hintTitle != null)
            _hintTitle.text = Translate(KeyHintTitle, FallbackHintTitle);

        if (_hintBody != null)
            _hintBody.text = Translate(KeyHintBody, FallbackHintBody);

        RefreshFonts();
    }

    void ClampHintInsideScreen()
    {
        if (_hintRoot == null || _root == null || _canvas == null)
            return;

        // Keep hint above the bar; if near top of safe area, still prefer upward.
        float preferredHeight = 96f;
        if (_hintBody != null)
            preferredHeight = Mathf.Clamp(36f + _hintBody.preferredHeight + 20f, 72f, 160f);

        _hintRoot.sizeDelta = new Vector2(HintWidth, preferredHeight);
        _hintRoot.anchoredPosition = new Vector2(0f, HintGap);
    }

    void OnLanguageChanged()
    {
        RefreshFonts();
        RefreshScale();
        if (_hintVisible)
            ApplyHintText();
    }

    void OnCpuMonitorChanged(bool enabled)
    {
        _cpuEnabled = enabled;
        RefreshSafeAreaLayout();
    }

    void OnScaleModeChanged(SolarSystemApp.ScaleMode mode)
    {
        RefreshScale();
    }

    void RefreshFonts()
    {
        var lang = LocalizationManager.CurrentLanguage;
        TMP_FontAsset font = LocalizationFontHelper.GetFontForLanguage(lang);
        Material overlay = LocalizationFontHelper.GetOverlayMaterialForLanguage(lang);

        ApplyFont(_label, font, overlay);
        ApplyFont(_hintTitle, font, overlay);
        ApplyFont(_hintBody, font, overlay);
    }

    static void ApplyFont(TMP_Text text, TMP_FontAsset font, Material overlay)
    {
        if (text == null)
            return;

        if (font != null)
            text.font = font;
        if (overlay != null)
            text.fontSharedMaterial = overlay;
    }

    static string Translate(string key, string fallback)
    {
        if (LocalizationManager.Instance == null)
            return fallback;

        string translation = LocalizationManager.Instance.GetTranslation(key);
        return string.IsNullOrEmpty(translation) ? fallback : translation;
    }
}
