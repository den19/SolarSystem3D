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

    LookAtTarget _lookAtTarget;
    BodyShowcaseCameraController _showcaseCamera;
    List<BodyNavigationOrder.NavigationEntry> _entries = new List<BodyNavigationOrder.NavigationEntry>();
    int _currentIndex = -1;
    bool _suppressTargetSync;
    Rect _lastSafeArea;
    bool _lastIsLandscape;
    const float BaseTopOffset = -14f;
    const float HorizontalMargin = 8f;
    const float BarHeight = 48f;

    void Awake()
    {
        if (barRect == null)
            barRect = GetComponent<RectTransform>();

        EnsureToolbarButtonsInBar();

        if (prevButton != null)
            prevButton.onClick.AddListener(OnPrevClicked);
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
        if (bodyNameButton != null)
            bodyNameButton.onClick.AddListener(OnBodyNameClicked);
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
    }

    void Start()
    {
        StartCoroutine(InitializeWhenReady());
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
        EnsureTopBarLayout();
        ApplySafeAreaInset();
        SyncFromCurrentTarget();
    }

    void Update()
    {
        bool landscape = Screen.width > Screen.height;
        if (Screen.safeArea != _lastSafeArea || landscape != _lastIsLandscape)
            ApplySafeAreaInset();
    }

    void EnsureTopBarLayout()
    {
        if (barRect == null)
            return;

        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.sizeDelta = new Vector2(0f, BarHeight);
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

    void OnBodyNameClicked()
    {
        PlayClickSound();
        if (_currentIndex >= 0 && _currentIndex < _entries.Count)
            NavigateToEntry(_entries[_currentIndex], showDescription: true);
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

        _lastSafeArea = Screen.safeArea;
        _lastIsLandscape = Screen.width > Screen.height;

        Canvas canvas = barRect.GetComponentInParent<Canvas>();
        SafeAreaInsets.GetCanvasInsets(canvas, out float left, out float right, out float top, out _);

        barRect.offsetMin = new Vector2(left + HorizontalMargin, barRect.offsetMin.y);
        barRect.offsetMax = new Vector2(-(right + HorizontalMargin), barRect.offsetMax.y);
        barRect.sizeDelta = new Vector2(0f, BarHeight);
        barRect.anchoredPosition = new Vector2(0f, BaseTopOffset - top);

        LayoutRebuilder.ForceRebuildLayoutImmediate(barRect);
    }

    static void PlayClickSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound();
    }
}
