using System.Collections;
using SolarSystemApp;
using UnityEngine;
using UnityEngine.UI;

public class MilkyWayControlController : MonoBehaviour
{
    [SerializeField] private Toggle milkyWayToggle;
    private bool isInitializing;

    IEnumerator Start()
    {
        isInitializing = true;

        if (milkyWayToggle != null)
        {
            milkyWayToggle.SetIsOnWithoutNotify(MilkyWaySettings.ShowMilkyWay);
            milkyWayToggle.onValueChanged.RemoveAllListeners();
            milkyWayToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        MilkyWaySettings.ShowMilkyWayChanged += OnShowMilkyWayChanged;

        yield return new WaitForEndOfFrame();

        isInitializing = false;
    }

    void OnDestroy()
    {
        MilkyWaySettings.ShowMilkyWayChanged -= OnShowMilkyWayChanged;
    }

    void OnToggleChanged(bool isOn)
    {
        if (isInitializing)
            return;

        MilkyWaySettings.SetShowMilkyWay(isOn);
    }

    void OnShowMilkyWayChanged(bool isOn)
    {
        if (milkyWayToggle != null)
            milkyWayToggle.SetIsOnWithoutNotify(isOn);
    }
}
