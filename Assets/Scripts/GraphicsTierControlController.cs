using System.Collections;
using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MainMenu control for Auto / Balanced / High graphics tier.
/// Expects a scene-authored GraphicsTier row under SettingsWindow (see GraphicsTierSceneSetup).
/// </summary>
public class GraphicsTierControlController : MonoBehaviour
{
    const string RowName = "GraphicsTier";
    const string LabelName = "GraphicsTierLabel";
    const string DropdownName = "GraphicsTierDropdown";

    [SerializeField] Dropdown tierDropdown;
    [SerializeField] Text labelText;

    bool isInitializing;

    /// <summary>
    /// Finds the scene GraphicsTier controller. Does not create UI at runtime.
    /// </summary>
    public static GraphicsTierControlController FindInScene()
    {
        var existing = Object.FindFirstObjectByType<GraphicsTierControlController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            existing.ResolveReferences();
            return existing;
        }

        Transform row = FindNamed(RowName);
        if (row == null)
        {
            Debug.LogWarning(
                "GraphicsTier row missing from MainMenu scene. Run menu: Solar System / Setup Graphics Tier UI.");
            return null;
        }

        var controller = row.GetComponent<GraphicsTierControlController>();
        if (controller == null)
            controller = row.gameObject.AddComponent<GraphicsTierControlController>();

        controller.ResolveReferences();
        return controller;
    }

    static Transform FindNamed(string name)
    {
        GameObject found = GameObject.Find(name);
        return found != null ? found.transform : null;
    }

    IEnumerator Start()
    {
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
