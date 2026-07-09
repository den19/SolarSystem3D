using UnityEngine;
using UnityEngine.SceneManagement;

public class SimulationSessionAutosave : MonoBehaviour
{
    static SimulationSessionAutosave instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject("SimulationSessionAutosave_Persistent");
        instance = go.AddComponent<SimulationSessionAutosave>();
        DontDestroyOnLoad(go);
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            TryCaptureLevel1State();
    }

    void OnApplicationQuit()
    {
        TryCaptureLevel1State();
    }

    static void TryCaptureLevel1State()
    {
        if (SceneManager.GetActiveScene().name != "Level1")
            return;

        SimulationSessionState.CaptureFromScene();
    }
}
