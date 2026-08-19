using SolarSystemApp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Isolated orthographic preview of the selected probe model for the HUD.
/// </summary>
public class ProbePreviewRig : MonoBehaviour
{
    public const int PreviewLayer = 31;

    const float YawSpeedDeg = 15f;
    const float PreviewWorldY = -500f;
    const float PreviewPanelPortrait = 280f;
    const float PreviewPanelLandscape = 240f;
    const float LabelHeight = 36f;

    Camera _camera;
    RenderTexture _renderTexture;
    ProbeCraft _previewCraft;
    RectTransform _panel;
    RawImage _image;
    TextMeshProUGUI _label;
    ProbeModelKind _builtModel = (ProbeModelKind)(-1);
    bool _customAntenna;
    bool _customEngine;
    bool _customShield;

    public static ProbePreviewRig EnsureOnHud(Transform hudRoot)
    {
        if (hudRoot == null)
            return null;

        Transform existing = hudRoot.Find("ProbePreview");
        if (existing != null)
        {
            var rig = existing.GetComponent<ProbePreviewRig>();
            if (rig == null)
                rig = existing.gameObject.AddComponent<ProbePreviewRig>();
            return rig;
        }

        var go = new GameObject("ProbePreview", typeof(RectTransform), typeof(ProbePreviewRig));
        go.layer = hudRoot.gameObject.layer;
        go.transform.SetParent(hudRoot, false);
        var preview = go.GetComponent<ProbePreviewRig>();
        preview.BuildUi();
        preview.EnsureCamera();
        return preview;
    }

    void OnEnable()
    {
        ProbeSettings.LoadoutChanged += RebuildPreview;
        ProbeSettings.UseProbeChanged += RefreshVisibility;
        if (ProbeSystemController.Instance != null)
            ProbeSystemController.Instance.StateChanged += OnStateChanged;
        LocalizationManager.OnLanguageChanged += RefreshLabel;
        EnsureCamera();
        RebuildPreview();
        RefreshVisibility(ProbeSettings.UseProbe);
    }

    void OnDisable()
    {
        ProbeSettings.LoadoutChanged -= RebuildPreview;
        ProbeSettings.UseProbeChanged -= RefreshVisibility;
        if (ProbeSystemController.Instance != null)
            ProbeSystemController.Instance.StateChanged -= OnStateChanged;
        LocalizationManager.OnLanguageChanged -= RefreshLabel;
    }

    void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
    }

    void OnStateChanged() => RefreshVisibility(ProbeSettings.UseProbe);

    void Update()
    {
        LayoutPanel();
        if (_previewCraft != null)
            _previewCraft.transform.Rotate(Vector3.up, YawSpeedDeg * Time.unscaledDeltaTime, Space.World);

        if (_camera != null && _camera.enabled)
            _camera.Render();
    }

    void BuildUi()
    {
        _panel = GetComponent<RectTransform>();
        _panel.anchorMin = new Vector2(0f, 1f);
        _panel.anchorMax = new Vector2(0f, 1f);
        _panel.pivot = new Vector2(0f, 1f);

        var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frameGo.layer = gameObject.layer;
        frameGo.transform.SetParent(_panel, false);
        var frameRt = frameGo.GetComponent<RectTransform>();
        Stretch(frameRt);
        frameGo.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.24f, 0.94f);
        var outline = frameGo.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.85f, 1f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        viewportGo.layer = gameObject.layer;
        viewportGo.transform.SetParent(frameGo.transform, false);
        var viewportRt = viewportGo.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = new Vector2(4f, LabelHeight + 4f);
        viewportRt.offsetMax = new Vector2(-4f, -4f);
        _image = viewportGo.GetComponent<RawImage>();
        _image.raycastTarget = false;

        _label = CreateLabel(frameGo.transform);
        _panel.gameObject.SetActive(false);
    }

    static TextMeshProUGUI CreateLabel(Transform parent)
    {
        var go = new GameObject("ProbePreviewLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(6f, 4f);
        rt.offsetMax = new Vector2(-6f, LabelHeight);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.Midline;
        tmp.color = new Color(0.88f, 0.94f, 1f, 1f);
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        var font = LocalizationFontHelper.GetFontForLanguage(LocalizationManager.CurrentLanguage);
        if (font != null)
            tmp.font = font;
        return tmp;
    }

    void EnsureCamera()
    {
        if (_camera != null)
            return;

        var camGo = new GameObject("ProbePreviewCamera");
        camGo.hideFlags = HideFlags.HideAndDontSave;
        _camera = camGo.AddComponent<Camera>();
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0.03f, 0.05f, 0.1f, 1f);
        _camera.orthographic = true;
        _camera.orthographicSize = 1.35f;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 20f;
        _camera.cullingMask = 1 << PreviewLayer;
        _camera.enabled = false;

        int size = GraphicsTierSettings.IsHighEffective ? 320 : 256;
        _renderTexture = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32);
        _renderTexture.name = "ProbePreviewRT";
        _camera.targetTexture = _renderTexture;
        if (_image != null)
            _image.texture = _renderTexture;
    }

    void RebuildPreview()
    {
        ProbeModelKind model = ProbeSettings.Model;
        bool antenna = ProbeSettings.CustomAntenna;
        bool engine = ProbeSettings.CustomEngine;
        bool shield = ProbeSettings.CustomShield;

        if (_previewCraft != null
            && _builtModel == model
            && _customAntenna == antenna
            && _customEngine == engine
            && _customShield == shield)
        {
            RefreshLabel();
            return;
        }

        if (_previewCraft != null)
            Destroy(_previewCraft.gameObject);

        _builtModel = model;
        _customAntenna = antenna;
        _customEngine = engine;
        _customShield = shield;

        _previewCraft = ProbePrefabFactory.Create(model, highDetail: false);
        _previewCraft.transform.position = new Vector3(0f, PreviewWorldY, 0f);
        _previewCraft.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
        SetLayerRecursively(_previewCraft.gameObject, PreviewLayer);
        RefreshLabel();
    }

    void RefreshLabel()
    {
        if (_label == null)
            return;

        string modelName = ProbeHudController.ResolveModelLabel(ProbeSettings.Model);
        if (ProbeSettings.Model != ProbeModelKind.Custom)
        {
            _label.text = modelName;
            return;
        }

        string a = ProbeSettings.CustomAntenna ? "A" : "—";
        string e = ProbeSettings.CustomEngine ? "E" : "—";
        string s = ProbeSettings.CustomShield ? "S" : "—";
        _label.text = modelName + "  " + a + " · " + e + " · " + s;
    }

    void RefreshVisibility(bool useProbe)
    {
        bool show = ShouldShowPreview(useProbe);
        if (_panel != null)
            _panel.gameObject.SetActive(show);
        if (_camera != null)
            _camera.enabled = show;
    }

    bool ShouldShowPreview(bool useProbe)
    {
        if (!useProbe)
            return false;

        var system = ProbeSystemController.Instance;
        if (system == null)
            return true;

        if (!system.IsFlying)
            return true;

        return system.FlightPreviewGraceRemaining > 0f;
    }

    void LayoutPanel()
    {
        if (_panel == null || !_panel.gameObject.activeSelf)
            return;

        bool landscape = Screen.width > Screen.height;
        Canvas canvas = GetComponentInParent<Canvas>();
        SafeAreaInsets.GetCanvasInsets(canvas, out float left, out _, out float top, out _);

        float size = landscape ? PreviewPanelLandscape : PreviewPanelPortrait;
        _panel.sizeDelta = new Vector2(size, size + LabelHeight);
        float navBottom = top + SidePanelUiBootstrap.BarHeight + 8f;
        _panel.anchoredPosition = new Vector2(left + 8f, -(navBottom + 8f));
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
