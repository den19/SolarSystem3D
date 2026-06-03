using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleScreenshotTrigger : MonoBehaviour, IPointerClickHandler
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

        foreach (var text in GetComponentsInChildren<Text>(true))
            text.raycastTarget = false;
        foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
            tmp.raycastTarget = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (screenshotUtility == null)
            return;

        screenshotUtility.TakeScreenshot();
    }
}
