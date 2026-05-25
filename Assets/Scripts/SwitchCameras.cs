using UnityEngine;

public class SwitchCameras : MonoBehaviour
{
    public Camera mainCamera;      // Главная камера (первоначальная)
    public Camera detailCamera;    // Детальная камера (которая появляется при фокусировке на объект)

    public GameObject focusTarget; // Целевое тело, которое будет рассматриваться детализированной камерой

    private bool isDetailCamActive = false; // Флаг активности детальной камеры

    void Start()
    {
        // Деактивируй дополнительную камеру вначале
        detailCamera.enabled = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Нажатие левой клавиши мыши
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;

            if (Physics.Raycast(ray, out hitInfo))
            {
                focusTarget = hitInfo.collider.gameObject;

                // Переключись на детальную камеру
                ToggleCameras();
            }
        }
    }

    void ToggleCameras()
    {
        if (!isDetailCamActive)
        {
            // Переключение на детальную камеру
            mainCamera.enabled = false;
            detailCamera.enabled = true;
            detailCamera.transform.LookAt(focusTarget.transform); // Наведение камеры на выбранный объект
            isDetailCamActive = true;
        }
        else
        {
            // Возврат к основной камере
            mainCamera.enabled = true;
            detailCamera.enabled = false;
            isDetailCamActive = false;
        }
    }
}