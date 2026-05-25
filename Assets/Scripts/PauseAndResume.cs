using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseAndResume : MonoBehaviour
{
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "Level1"; // Имя игровой сцены

    // Использование static гарантирует сохранение значения при смене сцены
    private static float previousTimeScale = 1f;

    private void Start()
    {
        // Гарантируем, что при старте сцены время идет с нормальной скоростью
        Time.timeScale = 1f;
    }

    public void EnterMenu()
    {
        // Сохраняем текущее значение timeScale перед выходом в меню
        if (Time.timeScale > 0)
        {
            previousTimeScale = Time.timeScale;
        }
        
        // При переходе в главное меню устанавливаем скорость времени равной 1,
        // чтобы работали 3D-анимации окон и вращение планет на заднем фоне меню.
        Time.timeScale = 1f;
        
        // Загружаем меню
        SceneManager.LoadSceneAsync(mainMenuSceneName);
    }

    public void ResumeGame()
    {
        // Восстанавливаем сохранённую скорость времени
        Time.timeScale = previousTimeScale > 0 ? previousTimeScale : 1f;
        
        // Возвращаемся к игровой сцене
        SceneManager.LoadSceneAsync(gameSceneName);
    }
}