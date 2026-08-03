using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using SolarSystemApp;

public class LookAtTarget : MonoBehaviour {

    public static event Action<GameObject> OnTargetChanged;

    [Tooltip("This is the object that the script's game object will look at by default")]
    public GameObject defaultTarget; // the default target that the camera should look at

    [Tooltip("This is the object that the script's game object is currently look at based on the player clicking on a gameObject")]
    public GameObject currentTarget; // the target that the camera should look at

    public GameObject myCanvasGameObject;

    public GameObject theSunGameObject;

    public GameObject theEarthGameObject;

    public GameObject theMoonGameObject;

    public GameObject theMarsGameObject;

    public GameObject theMercuryGameObject;

    public GameObject theVenusGameObject;
    //
    public GameObject theJupiterGameObject;

    public GameObject theSaturnGameObject;

    public GameObject theTitanGameObject;

    public GameObject theGanymedeGameObject;

    public GameObject theIoGameObject;

    public GameObject theEuropaGameObject;

    public GameObject theCallistoGameObject;

    public GameObject thePhobosGameObject;

    public GameObject theDeimosGameObject;

    public GameObject theUranusGameObject;

    public GameObject theNeptuneGameObject;

    public GameObject theTritonGameObject;

    public GameObject thePlutoGameObject;

    public Camera mainCamera;      // Главная камера (первоначальная)
    public GameObject earthCamera;    // Детальная камера Земли (которая появляется при фокусировке на объект)
    public GameObject marsCamera;    // Детальная камера Марса

    public GameObject neptuneCamera;    // Детальная камера Марса 
    public GameObject uranusCamera;    // Детальная камера Урана
    public GameObject saturnCamera;    // Детальная камера Сатурна
    public GameObject jupiterCamera;    // Детальная камера Юпитера
    public GameObject venusCamera;    // Детальная камера Венеры
    public GameObject mercuryCamera;    // Детальная камера Меркурия
    public GameObject moonCamera;    // Детальная камера Луны
    public GameObject titanCamera;    // Детальная камера Титана
    public GameObject ganymedeCamera;    // Детальная камера Ганимеда
    public GameObject ioCamera;    // Детальная камера Ио
    public GameObject europaCamera;    // Детальная камера Европы
    public GameObject callistoCamera;    // Детальная камера Каллисто
    public GameObject phobosCamera;    // Детальная камера Фобоса
    public GameObject deimosCamera;    // Детальная камера Деймоса
    public GameObject tritonCamera;    // Детальная камера Тритона
    public GameObject plutoCamera;    // Детальная камера Плутона

    MobileOrbitCamera _mainOrbitCamera;
    GameObject lastObservationTarget;

    private void Awake()
    {
        TouchInputBridge.EnsureInitialized();
    }

    void Start () {
		if (defaultTarget == null) 
		{
            defaultTarget = this.gameObject;
			Debug.Log ("defaultTarget target not specified. Defaulting to parent GameObject");
		}

        if (currentTarget == null)
        {
            currentTarget = this.gameObject;
            Debug.Log("currentTarget target not specified. Defaulting to parent GameObject");
        }

        if (mainCamera != null)
            _mainOrbitCamera = mainCamera.GetComponent<MobileOrbitCamera>();

        WireDescriptionCloseButtons();
        EnsureDescriptionPanelLayouts();
    }
	
    public void HideAllDescriptions()
    {
        MakeAllDescriptionsInvisible();
    }

    /// <summary>
    /// Hides encyclopedia panels without changing camera focus (detail/main stays as FocusPlanet set it).
    /// </summary>
    public void CloseActiveDescription()
    {
        MakeAllDescriptionsInvisible();
    }

    void MakeAllDescriptionsInvisible()
    {
        SetDescriptionActive(theEarthGameObject, false);
        SetDescriptionActive(theMoonGameObject, false);
        SetDescriptionActive(theMarsGameObject, false);
        SetDescriptionActive(theMercuryGameObject, false);
        SetDescriptionActive(theVenusGameObject, false);
        SetDescriptionActive(theJupiterGameObject, false);
        SetDescriptionActive(theSaturnGameObject, false);
        SetDescriptionActive(theTitanGameObject, false);
        SetDescriptionActive(theGanymedeGameObject, false);
        SetDescriptionActive(theIoGameObject, false);
        SetDescriptionActive(theEuropaGameObject, false);
        SetDescriptionActive(theCallistoGameObject, false);
        SetDescriptionActive(thePhobosGameObject, false);
        SetDescriptionActive(theDeimosGameObject, false);
        SetDescriptionActive(theUranusGameObject, false);
        SetDescriptionActive(theNeptuneGameObject, false);
        SetDescriptionActive(theTritonGameObject, false);
        SetDescriptionActive(thePlutoGameObject, false);
        SetDescriptionActive(theSunGameObject, false);

        if (CometDescriptionPanel.Instance != null)
            CometDescriptionPanel.Instance.Hide();
    }

    static void SetDescriptionActive(GameObject description, bool active)
    {
        if (description != null)
            description.SetActive(active);
    }

    void MakeDescriptionVisible(GameObject planet)
    {
        if (planet == null)
            return;

        planet.SetActive(true);
        var layout = planet.GetComponent<BodyDescriptionPanelLayout>();
        if (layout != null)
            layout.ApplyLayout();
    }

    void WireDescriptionCloseButtons()
    {
        WireCloseButton(theEarthGameObject);
        WireCloseButton(theMoonGameObject);
        WireCloseButton(theMarsGameObject);
        WireCloseButton(theMercuryGameObject);
        WireCloseButton(theVenusGameObject);
        WireCloseButton(theJupiterGameObject);
        WireCloseButton(theSaturnGameObject);
        WireCloseButton(theTitanGameObject);
        WireCloseButton(theGanymedeGameObject);
        WireCloseButton(theIoGameObject);
        WireCloseButton(theEuropaGameObject);
        WireCloseButton(theCallistoGameObject);
        WireCloseButton(thePhobosGameObject);
        WireCloseButton(theDeimosGameObject);
        WireCloseButton(theUranusGameObject);
        WireCloseButton(theNeptuneGameObject);
        WireCloseButton(theTritonGameObject);
        WireCloseButton(thePlutoGameObject);
        WireCloseButton(theSunGameObject);
    }

    void EnsureDescriptionPanelLayouts()
    {
        EnsureDescriptionPanelLayout(theEarthGameObject);
        EnsureDescriptionPanelLayout(theMoonGameObject);
        EnsureDescriptionPanelLayout(theMarsGameObject);
        EnsureDescriptionPanelLayout(theMercuryGameObject);
        EnsureDescriptionPanelLayout(theVenusGameObject);
        EnsureDescriptionPanelLayout(theJupiterGameObject);
        EnsureDescriptionPanelLayout(theSaturnGameObject);
        EnsureDescriptionPanelLayout(theTitanGameObject);
        EnsureDescriptionPanelLayout(theGanymedeGameObject);
        EnsureDescriptionPanelLayout(theIoGameObject);
        EnsureDescriptionPanelLayout(theEuropaGameObject);
        EnsureDescriptionPanelLayout(theCallistoGameObject);
        EnsureDescriptionPanelLayout(thePhobosGameObject);
        EnsureDescriptionPanelLayout(theDeimosGameObject);
        EnsureDescriptionPanelLayout(theUranusGameObject);
        EnsureDescriptionPanelLayout(theNeptuneGameObject);
        EnsureDescriptionPanelLayout(theTritonGameObject);
        EnsureDescriptionPanelLayout(thePlutoGameObject);
        EnsureDescriptionPanelLayout(theSunGameObject);
    }

    static void EnsureDescriptionPanelLayout(GameObject descriptionRoot)
    {
        if (descriptionRoot == null)
            return;

        if (descriptionRoot.GetComponent<BodyDescriptionPanelLayout>() == null)
            descriptionRoot.AddComponent<BodyDescriptionPanelLayout>();
    }

    void WireCloseButton(GameObject descriptionRoot)
    {
        if (descriptionRoot == null)
            return;

        Transform closeTransform = descriptionRoot.transform.Find("Close Button");
        if (closeTransform == null)
            return;

        Button closeButton = closeTransform.GetComponent<Button>();
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(CloseActiveDescription);
    }


    void Update()
    {
        if (_mainOrbitCamera == null && mainCamera != null)
            _mainOrbitCamera = mainCamera.GetComponent<MobileOrbitCamera>();

        bool userControlsOrbit = _mainOrbitCamera != null && _mainOrbitCamera.IsUserControlling;
        bool touchActive = TouchInputBridge.touchCount > 0;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (!userControlsOrbit && !touchActive)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUi(-1))
                    return;

            if (SimulationViewSettings.UseFreeObservation)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit) && TryFocusCometFromHit(hit))
                    return;

                TryPlaceMainCameraAtScreenPoint(Input.mousePosition);
            }
                else
                {
                    Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                    bool rayHit = Physics.Raycast(ray, out RaycastHit hit);

                    if (rayHit && TryFocusCometFromHit(hit))
                        return;

                    GameObject picked = BodyPickUtility.PickBody(mainCamera, Input.mousePosition);
                    if (picked == null && rayHit)
                        picked = hit.collider.gameObject;

                    if (picked != null)
                    {
                        StopActiveShowcase();
                        currentTarget = picked;
                        RecordObservationTarget(picked);
                        bool useDetailCamera = picked.name != "Sun";
                        FocusPlanet(picked.name, useDetailCamera, showDescription: true);
                    }
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                currentTarget = defaultTarget;
            }
        }
#endif

        if (userControlsOrbit || touchActive)
            return;

        if (currentTarget != null)
        {
            transform.LookAt(currentTarget.transform);
        }
        else
        {
            currentTarget = defaultTarget;
        }
    }

    public void FocusPlanet(string planetName, bool useDetailCamera, bool showDescription = true)
    {
        GameObject planetGo = GameObject.Find(planetName);
        if (planetGo == null)
        {
            Debug.LogWarning($"FocusPlanet: '{planetName}' not found in scene.");
            return;
        }

        StopActiveShowcase();
        currentTarget = planetGo;
        RecordObservationTarget(planetGo);
        NotifyTargetChanged(planetGo);
        MakeAllDescriptionsInvisible();
        TurnOffAllDetailCameras();

        if (planetName == "Sun" || !useDetailCamera)
        {
            TurnOnMainCamera();
        }
        else
        {
            TurnOffMainCamera();
            TurnOnDetailCameraForPlanet(planetName);
        }

        if (showDescription)
        {
            ShowDescriptionForPlanet(planetName);
        }

        if (planetName == "Sun" || !useDetailCamera)
        {
            var scaleController = FindFirstObjectByType<SolarSystemScaleController>();
            if (scaleController != null)
                scaleController.RefreshMainCameraLimits();
        }
    }

    public void FocusComet(GameObject comet, bool showDescription = true)
    {
        if (comet == null)
            return;

        var info = comet.GetComponent<CometInfo>();
        if (info == null)
            info = comet.GetComponentInParent<CometInfo>();
        if (info == null)
            return;

        StopActiveShowcase();
        GameObject cometRoot = info.gameObject;
        currentTarget = cometRoot;
        NotifyTargetChanged(cometRoot);
        MakeAllDescriptionsInvisible();
        TurnOffAllDetailCameras();
        TurnOnMainCamera();

        if (_mainOrbitCamera == null && mainCamera != null)
            _mainOrbitCamera = mainCamera.GetComponent<MobileOrbitCamera>();

        if (_mainOrbitCamera != null)
        {
            _mainOrbitCamera.target = cometRoot.transform;
            AlignMainCameraBehindComet(cometRoot.transform);
        }

        var scaleController = FindFirstObjectByType<SolarSystemScaleController>();
        if (scaleController != null)
            scaleController.RefreshMainCameraLimits(resetDistance: true);

        if (showDescription)
            ShowCometDescription(info);
    }

    public bool TryFocusCometFromHit(RaycastHit hit)
    {
        if (hit.collider == null)
            return false;

        var info = hit.collider.GetComponentInParent<CometInfo>();
        if (info == null)
            return false;

        FocusComet(info.gameObject, showDescription: true);
        return true;
    }

    void AlignMainCameraBehindComet(Transform comet)
    {
        if (_mainOrbitCamera == null || comet == null)
            return;

        Vector3 forward = comet.forward;
        if (forward.sqrMagnitude < 0.0001f)
        {
            var sun = GameObject.Find("Sun");
            Vector3 sunPos = sun != null ? sun.transform.position : Vector3.zero;
            forward = (comet.position - sunPos).normalized;
        }

        float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        _mainOrbitCamera.ApplyOrbitState(yaw + 180f, 12f, Mathf.Max(_mainOrbitCamera.minDistance, 2.5f));
    }

    void ShowCometDescription(CometInfo info)
    {
        if (info == null)
            return;

        if (CometDescriptionPanel.Instance == null && myCanvasGameObject != null)
            CometDescriptionPanel.EnsureOnCanvas(myCanvasGameObject.transform);

        if (CometDescriptionPanel.Instance != null)
            CometDescriptionPanel.Instance.Show(info);
    }

    public static bool IsCometObject(GameObject go)
    {
        if (go == null)
            return false;
        return go.GetComponent<CometInfo>() != null || go.GetComponentInParent<CometInfo>() != null;
    }

    public void TurnOffAllDetailCameras()
    {
        if (earthCamera) earthCamera.SetActive(false);
        if (moonCamera) moonCamera.SetActive(false);
        if (marsCamera) marsCamera.SetActive(false);
        if (mercuryCamera) mercuryCamera.SetActive(false);
        if (venusCamera) venusCamera.SetActive(false);
        if (jupiterCamera) jupiterCamera.SetActive(false);
        if (saturnCamera) saturnCamera.SetActive(false);
        if (titanCamera) titanCamera.SetActive(false);
        if (ganymedeCamera) ganymedeCamera.SetActive(false);
        if (ioCamera) ioCamera.SetActive(false);
        if (europaCamera) europaCamera.SetActive(false);
        if (callistoCamera) callistoCamera.SetActive(false);
        if (phobosCamera) phobosCamera.SetActive(false);
        if (deimosCamera) deimosCamera.SetActive(false);
        if (uranusCamera) uranusCamera.SetActive(false);
        if (neptuneCamera) neptuneCamera.SetActive(false);
        if (tritonCamera) tritonCamera.SetActive(false);
        if (plutoCamera) plutoCamera.SetActive(false);
    }

    private void ShowDescriptionForPlanet(string planetName)
    {
        if (planetName == "Sun" && theSunGameObject) MakeDescriptionVisible(theSunGameObject);
        else if (planetName == "Earth" && theEarthGameObject) MakeDescriptionVisible(theEarthGameObject);
        else if (planetName == "Moon" && theMoonGameObject) MakeDescriptionVisible(theMoonGameObject);
        else if (planetName == "Mars" && theMarsGameObject) MakeDescriptionVisible(theMarsGameObject);
        else if (planetName == "Mercury" && theMercuryGameObject) MakeDescriptionVisible(theMercuryGameObject);
        else if (planetName == "Venus" && theVenusGameObject) MakeDescriptionVisible(theVenusGameObject);
        else if (planetName == "Jupiter" && theJupiterGameObject) MakeDescriptionVisible(theJupiterGameObject);
        else if (planetName == "Saturn" && theSaturnGameObject) MakeDescriptionVisible(theSaturnGameObject);
        else if (planetName == "Titan" && theTitanGameObject) MakeDescriptionVisible(theTitanGameObject);
        else if (planetName == "Ganymede" && theGanymedeGameObject) MakeDescriptionVisible(theGanymedeGameObject);
        else if (planetName == "Io" && theIoGameObject) MakeDescriptionVisible(theIoGameObject);
        else if (planetName == "Europa" && theEuropaGameObject) MakeDescriptionVisible(theEuropaGameObject);
        else if (planetName == "Callisto" && theCallistoGameObject) MakeDescriptionVisible(theCallistoGameObject);
        else if (planetName == "Phobos" && thePhobosGameObject) MakeDescriptionVisible(thePhobosGameObject);
        else if (planetName == "Deimos" && theDeimosGameObject) MakeDescriptionVisible(theDeimosGameObject);
        else if (planetName == "Uranus" && theUranusGameObject) MakeDescriptionVisible(theUranusGameObject);
        else if (planetName == "Neptune" && theNeptuneGameObject) MakeDescriptionVisible(theNeptuneGameObject);
        else if (planetName == "Triton" && theTritonGameObject) MakeDescriptionVisible(theTritonGameObject);
        else if (planetName == "Pluto" && thePlutoGameObject) MakeDescriptionVisible(thePlutoGameObject);
    }

    private void TurnOnDetailCameraForPlanet(string planetName)
    {
        if (planetName == "Earth") TurnOnEarthCamera();
        else if (planetName == "Moon") TurnOnMoonCamera();
        else if (planetName == "Mars") TurnOnMarsCamera();
        else if (planetName == "Mercury") TurnOnMercuryCamera();
        else if (planetName == "Venus") TurnOnVenusCamera();
        else if (planetName == "Jupiter") TurnOnJupiterCamera();
        else if (planetName == "Saturn") TurnOnSaturnCamera();
        else if (planetName == "Titan") TurnOnTitanCamera();
        else if (planetName == "Ganymede") TurnOnGanymedeCamera();
        else if (planetName == "Io") TurnOnIoCamera();
        else if (planetName == "Europa") TurnOnEuropaCamera();
        else if (planetName == "Callisto") TurnOnCallistoCamera();
        else if (planetName == "Phobos") TurnOnPhobosCamera();
        else if (planetName == "Deimos") TurnOnDeimosCamera();
        else if (planetName == "Uranus") TurnOnUranusCamera();
        else if (planetName == "Neptune") TurnOnNeptuneCamera();
        else if (planetName == "Triton") TurnOnTritonCamera();
        else if (planetName == "Pluto") TurnOnPlutoCamera();
    }

    public GameObject GetActiveDetailCamera()
    {
        if (earthCamera != null && earthCamera.activeSelf) return earthCamera;
        if (moonCamera != null && moonCamera.activeSelf) return moonCamera;
        if (marsCamera != null && marsCamera.activeSelf) return marsCamera;
        if (mercuryCamera != null && mercuryCamera.activeSelf) return mercuryCamera;
        if (venusCamera != null && venusCamera.activeSelf) return venusCamera;
        if (jupiterCamera != null && jupiterCamera.activeSelf) return jupiterCamera;
        if (saturnCamera != null && saturnCamera.activeSelf) return saturnCamera;
        if (titanCamera != null && titanCamera.activeSelf) return titanCamera;
        if (ganymedeCamera != null && ganymedeCamera.activeSelf) return ganymedeCamera;
        if (ioCamera != null && ioCamera.activeSelf) return ioCamera;
        if (europaCamera != null && europaCamera.activeSelf) return europaCamera;
        if (callistoCamera != null && callistoCamera.activeSelf) return callistoCamera;
        if (phobosCamera != null && phobosCamera.activeSelf) return phobosCamera;
        if (deimosCamera != null && deimosCamera.activeSelf) return deimosCamera;
        if (uranusCamera != null && uranusCamera.activeSelf) return uranusCamera;
        if (neptuneCamera != null && neptuneCamera.activeSelf) return neptuneCamera;
        if (tritonCamera != null && tritonCamera.activeSelf) return tritonCamera;
        if (plutoCamera != null && plutoCamera.activeSelf) return plutoCamera;
        return null;
    }

    public void TurnOffMarsCamera()
    {
        marsCamera.SetActive(false);
    }

    public void TurnOnMarsCamera()
    {
        marsCamera.SetActive(true);
    }

    public void TurnOffMainCamera()
    {
        mainCamera.enabled = false;               
    }

    public void TurnOnMainCamera()
    {
        mainCamera.enabled = true;
    }

    public void TurnOffEarthCamera()
    {
        earthCamera.SetActive(false);
    }

    public void TurnOnEarthCamera()
    {
        earthCamera.SetActive(true);
    }

    public void TurnOnNeptuneCamera()
    {
        neptuneCamera.SetActive(true);
    }
    public void TurnOffNeptuneCamera()
    {
        neptuneCamera.SetActive(false);
    }
    public void TurnOnUranusCamera()
    {
        uranusCamera.SetActive(true);
    }
    public void TurnOffUranusCamera()
    {
        uranusCamera.SetActive(false);
    }
    public void TurnOnSaturnCamera()
    {
        saturnCamera.SetActive(true);
    }
    public void TurnOffSaturnCamera()
    {
        saturnCamera.SetActive(false);
    }
    public void TurnOnJupiterCamera()
    {
        jupiterCamera.SetActive(true);
    }

    public void TurnOffJupiterCamera()
    {
        jupiterCamera.SetActive(false);
    }

    public void TurnOnVenusCamera()
    {
        venusCamera.SetActive(true);
    }

    public void TurnOffVenusCamera()
    {
        venusCamera.SetActive(false);
    }

    public void TurnOnMercuryCamera()
    {
        mercuryCamera.SetActive(true);
    }

    public void TurnOffMercuryCamera()
    {
        mercuryCamera.SetActive(false);
    }

    public void TurnOnMoonCamera()
    {
        moonCamera.SetActive(true);
    }

    public void TurnOffMoonCamera()
    {
        moonCamera.SetActive(false);
    }

    public void TurnOnTitanCamera()
    {
        if (titanCamera) titanCamera.SetActive(true);
    }

    public void TurnOffTitanCamera()
    {
        if (titanCamera) titanCamera.SetActive(false);
    }

    public void TurnOnGanymedeCamera()
    {
        if (ganymedeCamera) ganymedeCamera.SetActive(true);
    }

    public void TurnOffGanymedeCamera()
    {
        if (ganymedeCamera) ganymedeCamera.SetActive(false);
    }

    public void TurnOnIoCamera()
    {
        if (ioCamera) ioCamera.SetActive(true);
    }

    public void TurnOffIoCamera()
    {
        if (ioCamera) ioCamera.SetActive(false);
    }

    public void TurnOnEuropaCamera()
    {
        if (europaCamera) europaCamera.SetActive(true);
    }

    public void TurnOffEuropaCamera()
    {
        if (europaCamera) europaCamera.SetActive(false);
    }

    public void TurnOnCallistoCamera()
    {
        if (callistoCamera) callistoCamera.SetActive(true);
    }

    public void TurnOffCallistoCamera()
    {
        if (callistoCamera) callistoCamera.SetActive(false);
    }

    public void TurnOnPhobosCamera()
    {
        if (phobosCamera) phobosCamera.SetActive(true);
    }

    public void TurnOffPhobosCamera()
    {
        if (phobosCamera) phobosCamera.SetActive(false);
    }

    public void TurnOnDeimosCamera()
    {
        if (deimosCamera) deimosCamera.SetActive(true);
    }

    public void TurnOffDeimosCamera()
    {
        if (deimosCamera) deimosCamera.SetActive(false);
    }

    public void TurnOnTritonCamera()
    {
        if (tritonCamera) tritonCamera.SetActive(true);
    }

    public void TurnOffTritonCamera()
    {
        if (tritonCamera) tritonCamera.SetActive(false);
    }

    public void TurnOnPlutoCamera()
    {
        if (plutoCamera) plutoCamera.SetActive(true);
    }

    public void TurnOffPlutoCamera()
    {
        if (plutoCamera) plutoCamera.SetActive(false);
    }

    public void RecordObservationTarget(GameObject body)
    {
        if (body != null && IsCatalogBody(body))
            lastObservationTarget = body;
    }

    public GameObject ResolveObservationTarget()
    {
        if (lastObservationTarget != null)
            return lastObservationTarget;

        if (mainCamera != null)
        {
            GameObject nearest = FindNearestCatalogBody(mainCamera.transform.position);
            if (nearest != null)
                return nearest;
        }

        return defaultTarget != null ? defaultTarget : currentTarget;
    }

    public bool TryPlaceMainCameraAtScreenPoint(Vector2 screenPosition, int pointerId = -1)
    {
        if (!SimulationViewSettings.UseFreeObservation || mainCamera == null)
            return false;

        if (IsPointerOverUi(pointerId))
            return false;

        if (_mainOrbitCamera == null)
            _mainOrbitCamera = mainCamera.GetComponent<MobileOrbitCamera>();

        GameObject focus = ResolveObservationTarget();
        if (focus == null)
            return false;

        TurnOffAllDetailCameras();
        TurnOnMainCamera();

        currentTarget = focus;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (!TryGetPlacementPoint(ray, focus.transform.position, out Vector3 worldPoint))
            return false;

        mainCamera.transform.position = worldPoint;
        Vector3 toFocus = focus.transform.position - worldPoint;
        if (toFocus.sqrMagnitude > 0.0001f)
            mainCamera.transform.rotation = Quaternion.LookRotation(toFocus.normalized, Vector3.up);
        transform.LookAt(focus.transform);

        if (_mainOrbitCamera != null)
        {
            _mainOrbitCamera.target = focus.transform;
            _mainOrbitCamera.SyncOrbitFromTransform();
        }

        var scaleController = FindFirstObjectByType<SolarSystemScaleController>();
        if (scaleController != null)
            scaleController.RefreshMainCameraLimits();

        return true;
    }

    static readonly List<RaycastResult> s_uiRaycastResults = new List<RaycastResult>(8);

    public static bool IsPointerOverUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        var eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
        s_uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, s_uiRaycastResults);
        return s_uiRaycastResults.Count > 0;
    }

    public static bool IsPointerOverUi(int pointerId)
    {
        if (pointerId < 0)
            return IsPointerOverUi(Input.mousePosition);

        int touchCount = TouchInputBridge.touchCount;
        for (int i = 0; i < touchCount; i++)
        {
            TouchInputBridge.TouchSample touch = TouchInputBridge.GetTouch(i);
            if (touch.fingerId == pointerId)
                return IsPointerOverUi(touch.position);
        }

        return false;
    }

    static bool IsCatalogBody(GameObject go)
    {
        return go != null && SolarSystemCatalog.TryGetBody(go.name, out _);
    }

    static GameObject FindNearestCatalogBody(Vector3 fromPoint)
    {
        GameObject nearest = null;
        float minDistSq = float.MaxValue;

        for (int i = 0; i < SolarSystemCatalog.Bodies.Length; i++)
        {
            GameObject bodyGo = GameObject.Find(SolarSystemCatalog.Bodies[i].objectName);
            if (bodyGo == null)
                continue;

            float distSq = (bodyGo.transform.position - fromPoint).sqrMagnitude;
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                nearest = bodyGo;
            }
        }

        return nearest;
    }

    static void NotifyTargetChanged(GameObject target)
    {
        OnTargetChanged?.Invoke(target);
    }

    static void StopActiveShowcase()
    {
        if (Camera.main == null)
            return;

        var showcase = Camera.main.GetComponent<BodyShowcaseCameraController>();
        if (showcase != null)
            showcase.StopShowcase();
    }

    static bool TryGetPlacementPoint(Ray ray, Vector3 focusPosition, out Vector3 worldPoint)
    {
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            worldPoint = hit.point;
            return true;
        }

        var plane = new Plane(ray.direction, focusPosition);
        if (plane.Raycast(ray, out float enter))
        {
            worldPoint = ray.GetPoint(enter);
            return true;
        }

        worldPoint = default;
        return false;
    }
}
