using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseAndResume : MonoBehaviour
{
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "Level1";

    private void Start()
    {
        SimulationSessionState.Load();
        Time.timeScale = SimulationSessionState.HasSavedState
            ? SimulationSessionState.TimeScale
            : 1f;
    }

    public void EnterMenu()
    {
        SimulationSessionState.CaptureFromScene();

        // Menu animations need normal time flow.
        Time.timeScale = 1f;

        SceneManager.LoadSceneAsync(mainMenuSceneName);
    }

    public void ResumeGame()
    {
        SimulationSessionState.Load();
        Time.timeScale = SimulationSessionState.HasSavedState
            ? SimulationSessionState.TimeScale
            : 1f;

        SceneManager.LoadSceneAsync(gameSceneName);
    }
}
