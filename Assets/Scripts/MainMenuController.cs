using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] GameObject continueButton;

    void OnEnable()
    {
        SimulationSessionState.Load();

        if (continueButton != null)
        {
            continueButton.SetActive(SimulationSessionState.HasSavedState);
        }
    }

    void Start()
    {
        if (!SimulationSessionState.HasSavedState || continueButton == null)
            return;

        if (!continueButton.activeInHierarchy)
            return;

        var selectable = continueButton.GetComponent<Selectable>();
        if (selectable == null || !selectable.IsInteractable() || EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(continueButton);
    }
}
