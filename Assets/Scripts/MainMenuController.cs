using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; private set; }

    [SerializeField] GameObject continueButton;

    Coroutine applyDefaultSelectionCoroutine;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        RefreshContinueButtonVisibility();
    }

    void Start()
    {
        ApplyDefaultSelection();
    }

    public void ApplyDefaultSelection()
    {
        if (!isActiveAndEnabled)
            return;

        ApplyDefaultSelectionImmediate();

        if (applyDefaultSelectionCoroutine != null)
            StopCoroutine(applyDefaultSelectionCoroutine);

        applyDefaultSelectionCoroutine = StartCoroutine(ApplyDefaultSelectionDeferred());
    }

    void RefreshContinueButtonVisibility()
    {
        SimulationSessionState.Load();

        if (continueButton != null)
            continueButton.SetActive(SimulationSessionState.HasSavedState);
    }

    void ApplyDefaultSelectionImmediate()
    {
        RefreshContinueButtonVisibility();

        var target = ResolveDefaultSelectionTarget();
        if (target == null || EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
    }

    IEnumerator ApplyDefaultSelectionDeferred()
    {
        yield return null;
        yield return null;

        applyDefaultSelectionCoroutine = null;
        ApplyDefaultSelectionImmediate();
    }

    GameObject ResolveDefaultSelectionTarget()
    {
        if (SimulationSessionState.HasSavedState && continueButton != null && continueButton.activeInHierarchy)
        {
            var continueSelectable = continueButton.GetComponent<Selectable>();
            if (continueSelectable != null && continueSelectable.IsInteractable())
                return continueButton;
        }

        var menuRoot = continueButton != null ? continueButton.transform.parent : null;
        if (menuRoot == null)
            return null;

        var selectables = menuRoot.GetComponentsInChildren<Selectable>(true);
        foreach (var selectable in selectables)
        {
            if (selectable.IsActive() && selectable.IsInteractable())
                return selectable.gameObject;
        }

        return null;
    }
}
