using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single dynamic overlay for comet encyclopedia text in the current language.
/// </summary>
public class CometDescriptionPanel : MonoBehaviour
{
    public static CometDescriptionPanel Instance { get; private set; }

    [SerializeField] GameObject panelRoot;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] Button closeButton;

    CometInfo _currentInfo;

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
        if (_currentInfo != null && panelRoot.activeSelf)
            RefreshText();
    }

    public void Show(CometInfo info)
    {
        if (info == null)
            return;

        _currentInfo = info;
        panelRoot.SetActive(true);
        RefreshText();
    }

    public void Hide()
    {
        _currentInfo = null;
        HideImmediate();
    }

    public void HideImmediate()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void RefreshText()
    {
        if (_currentInfo == null)
            return;

        if (titleText != null)
            titleText.text = _currentInfo.GetTitle();

        if (bodyText != null)
        {
            Language lang = LocalizationManager.CurrentLanguage;
            bodyText.text = _currentInfo.GetDescription(lang);
        }
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
        panel.titleText.fontSize = 22;
        panel.titleText.fontStyle = FontStyles.Bold;
        panel.titleText.color = new Color(0.85f, 0.92f, 1f, 1f);
        panel.titleText.alignment = TextAlignmentOptions.MidlineLeft;

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
        closeLabel.fontSize = 28;
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.color = Color.white;
        closeLabel.raycastTarget = false;

        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(cardGo.transform, false);
        var scrollRect = scrollGo.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.04f, 0.04f);
        scrollRect.anchorMax = new Vector2(0.96f, 0.86f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var viewportRect = viewportGo.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;
        viewportGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
        scroll.viewport = viewportRect;

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 400f);
        scroll.content = contentRect;

        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(contentGo.transform, false);
        var bodyRect = bodyGo.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.offsetMin = new Vector2(8f, 0f);
        bodyRect.offsetMax = new Vector2(-8f, 0f);

        panel.bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        panel.bodyText.fontSize = 15;
        panel.bodyText.color = new Color(0.9f, 0.92f, 0.95f, 1f);
        panel.bodyText.alignment = TextAlignmentOptions.TopLeft;
        panel.bodyText.textWrappingMode = TextWrappingModes.Normal;
        panel.bodyText.richText = true;

        var fitter = bodyGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        panel.WireCloseButton();
        rootGo.SetActive(false);
        return panel;
    }
}
