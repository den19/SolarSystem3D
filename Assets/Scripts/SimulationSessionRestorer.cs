using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimulationSessionRestorer : MonoBehaviour
{
    private static SimulationSessionRestorer instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("SimulationSessionRestorer_Persistent");
        instance = go.AddComponent<SimulationSessionRestorer>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Level1")
        {
            return;
        }

        StartCoroutine(RestoreAfterInit());
    }

    private IEnumerator RestoreAfterInit()
    {
        while (!SimulationViewBootstrap.SystemsReady)
        {
            yield return null;
        }

        if (SimulationSessionState.PendingLaunchMode != SimulationLaunchMode.Continue)
        {
            SimulationSessionState.PendingLaunchMode = SimulationLaunchMode.New;
            yield break;
        }

        SimulationSessionState.RestoreMotionState();
        SimulationSessionState.RestoreToScene();
        SimulationSessionState.PendingLaunchMode = SimulationLaunchMode.New;
    }
}
