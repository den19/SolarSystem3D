using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Wires the Share toolbar button to SimulationShareController.
/// Short tap shares a clean simulation frame; hold for 2s shares the screen with UI.
/// </summary>
[RequireComponent(typeof(Button))]
public class ShareButtonController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    const float LongPressThreshold = 2f;

    const string KeyShareNotReady = "ShareNotReadyMessage";
    const string FallbackShareNotReady = "Preparing share. Please try again in a moment.";

    Button _button;
    bool _retryPending;
    bool _pointerDown;
    bool _longPressTriggered;
    Coroutine _holdRoutine;

    void Awake()
    {
        _button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerDown = true;
        _longPressTriggered = false;

        if (_holdRoutine != null)
            StopCoroutine(_holdRoutine);

        _holdRoutine = StartCoroutine(WaitForLongPress());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_pointerDown && !_longPressTriggered)
            return;

        CancelHoldRoutine();

        if (_longPressTriggered)
        {
            _longPressTriggered = false;
            _pointerDown = false;
            return;
        }

        if (_pointerDown)
            TriggerShortShare();

        _pointerDown = false;
    }

    IEnumerator WaitForLongPress()
    {
        float elapsed = 0f;
        while (elapsed < LongPressThreshold)
        {
            if (!_pointerDown)
            {
                _holdRoutine = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_pointerDown)
        {
            _longPressTriggered = true;
            TriggerLongShare();
        }

        _holdRoutine = null;
    }

    void TriggerShortShare()
    {
        TryRequestShare(includeUi: false);
    }

    void TriggerLongShare()
    {
        ResetPressedVisual();
        TryRequestShare(includeUi: true);
    }

    void TryRequestShare(bool includeUi)
    {
        if (SimulationShareController.Instance != null)
        {
            if (includeUi)
                SimulationShareController.Instance.RequestShareWithUi();
            else
                SimulationShareController.Instance.RequestShare();
            return;
        }

        if (!SimulationViewBootstrap.SystemsReady && !_retryPending)
        {
            _retryPending = true;
            StartCoroutine(RetryShareAfterInit(includeUi));
            return;
        }

        TransientMessageController.ShowLocalized(KeyShareNotReady, FallbackShareNotReady);
    }

    IEnumerator RetryShareAfterInit(bool includeUi)
    {
        yield return null;

        _retryPending = false;

        if (SimulationShareController.Instance != null)
        {
            if (includeUi)
                SimulationShareController.Instance.RequestShareWithUi();
            else
                SimulationShareController.Instance.RequestShare();
        }
        else
        {
            TransientMessageController.ShowLocalized(KeyShareNotReady, FallbackShareNotReady);
        }
    }

    void ResetPressedVisual()
    {
        if (_button == null)
            return;

        _button.OnDeselect(null);

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject == gameObject)
            eventSystem.SetSelectedGameObject(null);
    }

    void CancelHoldRoutine()
    {
        if (_holdRoutine != null)
        {
            StopCoroutine(_holdRoutine);
            _holdRoutine = null;
        }
    }

    void OnDisable()
    {
        CancelHoldRoutine();
        _pointerDown = false;
        _longPressTriggered = false;
    }
}
