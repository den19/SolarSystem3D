using System.Collections;
using SolarSystemApp;
using UnityEngine;
using UnityEngine.UI;

public class GravityGridControlController : MonoBehaviour
{
    [SerializeField] private Toggle gravityGridToggle;
    private bool isInitializing;

    IEnumerator Start()
    {
        isInitializing = true;

        if (gravityGridToggle != null)
        {
            gravityGridToggle.SetIsOnWithoutNotify(GravityGridSettings.UseGravityGrid);
            gravityGridToggle.onValueChanged.RemoveAllListeners();
            gravityGridToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        GravityGridSettings.UseGravityGridChanged += OnGravityGridChanged;

        yield return new WaitForEndOfFrame();

        isInitializing = false;
    }

    void OnDestroy()
    {
        GravityGridSettings.UseGravityGridChanged -= OnGravityGridChanged;
    }

    void OnToggleChanged(bool isOn)
    {
        if (isInitializing)
            return;

        GravityGridSettings.SetUseGravityGrid(isOn);
    }

    void OnGravityGridChanged(bool isOn)
    {
        if (gravityGridToggle != null)
            gravityGridToggle.SetIsOnWithoutNotify(isOn);
    }
}
