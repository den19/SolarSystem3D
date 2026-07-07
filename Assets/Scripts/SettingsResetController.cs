using UnityEngine;

public class SettingsResetController : MonoBehaviour
{
    [SerializeField] private VolumeControlController volumeControl;
    [SerializeField] private LanguageChanger languageChanger;

    public void OnResetClicked()
    {
        SolarSystemConfigurationReset.ResetToDefaults();

        if (volumeControl == null)
        {
            volumeControl = GetComponentInChildren<VolumeControlController>(true);
        }

        if (languageChanger == null)
        {
            languageChanger = GetComponentInChildren<LanguageChanger>(true);
        }

        volumeControl?.RefreshFromSaved();
        languageChanger?.RefreshFromSaved();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound();
        }
    }
}
