using System;
using SolarSystemApp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CpuLoadMonitor : MonoBehaviour
{
    [SerializeField] float updateInterval = 0.75f;
    [SerializeField] bool showUnavailableAsDash = true;

    const string KeyUnavailable = "CpuLoadUnavailable";
    const string KeyZero = "CpuLoadZero";
    const string KeyFormat = "CpuLoadFormat";
    const string FallbackUnavailable = "CPU: --%";
    const string FallbackZero = "CPU: 0.0%";
    const string FallbackFormat = "CPU: {0:F1}%";
    const float HorizontalMargin = 16f;
    const float BottomMarginLandscape = 16f;
    const float GapAboveTimeBar = 8f;
    const float PanelWidth = 140f;
    const float PanelHeight = 32f;

    TextMeshProUGUI label;
    Image panel;
    RectTransform rect;
    Canvas canvas;
    float accumulatedDelta;
    int frameCount;
    float nextUpdateTime;
    bool settingEnabled = true;
    Rect lastSafeArea;
    int lastScreenWidth;
    int lastScreenHeight;
    bool layoutCached;

    static bool IsLandscape => Screen.width > Screen.height;

    void Awake()
    {
        BuildUi();
        transform.SetAsLastSibling();
        ApplySetting(CpuMonitorSettings.UseCpuMonitor);
        nextUpdateTime = Time.unscaledTime + updateInterval;
        RefreshSafeAreaLayout();
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        CpuMonitorSettings.UseCpuMonitorChanged += OnUseCpuMonitorChanged;
        RefreshDisplay();
        RefreshSafeAreaLayout();
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        CpuMonitorSettings.UseCpuMonitorChanged -= OnUseCpuMonitorChanged;
    }

    void Update()
    {
        if (layoutCached)
        {
            Rect safeArea = Screen.safeArea;
            if (safeArea != lastSafeArea
                || Screen.width != lastScreenWidth
                || Screen.height != lastScreenHeight)
            {
                RefreshSafeAreaLayout();
            }
        }

        if (!settingEnabled)
            return;

        accumulatedDelta += Time.unscaledDeltaTime;
        frameCount++;

        if (Time.unscaledTime < nextUpdateTime)
            return;

        RefreshDisplay();
        accumulatedDelta = 0f;
        frameCount = 0;
        nextUpdateTime = Time.unscaledTime + updateInterval;
    }

    void BuildUi()
    {
        rect = GetComponent<RectTransform>();
        if (rect == null)
            rect = gameObject.AddComponent<RectTransform>();

        canvas = GetComponentInParent<Canvas>();

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        if (GetComponent<CanvasRenderer>() == null)
            gameObject.AddComponent<CanvasRenderer>();

        panel = GetComponent<Image>();
        if (panel == null)
            panel = gameObject.AddComponent<Image>();

        panel.color = new Color(0f, 0f, 0f, 0.65f);
        panel.raycastTarget = false;

        Transform existingLabel = transform.Find("Label");
        GameObject labelGo;
        if (existingLabel != null)
        {
            labelGo = existingLabel.gameObject;
        }
        else
        {
            labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(transform, false);
        }

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);

        label = labelGo.GetComponent<TextMeshProUGUI>();
        if (label == null)
            label = labelGo.AddComponent<TextMeshProUGUI>();

        var lang = LocalizationManager.CurrentLanguage;
        label.font = LocalizationFontHelper.GetFontForLanguage(lang);
        var overlay = LocalizationFontHelper.GetOverlayMaterialForLanguage(lang);
        if (overlay != null)
            label.fontSharedMaterial = overlay;
        label.fontSize = 15f;
        label.color = new Color(1f, 0.6078f, 0.451f, 0.1f);
        label.alignment = TextAlignmentOptions.MidlineRight;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.text = GetUnavailableText();
    }

    public void RefreshSafeAreaLayout()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        SafeAreaInsets.GetCanvasInsets(canvas, out _, out float safeRight, out _, out float safeBottom);

        float x = -(safeRight + HorizontalMargin);
        float y;
        if (IsLandscape)
        {
            y = safeBottom + BottomMarginLandscape;
        }
        else
        {
            // Sit above SimulationTimeControlBar so they do not overlap in portrait.
            y = safeBottom
                + TimeControlUiBootstrap.BarBottomMargin
                + TimeControlUiBootstrap.BarHeight
                + GapAboveTimeBar;
        }

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        rect.anchoredPosition = new Vector2(x, y);

        lastSafeArea = Screen.safeArea;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        layoutCached = true;
    }

    void OnLanguageChanged()
    {
        if (label != null)
        {
            var lang = LocalizationManager.CurrentLanguage;
            label.font = LocalizationFontHelper.GetFontForLanguage(lang);
            var overlay = LocalizationFontHelper.GetOverlayMaterialForLanguage(lang);
            if (overlay != null)
                label.fontSharedMaterial = overlay;
        }
        RefreshDisplay();
    }

    void RefreshDisplay()
    {
        if (!settingEnabled || label == null)
            return;

        float targetFrameTime = GetTargetFrameTime();
        if (targetFrameTime <= 0f || frameCount <= 0)
        {
            label.text = showUnavailableAsDash ? GetUnavailableText() : GetZeroText();
            return;
        }

        float avgFrameTime = accumulatedDelta / frameCount;
        float loadPercent = Mathf.Clamp(avgFrameTime / targetFrameTime * 100f, 0f, 100f);
        label.text = FormatLoadText(loadPercent);
    }

    static string GetUnavailableText() => Translate(KeyUnavailable, FallbackUnavailable);

    static string GetZeroText() => Translate(KeyZero, FallbackZero);

    static string FormatLoadText(float loadPercent)
    {
        string format = Translate(KeyFormat, FallbackFormat);
        try
        {
            return string.Format(format, loadPercent);
        }
        catch (FormatException)
        {
            return string.Format(FallbackFormat, loadPercent);
        }
    }

    static string Translate(string key, string fallback)
    {
        if (LocalizationManager.Instance == null)
            return fallback;

        string translation = LocalizationManager.Instance.GetTranslation(key);
        return string.IsNullOrEmpty(translation) ? fallback : translation;
    }

    void OnUseCpuMonitorChanged(bool isOn)
    {
        ApplySetting(isOn);
    }

    void ApplySetting(bool isOn)
    {
        settingEnabled = isOn;

        if (panel != null)
            panel.enabled = isOn;

        if (label != null)
            label.gameObject.SetActive(isOn);
    }

    static float GetTargetFrameTime()
    {
        if (Application.targetFrameRate > 0)
            return 1f / Application.targetFrameRate;

        if (QualitySettings.vSyncCount > 0)
        {
            int refreshRate = Screen.currentResolution.refreshRate;
            if (refreshRate > 0)
                return 1f / refreshRate;
        }

        int fallbackHz = Screen.currentResolution.refreshRate;
        if (fallbackHz <= 0)
            fallbackHz = 60;

        return 1f / fallbackHz;
    }
}
