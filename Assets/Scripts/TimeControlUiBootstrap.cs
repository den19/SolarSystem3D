using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Layout constants and runtime fallback creation for SimulationTimeControlBar.
/// </summary>
public static class TimeControlUiBootstrap
{
    public const string BarObjectName = "SimulationTimeControlBar";
    public const float BarHeight = 40f;
    public const float BarHorizontalMargin = 8f;
    public const float BarBottomMargin = 8f;
    public const float BarWidthPortrait = 220f;
    public const float BarWidthLandscape = 220f;
    public const float ButtonSize = 36f;
    public const float SpeedLabelMinWidth = 52f;
    public const float BarCornerRadiusPadding = 8f;
    public const int BarPaddingHorizontal = 6;
    public const int BarPaddingVertical = 2;
    public const float BarSpacing = 4f;

    public static readonly Color BarBackgroundColor = new Color(0.12f, 0.15f, 0.22f, 0.92f);
    public static readonly Color IconColor = new Color(0.85f, 0.92f, 1f, 1f);
    public static readonly Color SpeedLabelColor = new Color(0.92f, 0.95f, 1f, 1f);

    public static SimulationTimeControlController EnsureTimeControlBar(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return null;

        Transform existing = canvasTransform.Find(BarObjectName);
        if (existing != null)
        {
            var controller = existing.GetComponent<SimulationTimeControlController>();
            if (controller == null)
                controller = existing.gameObject.AddComponent<SimulationTimeControlController>();

            return controller;
        }

        return CreateTimeControlBar(canvasTransform);
    }

    static SimulationTimeControlController CreateTimeControlBar(Transform canvasTransform)
    {
        int uiLayer = canvasTransform.gameObject.layer;

        var barGo = new GameObject(BarObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup), typeof(SimulationTimeControlController));
        barGo.layer = uiLayer;
        barGo.transform.SetParent(canvasTransform, false);

        var barRect = barGo.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.sizeDelta = new Vector2(BarWidthPortrait, BarHeight);

        var barImage = barGo.GetComponent<Image>();
        barImage.color = BarBackgroundColor;
        barImage.raycastTarget = true;

        var layout = barGo.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(BarPaddingHorizontal, BarPaddingHorizontal, BarPaddingVertical, BarPaddingVertical);
        layout.spacing = BarSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var controller = barGo.GetComponent<SimulationTimeControlController>();

        Button pauseButton = CreateIconButton(barGo.transform, "PausePlayButton", "II", uiLayer);
        Button speedDownButton = CreateIconButton(barGo.transform, "SpeedDownButton", "-", uiLayer);
        TMP_Text speedLabel = CreateSpeedLabel(barGo.transform, uiLayer);
        Button speedUpButton = CreateIconButton(barGo.transform, "SpeedUpButton", "+", uiLayer);

        controller.Configure(barRect, pauseButton, speedDownButton, speedUpButton, speedLabel);
        return controller;
    }

    static Button CreateIconButton(Transform parent, string name, string labelText, int layer)
    {
        var buttonGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonGo.layer = layer;
        buttonGo.transform.SetParent(parent, false);

        var image = buttonGo.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        var button = buttonGo.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.78f, 0.88f, 1f, 1f);
        colors.pressedColor = new Color(0.65f, 0.78f, 0.95f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        var buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(ButtonSize, ButtonSize);

        var layoutElement = buttonGo.GetComponent<LayoutElement>();
        layoutElement.minWidth = ButtonSize;
        layoutElement.minHeight = ButtonSize;
        layoutElement.preferredWidth = ButtonSize;
        layoutElement.preferredHeight = ButtonSize;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.layer = layer;
        labelGo.transform.SetParent(buttonGo.transform, false);

        var labelRect = labelGo.GetComponent<RectTransform>();
        StretchFull(labelRect);

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text = labelText;
        tmp.fontSize = name == "PausePlayButton" ? 16f : 22f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = IconColor;
        tmp.raycastTarget = false;

        button.targetGraphic = image;
        return button;
    }

    static TMP_Text CreateSpeedLabel(Transform parent, int layer)
    {
        var labelGo = new GameObject("TimeControlSpeedFormat", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelGo.layer = layer;
        labelGo.transform.SetParent(parent, false);

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(SpeedLabelMinWidth, ButtonSize);

        var layoutElement = labelGo.GetComponent<LayoutElement>();
        layoutElement.minWidth = SpeedLabelMinWidth;
        layoutElement.preferredWidth = SpeedLabelMinWidth;
        layoutElement.minHeight = ButtonSize;
        layoutElement.preferredHeight = ButtonSize;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text = "1x";
        tmp.fontSize = 14f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = SpeedLabelColor;
        tmp.raycastTarget = false;

        return tmp;
    }

    public static void ApplyBarRectLayout(RectTransform barRect, Canvas canvas, bool landscape, float safeLeft, float safeRight, float safeBottom)
    {
        if (barRect == null)
            return;

        float width = landscape ? BarWidthLandscape : BarWidthPortrait;
        float bottomInset = Mathf.Max(0f, safeBottom + BarBottomMargin);

        // Bottom-center in both orientations so the bar never sits under a side
        // cutout or gets pushed off-screen when safe-left is large in landscape.
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.sizeDelta = new Vector2(width, BarHeight);

        float x = 0f;
        if (canvas != null)
        {
            Canvas root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            float scaleFactor = root.scaleFactor > 0.01f ? root.scaleFactor : 1f;
            float canvasWidth = Screen.width / scaleFactor;
            float minX = -canvasWidth * 0.5f + safeLeft + BarHorizontalMargin + width * 0.5f;
            float maxX = canvasWidth * 0.5f - safeRight - BarHorizontalMargin - width * 0.5f;
            if (maxX >= minX)
                x = Mathf.Clamp(0f, minX, maxX);
        }

        barRect.anchoredPosition = new Vector2(x, bottomInset);
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
