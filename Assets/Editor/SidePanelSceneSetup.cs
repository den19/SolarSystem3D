#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SidePanelSceneSetup
{
    const string MenuPath = "Solar System/Setup SidePanel UI";
    const string GearIconPath = "Assets/Icons/icons8-settings-256.png";
    const float PanelWidth = 220f;
    const float PanelHeight = 392f;
    const float RowHeight = 34f;
    const float RowSpacing = 8f;
    const float MenuButtonSize = 44f;
    const float ButtonGap = 12f;
    static readonly Vector2 MenuButtonFallbackPosition = new Vector2(-8f, -63f);
    const float IconPadding = 4f;
    static readonly Color IconColor = new Color(0.85f, 0.92f, 1f, 1f);
    static readonly (string rowName, string labelName)[] ToggleRows =
    {
        ("SidePanelOrbitsLabel_Row", "SidePanelOrbitsLabel"),
        ("SidePanelGravityGridLabel_Row", "GravityGridLabel"),
        ("SidePanelLabelsLabel_Row", "SidePanelLabelsLabel"),
        ("SidePanelMinimapLabel_Row", "SidePanelMinimapLabel"),
        ("SidePanelUiLabel_Row", "SidePanelUiLabel"),
        ("SidePanelRealDistancesLabel_Row", "SidePanelRealDistancesLabel"),
        ("SidePanelRealSizesLabel_Row", "SidePanelRealSizesLabel"),
        ("SidePanelRealOrbitsLabel_Row", "SidePanelRealOrbitsLabel")
    };

    [InitializeOnLoadMethod]
    static void RegisterSceneOpened()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (!scene.path.Replace('\\', '/').EndsWith("Assets/_Scenes/Level1.unity"))
            return;

        SetupInternal(markSceneDirty: Object.FindFirstObjectByType<SimulationSidePanelController>() == null);
    }

    [MenuItem(MenuPath)]
    public static void SetupFromMenu()
    {
        SetupInternal(markSceneDirty: true);
    }

    public static void ExecuteBatchSetup()
    {
        const string scenePath = "Assets/_Scenes/Level1.unity";
        EditorSceneManager.OpenScene(scenePath);
        SetupInternal(markSceneDirty: true);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorApplication.Exit(0);
    }

    static void SetupInternal(bool markSceneDirty)
    {
        Transform canvasTransform = FindMainScreenCanvas();
        if (canvasTransform == null)
        {
            Debug.LogError("MainScreenCanvas not found. Open Level1 scene first.");
            return;
        }

        Button menuButton = EnsureMenuButton(canvasTransform);
        SimulationSidePanelController controller = EnsurePanel(canvasTransform, menuButton);
        RemoveLegacyGravityGridUi(canvasTransform);

        if (markSceneDirty)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("SidePanel UI setup complete on MainScreenCanvas.");
        }

        Selection.activeGameObject = controller.gameObject;
    }

    static Transform FindMainScreenCanvas()
    {
        GameObject canvasGo = GameObject.Find("MainScreenCanvas");
        return canvasGo != null ? canvasGo.transform : null;
    }

    static Button EnsureMenuButton(Transform canvasTransform)
    {
        Transform existing = canvasTransform.Find("SidePanelMenuButton");
        if (existing != null)
        {
            UpgradeMenuButton(existing);
            return existing.GetComponent<Button>();
        }

        var buttonGo = new GameObject("SidePanelMenuButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.layer = LayerMask.NameToLayer("UI");
        buttonGo.transform.SetParent(canvasTransform, false);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(MenuButtonSize, MenuButtonSize);
        LayoutMenuButtonRelativeToSimControl(canvasTransform, rect);

        StyleMenuButton(buttonGo);
        EnsureMenuButtonIcon(buttonGo.transform);

        return buttonGo.GetComponent<Button>();
    }

    static void UpgradeMenuButton(Transform button)
    {
        var rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(MenuButtonSize, MenuButtonSize);
        LayoutMenuButtonRelativeToSimControl(button.parent, rect);

        StyleMenuButton(button.gameObject);
        EnsureMenuButtonIcon(button);

        Transform label = button.Find("Label");
        if (label != null)
            Object.DestroyImmediate(label.gameObject);
    }

    static void LayoutMenuButtonRelativeToSimControl(Transform canvasTransform, RectTransform menuButtonRect)
    {
        menuButtonRect.anchorMin = new Vector2(1f, 1f);
        menuButtonRect.anchorMax = new Vector2(1f, 1f);
        menuButtonRect.pivot = new Vector2(1f, 1f);

        if (canvasTransform == null)
        {
            menuButtonRect.anchoredPosition = MenuButtonFallbackPosition;
            return;
        }

        Transform simControlTransform = canvasTransform.Find("SimulationControlButton");
        if (simControlTransform == null || !simControlTransform.TryGetComponent(out RectTransform simControlRect))
        {
            menuButtonRect.anchoredPosition = MenuButtonFallbackPosition;
            return;
        }

        float y = simControlRect.anchoredPosition.y - simControlRect.rect.height - ButtonGap;
        menuButtonRect.anchoredPosition = new Vector2(simControlRect.anchoredPosition.x, y);
    }

    static void StyleMenuButton(GameObject buttonGo)
    {
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
    }

    static void EnsureMenuButtonIcon(Transform button)
    {
        Transform iconTransform = button.Find("Icon");
        GameObject iconGo;
        if (iconTransform != null)
        {
            iconGo = iconTransform.gameObject;
        }
        else
        {
            iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.layer = button.gameObject.layer;
            iconGo.transform.SetParent(button, false);
        }

        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(IconPadding, IconPadding);
        iconRect.offsetMax = new Vector2(-IconPadding, -IconPadding);

        var iconImage = iconGo.GetComponent<Image>();
        iconImage.sprite = LoadGearIconSprite();
        iconImage.color = IconColor;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        var buttonComponent = button.GetComponent<Button>();
        buttonComponent.targetGraphic = iconImage;
    }

    static Sprite LoadGearIconSprite()
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(GearIconPath);
    }

    static SimulationSidePanelController EnsurePanel(Transform canvasTransform, Button menuButton)
    {
        Transform existing = canvasTransform.Find("SimulationSidePanel");
        GameObject panelGo;
        if (existing != null)
        {
            panelGo = existing.gameObject;
        }
        else
        {
            panelGo = new GameObject("SimulationSidePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelGo.layer = LayerMask.NameToLayer("UI");
            panelGo.transform.SetParent(canvasTransform, false);

            var rect = panelGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var image = panelGo.GetComponent<Image>();
            image.color = new Color(0.02f, 0.05f, 0.12f, 0.82f);
            image.raycastTarget = true;
        }

        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        var controller = panelGo.GetComponent<SimulationSidePanelController>();
        if (controller == null)
            controller = panelGo.AddComponent<SimulationSidePanelController>();

        float y = 70f;
        Toggle orbitsToggle = null;
        Toggle gravityGridToggle = null;
        Toggle labelsToggle = null;
        Toggle minimapToggle = null;
        Toggle uiToggle = null;
        Toggle realDistancesToggle = null;
        Toggle realSizesToggle = null;
        Toggle realOrbitsToggle = null;

        for (int i = 0; i < ToggleRows.Length; i++)
        {
            Toggle toggle = EnsureToggleRow(panelGo.transform, ToggleRows[i].rowName, ToggleRows[i].labelName, ref y, i);
            if (toggle == null)
                continue;

            switch (ToggleRows[i].rowName)
            {
                case "SidePanelOrbitsLabel_Row": orbitsToggle = toggle; break;
                case "SidePanelGravityGridLabel_Row": gravityGridToggle = toggle; break;
                case "SidePanelLabelsLabel_Row": labelsToggle = toggle; break;
                case "SidePanelMinimapLabel_Row": minimapToggle = toggle; break;
                case "SidePanelUiLabel_Row": uiToggle = toggle; break;
                case "SidePanelRealDistancesLabel_Row": realDistancesToggle = toggle; break;
                case "SidePanelRealSizesLabel_Row": realSizesToggle = toggle; break;
                case "SidePanelRealOrbitsLabel_Row": realOrbitsToggle = toggle; break;
            }
        }

        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("menuButton").objectReferenceValue = menuButton;
        serializedController.FindProperty("panelRect").objectReferenceValue = panelGo.GetComponent<RectTransform>();
        serializedController.FindProperty("orbitsToggle").objectReferenceValue = orbitsToggle;
        serializedController.FindProperty("gravityGridToggle").objectReferenceValue = gravityGridToggle;
        serializedController.FindProperty("labelsToggle").objectReferenceValue = labelsToggle;
        serializedController.FindProperty("minimapToggle").objectReferenceValue = minimapToggle;
        serializedController.FindProperty("uiToggle").objectReferenceValue = uiToggle;
        serializedController.FindProperty("realDistancesToggle").objectReferenceValue = realDistancesToggle;
        serializedController.FindProperty("realSizesToggle").objectReferenceValue = realSizesToggle;
        serializedController.FindProperty("realOrbitsToggle").objectReferenceValue = realOrbitsToggle;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        if (realOrbitsToggle != null)
            realOrbitsToggle.isOn = false;

        return controller;
    }

    static void RemoveLegacyGravityGridUi(Transform canvasTransform)
    {
        Transform legacy = canvasTransform.Find("GravityGrid");
        if (legacy == null)
            return;

        Object.DestroyImmediate(legacy.gameObject);
    }

    static Toggle EnsureToggleRow(Transform panel, string rowName, string labelName, ref float y, int siblingIndex)
    {
        Transform rowTransform = panel.Find(rowName);
        GameObject rowGo;
        if (rowTransform != null)
        {
            rowGo = rowTransform.gameObject;
        }
        else
        {
            rowGo = new GameObject(rowName, typeof(RectTransform));
            rowGo.layer = panel.gameObject.layer;
            rowGo.transform.SetParent(panel, false);
        }

        rowGo.transform.SetSiblingIndex(siblingIndex);

        var rowRect = rowGo.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, -y);
        rowRect.sizeDelta = new Vector2(-20f, RowHeight);

        y += RowHeight + RowSpacing;

        EnsureLabel(rowGo.transform, labelName);
        return EnsureToggle(rowGo.transform);
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

            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.68f, 1f);
            labelRect.offsetMin = new Vector2(8f, 0f);
            labelRect.offsetMax = Vector2.zero;

            var label = labelGo.GetComponent<Text>();
            label.fontSize = 14;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = new Color(0.92f, 0.95f, 1f, 1f);
            label.text = labelName;
        }

        if (labelGo.GetComponent<LocalizedText>() == null)
            labelGo.AddComponent<LocalizedText>();
    }

    static Toggle EnsureToggle(Transform row)
    {
        Transform toggleTransform = row.Find("Toggle");
        if (toggleTransform != null)
            return toggleTransform.GetComponent<Toggle>();

        var toggleGo = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
        toggleGo.layer = row.gameObject.layer;
        toggleGo.transform.SetParent(row, false);

        var toggleRect = toggleGo.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.72f, 0.15f);
        toggleRect.anchorMax = new Vector2(0.95f, 0.85f);
        toggleRect.offsetMin = Vector2.zero;
        toggleRect.offsetMax = Vector2.zero;

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
        toggle.isOn = true;
        return toggle;
    }
}
#endif
