using UnityEngine;

/// <summary>
/// Per-comet metadata and multilingual descriptions stored on the comet prefab.
/// </summary>
public class CometInfo : MonoBehaviour
{
    [SerializeField] string cometId;
    [SerializeField] string labelKey;

    [TextArea(8, 24)] [SerializeField] string descriptionEnglish;
    [TextArea(8, 24)] [SerializeField] string descriptionRussian;
    [TextArea(8, 24)] [SerializeField] string descriptionChinese;
    [TextArea(8, 24)] [SerializeField] string descriptionVietnamese;
    [TextArea(8, 24)] [SerializeField] string descriptionUzbek;

    public string CometId => cometId;
    public string LabelKey => labelKey;

    public void Configure(string id, string key)
    {
        cometId = id;
        labelKey = key;
    }

    public void SetDescriptions(string english, string russian, string chinese, string vietnamese, string uzbek)
    {
        descriptionEnglish = english;
        descriptionRussian = russian;
        descriptionChinese = chinese;
        descriptionVietnamese = vietnamese;
        descriptionUzbek = uzbek;
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
