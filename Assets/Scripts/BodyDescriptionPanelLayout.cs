using UnityEngine;

/// <summary>
/// Keeps body encyclopedia panels at 2× reference size while clamping height
/// so they stay clear of the top navigation bar and bottom time bar.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class BodyDescriptionPanelLayout : MonoBehaviour
{
    public const float TargetWidth = 512f;
    public const float TargetHeight = 690f;
    public const float BottomOffset = 150.23718f;
    public const float LeftOffset = 7f;
    const float TopGap = 16f;

    RectTransform _rect;
    Canvas _canvas;
    int _lastScreenWidth;
    int _lastScreenHeight;
    Rect _lastSafeArea;

    void Awake()
    {
        _rect = (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>();
        ApplyLayout();
    }

    void OnEnable()
    {
        ApplyLayout();
    }

    void LateUpdate()
    {
        if (Screen.width != _lastScreenWidth
            || Screen.height != _lastScreenHeight
            || Screen.safeArea != _lastSafeArea)
        {
            ApplyLayout();
        }
    }

    public void ApplyLayout()
    {
        if (_rect == null)
            _rect = (RectTransform)transform;
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
        _lastSafeArea = Screen.safeArea;

        float canvasHeight = ResolveCanvasHeight();
        float topInset = 0f;
        float bottomInset = 0f;
        if (_canvas != null)
            SafeAreaInsets.GetCanvasInsets(_canvas, out _, out _, out topInset, out bottomInset);

        float topReserve = SidePanelUiBootstrap.BarHeight + SidePanelUiBootstrap.BarTopMargin + TopGap + topInset;
        float bottomY = BottomOffset + bottomInset;
        float maxHeight = canvasHeight - bottomY - topReserve;
        float height = Mathf.Min(TargetHeight, Mathf.Max(120f, maxHeight));

        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.zero;
        _rect.pivot = Vector2.zero;
        _rect.anchoredPosition = new Vector2(LeftOffset, bottomY);
        _rect.sizeDelta = new Vector2(TargetWidth, height);
    }

    float ResolveCanvasHeight()
    {
        if (_canvas != null)
        {
            Canvas root = _canvas.rootCanvas != null ? _canvas.rootCanvas : _canvas;
            float scale = root.scaleFactor;
            if (scale < 0.01f)
                scale = 1f;
            if (Screen.height > 1)
                return Screen.height / scale;

            var rootRect = root.transform as RectTransform;
            if (rootRect != null && rootRect.rect.height > 1f)
                return rootRect.rect.height;
        }

        if (_rect != null && _rect.parent is RectTransform parent && parent.rect.height > 1f)
            return parent.rect.height;

        return TargetHeight + BottomOffset + SidePanelUiBootstrap.BarHeight + SidePanelUiBootstrap.BarTopMargin + TopGap;
    }
}
