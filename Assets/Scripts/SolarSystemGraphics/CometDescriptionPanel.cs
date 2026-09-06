using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single dynamic overlay for comet / asteroid encyclopedia text in the current language.
/// </summary>
public class CometDescriptionPanel : MonoBehaviour
{
    public static CometDescriptionPanel Instance { get; private set; }

    [SerializeField] GameObject panelRoot;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text orbitElementsText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] ScrollRect bodyScroll;
    [SerializeField] Button closeButton;

    CometInfo _currentComet;
    AsteroidInfo _currentAsteroid;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (panelRoot == null)
            panelRoot = gameObject;

        HideImmediate();
    }

    void Start()
    {
        WireCloseButton();
    }

    void WireCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(Hide);
        closeButton.onClick.AddListener(Hide);
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnLanguageChanged()
    {
        if ((_currentComet != null || _currentAsteroid != null) && panelRoot.activeSelf)
            RefreshText();
    }

    public void Show(CometInfo info)
    {
        if (info == null)
            return;

        _currentComet = info;
        _currentAsteroid = null;
        EnsureOrbitElementsRow();
        panelRoot.SetActive(true);
        RefreshText();
    }

    public void Show(AsteroidInfo info)
    {
        if (info == null)
            return;

        _currentAsteroid = info;
        _currentComet = null;
        EnsureOrbitElementsRow();
        panelRoot.SetActive(true);
        RefreshText();
    }

    public void Hide()
    {
        _currentComet = null;
        _currentAsteroid = null;
        HideImmediate();
    }

    public void HideImmediate()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public bool IsOpenFor(Transform body)
    {
        if (body == null || panelRoot == null || !panelRoot.activeInHierarchy)
            return false;

        string objectName = GetCurrentObjectName();
        if (string.IsNullOrEmpty(objectName))
            return false;

        for (Transform t = body; t != null; t = t.parent)
        {
            if (t.name == objectName)
                return true;
        }

        return false;
    }

    string GetCurrentObjectName()
    {
        if (_currentComet != null)
        {
            if (!string.IsNullOrEmpty(_currentComet.CometId))
                return _currentComet.CometId;
            return _currentComet.gameObject != null ? _currentComet.gameObject.name : null;
        }

        if (_currentAsteroid != null)
        {
            if (!string.IsNullOrEmpty(_currentAsteroid.AsteroidId))
                return _currentAsteroid.AsteroidId;
            return _currentAsteroid.gameObject != null ? _currentAsteroid.gameObject.name : null;
        }

        return null;
    }

    void RefreshText()
    {
        string title = null;
        string body = null;
        Language lang = LocalizationManager.CurrentLanguage;

        if (_currentComet != null)
        {
            title = _currentComet.GetTitle();
            body = _currentComet.GetDescription(lang);
        }
        else if (_currentAsteroid != null)
        {
            title = _currentAsteroid.GetTitle();
            body = _currentAsteroid.GetDescription(lang);
        }
        else
        {
            return;
        }

        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = body;

        RefreshOrbitElements();

        if (bodyText != null && bodyScroll != null && bodyScroll.content != null)
        {
            bodyText.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(bodyScroll.content);
            bodyScroll.verticalNormalizedPosition = 1f;
        }
    }

    void RefreshOrbitElements()
    {
        if (orbitElementsText == null)
            return;

        string objectName = GetCurrentObjectName();
        if (OrbitElementsDisplay.TryFormat(objectName, out string text))
        {
            orbitElementsText.gameObject.SetActive(true);
            OrbitElementsDisplay.ConfigureCardTmp(orbitElementsText as TextMeshProUGUI);
            orbitElementsText.text = text;
        }
        else
        {
            orbitElementsText.gameObject.SetActive(false);
        }
    }

    void EnsureOrbitElementsRow()
    {
        if (orbitElementsText != null)
            return;

        Transform card = panelRoot != null ? panelRoot.transform.Find("Card") : null;
        if (card == null)
            return;

        var existing = card.Find("OrbitElements");
        if (existing != null)
        {
            orbitElementsText = existing.GetComponent<TMP_Text>();
            return;
        }

        var titleRect = titleText != null ? titleText.rectTransform : null;
        var scrollRectTransform = bodyScroll != null ? bodyScroll.transform as RectTransform : null;
        var closeRect = closeButton != null ? closeButton.transform as RectTransform : null;

        if (titleRect != null)
        {
            titleRect.anchorMin = new Vector2(0.04f, 0.88f);
            titleRect.anchorMax = new Vector2(0.82f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
        }

        if (closeRect != null)
        {
            closeRect.anchorMin = new Vector2(0.86f, 0.88f);
            closeRect.anchorMax = new Vector2(0.98f, 0.98f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
        }

        if (scrollRectTransform != null)
        {
            scrollRectTransform.anchorMin = new Vector2(0.04f, 0.04f);
            scrollRectTransform.anchorMax = new Vector2(0.96f, 0.78f);
            scrollRectTransform.offsetMin = Vector2.zero;
            scrollRectTransform.offsetMax = Vector2.zero;
        }

        var orbitGo = new GameObject("OrbitElements");
        orbitGo.transform.SetParent(card, false);
        var orbitRect = orbitGo.AddComponent<RectTransform>();
        orbitRect.anchorMin = new Vector2(0.04f, 0.80f);
        orbitRect.anchorMax = new Vector2(0.96f, 0.87f);
        orbitRect.offsetMin = Vector2.zero;
        orbitRect.offsetMax = Vector2.zero;

        var tmp = orbitGo.AddComponent<TextMeshProUGUI>();
        OrbitElementsDisplay.ConfigureCardTmp(tmp);
        tmp.alignment = TextAlignmentOptions.Left;
        orbitElementsText = tmp;

        if (titleText != null)
            orbitGo.transform.SetSiblingIndex(titleText.transform.GetSiblingIndex() + 1);
    }

    public static CometDescriptionPanel EnsureOnCanvas(Transform canvasTransform)
    {
        if (Instance != null)
            return Instance;

        var existing = canvasTransform.Find("CometDescriptionPanel");
        if (existing != null)
        {
            var panel = existing.GetComponent<CometDescriptionPanel>();
            if (panel != null)
                return panel;
        }

        return BuildDefaultPanel(canvasTransform);
    }

    static CometDescriptionPanel BuildDefaultPanel(Transform canvasTransform)
    {
        var rootGo = new GameObject("CometDescriptionPanel");
        rootGo.transform.SetParent(canvasTransform, false);

        var rootRect = rootGo.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var dimmer = rootGo.AddComponent<Image>();
        dimmer.color = new Color(0f, 0f, 0f, 0.45f);
        dimmer.raycastTarget = true;

        var panel = rootGo.AddComponent<CometDescriptionPanel>();
        panel.panelRoot = rootGo;

        var cardGo = new GameObject("Card");
        cardGo.transform.SetParent(rootGo.transform, false);
        var cardRect = cardGo.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.08f, 0.12f);
        cardRect.anchorMax = new Vector2(0.92f, 0.88f);
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;

        var cardImage = cardGo.AddComponent<Image>();
        cardImage.color = new Color(0.06f, 0.08f, 0.14f, 0.92f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(cardGo.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.04f, 0.88f);
        titleRect.anchorMax = new Vector2(0.82f, 0.98f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        panel.titleText = titleGo.AddComponent<TextMeshProUGUI>();
        // Match planet encyclopedia panels (BodyDescription / *Header ≈ 40, *Content ≈ 28).
        panel.titleText.fontSize = 40;
        panel.titleText.fontSizeMin = 18;
        panel.titleText.fontSizeMax = 40;
        panel.titleText.enableAutoSizing = true;
        panel.titleText.fontStyle = FontStyles.Bold;
        panel.titleText.color = new Color(0.85f, 0.92f, 1f, 1f);
        panel.titleText.alignment = TextAlignmentOptions.TopLeft;
        panel.titleText.textWrappingMode = TextWrappingModes.Normal;
        panel.titleText.overflowMode = TextOverflowModes.Ellipsis;

        var closeGo = new GameObject("CloseButton");
        closeGo.transform.SetParent(cardGo.transform, false);
        var closeRect = closeGo.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.86f, 0.88f);
        closeRect.anchorMax = new Vector2(0.98f, 0.98f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;
        var closeImage = closeGo.AddComponent<Image>();
        closeImage.color = new Color(0.25f, 0.3f, 0.4f, 1f);
        panel.closeButton = closeGo.AddComponent<Button>();

        var closeLabelGo = new GameObject("Label");
        closeLabelGo.transform.SetParent(closeGo.transform, false);
        var closeLabelRect = closeLabelGo.AddComponent<RectTransform>();
        closeLabelRect.anchorMin = Vector2.zero;
        closeLabelRect.anchorMax = Vector2.one;
        closeLabelRect.offsetMin = Vector2.zero;
        closeLabelRect.offsetMax = Vector2.zero;
        var closeLabel = closeLabelGo.AddComponent<TextMeshProUGUI>();
        closeLabel.text = "×";
        closeLabel.fontSize = 40;
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.color = Color.white;
        closeLabel.raycastTarget = false;

        var orbitGo = new GameObject("OrbitElements");
        orbitGo.transform.SetParent(cardGo.transform, false);
        var orbitRect = orbitGo.AddComponent<RectTransform>();
        orbitRect.anchorMin = new Vector2(0.04f, 0.80f);
        orbitRect.anchorMax = new Vector2(0.96f, 0.87f);
        orbitRect.offsetMin = Vector2.zero;
        orbitRect.offsetMax = Vector2.zero;
        var orbitTmp = orbitGo.AddComponent<TextMeshProUGUI>();
        OrbitElementsDisplay.ConfigureCardTmp(orbitTmp);
        orbitTmp.alignment = TextAlignmentOptions.Left;
        panel.orbitElementsText = orbitTmp;

        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(cardGo.transform, false);
        var scrollRect = scrollGo.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.04f, 0.04f);
        scrollRect.anchorMax = new Vector2(0.96f, 0.78f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 20f;
        panel.bodyScroll = scroll;

        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var viewportRect = viewportGo.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportGo.AddComponent<RectMask2D>();
        viewportGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
        scroll.viewport = viewportRect;

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = Vector2.zero;
        scroll.content = contentRect;

        var layoutGroup = contentGo.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(8, 8, 8, 8);
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(contentGo.transform, false);
        var bodyRect = bodyGo.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.sizeDelta = Vector2.zero;

        var bodyLayout = bodyGo.AddComponent<LayoutElement>();
        bodyLayout.flexibleWidth = 1f;

        panel.bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        panel.bodyText.fontSize = 28;
        panel.bodyText.color = new Color(0.9f, 0.92f, 0.95f, 1f);
        panel.bodyText.alignment = TextAlignmentOptions.TopLeft;
        panel.bodyText.textWrappingMode = TextWrappingModes.Normal;
        panel.bodyText.richText = true;

        panel.WireCloseButton();
        rootGo.SetActive(false);
        return panel;
    }
}
