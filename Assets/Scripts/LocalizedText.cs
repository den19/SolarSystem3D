using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    private Text standardText;
    private TMP_Text tmpText;
    private string textKey; // Имя объекта используется в качестве ключа перевода

    private void Awake()
    {
        standardText = GetComponent<Text>();
        tmpText = GetComponent<TMP_Text>();
        textKey = gameObject.name; // Ключ перевода — это имя объекта в иерархии сцены
    }

    private void OnEnable()
    {
        // Подписываемся на событие смены языка
        LocalizationManager.OnLanguageChanged += UpdateText;
        UpdateText(); // Обновляем текст сразу при включении
    }

    private void OnDisable()
    {
        // Обязательно отписываемся во избежание утечек памяти
        LocalizationManager.OnLanguageChanged -= UpdateText;
    }

    private void UpdateText()
    {
        if (LocalizationManager.Instance == null) return;

        string translation = LocalizationManager.Instance.GetTranslation(textKey);
        if (string.IsNullOrEmpty(translation)) return;

        if (standardText != null)
        {
            standardText.text = translation;
        }
        else if (tmpText != null)
        {
            tmpText.text = translation;
        }
    }
}
