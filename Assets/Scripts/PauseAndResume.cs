using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseAndResume : MonoBehaviour
{
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "Level1"; // Имя игровой сцены

    private float previousTimeScale; // Сохранённое значение скорости времени

    public void EnterMenu()
    {
        // Сохраняем текущее значение timeScale
        //previousTimeScale = Time.timeScale;
        // Пауза игры
        //Time.timeScale = 0;
        // Загружаем меню
        SceneManager.LoadSceneAsync(mainMenuSceneName);
    }

    public void ResumeGame()
    {
        // Восстанавливаем сохранённую скорость времени
        //Time.timeScale = previousTimeScale;
        // Возвращаемся к игровой сцене
        SceneManager.LoadSceneAsync(gameSceneName);
    }
}