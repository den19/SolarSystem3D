using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ensures SidePanel rows exist and applies compact layout at runtime and in editor setup.
/// </summary>
public static class SidePanelUiBootstrap
{
    public const float RowHeight = 28f;
    public const float RowSpacing = 4f;
    public const float RowStartY = 8f;
    public const float PanelBottomPadding = 8f;
    public const float PanelWidth = 270f;
    public const float BarHeight = 56f;
    public const float BarHorizontalMargin = 8f;
    public const float BarTopMargin = 8f;
    const float RowHorizontalInset = 20f;
    const float LabelLeftOffset = 8f;
    const float LabelMaxAnchorX = 0.82f;
    const float ToggleMinAnchorX = 0.84f;
    const float ToggleMaxAnchorX = 0.96f;
    const int LabelFontSize = 12;

    public static readonly (string rowName, string labelName)[] ToggleRows =
    {
        ("SidePanelOrbitsLabel_Row", "SidePanelOrbitsLabel"),
        ("SidePanelGravityGridLabel_Row", "GravityGridLabel"),
        ("SidePanelProjectionLabel_Row", "SidePanelProjectionLabel"),
        ("SidePanelLabelsLabel_Row", "SidePanelLabelsLabel"),
        ("SidePanelMinimapLabel_Row", "SidePanelMinimapLabel"),
        ("SidePanelUiLabel_Row", "SidePanelUiLabel"),
        ("SidePanelScaleEducationalLabel_Row", "SidePanelScaleEducationalLabel"),
        ("SidePanelRealSizesLabel_Row", "SidePanelRealSizesLabel"),
        ("SidePanelRealOrbitsLabel_Row", "SidePanelRealOrbitsLabel"),
        ("SidePanelCometMovementLabel_Row", "SidePanelCometMovementLabel"),
        ("SidePanelFreeObservationLabel_Row", "SidePanelFreeObservationLabel"),
        ("SidePanelRealSunLabel_Row", "SidePanelRealSunLabel"),
        ("SidePanelTimeMachineLabel_Row", "SidePanelTimeMachineLabel")
    };

    public static float GetScaledPanelWidth(float scale) => PanelWidth * Mathf.Max(0.01f, scale);

    public static float ComputePanelHeight(int rowCount) => ComputePanelHeight(rowCount, 1f);

    public static float ComputePanelHeight(int rowCount, float scale)
    {
        scale = Mathf.Max(0.01f, scale);
        float rowStartY = RowStartY * scale;
        float bottomPadding = PanelBottomPadding * scale;
        if (rowCount <= 0)
            return rowStartY + bottomPadding;

        float rowHeight = RowHeight * scale;
        float rowSpacing = RowSpacing * scale;
        return rowStartY + rowCount * rowHeight + (rowCount - 1) * rowSpacing + bottomPadding;
    }

    public static void ApplyBarRectLayout(RectTransform bar, float safeLeft, float safeRight, float safeTop)
    {
        if (bar == null)
            return;

        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = Vector2.zero;
        bar.sizeDelta = Vector2.zero;

        float topInset = Mathf.Max(0f, safeTop + BarTopMargin);
        bar.offsetMin = new Vector2(safeLeft + BarHorizontalMargin, -(BarHeight + topInset));
        bar.offsetMax = new Vector2(-(safeRight + BarHorizontalMargin), -topInset);
    }

    public static void ApplyCompactLayout(Transform panel) => ApplyCompactLayout(panel, 1f);

    public static void ApplyCompactLayout(Transform panel, float scale)
    {
        if (panel == null)
            return;

        scale = Mathf.Max(0.01f, scale);
        RemoveOrphanRows(panel);

        float y = RowStartY * scale;
        for (int i = 0; i < ToggleRows.Length; i++)
        {
            Transform row = panel.Find(ToggleRows[i].rowName);
            if (row == null)
            {
                bool defaultOn = !IsDefaultOffRow(ToggleRows[i].rowName);
                row = EnsureToggleRow(panel, ToggleRows[i].rowName, ToggleRows[i].labelName, ref y, i, scale, defaultOn);
                if (row == null)
                    continue;
            }
            else
            {
                ApplyRowLayout(row, ref y, i, scale);
                ApplyRowChildLayouts(row, ToggleRows[i].labelName, scale);
            }
        }

        if (panel.TryGetComponent(out RectTransform panelRect))
            panelRect.sizeDelta = new Vector2(GetScaledPanelWidth(scale), ComputePanelHeight(ToggleRows.Length, scale));
    }

    static void RemoveOrphanRows(Transform panel)
    {
        for (int i = panel.childCount - 1; i >= 0; i--)
        {
            Transform child = panel.GetChild(i);
            if (!child.name.EndsWith("_Row"))
                continue;

            bool known = false;
            for (int j = 0; j < ToggleRows.Length; j++)
            {
                if (child.name == ToggleRows[j].rowName)
                {
                    known = true;
                    break;
                }
            }

            if (!known)
            {
                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    public static Toggle FindToggle(Transform panel, string rowName)
    {
        Transform row = panel.Find(rowName);
        if (row == null)
            return null;

        Transform toggleTransform = row.Find("Toggle");
        return toggleTransform != null ? toggleTransform.GetComponent<Toggle>() : null;
    }

    static void ApplyRowLayout(Transform row, ref float y, int siblingIndex, float scale)
    {
        row.SetSiblingIndex(siblingIndex);

        if (!row.TryGetComponent(out RectTransform rowRect))
            return;

        float rowHeight = RowHeight * scale;
        float rowSpacing = RowSpacing * scale;

        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, -y);
        rowRect.sizeDelta = new Vector2(-RowHorizontalInset * scale, rowHeight);

        y += rowHeight + rowSpacing;
    }

    static void ApplyRowChildLayouts(Transform row, string labelName, float scale)
    {
        if (!string.IsNullOrEmpty(labelName))
        {
            Transform label = row.Find(labelName);
            if (label != null)
                ApplyLabelLayout(label, scale);
        }
        else
        {
            foreach (Transform child in row)
            {
                if (child.name == "Toggle")
                    continue;

                ApplyLabelLayout(child, scale);
            }
        }

        Transform toggle = row.Find("Toggle");
        if (toggle != null)
            ApplyToggleLayout(toggle);
    }

    public static void ApplyLabelLayout(Transform labelTransform) => ApplyLabelLayout(labelTransform, 1f);

    public static void ApplyLabelLayout(Transform labelTransform, float scale)
    {
        if (labelTransform == null || !labelTransform.TryGetComponent(out RectTransform labelRect))
            return;

        scale = Mathf.Max(0.01f, scale);
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(LabelMaxAnchorX, 1f);
        labelRect.offsetMin = new Vector2(LabelLeftOffset * scale, 0f);
        labelRect.offsetMax = Vector2.zero;

        if (labelTransform.TryGetComponent(out Text label))
        {
            label.fontSize = Mathf.Max(1, Mathf.RoundToInt(LabelFontSize * scale));
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }

    public static void ApplyToggleLayout(Transform toggleTransform)
    {
        if (toggleTransform == null || !toggleTransform.TryGetComponent(out RectTransform toggleRect))
            return;

        toggleRect.anchorMin = new Vector2(ToggleMinAnchorX, 0.15f);
        toggleRect.anchorMax = new Vector2(ToggleMaxAnchorX, 0.85f);
        toggleRect.offsetMin = Vector2.zero;
        toggleRect.offsetMax = Vector2.zero;
    }

    static bool IsDefaultOffRow(string rowName)
    {
        return rowName == "SidePanelProjectionLabel_Row"
            || rowName == "SidePanelRealOrbitsLabel_Row"
            || rowName == "SidePanelFreeObservationLabel_Row"
            || rowName == "SidePanelTimeMachineLabel_Row";
    }

    static Transform EnsureToggleRow(Transform panel, string rowName, string labelName, ref float y, int siblingIndex, float scale, bool defaultOn = true)
    {
        var rowGo = new GameObject(rowName, typeof(RectTransform));
        rowGo.layer = panel.gameObject.layer;
        rowGo.transform.SetParent(panel, false);

        ApplyRowLayout(rowGo.transform, ref y, siblingIndex, scale);
        EnsureLabel(rowGo.transform, labelName);
        EnsureToggle(rowGo.transform, defaultOn: defaultOn);
        ApplyRowChildLayouts(rowGo.transform, labelName, scale);
        return rowGo.transform;
    }

    static void EnsureLabel(Transform row, string labelName)
    {
        Transform labelTransform = row.Find(labelName);
        GameObject labelGo;
        if (labelTransform != null)
        {
            labelGo = labelTransform.gameObject;
        }
        else
        {
            labelGo = new GameObject(labelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.layer = row.gameObject.layer;
            labelGo.transform.SetParent(row, false);

            var label = labelGo.GetComponent<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.color = new Color(0.92f, 0.95f, 1f, 1f);
            label.text = labelName;
        }

        if (labelGo.GetComponent<LocalizedText>() == null)
            labelGo.AddComponent<LocalizedText>();
    }

    static Toggle EnsureToggle(Transform row, bool defaultOn)
    {
        Transform toggleTransform = row.Find("Toggle");
        if (toggleTransform != null)
            return toggleTransform.GetComponent<Toggle>();

        var toggleGo = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
        toggleGo.layer = row.gameObject.layer;
        toggleGo.transform.SetParent(row, false);

        var backgroundGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundGo.layer = toggleGo.layer;
        backgroundGo.transform.SetParent(toggleGo.transform, false);

        var backgroundRect = backgroundGo.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        var backgroundImage = backgroundGo.GetComponent<Image>();
        backgroundImage.color = new Color(0.15f, 0.18f, 0.25f, 1f);

        var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        checkGo.layer = toggleGo.layer;
        checkGo.transform.SetParent(backgroundGo.transform, false);

        var checkRect = checkGo.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.15f, 0.15f);
        checkRect.anchorMax = new Vector2(0.85f, 0.85f);
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;

        var checkImage = checkGo.GetComponent<Image>();
        checkImage.color = new Color(0.45f, 0.85f, 1f, 1f);

        var toggle = toggleGo.GetComponent<Toggle>();
        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkImage;
        toggle.isOn = defaultOn;
        return toggle;
    }
}
