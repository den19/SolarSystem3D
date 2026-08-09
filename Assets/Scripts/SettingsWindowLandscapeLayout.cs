using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scales SettingsWindow content ×1.5 in landscape and keeps checkboxes on the right edge.
/// Portrait restores scene baselines captured in Awake. Does not change the window's localScale (tilt).
/// </summary>
public class SettingsWindowLandscapeLayout : MonoBehaviour
{
    const float LandscapeFactor = 1.5f;
    const float ToggleRightPadding = 24f;
    const float ToggleBaseSize = 20f;
    /// <summary>Minimum inset from each horizontal edge of the Settings window.</summary>
    const float SidePaddingRatio = 0.10f;

    static readonly string[] ContentChildNames =
    {
        "Title",
        "Language",
        "Volume",
        "Music",
        "ExtraGraphics",
        "MilkyWay",
        "GraphicsTier",
        "CpuMonitor",
        "Back",
        "Reset",
        "Audio",
    };

    /// <summary>Rows that should span panel width so right-edge toggles sit on the window edge.</summary>
    static readonly string[] FullWidthRowNames =
    {
        "Language",
        "Volume",
        "Music",
        "ExtraGraphics",
        "MilkyWay",
        "GraphicsTier",
        "CpuMonitor",
    };

    RectTransform panelRect;
    Vector2 basePanelSize;
    Vector2 basePanelPos;

    readonly List<RectBaseline> contentBaselines = new List<RectBaseline>(12);
    readonly List<TextBaseline> textBaselines = new List<TextBaseline>(24);
    readonly List<ToggleBaseline> toggleBaselines = new List<ToggleBaseline>(4);

    bool lastIsLandscape;
    bool applied;

    static bool IsLandscape => Screen.width > Screen.height;

    struct RectBaseline
    {
        public RectTransform Rect;
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
        public Vector3 LocalScale;
    }

    struct TextBaseline
    {
        public Text Text;
        public int FontSize;
    }

    struct ToggleBaseline
    {
        public RectTransform Rect;
        public Vector3 LocalScale;
    }

    void Awake()
    {
        panelRect = GetComponent<RectTransform>();
        if (panelRect == null)
            return;

        AlignTogglesToRightEdge();
        CacheBaselines();
        ApplyLayout(force: true);
    }

    void OnEnable()
    {
        ApplyLayout(force: true);
    }

    void Update()
    {
        bool landscape = IsLandscape;
        if (applied && landscape == lastIsLandscape)
            return;

        ApplyLayout(force: false);
    }

    void CacheBaselines()
    {
        contentBaselines.Clear();
        textBaselines.Clear();
        toggleBaselines.Clear();

        basePanelSize = panelRect.sizeDelta;
        basePanelPos = panelRect.anchoredPosition;

        for (int i = 0; i < ContentChildNames.Length; i++)
        {
            Transform child = FindDirectChild(ContentChildNames[i]);
            if (child == null)
                continue;

            var rect = child as RectTransform;
            if (rect == null)
                rect = child.GetComponent<RectTransform>();
            if (rect == null)
                continue;

            contentBaselines.Add(new RectBaseline
            {
                Rect = rect,
                AnchoredPosition = rect.anchoredPosition,
                SizeDelta = rect.sizeDelta,
                LocalScale = rect.localScale,
            });
        }

        Text[] texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;
            textBaselines.Add(new TextBaseline
            {
                Text = texts[i],
                FontSize = texts[i].fontSize,
            });
        }

        CacheToggle("Volume Toggle");
        CacheToggle("Music Toggle");
        CacheToggle("Extra Graphics Toggle");
        CacheToggle("Milky Way Toggle");
        CacheToggle("CPU Monitor Toggle");
    }

    void CacheToggle(string objectName)
    {
        Transform found = FindDeepChild(transform, objectName);
        if (found == null)
            return;

        var rect = found as RectTransform ?? found.GetComponent<RectTransform>();
        if (rect == null)
            return;

        toggleBaselines.Add(new ToggleBaseline
        {
            Rect = rect,
            LocalScale = rect.localScale,
        });
    }

    void AlignTogglesToRightEdge()
    {
        AlignToggle(FindDeepChild(transform, "Volume Toggle"));
        AlignToggle(FindDeepChild(transform, "Music Toggle"));
        AlignToggle(FindDeepChild(transform, "Extra Graphics Toggle"));
        AlignToggle(FindDeepChild(transform, "Milky Way Toggle"));
        AlignToggle(FindDeepChild(transform, "CPU Monitor Toggle"));
    }

    static void AlignToggle(Transform toggleTransform)
    {
        if (toggleTransform == null)
            return;

        var rect = toggleTransform as RectTransform ?? toggleTransform.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(ToggleBaseSize, ToggleBaseSize);
        rect.anchoredPosition = new Vector2(-ToggleRightPadding, 0f);
    }

    void ApplyLayout(bool force)
    {
        if (panelRect == null)
            return;

        bool landscape = IsLandscape;
        if (!force && applied && landscape == lastIsLandscape)
            return;

        float factor = landscape ? LandscapeFactor : 1f;

        panelRect.sizeDelta = new Vector2(basePanelSize.x * factor, basePanelSize.y * factor);
        panelRect.anchoredPosition = basePanelPos;

        float sidePad = panelRect.sizeDelta.x * SidePaddingRatio;
        float fullRowWidth = Mathf.Max(1f, panelRect.sizeDelta.x - sidePad * 2f);

        for (int i = 0; i < contentBaselines.Count; i++)
        {
            RectBaseline b = contentBaselines[i];
            if (b.Rect == null)
                continue;

            b.Rect.localScale = b.LocalScale;

            if (IsNamed(b.Rect.name, "Title"))
            {
                // Stretch title keeps 10% padding via negative sizeDelta.x.
                b.Rect.anchoredPosition = new Vector2(0f, b.AnchoredPosition.y * factor);
                b.Rect.sizeDelta = new Vector2(-sidePad * 2f, b.SizeDelta.y * factor);
            }
            else if (IsFullWidthRow(b.Rect.name))
            {
                b.Rect.anchoredPosition = new Vector2(0f, b.AnchoredPosition.y * factor);
                b.Rect.sizeDelta = new Vector2(fullRowWidth, b.SizeDelta.y * factor);
            }
            else if (IsNamed(b.Rect.name, "Reset"))
            {
                float resetWidth = Mathf.Min(b.SizeDelta.x * factor, fullRowWidth);
                b.Rect.sizeDelta = new Vector2(resetWidth, b.SizeDelta.y * factor);
                b.Rect.anchoredPosition = new Vector2(0f, b.AnchoredPosition.y * factor);
            }
            else
            {
                b.Rect.anchoredPosition = new Vector2(
                    b.AnchoredPosition.x * factor,
                    b.AnchoredPosition.y * factor);
                float width = b.SizeDelta.x * factor;
                if (width > fullRowWidth)
                    width = fullRowWidth;
                b.Rect.sizeDelta = new Vector2(width, b.SizeDelta.y * factor);
            }
        }

        for (int i = 0; i < textBaselines.Count; i++)
        {
            TextBaseline b = textBaselines[i];
            if (b.Text == null)
                continue;
            b.Text.fontSize = Mathf.RoundToInt(b.FontSize * factor);
        }

        for (int i = 0; i < toggleBaselines.Count; i++)
        {
            ToggleBaseline b = toggleBaselines[i];
            if (b.Rect == null)
                continue;

            AlignToggle(b.Rect);
            b.Rect.localScale = b.LocalScale * factor;
        }

        lastIsLandscape = landscape;
        applied = true;
    }

    static bool IsFullWidthRow(string objectName)
    {
        for (int i = 0; i < FullWidthRowNames.Length; i++)
        {
            if (IsNamed(objectName, FullWidthRowNames[i]))
                return true;
        }

        return false;
    }

    static bool IsNamed(string objectName, string expected)
    {
        return objectName == expected || objectName.StartsWith(expected);
    }

    Transform FindDirectChild(string objectName)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == objectName)
                return child;
        }

        // Prefab instances may keep a display name with suffixes; match starts-with for Reset/Back.
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith(objectName))
                return child;
        }

        return null;
    }

    static Transform FindDeepChild(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
