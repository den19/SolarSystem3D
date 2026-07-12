using System.Collections;
using System.Collections.Generic;
using SolarSystemApp;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Scene-wired scrollable picker for celestial body navigation.
/// </summary>
public class BodyNavigationPickerController : MonoBehaviour
{
    const float RowHeight = 48f;
    const float RowSpacing = 2f;
    const float ContentPaddingTop = 6f;
    const float PanelGapBelowBar = 4f;
    const float PortraitMaxHeightRatio = 0.42f;
    const float LandscapeMaxHeightRatio = 0.55f;

    static readonly Color NormalRowColor = new Color(0.08f, 0.11f, 0.18f, 0.80f);
    static readonly Color SelectedRowColor = new Color(0.20f, 0.30f, 0.45f, 0.95f);
    static readonly Color NormalTextColor = new Color(0.78f, 0.85f, 0.95f, 1f);
    static readonly Color SelectedTextColor = new Color(0.92f, 0.95f, 1f, 1f);
    static readonly Color SelectionStripeColor = new Color(0.55f, 0.72f, 1f, 1f);

    [SerializeField] GameObject panelRoot;
    [SerializeField] RectTransform backdropRect;
    [SerializeField] RectTransform panelRect;
    [SerializeField] Button backdropButton;
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] RectTransform contentRoot;
    [SerializeField] RectTransform itemRowTemplate;
    [SerializeField] BodyNavigationController bodyNavigationController;

    readonly List<RectTransform> _rowInstances = new List<RectTransform>();
    List<BodyNavigationOrder.NavigationEntry> _entries = new List<BodyNavigationOrder.NavigationEntry>();
    int _currentIndex = -1;
    bool _isOpen;
    Canvas _canvas;
    RectTransform _navigationBarRect;
    Rect _lastSafeArea;
    int _lastScreenWidth;
    int _lastScreenHeight;

    public bool IsOpen => _isOpen;

    void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        _canvas = GetComponentInParent<Canvas>();
        HideImmediate();

        if (backdropButton != null)
        {
            backdropButton.onClick.RemoveListener(Hide);
            backdropButton.onClick.AddListener(Hide);
        }

        if (backdropRect == null)
        {
            Transform backdrop = transform.Find("Backdrop");
            if (backdrop != null)
                backdropRect = backdrop.GetComponent<RectTransform>();
        }

        if (bodyNavigationController == null)
            bodyNavigationController = FindFirstObjectByType<BodyNavigationController>();

        if (itemRowTemplate != null)
            itemRowTemplate.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        CometMovementSettings.UseCometMovementChanged += OnCometMovementChanged;
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        CometMovementSettings.UseCometMovementChanged -= OnCometMovementChanged;
    }

    void Update()
    {
        if (!_isOpen)
            return;

        if (Screen.width == _lastScreenWidth &&
            Screen.height == _lastScreenHeight &&
            Screen.safeArea == _lastSafeArea)
            return;

        RefreshLayout();
    }

    void OnLanguageChanged()
    {
        if (_isOpen)
            RebuildRows();
    }

    void OnCometMovementChanged(bool enabled)
    {
        if (!_isOpen)
            return;

        _entries = BodyNavigationOrder.BuildNavigationList();
        if (bodyNavigationController != null)
            _currentIndex = bodyNavigationController.CurrentIndex;

        RebuildRows();
    }

    public void Show(IReadOnlyList<BodyNavigationOrder.NavigationEntry> entries, int currentIndex)
    {
        if (entries == null || entries.Count == 0)
            return;

        if (currentIndex < 0 || currentIndex >= entries.Count)
            currentIndex = 0;

        _entries = new List<BodyNavigationOrder.NavigationEntry>(entries);
        _currentIndex = currentIndex;
        _isOpen = true;

        if (panelRoot == null)
            panelRoot = gameObject;

        gameObject.SetActive(true);
        panelRoot.SetActive(true);

        if (bodyNavigationController == null)
            bodyNavigationController = FindFirstObjectByType<BodyNavigationController>();

        CacheNavigationBarRect();
        RefreshLayout();
        RebuildRows();
        StartScrollAfterLayout();
    }

    public void Hide()
    {
        _isOpen = false;
        HideImmediate();
    }

    public void HideImmediate()
    {
        ClearRows();
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Toggle(IReadOnlyList<BodyNavigationOrder.NavigationEntry> entries, int currentIndex)
    {
        if (_isOpen)
            Hide();
        else
            Show(entries, currentIndex);
    }

    void CacheNavigationBarRect()
    {
        if (_navigationBarRect != null)
            return;

        Transform canvasTransform = _canvas != null ? _canvas.transform : null;
        if (canvasTransform == null)
            return;

        Transform bar = canvasTransform.Find("BodyNavigationBar");
        if (bar != null)
            _navigationBarRect = bar.GetComponent<RectTransform>();
    }

    void RefreshLayout()
    {
        if (panelRect == null || _canvas == null)
            return;

        Canvas.ForceUpdateCanvases();
        SafeAreaInsets.GetCanvasInsets(_canvas, out float left, out float right, out float top, out _);

        float horizontalMargin = SidePanelUiBootstrap.BarHorizontalMargin;
        float topInset = top + SidePanelUiBootstrap.BarTopMargin;
        float barHeight = SidePanelUiBootstrap.BarHeight;
        float panelTop = topInset + barHeight + PanelGapBelowBar;

        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.offsetMin = new Vector2(left + horizontalMargin, 0f);
        panelRect.offsetMax = new Vector2(-(right + horizontalMargin), 0f);

        var canvasRect = _canvas.GetComponent<RectTransform>();
        float canvasHeight = canvasRect != null ? canvasRect.rect.height : Screen.height;
        bool landscape = Screen.width > Screen.height;
        float maxHeightRatio = landscape ? LandscapeMaxHeightRatio : PortraitMaxHeightRatio;
        float maxHeight = canvasHeight * maxHeightRatio;
        float availableBelowBar = canvasHeight - panelTop - horizontalMargin;
        float panelHeight = Mathf.Clamp(Mathf.Min(maxHeight, availableBelowBar), RowHeight * 3f, maxHeight);
        panelRect.sizeDelta = new Vector2(0f, panelHeight);
        panelRect.anchoredPosition = new Vector2(0f, -panelTop);

        if (backdropRect != null)
        {
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = new Vector2(0f, -panelTop);
        }

        _lastSafeArea = Screen.safeArea;
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
    }

    void RebuildRows()
    {
        ClearRows();
        if (itemRowTemplate == null || contentRoot == null)
            return;

        ApplyRowFonts();

        for (int i = 0; i < _entries.Count; i++)
        {
            RectTransform row = Instantiate(itemRowTemplate, contentRoot);
            row.gameObject.SetActive(true);
            row.name = $"ItemRow_{_entries[i].objectName}";
            ConfigureRow(row, _entries[i], i);
            _rowInstances.Add(row);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    void ConfigureRow(RectTransform row, BodyNavigationOrder.NavigationEntry entry, int index)
    {
        bool selected = index == _currentIndex;

        Transform background = row.Find("RowBackground");
        if (background != null && background.TryGetComponent(out Image backgroundImage))
        {
            backgroundImage.color = selected ? SelectedRowColor : NormalRowColor;
            backgroundImage.raycastTarget = true;
        }

        Transform stripe = row.Find("SelectionStripe");
        if (stripe != null)
            stripe.gameObject.SetActive(selected);

        Transform labelTransform = row.Find("Label");
        if (labelTransform != null && labelTransform.TryGetComponent(out TMP_Text label))
        {
            Language lang = LocalizationManager.CurrentLanguage;
            TMP_FontAsset font = LocalizationFontHelper.GetFontForLanguage(lang);
            if (font != null)
                label.font = font;

            Material overlay = LocalizationFontHelper.GetOverlayMaterialForLanguage(lang);
            if (overlay != null)
                label.fontSharedMaterial = overlay;

            label.text = GetLocalizedLabel(entry);
            label.color = selected ? SelectedTextColor : NormalTextColor;
        }

        Transform thumbnailTransform = row.Find("Thumbnail");
        if (thumbnailTransform != null && thumbnailTransform.TryGetComponent(out RawImage thumbnail))
            thumbnail.texture = BodyNavigationThumbnailCatalog.GetThumbnail(entry.objectName);

        if (!row.TryGetComponent(out Button rowButton))
            rowButton = row.gameObject.AddComponent<Button>();

        rowButton.transition = Selectable.Transition.ColorTint;
        var colors = rowButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.78f, 0.88f, 1f, 0.35f);
        colors.pressedColor = new Color(0.65f, 0.78f, 0.95f, 0.55f);
        colors.selectedColor = colors.highlightedColor;
        rowButton.colors = colors;

        if (background != null && background.TryGetComponent(out Image targetGraphic))
            rowButton.targetGraphic = targetGraphic;

        int capturedIndex = index;

        if (!row.TryGetComponent(out BodyPickerRowClickHandler clickHandler))
            clickHandler = row.gameObject.AddComponent<BodyPickerRowClickHandler>();
        clickHandler.Configure(this, capturedIndex);
    }

    internal void OnRowClicked(int index)
    {
        if (bodyNavigationController == null)
            bodyNavigationController = FindFirstObjectByType<BodyNavigationController>();

        if (bodyNavigationController != null)
            bodyNavigationController.NavigateToEntryByIndex(index, showDescription: false);

        PlayClickSound();
        Hide();
    }

    void ApplyRowFonts()
    {
        if (itemRowTemplate == null)
            return;

        Transform label = itemRowTemplate.Find("Label");
        if (label == null || !label.TryGetComponent(out TMP_Text templateLabel))
            return;

        Language lang = LocalizationManager.CurrentLanguage;
        TMP_FontAsset font = LocalizationFontHelper.GetFontForLanguage(lang);
        if (font != null)
            templateLabel.font = font;

        Material overlay = LocalizationFontHelper.GetOverlayMaterialForLanguage(lang);
        if (overlay != null)
            templateLabel.fontSharedMaterial = overlay;
    }

    static string GetLocalizedLabel(BodyNavigationOrder.NavigationEntry entry)
    {
        string label = entry.objectName;
        if (LocalizationManager.Instance != null)
        {
            string localized = LocalizationManager.Instance.GetTranslation(entry.labelKey);
            if (!string.IsNullOrEmpty(localized))
                label = localized;
        }

        return label;
    }

    void StartScrollAfterLayout()
    {
        ScrollToCurrentNow();

        if (bodyNavigationController != null && bodyNavigationController.isActiveAndEnabled)
            bodyNavigationController.StartCoroutine(ScrollToCurrentAfterLayout());
        else if (isActiveAndEnabled)
            StartCoroutine(ScrollToCurrentAfterLayout());
    }

    void ScrollToCurrentNow()
    {
        if (scrollRect == null || contentRoot == null || _currentIndex < 0)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        ApplyScrollToCurrentIndex();
    }

    void ApplyScrollToCurrentIndex()
    {
        if (scrollRect == null || contentRoot == null || _currentIndex < 0)
            return;

        float contentHeight = contentRoot.rect.height;
        RectTransform viewport = scrollRect.viewport;
        float viewportHeight = viewport != null ? viewport.rect.height : 0f;
        if (contentHeight <= viewportHeight || viewportHeight <= 0f)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            return;
        }

        float rowTop = ContentPaddingTop + _currentIndex * (RowHeight + RowSpacing);
        float rowCenter = rowTop + RowHeight * 0.5f;
        float scrollRange = contentHeight - viewportHeight;
        float target = Mathf.Clamp(rowCenter - viewportHeight * 0.5f, 0f, scrollRange);
        scrollRect.verticalNormalizedPosition = 1f - target / scrollRange;
    }

    IEnumerator ScrollToCurrentAfterLayout()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        if (scrollRect == null || contentRoot == null || _currentIndex < 0)
            yield break;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        ApplyScrollToCurrentIndex();
    }

    void ClearRows()
    {
        for (int i = 0; i < _rowInstances.Count; i++)
        {
            if (_rowInstances[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(_rowInstances[i].gameObject);
                else
                    DestroyImmediate(_rowInstances[i].gameObject);
            }
        }

        _rowInstances.Clear();
    }

    static void PlayClickSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound();
    }
}

/// <summary>
/// Reliable row tap inside ScrollRect (Button alone misses when raycastTarget was off or drag steals input).
/// </summary>
sealed class BodyPickerRowClickHandler : MonoBehaviour, IPointerClickHandler
{
    BodyNavigationPickerController _picker;
    int _index;

    public void Configure(BodyNavigationPickerController picker, int index)
    {
        _picker = picker;
        _index = index;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_picker == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        _picker.OnRowClicked(_index);
    }
}
