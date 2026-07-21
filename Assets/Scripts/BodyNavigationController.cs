using System.Collections;
using System.Collections.Generic;
using SolarSystemApp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene-wired navigation bar for cycling celestial bodies with showcase camera.
/// </summary>
public class BodyNavigationController : MonoBehaviour
{
    [SerializeField] Button prevButton;
    [SerializeField] Button nextButton;
    [SerializeField] Button bodyNameButton;
    [SerializeField] TMP_Text bodyNameText;
    [SerializeField] RectTransform barRect;
    [SerializeField] BodyNavigationPickerController bodyNavigationPicker;
    [SerializeField] BodyNameLongPressHandler bodyNameLongPressHandler;

    LookAtTarget _lookAtTarget;
    BodyShowcaseCameraController _showcaseCamera;
    List<BodyNavigationOrder.NavigationEntry> _entries = new List<BodyNavigationOrder.NavigationEntry>();
    int _currentIndex = -1;
    bool _suppressTargetSync;
    bool _screenLayoutCached;
    Rect _lastSafeArea;
    int _lastScreenWidth;
    int _lastScreenHeight;
    Coroutine _safeAreaRefreshRoutine;
    SimulationSidePanelController _sidePanelController;
    HorizontalLayoutGroup _barLayoutGroup;

    struct ToolbarLayoutProfile
    {
        public float IconButtonSize;
        public float SimControlPreferred;
        public float SimControlMin;
        public float NameMinWidth;
        public float NamePreferredWidth;
        public float NameMinHeight;
        public float NamePreferredHeight;
        public float Spacing;
        public int PaddingHorizontal;
        public int PaddingVertical;

        public static ToolbarLayoutProfile Normal => new ToolbarLayoutProfile
        {
            IconButtonSize = 44f,
            SimControlPreferred = 88f,
            SimControlMin = 72f,
            NameMinWidth = 48f,
            NamePreferredWidth = 120f,
            NameMinHeight = 44f,
            NamePreferredHeight = 44f,
            Spacing = 4f,
            PaddingHorizontal = 8,
            PaddingVertical = 4
        };

        public static ToolbarLayoutProfile Compact => new ToolbarLayoutProfile
        {
            IconButtonSize = 36f,
            SimControlPreferred = 72f,
            SimControlMin = 60f,
            NameMinWidth = 36f,
            NamePreferredWidth = 96f,
            NameMinHeight = 40f,
            NamePreferredHeight = 40f,
            Spacing = 2f,
            PaddingHorizontal = 8,
            PaddingVertical = 4
        };

        public static ToolbarLayoutProfile Tight => new ToolbarLayoutProfile
        {
            IconButtonSize = 32f,
            SimControlPreferred = 60f,
            SimControlMin = 52f,
            NameMinWidth = 28f,
            NamePreferredWidth = 72f,
            NameMinHeight = 36f,
            NamePreferredHeight = 36f,
            Spacing = 2f,
            PaddingHorizontal = 4,
            PaddingVertical = 4
        };
    }

    const float BodyNameFontSizeMin = 9f;
    const float BodyNameFontSizeMax = 17f;
    const char ZeroWidthSpace = '\u200B';

    public IReadOnlyList<BodyNavigationOrder.NavigationEntry> NavigationEntries => _entries;
    public int CurrentIndex => _currentIndex;

    void Awake()
    {
        if (barRect == null)
            barRect = GetComponent<RectTransform>();

        EnsureToolbarButtonsInBar();
        EnsureTopBarLayout();
        ConfigureBodyNameText(bodyNameText);

        if (prevButton != null)
            prevButton.onClick.AddListener(OnPrevClicked);
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        EnsureBodyNameInputHandler();
    }

    void EnsureBodyNameInputHandler()
    {
        if (bodyNameButton == null)
            return;

        if (bodyNavigationPicker == null)
            bodyNavigationPicker = FindFirstObjectByType<BodyNavigationPickerController>(FindObjectsInactive.Include);

        if (bodyNameLongPressHandler == null)
        {
            if (!bodyNameButton.TryGetComponent(out bodyNameLongPressHandler))
                bodyNameLongPressHandler = bodyNameButton.gameObject.AddComponent<BodyNameLongPressHandler>();
        }

        bodyNameLongPressHandler.Configure(this);
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshLabel;
        CometMovementSettings.UseCometMovementChanged += OnCometMovementChanged;
        LookAtTarget.OnTargetChanged += OnTargetChanged;
        SimulationViewSettings.ShowSimulationUiChanged += OnShowSimulationUiChanged;
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshLabel;
        CometMovementSettings.UseCometMovementChanged -= OnCometMovementChanged;
        LookAtTarget.OnTargetChanged -= OnTargetChanged;
        SimulationViewSettings.ShowSimulationUiChanged -= OnShowSimulationUiChanged;

        if (_safeAreaRefreshRoutine != null)
        {
            StopCoroutine(_safeAreaRefreshRoutine);
            _safeAreaRefreshRoutine = null;
        }
    }

    void OnShowSimulationUiChanged(bool showSimulationUi)
    {
        RequestSafeAreaRefresh();
    }

    void Start()
    {
        RebuildNavigationList();
        StartCoroutine(LayoutAfterCanvasReady());
        StartCoroutine(InitializeWhenReady());
    }

    IEnumerator LayoutAfterCanvasReady()
    {
        yield return new WaitForEndOfFrame();
        ApplySafeAreaInset();
        NotifySidePanelLayoutChanged();
    }

    IEnumerator InitializeWhenReady()
    {
        while (!SimulationViewBootstrap.SystemsReady)
            yield return null;

        yield return null;

        _lookAtTarget = FindFirstObjectByType<LookAtTarget>();
        if (Camera.main != null)
            _showcaseCamera = Camera.main.GetComponent<BodyShowcaseCameraController>();

        RebuildNavigationList();
        EnsureToolbarButtonsInBar();
        ApplySafeAreaInset();
        NotifySidePanelLayoutChanged();
        SyncFromCurrentTarget();
    }

    void Update()
    {
        if (!_screenLayoutCached)
            return;

        if (Screen.width == _lastScreenWidth &&
            Screen.height == _lastScreenHeight &&
            Screen.safeArea == _lastSafeArea)
            return;

        RequestSafeAreaRefresh();
    }

    void RequestSafeAreaRefresh()
    {
        if (_safeAreaRefreshRoutine != null)
            StopCoroutine(_safeAreaRefreshRoutine);

        _safeAreaRefreshRoutine = StartCoroutine(RefreshSafeAreaAfterLayout());
    }

    IEnumerator RefreshSafeAreaAfterLayout()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        _safeAreaRefreshRoutine = null;
        ApplySafeAreaInset();
        NotifySidePanelLayoutChanged();
    }

    void EnsureTopBarLayout()
    {
        if (barRect == null)
            return;

        SidePanelUiBootstrap.ApplyBarRectLayout(barRect, 0f, 0f, 0f);
    }

    void EnsureToolbarButtonsInBar()
    {
        if (barRect == null)
            return;

        Canvas canvas = barRect.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        ConfigureNameButtonLayout(ToolbarLayoutProfile.Normal);
        ShareButtonUiBootstrap.EnsureShareButton(barRect);
        ApplyAdaptiveToolbarLayout();
    }

    void ConfigureNameButtonLayout(ToolbarLayoutProfile profile)
    {
        if (bodyNameButton == null)
            return;

        if (!bodyNameButton.TryGetComponent(out LayoutElement layoutElement))
            layoutElement = bodyNameButton.gameObject.AddComponent<LayoutElement>();

        layoutElement.minWidth = profile.NameMinWidth;
        layoutElement.preferredWidth = profile.NamePreferredWidth;
        layoutElement.flexibleWidth = 1f;
        layoutElement.minHeight = profile.NameMinHeight;
        layoutElement.preferredHeight = profile.NamePreferredHeight;
        layoutElement.flexibleHeight = 0f;

        ConfigureBodyNameText(bodyNameText);
    }

    static void ConfigureBodyNameText(TMP_Text text)
    {
        if (text == null)
            return;

        text.enableAutoSizing = true;
        text.fontSizeMin = BodyNameFontSizeMin;
        text.fontSizeMax = BodyNameFontSizeMax;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    void ApplyAdaptiveToolbarLayout()
    {
        if (barRect == null)
            return;

        if (_barLayoutGroup == null)
            _barLayoutGroup = barRect.GetComponent<HorizontalLayoutGroup>();

        Canvas canvas = barRect.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        bool cleanView = !SimulationViewSettings.ShowSimulationUi;
        if (_barLayoutGroup != null)
            _barLayoutGroup.childAlignment = cleanView ? TextAnchor.UpperRight : TextAnchor.MiddleCenter;

        float availableWidth = barRect.rect.width;
        if (_barLayoutGroup != null)
            availableWidth -= _barLayoutGroup.padding.horizontal;

        ToolbarLayoutProfile profile = SelectToolbarProfile(availableWidth, cleanView);
        ApplyToolbarProfile(profile, canvas);

        if (_barLayoutGroup != null)
        {
            _barLayoutGroup.spacing = profile.Spacing;
            _barLayoutGroup.padding = new RectOffset(
                profile.PaddingHorizontal,
                profile.PaddingHorizontal,
                profile.PaddingVertical,
                profile.PaddingVertical);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(barRect);
    }

    ToolbarLayoutProfile SelectToolbarProfile(float availableWidth, bool cleanView)
    {
        if (cleanView || availableWidth <= 0f)
            return ToolbarLayoutProfile.Normal;

        ToolbarLayoutProfile[] profiles =
        {
            ToolbarLayoutProfile.Normal,
            ToolbarLayoutProfile.Compact,
            ToolbarLayoutProfile.Tight
        };

        for (int i = 0; i < profiles.Length; i++)
        {
            if (EstimateRequiredWidth(profiles[i]) <= availableWidth)
                return profiles[i];
        }

        return ToolbarLayoutProfile.Tight;
    }

    float EstimateRequiredWidth(ToolbarLayoutProfile profile)
    {
        int activeCount = 0;
        float width = profile.PaddingHorizontal * 2f;

        if (IsBarChildActive(prevButton))
        {
            width += profile.IconButtonSize;
            activeCount++;
        }

        if (IsBarChildActive(bodyNameButton))
        {
            width += profile.NameMinWidth;
            activeCount++;
        }

        if (IsBarChildActive(nextButton))
        {
            width += profile.IconButtonSize;
            activeCount++;
        }

        Transform menuButton = FindUiTransform(barRect, "SidePanelMenuButton");
        if (IsBarChildActive(menuButton))
        {
            width += profile.IconButtonSize;
            activeCount++;
        }

        Transform shareButton = FindUiTransform(barRect, "ShareButton");
        if (IsBarChildActive(shareButton))
        {
            width += profile.IconButtonSize;
            activeCount++;
        }

        Transform simControlButton = FindUiTransform(barRect, "SimulationControlButton");
        if (IsBarChildActive(simControlButton))
        {
            width += profile.SimControlMin;
            activeCount++;
        }

        if (activeCount > 1)
            width += profile.Spacing * (activeCount - 1);

        return width;
    }

    void ApplyToolbarProfile(ToolbarLayoutProfile profile, Canvas canvas)
    {
        ConfigureNameButtonLayout(profile);
        EnsureBarChildButton(FindUiTransform(canvas.transform, "SidePanelMenuButton"), 3, profile.IconButtonSize, profile.IconButtonSize);
        EnsureBarChildButton(FindUiTransform(canvas.transform, "ShareButton"), 4, profile.IconButtonSize, profile.IconButtonSize);
        EnsureBarChildButton(
            FindUiTransform(canvas.transform, "SimulationControlButton"),
            5,
            profile.SimControlPreferred,
            profile.IconButtonSize,
            profile.SimControlMin);

        if (prevButton != null)
            ApplyIconButtonSize(prevButton.transform, profile.IconButtonSize);
        if (nextButton != null)
            ApplyIconButtonSize(nextButton.transform, profile.IconButtonSize);
    }

    static void ApplyIconButtonSize(Transform button, float size)
    {
        if (button == null || !button.TryGetComponent(out LayoutElement layoutElement))
            return;

        layoutElement.minWidth = size;
        layoutElement.preferredWidth = size;
        layoutElement.minHeight = size;
        layoutElement.preferredHeight = size;
    }

    static bool IsBarChildActive(Component component)
    {
        return component != null && component.gameObject.activeInHierarchy;
    }

    static bool IsBarChildActive(Transform transform)
    {
        return transform != null && transform.gameObject.activeInHierarchy;
    }

    void ConfigureNameButtonLayout()
    {
        ConfigureNameButtonLayout(ToolbarLayoutProfile.Normal);
    }

    static void EnsureBarChildButton(Transform button, int siblingIndex, float preferredWidth, float preferredHeight, float minWidth = -1f)
    {
        if (button == null)
            return;

        Transform bar = button.parent;
        while (bar != null && bar.name != "BodyNavigationBar")
            bar = bar.parent;

        if (bar == null)
            bar = button.root.Find("BodyNavigationBar");

        if (bar == null)
            return;

        if (button.parent != bar)
            button.SetParent(bar, false);

        button.SetSiblingIndex(siblingIndex);

        if (!button.TryGetComponent(out RectTransform rect))
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;

        if (!button.TryGetComponent(out LayoutElement layoutElement))
            layoutElement = button.gameObject.AddComponent<LayoutElement>();

        float resolvedMinWidth = minWidth > 0f ? minWidth : preferredWidth;
        layoutElement.minWidth = resolvedMinWidth;
        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredWidth = preferredWidth;
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        if (button.TryGetComponent(out Animator animator))
            animator.applyRootMotion = false;
    }

    static Transform FindUiTransform(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        Transform direct = root.Find(objectName);
        if (direct != null)
            return direct;

        Transform inBar = root.Find("BodyNavigationBar/" + objectName);
        if (inBar != null)
            return inBar;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform nested = FindUiTransform(root.GetChild(i), objectName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    void OnCometMovementChanged(bool enabled)
    {
        RebuildNavigationList();
        SyncFromCurrentTarget();
    }

    void OnTargetChanged(GameObject target)
    {
        if (_suppressTargetSync)
            return;

        SyncFromCurrentTarget(target);
    }

    void RebuildNavigationList()
    {
        _entries = BodyNavigationOrder.BuildNavigationList();
        UpdateButtonInteractable();
    }

    void SyncFromCurrentTarget()
    {
        GameObject target = _lookAtTarget != null ? _lookAtTarget.currentTarget : null;
        SyncFromCurrentTarget(target);
    }

    void SyncFromCurrentTarget(GameObject target)
    {
        if (target == null)
            return;

        int index = BodyNavigationOrder.FindIndexForObject(_entries, target);
        if (index < 0)
        {
            RebuildNavigationList();
            index = BodyNavigationOrder.FindIndexForObject(_entries, target);
        }

        _currentIndex = index;
        RefreshLabel();
        UpdateButtonInteractable();
    }

    void OnPrevClicked()
    {
        PlayClickSound();
        NavigateRelative(-1);
    }

    void OnNextClicked()
    {
        PlayClickSound();
        NavigateRelative(1);
    }

    public void OnBodyNameShortClicked()
    {
        PlayClickSound();
        if (_currentIndex >= 0 && _currentIndex < _entries.Count)
            NavigateToEntry(_entries[_currentIndex], showDescription: true);
    }

    public bool EnsureNavigationReady()
    {
        if (_entries.Count == 0)
            RebuildNavigationList();

        if (_lookAtTarget == null)
            _lookAtTarget = FindFirstObjectByType<LookAtTarget>();

        if (_currentIndex < 0 && _lookAtTarget != null)
            SyncFromCurrentTarget();

        if (_currentIndex < 0 && _entries.Count > 0)
            _currentIndex = 0;

        UpdateButtonInteractable();
        return _entries.Count > 0;
    }

    public bool TryOpenBodyPicker()
    {
        if (!EnsureNavigationReady())
            return false;

        if (bodyNavigationPicker == null)
            bodyNavigationPicker = FindFirstObjectByType<BodyNavigationPickerController>(FindObjectsInactive.Include);

        if (bodyNavigationPicker == null)
            return false;

        bodyNavigationPicker.Toggle(NavigationEntries, CurrentIndex);
        return true;
    }

    public void NavigateToEntryByIndex(int index, bool showDescription)
    {
        if (index < 0 || index >= _entries.Count)
            return;

        _currentIndex = index;
        NavigateToEntry(_entries[index], showDescription);
    }

    void NavigateRelative(int delta)
    {
        if (_entries.Count == 0)
            return;

        if (_currentIndex < 0)
            _currentIndex = 0;
        else
            _currentIndex = BodyNavigationOrder.WrapIndex(_currentIndex + delta, _entries.Count);

        NavigateToEntry(_entries[_currentIndex], showDescription: false);
    }

    void NavigateToEntry(BodyNavigationOrder.NavigationEntry entry, bool showDescription)
    {
        if (entry.sceneObject == null)
            return;

        if (_lookAtTarget == null)
            _lookAtTarget = FindFirstObjectByType<LookAtTarget>();

        if (_showcaseCamera == null && Camera.main != null)
            _showcaseCamera = Camera.main.GetComponent<BodyShowcaseCameraController>();

        _suppressTargetSync = true;

        if (entry.kind == BodyNavigationOrder.EntryKind.Comet)
            _lookAtTarget.FocusComet(entry.sceneObject, showDescription);
        else
            _lookAtTarget.FocusPlanet(entry.objectName, useDetailCamera: false, showDescription);

        _currentIndex = BodyNavigationOrder.FindIndexForObject(_entries, entry.sceneObject);
        RefreshLabel();
        UpdateButtonInteractable();

        _suppressTargetSync = false;

        if (_showcaseCamera != null)
            _showcaseCamera.StartShowcase(entry.sceneObject.transform);
    }

    void RefreshLabel()
    {
        if (bodyNameText == null)
            return;

        if (_currentIndex < 0 || _currentIndex >= _entries.Count)
        {
            bodyNameText.text = string.Empty;
            return;
        }

        BodyNavigationOrder.NavigationEntry entry = _entries[_currentIndex];
        string label = entry.objectName;

        if (LocalizationManager.Instance != null)
        {
            string localized = LocalizationManager.Instance.GetTranslation(entry.labelKey);
            if (!string.IsNullOrEmpty(localized))
                label = localized;
        }

        bodyNameText.text = InsertSoftBreaks(label);
    }

    /// <summary>
    /// Inserts zero-width spaces after '/' and em/en dashes so TMP can wrap long comet names
    /// (e.g. 45P/Хонда—Мркос—Пайдушакова) without changing visible glyphs.
    /// </summary>
    static string InsertSoftBreaks(string label)
    {
        if (string.IsNullOrEmpty(label))
            return label;

        var sb = new System.Text.StringBuilder(label.Length + 4);
        for (int i = 0; i < label.Length; i++)
        {
            char c = label[i];
            sb.Append(c);
            if (c == '/' || c == '\u2014' || c == '\u2013')
            {
                if (i + 1 < label.Length && label[i + 1] != ZeroWidthSpace)
                    sb.Append(ZeroWidthSpace);
            }
        }

        return sb.ToString();
    }

    void UpdateButtonInteractable()
    {
        bool canNavigate = _entries.Count > 1;
        if (prevButton != null)
            prevButton.interactable = canNavigate;
        if (nextButton != null)
            nextButton.interactable = canNavigate;
        if (bodyNameButton != null)
            bodyNameButton.interactable = _entries.Count > 0;
    }

    void ApplySafeAreaInset()
    {
        if (barRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        Canvas canvas = barRect.GetComponentInParent<Canvas>();
        SafeAreaInsets.GetCanvasInsets(canvas, out float left, out float right, out float top, out _);

        SidePanelUiBootstrap.ApplyBarRectLayout(barRect, left, right, top);
        LayoutRebuilder.ForceRebuildLayoutImmediate(barRect);
        ApplyAdaptiveToolbarLayout();
        CacheScreenLayoutState();
    }

    void NotifySidePanelLayoutChanged()
    {
        if (_sidePanelController == null)
            _sidePanelController = FindFirstObjectByType<SimulationSidePanelController>();

        if (_sidePanelController != null)
            _sidePanelController.RefreshSafeAreaLayout();
    }

    void CacheScreenLayoutState()
    {
        _lastSafeArea = Screen.safeArea;
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
        _screenLayoutCached = true;
    }

    static void PlayClickSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound();
    }
}
