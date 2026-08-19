using SolarSystemApp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Probe launch HUD, camera modes, PIP views, and telemetry.
/// </summary>
public class ProbeHudController : MonoBehaviour
{
    public const string ObjectName = ProbeSystemController.HudObjectName;

    const float UpdateInterval = 0.1f;
    const float RowHeight = 56f;
    const float ButtonFontSize = 26f;
    const float TelemetryFontSize = 26f;
    const float CheckboxSize = 28f;
    const float SliderHeight = 36f;
    const float TelemetryWidth = 360f;
    const float TelemetryWidthLandscape = 400f;
    const float TelemetryHeight = 340f;
    const float BarWidthLandscape = 720f;
    const float BarHeightStandard = 380f;
    const float BarHeightCustom = 440f;
    const float PipWidthLandscape = 320f;
    const float PipHeightLandscape = 180f;
    const float PipWidthPortrait = 280f;
    const float PipHeightPortrait = 150f;
    const float PipFrameInset = 4f;
    const float PipCaptionHeight = 28f;

    RectTransform _root;
    RectTransform _bar;
    RectTransform _telemetry;
    RectTransform _forwardPip;
    RectTransform _rearLeftPip;
    RectTransform _rearRightPip;
    RawImage _forwardImage;
    RawImage _rearLeftImage;
    RawImage _rearRightImage;
    Slider _impulse;
    Slider _heading;
    Toggle _viewsToggle;
    Toggle _antennaToggle;
    Toggle _engineToggle;
    Toggle _shieldToggle;
    TextMeshProUGUI _telemetryText;
    GameObject _customRow;
    Button _burnButton;
    Button _postcardButton;
    float _nextTelemetry;

    public static ProbeHudController EnsureOnCanvas(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return null;

        Transform existing = canvasTransform.Find(ObjectName);
        if (existing != null)
        {
            var controller = existing.GetComponent<ProbeHudController>();
            if (controller == null)
                controller = existing.gameObject.AddComponent<ProbeHudController>();
            return controller;
        }

        var go = new GameObject(ObjectName, typeof(RectTransform), typeof(ProbeHudController));
        go.layer = canvasTransform.gameObject.layer;
        go.transform.SetParent(canvasTransform, false);
        var hud = go.GetComponent<ProbeHudController>();
        hud.Build();
        return hud;
    }

    void OnEnable()
    {
        if (ProbeSystemController.Instance != null)
            ProbeSystemController.Instance.StateChanged += RefreshState;
        ProbeSettings.LoadoutChanged += RefreshState;
        ProbeSettings.ShowProbeViewsChanged += RefreshPips;
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    void OnDisable()
    {
        if (ProbeSystemController.Instance != null)
            ProbeSystemController.Instance.StateChanged -= RefreshState;
        ProbeSettings.LoadoutChanged -= RefreshState;
        ProbeSettings.ShowProbeViewsChanged -= RefreshPips;
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    void OnLanguageChanged()
    {
        RefreshLabels();
        RefreshTelemetryNow();
    }

    void Start()
    {
        if (_root == null)
            Build();
        RefreshState();
    }

    void Update()
    {
        Layout();
        if (Time.unscaledTime < _nextTelemetry)
            return;

        _nextTelemetry = Time.unscaledTime + UpdateInterval;
        RefreshTelemetryNow();
        RefreshPips();
    }

    void Build()
    {
        _root = GetComponent<RectTransform>();
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = Vector2.zero;
        _root.offsetMax = Vector2.zero;

        _bar = CreatePanel("ProbeControlBar", new Color(0.08f, 0.1f, 0.16f, 0.92f));
        var layout = _bar.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        CreateModelRow(_bar);
        _customRow = CreateCustomRow(_bar).gameObject;
        CreateSliderRow(_bar, "ProbeImpulseLabel", "Impulse", out _impulse, 0.15f, 1f, ProbeSystemController.Instance != null ? ProbeSystemController.Instance.ImpulseNormalized : 0.55f);
        CreateSliderRow(_bar, "ProbeHeadingLabel", "Heading", out _heading, -180f, 180f, 0f);
        CreateActionRow(_bar);
        CreateCameraRow(_bar);

        _impulse.onValueChanged.AddListener(v =>
        {
            if (ProbeSystemController.Instance != null)
                ProbeSystemController.Instance.ImpulseNormalized = v;
        });
        _heading.onValueChanged.AddListener(v =>
        {
            if (ProbeSystemController.Instance != null)
                ProbeSystemController.Instance.AimHeadingDeg = v;
        });

        _telemetry = CreatePanel("ProbeTelemetry", new Color(0.02f, 0.05f, 0.1f, 0.78f));
        _telemetryText = CreateTmp(_telemetry, "ProbeTelemetryBody", TelemetryFontSize, TextAlignmentOptions.TopLeft);
        var le = _telemetryText.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 280f;
        _telemetryText.margin = new Vector4(10f, 8f, 10f, 8f);

        _forwardPip = CreatePip("ProbeViewForwardImage", string.Empty, out _forwardImage);
        _rearLeftPip = CreatePip("ProbeViewRearLeftImage", "◀", out _rearLeftImage);
        _rearRightPip = CreatePip("ProbeViewRearRightImage", "▶", out _rearRightImage);
    }

    void CreateModelRow(RectTransform parent)
    {
        var row = CreateRow(parent, "ProbeModelRow");
        CreateModeButton(row, "ProbeModelVoyager", ProbeModelKind.Voyager);
        CreateModeButton(row, "ProbeModelNewHorizons", ProbeModelKind.NewHorizons);
        CreateModeButton(row, "ProbeModelJuno", ProbeModelKind.Juno);
        CreateModeButton(row, "ProbeModelCustom", ProbeModelKind.Custom);
    }

    RectTransform CreateCustomRow(RectTransform parent)
    {
        var row = CreateRow(parent, "ProbeCustomRow");
        _antennaToggle = CreateLabeledToggle(row, "ProbePartAntenna", ProbeSettings.CustomAntenna, ProbeSettings.SetCustomAntenna);
        _engineToggle = CreateLabeledToggle(row, "ProbePartEngine", ProbeSettings.CustomEngine, ProbeSettings.SetCustomEngine);
        _shieldToggle = CreateLabeledToggle(row, "ProbePartShield", ProbeSettings.CustomShield, ProbeSettings.SetCustomShield);
        return row;
    }

    void CreateActionRow(RectTransform parent)
    {
        var row = CreateRow(parent, "ProbeActionRow");
        CreateButton(row, "ProbeLaunchLabel", () => ProbeSystemController.Instance?.Launch());
        CreateButton(row, "ProbeAbortLabel", () => ProbeSystemController.Instance?.Abort("ProbeAbortedManual", "Probe aborted."));
        _burnButton = CreateButton(row, "ProbeBurnLabel", () => ProbeSystemController.Instance?.Burn());
        _postcardButton = CreateButton(row, "ProbePostcardLabel", () => ProbeSystemController.Instance?.RequestPostcard());
    }

    void CreateCameraRow(RectTransform parent)
    {
        var row = CreateRow(parent, "ProbeCameraRow");
        CreateButton(row, "ProbeWorldLabel", () => ProbeSettings.SetCameraMode(ProbeCameraMode.World));
        CreateButton(row, "ProbeChaseLabel", () => ProbeSettings.SetCameraMode(ProbeCameraMode.Chase));
        CreateButton(row, "ProbeCockpitLabel", () => ProbeSettings.SetCameraMode(ProbeCameraMode.Cockpit));
        _viewsToggle = CreateLabeledToggle(row, "ProbeViewsLabel", ProbeSettings.ShowProbeViews, ProbeSettings.SetShowProbeViews);
    }

    void CreateModeButton(RectTransform parent, string key, ProbeModelKind kind)
    {
        CreateButton(parent, key, () => ProbeSettings.SetModel(kind));
    }

    Button CreateButton(RectTransform parent, string key, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(key, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.32f, 1f);
        var le = go.GetComponent<LayoutElement>();
        le.minHeight = RowHeight;
        le.preferredHeight = RowHeight;
        var label = CreateTmp(go.GetComponent<RectTransform>(), key + "_Text", ButtonFontSize, TextAlignmentOptions.Center);
        label.text = key;
        label.raycastTarget = false;

        var button = go.GetComponent<Button>();
        button.onClick.AddListener(action);
        ApplyLocalizedName(go, key);
        return button;
    }

    Toggle CreateLabeledToggle(RectTransform parent, string key, bool initial, System.Action<bool> setter)
    {
        var go = new GameObject(key + "_Row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().minHeight = RowHeight;
        go.GetComponent<LayoutElement>().preferredHeight = RowHeight;
        go.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.95f);
        var rowLayout = go.GetComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(8, 8, 6, 6);
        rowLayout.spacing = 8f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        var boxGo = new GameObject("Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        boxGo.layer = gameObject.layer;
        boxGo.transform.SetParent(go.transform, false);
        boxGo.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 1f);
        var boxLe = boxGo.GetComponent<LayoutElement>();
        boxLe.minWidth = CheckboxSize;
        boxLe.minHeight = CheckboxSize;
        boxLe.preferredWidth = CheckboxSize;
        boxLe.preferredHeight = CheckboxSize;
        boxLe.flexibleWidth = 0f;

        var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        checkGo.layer = gameObject.layer;
        checkGo.transform.SetParent(boxGo.transform, false);
        Stretch(checkGo.GetComponent<RectTransform>());
        checkGo.GetComponent<RectTransform>().offsetMin = new Vector2(4f, 4f);
        checkGo.GetComponent<RectTransform>().offsetMax = new Vector2(-4f, -4f);
        checkGo.GetComponent<Image>().color = new Color(0.35f, 0.85f, 1f, 1f);

        var label = CreateTmp(go.GetComponent<RectTransform>(), key, ButtonFontSize, TextAlignmentOptions.MidlineLeft);
        label.enableWordWrapping = false;
        var labelLe = label.gameObject.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1f;
        labelLe.minHeight = RowHeight;

        var toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = go.GetComponent<Image>();
        toggle.graphic = checkGo.GetComponent<Image>();
        toggle.isOn = initial;
        toggle.onValueChanged.AddListener(v => setter(v));
        ApplyLocalizedName(label.gameObject, key);
        return toggle;
    }

    void CreateSliderRow(RectTransform parent, string key, string fallback, out Slider slider, float min, float max, float value)
    {
        var row = CreateRow(parent, key + "_Row");
        CreateTmp(row, key, ButtonFontSize, TextAlignmentOptions.MidlineLeft);
        var sliderGo = new GameObject(key + "_Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        sliderGo.layer = gameObject.layer;
        sliderGo.transform.SetParent(row, false);
        sliderGo.GetComponent<LayoutElement>().minHeight = SliderHeight;
        sliderGo.GetComponent<LayoutElement>().preferredHeight = SliderHeight;
        sliderGo.GetComponent<LayoutElement>().flexibleWidth = 1f;
        slider = sliderGo.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(sliderGo.transform, false);
        Stretch(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.25f, 1f);
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Stretch(fill.GetComponent<RectTransform>());
        fill.GetComponent<Image>().color = new Color(0.35f, 0.75f, 1f, 1f);
        slider.targetGraphic = bg.GetComponent<Image>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        ApplyLocalizedName(row.Find(key)?.gameObject, key);
    }

    RectTransform CreateRow(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        var h = go.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 6f;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlHeight = true;
        h.childControlWidth = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = false;
        go.GetComponent<LayoutElement>().minHeight = RowHeight;
        go.GetComponent<LayoutElement>().preferredHeight = RowHeight;
        return go.GetComponent<RectTransform>();
    }

    RectTransform CreatePanel(string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(_root, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    RectTransform CreatePip(string name, string caption, out RawImage image)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(_root, false);
        go.GetComponent<Image>().color = new Color(0.18f, 0.24f, 0.34f, 0.95f);
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.55f, 0.82f, 1f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        var innerGo = new GameObject("Inner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        innerGo.layer = gameObject.layer;
        innerGo.transform.SetParent(go.transform, false);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(PipFrameInset, PipFrameInset);
        innerRt.offsetMax = new Vector2(-PipFrameInset, -PipFrameInset);
        innerGo.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 1f);

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        viewportGo.layer = gameObject.layer;
        viewportGo.transform.SetParent(innerGo.transform, false);
        Stretch(viewportGo.GetComponent<RectTransform>());
        image = viewportGo.GetComponent<RawImage>();
        image.color = Color.white;
        image.raycastTarget = false;

        var captionTmp = CreateTmp(go.GetComponent<RectTransform>(), name + "_Caption", ButtonFontSize, TextAlignmentOptions.TopLeft);
        captionTmp.enableWordWrapping = false;
        captionTmp.text = caption ?? string.Empty;
        captionTmp.fontSize = ButtonFontSize;
        var capRt = captionTmp.GetComponent<RectTransform>();
        capRt.anchorMin = new Vector2(0f, 1f);
        capRt.anchorMax = new Vector2(1f, 1f);
        capRt.pivot = new Vector2(0.5f, 1f);
        capRt.offsetMin = new Vector2(8f, -PipCaptionHeight);
        capRt.offsetMax = new Vector2(-8f, -4f);

        go.SetActive(false);
        return go.GetComponent<RectTransform>();
    }

    static TextMeshProUGUI CreateTmp(RectTransform parent, string name, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>());
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = new Color(0.92f, 0.95f, 1f, 1f);
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        tmp.text = name;
        var font = LocalizationFontHelper.GetFontForLanguage(LocalizationManager.CurrentLanguage);
        if (font != null)
            tmp.font = font;
        if (go.GetComponent<LocalizedText>() == null && !name.EndsWith("_Text") && !name.EndsWith("Body") && !name.EndsWith("_Caption"))
            go.AddComponent<LocalizedText>();
        return tmp;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void ApplyLocalizedName(GameObject go, string key)
    {
        if (go == null)
            return;
        // Button visual text: copy translation onto child if present.
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null)
            return;
        if (LocalizationManager.Instance != null)
        {
            string t = LocalizationManager.Instance.GetTranslation(key);
            if (!string.IsNullOrEmpty(t))
                tmp.text = t;
        }
    }

    void Layout()
    {
        bool landscape = Screen.width > Screen.height;
        Canvas canvas = GetComponentInParent<Canvas>();
        SafeAreaInsets.GetCanvasInsets(canvas, out float left, out float right, out float top, out float bottom);

        float scale = canvas != null && canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
        float canvasWidth = Screen.width / scale;
        float barWidth = landscape
            ? BarWidthLandscape
            : Mathf.Max(280f, canvasWidth - left - right - 24f);
        float barHeight = ProbeSettings.Model == ProbeModelKind.Custom ? BarHeightCustom : BarHeightStandard;
        float timeBar = TimeControlUiBootstrap.BarHeight + TimeControlUiBootstrap.BarBottomMargin + 10f;

        _bar.anchorMin = new Vector2(0.5f, 0f);
        _bar.anchorMax = new Vector2(0.5f, 0f);
        _bar.pivot = new Vector2(0.5f, 0f);
        _bar.sizeDelta = new Vector2(barWidth, barHeight);
        _bar.anchoredPosition = new Vector2(0f, bottom + timeBar);

        float pipW = landscape ? PipWidthLandscape : PipWidthPortrait;
        float pipH = landscape ? PipHeightLandscape : PipHeightPortrait;
        float navBottom = top + SidePanelUiBootstrap.BarHeight + 8f;
        float telemW = landscape ? TelemetryWidthLandscape : TelemetryWidth;

        _telemetry.anchorMin = new Vector2(1f, 1f);
        _telemetry.anchorMax = new Vector2(1f, 1f);
        _telemetry.pivot = new Vector2(1f, 1f);
        _telemetry.sizeDelta = new Vector2(telemW, TelemetryHeight);
        _telemetry.anchoredPosition = new Vector2(-(right + 10f), -(navBottom + pipH + 8f));

        _forwardPip.anchorMin = new Vector2(0.5f, 1f);
        _forwardPip.anchorMax = new Vector2(0.5f, 1f);
        _forwardPip.pivot = new Vector2(0.5f, 1f);
        _forwardPip.sizeDelta = new Vector2(pipW, pipH);
        _forwardPip.anchoredPosition = new Vector2(0f, -navBottom);

        _rearLeftPip.anchorMin = new Vector2(0f, 1f);
        _rearLeftPip.anchorMax = new Vector2(0f, 1f);
        _rearLeftPip.pivot = new Vector2(0f, 1f);
        _rearLeftPip.sizeDelta = new Vector2(pipW, pipH);
        _rearLeftPip.anchoredPosition = new Vector2(left + (landscape ? 96f : 8f), -navBottom);

        _rearRightPip.anchorMin = new Vector2(1f, 1f);
        _rearRightPip.anchorMax = new Vector2(1f, 1f);
        _rearRightPip.pivot = new Vector2(1f, 1f);
        _rearRightPip.sizeDelta = new Vector2(pipW, pipH);
        _rearRightPip.anchoredPosition = new Vector2(-(right + 8f), -navBottom);

        ApplyHudFontSizes();
    }

    void ApplyHudFontSizes()
    {
        ApplyFontSize(_bar, ButtonFontSize);
        ApplyFontSize(_telemetry, TelemetryFontSize);
    }

    static void ApplyFontSize(RectTransform root, float size)
    {
        if (root == null)
            return;

        var labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
            labels[i].fontSize = size;
    }

    void RefreshState()
    {
        bool flying = ProbeSystemController.Instance != null && ProbeSystemController.Instance.IsFlying;
        if (_customRow != null)
            _customRow.SetActive(ProbeSettings.Model == ProbeModelKind.Custom);
        if (_burnButton != null)
            _burnButton.interactable = flying && ProbeSettings.ResolveHasEngine();
        if (_postcardButton != null)
            _postcardButton.interactable = flying && ProbeSettings.ResolveHasAntenna();
        if (_viewsToggle != null && _viewsToggle.isOn != ProbeSettings.ShowProbeViews)
            _viewsToggle.SetIsOnWithoutNotify(ProbeSettings.ShowProbeViews);
        RefreshLabels();
        RefreshPips();
        RefreshTelemetryNow();
    }

    void RefreshLabels()
    {
        if (_bar == null)
            return;

        var labels = _bar.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            string key = labels[i].gameObject.name;
            if (key.EndsWith("_Text", System.StringComparison.Ordinal))
                key = key.Substring(0, key.Length - 5);
            if (key.EndsWith("Body", System.StringComparison.Ordinal))
                continue;
            ApplyLocalizedName(labels[i].gameObject, key);
        }
    }

    void RefreshPips()
    {
        bool show = ProbeSettings.ShowProbeViews
            && ProbeSystemController.Instance != null
            && ProbeSystemController.Instance.IsFlying;
        var rig = ProbeSystemController.Instance != null
            ? ProbeSystemController.Instance.GetComponent<ProbeViewRig>()
            : null;

        if (_forwardPip != null)
            _forwardPip.gameObject.SetActive(show);
        if (_rearLeftPip != null)
            _rearLeftPip.gameObject.SetActive(show);
        if (_rearRightPip != null)
            _rearRightPip.gameObject.SetActive(show);
        if (!show || rig == null)
            return;

        if (rig.ForwardTexture != null && _forwardImage != null)
            _forwardImage.texture = rig.ForwardTexture;
        if (rig.RearLeftTexture != null && _rearLeftImage != null)
            _rearLeftImage.texture = rig.RearLeftTexture;
        if (rig.RearRightTexture != null && _rearRightImage != null)
            _rearRightImage.texture = rig.RearRightTexture;
    }

    void RefreshTelemetryNow()
    {
        if (_telemetryText == null)
            return;

        var system = ProbeSystemController.Instance;
        bool show = system != null && ProbeSettings.UseProbe;
        _telemetry.gameObject.SetActive(show);
        if (!show)
            return;

        Vector3 pos;
        Vector3 vel;
        if (system.IsFlying && system.Craft != null)
        {
            pos = system.Craft.transform.position;
            vel = system.Craft.Velocity;
        }
        else
        {
            pos = system.Sun != null ? system.Sun.position + Vector3.right * 22f : Vector3.zero;
            vel = system.AimVelocity;
        }

        Transform sun = system.Sun;
        Vector3 sunPos = sun != null ? sun.position : Vector3.zero;
        Vector3 rel = pos - sunPos;
        float auToUnity = Mathf.Max(0.01f, system.AuToUnity);
        float rAu = new Vector2(rel.x, rel.z).magnitude / auToUnity;
        float zAu = rel.y / auToUnity;
        float lon = Mathf.Atan2(rel.x, rel.z) * Mathf.Rad2Deg;
        if (lon < 0f)
            lon += 360f;

        Vector3 sunVel = Vector3.zero;
        if (system.TryGetAttractor("Sun", out ProbeGravityIntegrator.Attractor sunA))
            sunVel = sunA.velocity;
        float vSun = (vel - sunVel).magnitude / auToUnity;

        string n1 = system.FindNearestName(pos, out float d1);
        string n2 = "—";
        string n3 = "—";
        float d2 = 0f;
        float d3 = 0f;
        FindTopThree(system, pos, out n1, out d1, out n2, out d2, out n3, out d3);

        float vRel = 0f;
        GameObject nearestGo = GameObject.Find(n1);
        if (nearestGo != null && system.TryGetAttractor(n1, out ProbeGravityIntegrator.Attractor nearA))
            vRel = (vel - nearA.velocity).magnitude / auToUnity;

        string vSunText = FormatSpeed(vSun, rAu);
        string vRelText = FormatSpeed(vRel, d1 / auToUnity);
        string rText = FormatDistance(rAu);
        string zText = FormatDistance(Mathf.Abs(zAu));

        _telemetryText.text =
            T("ProbeTelemetrySunSpeed", "v☉") + "  " + vSunText + "\n" +
            T("ProbeTelemetryRelSpeed", "v rel") + "  " + vRelText + "\n" +
            T("ProbeTelemetryRadius", "r") + "  " + rText + "\n" +
            T("ProbeTelemetryLon", "λ") + "  " + lon.ToString("0.0") + "°\n" +
            T("ProbeTelemetryZ", "z") + "  " + zText + "\n" +
            T("ProbeNearestLabel", "Near") + "\n" +
            "1. " + BodyLabel(n1) + "  " + FormatDistance(d1 / auToUnity) + "\n" +
            "2. " + BodyLabel(n2) + "  " + FormatDistance(d2 / auToUnity) + "\n" +
            "3. " + BodyLabel(n3) + "  " + FormatDistance(d3 / auToUnity);
    }

    static void FindTopThree(
        ProbeSystemController system,
        Vector3 pos,
        out string n1, out float d1,
        out string n2, out float d2,
        out string n3, out float d3)
    {
        n1 = n2 = n3 = "—";
        d1 = d2 = d3 = 0f;
        var scratch = new ProbeGravityIntegrator.Attractor[ProbeSystemController.MaxAttractors];
        int count = system.CopyAttractors(scratch);
        var dist = new float[count];
        var names = new string[count];
        for (int i = 0; i < count; i++)
        {
            names[i] = scratch[i].name;
            dist[i] = Mathf.Max(0f, Vector3.Distance(pos, scratch[i].position) - scratch[i].radius);
        }

        for (int pass = 0; pass < 3; pass++)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (dist[i] < bestD)
                {
                    bestD = dist[i];
                    best = i;
                }
            }

            if (best < 0)
                break;

            if (pass == 0) { n1 = names[best]; d1 = bestD; }
            if (pass == 1) { n2 = names[best]; d2 = bestD; }
            if (pass == 2) { n3 = names[best]; d3 = bestD; }
            dist[best] = float.MaxValue;
        }
    }

    static string FormatDistance(float au)
    {
        if (au < 0.001f)
        {
            float km = au * SolarSystemCatalog.AuKm;
            return km.ToString("0") + " km";
        }

        if (au < 0.1f)
            return au.ToString("0.000") + " AU";
        return au.ToString("0.00") + " AU";
    }

    static string FormatSpeed(float auPerSec, float nearbyAu)
    {
        if (nearbyAu < 0.002f)
        {
            float kmPerSec = auPerSec * SolarSystemCatalog.AuKm;
            return kmPerSec.ToString("0.0") + " km/s";
        }

        return auPerSec.ToString("0.000") + " AU/s";
    }

    static string BodyLabel(string objectName)
    {
        if (string.IsNullOrEmpty(objectName) || objectName == "—")
            return "—";

        string key = objectName + "Header";
        if (objectName.StartsWith("Comet_"))
        {
            if (CometCatalog.TryGetByObjectName(objectName, out CometCatalog.CometDefinition def))
                key = def.labelKey;
        }

        if (LocalizationManager.Instance != null)
        {
            string t = LocalizationManager.Instance.GetTranslation(key);
            if (!string.IsNullOrEmpty(t))
                return t;
        }

        return objectName;
    }

    static string T(string key, string fallback)
    {
        if (LocalizationManager.Instance == null)
            return fallback;
        string t = LocalizationManager.Instance.GetTranslation(key);
        return string.IsNullOrEmpty(t) ? fallback : t;
    }
}
