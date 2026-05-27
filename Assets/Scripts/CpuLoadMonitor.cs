using SolarSystemApp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CpuLoadMonitor : MonoBehaviour
{
    [SerializeField] float updateInterval = 0.75f;
    [SerializeField] bool showUnavailableAsDash = true;

    const string UnavailableLabel = "CPU: --%";

    TextMeshProUGUI label;
    Image panel;
    float accumulatedDelta;
    int frameCount;
    float nextUpdateTime;
    bool settingEnabled = true;

    void Awake()
    {
        BuildUi();
        transform.SetAsLastSibling();
        ApplySetting(CpuMonitorSettings.UseCpuMonitor);
        CpuMonitorSettings.UseCpuMonitorChanged += OnUseCpuMonitorChanged;
        nextUpdateTime = Time.unscaledTime + updateInterval;
    }

    void OnDestroy()
    {
        CpuMonitorSettings.UseCpuMonitorChanged -= OnUseCpuMonitorChanged;
    }

    void Update()
    {
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
        var rect = GetComponent<RectTransform>();
        if (rect == null)
            rect = gameObject.AddComponent<RectTransform>();

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-16f, 16f);
        rect.sizeDelta = new Vector2(140f, 32f);

        if (GetComponent<CanvasRenderer>() == null)
            gameObject.AddComponent<CanvasRenderer>();

        panel = GetComponent<Image>();
        if (panel == null)
            panel = gameObject.AddComponent<Image>();

        panel.color = new Color(0f, 0f, 0f, 0.65f);
        panel.raycastTarget = false;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(transform, false);

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);

        label = labelGo.AddComponent<TextMeshProUGUI>();
        label.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        label.fontSharedMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Overlay");
        label.fontSize = 15f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineRight;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.text = UnavailableLabel;
    }

    void RefreshDisplay()
    {
        if (!settingEnabled || label == null)
            return;

        float targetFrameTime = GetTargetFrameTime();
        if (targetFrameTime <= 0f || frameCount <= 0)
        {
            label.text = showUnavailableAsDash ? UnavailableLabel : "CPU: 0.0%";
            return;
        }

        float avgFrameTime = accumulatedDelta / frameCount;
        float loadPercent = Mathf.Clamp(avgFrameTime / targetFrameTime * 100f, 0f, 100f);
        label.text = $"CPU: {loadPercent:F1}%";
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
