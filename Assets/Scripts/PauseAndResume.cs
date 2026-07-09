using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseAndResume : MonoBehaviour
{
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "Level1";

    private void Start()
    {
        SimulationSessionState.Load();

        if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            GameState.isPaused = true;
        }

        Time.timeScale = SimulationSessionState.HasSavedState
            && SimulationSessionState.PendingLaunchMode == SimulationLaunchMode.Continue
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

    public void StartNewSimulation()
    {
        SimulationSessionState.Clear();
        SimulationSessionState.PendingLaunchMode = SimulationLaunchMode.New;
        GameState.isPaused = true;
        Time.timeScale = 1f;
    }

    public void ResumeGame()
    {
        SimulationSessionState.Load();

        if (!SimulationSessionState.HasSavedState)
        {
            return;
        }

        SimulationSessionState.PendingLaunchMode = SimulationLaunchMode.Continue;
        GameState.isPaused = true;
        Time.timeScale = SimulationSessionState.TimeScale;
    }
}
