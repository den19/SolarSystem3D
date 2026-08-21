using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bootstraps simulation view systems when Level1 loads.
/// </summary>
public class SimulationViewBootstrap : MonoBehaviour
{
    static SimulationViewBootstrap instance;

    bool systemsInitialized;

    public static bool SystemsReady { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Play-from-Level1 in editor: sceneLoaded already fired before this subscription.
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.isLoaded && activeScene.name == "Level1")
            EnsureLevel1Systems(activeScene, LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureLevel1Systems(scene, mode);
    }

    static void EnsureLevel1Systems(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Level1")
            return;

        if (instance != null)
            Destroy(instance.gameObject);

        SystemsReady = false;

        var go = new GameObject("SimulationViewSystems");
        instance = go.AddComponent<SimulationViewBootstrap>();
        go.AddComponent<SimulationShareController>();
        go.AddComponent<TransientMessageController>();
    }

    void Start()
    {
        // Fallback for edge cases where EnsureLevel1Systems was skipped.
        if (SceneManager.GetActiveScene().name == "Level1" && !systemsInitialized)
            StartCoroutine(InitializeAfterSceneReady());
    }

    IEnumerator InitializeAfterSceneReady()
    {
        if (systemsInitialized)
            yield break;

        yield return null;

        if (systemsInitialized)
            yield break;

        systemsInitialized = true;

        try
        {
            var orbitLines = gameObject.AddComponent<OrbitLinesManager>();
            var bodyLabels = gameObject.AddComponent<BodyLabelManager>();
            gameObject.AddComponent<MinimapController>();

            var cometSystem = gameObject.AddComponent<CometSystemController>();
            cometSystem.Initialize(orbitLines, bodyLabels);

            var asteroidSystem = gameObject.AddComponent<AsteroidSystemController>();
            asteroidSystem.Initialize(orbitLines, bodyLabels);

            gameObject.AddComponent<SolarSystemScaleController>();
            gameObject.AddComponent<BodyOrbitSystemController>();
            gameObject.AddComponent<BodyGridProjectionController>();
            TimeMachineController.EnsureOnHost(gameObject);
            ProbeSystemController.EnsureOnHost(gameObject);

            Canvas canvas = null;
            GameObject canvasGo = GameObject.Find("MainScreenCanvas");
            if (canvasGo != null)
                canvas = canvasGo.GetComponent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>();
            SimulationSidePanelController sidePanel = null;
            if (canvas != null)
                sidePanel = canvas.GetComponentInChildren<SimulationSidePanelController>(true);

            if (sidePanel == null)
                Debug.LogError("SimulationSidePanelController not found on MainScreenCanvas. Add SimulationSidePanel to the scene.");

            if (canvas != null)
            {
                CometDescriptionPanel.EnsureOnCanvas(canvas.transform);
                TimeControlUiBootstrap.EnsureTimeControlBar(canvas.transform);
                ScaleBarController.EnsureOnCanvas(canvas.transform);
                // TimeMachineDateHud is created only when Time Machine mode is enabled.
            }

            var cleanView = gameObject.AddComponent<CleanViewController>();
            cleanView.Initialize(cometSystem, sidePanel);
        }
        catch (Exception exception)
        {
            Debug.LogError("SimulationViewBootstrap: simulation systems init failed. " + exception);
        }

        yield return null;

        SystemsReady = true;
    }
}
