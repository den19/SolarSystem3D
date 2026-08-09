using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Applies Space 5 (off) or retouched Space 3 galactic skybox (on) from MilkyWaySettings.
/// Galactic inclination (~60.2°) is authored on the Space 3 material shader.
/// Node longitude ~90° places denser band (former Left) toward -X after tilt — educational Sagittarius-like azimuth.
/// </summary>
public class MilkyWaySkyboxApplier : MonoBehaviour
{
    [SerializeField] Material starfieldSkybox;
    [SerializeField] Material milkyWaySkybox;

    void OnEnable()
    {
        MilkyWaySettings.ShowMilkyWayChanged += OnShowMilkyWayChanged;
        Apply(MilkyWaySettings.ShowMilkyWay);
    }

    void OnDisable()
    {
        MilkyWaySettings.ShowMilkyWayChanged -= OnShowMilkyWayChanged;
    }

    void OnShowMilkyWayChanged(bool show)
    {
        Apply(show);
    }

    void Apply(bool showMilkyWay)
    {
        Material chosen = showMilkyWay ? milkyWaySkybox : starfieldSkybox;
        if (chosen == null)
            return;

        RenderSettings.skybox = chosen;
        DynamicGI.UpdateEnvironment();
    }
}
