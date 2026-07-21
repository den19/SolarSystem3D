using System;

namespace SolarSystemApp
{
    /// <summary>
    /// Persisted Projection mode toggle: body drop lines and green glows on the gravity grid.
    /// Default off — enable only via side panel.
    /// </summary>
    public static class ProjectionSettings
    {
        const string PlayerPrefsKey = "SolarSystem_UseProjection";

        static bool initialized;
        static bool cachedValue;

        public static event Action<bool> UseProjectionChanged;

        public static bool UseProjection
        {
            get
            {
                EnsureInitialized();
                return cachedValue;
            }
        }

        static void EnsureInitialized()
        {
            if (initialized)
                return;

            cachedValue = UnityEngine.PlayerPrefs.GetInt(PlayerPrefsKey, 0) != 0;
            initialized = true;
        }

        public static void SetUseProjection(bool enabled)
        {
            EnsureInitialized();
            if (cachedValue == enabled)
                return;

            cachedValue = enabled;
            UnityEngine.PlayerPrefs.SetInt(PlayerPrefsKey, cachedValue ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            UseProjectionChanged?.Invoke(cachedValue);
        }

        public static void ResetToDefaults()
        {
            UnityEngine.PlayerPrefs.DeleteKey(PlayerPrefsKey);

            initialized = true;
            cachedValue = false;

            UnityEngine.PlayerPrefs.Save();
            UseProjectionChanged?.Invoke(cachedValue);
        }
    }
}
