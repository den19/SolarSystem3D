using System;

namespace SolarSystemApp
{
    /// <summary>
    /// Persisted gravity grid toggle for the spacetime fabric visualization in Level1.
    /// </summary>
    public static class GravityGridSettings
    {
        const string PlayerPrefsKey = "SolarSystem_UseGravityGrid";

        static bool initialized;
        static bool cachedValue = true;

        public static event Action<bool> UseGravityGridChanged;

        public static bool UseGravityGrid
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

        public static void SetUseGravityGrid(bool enabled)
        {
            EnsureInitialized();
            if (cachedValue == enabled) return;

            cachedValue = enabled;
            UnityEngine.PlayerPrefs.SetInt(PlayerPrefsKey, cachedValue ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            UseGravityGridChanged?.Invoke(cachedValue);
        }
    }
}
