#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TimeControlSceneSetup
{
    const string MenuPath = "Solar System/Setup Time Control UI";
    const string RoundedPanelSpritePath = "Assets/Unity UI Samples/Textures and Sprites/Rounded UI/UIPanel.png";
    const string ChevronLeftPath = "Assets/Icons/chevron-left.png";
    const string ChevronRightPath = "Assets/Icons/chevron-right.png";
    const string AntonFontPath = "Assets/Resources/Fonts & Materials/Anton SDF.asset";
    const float IconPadding = 8f;

    static readonly Color IconColor = TimeControlUiBootstrap.IconColor;

    [InitializeOnLoadMethod]
    static void RegisterSceneOpened()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (!scene.path.Replace('\\', '/').EndsWith("Assets/_Scenes/Level1.unity"))
            return;

        Transform canvasTransform = FindMainScreenCanvas();
        if (canvasTransform == null)
            return;

        EnsureTimeControlBar(canvasTransform);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    [MenuItem(MenuPath)]
    public static void SetupFromMenu()
    {
        Transform canvasTransform = FindMainScreenCanvas();
        if (canvasTransform == null)
        {
            Debug.LogError("MainScreenCanvas not found. Open Level1 scene first.");
            return;
        }

        SimulationTimeControlController controller = EnsureTimeControlBar(canvasTransform);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = controller.gameObject;
        Debug.Log("Time Control UI setup complete on MainScreenCanvas.");
    }

    public static void EnsureTimeControlBarPublic(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return;

        EnsureTimeControlBar(canvasTransform);
    }

    public static void ExecuteBatchSetup()
    {
        const string scenePath = "Assets/_Scenes/Level1.unity";
        EditorSceneManager.OpenScene(scenePath);
        Transform canvasTransform = FindMainScreenCanvas();
        if (canvasTransform != null)
            EnsureTimeControlBar(canvasTransform);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorApplication.Exit(0);
    }

    static SimulationTimeControlController EnsureTimeControlBar(Transform canvasTransform)
    {
        Transform existing = canvasTransform.Find(TimeControlUiBootstrap.BarObjectName);
        GameObject barGo;
        if (existing != null)
        {
            barGo = existing.gameObject;
        }
        else
        {
            barGo = new GameObject(TimeControlUiBootstrap.BarObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
            barGo.layer = LayerMask.NameToLayer("UI");
            barGo.transform.SetParent(canvasTransform, false);
        }

        var barRect = barGo.GetComponent<RectTransform>();
        var barImage = EnsureComponent<Image>(barGo);
        barImage.sprite = LoadRoundedPanelSprite();
        barImage.type = Image.Type.Sliced;
        barImage.color = TimeControlUiBootstrap.BarBackgroundColor;
        barImage.raycastTarget = true;

        var layout = EnsureComponent<HorizontalLayoutGroup>(barGo);
        TimeControlUiBootstrap.ApplyBarLayoutGroup(layout);

        Button pauseButton = EnsurePausePlayButton(barGo.transform);
        Button speedDownButton = EnsureSpeedButton(barGo.transform, "SpeedDownButton", LoadChevronLeftSprite(), true);
        TMP_Text speedLabel = EnsureSpeedLabel(barGo.transform);
        Button speedUpButton = EnsureSpeedButton(barGo.transform, "SpeedUpButton", LoadChevronRightSprite(), false);

        var controller = EnsureComponent<SimulationTimeControlController>(barGo);
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("barRect").objectReferenceValue = barRect;
        serialized.FindProperty("pausePlayButton").objectReferenceValue = pauseButton;
        serialized.FindProperty("speedDownButton").objectReferenceValue = speedDownButton;
        serialized.FindProperty("speedUpButton").objectReferenceValue = speedUpButton;
        serialized.FindProperty("speedLabel").objectReferenceValue = speedLabel;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        TimeControlUiBootstrap.ApplyControlSizes(barGo.transform);
        controller.RefreshSafeAreaLayout();
        return controller;
    }

    static Button EnsurePausePlayButton(Transform parent)
    {
        Transform existing = parent.Find("PausePlayButton");
        GameObject buttonGo;
        if (existing != null)
        {
            buttonGo = existing.gameObject;
        }
        else
        {
            buttonGo = new GameObject("PausePlayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGo.layer = parent.gameObject.layer;
            buttonGo.transform.SetParent(parent, false);
            buttonGo.transform.SetSiblingIndex(0);
        }

        StyleToolbarButton(buttonGo);
        ApplyButtonRectSize(buttonGo.GetComponent<RectTransform>());
        ApplyButtonLayout(buttonGo.GetComponent<LayoutElement>());

        Transform label = EnsureChild(buttonGo.transform, "Label");
        var labelText = EnsureComponent<TextMeshProUGUI>(label.gameObject);
        labelText.text = "II";
        labelText.font = LoadAntonFont();
        labelText.fontSize = TimeControlUiBootstrap.PausePlayFontSize;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = IconColor;
        labelText.raycastTarget = false;
        StretchFull(label.GetComponent<RectTransform>());

        return buttonGo.GetComponent<Button>();
    }

    static Button EnsureSpeedButton(Transform parent, string name, Sprite iconSprite, bool isDown)
    {
        Transform existing = parent.Find(name);
        GameObject buttonGo;
        if (existing != null)
        {
            buttonGo = existing.gameObject;
        }
        else
        {
            buttonGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGo.layer = parent.gameObject.layer;
            buttonGo.transform.SetParent(parent, false);
            buttonGo.transform.SetSiblingIndex(isDown ? 1 : 3);
        }

        StyleToolbarButton(buttonGo);
        ApplyButtonRectSize(buttonGo.GetComponent<RectTransform>());
        ApplyButtonLayout(buttonGo.GetComponent<LayoutElement>());

        Transform icon = EnsureChild(buttonGo.transform, "Icon");
        var iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(IconPadding, IconPadding);
        iconRect.offsetMax = new Vector2(-IconPadding, -IconPadding);

        var iconImage = EnsureComponent<Image>(icon.gameObject);
        iconImage.sprite = iconSprite;
        iconImage.color = IconColor;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        var button = buttonGo.GetComponent<Button>();
        button.targetGraphic = iconImage;

        return button;
    }

    static TMP_Text EnsureSpeedLabel(Transform parent)
    {
        Transform existing = parent.Find("TimeControlSpeedFormat");
        GameObject labelGo;
        if (existing != null)
        {
            labelGo = existing.gameObject;
        }
        else
        {
            labelGo = new GameObject("TimeControlSpeedFormat", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelGo.layer = parent.gameObject.layer;
            labelGo.transform.SetParent(parent, false);
            labelGo.transform.SetSiblingIndex(2);
        }

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(TimeControlUiBootstrap.SpeedLabelMinWidth, TimeControlUiBootstrap.ButtonSize);

        var layoutElement = EnsureComponent<LayoutElement>(labelGo);
        layoutElement.minWidth = TimeControlUiBootstrap.SpeedLabelMinWidth;
        layoutElement.preferredWidth = TimeControlUiBootstrap.SpeedLabelMinWidth;
        layoutElement.minHeight = TimeControlUiBootstrap.ButtonSize;
        layoutElement.preferredHeight = TimeControlUiBootstrap.ButtonSize;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        var labelText = EnsureComponent<TextMeshProUGUI>(labelGo);
        labelText.text = "1x";
        labelText.font = LoadAntonFont();
        labelText.fontSize = TimeControlUiBootstrap.SpeedLabelFontSize;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = TimeControlUiBootstrap.SpeedLabelColor;
        labelText.raycastTarget = false;

        return labelText;
    }

    static void StyleToolbarButton(GameObject buttonGo)
    {
        var image = EnsureComponent<Image>(buttonGo);
        image.sprite = null;
        image.color = Color.clear;
        image.raycastTarget = true;

        var button = EnsureComponent<Button>(buttonGo);
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.78f, 0.88f, 1f, 1f);
        colors.pressedColor = new Color(0.65f, 0.78f, 0.95f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
    }

    static void ApplyButtonRectSize(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.sizeDelta = new Vector2(TimeControlUiBootstrap.ButtonSize, TimeControlUiBootstrap.ButtonSize);
    }

    static void ApplyButtonLayout(LayoutElement layoutElement)
    {
        if (layoutElement == null)
            return;

        layoutElement.minWidth = TimeControlUiBootstrap.ButtonSize;
        layoutElement.minHeight = TimeControlUiBootstrap.ButtonSize;
        layoutElement.preferredWidth = TimeControlUiBootstrap.ButtonSize;
        layoutElement.preferredHeight = TimeControlUiBootstrap.ButtonSize;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }

    static Transform EnsureChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            return child;

        var childGo = new GameObject(childName, typeof(RectTransform));
        childGo.layer = parent.gameObject.layer;
        childGo.transform.SetParent(parent, false);
        return childGo.transform;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
    {
        if (!go.TryGetComponent(out T component))
            component = go.AddComponent<T>();
        return component;
    }

    static Transform FindMainScreenCanvas()
    {
        GameObject canvasGo = GameObject.Find("MainScreenCanvas");
        return canvasGo != null ? canvasGo.transform : null;
    }

    static Sprite LoadRoundedPanelSprite() => AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPanelSpritePath);
    static Sprite LoadChevronLeftSprite() => AssetDatabase.LoadAssetAtPath<Sprite>(ChevronLeftPath);
    static Sprite LoadChevronRightSprite() => AssetDatabase.LoadAssetAtPath<Sprite>(ChevronRightPath);
    static TMP_FontAsset LoadAntonFont() => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AntonFontPath);
}
#endif
