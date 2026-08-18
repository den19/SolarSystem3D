#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Inserts RuStore and Write-to-developer buttons under BackButton on the live
/// MainMenu AboutPanel. Scene-static UI only — runtime just opens URL/email.
/// </summary>
public static class AboutDeveloperLinksSceneSetup
{
    const string MenuPath = "Solar System/Setup About Developer Links UI";
    const string ScenePath = "Assets/_Scenes/MainMenu.unity";
    const string RuStoreButtonName = "RuStoreButton";
    const string WriteButtonName = "WriteToDeveloperButton";
    const string WriteLabelName = "WriteToDeveloper";
    const string RuStoreIconName = "RuStoreIcon";
    const string RuStoreLabelName = "RuStoreLabel";
    const string RuStoreFallbackName = "RuStoreFallback";
    const string LogoPath = "Assets/Icons/rustore-logo.png";
    const string RoundedPanelSpritePath = "Assets/Unity UI Samples/Textures and Sprites/Rounded UI/UIPanel.png";

    const float MaxWidth = 860f;
    const float GapAfterBack = 28f;
    const float GapBetweenButtons = 20f;
    const float RuStoreWidth = 560f;
    const float RuStoreHeight = 120f;
    const float RuStoreIconSize = 80f;
    const float RuStoreIconLeft = 28f;
    const float RuStoreIconLabelGap = 16f;
    const float RuStoreLabelRightInset = 24f;
    const float RuStoreLabelInsetY = 8f;
    const float WriteWidth = 860f;
    const float WriteHeight = 120f;
    const float WriteLabelInsetX = 20f;
    const float WriteLabelInsetY = 8f;

    static readonly Color EmailAccent = new Color(0f, 0.55f, 1f, 1f);
    static readonly Color EmailHitArea = new Color(0f, 0.55f, 1f, 0.16f);
    static readonly Color RuStorePurple = new Color(0.48f, 0.118f, 0.631f, 1f);
    static readonly Color RuStoreButtonBack = Color.white;
    static readonly Color RuStoreLabelColor = new Color(0.129f, 0.129f, 0.129f, 1f);

    [MenuItem(MenuPath)]
    public static void SetupFromMenu()
    {
        if (!EnsureMainMenuOpen())
            return;

        SetupInternal(markDirty: true);
        Debug.Log("About developer links UI setup complete on MainMenu AboutPanel.");
    }

    public static void ExecuteBatchSetup()
    {
        EditorSceneManager.OpenScene(ScenePath);
        bool ok = SetupInternal(markDirty: true);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool EnsureMainMenuOpen()
    {
        if (SceneManager.GetActiveScene().path.Replace('\\', '/').EndsWith(ScenePath))
            return true;

        EditorSceneManager.OpenScene(ScenePath);
        return true;
    }

    static bool SetupInternal(bool markDirty)
    {
        Transform inner = FindInnerAboutContent();
        if (inner == null)
        {
            Debug.LogError("Inner AboutPanel (with BackButton) not found in MainMenu.");
            return false;
        }

        Transform rootWindow = inner.parent;
        if (rootWindow == null || rootWindow.name != "AboutPanel")
        {
            Debug.LogError("Root AboutPanel window not found above inner content.");
            return false;
        }

        Transform back = inner.Find("BackButton");
        var backRect = back as RectTransform;
        if (backRect == null)
        {
            Debug.LogError("BackButton not found under inner AboutPanel.");
            return false;
        }

        var controller = EnsureComponent<AboutDeveloperLinksController>(rootWindow.gameObject);
        AudioManager audio = FindSceneAudioManager();
        Font uiFont = LoadUiFont(back);

        Button rustoreButton = EnsureRuStoreButton(inner, backRect, uiFont);
        Button writeButton = EnsureWriteButton(inner, backRect, uiFont);

        LayoutRuStoreButton(rustoreButton.transform as RectTransform, backRect);
        LayoutWriteButton(writeButton.transform as RectTransform, backRect);

        rustoreButton.transform.SetSiblingIndex(back.GetSiblingIndex() + 1);
        writeButton.transform.SetSiblingIndex(back.GetSiblingIndex() + 2);

        RewireButton(rustoreButton, controller.OpenRuStorePage, audio);
        RewireButton(writeButton, controller.WriteToDeveloper, audio);

        Debug.Log(
            "About developer links layout: RuStore y=" +
            ((RectTransform)rustoreButton.transform).anchoredPosition.y +
            " size=" + RuStoreWidth + "x" + RuStoreHeight +
            "; WriteToDeveloper y=" +
            ((RectTransform)writeButton.transform).anchoredPosition.y +
            " size=" + WriteWidth + "x" + WriteHeight +
            "; BackButton unchanged y=" + backRect.anchoredPosition.y);

        if (markDirty)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        return true;
    }

    static Transform FindInnerAboutContent()
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t == null || t.name != "AboutPanel")
                continue;
            if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded)
                continue;
            if (t.Find("BackButton") != null)
                return t;
        }

        return null;
    }

    static AudioManager FindSceneAudioManager()
    {
        AudioManager[] managers = Resources.FindObjectsOfTypeAll<AudioManager>();
        for (int i = 0; i < managers.Length; i++)
        {
            AudioManager manager = managers[i];
            if (manager == null)
                continue;
            if (!manager.gameObject.scene.IsValid() || !manager.gameObject.scene.isLoaded)
                continue;
            return manager;
        }

        return Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
    }

    static Button EnsureRuStoreButton(Transform inner, RectTransform backRect, Font uiFont)
    {
        Transform existing = inner.Find(RuStoreButtonName);
        GameObject buttonGo;
        if (existing != null)
        {
            buttonGo = existing.gameObject;
        }
        else
        {
            buttonGo = new GameObject(
                RuStoreButtonName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonGo.layer = inner.gameObject.layer;
            buttonGo.transform.SetParent(inner, false);
        }

        var image = EnsureComponent<Image>(buttonGo);
        Sprite logo = LoadRuStoreLogo();
        Sprite rounded = LoadRoundedPanelSprite();

        image.sprite = rounded;
        image.type = rounded != null ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = true;

        Transform fallback = buttonGo.transform.Find(RuStoreFallbackName);
        if (logo != null)
        {
            image.color = RuStoreButtonBack;
            EnsureRuStoreIcon(buttonGo.transform, logo);
            EnsureRuStoreLabel(buttonGo.transform, uiFont);
            if (fallback != null)
                fallback.gameObject.SetActive(false);
        }
        else
        {
            image.color = RuStorePurple;
            SetChildActive(buttonGo.transform, RuStoreIconName, false);
            SetChildActive(buttonGo.transform, RuStoreLabelName, false);
            EnsureRuStoreFallbackLabel(buttonGo.transform, uiFont);
        }

        var button = EnsureComponent<Button>(buttonGo);
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = MakePressedColors(Color.white);
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        LayoutRuStoreButton(buttonGo.GetComponent<RectTransform>(), backRect);
        return button;
    }

    static void EnsureRuStoreIcon(Transform button, Sprite logo)
    {
        Transform icon = button.Find(RuStoreIconName);
        if (icon == null)
        {
            var go = new GameObject(RuStoreIconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = button.gameObject.layer;
            go.transform.SetParent(button, false);
            icon = go.transform;
        }

        icon.gameObject.SetActive(true);
        icon.SetSiblingIndex(0);

        var rect = icon.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(RuStoreIconSize, RuStoreIconSize);
        rect.anchoredPosition = new Vector2(RuStoreIconLeft, 0f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        var image = EnsureComponent<Image>(icon.gameObject);
        image.sprite = logo;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    static void EnsureRuStoreLabel(Transform button, Font uiFont)
    {
        Transform label = button.Find(RuStoreLabelName);
        if (label == null)
        {
            var go = new GameObject(RuStoreLabelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.layer = button.gameObject.layer;
            go.transform.SetParent(button, false);
            label = go.transform;
        }

        label.gameObject.SetActive(true);
        label.SetSiblingIndex(1);

        var rect = label.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        float left = RuStoreIconLeft + RuStoreIconSize + RuStoreIconLabelGap;
        rect.offsetMin = new Vector2(left, RuStoreLabelInsetY);
        rect.offsetMax = new Vector2(-RuStoreLabelRightInset, -RuStoreLabelInsetY);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        var text = EnsureComponent<Text>(label.gameObject);
        text.font = uiFont;
        text.text = "RuStore";
        text.alignment = TextAnchor.MiddleLeft;
        text.color = RuStoreLabelColor;
        text.fontSize = 42;
        text.fontStyle = FontStyle.Bold;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 28;
        text.resizeTextMaxSize = 48;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
    }

    static void EnsureRuStoreFallbackLabel(Transform button, Font uiFont)
    {
        Transform label = button.Find(RuStoreFallbackName);
        if (label == null)
        {
            var go = new GameObject(RuStoreFallbackName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.layer = button.gameObject.layer;
            go.transform.SetParent(button, false);
            label = go.transform;
        }

        label.gameObject.SetActive(true);
        var rect = label.GetComponent<RectTransform>();
        StretchWithInset(rect, 16f, 8f);

        var text = EnsureComponent<Text>(label.gameObject);
        text.font = uiFont;
        text.text = "RuStore";
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = 40;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 24;
        text.resizeTextMaxSize = 44;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
    }

    static Button EnsureWriteButton(Transform inner, RectTransform backRect, Font uiFont)
    {
        Transform existing = inner.Find(WriteButtonName);
        GameObject buttonGo;
        if (existing != null)
        {
            buttonGo = existing.gameObject;
        }
        else
        {
            buttonGo = new GameObject(
                WriteButtonName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonGo.layer = inner.gameObject.layer;
            buttonGo.transform.SetParent(inner, false);
        }

        var image = EnsureComponent<Image>(buttonGo);
        Sprite rounded = LoadRoundedPanelSprite();
        image.sprite = rounded;
        image.type = rounded != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = EmailHitArea;
        image.raycastTarget = true;
        image.preserveAspect = false;

        var button = EnsureComponent<Button>(buttonGo);
        button.transition = Selectable.Transition.ColorTint;
        button.colors = MakePressedColors(Color.white);
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        Transform label = buttonGo.transform.Find(WriteLabelName);
        if (label == null)
        {
            var go = new GameObject(WriteLabelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.layer = buttonGo.layer;
            go.transform.SetParent(buttonGo.transform, false);
            label = go.transform;
        }

        StretchWithInset(label.GetComponent<RectTransform>(), WriteLabelInsetX, WriteLabelInsetY);

        var text = EnsureComponent<Text>(label.gameObject);
        text.font = uiFont;
        text.text = "Write to the developer (you can send improvement suggestions)";
        text.alignment = TextAnchor.MiddleCenter;
        text.color = EmailAccent;
        text.fontSize = 28;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 24;
        text.resizeTextMaxSize = 34;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.supportRichText = true;
        text.raycastTarget = false;

        EnsureComponent<LocalizedText>(label.gameObject);

        button.targetGraphic = image;
        LayoutWriteButton(buttonGo.GetComponent<RectTransform>(), backRect);
        return button;
    }

    static void LayoutRuStoreButton(RectTransform rect, RectTransform backRect)
    {
        if (rect == null || backRect == null)
            return;

        ApplyCenterAnchors(rect);
        rect.sizeDelta = new Vector2(Mathf.Min(RuStoreWidth, MaxWidth), RuStoreHeight);
        rect.anchoredPosition = new Vector2(0f, RuStoreCenterY(backRect));
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static void LayoutWriteButton(RectTransform rect, RectTransform backRect)
    {
        if (rect == null || backRect == null)
            return;

        ApplyCenterAnchors(rect);
        rect.sizeDelta = new Vector2(Mathf.Min(WriteWidth, MaxWidth), WriteHeight);
        rect.anchoredPosition = new Vector2(0f, WriteCenterY(backRect));
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static float BackBottom(RectTransform backRect)
    {
        return backRect.anchoredPosition.y - backRect.sizeDelta.y * backRect.pivot.y;
    }

    static float RuStoreCenterY(RectTransform backRect)
    {
        return BackBottom(backRect) - GapAfterBack - RuStoreHeight * 0.5f;
    }

    static float WriteCenterY(RectTransform backRect)
    {
        float rustoreBottom = RuStoreCenterY(backRect) - RuStoreHeight * 0.5f;
        return rustoreBottom - GapBetweenButtons - WriteHeight * 0.5f;
    }

    static void ApplyCenterAnchors(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    static void StretchWithInset(RectTransform rect, float insetX, float insetY)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(insetX, insetY);
        rect.offsetMax = new Vector2(-insetX, -insetY);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static void RewireButton(Button button, UnityAction action, AudioManager audio)
    {
        if (button == null || action == null)
            return;

        var so = new SerializedObject(button);
        SerializedProperty onClick = so.FindProperty("m_OnClick");
        SerializedProperty calls = onClick != null
            ? onClick.FindPropertyRelative("m_PersistentCalls.m_Calls")
            : null;
        if (calls != null)
        {
            calls.ClearArray();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        UnityEventTools.AddVoidPersistentListener(button.onClick, action);
        if (audio != null)
            UnityEventTools.AddVoidPersistentListener(button.onClick, audio.PlaySound);
    }

    static ColorBlock MakePressedColors(Color normal)
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = normal;
        colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.72f, 0.78f, 0.86f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        return colors;
    }

    static Sprite LoadRuStoreLogo()
    {
        EnsureLogoImportedAsSprite();
        return AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
    }

    static void EnsureLogoImportedAsSprite()
    {
        var importer = AssetImporter.GetAtPath(LogoPath) as TextureImporter;
        if (importer == null)
            return;

        bool dirty = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            dirty = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            dirty = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            dirty = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            dirty = true;
        }

        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            dirty = true;
        }

        if (dirty)
            importer.SaveAndReimport();
    }

    static void SetChildActive(Transform parent, string childName, bool active)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            child.gameObject.SetActive(active);
    }

    static Sprite LoadRoundedPanelSprite() => AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPanelSpritePath);

    static Font LoadUiFont(Transform back)
    {
        Text backText = back != null ? back.GetComponentInChildren<Text>(true) : null;
        if (backText != null && backText.font != null)
            return backText.font;

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
    {
        if (!go.TryGetComponent(out T component))
            component = go.AddComponent<T>();
        return component;
    }
}
#endif
