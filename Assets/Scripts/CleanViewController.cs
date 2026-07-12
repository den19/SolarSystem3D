using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hides simulation HUD for clean cosmic view. Comet visibility is controlled by CometMovementSettings.
/// </summary>
public class CleanViewController : MonoBehaviour
{
    static readonly HashSet<string> ExcludedCanvasChildren = new HashSet<string>
    {
        "BodyNavigationBar",
        "SimulationSidePanel"
    };

    static readonly HashSet<string> NavigationBarChildrenToHide = new HashSet<string>
    {
        "PrevButton",
        "BodyNameButton",
        "NextButton",
        "SimulationControlButton"
    };

    [SerializeField] string mainCanvasName = "MainScreenCanvas";

    Transform _mainCanvas;
    readonly List<(Transform transform, bool wasActive)> _hiddenElements = new List<(Transform, bool)>();
    readonly List<(Transform transform, bool wasActive)> _hiddenBarChildren = new List<(Transform, bool)>();
    Image _navigationBarBackground;
    Color _navigationBarBackgroundColor;
    bool _navigationBarBackgroundStored;
    LookAtTarget _lookAtTarget;
    CometSystemController _cometSystem;
    SimulationSidePanelController _sidePanel;

    public void Initialize(CometSystemController cometSystem, SimulationSidePanelController sidePanel)
    {
        _cometSystem = cometSystem;
        _sidePanel = sidePanel;
        _lookAtTarget = FindFirstObjectByType<LookAtTarget>();
        _mainCanvas = GameObject.Find(mainCanvasName)?.transform;
        CacheNavigationBarBackground();
        ApplyCleanView(SimulationViewSettings.ShowSimulationUi);
        ApplyCometMovement(CometMovementSettings.UseCometMovement);
    }

    void OnEnable()
    {
        SimulationViewSettings.ShowSimulationUiChanged += ApplyCleanView;
        CometMovementSettings.UseCometMovementChanged += ApplyCometMovement;
    }

    void OnDisable()
    {
        SimulationViewSettings.ShowSimulationUiChanged -= ApplyCleanView;
        CometMovementSettings.UseCometMovementChanged -= ApplyCometMovement;
    }

    void ApplyCleanView(bool showSimulationUi)
    {
        if (showSimulationUi)
            RestoreHiddenElements();
        else
            HideSimulationUi();

        if (!showSimulationUi)
        {
            if (_sidePanel != null)
                _sidePanel.ClosePanelImmediate();

            var bodyPicker = FindFirstObjectByType<BodyNavigationPickerController>();
            if (bodyPicker != null)
                bodyPicker.Hide();
        }
    }

    void ApplyCometMovement(bool enabled)
    {
        if (_cometSystem != null && _cometSystem.CometsRoot != null)
            _cometSystem.CometsRoot.gameObject.SetActive(enabled);
    }

    void CacheNavigationBarBackground()
    {
        if (_mainCanvas == null)
            return;

        Transform bar = _mainCanvas.Find("BodyNavigationBar");
        if (bar == null || !bar.TryGetComponent(out _navigationBarBackground))
            return;

        _navigationBarBackgroundColor = _navigationBarBackground.color;
        _navigationBarBackgroundStored = true;
    }

    void HideSimulationUi()
    {
        _hiddenElements.Clear();
        _hiddenBarChildren.Clear();

        if (_mainCanvas != null)
        {
            for (int i = 0; i < _mainCanvas.childCount; i++)
            {
                Transform child = _mainCanvas.GetChild(i);
                if (ExcludedCanvasChildren.Contains(child.name))
                {
                    if (child.name == "BodyNavigationBar")
                        HideNavigationBarChildren(child);
                    continue;
                }

                _hiddenElements.Add((child, child.gameObject.activeSelf));
                child.gameObject.SetActive(false);
            }
        }

        if (_lookAtTarget != null)
            _lookAtTarget.HideAllDescriptions();
    }

    void HideNavigationBarChildren(Transform navigationBar)
    {
        for (int i = 0; i < navigationBar.childCount; i++)
        {
            Transform child = navigationBar.GetChild(i);
            if (!NavigationBarChildrenToHide.Contains(child.name))
                continue;

            _hiddenBarChildren.Add((child, child.gameObject.activeSelf));
            child.gameObject.SetActive(false);
        }

        if (!_navigationBarBackgroundStored && navigationBar.TryGetComponent(out _navigationBarBackground))
        {
            _navigationBarBackgroundColor = _navigationBarBackground.color;
            _navigationBarBackgroundStored = true;
        }

        if (_navigationBarBackground != null)
        {
            Color transparent = _navigationBarBackgroundColor;
            transparent.a = 0f;
            _navigationBarBackground.color = transparent;
        }
    }

    void RestoreHiddenElements()
    {
        for (int i = 0; i < _hiddenBarChildren.Count; i++)
        {
            Transform target = _hiddenBarChildren[i].transform;
            if (target != null)
                target.gameObject.SetActive(_hiddenBarChildren[i].wasActive);
        }

        _hiddenBarChildren.Clear();

        if (_navigationBarBackground != null && _navigationBarBackgroundStored)
            _navigationBarBackground.color = _navigationBarBackgroundColor;

        for (int i = 0; i < _hiddenElements.Count; i++)
        {
            Transform target = _hiddenElements[i].transform;
            if (target != null)
                target.gameObject.SetActive(_hiddenElements[i].wasActive);
        }

        _hiddenElements.Clear();
    }
}
