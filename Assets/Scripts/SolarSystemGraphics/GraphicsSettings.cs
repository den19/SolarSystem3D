using System;

namespace SolarSystemApp
{
    /// <summary>
    /// Persisted Extra Graphics toggle. Lives in SolarSystemApp to avoid clashes with UnityEngine.Rendering.GraphicsSettings.
    /// </summary>
    public static class GraphicsSettings
    {
        const string PlayerPrefsKey = "SolarSystem_UseExtraGraphics";

        static bool Initialized;
        static bool CachedValue = true;

        /// <summary>Fired whenever the persisted value changes (after save).</summary>
        public static event Action<bool> UseExtraGraphicsChanged;

        public static bool UseExtraGraphics
        {
            get
            {
                EnsureInitialized();
                return CachedValue;
            }
        }

        static void EnsureInitialized()
        {
            if (Initialized) return;
            CachedValue = UnityEngine.PlayerPrefs.GetInt(PlayerPrefsKey, 1) != 0;
            Initialized = true;
        }

        public static void SetUseExtraGraphics(bool useExtraGraphics)
        {
            EnsureInitialized();
            if (CachedValue == useExtraGraphics) return;

            CachedValue = useExtraGraphics;
            UnityEngine.PlayerPrefs.SetInt(PlayerPrefsKey, CachedValue ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            UseExtraGraphicsChanged?.Invoke(CachedValue);
        }
    }
}
