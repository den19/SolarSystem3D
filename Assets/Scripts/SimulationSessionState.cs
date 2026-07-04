using UnityEngine;

public static class SimulationSessionState
{
    private const string KeyHasSessionState = "SolarSystem_HasSessionState";
    private const string KeySelectedPlanet = "SolarSystem_SelectedPlanet";
    private const string KeyIsDetailCamera = "SolarSystem_IsDetailCamera";
    private const string KeyTimeScale = "SolarSystem_TimeScale";
    private const string KeyMainCamX = "SolarSystem_MainCamX";
    private const string KeyMainCamY = "SolarSystem_MainCamY";
    private const string KeyMainCamDistance = "SolarSystem_MainCamDistance";
    private const string KeyDetailCamX = "SolarSystem_DetailCamX";
    private const string KeyDetailCamY = "SolarSystem_DetailCamY";
    private const string KeyDetailCamDistance = "SolarSystem_DetailCamDistance";

    public static bool HasSavedState { get; private set; }
    public static string SelectedPlanetName { get; private set; } = "Earth";
    public static bool IsDetailCamera { get; private set; }
    public static float TimeScale { get; private set; } = 1f;
    public static float MainCamX { get; private set; }
    public static float MainCamY { get; private set; }
    public static float MainCamDistance { get; private set; } = 8f;
    public static float DetailCamX { get; private set; }
    public static float DetailCamY { get; private set; }
    public static float DetailCamDistance { get; private set; } = 4.5f;

    public static void CaptureFromScene()
    {
        LookAtTarget lookAt = Object.FindFirstObjectByType<LookAtTarget>();
        if (lookAt == null)
        {
            Debug.LogWarning("[SimulationSessionState] LookAtTarget not found; session state not captured.");
            return;
        }

        SelectedPlanetName = lookAt.currentTarget != null ? lookAt.currentTarget.name : "Earth";
        IsDetailCamera = lookAt.mainCamera != null && !lookAt.mainCamera.enabled;
        TimeScale = UnityEngine.Time.timeScale > 0f ? UnityEngine.Time.timeScale : 1f;

        float mainCamX, mainCamY, mainCamDistance;
        CaptureOrbitFromCamera(lookAt.mainCamera != null ? lookAt.mainCamera.gameObject : null,
            out mainCamX, out mainCamY, out mainCamDistance);
        MainCamX = mainCamX;
        MainCamY = mainCamY;
        MainCamDistance = mainCamDistance;

        GameObject activeDetailCamera = lookAt.GetActiveDetailCamera();
        if (activeDetailCamera != null)
        {
            float detailCamX, detailCamY, detailCamDistance;
            CaptureOrbitFromCamera(activeDetailCamera, out detailCamX, out detailCamY, out detailCamDistance);
            DetailCamX = detailCamX;
            DetailCamY = detailCamY;
            DetailCamDistance = detailCamDistance;
        }

        HasSavedState = true;
        Save();
    }

    public static void Save()
    {
        PlayerPrefs.SetInt(KeyHasSessionState, HasSavedState ? 1 : 0);
        PlayerPrefs.SetString(KeySelectedPlanet, SelectedPlanetName ?? "Earth");
        PlayerPrefs.SetInt(KeyIsDetailCamera, IsDetailCamera ? 1 : 0);
        PlayerPrefs.SetFloat(KeyTimeScale, TimeScale);
        PlayerPrefs.SetFloat(KeyMainCamX, MainCamX);
        PlayerPrefs.SetFloat(KeyMainCamY, MainCamY);
        PlayerPrefs.SetFloat(KeyMainCamDistance, MainCamDistance);
        PlayerPrefs.SetFloat(KeyDetailCamX, DetailCamX);
        PlayerPrefs.SetFloat(KeyDetailCamY, DetailCamY);
        PlayerPrefs.SetFloat(KeyDetailCamDistance, DetailCamDistance);
        PlayerPrefs.Save();
    }

    public static void Load()
    {
        HasSavedState = PlayerPrefs.GetInt(KeyHasSessionState, 0) == 1;
        SelectedPlanetName = PlayerPrefs.GetString(KeySelectedPlanet, "Earth");
        IsDetailCamera = PlayerPrefs.GetInt(KeyIsDetailCamera, 0) == 1;
        TimeScale = PlayerPrefs.GetFloat(KeyTimeScale, 1f);
        MainCamX = PlayerPrefs.GetFloat(KeyMainCamX, 0f);
        MainCamY = PlayerPrefs.GetFloat(KeyMainCamY, 0f);
        MainCamDistance = PlayerPrefs.GetFloat(KeyMainCamDistance, 8f);
        DetailCamX = PlayerPrefs.GetFloat(KeyDetailCamX, 0f);
        DetailCamY = PlayerPrefs.GetFloat(KeyDetailCamY, 0f);
        DetailCamDistance = PlayerPrefs.GetFloat(KeyDetailCamDistance, 4.5f);

        if (TimeScale <= 0f)
        {
            TimeScale = 1f;
        }
    }

    public static void RestoreToScene()
    {
        Load();

        if (!HasSavedState)
        {
            return;
        }

        LookAtTarget lookAt = Object.FindFirstObjectByType<LookAtTarget>();
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

        if (lookAt.mainCamera != null)
        {
            MobileOrbitCamera mainOrbit = lookAt.mainCamera.GetComponent<MobileOrbitCamera>();
            if (mainOrbit != null)
            {
                mainOrbit.ApplyOrbitState(MainCamX, MainCamY, MainCamDistance);
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
                    detailOrbit.ApplyOrbitState(DetailCamX, DetailCamY, DetailCamDistance);
                }
            }
        }

        UnityEngine.Time.timeScale = TimeScale;
    }

    private static void CaptureOrbitFromCamera(GameObject cameraObject, out float orbitX, out float orbitY, out float orbitDistance)
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
            orbitCam.GetOrbitState(out orbitX, out orbitY, out orbitDistance);
        }
    }
}
