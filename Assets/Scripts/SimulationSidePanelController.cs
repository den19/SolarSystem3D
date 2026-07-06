using System.Collections;
using SolarSystemApp;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sliding side panel with simulation view toggles. UI lives on MainScreenCanvas in the scene.
/// </summary>
public class SimulationSidePanelController : MonoBehaviour
{
    const float PanelWidth = SidePanelUiBootstrap.PanelWidth;
    const float SlideDuration = 0.22f;
    const float MenuButtonGap = 12f;
    const float PanelBelowMenuGap = 8f;
    static readonly Vector2 MenuButtonFallbackPosition = new Vector2(-8f, -63f);

    [SerializeField] Button menuButton;
    [SerializeField] RectTransform panelRect;
    [SerializeField] Toggle orbitsToggle;
    [SerializeField] Toggle gravityGridToggle;
    [SerializeField] Toggle labelsToggle;
    [SerializeField] Toggle minimapToggle;
    [SerializeField] Toggle uiToggle;
    [SerializeField] Toggle realDistancesToggle;
    [SerializeField] Toggle realSizesToggle;
    [SerializeField] Toggle realOrbitsToggle;
    [SerializeField] Toggle cometMovementToggle;
    [SerializeField] Toggle freeObservationToggle;

    bool isInitializing;
    bool isPanelOpen;
    Coroutine slideRoutine;
    float panelClosedX;
    float panelOpenX;

    void Awake()
    {
        SidePanelUiBootstrap.ApplyCompactLayout(transform);
        ResolveReferences();
        LayoutMenuButton();
        LayoutPanelBelowMenuButton();

        panelClosedX = PanelWidth + 12f;
        panelOpenX = -12f;

        if (panelRect != null)
        {
            float panelY = panelRect.anchoredPosition.y;
            panelRect.anchoredPosition = new Vector2(panelClosedX, panelY);
        }

        if (menuButton != null)
            menuButton.onClick.AddListener(TogglePanel);
    }

    void ResolveReferences()
    {
        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        if (menuButton == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Transform buttonTransform = canvas.transform.Find("SidePanelMenuButton");
                if (buttonTransform != null)
                    menuButton = buttonTransform.GetComponent<Button>();
            }
        }

        if (orbitsToggle == null)
            orbitsToggle = FindToggle("SidePanelOrbitsLabel_Row");
        if (gravityGridToggle == null)
            gravityGridToggle = FindToggle("SidePanelGravityGridLabel_Row");
        if (labelsToggle == null)
            labelsToggle = FindToggle("SidePanelLabelsLabel_Row");
        if (minimapToggle == null)
            minimapToggle = FindToggle("SidePanelMinimapLabel_Row");
        if (uiToggle == null)
            uiToggle = FindToggle("SidePanelUiLabel_Row");
        if (realDistancesToggle == null)
            realDistancesToggle = FindToggle("SidePanelRealDistancesLabel_Row");
        if (realSizesToggle == null)
            realSizesToggle = FindToggle("SidePanelRealSizesLabel_Row");
        if (realOrbitsToggle == null)
            realOrbitsToggle = FindToggle("SidePanelRealOrbitsLabel_Row");
        if (cometMovementToggle == null)
            cometMovementToggle = FindToggle("SidePanelCometMovementLabel_Row");
        if (freeObservationToggle == null)
            freeObservationToggle = FindToggle("SidePanelFreeObservationLabel_Row");
    }

    Toggle FindToggle(string rowName)
    {
        return SidePanelUiBootstrap.FindToggle(transform, rowName);
    }

    void LayoutMenuButton()
    {
        if (menuButton == null)
            return;

        var menuButtonRect = menuButton.GetComponent<RectTransform>();
        if (menuButtonRect == null)
            return;

        menuButtonRect.anchorMin = new Vector2(1f, 1f);
        menuButtonRect.anchorMax = new Vector2(1f, 1f);
        menuButtonRect.pivot = new Vector2(1f, 1f);

        Transform canvasTransform = transform.parent;
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

        float y = simControlRect.anchoredPosition.y - simControlRect.rect.height - MenuButtonGap;
        menuButtonRect.anchoredPosition = new Vector2(simControlRect.anchoredPosition.x, y);
    }

    void LayoutPanelBelowMenuButton()
    {
        if (panelRect == null || menuButton == null)
            return;

        var menuButtonRect = menuButton.GetComponent<RectTransform>();
        if (menuButtonRect == null)
            return;

        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);

        float y = menuButtonRect.anchoredPosition.y - menuButtonRect.rect.height - PanelBelowMenuGap;
        panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, y);
    }

    IEnumerator Start()
    {
        isInitializing = true;

        if (orbitsToggle != null)
        {
            orbitsToggle.SetIsOnWithoutNotify(SimulationViewSettings.ShowOrbitLines);
            orbitsToggle.onValueChanged.RemoveAllListeners();
            orbitsToggle.onValueChanged.AddListener(OnOrbitsToggleChanged);
        }

        if (gravityGridToggle != null)
        {
            gravityGridToggle.SetIsOnWithoutNotify(GravityGridSettings.UseGravityGrid);
            gravityGridToggle.onValueChanged.RemoveAllListeners();
            gravityGridToggle.onValueChanged.AddListener(OnGravityGridToggleChanged);
        }

        if (labelsToggle != null)
        {
            labelsToggle.SetIsOnWithoutNotify(SimulationViewSettings.ShowBodyLabels);
            labelsToggle.onValueChanged.RemoveAllListeners();
            labelsToggle.onValueChanged.AddListener(OnLabelsToggleChanged);
        }

        if (minimapToggle != null)
        {
            minimapToggle.SetIsOnWithoutNotify(SimulationViewSettings.ShowMinimap);
            minimapToggle.onValueChanged.RemoveAllListeners();
            minimapToggle.onValueChanged.AddListener(OnMinimapToggleChanged);
        }

        if (uiToggle != null)
        {
            uiToggle.SetIsOnWithoutNotify(SimulationViewSettings.ShowSimulationUi);
            uiToggle.onValueChanged.RemoveAllListeners();
            uiToggle.onValueChanged.AddListener(OnUiToggleChanged);
        }

        if (realDistancesToggle != null)
        {
            realDistancesToggle.SetIsOnWithoutNotify(ScaleSettings.UseRealDistances);
            realDistancesToggle.onValueChanged.RemoveAllListeners();
            realDistancesToggle.onValueChanged.AddListener(OnRealDistancesToggleChanged);
        }

        if (realSizesToggle != null)
        {
            realSizesToggle.SetIsOnWithoutNotify(ScaleSettings.UseRealSizes);
            realSizesToggle.onValueChanged.RemoveAllListeners();
            realSizesToggle.onValueChanged.AddListener(OnRealSizesToggleChanged);
        }

        if (realOrbitsToggle != null)
        {
            realOrbitsToggle.SetIsOnWithoutNotify(OrbitSettings.UseRealOrbits);
            realOrbitsToggle.onValueChanged.RemoveAllListeners();
            realOrbitsToggle.onValueChanged.AddListener(OnRealOrbitsToggleChanged);
        }

        if (cometMovementToggle != null)
        {
            cometMovementToggle.SetIsOnWithoutNotify(CometMovementSettings.UseCometMovement);
            cometMovementToggle.onValueChanged.RemoveAllListeners();
            cometMovementToggle.onValueChanged.AddListener(OnCometMovementToggleChanged);
        }

        if (freeObservationToggle != null)
        {
            freeObservationToggle.SetIsOnWithoutNotify(SimulationViewSettings.UseFreeObservation);
            freeObservationToggle.onValueChanged.RemoveAllListeners();
            freeObservationToggle.onValueChanged.AddListener(OnFreeObservationToggleChanged);
        }

        SimulationViewSettings.ShowOrbitLinesChanged += OnOrbitsSettingChanged;
        SimulationViewSettings.ShowBodyLabelsChanged += OnLabelsSettingChanged;
        SimulationViewSettings.ShowMinimapChanged += OnMinimapSettingChanged;
        SimulationViewSettings.ShowSimulationUiChanged += OnUiSettingChanged;
        SimulationViewSettings.UseFreeObservationChanged += OnFreeObservationSettingChanged;
        GravityGridSettings.UseGravityGridChanged += OnGravityGridSettingChanged;
        ScaleSettings.UseRealDistancesChanged += OnRealDistancesSettingChanged;
        ScaleSettings.UseRealSizesChanged += OnRealSizesSettingChanged;
        OrbitSettings.UseRealOrbitsChanged += OnRealOrbitsSettingChanged;
        CometMovementSettings.UseCometMovementChanged += OnCometMovementSettingChanged;

        yield return new WaitForEndOfFrame();
        isInitializing = false;
    }

    void OnDestroy()
    {
        SimulationViewSettings.ShowOrbitLinesChanged -= OnOrbitsSettingChanged;
        SimulationViewSettings.ShowBodyLabelsChanged -= OnLabelsSettingChanged;
        SimulationViewSettings.ShowMinimapChanged -= OnMinimapSettingChanged;
        SimulationViewSettings.ShowSimulationUiChanged -= OnUiSettingChanged;
        SimulationViewSettings.UseFreeObservationChanged -= OnFreeObservationSettingChanged;
        GravityGridSettings.UseGravityGridChanged -= OnGravityGridSettingChanged;
        ScaleSettings.UseRealDistancesChanged -= OnRealDistancesSettingChanged;
        ScaleSettings.UseRealSizesChanged -= OnRealSizesSettingChanged;
        OrbitSettings.UseRealOrbitsChanged -= OnRealOrbitsSettingChanged;
        CometMovementSettings.UseCometMovementChanged -= OnCometMovementSettingChanged;

        if (menuButton != null)
            menuButton.onClick.RemoveListener(TogglePanel);
    }

    void OnOrbitsToggleChanged(bool isOn)
    {
        if (isInitializing) return;
        SimulationViewSettings.SetShowOrbitLines(isOn);
    }

    void OnGravityGridToggleChanged(bool isOn)
    {
        if (isInitializing) return;
        GravityGridSettings.SetUseGravityGrid(isOn);
    }

    void OnLabelsToggleChanged(bool isOn)
    {
        if (isInitializing) return;
        SimulationViewSettings.SetShowBodyLabels(isOn);
    }

    void OnMinimapToggleChanged(bool isOn)
    {
        if (isInitializing) return;
        SimulationViewSettings.SetShowMinimap(isOn);
    }

    void OnUiToggleChanged(bool isOn)
    {
        if (isInitializing) return;
        SimulationViewSettings.SetShowSimulationUi(isOn);
    }

    void OnRealDistancesToggleChanged(bool isOn)
    {
        if (isInitializing) return;
        ScaleSettings.SetUseRealDistances(isOn);
    }

    void OnRealSizesToggleChanged(bool isOn)
    {
        if (isInitializing) return;
        ScaleSettings.SetUseRealSizes(isOn);
    }

    void OnRealOrbitsToggleChanged(bool isOn)
    {
        if (isInitializing) return;
        OrbitSettings.SetUseRealOrbits(isOn);
    }

    void OnCometMovementToggleChanged(bool isOn)
    {
        if (isInitializing) return;
        CometMovementSettings.SetUseCometMovement(isOn);
    }

    void OnFreeObservationToggleChanged(bool isOn)
    {
        if (isInitializing) return;
        SimulationViewSettings.SetUseFreeObservation(isOn);
    }

    void OnOrbitsSettingChanged(bool isOn)
    {
        if (orbitsToggle != null)
            orbitsToggle.SetIsOnWithoutNotify(isOn);
    }

    void OnGravityGridSettingChanged(bool isOn)
    {
        if (gravityGridToggle != null)
            gravityGridToggle.SetIsOnWithoutNotify(isOn);
    }

    void OnLabelsSettingChanged(bool isOn)
    {
        if (labelsToggle != null)
            labelsToggle.SetIsOnWithoutNotify(isOn);
    }

    void OnMinimapSettingChanged(bool isOn)
    {
        if (minimapToggle != null)
            minimapToggle.SetIsOnWithoutNotify(isOn);
    }

    void OnUiSettingChanged(bool isOn)
    {
        if (uiToggle != null)
            uiToggle.SetIsOnWithoutNotify(isOn);
    }

    void OnRealDistancesSettingChanged(bool isOn)
    {
        if (realDistancesToggle != null)
            realDistancesToggle.SetIsOnWithoutNotify(isOn);
    }

    void OnRealSizesSettingChanged(bool isOn)
    {
        if (realSizesToggle != null)
            realSizesToggle.SetIsOnWithoutNotify(isOn);
    }

    void OnRealOrbitsSettingChanged(bool isOn)
    {
        if (realOrbitsToggle != null)
            realOrbitsToggle.SetIsOnWithoutNotify(isOn);
    }

    void OnCometMovementSettingChanged(bool isOn)
    {
        if (cometMovementToggle != null)
            cometMovementToggle.SetIsOnWithoutNotify(isOn);
    }

    void OnFreeObservationSettingChanged(bool isOn)
    {
        if (freeObservationToggle != null)
            freeObservationToggle.SetIsOnWithoutNotify(isOn);
    }

    public void TogglePanel()
    {
        SetPanelOpen(!isPanelOpen);
    }

    public void ClosePanelImmediate()
    {
        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
            slideRoutine = null;
        }

        isPanelOpen = false;
        if (panelRect != null)
            panelRect.anchoredPosition = new Vector2(panelClosedX, panelRect.anchoredPosition.y);
    }

    void SetPanelOpen(bool open)
    {
        isPanelOpen = open;
        if (slideRoutine != null)
            StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlidePanel(open ? panelOpenX : panelClosedX));
    }

    IEnumerator SlidePanel(float targetX)
    {
        Vector2 start = panelRect.anchoredPosition;
        Vector2 end = new Vector2(targetX, start.y);
        float elapsed = 0f;

        while (elapsed < SlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / SlideDuration);
            t = t * t * (3f - 2f * t);
            panelRect.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        panelRect.anchoredPosition = end;
        slideRoutine = null;
    }
}
