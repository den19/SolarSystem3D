using System.Collections;
using SolarSystemApp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows short localized toast messages at the bottom of the screen.
/// </summary>
public class TransientMessageController : MonoBehaviour
{
    public static TransientMessageController Instance { get; private set; }

    const float DefaultDuration = 3f;

    RectTransform _panelRect;
    TextMeshProUGUI _label;
    Coroutine _hideRoutine;
    string _currentKey;
    string _currentFallback;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        BuildUi();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshCurrentText;
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshCurrentText;
    }

    public static void ShowLocalized(string key, string fallback, float duration = DefaultDuration)
    {
        if (Instance == null)
        {
            Debug.LogWarning("TransientMessageController: " + Translate(key, fallback));
            return;
        }

        Instance.ShowInternal(key, fallback, duration);
    }

    void ShowInternal(string key, string fallback, float duration)
    {
        _currentKey = key;
        _currentFallback = fallback;
        ApplyText(key, fallback);

        if (_panelRect != null)
            _panelRect.gameObject.SetActive(true);

        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);

        _hideRoutine = StartCoroutine(HideAfterDelay(duration));
    }

    IEnumerator HideAfterDelay(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        _hideRoutine = null;

        if (_panelRect != null)
            _panelRect.gameObject.SetActive(false);
    }

    void RefreshCurrentText()
    {
        if (_panelRect == null || !_panelRect.gameObject.activeSelf)
            return;

        ApplyText(_currentKey, _currentFallback);
    }

    void ApplyText(string key, string fallback)
    {
        if (_label == null)
            return;

        _label.text = Translate(key, fallback);

        var lang = LocalizationManager.CurrentLanguage;
        _label.font = LocalizationFontHelper.GetFontForLanguage(lang);
        var overlay = LocalizationFontHelper.GetOverlayMaterialForLanguage(lang);
        if (overlay != null)
            _label.fontSharedMaterial = overlay;
    }

    static string Translate(string key, string fallback)
    {
        if (LocalizationManager.Instance == null)
            return fallback;

        string translation = LocalizationManager.Instance.GetTranslation(key);
        return string.IsNullOrEmpty(translation) ? fallback : translation;
    }

    void BuildUi()
    {
        Canvas canvas = FindMainScreenCanvas();
        if (canvas == null)
            return;

        var rootGo = new GameObject("TransientMessage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rootGo.layer = canvas.gameObject.layer;
        rootGo.transform.SetParent(canvas.transform, false);

        _panelRect = rootGo.GetComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(0.5f, 0f);
        _panelRect.anchorMax = new Vector2(0.5f, 0f);
        _panelRect.pivot = new Vector2(0.5f, 0f);
        _panelRect.anchoredPosition = new Vector2(0f, 24f);
        _panelRect.sizeDelta = new Vector2(640f, 56f);

        var panelImage = rootGo.GetComponent<Image>();
        panelImage.color = new Color(0.02f, 0.05f, 0.12f, 0.88f);
        panelImage.raycastTarget = false;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(rootGo.transform, false);

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(16f, 8f);
        labelRect.offsetMax = new Vector2(-16f, -8f);

        _label = labelGo.AddComponent<TextMeshProUGUI>();
        _label.fontSize = 16f;
        _label.color = new Color(0.92f, 0.95f, 1f, 1f);
        _label.alignment = TextAlignmentOptions.Center;
        _label.textWrappingMode = TextWrappingModes.Normal;
        _label.raycastTarget = false;

        rootGo.SetActive(false);
    }

    static Canvas FindMainScreenCanvas()
    {
        GameObject canvasGo = GameObject.Find("MainScreenCanvas");
        return canvasGo != null ? canvasGo.GetComponent<Canvas>() : null;
    }
}
