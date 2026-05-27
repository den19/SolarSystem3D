using SolarSystemApp;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds semi-transparent HUD with a button mirror of PlayerPrefs-backed Extra Graphics state.
/// Attached under MainScreenCanvas at runtime while Level1 is active.
/// </summary>
public static class ExtraGraphicsHud
{
    const string HudRootName = "ExtraGraphicsPanel_Runtime";

    public static void SpawnHud(RectTransform hostCanvasRt)
    {
        if (!hostCanvasRt || hostCanvasRt.Find(HudRootName))
            return;

        Sprite white = CreateOnePixelSprite();

        GameObject panel = new GameObject(HudRootName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.SetParent(hostCanvasRt, false);
        panelRt.anchorMin = new Vector2(1f, 0f);
        panelRt.anchorMax = new Vector2(1f, 0f);
        panelRt.pivot = new Vector2(1f, 0f);
        panelRt.anchoredPosition = new Vector2(-12f, 12f);
        panelRt.sizeDelta = new Vector2(240f, 58f);

        var panelImage = panel.GetComponent<Image>();
        panelImage.sprite = white;
        panelImage.type = Image.Type.Simple;
        panelImage.color = new Color(0f, 0f, 0f, 0.55f);

        var layout = panel.GetComponent<LayoutElement>();
        layout.preferredHeight = 58f;
        layout.preferredWidth = 240f;

        GameObject buttonGo = new GameObject("ToggleArea", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button));

        RectTransform btnRt = buttonGo.GetComponent<RectTransform>();
        btnRt.SetParent(panelRt, false);
        btnRt.anchorMin = Vector2.zero;
        btnRt.anchorMax = Vector2.one;
        btnRt.offsetMin = new Vector2(8f, 6f);
        btnRt.offsetMax = new Vector2(-8f, -6f);

        var btnGraphic = buttonGo.GetComponent<Image>();
        btnGraphic.sprite = white;
        btnGraphic.type = Image.Type.Simple;
        btnGraphic.color = new Color(1f, 1f, 1f, 0.12f);

        GameObject txtGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.SetParent(btnRt, false);
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        Text label = txtGo.GetComponent<Text>();
        label.alignByGeometry = true;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.fontSize = 16;
        label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (label.font == null)
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        Button button = buttonGo.GetComponent<Button>();
        ApplyLabel(label);
        button.onClick.AddListener(() =>
        {
            GraphicsSettings.SetUseExtraGraphics(!GraphicsSettings.UseExtraGraphics);
            ApplyLabel(label);
        });

        GraphicsSettings.UseExtraGraphicsChanged += _ => ApplyLabel(label);
    }

    static void ApplyLabel(Text label)
    {
        if (!label) return;
        label.text = GraphicsSettings.UseExtraGraphics ? "Extra Graphics: ON" : "Extra Graphics: OFF";
    }

    static Sprite CreateOnePixelSprite()
    {
        var tex = Texture2D.whiteTexture;
        var s = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f),
            Mathf.Max(tex.width, 1));
        return s;
    }
}
