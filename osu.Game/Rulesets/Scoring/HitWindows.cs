// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Scoring
{
    /// <summary>
    /// A structure containing timing data for hit window based gameplay.
    /// </summary>
    public class HitWindows
    {
        private HitHalfWindows early_hit_windows;
        private HitHalfWindows late_hit_windows;

        private static readonly DifficultyRange[] base_ranges =
        {
            new DifficultyRange(HitResult.Perfect, 40, 30, 20),
            new DifficultyRange(HitResult.Great, 60, 45, 30),
            new DifficultyRange(HitResult.Good, 120, 90, 60),
            new DifficultyRange(HitResult.Ok, 180, 135, 90),
            new DifficultyRange(HitResult.Meh, 240, 180, 120),
            new DifficultyRange(HitResult.Miss, 300, 225, 150),
        };

        /// <summary>
        /// An empty <see cref="HitWindows"/> with only <see cref="HitResult.Miss"/> and <see cref="HitResult.Perfect"/>.
        /// No time values are provided (meaning instantaneous hit or miss).
        /// </summary>
        public static HitWindows Empty => new EmptyHitWindows();

        public HitWindows()
        {
            // why is miss condition needed? also, maybe refactor to separate method, and do for both early and late? (although my late mania doesn't have miss..)
            Debug.Assert(GetRanges().Any(r => r.Result == HitResult.Miss), $"{nameof(GetRanges)} should always contain {nameof(HitResult.Miss)}");
            Debug.Assert(GetRanges().Any(r => r.Result != HitResult.Miss), $"{nameof(GetRanges)} should always contain at least one result type other than {nameof(HitResult.Miss)}.");

            early_hit_windows = new HitHalfWindows(GetRangesEarly(), false, LegacyIsInclusive, IsHitResultAllowedEarly);
            late_hit_windows = new HitHalfWindows(GetRangesLate(), false, LegacyIsInclusive, IsHitResultAllowedLate);
        }

        private class HitHalfWindows
        {
            private DifficultyRange[] ranges;
            public bool IsLegacy;
            private bool legacy_is_inclusive;
            private Func<HitResult, bool> is_hit_result_allowed;
            public double SpeedRate { get; set; } // for optional use for speed rate immutability

            private double perfect;
            private double great;
            private double good;
            private double ok;
            private double meh;
            private double miss;

            public HitHalfWindows(DifficultyRange[] difficultyRanges, bool isLegacy, bool legacyIsInclusive, Func<HitResult, bool> isHitResultAllowed)
            {
                ranges = difficultyRanges;
                IsLegacy = isLegacy;
                legacy_is_inclusive = legacyIsInclusive;
                is_hit_result_allowed = isHitResultAllowed;
                SpeedRate = 1.0;
            }

            /// <summary>
            /// Sets hit windows with values that correspond to a difficulty parameter.
            /// </summary>
            /// <param name="difficulty">The parameter.</param>
            public void SetDifficulty(double difficulty)
            {
                foreach (var range in ranges)
                {
                    double value = HitWindowValueFor(difficulty, range);

                    switch (range.Result)
                    {
                        case HitResult.Miss:
                            miss = value;
                            break;

                        case HitResult.Meh:
                            meh = value;
                            break;

                        case HitResult.Ok:
                            ok = value;
                            break;

                        case HitResult.Good:
                            good = value;
                            break;

                        case HitResult.Great:
                            great = value;
                            break;

                        case HitResult.Perfect:
                            perfect = value;
                            break;
                    }
                }
            }

            /// <summary>
            /// Calculates the difficulty value to which the difficulty parameter maps in the difficulty range.
            /// </summary>
            /// <param name="difficulty">The difficulty parameter.</param>
            /// <param name="range">The range of difficulty values.</param>
            /// <returns>Value to which the difficulty parameter maps in the specified range.</returns>
            protected virtual double HitWindowValueFor(double difficulty, DifficultyRange range)
            {
                double value = IBeatmapDifficultyInfo.DifficultyRange(difficulty, (range.Min, range.Average, range.Max));

                value *= SpeedRate;

                if (IsLegacy)
                {
                    value = Math.Floor(value) - 0.5; // represents the "true" hit windows in osu!stable; osu!stable rounded input times to integers (which is equivalent to the 0.5 ms shift here), and hit windows were floored

                    if (legacy_is_inclusive)
                    {
                        value += 1.0; // abs(round(hit_error)) <= floor(hit_window) is equivalent to abs(round(hit_error)) < floor(hit_window) + 1 = floor(hit_window + 1), because both sides of the inequality are integers; therefore, inclusive legacy hit windows are equivalent to 1 ms wider exclusive legacy hit windows
                    }
                }

                return value;
            }

            /// <summary>
            /// Retrieves the <see cref="HitResult"/> with the largest hit window that produces a successful hit.
            /// </summary>
            /// <returns>The lowest allowed successful <see cref="HitResult"/>.</returns>
            protected HitResult LowestSuccessfulHitResult()
            {
                for (var result = HitResult.Meh; result <= HitResult.Perfect; ++result)
                {
                    if (is_hit_result_allowed(result))
                        return result;
                }

                return HitResult.None;
            }


            public bool CanBeHit(double timeOffset) => contains(timeOffset, LowestSuccessfulHitResult());

            public HitResult ResultFor(double timeOffset)
            {
                for (var result = HitResult.Perfect; result >= HitResult.Miss; --result)
                {
                    if (is_hit_result_allowed(result) && contains(timeOffset, result))
                    {
                        return result;
                    }
                }

                return HitResult.None;
            }

            /// <summary>
            /// Given a time offset and a hit result, checks whether the time offset is contained within the hit window of the hit result.
            /// </summary>
            /// <param name="timeOffset">The time offset.</param>
            /// <param name="result">The <see cref="HitResult"/>.</param>
            /// <returns>Whether the time offset is contained within the hit window of the hit result.</returns>
            private bool contains(double timeOffset, HitResult result)
            {
                return timeOffset <= WindowFor(result);
            }

            public double WindowFor(HitResult result)
            {
                switch (result)
                {
                    case HitResult.Perfect:
                        return perfect;

                    case HitResult.Great:
                        return great;

                    case HitResult.Good:
                        return good;

                    case HitResult.Ok:
                        return ok;

                    case HitResult.Meh:
                        return meh;

                    case HitResult.Miss:
                        return miss;

                    default:
                        throw new ArgumentException("Unknown enum member", nameof(result));
                }
            }
        }

        /// <summary>
        /// Retrieves a mapping of <see cref="HitResult"/>s to their timing windows for all allowed <see cref="HitResult"/>s.
        /// </summary>
        public IEnumerable<(HitResult result, double length)> GetAllAvailableWindows()
        {
            for (var result = HitResult.Meh; result <= HitResult.Perfect; ++result)
            {
                if (IsHitResultAllowed(result))
                    yield return (result, WindowFor(result));
            }
        }
        // to-do: rewrite this and BarHitErrorMeter to support asymmetrical hit windows

        /// <summary>
        /// Check whether it is possible to achieve the provided <see cref="HitResult"/>.
        /// </summary>
        /// <param name="result">The result type to check.</param>
        /// <returns>Whether the <see cref="HitResult"/> can be achieved.</returns>
        public virtual bool IsHitResultAllowed(HitResult result) => true; // Early, to not have to rewrite everything for now
        public virtual bool IsHitResultAllowedEarly(HitResult result) => IsHitResultAllowed(result);
        public virtual bool IsHitResultAllowedLate(HitResult result) => IsHitResultAllowed(result);

        /// <summary>
        /// Sets hit windows with values that correspond to a difficulty parameter.
        /// </summary>
        /// <param name="difficulty">The parameter.</param>
        public void SetDifficulty(double difficulty)
        {
            early_hit_windows.SetDifficulty(difficulty);
            late_hit_windows.SetDifficulty(difficulty);
        }

        /// <summary>
        /// Retrieves the <see cref="HitResult"/> for a time offset.
        /// </summary>
        /// <param name="timeOffset">The time offset.</param>
        /// <returns>The hit result, or <see cref="HitResult.None"/> if <paramref name="timeOffset"/> doesn't result in a judgement.</returns>
        public HitResult ResultFor(double timeOffset)
        {
            if (double.IsNegative(timeOffset))
            {
                timeOffset = Math.Abs(timeOffset); // important to keep for negative offset!

                return early_hit_windows.ResultFor(timeOffset);
            }

            return late_hit_windows.ResultFor(timeOffset);
        }

        // to caller: be careful with this method..
        public double WindowFor(HitResult result) => EarlyWindowFor(result);

        /// <summary>
        /// Retrieves the early hit window value for a <see cref="HitResult"/>.
        /// This is the number of negative milliseconds allowed for the requested result.
        /// </summary>
        /// <param name="result">The expected <see cref="HitResult"/>.</param>
        /// <returns>Early half of the hit window for <paramref name="result"/>.</returns>
        public double EarlyWindowFor(HitResult result) => early_hit_windows.WindowFor(result);

        /// <summary>
        /// Retrieves the late hit window value for a <see cref="HitResult"/>.
        /// This is the number of non-negative milliseconds allowed for the requested result.
        /// </summary>
        /// <param name="result">The expected <see cref="HitResult"/>.</param>
        /// <returns>Late half of the hit window for <paramref name="result"/>.</returns>
        public double LateWindowFor(HitResult result) => late_hit_windows.WindowFor(result);

        /// <summary>
        /// Given a time offset, whether the <see cref="HitObject"/> can ever be hit in the future with a non-<see cref="HitResult.Miss"/> result.
        /// This happens if <paramref name="timeOffset"/> is less than what is required for the lowest successful late hit result.
        /// </summary>
        /// <param name="timeOffset">The time offset.</param>
        /// <returns>Whether the <see cref="HitObject"/> can be hit at any point in the future from this time offset.</returns>
        public bool CanBeHit(double timeOffset) => late_hit_windows.CanBeHit(timeOffset); // only late!

        /// <summary>
        /// Retrieve a valid list of <see cref="DifficultyRange"/>s representing hit windows.
        /// Defaults are provided but can be overridden to customise for a ruleset.
        /// </summary>
        //// protected virtual DifficultyRange[] GetRanges() => base_ranges;
        protected virtual DifficultyRange[] GetRanges() => base_ranges; // symmetric should only call this one, not two others imo
        protected virtual DifficultyRange[] GetRangesEarly() => GetRanges();
        protected virtual DifficultyRange[] GetRangesLate() => GetRanges();

        // protected virtual bool IsLegacy => false;
        protected virtual bool LegacyIsInclusive => false; // xmldocs todo
        // protected virtual bool SpeedRateImmutable => false;

        // you should do SetDifficulty after doing these too! I think I'll append that to this method later because HHWs are in a bad state after this otherwise
        public void SetLegacy(bool is_legacy)
        {
            early_hit_windows.IsLegacy = is_legacy;
            late_hit_windows.IsLegacy = is_legacy;
        }

        public void SetSpeedRate(double value)
        {
            early_hit_windows.SpeedRate = value;
            late_hit_windows.SpeedRate = value;
        }

        public class EmptyHitWindows : HitWindows
        {
            private static readonly DifficultyRange[] ranges =
            {
                new DifficultyRange(HitResult.Perfect, 0, 0, 0),
                new DifficultyRange(HitResult.Miss, 0, 0, 0),
            };

            public override bool IsHitResultAllowed(HitResult result)
            {
                switch (result)
                {
                    case HitResult.Perfect:
                    case HitResult.Miss:
                        return true;
                }

                return false;
            }

            protected override DifficultyRange[] GetRanges() => ranges;
        }
    }

    public struct DifficultyRange
    {
        public readonly HitResult Result;

        public double Min;
        public double Average;
        public double Max;

        public DifficultyRange(HitResult result, double min, double average, double max)
        {
            Result = result;

            Min = min;
            Average = average;
            Max = max;
        }
    }
}
