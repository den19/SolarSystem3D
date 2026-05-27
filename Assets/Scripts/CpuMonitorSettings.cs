using System;

namespace SolarSystemApp
{
    /// <summary>
    /// Persisted CPU monitor toggle. Separate from graphics to keep concerns isolated.
    /// </summary>
    public static class CpuMonitorSettings
    {
        const string PlayerPrefsKey = "SolarSystem_UseCpuMonitor";

        static bool initialized;
        static bool cachedValue = true;

        /// <summary>Fired whenever the persisted value changes (after save).</summary>
        public static event Action<bool> UseCpuMonitorChanged;

        public static bool UseCpuMonitor
        {
            get
            {
                EnsureInitialized();
                return cachedValue;
            }
        }

        static void EnsureInitialized()
        {
            if (initialized) return;
            cachedValue = UnityEngine.PlayerPrefs.GetInt(PlayerPrefsKey, 1) != 0;
            initialized = true;
        }

        public static void SetUseCpuMonitor(bool enabled)
        {
            EnsureInitialized();
            if (cachedValue == enabled) return;

            cachedValue = enabled;
            UnityEngine.PlayerPrefs.SetInt(PlayerPrefsKey, cachedValue ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            UseCpuMonitorChanged?.Invoke(cachedValue);
        }
    }
}

