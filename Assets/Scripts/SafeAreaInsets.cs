using UnityEngine;

/// <summary>
/// Converts Screen.safeArea into canvas-local pixel insets for UI layout.
/// </summary>
public static class SafeAreaInsets
{
    public static void GetCanvasInsets(Canvas canvas, out float left, out float right, out float top, out float bottom)
    {
        left = right = top = bottom = 0f;
        if (canvas == null)
            return;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        if (screenWidth <= 0f || screenHeight <= 0f)
            return;

        var canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null)
            return;

        Rect safeArea = Screen.safeArea;
        Vector2 canvasSize = canvasRect.rect.size;
        float scaleX = canvasSize.x / screenWidth;
        float scaleY = canvasSize.y / screenHeight;

        left = safeArea.x * scaleX;
        bottom = safeArea.y * scaleY;
        right = (screenWidth - (safeArea.x + safeArea.width)) * scaleX;
        top = (screenHeight - (safeArea.y + safeArea.height)) * scaleY;
    }
}
