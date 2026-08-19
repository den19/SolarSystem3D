using UnityEngine;

namespace SolarSystemApp
{
    /// <summary>
    /// Persists whether the user completed their first probe launch (coach dismissed forever).
    /// </summary>
    public static class ProbeCoachSettings
    {
        const string KeyLaunchCompleted = "SolarSystem_ProbeCoachLaunchCompleted";

        static bool initialized;
        static bool launchCompleted;

        public static event System.Action LaunchCompletedChanged;

        public static bool LaunchCompleted
        {
            get
            {
                EnsureInitialized();
                return launchCompleted;
            }
        }

        static void EnsureInitialized()
        {
            if (initialized)
                return;

            launchCompleted = PlayerPrefs.GetInt(KeyLaunchCompleted, 0) != 0;
            initialized = true;
        }

        public static void MarkLaunchCompleted()
        {
            EnsureInitialized();
            if (launchCompleted)
                return;

            launchCompleted = true;
            PlayerPrefs.SetInt(KeyLaunchCompleted, 1);
            PlayerPrefs.Save();
            LaunchCompletedChanged?.Invoke();
        }

        public static void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(KeyLaunchCompleted);
            initialized = true;
            launchCompleted = false;
            LaunchCompletedChanged?.Invoke();
        }
    }
}
