using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Distinguishes short tap (show description) from long press (open body picker) on BodyNameButton.
/// </summary>
public class BodyNameLongPressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    const float LongPressThreshold = 0.45f;

    [SerializeField] BodyNavigationController bodyNavigationController;

    Coroutine _holdRoutine;
    bool _pointerDown;
    bool _longPressTriggered;

    public void Configure(BodyNavigationController navigationController)
    {
        bodyNavigationController = navigationController;
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
            TriggerShortClick();

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
            TriggerLongPress();
        }

        _holdRoutine = null;
    }

    void TriggerLongPress()
    {
        if (bodyNavigationController == null)
            bodyNavigationController = FindFirstObjectByType<BodyNavigationController>();

        if (bodyNavigationController != null)
            bodyNavigationController.TryOpenBodyPicker();
    }

    void TriggerShortClick()
    {
        if (bodyNavigationController == null)
            bodyNavigationController = FindFirstObjectByType<BodyNavigationController>();

        if (bodyNavigationController != null)
            bodyNavigationController.OnBodyNameShortClicked();
    }

    void CancelHoldRoutine()
    {
        if (_holdRoutine != null)
        {
            StopCoroutine(_holdRoutine);
            _holdRoutine = null;
        }
    }
}

