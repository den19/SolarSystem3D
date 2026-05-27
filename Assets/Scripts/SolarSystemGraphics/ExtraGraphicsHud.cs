using SolarSystemApp;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds semi-transparent HUD with a button mirror of PlayerPrefs-backed Extra Graphics state.
/// Attached under MainScreenCanvas at runtime while Level1 is active.
/// </summary>
public static class ExtraGraphicsHud
{
    const string HudRootName = "ExtraGraphicsPanel_Runtime";
    const string LabelOnKey = "ExtraGraphicsHud_On";
    const string LabelOffKey = "ExtraGraphicsHud_Off";
    const string LabelOnFallback = "Extra Graphics: ON";
    const string LabelOffFallback = "Extra Graphics: OFF";
    static Button _toggleButton;
    static Text _label;
    static bool _subscribed;
    static bool _languageSubscribed;

    public static void SpawnHud(RectTransform hostCanvasRt)
    {
        if (!hostCanvasRt)
            return;

        var panel = hostCanvasRt.Find(HudRootName);
        if (!panel)
        {
            Debug.LogWarning("ExtraGraphicsHud: design-time panel 'ExtraGraphicsPanel_Runtime' is missing under MainScreenCanvas.");
            return;
        }

        Bind(panel);
    }

    static void Bind(Transform panel)
    {
        var newLabel = panel.Find("Label")?.GetComponent<Text>();
        if (!newLabel)
        {
            Debug.LogWarning("ExtraGraphicsHud: missing child Text 'Label' on ExtraGraphicsPanel_Runtime.");
            return;
        }

        var newButton = panel.GetComponent<Button>();
        if (!newButton)
        {
            Debug.LogWarning("ExtraGraphicsHud: missing Button on ExtraGraphicsPanel_Runtime.");
            return;
        }

        if (_toggleButton)
            _toggleButton.onClick.RemoveListener(OnToggleClicked);

        _toggleButton = newButton;
        _label = newLabel;
        _toggleButton.onClick.AddListener(OnToggleClicked);

        if (!_subscribed)
        {
            GraphicsSettings.UseExtraGraphicsChanged += OnUseExtraGraphicsChanged;
            _subscribed = true;
        }

        if (!_languageSubscribed)
        {
            LocalizationManager.OnLanguageChanged += OnLanguageChanged;
            _languageSubscribed = true;
        }

        ApplyLabel(_label);
    }

    static void ApplyLabel(Text label)
    {
        if (!label) return;
        var isExtraGraphicsEnabled = GraphicsSettings.UseExtraGraphics;
        var key = isExtraGraphicsEnabled ? LabelOnKey : LabelOffKey;
        var fallback = isExtraGraphicsEnabled ? LabelOnFallback : LabelOffFallback;

        var localizationManager = LocalizationManager.Instance;
        var localizedText = localizationManager ? localizationManager.GetTranslation(key) : null;
        label.text = string.IsNullOrEmpty(localizedText) ? fallback : localizedText;
    }

    static void OnUseExtraGraphicsChanged(bool _)
    {
        ApplyLabel(_label);
    }

    static void OnLanguageChanged()
    {
        ApplyLabel(_label);
    }

    static void OnToggleClicked() => GraphicsSettings.SetUseExtraGraphics(!GraphicsSettings.UseExtraGraphics);
}
