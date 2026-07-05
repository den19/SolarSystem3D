using System;

namespace SolarSystemApp
{
    /// <summary>
    /// Persisted comet movement toggle for Level1 comet visibility and animation.
    /// </summary>
    public static class CometMovementSettings
    {
        const string KeyCometMovement = "SolarSystem_UseCometMovement";

        static bool initialized;
        static bool useCometMovement = true;

        public static event Action<bool> UseCometMovementChanged;

        public static bool UseCometMovement
        {
            get { EnsureInitialized(); return useCometMovement; }
        }

        static void EnsureInitialized()
        {
            if (initialized) return;

            useCometMovement = UnityEngine.PlayerPrefs.GetInt(KeyCometMovement, 1) != 0;
            initialized = true;
        }

        public static void SetUseCometMovement(bool enabled)
        {
            EnsureInitialized();
            if (useCometMovement == enabled) return;

            useCometMovement = enabled;
            UnityEngine.PlayerPrefs.SetInt(KeyCometMovement, enabled ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
            UseCometMovementChanged?.Invoke(enabled);
        }
    }
}
