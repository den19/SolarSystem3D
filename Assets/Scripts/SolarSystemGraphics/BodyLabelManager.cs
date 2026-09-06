using System.Collections.Generic;
using SolarSystemApp;
using TMPro;
using UnityEngine;

/// <summary>
/// World-space billboard labels for celestial bodies and comets.
/// </summary>
public class BodyLabelManager : MonoBehaviour
{
    const float BaseBodyFontSize = 2.4f;
    const float BaseCometFontSize = 3.2f;
    const float SunFontScale = 1.7f;
    const float ObservationFontScale = 2f;
    const float OrbitElementsFontScale = 0.55f;
    const float OrbitElementsGapFactor = 0.7f;

    struct LabelEntry
    {
        public Transform target;
        public string labelKey;
        public float verticalOffset;
        public float baselineTargetScale;
        public float baseFontSize;
        public bool alwaysVisible;
        public TextMeshPro label;
        public TextMeshPro orbitElements;
    }

    static readonly string[] BodyNames =
    {
        "Sun", "Mercury", "Venus", "Earth", "Moon", "Mars", "Phobos", "Deimos", "Jupiter", "Io", "Europa", "Ganymede", "Callisto", "Saturn", "Titan", "Uranus", "Neptune", "Triton", "Pluto"
    };

    readonly List<LabelEntry> _entries = new List<LabelEntry>();
    Camera _mainCamera;
    LookAtTarget _lookAtTarget;
    bool _visible;

    void Awake()
    {
        _mainCamera = Camera.main;
        _lookAtTarget = FindFirstObjectByType<LookAtTarget>();
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

    public void RegisterCometLabel(Transform cometTransform, string labelKey, float verticalOffset = 1.6f)
    {
        _entries.Add(new LabelEntry
        {
            target = cometTransform,
            labelKey = labelKey,
            verticalOffset = verticalOffset,
            baselineTargetScale = cometTransform.lossyScale.x,
            baseFontSize = BaseCometFontSize,
            alwaysVisible = true,
            label = CreateCometLabelObject(cometTransform.name + "_Label", labelKey),
            orbitElements = CreateOrbitElementsLabel(cometTransform.name + "_OrbitElements")
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

            string labelKey = bodyName + "Header";
            float fontSize = bodyName == "Sun"
                ? BaseBodyFontSize * SunFontScale
                : BaseBodyFontSize;
            _entries.Add(new LabelEntry
            {
                target = bodyGo.transform,
                labelKey = labelKey,
                verticalOffset = 1.5f,
                baselineTargetScale = bodyGo.transform.lossyScale.x,
                baseFontSize = fontSize,
                label = CreateLabelObject(bodyName + "_OrbitLabel", labelKey),
                orbitElements = CreateOrbitElementsLabel(bodyName + "_OrbitElements")
            });
        }

        RefreshAllTexts();
    }

    TextMeshPro CreateLabelObject(string objectName, string labelKey)
    {
        var labelGo = new GameObject(objectName);
        labelGo.transform.SetParent(transform, false);
        var tmp = labelGo.AddComponent<TextMeshPro>();
        tmp.fontSize = BaseBodyFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.95f, 0.95f, 1f, 0.92f);
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.sortingOrder = 10;

        ApplyLabelFont(tmp);
        return tmp;
    }

    TextMeshPro CreateCometLabelObject(string objectName, string labelKey)
    {
        var labelGo = new GameObject(objectName);
        labelGo.transform.SetParent(transform, false);
        var tmp = labelGo.AddComponent<TextMeshPro>();
        tmp.fontSize = BaseCometFontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.95f, 0.7f, 1f);
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.sortingOrder = 20;

        ApplyLabelFont(tmp);
        return tmp;
    }

    TextMeshPro CreateOrbitElementsLabel(string objectName)
    {
        var labelGo = new GameObject(objectName);
        labelGo.transform.SetParent(transform, false);
        var tmp = labelGo.AddComponent<TextMeshPro>();
        tmp.fontSize = BaseBodyFontSize * OrbitElementsFontScale;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = OrbitElementsDisplay.IttenOrange;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.characterSpacing = 4f;
        tmp.sortingOrder = 11;
        tmp.gameObject.SetActive(false);

        ApplyLabelFont(tmp);
        return tmp;
    }

    static void ApplyLabelFont(TextMeshPro tmp)
    {
        var lang = LocalizationManager.CurrentLanguage;
        tmp.font = LocalizationFontHelper.GetFontForLanguage(lang);
        var overlay = LocalizationFontHelper.GetOverlayMaterialForLanguage(lang);
        if (overlay != null)
            tmp.fontSharedMaterial = overlay;
    }

    void LateUpdate()
    {
        if (_mainCamera == null)
            return;

        if (_lookAtTarget == null)
            _lookAtTarget = FindFirstObjectByType<LookAtTarget>();

        Transform observedTarget = _lookAtTarget != null && _lookAtTarget.currentTarget != null
            ? _lookAtTarget.currentTarget.transform
            : null;

        for (int i = 0; i < _entries.Count; i++)
        {
            LabelEntry entry = _entries[i];
            if (entry.target == null || entry.label == null)
                continue;

            if (!ShouldShowEntry(entry, _visible))
            {
                if (entry.label.gameObject.activeSelf)
                    entry.label.gameObject.SetActive(false);
                if (entry.orbitElements != null && entry.orbitElements.gameObject.activeSelf)
                    entry.orbitElements.gameObject.SetActive(false);
                continue;
            }

            if (!entry.label.gameObject.activeSelf)
                entry.label.gameObject.SetActive(true);

            bool isObserved = IsObservedEntry(entry, observedTarget);
            float fontScale = isObserved ? ObservationFontScale : 1f;
            float desiredFontSize = entry.baseFontSize * fontScale;
            if (!Mathf.Approximately(entry.label.fontSize, desiredFontSize))
                entry.label.fontSize = desiredFontSize;

            Vector3 worldPos = entry.target.position + Vector3.up * GetEffectiveOffset(entry, fontScale);
            entry.label.transform.position = worldPos;
            entry.label.transform.rotation = Quaternion.LookRotation(_mainCamera.transform.forward, _mainCamera.transform.up);

            UpdateOrbitElementsLabel(entry, isObserved, worldPos, desiredFontSize);
        }
    }

    void UpdateOrbitElementsLabel(LabelEntry entry, bool isObserved, Vector3 nameWorldPos, float nameFontSize)
    {
        if (entry.orbitElements == null)
            return;

        string text = string.Empty;
        bool show = isObserved
            && _lookAtTarget != null
            && _lookAtTarget.IsEncyclopediaOpenFor(entry.target)
            && entry.target != null
            && OrbitElementsDisplay.TryFormat(entry.target.name, out text);

        if (!show)
        {
            if (entry.orbitElements.gameObject.activeSelf)
                entry.orbitElements.gameObject.SetActive(false);
            return;
        }

        if (!entry.orbitElements.gameObject.activeSelf)
            entry.orbitElements.gameObject.SetActive(true);

        if (entry.orbitElements.text != text)
            entry.orbitElements.text = text;

        float eiFontSize = nameFontSize * OrbitElementsFontScale;
        if (!Mathf.Approximately(entry.orbitElements.fontSize, eiFontSize))
            entry.orbitElements.fontSize = eiFontSize;

        Vector3 eiPos = nameWorldPos - _mainCamera.transform.up * (nameFontSize * OrbitElementsGapFactor);
        entry.orbitElements.transform.position = eiPos;
        entry.orbitElements.transform.rotation = Quaternion.LookRotation(_mainCamera.transform.forward, _mainCamera.transform.up);
    }

    static bool IsObservedEntry(LabelEntry entry, Transform observedTarget)
    {
        if (observedTarget == null || entry.target == null)
            return false;

        if (entry.target == observedTarget)
            return true;

        // Comet focus may be on a root while the label tracks a child (or vice versa).
        return entry.target.IsChildOf(observedTarget) || observedTarget.IsChildOf(entry.target);
    }

    static float GetEffectiveOffset(LabelEntry entry, float fontScale)
    {
        if (entry.target == null)
            return entry.verticalOffset * fontScale;

        float meshRadius = 0.5f * Mathf.Max(0.0001f, entry.target.lossyScale.x);
        return Mathf.Max(0.15f, meshRadius * 1.4f) * fontScale;
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

        if (entry.orbitElements != null)
        {
            entry.orbitElements.font = LocalizationFontHelper.GetFontForLanguage(lang);
            var eiOverlay = LocalizationFontHelper.GetOverlayMaterialForLanguage(lang);
            if (eiOverlay != null)
                entry.orbitElements.fontSharedMaterial = eiOverlay;
            entry.orbitElements.color = OrbitElementsDisplay.IttenOrange;

            if (entry.target != null && OrbitElementsDisplay.TryFormat(entry.target.name, out string eiText))
                entry.orbitElements.text = eiText;
        }
    }

    void ApplyVisibility(bool visible)
    {
        _visible = visible;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].label != null)
                _entries[i].label.gameObject.SetActive(ShouldShowEntry(_entries[i], visible));
            if (_entries[i].orbitElements != null)
                _entries[i].orbitElements.gameObject.SetActive(false);
        }
    }

    static bool ShouldShowEntry(LabelEntry entry, bool bodyLabelsVisible)
    {
        if (entry.target != null && !entry.target.gameObject.activeInHierarchy)
            return false;

        return entry.alwaysVisible || bodyLabelsVisible;
    }
}
