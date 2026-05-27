using System.Collections;
using SolarSystemApp;
using UnityEngine;
using UnityEngine.UI;

public class CpuMonitorControlController : MonoBehaviour
{
    [SerializeField] private Toggle cpuMonitorToggle;
    private bool isInitializing;

    IEnumerator Start()
    {
        isInitializing = true;

        if (cpuMonitorToggle != null)
        {
            cpuMonitorToggle.SetIsOnWithoutNotify(CpuMonitorSettings.UseCpuMonitor);
            cpuMonitorToggle.onValueChanged.RemoveAllListeners();
            cpuMonitorToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        CpuMonitorSettings.UseCpuMonitorChanged += OnCpuMonitorChanged;

        yield return new WaitForEndOfFrame();

        isInitializing = false;
    }

    void OnDestroy()
    {
        CpuMonitorSettings.UseCpuMonitorChanged -= OnCpuMonitorChanged;
    }

    void OnToggleChanged(bool isOn)
    {
        if (isInitializing)
            return;

        CpuMonitorSettings.SetUseCpuMonitor(isOn);
    }

    void OnCpuMonitorChanged(bool isOn)
    {
        if (cpuMonitorToggle != null)
            cpuMonitorToggle.SetIsOnWithoutNotify(isOn);
    }
}

