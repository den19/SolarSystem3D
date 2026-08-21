using SolarSystemApp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Step-by-step coach overlay until the first successful probe launch.
/// </summary>
public class ProbeCoachOverlay : MonoBehaviour
{
    const float TitleFontSize = 30f;
    const float BodyFontSize = 28f;
    const float ButtonFontSize = 26f;
    const float StepFontSize = 22f;

    RectTransform _root;
    RectTransform _card;
    TextMeshProUGUI _stepLabel;
    TextMeshProUGUI _title;
    TextMeshProUGUI _body;
    Button _backButton;
    Button _skipButton;
    Button _forwardButton;
    TextMeshProUGUI _backLabel;
    TextMeshProUGUI _skipLabel;
    TextMeshProUGUI _forwardLabel;
    const int FirstCoachStep = 2;
    int _manualStep = -1;
    int _lastRenderedStep = int.MinValue;
    float _lastCardWidth = -1f;
    LookAtTarget _lookAt;

    public static ProbeCoachOverlay EnsureOnHud(Transform hudRoot)
    {
        if (hudRoot == null)
            return null;

        Transform existing = hudRoot.Find("ProbeCoach");
        if (existing != null)
        {
            var overlay = existing.GetComponent<ProbeCoachOverlay>();
            if (overlay == null)
                overlay = existing.gameObject.AddComponent<ProbeCoachOverlay>();
            return overlay;
        }

        var go = new GameObject("ProbeCoach", typeof(RectTransform), typeof(ProbeCoachOverlay));
        go.layer = hudRoot.gameObject.layer;
        go.transform.SetParent(hudRoot, false);
        var coach = go.GetComponent<ProbeCoachOverlay>();
        coach.Build();
        return coach;
    }

    void OnEnable()
    {
        ProbeCoachSettings.LaunchCompletedChanged += RefreshVisibility;
        ProbeSettings.UseProbeChanged += OnProbeSettingChanged;
        LocalizationManager.OnLanguageChanged += RefreshLabels;
        _lookAt = FindFirstObjectByType<LookAtTarget>();
        RefreshVisibility();
    }

    void OnDisable()
    {
        ProbeCoachSettings.LaunchCompletedChanged -= RefreshVisibility;
        ProbeSettings.UseProbeChanged -= OnProbeSettingChanged;
        LocalizationManager.OnLanguageChanged -= RefreshLabels;
    }

    void OnProbeSettingChanged(bool _) => RefreshVisibility();

    void Update()
    {
        if (!ShouldShow())
            return;

        RefreshStep(forceLayout: false);
        LayoutCardIfNeeded();
    }

    void Build()
    {
        _root = GetComponent<RectTransform>();
        Stretch(_root);

        var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dimGo.layer = gameObject.layer;
        dimGo.transform.SetParent(_root, false);
        Stretch(dimGo.GetComponent<RectTransform>());
        dimGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        dimGo.GetComponent<Image>().raycastTarget = false;

        var cardGo = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        cardGo.layer = gameObject.layer;
        cardGo.transform.SetParent(_root, false);
        _card = cardGo.GetComponent<RectTransform>();
        _card.anchorMin = new Vector2(0.5f, 0.5f);
        _card.anchorMax = new Vector2(0.5f, 0.5f);
        _card.pivot = new Vector2(0.5f, 0.5f);
        _card.sizeDelta = new Vector2(680f, 0f);
        cardGo.GetComponent<Image>().color = new Color(0.06f, 0.1f, 0.18f, 0.96f);
        cardGo.GetComponent<Image>().raycastTarget = true;

        var layout = cardGo.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var cardFitter = cardGo.GetComponent<ContentSizeFitter>();
        cardFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _stepLabel = CreateTmp(cardGo.transform, "ProbeCoachStepIndicator", StepFontSize, TextAlignmentOptions.TopLeft);
        _title = CreateTmp(cardGo.transform, "ProbeCoachTitle", TitleFontSize, TextAlignmentOptions.TopLeft);
        _title.fontStyle = FontStyles.Bold;
        _body = CreateTmp(cardGo.transform, "ProbeCoachBody", BodyFontSize, TextAlignmentOptions.TopLeft);

        var buttonRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        buttonRow.layer = gameObject.layer;
        buttonRow.transform.SetParent(cardGo.transform, false);
        var rowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.childForceExpandHeight = true;
        rowLayout.childForceExpandWidth = true;
        var rowLe = buttonRow.GetComponent<LayoutElement>();
        rowLe.minHeight = 56f;
        rowLe.preferredHeight = 56f;

        _backButton = CreateCoachButton(buttonRow.transform, "ProbeCoachBack", out _backLabel);
        _skipButton = CreateCoachButton(buttonRow.transform, "ProbeCoachSkip", out _skipLabel);
        _forwardButton = CreateCoachButton(buttonRow.transform, "ProbeCoachForward", out _forwardLabel);
        _backButton.onClick.AddListener(GoBackCoach);
        _skipButton.onClick.AddListener(SkipCoach);
        _forwardButton.onClick.AddListener(ForwardCoach);

        RefreshLabels();
        RefreshStep();
    }

    public void OpenManual()
    {
        if (ProbeCoachSettings.LaunchCompleted)
        {
            TransientMessageController.ShowLocalized(
                "ProbeCoachHelpDone",
                "Tips already completed.");
            return;
        }

        _manualStep = ResolveAutoStep();
        RefreshVisibility();
    }

    void SkipCoach()
    {
        _manualStep = -1;
        gameObject.SetActive(false);
    }

    void GoBackCoach()
    {
        int step = CurrentStep();
        if (step <= FirstCoachStep)
            return;

        _manualStep = step - 1;
        RefreshStep(forceLayout: true);
    }

    void ForwardCoach()
    {
        int step = CurrentStep();
        if (step >= 4)
        {
            ProbeSystemController.Instance?.Launch();
            return;
        }

        _manualStep = step + 1;
        RefreshStep(forceLayout: true);
    }

    int CurrentStep() => _manualStep >= 0 ? _manualStep : ResolveAutoStep();

    public void RefreshVisibility()
    {
        if (!ProbeSettings.UseProbe)
            return;

        bool show = ShouldShow();
        gameObject.SetActive(show);
        if (!show)
            return;

        LayoutCardIfNeeded(force: true);
        RefreshStep(forceLayout: true);
    }

    void LayoutCardIfNeeded(bool force = false)
    {
        if (_card == null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        float scale = canvas != null && canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
        float canvasWidth = Screen.width / scale;
        float width = Mathf.Min(Mathf.Max(320f, canvasWidth * 0.9f), 760f);
        if (!force && Mathf.Abs(width - _lastCardWidth) < 0.5f)
            return;

        _lastCardWidth = width;
        _card.sizeDelta = new Vector2(width, _card.sizeDelta.y);
        SyncTextPreferredHeights();
        RebuildCardLayout();
    }

    void RebuildCardLayout()
    {
        if (_card == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_card);
    }

    bool ShouldShow()
    {
        if (ProbeCoachSettings.LaunchCompleted)
            return false;

        if (!ProbeSettings.UseProbe)
            return false;

        var system = ProbeSystemController.Instance;
        if (system != null && system.IsFlying)
            return false;

        return true;
    }

    void RefreshStep(bool forceLayout = true)
    {
        if (!ShouldShow() || _title == null)
            return;

        int step = CurrentStep();
        bool stepChanged = step != _lastRenderedStep;
        if (!forceLayout && !stepChanged)
            return;

        _lastRenderedStep = step;
        _stepLabel.text = string.Format("{0}/4", step);

        string titleKey = "ProbeCoachStep" + step + "Title";
        string bodyKey = "ProbeCoachStep" + step + "Body";
        _title.text = T(titleKey, titleKey);
        _body.text = T(bodyKey, bodyKey);
        SyncTextPreferredHeights();

        if (_backButton != null)
            _backButton.interactable = step > FirstCoachStep;

        RebuildCardLayout();
    }

    void SyncTextPreferredHeights()
    {
        SyncTmpPreferredHeight(_stepLabel);
        SyncTmpPreferredHeight(_title);
        SyncTmpPreferredHeight(_body);
    }

    static void SyncTmpPreferredHeight(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;

        var le = tmp.GetComponent<LayoutElement>();
        if (le == null)
            return;

        float width = tmp.rectTransform.rect.width;
        if (width < 1f && tmp.rectTransform.parent is RectTransform parent)
            width = Mathf.Max(1f, parent.rect.width - 48f);

        Vector2 preferred = tmp.GetPreferredValues(tmp.text, width > 1f ? width : 680f, float.PositiveInfinity);
        le.minHeight = preferred.y;
        le.preferredHeight = preferred.y;
    }

    int ResolveAutoStep()
    {
        if (!HasCatalogBodyFocused())
            return 2;

        return 3;
    }

    bool HasCatalogBodyFocused()
    {
        if (_lookAt == null)
            _lookAt = FindFirstObjectByType<LookAtTarget>();

        GameObject target = _lookAt != null ? _lookAt.currentTarget : null;
        if (target == null)
            return false;

        if (target.name == ProbePrefabFactory.RootName)
            return false;

        return true;
    }

    void RefreshLabels()
    {
        if (_backLabel != null)
            _backLabel.text = T("ProbeCoachBack", "Back");
        if (_skipLabel != null)
            _skipLabel.text = T("ProbeCoachSkip", "Skip");
        if (_forwardLabel != null)
            _forwardLabel.text = T("ProbeCoachForward", "Forward");
        _lastRenderedStep = int.MinValue;
        RefreshStep(forceLayout: true);
    }

    static Button CreateCoachButton(Transform parent, string key, out TextMeshProUGUI label)
    {
        var go = new GameObject(key, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.18f, 0.24f, 0.34f, 1f);
        go.GetComponent<Image>().raycastTarget = true;
        var le = go.GetComponent<LayoutElement>();
        le.minHeight = 56f;
        le.preferredHeight = 56f;

        label = CreateTmp(go.transform, key + "_Text", ButtonFontSize, TextAlignmentOptions.Center);
        label.text = key;
        label.raycastTarget = false;
        return go.GetComponent<Button>();
    }

    static TextMeshProUGUI CreateTmp(Transform parent, string name, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.minHeight = size + 8f;
        le.preferredHeight = size + 8f;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = new Color(0.92f, 0.95f, 1f, 1f);
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        var font = LocalizationFontHelper.GetFontForLanguage(LocalizationManager.CurrentLanguage);
        if (font != null)
            tmp.font = font;
        return tmp;
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
