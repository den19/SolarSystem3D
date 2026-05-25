using UnityEngine;

public class MobileOrbitCamera : MonoBehaviour
{
    [Header("Target Tracking")]
    public Transform target; // Объект вращения (планета или солнце)
    
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

    private float x = 0.0f;
    private float y = 0.0f;

    private float lastTapTime = 0f;
    private const float doubleTapDelay = 0.3f;
    
    private LookAtTarget globalLookAtScript;
    private bool isControlled = false;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        // Находим главный скрипт отслеживания на сцене
        globalLookAtScript = FindFirstObjectByType<LookAtTarget>();
        
        // Автоматически настраиваем начальную дистанцию на основе текущего положения камеры
        if (target != null)
        {
            distance = Vector3.Distance(transform.position, target.position);
        }
        else if (globalLookAtScript != null && this.gameObject.name == "Main Camera")
        {
            // Для главной камеры берем текущую выбранную планету в качестве цели
            target = globalLookAtScript.currentTarget != null ? globalLookAtScript.currentTarget.transform : globalLookAtScript.defaultTarget.transform;
            distance = Vector3.Distance(transform.position, target.position);
        }
    }

    void LateUpdate()
    {
        // Динамически обновляем цель для главной камеры, если она переключилась
        if (this.gameObject.name == "Main Camera" && globalLookAtScript != null)
        {
            if (globalLookAtScript.currentTarget != null && target != globalLookAtScript.currentTarget.transform)
            {
                target = globalLookAtScript.currentTarget.transform;
                distance = Vector3.Distance(transform.position, target.position);
            }
        }

        if (target == null) return;

        isControlled = false;

        // ОБРАБОТКА СЕНСОРНОГО ВВОДА (TOUCH)
        if (Input.touchCount > 0)
        {
            isControlled = true;

            // --- РЕЖИМ 1: ОДИН ПАЛЕЦ (Вращение по орбите и Тапы) ---
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                // Одиночный / Двойной Тап по экрану
                if (touch.phase == TouchPhase.Began)
                {
                    float timeSinceLastTap = Time.time - lastTapTime;
                    if (timeSinceLastTap <= doubleTapDelay)
                    {
                        OnDoubleTap(touch.position);
                    }
                    else
                    {
                        OnSingleTap(touch.position);
                    }
                    lastTapTime = Time.time;
                }

                // Плавное вращение при свайпе
                if (touch.phase == TouchPhase.Moved)
                {
                    x += touch.deltaPosition.x * xSpeed;
                    y -= touch.deltaPosition.y * ySpeed;

                    y = ClampAngle(y, yMinLimit, yMaxLimit);
                }
            }
            // --- РЕЖИМ 2: ДВА ПАЛЬЦА (Pinch-to-Zoom) ---
            else if (Input.touchCount == 2)
            {
                Touch touchZero = Input.GetTouch(0);
                Touch touchOne = Input.GetTouch(1);

                // Находим позиции пальцев в предыдущем кадре
                Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                // Находим расстояние между пальцами в текущем и предыдущем кадрах
                float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                // Разница между расстояниями — это величина зума
                float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                // Изменяем дистанцию камеры
                distance += deltaMagnitudeDiff * zoomSpeed;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
        }
        // --- ДЛЯ ТЕСТИРОВАНИЯ В РЕДАКТОРЕ UNITY (МЫШЬ И СВИДОК) ---
        else if (Application.isEditor)
        {
            if (Input.GetMouseButton(0))
            {
                isControlled = true;
                x += Input.GetAxis("Mouse X") * xSpeed * 20f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 20f;
                y = ClampAngle(y, yMinLimit, yMaxLimit);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                isControlled = true;
                distance -= scroll * zoomSpeed * 300f;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
        }

        // Если камера под управлением (свайп/зум), мы вычисляем её положение вокруг цели
        if (isControlled)
        {
            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + target.position;

            transform.rotation = rotation;
            transform.position = position;
        }
        else
        {
            // Если игрок не управляет пальцем, то детальная камера продолжает лететь на жесткой сцепке с планетой,
            // а главная камера следит за LookAtTarget
            if (this.gameObject.name == "Main Camera")
            {
                // Позволяем LookAtTarget плавно направлять камеру, но сохраняем наши углы x и y синхронизированными
                Vector3 angles = transform.eulerAngles;
                x = angles.y;
                y = angles.x;
            }
            else
            {
                // Для детальных камер планет мы сохраняем их орбиту вращения вокруг родителя, если нет инпута
                Quaternion rotation = Quaternion.Euler(y, x, 0);
                Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
                transform.position = rotation * negDistance + target.position;
                transform.rotation = rotation;
            }
        }
    }

    private void OnSingleTap(Vector2 screenPosition)
    {
        // Обрабатываем клик/тап по планете только для главной камеры
        if (this.gameObject.name == "Main Camera" && globalLookAtScript != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObject = hit.collider.gameObject;
                
                // Если тапнули по планете, делаем её текущей целью
                if (hitObject != globalLookAtScript.currentTarget)
                {
                    globalLookAtScript.currentTarget = hitObject;
                    // Имитируем логику LookAtTarget для включения описаний планет
                    TriggerDescription(hitObject.name);
                }
            }
        }
    }

    private void OnDoubleTap(Vector2 screenPosition)
    {
        if (globalLookAtScript == null) return;

        // Если двойной тап на главной камере — переключаемся на детальную камеру выбранной планеты
        if (this.gameObject.name == "Main Camera")
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObject = hit.collider.gameObject;
                globalLookAtScript.currentTarget = hitObject;
                
                // Переключаемся на детальную камеру
                TriggerCameraSwitch(hitObject.name, true);
            }
        }
        // Если двойной тап на детальной камере — возвращаемся к главной камере (выход из фокуса)
        else
        {
            TriggerCameraSwitch(target.gameObject.name, false);
        }
    }

    private void TriggerDescription(string planetName)
    {
        if (globalLookAtScript == null) return;

        // Находим и активируем соответствующее описание
        var sunDesc = globalLookAtScript.theSunGameObject;
        var earthDesc = globalLookAtScript.theEarthGameObject;
        var moonDesc = globalLookAtScript.theMoonGameObject;
        var marsDesc = globalLookAtScript.theMarsGameObject;
        var mercuryDesc = globalLookAtScript.theMercuryGameObject;
        var venusDesc = globalLookAtScript.theVenusGameObject;
        var jupiterDesc = globalLookAtScript.theJupiterGameObject;
        var saturnDesc = globalLookAtScript.theSaturnGameObject;
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
        if (uranusDesc) uranusDesc.SetActive(planetName == "Uranus");
        if (neptuneDesc) neptuneDesc.SetActive(planetName == "Neptune");
    }

    private void TriggerCameraSwitch(string planetName, bool toDetail)
    {
        if (globalLookAtScript == null) return;

        if (toDetail)
        {
            // Отключаем главную камеру и включаем детальную
            globalLookAtScript.TurnOffMainCamera();
            TriggerDescription(planetName);

            // Активируем нужную детальную камеру
            SetCameraActive(planetName, true);
        }
        else
        {
            // Возврат к главной камере
            globalLookAtScript.TurnOnMainCamera();
            SetCameraActive(planetName, false);
            
            // Сбрасываем цель на дефолтную (например, Солнце или сама камера)
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
        if (planetName == "Uranus" && globalLookAtScript.uranusCamera) globalLookAtScript.uranusCamera.SetActive(active);
        if (planetName == "Neptune" && globalLookAtScript.neptuneCamera) globalLookAtScript.neptuneCamera.SetActive(active);
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
