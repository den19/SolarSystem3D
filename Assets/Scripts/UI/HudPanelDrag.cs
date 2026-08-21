using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drag a HUD panel within its parent RectTransform. Offset lasts for the session only.
/// </summary>
public class HudPanelDrag : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    RectTransform _panel;
    RectTransform _parent;
    Vector2 _originalLocalPointerPosition;
    Vector3 _originalPanelLocalPosition;
    bool _hasUserOffset;

    public bool HasUserOffset => _hasUserOffset;

    public void ClearUserOffset()
    {
        _hasUserOffset = false;
    }

    public void EnsureClamped()
    {
        EnsureRefs();
        ClampToParent();
    }

    void Awake()
    {
        EnsureRefs();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        EnsureRefs();
        if (_panel == null || _parent == null)
            return;

        _panel.SetAsLastSibling();
        _originalPanelLocalPosition = _panel.localPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parent, eventData.position, eventData.pressEventCamera, out _originalLocalPointerPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        EnsureRefs();
        if (_panel == null || _parent == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parent, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPosition))
            return;

        Vector3 offsetToOriginal = localPointerPosition - _originalLocalPointerPosition;
        _panel.localPosition = _originalPanelLocalPosition + offsetToOriginal;
        ClampToParent();

        if (!_hasUserOffset && offsetToOriginal.sqrMagnitude > 0.01f)
            _hasUserOffset = true;
    }

    void EnsureRefs()
    {
        if (_panel == null)
            _panel = transform as RectTransform;
        if (_parent == null && _panel != null)
            _parent = _panel.parent as RectTransform;
    }

    void ClampToParent()
    {
        if (_panel == null || _parent == null)
            return;

        Vector3 pos = _panel.localPosition;
        Vector3 minPosition = _parent.rect.min - _panel.rect.min;
        Vector3 maxPosition = _parent.rect.max - _panel.rect.max;
        pos.x = Mathf.Clamp(pos.x, minPosition.x, maxPosition.x);
        pos.y = Mathf.Clamp(pos.y, minPosition.y, maxPosition.y);
        _panel.localPosition = pos;
    }
}
