using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LanguageChanger : MonoBehaviour
{
    static readonly string[] LanguageLabels = { "English", "Русский", "中文", "Tiếng Việt", "Oʻzbek" };

    public Dropdown languageDropdown;
    private bool isInitializing = false; // Флаг для блокировки ложных срабатываний UI

    IEnumerator Start()
    {
        isInitializing = true; // Блокируем вызовы во время настройки UI
        
        // Восстанавливаем время для Главного меню, чтобы работали 3D-анимации окон и фон
        Time.timeScale = 1f;

        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new List<string>(LanguageLabels));

            // Получаем сохраненный язык из PlayerPrefs (синхронно с LocalizationManager)
            int savedLanguage = PlayerPrefs.GetInt("SelectedLanguage", (int)Language.English);
            int maxLanguage = (int)Language.Uzbek;
            if (savedLanguage < 0 || savedLanguage > maxLanguage)
                savedLanguage = (int)Language.English;
            languageDropdown.value = savedLanguage;
            GameState.language = (Language)savedLanguage;

            // Dynamically register the listener
            languageDropdown.onValueChanged.RemoveAllListeners();
            languageDropdown.onValueChanged.AddListener(OnChangeLanguage);

            // Apply initially selected language if localization manager exists
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.ChangeLanguage((Language)savedLanguage);
            }
        }
        
        // КРИТИЧЕСКИЙ СЕЙФГАРД: Ждем окончания кадра.
        // За это время Unity завершит все внутренние перестроения макета (layout rebuild),
        // которые вызывают ложные срабатывания onValueChanged(0).
        yield return new WaitForEndOfFrame();
        
        isInitializing = false; // Разблокируем обработку кликов пользователя
    }

    public void OnChangeLanguage(int index)
    {
        if (isInitializing) return; // Игнорируем авто-сбросы от Unity

        Language lang = (Language)index;
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.ChangeLanguage(lang);
        }
        GameState.language = lang;
    }

    public void RefreshFromSaved()
    {
        if (languageDropdown == null)
        {
            return;
        }

        isInitializing = true;

        int savedLanguage = PlayerPrefs.GetInt("SelectedLanguage", (int)Language.English);
        int maxLanguage = (int)Language.Uzbek;
        if (savedLanguage < 0 || savedLanguage > maxLanguage)
        {
            savedLanguage = (int)Language.English;
        }

        languageDropdown.SetValueWithoutNotify(savedLanguage);
        GameState.language = (Language)savedLanguage;

        isInitializing = false;
    }
}