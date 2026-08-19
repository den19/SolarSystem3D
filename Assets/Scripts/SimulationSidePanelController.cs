using System.Collections;
using System.Collections.Generic;
using SolarSystemApp;
using SolarScaleMode = SolarSystemApp.ScaleMode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Sliding side panel with simulation view toggles. UI lives on MainScreenCanvas in the scene.
/// </summary>
public class SimulationSidePanelController : MonoBehaviour
{
    const float PortraitScale = 2f;
    const float LandscapeScale = 1.5f;
    const float MinPanelScale = 0.75f;
    const float BottomFitPadding = 8f;
    const float SlideDuration = 0.22f;
    const float EdgeMargin = 12f;
    const float PanelBelowMenuGap = 8f;

    [SerializeField] Button menuButton;
    [SerializeField] RectTransform panelRect;
    [SerializeField] Toggle orbitsToggle;
    [SerializeField] Toggle gravityGridToggle;
    [SerializeField] Toggle projectionToggle;
    [SerializeField] Toggle labelsToggle;
    [SerializeField] Toggle minimapToggle;
    [SerializeField] Toggle uiToggle;
    [SerializeField] Toggle educationalToggle;
    [SerializeField] Toggle realSizesToggle;
    [SerializeField] Toggle realOrbitsToggle;
    [SerializeField] Toggle cometMovementToggle;
    [SerializeField] Toggle freeObservationToggle;
    [SerializeField] Toggle realSunToggle;
    [SerializeField] Toggle timeMachineToggle;
    [SerializeField] Toggle probeToggle;

    bool isInitializing;
    bool isPanelOpen;
    Coroutine slideRoutine;
    float panelClosedX;
    float panelOpenX;
    Canvas canvas;
    Rect lastSafeArea;
    bool lastIsLandscape;
    bool safeAreaCached;
    float lastAppliedScale = -1f;

    static readonly List<RaycastResult> s_raycastResults = new List<RaycastResult>(8);

    static bool IsLandscape => Screen.width > Screen.height;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        ResolveReferences();
        isPanelOpen = false;
        RefreshSafeAreaLayout();

        if (menuButton != null)
            menuButton.onClick.AddListener(TogglePanel);
    }

    void Update()
    {
        if (!safeAreaCached)
            return;

        TryCloseOnOutsideClick();

        bool landscape = IsLandscape;
        Rect safeArea = Screen.safeArea;
        if (safeArea == lastSafeArea && landscape == lastIsLandscape)
            return;

        RefreshSafeAreaLayout();
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
                Transform buttonTransform = FindUiTransform(canvas.transform, "SidePanelMenuButton");
                if (buttonTransform != null)
                    menuButton = buttonTransform.GetComponent<Button>();
            }
        }

        if (orbitsToggle == null)
            orbitsToggle = FindToggle("SidePanelOrbitsLabel_Row");
        if (gravityGridToggle == null)
            gravityGridToggle = FindToggle("SidePanelGravityGridLabel_Row");
        if (projectionToggle == null)
            projectionToggle = FindToggle("SidePanelProjectionLabel_Row");
        if (labelsToggle == null)
            labelsToggle = FindToggle("SidePanelLabelsLabel_Row");
        if (minimapToggle == null)
            minimapToggle = FindToggle("SidePanelMinimapLabel_Row");
        if (uiToggle == null)
            uiToggle = FindToggle("SidePanelUiLabel_Row");
        if (educationalToggle == null)
            educationalToggle = FindToggle("SidePanelScaleEducationalLabel_Row");
        if (realSizesToggle == null)
            realSizesToggle = FindToggle("SidePanelRealSizesLabel_Row");
        if (realOrbitsToggle == null)
            realOrbitsToggle = FindToggle("SidePanelRealOrbitsLabel_Row");
        if (cometMovementToggle == null)
            cometMovementToggle = FindToggle("SidePanelCometMovementLabel_Row");
        if (freeObservationToggle == null)
            freeObservationToggle = FindToggle("SidePanelFreeObservationLabel_Row");
        if (realSunToggle == null)
            realSunToggle = FindToggle("SidePanelRealSunLabel_Row");
        if (timeMachineToggle == null)
            timeMachineToggle = FindToggle("SidePanelTimeMachineLabel_Row");
        if (probeToggle == null)
            probeToggle = FindToggle("SidePanelProbeLabel_Row");
    }

    Toggle FindToggle(string rowName)
    {
        return SidePanelUiBootstrap.FindToggle(transform, rowName);
    }

    float GetLandscapeRightInset()
    {
        if (!IsLandscape || canvas == null)
            return 0f;

        SafeAreaInsets.GetCanvasInsets(canvas, out _, out float right, out _, out _);
        return right;
    }

    float GetRightMargin() => EdgeMargin + GetLandscapeRightInset();

    public void RefreshSafeAreaLayout()
    {
        LayoutPanelBelowMenuButton();
        ApplyScaledLayout();

        float panelWidth = panelRect != null
            ? panelRect.sizeDelta.x
            : SidePanelUiBootstrap.GetScaledPanelWidth(lastAppliedScale > 0f ? lastAppliedScale : 1f);
        panelClosedX = panelWidth + GetRightMargin();
        panelOpenX = -GetRightMargin();

        if (panelRect == null)
        {
            CacheSafeAreaState();
            return;
        }

        float targetX = isPanelOpen ? panelOpenX : panelClosedX;
        float panelY = panelRect.anchoredPosition.y;

        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
            slideRoutine = StartCoroutine(SlidePanel(targetX));
        }
        else
        {
            panelRect.anchoredPosition = new Vector2(targetX, panelY);
        }

        CacheSafeAreaState();
    }

    void CacheSafeAreaState()
    {
        lastSafeArea = Screen.safeArea;
        lastIsLandscape = IsLandscape;
        safeAreaCached = true;
    }

    void ApplyScaledLayout()
    {
        float scale = ComputePanelScale();
        bool missingProbeRow = transform.Find("SidePanelProbeLabel_Row") == null;
        if (!missingProbeRow && Mathf.Abs(scale - lastAppliedScale) < 0.001f)
            return;

        SidePanelUiBootstrap.ApplyCompactLayout(transform, scale);
        lastAppliedScale = scale;
        ResolveReferences();
    }

    float ComputePanelScale()
    {
        float maxScale = IsLandscape ? LandscapeScale : PortraitScale;
        float scale = maxScale;
        float baseHeight = SidePanelUiBootstrap.ComputePanelHeight(SidePanelUiBootstrap.ToggleRows.Length, 1f);
        float availableHeight = GetAvailablePanelHeight();
        if (baseHeight > 0.01f && availableHeight > 0.01f)
            scale = Mathf.Min(scale, availableHeight / baseHeight);

        return Mathf.Clamp(scale, MinPanelScale, maxScale);
    }

    float GetAvailablePanelHeight()
    {
        float fallback = SidePanelUiBootstrap.ComputePanelHeight(SidePanelUiBootstrap.ToggleRows.Length, LandscapeScale);
        if (panelRect == null || canvas == null)
            return fallback;

        Canvas root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
        float scaleFactor = root.scaleFactor;
        if (scaleFactor < 0.01f)
            scaleFactor = 1f;

        float canvasHeight = Screen.height / scaleFactor;
        if (canvasHeight < 1f)
            return fallback;

        SafeAreaInsets.GetCanvasInsets(canvas, out _, out _, out _, out float bottom);
        // Panel is top-anchored; anchoredPosition.y is negative offset from canvas top.
        float fromTop = -panelRect.anchoredPosition.y;
        float available = canvasHeight - fromTop - bottom - BottomFitPadding;
        return Mathf.Max(0f, available);
    }

    void LayoutPanelBelowMenuButton()
    {
        if (panelRect == null || menuButton == null || canvas == null)
            return;

        var menuButtonRect = menuButton.GetComponent<RectTransform>();
        var canvasRect = canvas.GetComponent<RectTransform>();
        if (menuButtonRect == null || canvasRect == null)
            return;

        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);

        Bounds menuBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, menuButtonRect);
        float y = menuBounds.min.y - canvasRect.rect.yMax - PanelBelowMenuGap;
        panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, y);
    }

    static Transform FindUiTransform(Transform root, string objectName)
    {
        if (root == null)
            return null;

        Transform direct = root.Find(objectName);
        if (direct != null)
            return direct;

        Transform inBar = root.Find("BodyNavigationBar/" + objectName);
        if (inBar != null)
            return inBar;

        foreach (Transform child in root)
        {
            Transform nested = FindUiTransform(child, objectName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    IEnumerator Start()
    {
        isInitializing = true;
        ResolveReferences();

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

        if (projectionToggle != null)
        {
            projectionToggle.SetIsOnWithoutNotify(ProjectionSettings.UseProjection);
            projectionToggle.onValueChanged.RemoveAllListeners();
            projectionToggle.onValueChanged.AddListener(OnProjectionToggleChanged);
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

        SetupScaleModeToggle(educationalToggle, SolarScaleMode.Educational);
        SetupScaleModeToggle(realSizesToggle, SolarScaleMode.TrueScale);
        SyncScaleModeToggles(ScaleSettings.Mode);

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

        if (realSunToggle != null)
        {
            realSunToggle.SetIsOnWithoutNotify(SunAppearanceSettings.UseRealSun);
            realSunToggle.onValueChanged.RemoveAllListeners();
            realSunToggle.onValueChanged.AddListener(OnRealSunToggleChanged);
        }

        if (timeMachineToggle != null)
        {
            timeMachineToggle.SetIsOnWithoutNotify(TimeMachineSettings.UseTimeMachine);
            timeMachineToggle.onValueChanged.RemoveAllListeners();
            timeMachineToggle.onValueChanged.AddListener(OnTimeMachineToggleChanged);
        }

        if (probeToggle != null)
        {
            probeToggle.SetIsOnWithoutNotify(ProbeSettings.UseProbe);
            probeToggle.onValueChanged.RemoveAllListeners();
            probeToggle.onValueChanged.AddListener(OnProbeToggleChanged);
        }

        SimulationViewSettings.ShowOrbitLinesChanged += OnOrbitsSettingChanged;
        SimulationViewSettings.ShowBodyLabelsChanged += OnLabelsSettingChanged;
        SimulationViewSettings.ShowMinimapChanged += OnMinimapSettingChanged;
        SimulationViewSettings.ShowSimulationUiChanged += OnUiSettingChanged;
        SimulationViewSettings.UseFreeObservationChanged += OnFreeObservationSettingChanged;
        GravityGridSettings.UseGravityGridChanged += OnGravityGridSettingChanged;
        ProjectionSettings.UseProjectionChanged += OnProjectionSettingChanged;
        ScaleSettings.ModeChanged += OnScaleModeSettingChanged;
        OrbitSettings.UseRealOrbitsChanged += OnRealOrbitsSettingChanged;
        CometMovementSettings.UseCometMovementChanged += OnCometMovementSettingChanged;
        SunAppearanceSettings.UseRealSunChanged += OnRealSunSettingChanged;
        TimeMachineSettings.UseTimeMachineChanged += OnTimeMachineSettingChanged;
        ProbeSettings.UseProbeChanged += OnProbeSettingChanged;

        yield return new WaitForEndOfFrame();
        isInitializing = false;
        RefreshSafeAreaLayout();
    }

    void OnDestroy()
    {
        SimulationViewSettings.ShowOrbitLinesChanged -= OnOrbitsSettingChanged;
        SimulationViewSettings.ShowBodyLabelsChanged -= OnLabelsSettingChanged;
        SimulationViewSettings.ShowMinimapChanged -= OnMinimapSettingChanged;
        SimulationViewSettings.ShowSimulationUiChanged -= OnUiSettingChanged;
        SimulationViewSettings.UseFreeObservationChanged -= OnFreeObservationSettingChanged;
        GravityGridSettings.UseGravityGridChanged -= OnGravityGridSettingChanged;
        ProjectionSettings.UseProjectionChanged -= OnProjectionSettingChanged;
        ScaleSettings.ModeChanged -= OnScaleModeSettingChanged;
        OrbitSettings.UseRealOrbitsChanged -= OnRealOrbitsSettingChanged;
        CometMovementSettings.UseCometMovementChanged -= OnCometMovementSettingChanged;
        SunAppearanceSettings.UseRealSunChanged -= OnRealSunSettingChanged;
        TimeMachineSettings.UseTimeMachineChanged -= OnTimeMachineSettingChanged;
        ProbeSettings.UseProbeChanged -= OnProbeSettingChanged;

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

    void OnProjectionToggleChanged(bool isOn)
    {
        if (isInitializing) return;
        ProjectionSettings.SetUseProjection(isOn);
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

    void SetupScaleModeToggle(Toggle toggle, SolarScaleMode mode)
    {
        if (toggle == null)
            return;

        toggle.SetIsOnWithoutNotify(ScaleSettings.Mode == mode);
        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener(isOn => OnScaleModeToggleChanged(mode, isOn));
    }

    void OnScaleModeToggleChanged(SolarScaleMode mode, bool isOn)
    {
        if (isInitializing) return;

        if (isOn)
        {
            ScaleSettings.SetMode(mode);
            return;
        }

        // Radio behaviour: never allow both scale toggles off. Re-arm the one the user tried to clear.
        if (!AnyScaleModeToggleOn())
            GetScaleModeToggle(mode)?.SetIsOnWithoutNotify(true);
    }

    bool AnyScaleModeToggleOn()
    {
        return (educationalToggle != null && educationalToggle.isOn)
            || (realSizesToggle != null && realSizesToggle.isOn);
    }

    Toggle GetScaleModeToggle(SolarScaleMode mode)
    {
        switch (mode)
        {
            case SolarScaleMode.Educational: return educationalToggle;
            case SolarScaleMode.TrueScale: return realSizesToggle;
            default: return null;
        }
    }

    void SyncScaleModeToggles(SolarScaleMode mode)
    {
        educationalToggle?.SetIsOnWithoutNotify(mode == SolarScaleMode.Educational);
        realSizesToggle?.SetIsOnWithoutNotify(mode == SolarScaleMode.TrueScale);
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

    void OnRealSunToggleChanged(bool isOn)
    {
        if (isInitializing) return;
        SunAppearanceSettings.SetUseRealSun(isOn);
    }

    void OnTimeMachineToggleChanged(bool isOn)
    {
        if (isInitializing) return;
        TimeMachineSettings.SetUseTimeMachine(isOn);
    }

    void OnProbeToggleChanged(bool isOn)
    {
        if (isInitializing) return;
        ProbeSettings.SetUseProbe(isOn);
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

    void OnProjectionSettingChanged(bool isOn)
    {
        if (projectionToggle != null)
            projectionToggle.SetIsOnWithoutNotify(isOn);
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

    void OnScaleModeSettingChanged(SolarScaleMode mode)
    {
        SyncScaleModeToggles(mode);
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

    void OnRealSunSettingChanged(bool isOn)
    {
        if (realSunToggle != null)
            realSunToggle.SetIsOnWithoutNotify(isOn);
    }

    void OnTimeMachineSettingChanged(bool isOn)
    {
        if (timeMachineToggle != null)
            timeMachineToggle.SetIsOnWithoutNotify(isOn);
    }

    void OnProbeSettingChanged(bool isOn)
    {
        if (probeToggle != null)
            probeToggle.SetIsOnWithoutNotify(isOn);
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

    void TryCloseOnOutsideClick()
    {
        if (!isPanelOpen || isInitializing)
            return;

        if (EventSystem.current == null)
            return;

        if (!TryGetPrimaryPointerDown(out Vector2 screenPosition))
            return;

        if (IsPointerOverPanelOrMenu(screenPosition))
            return;

        SetPanelOpen(false);
    }

    static bool TryGetPrimaryPointerDown(out Vector2 screenPosition)
    {
        TouchInputBridge.EnsureInitialized();

        for (int i = 0; i < TouchInputBridge.touchCount; i++)
        {
            TouchInputBridge.TouchSample touch = TouchInputBridge.GetTouch(i);
            if (touch.phase == TouchPhase.Began)
            {
                screenPosition = touch.position;
                return true;
            }
        }

        if (TouchInputBridge.touchCount == 0 && Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        screenPosition = default;
        return false;
    }

    bool IsPointerOverPanelOrMenu(Vector2 screenPosition)
    {
        s_raycastResults.Clear();
        var eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
        EventSystem.current.RaycastAll(eventData, s_raycastResults);

        for (int i = 0; i < s_raycastResults.Count; i++)
        {
            Transform hit = s_raycastResults[i].gameObject.transform;
            if (panelRect != null && hit.IsChildOf(panelRect))
                return true;
            if (menuButton != null && hit.IsChildOf(menuButton.transform))
                return true;
        }

        return false;
    }
}
