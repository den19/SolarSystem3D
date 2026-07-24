using UnityEngine;

public class SettingsResetController : MonoBehaviour
{
    [SerializeField] private VolumeControlController volumeControl;
    [SerializeField] private MusicControlController musicControl;
    [SerializeField] private LanguageChanger languageChanger;

    public void OnResetClicked()
    {
        SolarSystemConfigurationReset.ResetToDefaults();

        if (volumeControl == null)
        {
            volumeControl = GetComponentInChildren<VolumeControlController>(true);
        }

        if (musicControl == null)
        {
            musicControl = GetComponentInChildren<MusicControlController>(true);
        }

        if (languageChanger == null)
        {
            languageChanger = GetComponentInChildren<LanguageChanger>(true);
        }

        volumeControl?.RefreshFromSaved();
        musicControl?.RefreshFromSaved();
        languageChanger?.RefreshFromSaved();

        var extraGraphics = GetComponentInChildren<ExtraGraphicsControlController>(true);
        if (extraGraphics != null)
        {
            // Extra graphics toggle refreshes via event from ResetToDefaults.
        }

        var graphicsTier = GraphicsTierControlController.FindInScene();
        graphicsTier?.RefreshFromSaved();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound();
        }
    }
}
