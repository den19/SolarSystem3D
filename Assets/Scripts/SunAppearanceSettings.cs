using System;

namespace SolarSystemApp
{
    /// <summary>
    /// Persisted Real Sun toggle for Level1. When on, the Sun uses fiery animated emission;
    /// when off, it appears as a bright white lantern.
    /// </summary>
    public static class SunAppearanceSettings
    {
        const string KeyUseRealSun = "SolarSystem_UseRealSun";

        static bool initialized;
        static bool useRealSun = true;

        public static event Action<bool> UseRealSunChanged;

        public static bool UseRealSun
        {
            get { EnsureInitialized(); return useRealSun; }
        }

        static void EnsureInitialized()
        {
            if (initialized) return;

            useRealSun = UnityEngine.PlayerPrefs.GetInt(KeyUseRealSun, 1) != 0;
            initialized = true;
        }

        public static void SetUseRealSun(bool enabled)
        {
            EnsureInitialized();
            if (useRealSun == enabled) return;

            useRealSun = enabled;
            UnityEngine.PlayerPrefs.SetInt(KeyUseRealSun, enabled ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            UseRealSunChanged?.Invoke(enabled);
        }

        public static void ResetToDefaults()
        {
            UnityEngine.PlayerPrefs.DeleteKey(KeyUseRealSun);

            initialized = true;
            useRealSun = true;

            UnityEngine.PlayerPrefs.Save();
            UseRealSunChanged?.Invoke(useRealSun);
        }
    }
}
