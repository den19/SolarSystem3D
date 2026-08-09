using System;

namespace SolarSystemApp
{
    /// <summary>
    /// Persisted Milky Way skybox toggle (Space 3 on / Space 5 off).
    /// </summary>
    public static class MilkyWaySettings
    {
        const string PlayerPrefsKey = "SolarSystem_ShowMilkyWay";

        static bool initialized;
        static bool cachedValue;

        /// <summary>Fired whenever the persisted value changes (after save).</summary>
        public static event Action<bool> ShowMilkyWayChanged;

        public static bool ShowMilkyWay
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
            cachedValue = UnityEngine.PlayerPrefs.GetInt(PlayerPrefsKey, 0) != 0;
            initialized = true;
        }

        public static void SetShowMilkyWay(bool enabled)
        {
            EnsureInitialized();
            if (cachedValue == enabled) return;

            cachedValue = enabled;
            UnityEngine.PlayerPrefs.SetInt(PlayerPrefsKey, cachedValue ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            ShowMilkyWayChanged?.Invoke(cachedValue);
        }

        public static void ResetToDefaults()
        {
            UnityEngine.PlayerPrefs.DeleteKey(PlayerPrefsKey);

            initialized = true;
            cachedValue = false;

            UnityEngine.PlayerPrefs.Save();
            ShowMilkyWayChanged?.Invoke(cachedValue);
        }
    }
}
