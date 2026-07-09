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

    void Awake()
    {
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
        SyncFromCurrentTarget();
        ApplySafeAreaInset();
    }

    void Update()
    {
        ApplySafeAreaInset();
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
            NavigateToEntry(_entries[_currentIndex]);
    }

    void NavigateRelative(int delta)
    {
        if (_entries.Count == 0)
            return;

        if (_currentIndex < 0)
            _currentIndex = 0;
        else
            _currentIndex = BodyNavigationOrder.WrapIndex(_currentIndex + delta, _entries.Count);

        NavigateToEntry(_entries[_currentIndex]);
    }

    void NavigateToEntry(BodyNavigationOrder.NavigationEntry entry)
    {
        if (entry.sceneObject == null)
            return;

        if (_lookAtTarget == null)
            _lookAtTarget = FindFirstObjectByType<LookAtTarget>();

        if (_showcaseCamera == null && Camera.main != null)
            _showcaseCamera = Camera.main.GetComponent<BodyShowcaseCameraController>();

        _suppressTargetSync = true;

        if (entry.kind == BodyNavigationOrder.EntryKind.Comet)
            _lookAtTarget.FocusComet(entry.sceneObject, showDescription: true);
        else
            _lookAtTarget.FocusPlanet(entry.objectName, useDetailCamera: false, showDescription: true);

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

        Rect safe = Screen.safeArea;
        float topInset = Screen.height - safe.yMax;
        if (topInset > 0f)
            barRect.anchoredPosition = new Vector2(barRect.anchoredPosition.x, -14f - topInset * 0.5f);
    }

    static void PlayClickSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound();
    }
}
