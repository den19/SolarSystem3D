using System;
using UnityEngine;

namespace SolarSystemApp
{
    public enum ProbeModelKind
    {
        Voyager = 0,
        NewHorizons = 1,
        Juno = 2,
        Custom = 3,
        Luna1 = 4,
        Venera7 = 5,
        Luna16 = 6,
        Mars3 = 7,
        Change4 = 8,
        Tianwen1 = 9,
        Change5 = 10,
        Hayabusa2 = 11,
        Akatsuki = 12,
        Chandrayaan3 = 13,
    }

    public enum ProbeCameraMode
    {
        World = 0,
        Chase = 1,
        Cockpit = 2
    }

    /// <summary>
    /// Persisted probe / slingshot mode. Default off — enable via side panel.
    /// </summary>
    public static class ProbeSettings
    {
        const string KeyUse = "SolarSystem_UseProbe";
        const string KeyModel = "SolarSystem_ProbeModel";
        const string KeyAntenna = "SolarSystem_ProbeCustomAntenna";
        const string KeyEngine = "SolarSystem_ProbeCustomEngine";
        const string KeyShield = "SolarSystem_ProbeCustomShield";
        const string KeyViews = "SolarSystem_ShowProbeViews";
        const string KeyCamera = "SolarSystem_ProbeCameraMode";
        const string KeyKeepChaseInspectAngle = "SolarSystem_KeepChaseInspectAngle";

        static bool initialized;
        static bool useProbe;
        static ProbeModelKind model;
        static bool customAntenna = true;
        static bool customEngine;
        static bool customShield;
        static bool showProbeViews;
        static ProbeCameraMode cameraMode;
        static bool keepChaseInspectAngle = true;

        public static event Action<bool> UseProbeChanged;
        public static event Action ShowProbeViewsChanged;
        public static event Action LoadoutChanged;
        public static event Action<bool> KeepChaseInspectAngleChanged;

        public static bool UseProbe
        {
            get
            {
                EnsureInitialized();
                return useProbe;
            }
        }

        public static ProbeModelKind Model
        {
            get
            {
                EnsureInitialized();
                return model;
            }
        }

        public static bool CustomAntenna
        {
            get
            {
                EnsureInitialized();
                return customAntenna;
            }
        }

        public static bool CustomEngine
        {
            get
            {
                EnsureInitialized();
                return customEngine;
            }
        }

        public static bool CustomShield
        {
            get
            {
                EnsureInitialized();
                return customShield;
            }
        }

        public static bool ShowProbeViews
        {
            get
            {
                EnsureInitialized();
                return showProbeViews;
            }
        }

        public static ProbeCameraMode CameraMode
        {
            get
            {
                EnsureInitialized();
                return cameraMode;
            }
        }

        public static bool KeepChaseInspectAngle
        {
            get
            {
                EnsureInitialized();
                return keepChaseInspectAngle;
            }
        }

        static void EnsureInitialized()
        {
            if (initialized)
                return;

            useProbe = PlayerPrefs.GetInt(KeyUse, 0) != 0;
            model = ClampModel(PlayerPrefs.GetInt(KeyModel, 0));
            customAntenna = PlayerPrefs.GetInt(KeyAntenna, 1) != 0;
            customEngine = PlayerPrefs.GetInt(KeyEngine, 0) != 0;
            customShield = PlayerPrefs.GetInt(KeyShield, 0) != 0;
            showProbeViews = PlayerPrefs.GetInt(KeyViews, 0) != 0;
            cameraMode = ClampCamera(PlayerPrefs.GetInt(KeyCamera, 0));
            keepChaseInspectAngle = PlayerPrefs.GetInt(KeyKeepChaseInspectAngle, 1) != 0;
            initialized = true;
        }

        static ProbeModelKind ClampModel(int raw)
        {
            if (raw < 0 || raw > (int)ProbeModelKind.Chandrayaan3)
                return ProbeModelCatalog.GetDefaultPickerModel();
            return ProbeModelCatalog.ClampToPicker((ProbeModelKind)raw);
        }

        static ProbeCameraMode ClampCamera(int raw)
        {
            if (raw < 0 || raw > (int)ProbeCameraMode.Cockpit)
                return ProbeCameraMode.World;
            return (ProbeCameraMode)raw;
        }

        public static void SetUseProbe(bool enabled)
        {
            EnsureInitialized();
            if (useProbe == enabled)
                return;

            useProbe = enabled;
            PlayerPrefs.SetInt(KeyUse, useProbe ? 1 : 0);
            PlayerPrefs.Save();
            UseProbeChanged?.Invoke(useProbe);
        }

        public static void SetModel(ProbeModelKind kind)
        {
            EnsureInitialized();
            kind = ProbeModelCatalog.ClampToPicker(kind);
            if (model == kind)
                return;

            model = kind;
            PlayerPrefs.SetInt(KeyModel, (int)model);
            PlayerPrefs.Save();
            LoadoutChanged?.Invoke();
        }

        public static void SetCustomAntenna(bool enabled)
        {
            EnsureInitialized();
            if (customAntenna == enabled)
                return;

            customAntenna = enabled;
            PlayerPrefs.SetInt(KeyAntenna, customAntenna ? 1 : 0);
            PlayerPrefs.Save();
            LoadoutChanged?.Invoke();
        }

        public static void SetCustomEngine(bool enabled)
        {
            EnsureInitialized();
            if (customEngine == enabled)
                return;

            customEngine = enabled;
            PlayerPrefs.SetInt(KeyEngine, customEngine ? 1 : 0);
            PlayerPrefs.Save();
            LoadoutChanged?.Invoke();
        }

        public static void SetCustomShield(bool enabled)
        {
            EnsureInitialized();
            if (customShield == enabled)
                return;

            customShield = enabled;
            PlayerPrefs.SetInt(KeyShield, customShield ? 1 : 0);
            PlayerPrefs.Save();
            LoadoutChanged?.Invoke();
        }

        public static void SetShowProbeViews(bool enabled)
        {
            EnsureInitialized();
            if (showProbeViews == enabled)
                return;

            showProbeViews = enabled;
            PlayerPrefs.SetInt(KeyViews, showProbeViews ? 1 : 0);
            PlayerPrefs.Save();
            ShowProbeViewsChanged?.Invoke();
        }

        public static void SetCameraMode(ProbeCameraMode mode, bool force = false)
        {
            EnsureInitialized();
            if (!force && cameraMode == mode)
                return;

            cameraMode = mode;
            PlayerPrefs.SetInt(KeyCamera, (int)cameraMode);
            PlayerPrefs.Save();
        }

        public static void SetKeepChaseInspectAngle(bool enabled)
        {
            EnsureInitialized();
            if (keepChaseInspectAngle == enabled)
                return;

            keepChaseInspectAngle = enabled;
            PlayerPrefs.SetInt(KeyKeepChaseInspectAngle, keepChaseInspectAngle ? 1 : 0);
            PlayerPrefs.Save();
            KeepChaseInspectAngleChanged?.Invoke(keepChaseInspectAngle);
        }

        public static bool ResolveHasAntenna()
        {
            EnsureInitialized();
            if (model == ProbeModelKind.Custom)
                return customAntenna;
            return true;
        }

        public static bool ResolveHasEngine()
        {
            EnsureInitialized();
            return model == ProbeModelKind.Custom && customEngine;
        }

        public static bool ResolveHasShield()
        {
            EnsureInitialized();
            return model == ProbeModelKind.Custom && customShield;
        }

        public static float ResolveLaunchSpeedScale()
        {
            EnsureInitialized();
            return model == ProbeModelKind.NewHorizons ? 1.35f : 1f;
        }

        public static void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(KeyUse);
            PlayerPrefs.DeleteKey(KeyModel);
            PlayerPrefs.DeleteKey(KeyAntenna);
            PlayerPrefs.DeleteKey(KeyEngine);
            PlayerPrefs.DeleteKey(KeyShield);
            PlayerPrefs.DeleteKey(KeyViews);
            PlayerPrefs.DeleteKey(KeyCamera);
            PlayerPrefs.DeleteKey(KeyKeepChaseInspectAngle);
            ProbeCoachSettings.ResetToDefaults();

            initialized = true;
            useProbe = false;
            model = ProbeModelKind.Voyager;
            customAntenna = true;
            customEngine = false;
            customShield = false;
            showProbeViews = false;
            cameraMode = ProbeCameraMode.World;
            keepChaseInspectAngle = true;

            PlayerPrefs.Save();
            UseProbeChanged?.Invoke(useProbe);
            ShowProbeViewsChanged?.Invoke();
            LoadoutChanged?.Invoke();
            KeepChaseInspectAngleChanged?.Invoke(keepChaseInspectAngle);
        }
    }
}
