using UnityEngine;

/// <summary>
/// Per-asteroid metadata and multilingual descriptions.
/// </summary>
public class AsteroidInfo : MonoBehaviour
{
    [SerializeField] string asteroidId;
    [SerializeField] string labelKey;

    [TextArea(8, 24)] [SerializeField] string descriptionEnglish;
    [TextArea(8, 24)] [SerializeField] string descriptionRussian;
    [TextArea(8, 24)] [SerializeField] string descriptionChinese;
    [TextArea(8, 24)] [SerializeField] string descriptionVietnamese;
    [TextArea(8, 24)] [SerializeField] string descriptionUzbek;
    [TextArea(8, 24)] [SerializeField] string descriptionTatar;
    [TextArea(8, 24)] [SerializeField] string descriptionBelarusian;

    public string AsteroidId => asteroidId;
    public string LabelKey => labelKey;

    public void Configure(string id, string key)
    {
        asteroidId = id;
        labelKey = key;
    }

    public void SetDescriptions(
        string english,
        string russian,
        string chinese,
        string vietnamese,
        string uzbek,
        string tatar,
        string belarusian)
    {
        descriptionEnglish = english;
        descriptionRussian = russian;
        descriptionChinese = chinese;
        descriptionVietnamese = vietnamese;
        descriptionUzbek = uzbek;
        descriptionTatar = tatar;
        descriptionBelarusian = belarusian;
    }

    public string GetDescription(Language language)
    {
        switch (language)
        {
            case Language.Russian:
                return descriptionRussian;
            case Language.Chinese:
                return descriptionChinese;
            case Language.Vietnamese:
                return descriptionVietnamese;
            case Language.Uzbek:
                return descriptionUzbek;
            case Language.Tatar:
                return descriptionTatar;
            case Language.Belarusian:
                return descriptionBelarusian;
            default:
                return descriptionEnglish;
        }
    }

    public string GetTitle()
    {
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(labelKey))
        {
            string translation = LocalizationManager.Instance.GetTranslation(labelKey);
            if (!string.IsNullOrEmpty(translation))
                return translation;
        }

        return labelKey;
    }
}
