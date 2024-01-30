// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Configuration;

namespace osu.Game.Rulesets.Mods
{
    public abstract class ModClassic : Mod
    {
        public override string Name => "Classic";

        public override string Acronym => "CL";

        [SettingSource("Legacy hit windows", "Uses half-integer legacy hit windows.")]
        public Bindable<bool> LegacyHitWindows { get; } = new BindableBool(true);

        public override double ScoreMultiplier => 0.96;

        public override IconUsage? Icon => FontAwesome.Solid.History;

        public override LocalisableString Description => "Feeling nostalgic?";

        public override ModType Type => ModType.Conversion;
    }
}
