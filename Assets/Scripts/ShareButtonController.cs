using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires the Share toolbar button to SimulationShareController.
/// </summary>
[RequireComponent(typeof(Button))]
public class ShareButtonController : MonoBehaviour
{
    Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
    }

    void OnEnable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnShareClicked);
            _button.onClick.AddListener(OnShareClicked);
        }
    }

    void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnShareClicked);
    }

    void OnShareClicked()
    {
        if (SimulationShareController.Instance != null)
            SimulationShareController.Instance.RequestShare();
        else
            Debug.LogWarning("SimulationShareController is not ready yet.");
    }
}
