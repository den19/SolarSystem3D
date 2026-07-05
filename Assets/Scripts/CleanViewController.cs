using System.Collections.Generic;
using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Hides simulation HUD for clean cosmic view. Comet visibility is controlled by CometMovementSettings.
/// </summary>
public class CleanViewController : MonoBehaviour
{
    static readonly HashSet<string> ExcludedCanvasChildren = new HashSet<string>
    {
        "SidePanelMenuButton",
        "SimulationSidePanel"
    };

    [SerializeField] string mainCanvasName = "MainScreenCanvas";

    Transform _mainCanvas;
    readonly List<(Transform transform, bool wasActive)> _hiddenElements = new List<(Transform, bool)>();
    LookAtTarget _lookAtTarget;
    CometSystemController _cometSystem;
    SimulationSidePanelController _sidePanel;

    public void Initialize(CometSystemController cometSystem, SimulationSidePanelController sidePanel)
    {
        _cometSystem = cometSystem;
        _sidePanel = sidePanel;
        _lookAtTarget = FindFirstObjectByType<LookAtTarget>();
        _mainCanvas = GameObject.Find(mainCanvasName)?.transform;
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

        if (!showSimulationUi && _sidePanel != null)
            _sidePanel.ClosePanelImmediate();
    }

    void ApplyCometMovement(bool enabled)
    {
        if (_cometSystem != null && _cometSystem.CometsRoot != null)
            _cometSystem.CometsRoot.gameObject.SetActive(enabled);
    }

    void HideSimulationUi()
    {
        _hiddenElements.Clear();

        if (_mainCanvas != null)
        {
            for (int i = 0; i < _mainCanvas.childCount; i++)
            {
                Transform child = _mainCanvas.GetChild(i);
                if (ExcludedCanvasChildren.Contains(child.name))
                    continue;

                _hiddenElements.Add((child, child.gameObject.activeSelf));
                child.gameObject.SetActive(false);
            }
        }

        if (_lookAtTarget != null)
            _lookAtTarget.HideAllDescriptions();
    }

    void RestoreHiddenElements()
    {
        for (int i = 0; i < _hiddenElements.Count; i++)
        {
            Transform target = _hiddenElements[i].transform;
            if (target != null)
                target.gameObject.SetActive(_hiddenElements[i].wasActive);
        }

        _hiddenElements.Clear();
    }
}
