using System;
using UnityEngine;

namespace SolarSystemApp
{
    public enum GraphicsTierMode
    {
        Auto = 0,
        Balanced = 1,
        High = 2
    }

    public enum MobileGraphicsTier
    {
        Balanced = 0,
        High = 1
    }

    /// <summary>
    /// Mobile graphics tier: Balanced (renderScale 0.8) vs High (1.0 + MSAA 2).
    /// High also requires Extra Graphics ON. Auto picks High when GPU memory &gt;= 2048 MB.
    /// </summary>
    public static class GraphicsTierSettings
    {
        public const string PlayerPrefsKey = "SolarSystem_GraphicsTier";
        public const int AutoHighMinGraphicsMemoryMb = 2048;

        static bool initialized;
        static GraphicsTierMode mode = GraphicsTierMode.Auto;
        static MobileGraphicsTier effectiveTier = MobileGraphicsTier.Balanced;

        public static event Action<GraphicsTierMode> ModeChanged;
        public static event Action<MobileGraphicsTier> EffectiveTierChanged;

        public static GraphicsTierMode Mode
        {
            get
            {
                EnsureInitialized();
                return mode;
            }
        }

        public static MobileGraphicsTier EffectiveTier
        {
            get
            {
                EnsureInitialized();
                return effectiveTier;
            }
        }

        public static bool IsHighEffective => EffectiveTier == MobileGraphicsTier.High;

        static void EnsureInitialized()
        {
            if (initialized)
                return;

            int raw = PlayerPrefs.GetInt(PlayerPrefsKey, (int)GraphicsTierMode.Auto);
            mode = ClampMode(raw);
            initialized = true;
            RefreshEffectiveTier(invokeEvent: false);
            GraphicsSettings.UseExtraGraphicsChanged += OnExtraGraphicsChanged;
        }

        static GraphicsTierMode ClampMode(int raw)
        {
            if (raw == (int)GraphicsTierMode.Balanced)
                return GraphicsTierMode.Balanced;
            if (raw == (int)GraphicsTierMode.High)
                return GraphicsTierMode.High;
            return GraphicsTierMode.Auto;
        }

        static void OnExtraGraphicsChanged(bool _)
        {
            RefreshEffectiveTier(invokeEvent: true);
        }

        public static void SetMode(GraphicsTierMode newMode)
        {
            EnsureInitialized();
            if (mode == newMode)
                return;

            mode = newMode;
            PlayerPrefs.SetInt(PlayerPrefsKey, (int)mode);
            PlayerPrefs.Save();
            ModeChanged?.Invoke(mode);
            RefreshEffectiveTier(invokeEvent: true);
        }

        public static void RefreshEffectiveTier(bool invokeEvent)
        {
            EnsureInitialized();
            MobileGraphicsTier next = ComputeEffectiveTier();
            if (next == effectiveTier)
                return;

            effectiveTier = next;
            if (invokeEvent)
                EffectiveTierChanged?.Invoke(effectiveTier);
        }

        static MobileGraphicsTier ComputeEffectiveTier()
        {
            if (!GraphicsSettings.UseExtraGraphics)
                return MobileGraphicsTier.Balanced;

            switch (mode)
            {
                case GraphicsTierMode.High:
                    return MobileGraphicsTier.High;
                case GraphicsTierMode.Balanced:
                    return MobileGraphicsTier.Balanced;
                default:
                    return SystemInfo.graphicsMemorySize >= AutoHighMinGraphicsMemoryMb
                        ? MobileGraphicsTier.High
                        : MobileGraphicsTier.Balanced;
            }
        }

        public static void ResetToDefaults()
        {
            EnsureInitialized();
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            mode = GraphicsTierMode.Auto;
            PlayerPrefs.Save();
            ModeChanged?.Invoke(mode);
            RefreshEffectiveTier(invokeEvent: true);
        }
    }
}
