using SolarSystemApp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Date label for Time Machine, anchored bottom-right above the scale bar.
/// </summary>
public class TimeMachineDateHud : MonoBehaviour
{
    public const string ObjectName = "TimeMachineDateHud";

    const float GapAboveScaleBar = 8f;
    const float ScaleBarPanelHeight = 36f;
    const float PanelHeight = 28f;
    const float PanelPaddingH = 10f;
    const float MinWidth = 120f;

    static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.65f);
    static readonly Color LabelColor = new Color(0.92f, 0.95f, 1f, 0.95f);

    RectTransform _root;
    TextMeshProUGUI _label;
    Canvas _canvas;
    Rect _lastSafeArea;
    int _lastScreenWidth;
    int _lastScreenHeight;
    bool _cpuEnabled = true;

    public static TimeMachineDateHud EnsureOnCanvas(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return null;

        Transform existing = canvasTransform.Find(ObjectName);
        if (existing != null)
        {
            var controller = existing.GetComponent<TimeMachineDateHud>();
            if (controller == null)
                controller = existing.gameObject.AddComponent<TimeMachineDateHud>();
            return controller;
        }

        var go = new GameObject(ObjectName, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = canvasTransform.gameObject.layer;
        go.transform.SetParent(canvasTransform, false);
        go.SetActive(false);
        var hud = go.AddComponent<TimeMachineDateHud>();
        hud.BuildUi();
        return hud;
    }

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_root == null)
            BuildUi();
        CpuMonitorSettings.UseCpuMonitorChanged += OnCpuEnabledChanged;
        _cpuEnabled = CpuMonitorSettings.UseCpuMonitor;
        // Visible only while Time Machine drives the session.
        if (!SimulationClock.DrivesMotion)
            gameObject.SetActive(false);
        else
            RefreshSafeAreaLayout();
    }

    void OnDestroy()
    {
        CpuMonitorSettings.UseCpuMonitorChanged -= OnCpuEnabledChanged;
    }

    void OnCpuEnabledChanged(bool enabled)
    {
        _cpuEnabled = enabled;
        RefreshSafeAreaLayout();
    }

    void Update()
    {
        Rect safeArea = Screen.safeArea;
        if (safeArea == _lastSafeArea
            && Screen.width == _lastScreenWidth
            && Screen.height == _lastScreenHeight)
            return;

        RefreshSafeAreaLayout();
    }

    public void SetDateText(string text)
    {
        string next = text ?? string.Empty;
        if (_label != null)
        {
            if (_label.text == next)
                return;

            _label.text = next;
        }

        RefreshSafeAreaLayout();
    }

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);

        if (visible)
            RefreshSafeAreaLayout();
    }

    void BuildUi()
    {
        _root = GetComponent<RectTransform>();
        _root.anchorMin = new Vector2(1f, 0f);
        _root.anchorMax = new Vector2(1f, 0f);
        _root.pivot = new Vector2(1f, 0f);
        _root.sizeDelta = new Vector2(MinWidth, PanelHeight);

        var image = gameObject.GetComponent<Image>();
        if (image == null)
            image = gameObject.AddComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = false;

        Transform labelTransform = transform.Find("DateLabel");
        RectTransform labelRect;
        if (labelTransform == null)
        {
            var labelGo = new GameObject("DateLabel", typeof(RectTransform), typeof(CanvasRenderer));
            labelGo.layer = gameObject.layer;
            labelGo.transform.SetParent(transform, false);
            labelRect = labelGo.GetComponent<RectTransform>();
            _label = labelGo.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            labelRect = labelTransform.GetComponent<RectTransform>();
            _label = labelTransform.GetComponent<TextMeshProUGUI>();
            if (_label == null)
                _label = labelTransform.gameObject.AddComponent<TextMeshProUGUI>();
        }

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(PanelPaddingH, 2f);
        labelRect.offsetMax = new Vector2(-PanelPaddingH, -2f);

        _label.text = SimulationClock.FormatDateYyyyMmDd();
        _label.fontSize = 14f;
        _label.alignment = TextAlignmentOptions.Center;
        _label.color = LabelColor;
        _label.raycastTarget = false;
        _label.textWrappingMode = TextWrappingModes.NoWrap;

        Language lang = LocalizationManager.CurrentLanguage;
        TMP_FontAsset font = LocalizationFontHelper.GetFontForLanguage(lang);
        Material overlay = LocalizationFontHelper.GetOverlayMaterialForLanguage(lang);
        if (font != null)
            _label.font = font;
        if (overlay != null)
            _label.fontSharedMaterial = overlay;
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

        float preferred = _label != null ? _label.preferredWidth + PanelPaddingH * 2f : MinWidth;
        _root.sizeDelta = new Vector2(Mathf.Max(MinWidth, preferred), PanelHeight);

        _lastSafeArea = Screen.safeArea;
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
    }

    float ComputeBottomOffset()
    {
        // Match ScaleBarController / time bar: fixed bottom stack (no safeBottom jump).
        float y = TimeControlUiBootstrap.BarBottomMargin
            + TimeControlUiBootstrap.BarHeight
            + CpuLoadMonitor.GapAboveTimeBar;

        if (_cpuEnabled)
            y += CpuLoadMonitor.PanelHeight + 8f;

        y += ScaleBarPanelHeight + GapAboveScaleBar;
        return y;
    }
}
