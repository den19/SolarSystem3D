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

    public IReadOnlyList<BodyNavigationOrder.NavigationEntry> NavigationEntries => _entries;
    public int CurrentIndex => _currentIndex;

    void Awake()
    {
        if (barRect == null)
            barRect = GetComponent<RectTransform>();

        EnsureToolbarButtonsInBar();
        EnsureTopBarLayout();

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
            bodyNavigationPicker = FindFirstObjectByType<BodyNavigationPickerController>();

        if (bodyNameLongPressHandler == null)
        {
            if (!bodyNameButton.TryGetComponent(out bodyNameLongPressHandler))
                bodyNameLongPressHandler = bodyNameButton.gameObject.AddComponent<BodyNameLongPressHandler>();
        }

        bodyNameLongPressHandler.Configure(this, bodyNavigationPicker);
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshLabel;
        CometMovementSettings.UseCometMovementChanged += OnCometMovementChanged;
        LookAtTarget.OnTargetChanged += OnTargetChanged;
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshLabel;
        CometMovementSettings.UseCometMovementChanged -= OnCometMovementChanged;
        LookAtTarget.OnTargetChanged -= OnTargetChanged;

        if (_safeAreaRefreshRoutine != null)
        {
            StopCoroutine(_safeAreaRefreshRoutine);
            _safeAreaRefreshRoutine = null;
        }
    }

    void Start()
    {
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

        ConfigureNameButtonLayout();
        EnsureBarChildButton(FindUiTransform(canvas.transform, "SidePanelMenuButton"), 3, 44f, 44f);
        EnsureBarChildButton(FindUiTransform(canvas.transform, "SimulationControlButton"), 4, 88f, 44f, 72f);
    }

    void ConfigureNameButtonLayout()
    {
        if (bodyNameButton == null)
            return;

        if (!bodyNameButton.TryGetComponent(out LayoutElement layoutElement))
            layoutElement = bodyNameButton.gameObject.AddComponent<LayoutElement>();

        layoutElement.minWidth = 48f;
        layoutElement.preferredWidth = 120f;
        layoutElement.flexibleWidth = 1f;
        layoutElement.minHeight = 40f;
        layoutElement.preferredHeight = 40f;
        layoutElement.flexibleHeight = 0f;
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

        bodyNameText.text = label;
    }

    void UpdateButtonInteractable()
    {
        bool canNavigate = _entries.Count > 1;
        if (prevButton != null)
            prevButton.interactable = canNavigate;
        if (nextButton != null)
            nextButton.interactable = canNavigate;
        if (bodyNameButton != null)
            bodyNameButton.interactable = _currentIndex >= 0 && _currentIndex < _entries.Count;
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
