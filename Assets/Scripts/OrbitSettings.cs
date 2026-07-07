using System;

namespace SolarSystemApp
{
    /// <summary>
    /// Persisted real-orbit toggle for Level1 elliptical planet motion and orbit lines.
    /// </summary>
    public static class OrbitSettings
    {
        const string KeyRealOrbits = "SolarSystem_UseRealOrbits";

        static bool initialized;
        static bool useRealOrbits;

        public static event Action<bool> UseRealOrbitsChanged;

        public static bool UseRealOrbits
        {
            get { EnsureInitialized(); return useRealOrbits; }
        }

        static void EnsureInitialized()
        {
            if (initialized) return;

            useRealOrbits = UnityEngine.PlayerPrefs.GetInt(KeyRealOrbits, 0) != 0;
            initialized = true;
        }

        public static void SetUseRealOrbits(bool enabled)
        {
            EnsureInitialized();
            if (useRealOrbits == enabled) return;

            useRealOrbits = enabled;
            UnityEngine.PlayerPrefs.SetInt(KeyRealOrbits, enabled ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            UseRealOrbitsChanged?.Invoke(enabled);
        }

        public static void ResetToDefaults()
        {
            UnityEngine.PlayerPrefs.DeleteKey(KeyRealOrbits);

            initialized = true;
            useRealOrbits = false;

            UnityEngine.PlayerPrefs.Save();
            UseRealOrbitsChanged?.Invoke(useRealOrbits);
        }
    }
}
