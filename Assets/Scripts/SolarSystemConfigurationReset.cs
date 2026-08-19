using SolarSystemApp;
using UnityEngine;

public static class SolarSystemConfigurationReset
{
    public static void ResetToDefaults()
    {
        SimulationViewSettings.ResetToDefaults();
        ScaleSettings.ResetToDefaults();
        OrbitSettings.ResetToDefaults();
        CometMovementSettings.ResetToDefaults();
        GravityGridSettings.ResetToDefaults();
        ProjectionSettings.ResetToDefaults();
        GraphicsSettings.ResetToDefaults();
        CpuMonitorSettings.ResetToDefaults();
        MilkyWaySettings.ResetToDefaults();
        SunAppearanceSettings.ResetToDefaults();
        TimeMachineSettings.ResetToDefaults();
        ProbeSettings.ResetToDefaults();
        GraphicsTierSettings.ResetToDefaults();
        SimulationSessionState.Clear();
        SimulationClock.ResetToSweepStart();
        SimulationClock.SetActive(false);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResetToDefaults();
        }

        PlayerPrefs.DeleteKey("SelectedLanguage");
        PlayerPrefs.Save();

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.ChangeLanguage(Language.Russian);
        }

        GameState.language = Language.Russian;
    }
}
