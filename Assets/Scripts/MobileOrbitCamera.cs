using UnityEngine;
using SolarSystemApp;

public class MobileOrbitCamera : MonoBehaviour
{
    [Header("Target Tracking")]
    public Transform target;

    [Header("Distance Settings")]
    public float distance = 25f;
    public float minDistance = 2f;
    public float maxDistance = 500f;

    [Header("Speed Settings")]
    public float xSpeed = 0.15f;
    public float ySpeed = 0.15f;
    public float zoomSpeed = 0.05f;

    [Header("Limits")]
    public float yMinLimit = -85f;
    public float yMaxLimit = 85f;

    [Header("Touch")]
    public float tapSlopPixels = 15f;

    private float x = 0.0f;
    private float y = 0.0f;

    private float lastTapTime = 0f;
    private const float doubleTapDelay = 0.3f;

    private LookAtTarget globalLookAtScript;
    private bool isControlled;

    private Vector2 _activeTouchBeganPosition;
    private bool _trackTapGesture;
    private bool _tapGestureCancelled;

    /// <summary>True while the user is touching the screen (orbit or pinch) this frame.</summary>
    public bool IsUserControlling { get; private set; }

    /// <summary>True while an external controller (e.g. showcase camera) owns orbit input.</summary>
    public bool ShowcaseOverrideActive { get; private set; }

    void Start()
    {
        TouchInputBridge.EnsureInitialized();

        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        globalLookAtScript = FindFirstObjectByType<LookAtTarget>();

        if (target != null)
        {
            distance = Vector3.Distance(transform.position, target.position);
        }
        else if (globalLookAtScript != null && gameObject.name == "Main Camera")
        {
            target = globalLookAtScript.currentTarget != null
                ? globalLookAtScript.currentTarget.transform
                : globalLookAtScript.defaultTarget.transform;
            distance = Vector3.Distance(transform.position, target.position);
        }
    }

    void LateUpdate()
    {
        var cam = GetComponent<Camera>();
        if (cam != null && !cam.enabled)
            return;

        if (gameObject.name == "Main Camera" && globalLookAtScript != null)
        {
            if (globalLookAtScript.currentTarget != null &&
                target != globalLookAtScript.currentTarget.transform)
            {
                target = globalLookAtScript.currentTarget.transform;
                distance = Vector3.Distance(transform.position, target.position);
                var scaleController = FindFirstObjectByType<SolarSystemScaleController>();
                if (scaleController != null)
                    scaleController.RefreshMainCameraLimits();
            }
        }

        if (target == null)
            return;

        isControlled = false;
        IsUserControlling = false;

        if (ShowcaseOverrideActive)
        {
            DetectUserOverrideDuringShowcase();
            return;
        }

        int touchCount = TouchInputBridge.touchCount;
        if (touchCount > 0)
        {
            if (touchCount == 1)
            {
                TouchInputBridge.TouchSample touch = TouchInputBridge.GetTouch(0);
                if (!LookAtTarget.IsPointerOverUi(touch.position))
                {
                    isControlled = true;
                    IsUserControlling = true;
                    HandleSingleFingerTouch(touch);
                }
            }
            else if (touchCount == 2)
            {
                TouchInputBridge.TouchSample touchZero = TouchInputBridge.GetTouch(0);
                TouchInputBridge.TouchSample touchOne = TouchInputBridge.GetTouch(1);
                bool pinchOverUi = LookAtTarget.IsPointerOverUi(touchZero.position)
                    || LookAtTarget.IsPointerOverUi(touchOne.position);

                if (!pinchOverUi)
                {
                    isControlled = true;
                    IsUserControlling = true;
                    _trackTapGesture = false;
                    HandlePinchZoom(touchZero, touchOne);
                }
            }
        }

        if (touchCount == 0 && Application.isEditor)
        {
            bool pointerOverUi = LookAtTarget.IsPointerOverUi(Input.mousePosition);

            if (!pointerOverUi && Input.GetMouseButton(0))
            {
                isControlled = true;
                IsUserControlling = true;
                x += Input.GetAxis("Mouse X") * xSpeed * 20f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 20f;
                y = ClampAngle(y, yMinLimit, yMaxLimit);
            }

            if (!pointerOverUi)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    isControlled = true;
                    IsUserControlling = true;
                    distance -= scroll * zoomSpeed * 300f;
                    distance = Mathf.Clamp(distance, minDistance, maxDistance);
                }
            }
        }

        if (isControlled)
        {
            ApplyOrbitTransform();
        }
        else if (gameObject.name == "Main Camera")
        {
            Vector3 angles = transform.eulerAngles;
            x = angles.y;
            y = angles.x;
        }
        else
        {
            ApplyOrbitTransform();
        }
    }

    void HandleSingleFingerTouch(TouchInputBridge.TouchSample touch)
    {
        if (touch.phase == TouchPhase.Began)
        {
            SyncOrbitFromTransform();
            _activeTouchBeganPosition = touch.position;
            _trackTapGesture = true;
            _tapGestureCancelled = false;
        }

        if (_trackTapGesture &&
            (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
        {
            if ((touch.position - _activeTouchBeganPosition).sqrMagnitude > tapSlopPixels * tapSlopPixels)
                _tapGestureCancelled = true;
        }

        if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
        {
            Vector2 delta = touch.deltaPosition;
            if (delta.sqrMagnitude > 0.0001f)
            {
                x += delta.x * xSpeed;
                y -= delta.y * ySpeed;
                y = ClampAngle(y, yMinLimit, yMaxLimit);
            }
        }

        if (_trackTapGesture && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
        {
            _trackTapGesture = false;
            if (!_tapGestureCancelled &&
                (touch.position - _activeTouchBeganPosition).sqrMagnitude <= tapSlopPixels * tapSlopPixels)
            {
                float timeSinceLastTap = Time.unscaledTime - lastTapTime;
                if (timeSinceLastTap <= doubleTapDelay)
                    OnDoubleTap(touch.position);
                else
                    OnSingleTap(touch.position, touch.fingerId);
                lastTapTime = Time.unscaledTime;
            }
        }
    }

    void HandlePinchZoom(TouchInputBridge.TouchSample touchZero, TouchInputBridge.TouchSample touchOne)
    {
        Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
        Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

        float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
        float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;
        float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

        float zoomScale = GetPinchZoomScale();
        distance += deltaMagnitudeDiff * zoomSpeed * zoomScale;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    static float GetPinchZoomScale()
    {
#if UNITY_ANDROID || UNITY_IOS
        float dpi = Screen.dpi > 1f ? Screen.dpi : 160f;
        return Mathf.Max(2f, dpi / 160f);
#else
        return 1f;
#endif
    }

    public void SyncOrbitFromTransform()
    {
        if (target == null)
            return;

        Vector3 offset = transform.position - target.position;
        distance = offset.magnitude;
        if (distance < 0.001f)
            return;

        Quaternion orbitRotation = Quaternion.LookRotation(-offset.normalized, Vector3.up);
        Vector3 euler = orbitRotation.eulerAngles;
        x = euler.y;
        y = euler.x;
        y = ClampAngle(y, yMinLimit, yMaxLimit);
    }

    void ApplyOrbitTransform()
    {
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
        Vector3 position = rotation * negDistance + target.position;
        transform.rotation = rotation;
        transform.position = position;
    }

    private void OnSingleTap(Vector2 screenPosition, int fingerId = -1)
    {
        if (gameObject.name == "Main Camera" && globalLookAtScript != null)
        {
            if (LookAtTarget.IsPointerOverUi(fingerId))
                return;

            if (SimulationViewSettings.UseFreeObservation)
            {
                Ray cometRay = Camera.main.ScreenPointToRay(screenPosition);
                if (Physics.Raycast(cometRay, out RaycastHit cometHit) &&
                    globalLookAtScript.TryFocusCometFromHit(cometHit))
                    return;
            }

            if (SimulationViewSettings.UseFreeObservation &&
                globalLookAtScript.TryPlaceMainCameraAtScreenPoint(screenPosition, fingerId))
                return;

            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            bool rayHit = Physics.Raycast(ray, out RaycastHit hit);

            if (rayHit && globalLookAtScript.TryFocusCometFromHit(hit))
                return;

            GameObject picked = BodyPickUtility.PickBody(Camera.main, screenPosition);
            if (picked == null && rayHit)
                picked = hit.collider.gameObject;

            if (picked != null && picked != globalLookAtScript.currentTarget)
            {
                globalLookAtScript.currentTarget = picked;
                globalLookAtScript.RecordObservationTarget(picked);
                TriggerDescription(picked.name);
            }
        }
    }

    private void OnDoubleTap(Vector2 screenPosition)
    {
        if (globalLookAtScript == null) return;

        if (gameObject.name == "Main Camera")
        {
            if (LookAtTarget.IsPointerOverUi(-1))
                return;

            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            bool rayHit = Physics.Raycast(ray, out RaycastHit hit);

            if (rayHit && globalLookAtScript.TryFocusCometFromHit(hit))
                return;

            GameObject picked = BodyPickUtility.PickBody(Camera.main, screenPosition);
            if (picked == null && rayHit)
                picked = hit.collider.gameObject;

            if (picked != null)
            {
                globalLookAtScript.currentTarget = picked;
                TriggerCameraSwitch(picked.name, true);
            }
        }
        else
        {
            TriggerCameraSwitch(target.gameObject.name, false);
        }
    }

    private void TriggerDescription(string planetName)
    {
        if (globalLookAtScript == null) return;

        var sunDesc = globalLookAtScript.theSunGameObject;
        var earthDesc = globalLookAtScript.theEarthGameObject;
        var moonDesc = globalLookAtScript.theMoonGameObject;
        var marsDesc = globalLookAtScript.theMarsGameObject;
        var mercuryDesc = globalLookAtScript.theMercuryGameObject;
        var venusDesc = globalLookAtScript.theVenusGameObject;
        var jupiterDesc = globalLookAtScript.theJupiterGameObject;
        var saturnDesc = globalLookAtScript.theSaturnGameObject;
        var titanDesc = globalLookAtScript.theTitanGameObject;
        var ganymedeDesc = globalLookAtScript.theGanymedeGameObject;
        var ioDesc = globalLookAtScript.theIoGameObject;
        var europaDesc = globalLookAtScript.theEuropaGameObject;
        var callistoDesc = globalLookAtScript.theCallistoGameObject;
        var phobosDesc = globalLookAtScript.thePhobosGameObject;
        var deimosDesc = globalLookAtScript.theDeimosGameObject;
        var uranusDesc = globalLookAtScript.theUranusGameObject;
        var neptuneDesc = globalLookAtScript.theNeptuneGameObject;
        var tritonDesc = globalLookAtScript.theTritonGameObject;
        var plutoDesc = globalLookAtScript.thePlutoGameObject;

        if (sunDesc) sunDesc.SetActive(planetName == "Sun");
        if (earthDesc) earthDesc.SetActive(planetName == "Earth");
        if (moonDesc) moonDesc.SetActive(planetName == "Moon");
        if (marsDesc) marsDesc.SetActive(planetName == "Mars");
        if (mercuryDesc) mercuryDesc.SetActive(planetName == "Mercury");
        if (venusDesc) venusDesc.SetActive(planetName == "Venus");
        if (jupiterDesc) jupiterDesc.SetActive(planetName == "Jupiter");
        if (saturnDesc) saturnDesc.SetActive(planetName == "Saturn");
        if (titanDesc) titanDesc.SetActive(planetName == "Titan");
        if (ganymedeDesc) ganymedeDesc.SetActive(planetName == "Ganymede");
        if (ioDesc) ioDesc.SetActive(planetName == "Io");
        if (europaDesc) europaDesc.SetActive(planetName == "Europa");
        if (callistoDesc) callistoDesc.SetActive(planetName == "Callisto");
        if (phobosDesc) phobosDesc.SetActive(planetName == "Phobos");
        if (deimosDesc) deimosDesc.SetActive(planetName == "Deimos");
        if (uranusDesc) uranusDesc.SetActive(planetName == "Uranus");
        if (neptuneDesc) neptuneDesc.SetActive(planetName == "Neptune");
        if (tritonDesc) tritonDesc.SetActive(planetName == "Triton");
        if (plutoDesc) plutoDesc.SetActive(planetName == "Pluto");
    }

    private void TriggerCameraSwitch(string planetName, bool toDetail)
    {
        if (globalLookAtScript == null) return;

        if (toDetail)
        {
            globalLookAtScript.FocusPlanet(planetName, useDetailCamera: true, showDescription: true);
        }
        else
        {
            globalLookAtScript.TurnOnMainCamera();
            SetCameraActive(planetName, false);
            globalLookAtScript.currentTarget = globalLookAtScript.defaultTarget;
            TriggerDescription("");
        }
    }

    private void SetCameraActive(string planetName, bool active)
    {
        if (globalLookAtScript == null) return;

        if (planetName == "Earth" && globalLookAtScript.earthCamera) globalLookAtScript.earthCamera.SetActive(active);
        if (planetName == "Moon" && globalLookAtScript.moonCamera) globalLookAtScript.moonCamera.SetActive(active);
        if (planetName == "Mars" && globalLookAtScript.marsCamera) globalLookAtScript.marsCamera.SetActive(active);
        if (planetName == "Mercury" && globalLookAtScript.mercuryCamera) globalLookAtScript.mercuryCamera.SetActive(active);
        if (planetName == "Venus" && globalLookAtScript.venusCamera) globalLookAtScript.venusCamera.SetActive(active);
        if (planetName == "Jupiter" && globalLookAtScript.jupiterCamera) globalLookAtScript.jupiterCamera.SetActive(active);
        if (planetName == "Saturn" && globalLookAtScript.saturnCamera) globalLookAtScript.saturnCamera.SetActive(active);
        if (planetName == "Titan" && globalLookAtScript.titanCamera) globalLookAtScript.titanCamera.SetActive(active);
        if (planetName == "Ganymede" && globalLookAtScript.ganymedeCamera) globalLookAtScript.ganymedeCamera.SetActive(active);
        if (planetName == "Io" && globalLookAtScript.ioCamera) globalLookAtScript.ioCamera.SetActive(active);
        if (planetName == "Europa" && globalLookAtScript.europaCamera) globalLookAtScript.europaCamera.SetActive(active);
        if (planetName == "Callisto" && globalLookAtScript.callistoCamera) globalLookAtScript.callistoCamera.SetActive(active);
        if (planetName == "Phobos" && globalLookAtScript.phobosCamera) globalLookAtScript.phobosCamera.SetActive(active);
        if (planetName == "Deimos" && globalLookAtScript.deimosCamera) globalLookAtScript.deimosCamera.SetActive(active);
        if (planetName == "Uranus" && globalLookAtScript.uranusCamera) globalLookAtScript.uranusCamera.SetActive(active);
        if (planetName == "Neptune" && globalLookAtScript.neptuneCamera) globalLookAtScript.neptuneCamera.SetActive(active);
        if (planetName == "Triton" && globalLookAtScript.tritonCamera) globalLookAtScript.tritonCamera.SetActive(active);
        if (planetName == "Pluto" && globalLookAtScript.plutoCamera) globalLookAtScript.plutoCamera.SetActive(active);
    }

    public void GetOrbitState(out float outX, out float outY, out float outDistance)
    {
        outX = x;
        outY = y;
        outDistance = distance;
    }

    public void ApplyOrbitState(float newX, float newY, float newDistance)
    {
        x = newX;
        y = ClampAngle(newY, yMinLimit, yMaxLimit);
        distance = Mathf.Clamp(newDistance, minDistance, maxDistance);

        if (target == null) return;

        ApplyOrbitTransform();
    }

    public float GetOrbitX() => x;

    public float GetOrbitY() => y;

    public void SetMinDistance(float newMinDistance)
    {
        minDistance = Mathf.Max(0.1f, newMinDistance);
        maxDistance = Mathf.Max(minDistance + 1f, maxDistance);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    public void SetMaxDistance(float newMaxDistance)
    {
        maxDistance = Mathf.Max(minDistance + 1f, newMaxDistance);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    public void SetExternalOrbitControl(bool enabled)
    {
        ShowcaseOverrideActive = enabled;
        if (!enabled)
            IsUserControlling = false;
    }

    void DetectUserOverrideDuringShowcase()
    {
        int touchCount = TouchInputBridge.touchCount;
        if (touchCount > 0)
        {
            TouchInputBridge.TouchSample touch = TouchInputBridge.GetTouch(0);
            if (!LookAtTarget.IsPointerOverUi(touch.position) &&
                (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
            {
                IsUserControlling = true;
                return;
            }
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        if (!LookAtTarget.IsPointerOverUi(Input.mousePosition) && Input.GetMouseButton(0))
            IsUserControlling = true;
#endif
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
