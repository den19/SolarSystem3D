using SolarSystemApp;
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
            if (SimulationSessionState.HasSavedState
                && SimulationSessionState.PendingLaunchMode == SimulationLaunchMode.Continue)
            {
                SimulationTimeController.RestoreState(
                    SimulationSessionState.SpeedMultiplier,
                    SimulationSessionState.IsSimulationPaused);
            }
            else
            {
                SimulationTimeController.ResetToDefaults();
            }
        }
        else if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            Time.timeScale = 1f;
        }
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
        SimulationTimeController.ResetToDefaults();
        // Time Machine is manual-only; never auto-start on New.
        TimeMachineSettings.SetUseTimeMachine(false);
        Time.timeScale = 1f;
        // Load here so menu works even if AudioManager OnClick target is stale.
        SceneManager.LoadSceneAsync(gameSceneName);
    }

    public void ResumeGame()
    {
        SimulationSessionState.Load();

        if (!SimulationSessionState.HasSavedState)
        {
            return;
        }

        SimulationSessionState.PendingLaunchMode = SimulationLaunchMode.Continue;
        SimulationTimeController.RestoreState(
            SimulationSessionState.SpeedMultiplier,
            SimulationSessionState.IsSimulationPaused,
            apply: false);
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(gameSceneName);
    }
}
