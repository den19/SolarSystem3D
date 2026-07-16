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

        var extraGraphics = GetComponentInChildren<ExtraGraphicsControlController>(true);
        if (extraGraphics != null)
        {
            // Extra graphics toggle refreshes via event from ResetToDefaults.
        }

        var graphicsTier = GraphicsTierControlController.EnsureRow();
        graphicsTier?.RefreshFromSaved();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound();
        }
    }
}
