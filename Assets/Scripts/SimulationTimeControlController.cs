using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene-wired UI for simulation pause and speed controls on MainScreenCanvas.
/// </summary>
public class SimulationTimeControlController : MonoBehaviour
{
    [SerializeField] RectTransform barRect;
    [SerializeField] Button pausePlayButton;
    [SerializeField] Button speedDownButton;
    [SerializeField] Button speedUpButton;
    [SerializeField] TMP_Text speedLabel;

    Canvas _canvas;
    Rect _lastSafeArea;
    int _lastScreenWidth;
    int _lastScreenHeight;
    bool _layoutCached;
    TMP_Text _pausePlayLabel;

    static bool IsLandscape => Screen.width > Screen.height;

    void Awake()
    {
        if (barRect == null)
            barRect = GetComponent<RectTransform>();

        _canvas = GetComponentInParent<Canvas>();
        ResolveReferences();
        WireButtons();
    }

    void OnEnable()
    {
        SimulationTimeController.SpeedMultiplierChanged += OnSpeedMultiplierChanged;
        SimulationTimeController.IsPausedChanged += OnIsPausedChanged;
        LocalizationManager.OnLanguageChanged += RefreshSpeedLabel;

        RefreshSpeedLabel();
        RefreshPauseButtonVisual();
        RefreshSafeAreaLayout();
    }

    void OnDisable()
    {
        SimulationTimeController.SpeedMultiplierChanged -= OnSpeedMultiplierChanged;
        SimulationTimeController.IsPausedChanged -= OnIsPausedChanged;
        LocalizationManager.OnLanguageChanged -= RefreshSpeedLabel;
    }

    void Start()
    {
        RefreshSpeedLabel();
        RefreshPauseButtonVisual();
        RefreshSafeAreaLayout();
    }

    void Update()
    {
        if (!_layoutCached)
            return;

        Rect safeArea = Screen.safeArea;
        if (safeArea == _lastSafeArea
            && Screen.width == _lastScreenWidth
            && Screen.height == _lastScreenHeight)
        {
            return;
        }

        RefreshSafeAreaLayout();
    }

    public void Configure(RectTransform rect, Button pauseButton, Button downButton, Button upButton, TMP_Text label)
    {
        barRect = rect;
        pausePlayButton = pauseButton;
        speedDownButton = downButton;
        speedUpButton = upButton;
        speedLabel = label;
        ResolveReferences();
        WireButtons();
        RefreshSafeAreaLayout();
    }

    void ResolveReferences()
    {
        if (pausePlayButton == null)
            pausePlayButton = transform.Find("PausePlayButton")?.GetComponent<Button>();

        if (speedDownButton == null)
            speedDownButton = transform.Find("SpeedDownButton")?.GetComponent<Button>();

        if (speedUpButton == null)
            speedUpButton = transform.Find("SpeedUpButton")?.GetComponent<Button>();

        if (speedLabel == null)
            speedLabel = transform.Find("TimeControlSpeedFormat")?.GetComponent<TMP_Text>();

        _pausePlayLabel = pausePlayButton != null
            ? pausePlayButton.GetComponentInChildren<TMP_Text>(true)
            : null;
    }

    void WireButtons()
    {
        if (pausePlayButton != null)
        {
            pausePlayButton.onClick.RemoveListener(OnPausePlayClicked);
            pausePlayButton.onClick.AddListener(OnPausePlayClicked);
        }

        if (speedDownButton != null)
        {
            speedDownButton.onClick.RemoveListener(OnSpeedDownClicked);
            speedDownButton.onClick.AddListener(OnSpeedDownClicked);
        }

        if (speedUpButton != null)
        {
            speedUpButton.onClick.RemoveListener(OnSpeedUpClicked);
            speedUpButton.onClick.AddListener(OnSpeedUpClicked);
        }
    }

    void OnPausePlayClicked()
    {
        SimulationTimeController.TogglePause();
    }

    void OnSpeedDownClicked()
    {
        SimulationTimeController.StepSpeedDown();
    }

    void OnSpeedUpClicked()
    {
        SimulationTimeController.StepSpeedUp();
    }

    void OnSpeedMultiplierChanged(float _)
    {
        RefreshSpeedLabel();
    }

    void OnIsPausedChanged(bool _)
    {
        RefreshPauseButtonVisual();
    }

    void RefreshSpeedLabel()
    {
        if (speedLabel == null)
            return;

        string format = GetSpeedFormat();
        string speedText = SimulationTimeController.FormatSpeedLabel(SimulationTimeController.SpeedMultiplier);
        speedLabel.text = string.Format(format, speedText);
    }

    void RefreshPauseButtonVisual()
    {
        if (_pausePlayLabel == null)
            return;

        bool paused = SimulationTimeController.IsPaused;
        _pausePlayLabel.text = paused ? ">" : "II";
    }

    string GetSpeedFormat()
    {
        if (LocalizationManager.Instance != null)
        {
            string translation = LocalizationManager.Instance.GetTranslation("TimeControlSpeedFormat");
            if (!string.IsNullOrEmpty(translation))
                return translation;
        }

        return "{0}x";
    }

    public void RefreshSafeAreaLayout()
    {
        if (barRect == null)
            barRect = GetComponent<RectTransform>();

        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        SafeAreaInsets.GetCanvasInsets(_canvas, out float safeLeft, out float safeRight, out _, out float safeBottom);
        TimeControlUiBootstrap.ApplyBarRectLayout(barRect, _canvas, IsLandscape, safeLeft, safeRight, safeBottom);

        _lastSafeArea = Screen.safeArea;
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
        _layoutCached = true;
    }
}
