using System;
using UnityEngine;

/// <summary>
/// Central simulation time control: pause/resume and speed multiplier via Time.timeScale.
/// Speed is stored separately from pause so resume restores the previous multiplier.
/// </summary>
public static class SimulationTimeController
{
    public const float MinSpeedMultiplier = 0.25f;
    public const float MaxSpeedMultiplier = 256f;
    public const float DefaultSpeedMultiplier = 1f;

    static float _speedMultiplier = DefaultSpeedMultiplier;
    static bool _isPaused;

    public static event Action<float> SpeedMultiplierChanged;
    public static event Action<bool> IsPausedChanged;

    public static float SpeedMultiplier
    {
        get => _speedMultiplier;
        set => SetSpeedMultiplier(value);
    }

    public static bool IsPaused
    {
        get => _isPaused;
        set => SetPaused(value);
    }

    public static void SetSpeedMultiplier(float multiplier, bool apply = true)
    {
        float clamped = ClampSpeed(multiplier);
        if (Mathf.Approximately(_speedMultiplier, clamped))
            return;

        _speedMultiplier = clamped;
        SpeedMultiplierChanged?.Invoke(_speedMultiplier);

        if (apply && !_isPaused)
            Apply();
    }

    public static void SetPaused(bool paused, bool apply = true)
    {
        if (_isPaused == paused)
            return;

        _isPaused = paused;
        IsPausedChanged?.Invoke(_isPaused);

        if (apply)
            Apply();
    }

    public static void TogglePause()
    {
        SetPaused(!_isPaused);
    }

    public static void StepSpeedDown()
    {
        SetSpeedMultiplier(_speedMultiplier * 0.5f);
    }

    public static void StepSpeedUp()
    {
        SetSpeedMultiplier(_speedMultiplier * 2f);
    }

    public static void Apply()
    {
        Time.timeScale = _isPaused ? 0f : _speedMultiplier;
    }

    public static void ResetToDefaults(bool apply = true)
    {
        _speedMultiplier = DefaultSpeedMultiplier;
        _isPaused = false;
        SpeedMultiplierChanged?.Invoke(_speedMultiplier);
        IsPausedChanged?.Invoke(_isPaused);

        if (apply)
            Apply();
    }

    public static void RestoreState(float speedMultiplier, bool isPaused, bool apply = true)
    {
        _speedMultiplier = ClampSpeed(speedMultiplier);
        _isPaused = isPaused;
        SpeedMultiplierChanged?.Invoke(_speedMultiplier);
        IsPausedChanged?.Invoke(_isPaused);

        if (apply)
            Apply();
    }

    public static float ClampSpeed(float multiplier)
    {
        if (multiplier <= 0f || float.IsNaN(multiplier) || float.IsInfinity(multiplier))
            return DefaultSpeedMultiplier;

        return Mathf.Clamp(multiplier, MinSpeedMultiplier, MaxSpeedMultiplier);
    }

    public static string FormatSpeedLabel(float multiplier)
    {
        if (multiplier >= 1f)
        {
            float rounded = Mathf.Round(multiplier);
            if (Mathf.Approximately(multiplier, rounded))
                return rounded.ToString("0");
        }

        if (Mathf.Approximately(multiplier, 0.25f))
            return "0.25";

        if (Mathf.Approximately(multiplier, 0.5f))
            return "0.5";

        return multiplier.ToString("0.##");
    }
}
