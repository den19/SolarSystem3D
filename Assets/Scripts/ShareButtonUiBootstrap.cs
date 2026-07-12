using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ensures the Share toolbar button exists at runtime when the scene was not refreshed in the editor.
/// </summary>
public static class ShareButtonUiBootstrap
{
    const string ShareButtonName = "ShareButton";
    const string ShareIconResourcePath = "Icons/icons8-share-256";
    const float ButtonSize = 44f;
    const float IconPadding = 4f;
    static readonly Color IconColor = new Color(0.85f, 0.92f, 1f, 1f);

    public static void EnsureShareButton(Transform navigationBar)
    {
        if (navigationBar == null)
            return;

        Transform existing = navigationBar.Find(ShareButtonName);
        if (existing == null)
            existing = FindShareButton(navigationBar.root);

        if (existing == null)
            existing = CreateShareButton(navigationBar).transform;

        if (existing.parent != navigationBar)
            existing.SetParent(navigationBar, false);

        existing.SetSiblingIndex(4);
        ApplyLayout(existing);
    }

    static Transform FindShareButton(Transform root)
    {
        if (root == null)
            return null;

        Transform direct = root.Find(ShareButtonName);
        if (direct != null)
            return direct;

        Transform inBar = root.Find("BodyNavigationBar/" + ShareButtonName);
        if (inBar != null)
            return inBar;

        return null;
    }

    static GameObject CreateShareButton(Transform navigationBar)
    {
        var buttonGo = new GameObject(ShareButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(ShareButtonController));
        buttonGo.layer = navigationBar.gameObject.layer;
        buttonGo.transform.SetParent(navigationBar, false);

        var image = buttonGo.GetComponent<Image>();
        image.sprite = null;
        image.color = Color.clear;
        image.raycastTarget = true;

        var button = buttonGo.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.78f, 0.88f, 1f, 1f);
        colors.pressedColor = new Color(0.65f, 0.78f, 0.95f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconGo.layer = buttonGo.layer;
        iconGo.transform.SetParent(buttonGo.transform, false);

        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(IconPadding, IconPadding);
        iconRect.offsetMax = new Vector2(-IconPadding, -IconPadding);

        var iconImage = iconGo.GetComponent<Image>();
        iconImage.sprite = Resources.Load<Sprite>(ShareIconResourcePath);
        iconImage.color = IconColor;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        button.targetGraphic = iconImage;

        return buttonGo;
    }

    static void ApplyLayout(Transform button)
    {
        if (!button.TryGetComponent(out RectTransform rect))
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;

        if (!button.TryGetComponent(out LayoutElement layoutElement))
            layoutElement = button.gameObject.AddComponent<LayoutElement>();

        layoutElement.minWidth = ButtonSize;
        layoutElement.minHeight = ButtonSize;
        layoutElement.preferredWidth = ButtonSize;
        layoutElement.preferredHeight = ButtonSize;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        if (button.GetComponent<ShareButtonController>() == null)
            button.gameObject.AddComponent<ShareButtonController>();
    }
}
