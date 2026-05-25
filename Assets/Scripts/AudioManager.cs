using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance = null;
    public static AudioManager Instance { get => instance; }

    // Публичное поле для задания аудиоклипа
    public AudioClip clickSound;

    // Компонент для воспроизведения звука
    private AudioSource audioSource;
    private string nextSceneName;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            //Destroy(gameObject);

        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Сохранение экземпляра между сценами
        }

        // Проверяем наличие компонента AudioSource и добавляем, если отсутствует
        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // Воспроизводит звук клика
    public void PlaySound()
    {
        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    // Функция для воспроизведения звука и перехода на другую сцену
    public void PlaySoundAndTransition(string sceneName)
    {
        if (clickSound != null)
        {
            audioSource.clip = clickSound;
            audioSource.loop = false;
            audioSource.Play();

            // Запоминаем название следующей сцены
            nextSceneName = sceneName;
        }
        else
        {
            SceneManager.LoadScene(sceneName); // Если звука нет, загружаем сцену немедленно
        }
    }

    // Загружает следующую сцену
    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            nextSceneName = "";
        }
    }

    // Периодически проверяем, закончилось ли воспроизведение звука
    void Update()
    {
        if (!audioSource.isPlaying && !string.IsNullOrEmpty(nextSceneName))
        {
            LoadNextScene();
        }
    }
}