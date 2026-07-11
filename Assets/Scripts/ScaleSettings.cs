using System;

namespace SolarSystemApp
{
    /// <summary>
    /// Presentation scale presets for Level1, chosen as mutually exclusive radio options.
    /// Schematic  = compressed educational layout (compact orbits, enlarged bodies).
    /// TrueScale  = true orbit ratios with catalog body sizes (soft-compressed),
    ///              so inner planets never fall inside the Sun.
    /// </summary>
    public enum ScaleMode
    {
        Educational = 0,
        // Value 1 was RealDistances (removed); kept out of enum for PlayerPrefs migration.
        TrueScale = 2
    }

    /// <summary>
    /// Persisted scale mode for Level1. Exposes legacy UseRealDistances/UseRealSizes
    /// booleans (derived from the mode) so existing subscribers keep working.
    /// </summary>
    public static class ScaleSettings
    {
        const string KeyMode = "SolarSystem_ScaleMode";
        const string KeyRealDistances = "SolarSystem_UseRealDistances"; // legacy
        const string KeyRealSizes = "SolarSystem_UseRealSizes";         // legacy

        const int LegacyRealDistancesRaw = 1;
        const int LegacyTrueScaleRaw = 2;

        const ScaleMode DefaultMode = ScaleMode.Educational;

        static bool initialized;
        static ScaleMode mode;

        public static event Action<ScaleMode> ModeChanged;
        public static event Action<bool> UseRealDistancesChanged;
        public static event Action<bool> UseRealSizesChanged;

        public static ScaleMode Mode
        {
            get { EnsureInitialized(); return mode; }
        }

        public static bool UseRealDistances
        {
            get { EnsureInitialized(); return mode != ScaleMode.Educational; }
        }

        public static bool UseRealSizes
        {
            get { EnsureInitialized(); return mode == ScaleMode.TrueScale; }
        }

        static void EnsureInitialized()
        {
            if (initialized) return;

            if (UnityEngine.PlayerPrefs.HasKey(KeyMode))
                mode = ClampMode(UnityEngine.PlayerPrefs.GetInt(KeyMode, (int)DefaultMode));
            else
                mode = MigrateLegacy();

            initialized = true;
        }

        static ScaleMode MigrateLegacy()
        {
            bool hasLegacy = UnityEngine.PlayerPrefs.HasKey(KeyRealDistances)
                || UnityEngine.PlayerPrefs.HasKey(KeyRealSizes);
            if (!hasLegacy)
                return DefaultMode;

            bool legacyDistances = UnityEngine.PlayerPrefs.GetInt(KeyRealDistances, 1) != 0;
            bool legacySizes = UnityEngine.PlayerPrefs.GetInt(KeyRealSizes, 1) != 0;

            if (legacySizes)
                return ScaleMode.TrueScale;
            if (legacyDistances)
                return ScaleMode.Educational;
            return ScaleMode.Educational;
        }

        static ScaleMode ClampMode(int raw)
        {
            switch (raw)
            {
                case (int)ScaleMode.Educational:
                    return ScaleMode.Educational;
                case LegacyTrueScaleRaw:
                    return ScaleMode.TrueScale;
                case LegacyRealDistancesRaw:
                    return ScaleMode.Educational;
                default:
                    return ScaleMode.Educational;
            }
        }

        public static void SetMode(ScaleMode newMode)
        {
            EnsureInitialized();
            if (mode == newMode) return;

            bool prevDistances = mode != ScaleMode.Educational;
            bool prevSizes = mode == ScaleMode.TrueScale;

            mode = newMode;
            UnityEngine.PlayerPrefs.SetInt(KeyMode, (int)mode);
            UnityEngine.PlayerPrefs.Save();

            ModeChanged?.Invoke(mode);

            bool nowDistances = mode != ScaleMode.Educational;
            bool nowSizes = mode == ScaleMode.TrueScale;
            if (nowDistances != prevDistances)
                UseRealDistancesChanged?.Invoke(nowDistances);
            if (nowSizes != prevSizes)
                UseRealSizesChanged?.Invoke(nowSizes);
        }

        // Backward-compatible helpers that map onto the mutually exclusive presets.
        public static void SetUseRealDistances(bool enabled)
        {
            EnsureInitialized();
            if (enabled)
            {
                if (mode == ScaleMode.Educational)
                    SetMode(ScaleMode.TrueScale);
            }
            else
            {
                SetMode(ScaleMode.Educational);
            }
        }

        public static void SetUseRealSizes(bool enabled)
        {
            EnsureInitialized();
            if (enabled)
                SetMode(ScaleMode.TrueScale);
            else if (mode == ScaleMode.TrueScale)
                SetMode(ScaleMode.Educational);
        }

        public static void ResetToDefaults()
        {
            UnityEngine.PlayerPrefs.DeleteKey(KeyRealDistances);
            UnityEngine.PlayerPrefs.DeleteKey(KeyRealSizes);
            UnityEngine.PlayerPrefs.DeleteKey(KeyMode);

            bool prevDistances = initialized && mode != ScaleMode.Educational;
            bool prevSizes = initialized && mode == ScaleMode.TrueScale;

            initialized = true;
            mode = DefaultMode;

            UnityEngine.PlayerPrefs.SetInt(KeyMode, (int)mode);
            UnityEngine.PlayerPrefs.Save();

            ModeChanged?.Invoke(mode);

            bool nowDistances = mode != ScaleMode.Educational;
            bool nowSizes = mode == ScaleMode.TrueScale;
            if (nowDistances != prevDistances)
                UseRealDistancesChanged?.Invoke(nowDistances);
            if (nowSizes != prevSizes)
                UseRealSizesChanged?.Invoke(nowSizes);
        }
    }
}
