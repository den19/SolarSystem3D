using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VisualsInitializer : MonoBehaviour
{
    private static VisualsInitializer instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnRuntimeMethodLoad()
    {
        if (instance != null) return;
        GameObject go = new GameObject("VisualsInitializer_Persistent");
        instance = go.AddComponent<VisualsInitializer>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "Level1")
        {
            ApplyGraphicsInitialization();
        }
    }

    private void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Level1")
        {
            ApplyGraphicsInitialization();
        }
    }

    public void ApplyGraphicsInitialization()
    {
        // 1. Включаем постобработку на абсолютно всех камерах в сцене
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera cam in allCameras)
        {
            var cameraData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
            {
                cameraData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            
            // Принудительно включаем постобработку и настраиваем объемные маски
            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = 1; // Default layer
            
            Debug.Log($"[Visuals] Постобработка успешно включена на камере: {cam.name}");

            // 1.Б Автоматически навешиваем MobileOrbitCamera для сенсорного управления
            if (cam.gameObject.GetComponent<MobileOrbitCamera>() == null)
            {
                MobileOrbitCamera orbitCam = cam.gameObject.AddComponent<MobileOrbitCamera>();
                
                // Находим цель для детальных камер планет
                string planetTargetName = MapCameraToPlanet(cam.name);
                if (!string.IsNullOrEmpty(planetTargetName))
                {
                    GameObject planetGo = GameObject.Find(planetTargetName);
                    if (planetGo != null)
                    {
                        orbitCam.target = planetGo.transform;
                        
                        // Тонкие настройки зума для детальных камер, чтобы не залетать сквозь меши
                        orbitCam.minDistance = 1.8f;
                        orbitCam.maxDistance = 15f;
                        orbitCam.distance = 4.5f;
                        
                        Debug.Log($"[Visuals] Сенсорное управление MobileOrbitCamera добавлено на детальную камеру: {cam.name} с фокусом на {planetTargetName}");
                    }
                }
                else
                {
                    // Для главной камеры настраиваем более широкий зум
                    orbitCam.minDistance = 10f;
                    orbitCam.maxDistance = 600f;
                    orbitCam.distance = 45f;
                    
                    Debug.Log($"[Visuals] Сенсорное управление MobileOrbitCamera успешно добавлено на главную камеру: {cam.name}");
                }
            }
        }

        // 2. Автоматически создаем или настраиваем Global Volume в сцене
        Volume existingVolume = FindFirstObjectByType<Volume>();
        if (existingVolume == null)
        {
            GameObject volumeGo = new GameObject("GlobalPostProcessingVolume");
            Volume newVolume = volumeGo.AddComponent<Volume>();
            newVolume.isGlobal = true;
            newVolume.priority = 1.0f;

            // Загружаем наш настроенный профиль из папки Settings
            VolumeProfile profile = LoadProfileByPath();

            if (profile != null)
            {
                newVolume.sharedProfile = profile;
                Debug.Log("[Visuals] Создан Global Volume и привязан профиль SampleSceneProfile.");
            }
            else
            {
                Debug.LogWarning("[Visuals] Не удалось найти профиль SampleSceneProfile в ассетах.");
            }
        }
        else
        {
            // Если Volume уже есть, убедимся, что он глобальный и включен
            existingVolume.isGlobal = true;
            existingVolume.enabled = true;
            
            if (existingVolume.sharedProfile == null)
            {
                existingVolume.sharedProfile = LoadProfileByPath();
            }
            Debug.Log($"[Visuals] Существующий Volume настроен и активирован: {existingVolume.name}");
        }
    }

    private VolumeProfile LoadProfileByPath()
    {
        // Поиск профиля по разным путям в проекте
        #if UNITY_EDITOR
        string path = "Assets/Settings/SampleSceneProfile.asset";
        return UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        #else
        // В сборке загружаем из ресурсов
        return Resources.Load<VolumeProfile>("SampleSceneProfile");
        #endif
    }

    private string MapCameraToPlanet(string cameraName)
    {
        string camLower = cameraName.ToLower();
        if (camLower.Contains("earth")) return "Earth";
        if (camLower.Contains("mars")) return "Mars";
        if (camLower.Contains("neptune")) return "Neptune";
        if (camLower.Contains("uranus")) return "Uranus";
        if (camLower.Contains("saturn")) return "Saturn";
        if (camLower.Contains("jupiter")) return "Jupiter";
        if (camLower.Contains("venus")) return "Venus";
        if (camLower.Contains("mercury")) return "Mercury";
        if (camLower.Contains("moon")) return "Moon";
        return null;
    }
}
