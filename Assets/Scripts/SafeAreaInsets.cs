using UnityEngine;

/// <summary>
/// Converts Screen.safeArea into canvas-local pixel insets for UI layout.
/// Uses Screen size / rootCanvas.scaleFactor so insets stay correct when
/// RectTransform.rect is still stale after orientation or resolution changes.
/// </summary>
public static class SafeAreaInsets
{
    const float MaxInsetFraction = 0.4f;

    public static void GetCanvasInsets(Canvas canvas, out float left, out float right, out float top, out float bottom)
    {
        left = right = top = bottom = 0f;
        if (canvas == null)
            return;

        Canvas root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        if (screenWidth <= 1f || screenHeight <= 1f)
            return;

        Rect safeArea = Screen.safeArea;
        if (safeArea.width <= 1f || safeArea.height <= 1f)
            return;

        // Mid-rotation / multi-display glitches: safe area outside current screen.
        if (safeArea.xMax < 0f || safeArea.yMax < 0f
            || safeArea.xMin > screenWidth || safeArea.yMin > screenHeight)
            return;

        float xMin = Mathf.Clamp(safeArea.xMin, 0f, screenWidth);
        float yMin = Mathf.Clamp(safeArea.yMin, 0f, screenHeight);
        float xMax = Mathf.Clamp(safeArea.xMax, 0f, screenWidth);
        float yMax = Mathf.Clamp(safeArea.yMax, 0f, screenHeight);
        if (xMax <= xMin || yMax <= yMin)
            return;

        float scaleFactor = root.scaleFactor;
        if (scaleFactor < 0.01f)
            scaleFactor = 1f;

        // Derive canvas size from screen + scaleFactor (not canvas.rect — can be stale).
        float canvasWidth = screenWidth / scaleFactor;
        float canvasHeight = screenHeight / scaleFactor;

        left = xMin / scaleFactor;
        bottom = yMin / scaleFactor;
        right = (screenWidth - xMax) / scaleFactor;
        top = (screenHeight - yMax) / scaleFactor;

        float maxHorizontal = canvasWidth * MaxInsetFraction;
        float maxVertical = canvasHeight * MaxInsetFraction;
        left = Mathf.Clamp(left, 0f, maxHorizontal);
        right = Mathf.Clamp(right, 0f, maxHorizontal);
        top = Mathf.Clamp(top, 0f, maxVertical);
        bottom = Mathf.Clamp(bottom, 0f, maxVertical);
    }
}
