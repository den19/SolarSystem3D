using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

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
        ApplyTranslations();
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
        // Автоматически переводим всю сцену при её загрузке
        ApplyTranslations();
    }

    public void ChangeLanguage(Language lang)
    {
        // Сейфгард: Если этот язык уже загружен, не делаем повторную работу
        if (translations != null && translations.Count > 0 && CurrentLanguage == lang)
        {
            return;
        }

        LoadTranslations(lang);
        ApplyTranslations();

        // Сохраняем выбор в память устройства
        PlayerPrefs.SetInt("SelectedLanguage", (int)lang);
        PlayerPrefs.Save();
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

    private void ApplyTranslations()
    {
        if (translations == null || translations.Count == 0) return;

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