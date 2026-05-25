using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    // Делегат и событие смены языка
    public delegate void LanguageChangedDelegate();
    public static event LanguageChangedDelegate OnLanguageChanged;

    private Dictionary<string, string> translations = new();
    public static Language CurrentLanguage { get; set; } = Language.English;

    // Метод для гарантированной инициализации синглтона
    public static void Initialize()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("LocalizationManager_Persistent");
        Instance = go.AddComponent<LocalizationManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        // Если это старый дубликат из сцены — уничтожаем только скрипт, не трогая сам GameObject
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Загружаем сохраненный язык из PlayerPrefs (по умолчанию English = 0)
        int savedLanguage = PlayerPrefs.GetInt("SelectedLanguage", (int)Language.English);
        CurrentLanguage = (Language)savedLanguage;
        GameState.language = CurrentLanguage; // Синхронизируем GameState

        LoadTranslations(CurrentLanguage);
    }

    private void OnEnable()
    {
        // Подписываемся на автоматический перевод каждой новой загруженной сцены
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Автоматически находим новые текстовые объекты в сцене и вешаем на них локализатор
        AutoRegisterLocalizedTexts();
        
        // Оповещаем все текстовые объекты о смене сцены и необходимости обновить тексты
        OnLanguageChanged?.Invoke();
    }

    public void ChangeLanguage(Language lang)
    {
        // Сейфгард: Если этот язык уже загружен, не делаем повторную работу
        if (translations != null && translations.Count > 0 && CurrentLanguage == lang)
        {
            return;
        }

        LoadTranslations(lang);
        
        // Оповещаем все подписанные текстовые объекты о смене языка
        OnLanguageChanged?.Invoke();

        // Сохраняем выбор в память устройства
        PlayerPrefs.SetInt("SelectedLanguage", (int)lang);
        PlayerPrefs.Save();
    }

    // Метод получения перевода по ключу
    public string GetTranslation(string key)
    {
        if (translations != null && translations.TryGetValue(key, out string translation))
        {
            return translation;
        }
        return null;
    }

    private void LoadTranslations(Language lang)
    {
        string path = $"Languages/{lang.ToString().ToLower()}";
        TextAsset jsonData = Resources.Load<TextAsset>(path);

        if (jsonData != null)
        {
            translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData.text);
            CurrentLanguage = lang;
        }
        else
        {
            Debug.LogError($"Файл перевода не найден: {path}");
        }
    }

    // Вспомогательный метод для автоматической регистрации локализации на текстовых объектах
    private void AutoRegisterLocalizedTexts()
    {
        // Находим все текстовые элементы в сцене и автоматически вешаем LocalizedText
        var texts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in texts)
        {
            if (t.gameObject.GetComponent<LocalizedText>() == null)
            {
                t.gameObject.AddComponent<LocalizedText>();
            }
        }

        var tmpTexts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tmp in tmpTexts)
        {
            if (tmp.gameObject.GetComponent<LocalizedText>() == null)
            {
                tmp.gameObject.AddComponent<LocalizedText>();
            }
        }
    }
}

public enum Language
{
    English,
    Russian
}