using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Enables or disables the Level1 minimap camera based on persisted settings.
/// </summary>
public class MinimapController : MonoBehaviour
{
    Camera _minimapCamera;

    void Awake()
    {
        GameObject minimapGo = GameObject.Find("Minimap Camera");
        if (minimapGo != null)
            _minimapCamera = minimapGo.GetComponent<Camera>();

        ApplyVisibility(SimulationViewSettings.ShowMinimap);
    }

    void OnEnable()
    {
        SimulationViewSettings.ShowMinimapChanged += ApplyVisibility;
    }

    void OnDisable()
    {
        SimulationViewSettings.ShowMinimapChanged -= ApplyVisibility;
    }

    void ApplyVisibility(bool visible)
    {
        if (_minimapCamera != null)
            _minimapCamera.enabled = visible;
    }
}
