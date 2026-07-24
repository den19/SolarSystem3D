using System;
using UnityEngine;

namespace SolarSystemApp
{
    /// <summary>
    /// Simulation calendar for Time Machine: sweep from -100000 to +100000, then snap to UTC+3 now.
    /// Advanced by TimeMachineController with unscaledDeltaTime × SpeedMultiplier.
    /// After year-0 / current-date milestones, sweep base rate drops from Phase A (50 yr/s) to Phase B (1 yr/s).
    /// </summary>
    public static class SimulationClock
    {
        public enum ClockPhase
        {
            Sweep,
            Live
        }

        public const double SweepStartYear = -100000.0;
        public const double SweepEndYear = 100000.0;
        public const double PhaseAYearsPerSecond = 50.0;
        public const double PhaseBYearsPerSecond = 1.0;
        public const double DaysPerYear = 365.25;
        public const double J2000Year = 2000.0;

        static readonly TimeSpan UtcPlus3 = TimeSpan.FromHours(3);

        static double _yearContinuous = SweepStartYear;
        static ClockPhase _phase = ClockPhase.Sweep;
        static bool _drivesMotion;
        static bool _active;
        static string _lastFormattedDate;
        static bool _crossedYearZero;
        static bool _crossedCurrentDate;
        static double _currentDateThreshold;
        /// <summary>After a speed-reset milestone, Sweep uses Phase B (1 yr/s) instead of Phase A (50 yr/s).</summary>
        static bool _useSlowSweepRate;

        public static event Action DateChanged;

        /// <summary>Fired once when the calendar crosses year 0 or the real current date (UTC+3).</summary>
        public static event Action SpeedResetMilestoneReached;

        public static bool IsActive => _active;

        public static bool DrivesMotion => _drivesMotion && _active;

        public static ClockPhase Phase => _phase;

        /// <summary>Astronomical continuous year (e.g. -100000.0, 2000.5).</summary>
        public static double YearContinuous => _yearContinuous;

        /// <summary>Days since J2000.0 for Kepler mean motion.</summary>
        public static double DaysSinceJ2000 => (_yearContinuous - J2000Year) * DaysPerYear;

        public static void SetActive(bool active)
        {
            _active = active;
            _drivesMotion = active;
            if (!active)
                return;

            NotifyDateChangedIfNeeded(force: true);
        }

        public static void ResetToSweepStart()
        {
            _yearContinuous = SweepStartYear;
            _phase = ClockPhase.Sweep;
            ResetSpeedMilestones();
            NotifyDateChangedIfNeeded(force: true);
        }

        public static void SnapToUtcPlus3Now()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow.ToOffset(UtcPlus3);
            _yearContinuous = DateTimeOffsetToContinuousYear(now);
            _phase = ClockPhase.Live;
            // Snap lands on "now" — treat current-date milestone as already consumed.
            _crossedCurrentDate = true;
            _useSlowSweepRate = true;
            NotifyDateChangedIfNeeded(force: true);
        }

        /// <returns>True if a speed-reset milestone was crossed this tick.</returns>
        public static bool Tick(float deltaTime)
        {
            if (!_active || deltaTime <= 0f)
                return false;

            double yearsPerSecond = GetYearsPerSecond();

            double previousYear = _yearContinuous;
            double proposedYear = previousYear + yearsPerSecond * deltaTime;

            // Stop exactly on the earliest speed-reset milestone in this step so the
            // remainder of a high-speed jump does not keep racing after 1× is forced.
            if (TryGetNextSpeedMilestone(previousYear, out double milestoneYear))
            {
                if (proposedYear >= milestoneYear)
                {
                    _yearContinuous = milestoneYear;
                    MarkMilestoneReached(milestoneYear);
                    // Panel → 1× and drop Sweep base rate 50 yr/s → 1 yr/s for the rest of the sweep.
                    _useSlowSweepRate = true;
                    SimulationTimeController.ForceSpeedOneX();
                    SpeedResetMilestoneReached?.Invoke();
                    NotifyDateChangedIfNeeded(force: false);
                    return true;
                }
            }

            _yearContinuous = proposedYear;

            if (_phase == ClockPhase.Sweep && _yearContinuous >= SweepEndYear)
            {
                SnapToUtcPlus3Now();
                return false;
            }

            NotifyDateChangedIfNeeded(force: false);
            return false;
        }

        public static void ResetSpeedMilestones()
        {
            _crossedYearZero = false;
            _crossedCurrentDate = false;
            _useSlowSweepRate = false;
            DateTimeOffset now = DateTimeOffset.UtcNow.ToOffset(UtcPlus3);
            // Full continuous year of "today" UTC+3 (matches HUD yyyy-MM-dd), not Jan 1.
            _currentDateThreshold = DateTimeOffsetToContinuousYear(now);
        }

        public struct State
        {
            public double YearContinuous;
            public ClockPhase Phase;
            public bool CrossedYearZero;
            public bool CrossedCurrentDate;
            public bool UseSlowSweepRate;
            public double CurrentDateThreshold;
        }

        public static State CaptureState()
        {
            // Ensure threshold is valid even if milestones were never reset this session.
            if (_currentDateThreshold <= 0.0 && !_crossedCurrentDate)
            {
                DateTimeOffset now = DateTimeOffset.UtcNow.ToOffset(UtcPlus3);
                _currentDateThreshold = DateTimeOffsetToContinuousYear(now);
            }

            return new State
            {
                YearContinuous = _yearContinuous,
                Phase = _phase,
                CrossedYearZero = _crossedYearZero,
                CrossedCurrentDate = _crossedCurrentDate,
                UseSlowSweepRate = _useSlowSweepRate,
                CurrentDateThreshold = _currentDateThreshold
            };
        }

        public static void RestoreState(State state)
        {
            _yearContinuous = state.YearContinuous;
            _phase = state.Phase;
            _crossedYearZero = state.CrossedYearZero;
            _crossedCurrentDate = state.CrossedCurrentDate;
            _useSlowSweepRate = state.UseSlowSweepRate;

            if (state.CurrentDateThreshold > 0.0)
            {
                _currentDateThreshold = state.CurrentDateThreshold;
            }
            else
            {
                DateTimeOffset now = DateTimeOffset.UtcNow.ToOffset(UtcPlus3);
                _currentDateThreshold = DateTimeOffsetToContinuousYear(now);
            }

            _lastFormattedDate = null;
            NotifyDateChangedIfNeeded(force: true);
        }

        static double GetYearsPerSecond()
        {
            if (_phase == ClockPhase.Live || _useSlowSweepRate)
                return PhaseBYearsPerSecond;

            return PhaseAYearsPerSecond;
        }

        static bool TryGetNextSpeedMilestone(double previousYear, out double milestoneYear)
        {
            milestoneYear = 0.0;
            bool found = false;

            if (!_crossedYearZero && previousYear < 0.0)
            {
                milestoneYear = 0.0;
                found = true;
            }

            if (!_crossedCurrentDate && previousYear < _currentDateThreshold)
            {
                if (!found || _currentDateThreshold < milestoneYear)
                    milestoneYear = _currentDateThreshold;
                found = true;
            }

            return found;
        }

        static void MarkMilestoneReached(double milestoneYear)
        {
            if (Math.Abs(milestoneYear) < 1e-9)
                _crossedYearZero = true;
            else if (milestoneYear >= _currentDateThreshold - 1e-9)
                _crossedCurrentDate = true;
        }

        public static string FormatDateYyyyMmDd()
        {
            ContinuousYearToYmd(_yearContinuous, out int year, out int month, out int day);
            if (year < 0)
                return string.Format("-{0:D5}-{1:D2}-{2:D2}", -year, month, day);

            if (year >= 10000)
                return string.Format("{0}-{1:D2}-{2:D2}", year, month, day);

            return string.Format("{0:D4}-{1:D2}-{2:D2}", year, month, day);
        }

        public static float EvaluateSunActivityScale()
        {
            // Monotonic growth: ~0.35 at -100k, 1.0 near year 2000, rising into the future.
            if (_yearContinuous <= 2000.0)
            {
                float t = Mathf.InverseLerp((float)SweepStartYear, 2000f, (float)_yearContinuous);
                return Mathf.Lerp(0.35f, 1f, t);
            }

            double yearsAhead = _yearContinuous - 2000.0;
            return 1f + (float)(yearsAhead / 50000.0);
        }

        static void NotifyDateChangedIfNeeded(bool force)
        {
            string formatted = FormatDateYyyyMmDd();
            if (!force && formatted == _lastFormattedDate)
                return;

            _lastFormattedDate = formatted;
            DateChanged?.Invoke();
        }

        static double DateTimeOffsetToContinuousYear(DateTimeOffset dto)
        {
            int year = dto.Year;
            double dayOfYear = dto.DayOfYear - 1 + dto.TimeOfDay.TotalDays;
            double yearLength = GetYearLengthDays(year);
            return year + dayOfYear / yearLength;
        }

        /// <summary>
        /// Inverse of DateTimeOffsetToContinuousYear: same 365/366 year length and month table
        /// so the current-date milestone matches the HUD yyyy-MM-dd.
        /// </summary>
        static void ContinuousYearToYmd(double yearContinuous, out int year, out int month, out int day)
        {
            year = (int)Math.Floor(yearContinuous);
            double frac = yearContinuous - year;
            double yearLength = GetYearLengthDays(year);
            int maxDay = (int)yearLength;
            int dayOfYear = Mathf.Clamp((int)(frac * yearLength) + 1, 1, maxDay);
            DayOfYearToMonthDay(dayOfYear, year, out month, out day);
        }

        static double GetYearLengthDays(int year)
        {
            return IsLeapYearProleptic(year) ? 366.0 : 365.0;
        }

        static bool IsLeapYearProleptic(int year)
        {
            if (year >= 1 && year <= 9999)
                return DateTime.IsLeapYear(year);

            // Proleptic Gregorian for year 0 and BCE (year 0 is leap).
            int y = year % 400;
            if (y < 0)
                y += 400;
            if (y % 4 != 0)
                return false;
            if (y % 100 != 0)
                return true;
            return y % 400 == 0;
        }

        static void DayOfYearToMonthDay(int dayOfYear, int year, out int month, out int day)
        {
            int feb = IsLeapYearProleptic(year) ? 29 : 28;
            int[] lengths = { 31, feb, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            int remaining = dayOfYear;
            for (int m = 0; m < 12; m++)
            {
                if (remaining <= lengths[m])
                {
                    month = m + 1;
                    day = remaining;
                    return;
                }

                remaining -= lengths[m];
            }

            month = 12;
            day = lengths[11];
        }
    }
}
