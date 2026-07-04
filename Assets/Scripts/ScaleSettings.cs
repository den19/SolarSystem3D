using System;

namespace SolarSystemApp
{
    /// <summary>
    /// Persisted real-distance and real-size toggles for Level1 scale modes.
    /// </summary>
    public static class ScaleSettings
    {
        const string KeyRealDistances = "SolarSystem_UseRealDistances";
        const string KeyRealSizes = "SolarSystem_UseRealSizes";

        static bool initialized;
        static bool useRealDistances;
        static bool useRealSizes;

        public static event Action<bool> UseRealDistancesChanged;
        public static event Action<bool> UseRealSizesChanged;

        public static bool UseRealDistances
        {
            get { EnsureInitialized(); return useRealDistances; }
        }

        public static bool UseRealSizes
        {
            get { EnsureInitialized(); return useRealSizes; }
        }

        static void EnsureInitialized()
        {
            if (initialized) return;

            useRealDistances = UnityEngine.PlayerPrefs.GetInt(KeyRealDistances, 0) != 0;
            useRealSizes = UnityEngine.PlayerPrefs.GetInt(KeyRealSizes, 0) != 0;
            initialized = true;
        }

        public static void SetUseRealDistances(bool enabled)
        {
            EnsureInitialized();
            if (useRealDistances == enabled) return;

            useRealDistances = enabled;
            UnityEngine.PlayerPrefs.SetInt(KeyRealDistances, enabled ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            UseRealDistancesChanged?.Invoke(enabled);
        }

        public static void SetUseRealSizes(bool enabled)
        {
            EnsureInitialized();
            if (useRealSizes == enabled) return;

            useRealSizes = enabled;
            UnityEngine.PlayerPrefs.SetInt(KeyRealSizes, enabled ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            UseRealSizesChanged?.Invoke(enabled);
        }
    }
}
