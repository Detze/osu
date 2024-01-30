// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Mania.Scoring
{
    public class ManiaHitWindows : HitWindows
    {
        internal static readonly DifficultyRange[] MANIA_RANGES =
        {
            new DifficultyRange(HitResult.Perfect, 16, 16, 16),
            new DifficultyRange(HitResult.Great, 64, 49, 34),
            new DifficultyRange(HitResult.Good, 97, 82, 67),
            new DifficultyRange(HitResult.Ok, 127, 112, 97),
            new DifficultyRange(HitResult.Meh, 151, 136, 121),
            new DifficultyRange(HitResult.Miss, 188, 173, 158),
            // new DifficultyRange(HitResult.Great, 34, 34, 34),
            // new DifficultyRange(HitResult.Good, 67, 67, 67),
            // new DifficultyRange(HitResult.Ok, 97, 97, 97),
            // new DifficultyRange(HitResult.Meh, 121, 121, 121),
            // new DifficultyRange(HitResult.Miss, 158, 158, 158),
        };

        protected override bool LegacyIsInclusive => true;
        // protected override bool SpeedRateImmutable => true;

        public override bool IsHitResultAllowed(HitResult result)
        {
            switch (result)
            {
                case HitResult.Perfect:
                case HitResult.Great:
                case HitResult.Good:
                case HitResult.Ok:
                case HitResult.Meh:
                case HitResult.Miss:
                    return true;
            }

            return false;
        }

        public override bool IsHitResultAllowedLate(HitResult result)
        {
            switch (result)
            {
                case HitResult.Perfect:
                case HitResult.Great:
                case HitResult.Good:
                case HitResult.Ok:
                // case HitResult.Meh:
                // case HitResult.Miss: // ?!
                    return true;
            }

            return false;
        }

        // protected override DifficultyRange[] GetRanges() => MANIA_RANGES.Select(r =>
        //     new DifficultyRange(
        //         r.Result,
        //         r.Min * multiplier,
        //         r.Average * multiplier,
        //         r.Max * multiplier)).ToArray();

        protected override DifficultyRange[] GetRanges() => MANIA_RANGES;

        // this is going to get moved to ManiaModClassic (maybe to LegacyManiaHitWindows)
        protected override DifficultyRange[] GetRangesLate()
        {
            // DifficultyRange[] ranges = GetRanges(); // do I need to do a clone here? maybe I am accidentally modifying early Ok here, not sure...
            DifficultyRange[] ranges = (DifficultyRange[])MANIA_RANGES.Clone();

            // ranges[3] = new DifficultyRange(HitResult.Ok, ranges[3].Min * multiplier - 1.0, ranges[3].Average * multiplier - 1.0, ranges[3].Max * multiplier - 1.0); // on stable, the late Good check is exclusive, unlike all the other checks; it is also important to subtract 1.0 *after* the speed rate multiplication
            // ^ mistakenly doing .Good instead of .Ok here has interesting results...
            // ranges[3] = new DifficultyRange(HitResult.Ok, ranges[3].Min, ranges[3].Average, ranges[3].Max, true);
            // ^ to-do this line, later with HitWindowRange adjustment

            return ranges;
        }
    }
}
