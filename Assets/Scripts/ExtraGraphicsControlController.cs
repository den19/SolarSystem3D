using System.Collections;
using SolarSystemApp;
using UnityEngine;
using UnityEngine.UI;

public class ExtraGraphicsControlController : MonoBehaviour
{
    [SerializeField] private Toggle extraGraphicsToggle;
    private bool isInitializing;

    IEnumerator Start()
    {
        isInitializing = true;

        if (extraGraphicsToggle != null)
        {
            extraGraphicsToggle.SetIsOnWithoutNotify(GraphicsSettings.UseExtraGraphics);
            extraGraphicsToggle.onValueChanged.RemoveAllListeners();
            extraGraphicsToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        GraphicsSettings.UseExtraGraphicsChanged += OnExtraGraphicsChanged;

        yield return new WaitForEndOfFrame();

        isInitializing = false;
    }

    void OnDestroy()
    {
        GraphicsSettings.UseExtraGraphicsChanged -= OnExtraGraphicsChanged;
    }

    void OnToggleChanged(bool isOn)
    {
        if (isInitializing)
            return;

        GraphicsSettings.SetUseExtraGraphics(isOn);
    }

    void OnExtraGraphicsChanged(bool isOn)
    {
        if (extraGraphicsToggle != null)
            extraGraphicsToggle.SetIsOnWithoutNotify(isOn);
    }
}
