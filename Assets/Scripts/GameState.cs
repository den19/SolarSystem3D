using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState instance;

    public int score;
    public static bool isPaused;
    public static Language language;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Сохраняет объект при смене сцен
        }
    }
}