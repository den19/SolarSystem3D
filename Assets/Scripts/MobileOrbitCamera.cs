using UnityEngine;

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
        if (gameObject.name == "Main Camera" && globalLookAtScript != null)
        {
            if (globalLookAtScript.currentTarget != null &&
                target != globalLookAtScript.currentTarget.transform)
            {
                target = globalLookAtScript.currentTarget.transform;
                distance = Vector3.Distance(transform.position, target.position);
            }
        }

        if (target == null)
            return;

        isControlled = false;
        IsUserControlling = false;

        int touchCount = TouchInputBridge.touchCount;
        if (touchCount > 0)
        {
            isControlled = true;
            IsUserControlling = true;

            if (touchCount == 1)
            {
                HandleSingleFingerTouch(TouchInputBridge.GetTouch(0));
            }
            else if (touchCount == 2)
            {
                _trackTapGesture = false;
                HandlePinchZoom(
                    TouchInputBridge.GetTouch(0),
                    TouchInputBridge.GetTouch(1));
            }
        }
        else if (Application.isEditor)
        {
            if (Input.GetMouseButton(0))
            {
                isControlled = true;
                IsUserControlling = true;
                x += Input.GetAxis("Mouse X") * xSpeed * 20f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 20f;
                y = ClampAngle(y, yMinLimit, yMaxLimit);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                isControlled = true;
                IsUserControlling = true;
                distance -= scroll * zoomSpeed * 300f;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
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
                float timeSinceLastTap = Time.time - lastTapTime;
                if (timeSinceLastTap <= doubleTapDelay)
                    OnDoubleTap(touch.position);
                else
                    OnSingleTap(touch.position);
                lastTapTime = Time.time;
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

    void SyncOrbitFromTransform()
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

    private void OnSingleTap(Vector2 screenPosition)
    {
        if (gameObject.name == "Main Camera" && globalLookAtScript != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObject = hit.collider.gameObject;
                if (hitObject != globalLookAtScript.currentTarget)
                {
                    globalLookAtScript.currentTarget = hitObject;
                    TriggerDescription(hitObject.name);
                }
            }
        }
    }

    private void OnDoubleTap(Vector2 screenPosition)
    {
        if (globalLookAtScript == null) return;

        if (gameObject.name == "Main Camera")
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObject = hit.collider.gameObject;
                globalLookAtScript.currentTarget = hitObject;
                TriggerCameraSwitch(hitObject.name, true);
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
        var uranusDesc = globalLookAtScript.theUranusGameObject;
        var neptuneDesc = globalLookAtScript.theNeptuneGameObject;

        if (sunDesc) sunDesc.SetActive(planetName == "Sun");
        if (earthDesc) earthDesc.SetActive(planetName == "Earth");
        if (moonDesc) moonDesc.SetActive(planetName == "Moon");
        if (marsDesc) marsDesc.SetActive(planetName == "Mars");
        if (mercuryDesc) mercuryDesc.SetActive(planetName == "Mercury");
        if (venusDesc) venusDesc.SetActive(planetName == "Venus");
        if (jupiterDesc) jupiterDesc.SetActive(planetName == "Jupiter");
        if (saturnDesc) saturnDesc.SetActive(planetName == "Saturn");
        if (titanDesc) titanDesc.SetActive(planetName == "Titan");
        if (uranusDesc) uranusDesc.SetActive(planetName == "Uranus");
        if (neptuneDesc) neptuneDesc.SetActive(planetName == "Neptune");
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
        if (planetName == "Uranus" && globalLookAtScript.uranusCamera) globalLookAtScript.uranusCamera.SetActive(active);
        if (planetName == "Neptune" && globalLookAtScript.neptuneCamera) globalLookAtScript.neptuneCamera.SetActive(active);
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

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
