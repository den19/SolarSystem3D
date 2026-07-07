using System;

namespace SolarSystemApp
{
    /// <summary>
    /// Persisted simulation view toggles for Level1 (orbit lines, labels, minimap, HUD).
    /// </summary>
    public static class SimulationViewSettings
    {
        const string KeyOrbitLines = "SolarSystem_ShowOrbitLines";
        const string KeyBodyLabels = "SolarSystem_ShowBodyLabels";
        const string KeyMinimap = "SolarSystem_ShowMinimap";
        const string KeySimulationUi = "SolarSystem_ShowSimulationUi";
        const string KeyFreeObservation = "SolarSystem_UseFreeObservation";

        static bool initialized;
        static bool showOrbitLines = true;
        static bool showBodyLabels = true;
        static bool showMinimap = true;
        static bool showSimulationUi = true;
        static bool useFreeObservation;

        public static event Action<bool> ShowOrbitLinesChanged;
        public static event Action<bool> ShowBodyLabelsChanged;
        public static event Action<bool> ShowMinimapChanged;
        public static event Action<bool> ShowSimulationUiChanged;
        public static event Action<bool> UseFreeObservationChanged;

        public static bool ShowOrbitLines
        {
            get { EnsureInitialized(); return showOrbitLines; }
        }

        public static bool ShowBodyLabels
        {
            get { EnsureInitialized(); return showBodyLabels; }
        }

        public static bool ShowMinimap
        {
            get { EnsureInitialized(); return showMinimap; }
        }

        public static bool ShowSimulationUi
        {
            get { EnsureInitialized(); return showSimulationUi; }
        }

        public static bool UseFreeObservation
        {
            get { EnsureInitialized(); return useFreeObservation; }
        }

        static void EnsureInitialized()
        {
            if (initialized) return;

            showOrbitLines = UnityEngine.PlayerPrefs.GetInt(KeyOrbitLines, 1) != 0;
            showBodyLabels = UnityEngine.PlayerPrefs.GetInt(KeyBodyLabels, 1) != 0;
            showMinimap = UnityEngine.PlayerPrefs.GetInt(KeyMinimap, 1) != 0;
            showSimulationUi = UnityEngine.PlayerPrefs.GetInt(KeySimulationUi, 1) != 0;
            useFreeObservation = UnityEngine.PlayerPrefs.GetInt(KeyFreeObservation, 0) != 0;
            initialized = true;
        }

        public static void SetShowOrbitLines(bool enabled)
        {
            EnsureInitialized();
            if (showOrbitLines == enabled) return;

            showOrbitLines = enabled;
            UnityEngine.PlayerPrefs.SetInt(KeyOrbitLines, enabled ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            ShowOrbitLinesChanged?.Invoke(enabled);
        }

        public static void SetShowBodyLabels(bool enabled)
        {
            EnsureInitialized();
            if (showBodyLabels == enabled) return;

            showBodyLabels = enabled;
            UnityEngine.PlayerPrefs.SetInt(KeyBodyLabels, enabled ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            ShowBodyLabelsChanged?.Invoke(enabled);
        }

        public static void SetShowMinimap(bool enabled)
        {
            EnsureInitialized();
            if (showMinimap == enabled) return;

            showMinimap = enabled;
            UnityEngine.PlayerPrefs.SetInt(KeyMinimap, enabled ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            ShowMinimapChanged?.Invoke(enabled);
        }

        public static void SetShowSimulationUi(bool enabled)
        {
            EnsureInitialized();
            if (showSimulationUi == enabled) return;

            showSimulationUi = enabled;
            UnityEngine.PlayerPrefs.SetInt(KeySimulationUi, enabled ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            ShowSimulationUiChanged?.Invoke(enabled);
        }

        public static void SetUseFreeObservation(bool enabled)
        {
            EnsureInitialized();
            if (useFreeObservation == enabled) return;

            useFreeObservation = enabled;
            UnityEngine.PlayerPrefs.SetInt(KeyFreeObservation, enabled ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            UseFreeObservationChanged?.Invoke(enabled);
        }

        public static void ResetToDefaults()
        {
            UnityEngine.PlayerPrefs.DeleteKey(KeyOrbitLines);
            UnityEngine.PlayerPrefs.DeleteKey(KeyBodyLabels);
            UnityEngine.PlayerPrefs.DeleteKey(KeyMinimap);
            UnityEngine.PlayerPrefs.DeleteKey(KeySimulationUi);
            UnityEngine.PlayerPrefs.DeleteKey(KeyFreeObservation);

            initialized = true;
            showOrbitLines = true;
            showBodyLabels = true;
            showMinimap = true;
            showSimulationUi = true;
            useFreeObservation = false;

            UnityEngine.PlayerPrefs.Save();
            ShowOrbitLinesChanged?.Invoke(showOrbitLines);
            ShowBodyLabelsChanged?.Invoke(showBodyLabels);
            ShowMinimapChanged?.Invoke(showMinimap);
            ShowSimulationUiChanged?.Invoke(showSimulationUi);
            UseFreeObservationChanged?.Invoke(useFreeObservation);
        }
    }
}
