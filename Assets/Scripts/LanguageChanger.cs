using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LanguageChanger : MonoBehaviour
{
    public Dropdown languageDropdown;

    void Start()
    {
        if (!GameState.isPaused)
        {
            languageDropdown.ClearOptions();
            List<string> languages = new List<string>() { "English", "Русский" };
            languageDropdown.AddOptions(languages);
        }
        else
        {
            var ind = (int)GameState.language;
            languageDropdown.value = ind;
            OnChangeLanguage(ind);
        }
    }

    public void OnChangeLanguage(int index)
    {
        Language lang = (Language)index;
        LocalizationManager.Instance.ChangeLanguage(lang);
        GameState.language = lang;
    }
}