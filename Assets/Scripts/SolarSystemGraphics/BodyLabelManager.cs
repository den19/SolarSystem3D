using System.Collections.Generic;
using SolarSystemApp;
using TMPro;
using UnityEngine;

/// <summary>
/// World-space billboard labels for celestial bodies and comets.
/// </summary>
public class BodyLabelManager : MonoBehaviour
{
    struct LabelEntry
    {
        public Transform target;
        public string labelKey;
        public float verticalOffset;
        public TextMeshPro label;
    }

    static readonly string[] BodyNames =
    {
        "Sun", "Mercury", "Venus", "Earth", "Moon", "Mars", "Jupiter", "Saturn", "Titan", "Uranus", "Neptune"
    };

    static readonly Dictionary<string, float> BodyOffsets = new Dictionary<string, float>
    {
        { "Sun", 3.5f },
        { "Jupiter", 2.5f },
        { "Saturn", 2.2f }
    };

    readonly List<LabelEntry> _entries = new List<LabelEntry>();
    Camera _mainCamera;
    bool _visible;

    void Awake()
    {
        _mainCamera = Camera.main;
        BuildBodyLabels();
        ApplyVisibility(SimulationViewSettings.ShowBodyLabels);
    }

    void OnEnable()
    {
        SimulationViewSettings.ShowBodyLabelsChanged += ApplyVisibility;
        LocalizationManager.OnLanguageChanged += RefreshAllTexts;
    }

    void OnDisable()
    {
        SimulationViewSettings.ShowBodyLabelsChanged -= ApplyVisibility;
        LocalizationManager.OnLanguageChanged -= RefreshAllTexts;
    }

    public void RegisterCometLabel(Transform cometTransform, string labelKey, float verticalOffset = 1.2f)
    {
        _entries.Add(new LabelEntry
        {
            target = cometTransform,
            labelKey = labelKey,
            verticalOffset = verticalOffset,
            label = CreateLabelObject(cometTransform.name + "_Label", labelKey)
        });
        RefreshEntryText(_entries[_entries.Count - 1]);
        ApplyVisibility(_visible);
    }

    void BuildBodyLabels()
    {
        foreach (string bodyName in BodyNames)
        {
            GameObject bodyGo = GameObject.Find(bodyName);
            if (bodyGo == null)
                continue;

            float offset = 1.5f;
            if (BodyOffsets.TryGetValue(bodyName, out float customOffset))
                offset = customOffset;

            string labelKey = bodyName + "Header";
            _entries.Add(new LabelEntry
            {
                target = bodyGo.transform,
                labelKey = labelKey,
                verticalOffset = offset,
                label = CreateLabelObject(bodyName + "_OrbitLabel", labelKey)
            });
        }

        RefreshAllTexts();
    }

    TextMeshPro CreateLabelObject(string objectName, string labelKey)
    {
        var labelGo = new GameObject(objectName);
        labelGo.transform.SetParent(transform, false);
        var tmp = labelGo.AddComponent<TextMeshPro>();
        tmp.fontSize = 2.4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.95f, 0.95f, 1f, 0.92f);
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.sortingOrder = 10;

        var lang = LocalizationManager.CurrentLanguage;
        tmp.font = LocalizationFontHelper.GetFontForLanguage(lang);
        var overlay = LocalizationFontHelper.GetOverlayMaterialForLanguage(lang);
        if (overlay != null)
            tmp.fontSharedMaterial = overlay;

        return tmp;
    }

    void LateUpdate()
    {
        if (!_visible || _mainCamera == null)
            return;

        for (int i = 0; i < _entries.Count; i++)
        {
            LabelEntry entry = _entries[i];
            if (entry.target == null || entry.label == null)
                continue;

            Vector3 worldPos = entry.target.position + Vector3.up * entry.verticalOffset;
            entry.label.transform.position = worldPos;
            entry.label.transform.rotation = Quaternion.LookRotation(_mainCamera.transform.forward, _mainCamera.transform.up);
        }
    }

    void RefreshAllTexts()
    {
        for (int i = 0; i < _entries.Count; i++)
            RefreshEntryText(_entries[i]);
    }

    static void RefreshEntryText(LabelEntry entry)
    {
        if (entry.label == null)
            return;

        var lang = LocalizationManager.CurrentLanguage;
        entry.label.font = LocalizationFontHelper.GetFontForLanguage(lang);
        var overlay = LocalizationFontHelper.GetOverlayMaterialForLanguage(lang);
        if (overlay != null)
            entry.label.fontSharedMaterial = overlay;

        string text = entry.labelKey;
        if (LocalizationManager.Instance != null)
        {
            string translation = LocalizationManager.Instance.GetTranslation(entry.labelKey);
            if (!string.IsNullOrEmpty(translation))
                text = translation;
        }

        entry.label.text = text;
    }

    void ApplyVisibility(bool visible)
    {
        _visible = visible;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].label != null)
                _entries[i].label.gameObject.SetActive(visible);
        }
    }
}
