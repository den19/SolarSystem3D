using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared lookup / formatting for orbital eccentricity and inclination (encyclopedia + orbit labels).
/// </summary>
public static class OrbitElementsDisplay
{
    public static readonly Color IttenOrange = new Color(1f, 0.5f, 0.08f);

    public const string FormatKey = "OrbitElementsFormat";
    const string FallbackFormat = "e = {0} · i = {1}°";
    const string CardRowName = "OrbitElements";
    const float CardFontSize = 24f;
    const float CardPreferredHeight = 28f;

    public static bool TryGet(string objectName, out float eccentricity, out float inclinationDeg)
    {
        eccentricity = 0f;
        inclinationDeg = 0f;

        if (string.IsNullOrEmpty(objectName) || objectName == "Sun")
            return false;

        if (SolarSystemCatalog.TryGetBody(objectName, out SolarSystemCatalog.BodyDefinition body))
        {
            eccentricity = body.orbitalEccentricity;
            inclinationDeg = body.orbitalInclinationDeg;
            return true;
        }

        if (CometCatalog.TryGetByObjectName(objectName, out CometCatalog.CometDefinition comet))
        {
            eccentricity = comet.eccentricity;
            inclinationDeg = comet.inclinationDeg;
            return true;
        }

        if (AsteroidCatalog.TryGetByObjectName(objectName, out AsteroidCatalog.AsteroidDefinition asteroid))
        {
            eccentricity = asteroid.eccentricity;
            inclinationDeg = asteroid.inclinationDeg;
            return true;
        }

        return false;
    }

    public static string Format(float eccentricity, float inclinationDeg)
    {
        var culture = CultureInfo.InvariantCulture;
        string eText = eccentricity.ToString("F4", culture);
        string iText = inclinationDeg.ToString("F1", culture);

        string format = FallbackFormat;
        if (LocalizationManager.Instance != null)
        {
            string translation = LocalizationManager.Instance.GetTranslation(FormatKey);
            if (!string.IsNullOrEmpty(translation))
                format = translation;
        }

        return string.Format(culture, format, eText, iText);
    }

    public static bool TryFormat(string objectName, out string text)
    {
        if (!TryGet(objectName, out float eccentricity, out float inclinationDeg))
        {
            text = null;
            return false;
        }

        text = Format(eccentricity, inclinationDeg);
        return true;
    }

    /// <summary>
    /// Injects or updates an orange e/i row in a planet encyclopedia panel, immediately under *Header.
    /// </summary>
    public static void EnsureCardRow(GameObject descriptionRoot, string bodyName)
    {
        if (descriptionRoot == null)
            return;

        Transform content = descriptionRoot.transform.Find("Viewport/Content");
        if (content == null)
            return;

        Transform existing = content.Find(CardRowName);
        if (!TryFormat(bodyName, out string text))
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            return;
        }

        TextMeshProUGUI tmp;
        OrbitElementsCardBinder binder;
        if (existing == null)
        {
            var go = new GameObject(CardRowName, typeof(RectTransform));
            go.transform.SetParent(content, false);

            int headerIndex = 0;
            for (int i = 0; i < content.childCount; i++)
            {
                if (content.GetChild(i).name.EndsWith("Header"))
                {
                    headerIndex = i;
                    break;
                }
            }

            go.transform.SetSiblingIndex(Mathf.Min(headerIndex + 1, content.childCount - 1));

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, CardPreferredHeight);

            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = CardPreferredHeight;
            layout.preferredHeight = CardPreferredHeight;
            layout.flexibleWidth = 1f;

            tmp = go.AddComponent<TextMeshProUGUI>();
            ConfigureCardTmp(tmp);

            binder = go.AddComponent<OrbitElementsCardBinder>();
        }
        else
        {
            existing.gameObject.SetActive(true);
            tmp = existing.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                tmp = existing.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureCardTmp(tmp);

            binder = existing.GetComponent<OrbitElementsCardBinder>();
            if (binder == null)
                binder = existing.gameObject.AddComponent<OrbitElementsCardBinder>();
        }

        binder.Bind(bodyName, tmp);
        binder.Refresh();
    }

    public static void ConfigureCardTmp(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;

        tmp.fontSize = CardFontSize;
        tmp.color = IttenOrange;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.characterSpacing = 6f;
        tmp.raycastTarget = false;

        var lang = LocalizationManager.CurrentLanguage;
        tmp.font = LocalizationFontHelper.GetFontForLanguage(lang);
        var overlay = LocalizationFontHelper.GetOverlayMaterialForLanguage(lang);
        if (overlay != null)
            tmp.fontSharedMaterial = overlay;
    }
}

/// <summary>
/// Keeps an encyclopedia card e/i row in sync with the current language.
/// </summary>
public class OrbitElementsCardBinder : MonoBehaviour
{
    string _bodyName;
    TMP_Text _label;

    public void Bind(string bodyName, TMP_Text label)
    {
        _bodyName = bodyName;
        _label = label;
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (_label == null || string.IsNullOrEmpty(_bodyName))
            return;

        OrbitElementsDisplay.ConfigureCardTmp(_label as TextMeshProUGUI);

        if (!OrbitElementsDisplay.TryFormat(_bodyName, out string text))
        {
            gameObject.SetActive(false);
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        _label.text = text;
    }
}
