using System.Collections;
using SolarSystemApp;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sliding side panel with simulation view toggles. UI lives on MainScreenCanvas in the scene.
/// </summary>
public class SimulationSidePanelController : MonoBehaviour
{
    const float PanelWidth = 220f;
    const float SlideDuration = 0.22f;

    [SerializeField] Button menuButton;
    [SerializeField] RectTransform panelRect;
    [SerializeField] Toggle orbitsToggle;
    [SerializeField] Toggle gravityGridToggle;
    [SerializeField] Toggle labelsToggle;
    [SerializeField] Toggle minimapToggle;
    [SerializeField] Toggle uiToggle;

    bool isInitializing;
    bool isPanelOpen;
    Coroutine slideRoutine;
    float panelClosedX;
    float panelOpenX;

    void Awake()
    {
        ResolveReferences();

        panelClosedX = PanelWidth + 12f;
        panelOpenX = -12f;

        if (panelRect != null)
            panelRect.anchoredPosition = new Vector2(panelClosedX, 0f);

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
    }

    Toggle FindToggle(string rowName)
    {
        Transform row = transform.Find(rowName);
        if (row == null)
            return null;

        Transform toggleTransform = row.Find("Toggle");
        return toggleTransform != null ? toggleTransform.GetComponent<Toggle>() : null;
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

        SimulationViewSettings.ShowOrbitLinesChanged += OnOrbitsSettingChanged;
        SimulationViewSettings.ShowBodyLabelsChanged += OnLabelsSettingChanged;
        SimulationViewSettings.ShowMinimapChanged += OnMinimapSettingChanged;
        SimulationViewSettings.ShowSimulationUiChanged += OnUiSettingChanged;
        GravityGridSettings.UseGravityGridChanged += OnGravityGridSettingChanged;

        yield return new WaitForEndOfFrame();
        isInitializing = false;
    }

    void OnDestroy()
    {
        SimulationViewSettings.ShowOrbitLinesChanged -= OnOrbitsSettingChanged;
        SimulationViewSettings.ShowBodyLabelsChanged -= OnLabelsSettingChanged;
        SimulationViewSettings.ShowMinimapChanged -= OnMinimapSettingChanged;
        SimulationViewSettings.ShowSimulationUiChanged -= OnUiSettingChanged;
        GravityGridSettings.UseGravityGridChanged -= OnGravityGridSettingChanged;

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
            panelRect.anchoredPosition = new Vector2(panelClosedX, 0f);
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
        Vector2 end = new Vector2(targetX, 0f);
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
