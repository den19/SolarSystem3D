using System.Collections;
using SolarSystemApp;
using UnityEngine;
using UnityEngine.UI;

public class ProbeChaseInspectControlController : MonoBehaviour
{
    [SerializeField] private Toggle keepChaseInspectAngleToggle;
    private bool isInitializing;

    IEnumerator Start()
    {
        isInitializing = true;

        if (keepChaseInspectAngleToggle != null)
        {
            keepChaseInspectAngleToggle.SetIsOnWithoutNotify(ProbeSettings.KeepChaseInspectAngle);
            keepChaseInspectAngleToggle.onValueChanged.RemoveAllListeners();
            keepChaseInspectAngleToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        ProbeSettings.KeepChaseInspectAngleChanged += OnKeepChaseInspectAngleChanged;

        yield return new WaitForEndOfFrame();

        isInitializing = false;
    }

    void OnDestroy()
    {
        ProbeSettings.KeepChaseInspectAngleChanged -= OnKeepChaseInspectAngleChanged;
    }

    void OnToggleChanged(bool isOn)
    {
        if (isInitializing)
            return;

        ProbeSettings.SetKeepChaseInspectAngle(isOn);
    }

    void OnKeepChaseInspectAngleChanged(bool isOn)
    {
        if (keepChaseInspectAngleToggle != null)
            keepChaseInspectAngleToggle.SetIsOnWithoutNotify(isOn);
    }
}
