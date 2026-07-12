using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires the Share toolbar button to SimulationShareController.
/// </summary>
[RequireComponent(typeof(Button))]
public class ShareButtonController : MonoBehaviour
{
    const string KeyShareNotReady = "ShareNotReadyMessage";
    const string FallbackShareNotReady = "Preparing share. Please try again in a moment.";

    Button _button;
    bool _retryPending;

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
        {
            SimulationShareController.Instance.RequestShare();
            return;
        }

        if (!SimulationViewBootstrap.SystemsReady && !_retryPending)
        {
            _retryPending = true;
            StartCoroutine(RetryShareAfterInit());
            return;
        }

        TransientMessageController.ShowLocalized(KeyShareNotReady, FallbackShareNotReady);
    }

    IEnumerator RetryShareAfterInit()
    {
        yield return null;

        _retryPending = false;

        if (SimulationShareController.Instance != null)
            SimulationShareController.Instance.RequestShare();
        else
            TransientMessageController.ShowLocalized(KeyShareNotReady, FallbackShareNotReady);
    }
}
