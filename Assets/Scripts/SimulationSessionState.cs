using System;
using System.Collections.Generic;
using UnityEngine;

public enum SimulationLaunchMode
{
    New,
    Continue
}

public static class SimulationSessionState
{
    private const string KeyHasSessionState = "SolarSystem_HasSessionState";
    private const string KeySelectedPlanet = "SolarSystem_SelectedPlanet";
    private const string KeyIsDetailCamera = "SolarSystem_IsDetailCamera";
    private const string KeyTimeScale = "SolarSystem_TimeScale";
    private const string KeySpeedMultiplier = "SolarSystem_SpeedMultiplier";
    private const string KeyIsPaused = "SolarSystem_IsPaused";
    private const string KeyMainCamX = "SolarSystem_MainCamX";
    private const string KeyMainCamY = "SolarSystem_MainCamY";
    private const string KeyMainCamDistance = "SolarSystem_MainCamDistance";
    private const string KeyDetailCamX = "SolarSystem_DetailCamX";
    private const string KeyDetailCamY = "SolarSystem_DetailCamY";
    private const string KeyDetailCamDistance = "SolarSystem_DetailCamDistance";
    private const string KeyActiveCamDistance = "SolarSystem_ActiveCamDistance";
    private const string KeyMotionSnapshot = "SolarSystem_MotionSnapshot";

    public static SimulationLaunchMode PendingLaunchMode { get; set; } = SimulationLaunchMode.New;

    public static bool HasSavedState { get; private set; }
    public static string SelectedPlanetName { get; private set; } = "Earth";
    public static bool IsDetailCamera { get; private set; }
    public static float TimeScale { get; private set; } = 1f;
    public static float SpeedMultiplier { get; private set; } = SimulationTimeController.DefaultSpeedMultiplier;
    public static bool IsSimulationPaused { get; private set; }
    public static float MainCamX { get; private set; }
    public static float MainCamY { get; private set; }
    public static float MainCamDistance { get; private set; } = 8f;
    public static float DetailCamX { get; private set; }
    public static float DetailCamY { get; private set; }
    public static float DetailCamDistance { get; private set; } = 4.5f;
    public static float ActiveCameraDistance { get; private set; } = 8f;

    static MotionSnapshot _motionSnapshot;

    [Serializable]
    class BodyMotionSnapshot
    {
        public string name;
        public bool usePhase;
        public float phaseRad;
        public float posX;
        public float posY;
        public float posZ;
    }

    [Serializable]
    class CometMotionSnapshot
    {
        public string name;
        public float angle;
    }

    [Serializable]
    class MotionSnapshot
    {
        public BodyMotionSnapshot[] bodies;
        public CometMotionSnapshot[] comets;
    }

    public static void CaptureFromScene()
    {
        LookAtTarget lookAt = UnityEngine.Object.FindFirstObjectByType<LookAtTarget>();
        if (lookAt == null)
        {
            Debug.LogWarning("[SimulationSessionState] LookAtTarget not found; session state not captured.");
            return;
        }

        SelectedPlanetName = lookAt.currentTarget != null ? lookAt.currentTarget.name : "Earth";
        IsDetailCamera = lookAt.mainCamera != null && !lookAt.mainCamera.enabled;
        SpeedMultiplier = SimulationTimeController.SpeedMultiplier;
        IsSimulationPaused = SimulationTimeController.IsPaused;
        TimeScale = SpeedMultiplier;

        Transform observedTarget = lookAt.currentTarget != null ? lookAt.currentTarget.transform : null;

        float mainCamX, mainCamY, mainCamDistance;
        CaptureOrbitFromCamera(
            lookAt.mainCamera != null ? lookAt.mainCamera.gameObject : null,
            observedTarget,
            out mainCamX,
            out mainCamY,
            out mainCamDistance);
        MainCamX = mainCamX;
        MainCamY = mainCamY;
        MainCamDistance = mainCamDistance;

        GameObject activeDetailCamera = lookAt.GetActiveDetailCamera();
        if (activeDetailCamera != null)
        {
            float detailCamX, detailCamY, detailCamDistance;
            CaptureOrbitFromCamera(activeDetailCamera, observedTarget, out detailCamX, out detailCamY, out detailCamDistance);
            DetailCamX = detailCamX;
            DetailCamY = detailCamY;
            DetailCamDistance = detailCamDistance;
        }

        GameObject activeCamera = GetActiveCameraGameObject(lookAt);
        if (activeCamera != null && observedTarget != null)
        {
            ActiveCameraDistance = Vector3.Distance(activeCamera.transform.position, observedTarget.position);

            if (IsDetailCamera)
                DetailCamDistance = ActiveCameraDistance;
            else
                MainCamDistance = ActiveCameraDistance;
        }
        else
        {
            ActiveCameraDistance = IsDetailCamera ? DetailCamDistance : MainCamDistance;
        }

        CaptureMotionFromScene();

        HasSavedState = true;
        Save();
    }

    static void CaptureMotionFromScene()
    {
        var bodySnapshots = new List<BodyMotionSnapshot>();

        for (int i = 0; i < SolarSystemCatalog.Bodies.Length; i++)
        {
            SolarSystemCatalog.BodyDefinition definition = SolarSystemCatalog.Bodies[i];
            if (definition.objectName == "Sun")
                continue;

            GameObject bodyGo = GameObject.Find(definition.objectName);
            if (bodyGo == null)
                continue;

            var orbitController = bodyGo.GetComponent<BodyOrbitController>();
            var snapshot = new BodyMotionSnapshot { name = definition.objectName };

            if (orbitController != null && orbitController.enabled)
            {
                snapshot.usePhase = true;
                snapshot.phaseRad = orbitController.PhaseRad;
            }
            else
            {
                snapshot.usePhase = false;
                Vector3 pos = bodyGo.transform.position;
                snapshot.posX = pos.x;
                snapshot.posY = pos.y;
                snapshot.posZ = pos.z;
            }

            bodySnapshots.Add(snapshot);
        }

        var cometSnapshots = new List<CometMotionSnapshot>();
        CometOrbitController[] cometOrbits = UnityEngine.Object.FindObjectsByType<CometOrbitController>(FindObjectsSortMode.None);
        for (int i = 0; i < cometOrbits.Length; i++)
        {
            CometOrbitController orbit = cometOrbits[i];
            if (orbit == null)
                continue;

            cometSnapshots.Add(new CometMotionSnapshot
            {
                name = orbit.gameObject.name,
                angle = orbit.OrbitAngle
            });
        }

        _motionSnapshot = new MotionSnapshot
        {
            bodies = bodySnapshots.ToArray(),
            comets = cometSnapshots.ToArray()
        };
    }

    public static void Save()
    {
        PlayerPrefs.SetInt(KeyHasSessionState, HasSavedState ? 1 : 0);
        PlayerPrefs.SetString(KeySelectedPlanet, SelectedPlanetName ?? "Earth");
        PlayerPrefs.SetInt(KeyIsDetailCamera, IsDetailCamera ? 1 : 0);
        PlayerPrefs.SetFloat(KeyTimeScale, TimeScale);
        PlayerPrefs.SetFloat(KeySpeedMultiplier, SpeedMultiplier);
        PlayerPrefs.SetInt(KeyIsPaused, IsSimulationPaused ? 1 : 0);
        PlayerPrefs.SetFloat(KeyMainCamX, MainCamX);
        PlayerPrefs.SetFloat(KeyMainCamY, MainCamY);
        PlayerPrefs.SetFloat(KeyMainCamDistance, MainCamDistance);
        PlayerPrefs.SetFloat(KeyDetailCamX, DetailCamX);
        PlayerPrefs.SetFloat(KeyDetailCamY, DetailCamY);
        PlayerPrefs.SetFloat(KeyDetailCamDistance, DetailCamDistance);
        PlayerPrefs.SetFloat(KeyActiveCamDistance, ActiveCameraDistance);

        if (_motionSnapshot != null)
        {
            PlayerPrefs.SetString(KeyMotionSnapshot, JsonUtility.ToJson(_motionSnapshot));
        }

        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(KeyHasSessionState);
        PlayerPrefs.DeleteKey(KeySelectedPlanet);
        PlayerPrefs.DeleteKey(KeyIsDetailCamera);
        PlayerPrefs.DeleteKey(KeyTimeScale);
        PlayerPrefs.DeleteKey(KeySpeedMultiplier);
        PlayerPrefs.DeleteKey(KeyIsPaused);
        PlayerPrefs.DeleteKey(KeyMainCamX);
        PlayerPrefs.DeleteKey(KeyMainCamY);
        PlayerPrefs.DeleteKey(KeyMainCamDistance);
        PlayerPrefs.DeleteKey(KeyDetailCamX);
        PlayerPrefs.DeleteKey(KeyDetailCamY);
        PlayerPrefs.DeleteKey(KeyDetailCamDistance);
        PlayerPrefs.DeleteKey(KeyActiveCamDistance);
        PlayerPrefs.DeleteKey(KeyMotionSnapshot);
        PlayerPrefs.Save();

        HasSavedState = false;
        SelectedPlanetName = "Earth";
        IsDetailCamera = false;
        TimeScale = 1f;
        SpeedMultiplier = SimulationTimeController.DefaultSpeedMultiplier;
        IsSimulationPaused = false;
        SimulationTimeController.ResetToDefaults(apply: false);
        MainCamX = 0f;
        MainCamY = 0f;
        MainCamDistance = 8f;
        DetailCamX = 0f;
        DetailCamY = 0f;
        DetailCamDistance = 4.5f;
        ActiveCameraDistance = 8f;
        _motionSnapshot = null;
        PendingLaunchMode = SimulationLaunchMode.New;
    }

    public static void Load()
    {
        HasSavedState = PlayerPrefs.GetInt(KeyHasSessionState, 0) == 1;
        SelectedPlanetName = PlayerPrefs.GetString(KeySelectedPlanet, "Earth");
        IsDetailCamera = PlayerPrefs.GetInt(KeyIsDetailCamera, 0) == 1;
        TimeScale = PlayerPrefs.GetFloat(KeyTimeScale, 1f);
        if (PlayerPrefs.HasKey(KeySpeedMultiplier))
        {
            SpeedMultiplier = SimulationTimeController.ClampSpeed(PlayerPrefs.GetFloat(KeySpeedMultiplier, TimeScale));
        }
        else
        {
            SpeedMultiplier = SimulationTimeController.ClampSpeed(TimeScale);
        }

        IsSimulationPaused = PlayerPrefs.GetInt(KeyIsPaused, 0) == 1;
        MainCamX = PlayerPrefs.GetFloat(KeyMainCamX, 0f);
        MainCamY = PlayerPrefs.GetFloat(KeyMainCamY, 0f);
        MainCamDistance = PlayerPrefs.GetFloat(KeyMainCamDistance, 8f);
        DetailCamX = PlayerPrefs.GetFloat(KeyDetailCamX, 0f);
        DetailCamY = PlayerPrefs.GetFloat(KeyDetailCamY, 0f);
        DetailCamDistance = PlayerPrefs.GetFloat(KeyDetailCamDistance, 4.5f);
        ActiveCameraDistance = PlayerPrefs.GetFloat(KeyActiveCamDistance, MainCamDistance);

        if (TimeScale <= 0f)
            TimeScale = SimulationTimeController.DefaultSpeedMultiplier;

        SpeedMultiplier = SimulationTimeController.ClampSpeed(SpeedMultiplier);
        TimeScale = SpeedMultiplier;

        string motionJson = PlayerPrefs.GetString(KeyMotionSnapshot, string.Empty);
        if (!string.IsNullOrEmpty(motionJson))
        {
            _motionSnapshot = JsonUtility.FromJson<MotionSnapshot>(motionJson);
        }
        else
        {
            _motionSnapshot = null;
        }
    }

    public static void RestoreMotionState()
    {
        Load();

        if (!HasSavedState || _motionSnapshot == null)
        {
            return;
        }

        BodyOrbitSystemController bodyOrbitSystem = UnityEngine.Object.FindFirstObjectByType<BodyOrbitSystemController>();
        if (bodyOrbitSystem != null && _motionSnapshot.bodies != null)
        {
            var phases = new Dictionary<string, float>();
            var positions = new Dictionary<string, Vector3>();

            for (int i = 0; i < _motionSnapshot.bodies.Length; i++)
            {
                BodyMotionSnapshot body = _motionSnapshot.bodies[i];
                if (body.usePhase)
                {
                    phases[body.name] = body.phaseRad;
                }
                else
                {
                    positions[body.name] = new Vector3(body.posX, body.posY, body.posZ);
                }
            }

            bodyOrbitSystem.RestoreSavedMotion(phases, positions);
        }
        else if (_motionSnapshot.bodies != null)
        {
            for (int i = 0; i < _motionSnapshot.bodies.Length; i++)
            {
                BodyMotionSnapshot body = _motionSnapshot.bodies[i];
                GameObject bodyGo = GameObject.Find(body.name);
                if (bodyGo == null)
                    continue;

                if (body.usePhase)
                {
                    BodyOrbitController orbit = bodyGo.GetComponent<BodyOrbitController>();
                    if (orbit != null && orbit.enabled)
                        orbit.SetPhaseRad(body.phaseRad);
                }
                else
                {
                    bodyGo.transform.position = new Vector3(body.posX, body.posY, body.posZ);
                }
            }
        }

        CometSystemController cometSystem = UnityEngine.Object.FindFirstObjectByType<CometSystemController>();
        if (cometSystem != null && _motionSnapshot.comets != null)
        {
            var angles = new Dictionary<string, float>();
            for (int i = 0; i < _motionSnapshot.comets.Length; i++)
            {
                CometMotionSnapshot comet = _motionSnapshot.comets[i];
                angles[comet.name] = comet.angle;
            }

            cometSystem.RestoreSavedAngles(angles);
        }
    }

    public static void RestoreToScene()
    {
        Load();

        if (!HasSavedState)
        {
            return;
        }

        LookAtTarget lookAt = UnityEngine.Object.FindFirstObjectByType<LookAtTarget>();
        if (lookAt == null)
        {
            Debug.LogWarning("[SimulationSessionState] LookAtTarget not found; session state not restored.");
            return;
        }

        GameObject planetGo = GameObject.Find(SelectedPlanetName);
        if (planetGo == null)
        {
            Debug.LogWarning($"[SimulationSessionState] Planet '{SelectedPlanetName}' not found; falling back to Earth.");
            SelectedPlanetName = "Earth";
            IsDetailCamera = false;
        }

        bool useDetailCamera = IsDetailCamera && SelectedPlanetName != "Sun";
        lookAt.FocusPlanet(SelectedPlanetName, useDetailCamera, showDescription: true);

        Transform observedTarget = lookAt.currentTarget != null
            ? lookAt.currentTarget.transform
            : null;

        float restoreDistance = ActiveCameraDistance > 0f
            ? ActiveCameraDistance
            : (useDetailCamera ? DetailCamDistance : MainCamDistance);

        if (lookAt.mainCamera != null)
        {
            MobileOrbitCamera mainOrbit = lookAt.mainCamera.GetComponent<MobileOrbitCamera>();
            if (mainOrbit != null)
            {
                if (observedTarget != null)
                    mainOrbit.target = observedTarget;

                float distance = useDetailCamera ? MainCamDistance : restoreDistance;
                mainOrbit.ApplyOrbitState(MainCamX, MainCamY, distance);
            }
        }

        if (useDetailCamera)
        {
            GameObject activeDetailCamera = lookAt.GetActiveDetailCamera();
            if (activeDetailCamera != null)
            {
                MobileOrbitCamera detailOrbit = activeDetailCamera.GetComponent<MobileOrbitCamera>();
                if (detailOrbit != null)
                {
                    if (observedTarget != null)
                        detailOrbit.target = observedTarget;

                    detailOrbit.ApplyOrbitState(DetailCamX, DetailCamY, restoreDistance);
                }
            }
        }

        SimulationTimeController.RestoreState(SpeedMultiplier, IsSimulationPaused);
    }

    static GameObject GetActiveCameraGameObject(LookAtTarget lookAt)
    {
        if (lookAt == null)
            return null;

        if (lookAt.mainCamera != null && lookAt.mainCamera.enabled)
            return lookAt.mainCamera.gameObject;

        return lookAt.GetActiveDetailCamera();
    }

    private static void CaptureOrbitFromCamera(
        GameObject cameraObject,
        Transform observedTarget,
        out float orbitX,
        out float orbitY,
        out float orbitDistance)
    {
        orbitX = 0f;
        orbitY = 0f;
        orbitDistance = 8f;

        if (cameraObject == null)
        {
            return;
        }

        MobileOrbitCamera orbitCam = cameraObject.GetComponent<MobileOrbitCamera>();
        if (orbitCam != null)
        {
            if (observedTarget != null)
                orbitCam.target = observedTarget;

            orbitCam.SyncOrbitFromTransform();
            orbitCam.GetOrbitState(out orbitX, out orbitY, out orbitDistance);

            if (observedTarget != null)
            {
                orbitDistance = Vector3.Distance(cameraObject.transform.position, observedTarget.position);
            }
        }
    }
}
