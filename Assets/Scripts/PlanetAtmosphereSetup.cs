using UnityEngine;
using UnityEngine.SceneManagement;

public class PlanetAtmosphereSetup : MonoBehaviour
{
    private static PlanetAtmosphereSetup instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnRuntimeMethodLoad()
    {
        // Создаем глобальный долгоживущий объект для управления атмосферами
        if (instance != null) return;
        GameObject go = new GameObject("PlanetAtmosphereSetup_Persistent");
        instance = go.AddComponent<PlanetAtmosphereSetup>();
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
        if (scene.name == "Level1")
        {
            SetupAtmospheres();
        }
    }

    private void Start()
    {
        // На случай, если игра запущена напрямую со сцены Level1 в редакторе
        if (SceneManager.GetActiveScene().name == "Level1")
        {
            SetupAtmospheres();
        }
    }

    public void SetupAtmospheres()
    {
        Shader atmosphereShader = Shader.Find("Custom/AtmosphereRim");
        if (atmosphereShader == null)
        {
            Debug.LogWarning("Атмосферный шейдер Custom/AtmosphereRim не найден. Пожалуйста, убедитесь, что он скомпилирован.");
            return;
        }

        // Настройка атмосферы Земли (Zemlya): нежный лазурно-голубой цвет
        SetupPlanetAtmosphere("Earth", new Color(0.12f, 0.58f, 1.0f, 1.0f), 3.5f, 0.82f, atmosphereShader);
        
        // Настройка атмосферы Марса (Mars): разреженный оранжево-красный слой
        SetupPlanetAtmosphere("Mars", new Color(1.0f, 0.35f, 0.12f, 1.0f), 4.2f, 0.88f, atmosphereShader);
        
        // Настройка атмосферы Венеры (Venus): густая плотная золотисто-желтая оболочка
        SetupPlanetAtmosphere("Venus", new Color(1.0f, 0.82f, 0.38f, 1.0f), 2.5f, 0.72f, atmosphereShader);
    }

    private void SetupPlanetAtmosphere(string planetName, Color color, float rimPower, float sunInfluence, Shader shader)
    {
        GameObject planet = GameObject.Find(planetName);
        if (planet == null)
        {
            Debug.LogWarning($"Планета '{planetName}' не найдена в сцене. Пропуск создания атмосферы.");
            return;
        }

        string atmosphereName = "Atmosphere_Overlay_" + planetName;
        
        // Сейфгард: если атмосфера уже существует, повторно не создаем
        if (planet.transform.Find(atmosphereName) != null) return;

        // Создаем дочернюю сферу атмосферы
        GameObject atmosphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        atmosphereObj.name = atmosphereName;
        atmosphereObj.transform.SetParent(planet.transform);
        
        // Сбрасываем локальные координаты и слегка увеличиваем масштаб купола
        atmosphereObj.transform.localPosition = Vector3.zero;
        atmosphereObj.transform.localRotation = Quaternion.identity;
        atmosphereObj.transform.localScale = new Vector3(1.025f, 1.025f, 1.025f);

        // КРИТИЧЕСКИЙ ШАГ: уничтожаем коллайдер на сфере атмосферы,
        // чтобы клики/тапы пользователя проходили насквозь и попадали точно в планету!
        Collider col = atmosphereObj.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        // Настраиваем уникальный материал
        Renderer renderer = atmosphereObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(shader);
            mat.SetColor("_AtmosphereColor", color);
            mat.SetFloat("_RimPower", rimPower);
            mat.SetFloat("_SunInfluence", sunInfluence);
            
            // Отключаем тени от атмосферы
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            
            renderer.sharedMaterial = mat;
        }
    }
}
