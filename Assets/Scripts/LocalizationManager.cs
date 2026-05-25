using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    Dictionary<string, string> translations = new();
    public static Language CurrentLanguage { get; set; } = Language.English;

    private void Awake()
    {
        Instance = this;
        LoadTranslations(CurrentLanguage);
        ApplyTranslations();
    }

    public void ChangeLanguage(Language lang)
    {
        LoadTranslations(lang);
        ApplyTranslations();
    }

    private void LoadTranslations(Language lang)
    {
        string path = $"Languages/{lang.ToString().ToLower()}";
        TextAsset jsonData = Resources.Load<TextAsset>(path);

        translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData.text);

        CurrentLanguage = lang;
    }

    private void ApplyTranslations()
    {
        var textObjectsToTranslate = FindObjectsByType<Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (var textElement in textObjectsToTranslate)
        {
            if (translations.TryGetValue(textElement.name, out string translation))
            {
                textElement.text = translation;
            }
        }

        var texMeshProObjectsToTranslate = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (var textElement in texMeshProObjectsToTranslate)
        {
            if (translations.TryGetValue(textElement.name, out string translation))
            {
                textElement.text = translation;
            }
        }

    }

}

public enum Language
{
    English,
    Russian
}