using UnityEngine;

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
}
