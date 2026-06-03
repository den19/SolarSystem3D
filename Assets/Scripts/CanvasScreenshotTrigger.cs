using UnityEngine;
using UnityEngine.EventSystems;

public class CanvasScreenshotTrigger : MonoBehaviour, IPointerClickHandler
{
    ScreenshotUtility screenshotUtility;

    void Awake()
    {
        if (!Application.isEditor)
        {
            enabled = false;
            return;
        }

        screenshotUtility = GetComponent<ScreenshotUtility>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (screenshotUtility == null)
            return;

        screenshotUtility.TakeScreenshot();
    }
}
