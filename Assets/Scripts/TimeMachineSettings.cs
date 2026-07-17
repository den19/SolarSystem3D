using System;

namespace SolarSystemApp
{
    /// <summary>
    /// Persisted Time Machine mode toggle for Level1 calendar-driven orbits.
    /// Default off — enable only via side panel.
    /// </summary>
    public static class TimeMachineSettings
    {
        const string PlayerPrefsKey = "SolarSystem_UseTimeMachine";

        static bool initialized;
        static bool cachedValue;

        public static event Action<bool> UseTimeMachineChanged;

        public static bool UseTimeMachine
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

        public static void SetUseTimeMachine(bool enabled)
        {
            EnsureInitialized();
            if (cachedValue == enabled)
                return;

            cachedValue = enabled;
            UnityEngine.PlayerPrefs.SetInt(PlayerPrefsKey, cachedValue ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            UseTimeMachineChanged?.Invoke(cachedValue);
        }

        public static void ResetToDefaults()
        {
            UnityEngine.PlayerPrefs.DeleteKey(PlayerPrefsKey);

            initialized = true;
            cachedValue = false;

            UnityEngine.PlayerPrefs.Save();
            UseTimeMachineChanged?.Invoke(cachedValue);
        }
    }
}
