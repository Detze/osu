// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Replays;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Replays;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Replays;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osuTK;

namespace osu.Game.Tests.Visual
{
    public partial class TestSceneHitEarliestHitObject : RateAdjustedBeatmapTestScene
    {
        private readonly List<HitResult> correctResults = new List<HitResult> { HitResult.Meh, HitResult.Ok };
        private readonly List<HitResult> correctResultsTaiko = new List<HitResult> { HitResult.Ok, HitResult.Great };
        private readonly List<HitResult> correctResultsMania = new List<HitResult> { HitResult.Meh, HitResult.Good };

        private const int hit_objects_count = 2;
        private static readonly List<double> object_time = new List<double> { 1000.0, 1100.0 };
        private static readonly List<double> hit_offset = new List<double> { 119.0, 50.0 }; // 119 is lower than the Meh (or Ok on taiko) hit window value, so the first hit result should not be a Miss

        private static readonly Vector2 circle_position = Vector2.Zero;

        private readonly List<JudgementResult> judgementResults = new List<JudgementResult>();

        private ScoreAccessibleReplayPlayer currentPlayer = null!;

        [Test]
        public void TestOsu()
        {
            performOsuTest(8.0f);
        }

        [Test]
        public void TestTaiko()
        {
            performTaikoTest(0.0f);
        }

        [Test]
        public void TestMania()
        {
            performManiaTest(8.0f);
        }

        private static List<ReplayFrame> generateOsuFrames()
        {
            List<ReplayFrame> frames = new List<ReplayFrame>
            {
                new OsuReplayFrame(object_time[0] + hit_offset[0], circle_position, OsuAction.LeftButton)
            };
            frames.Add(new OsuReplayFrame(frames[^1].Time, ((OsuReplayFrame)frames[^1]).Position));
            frames.Add(new OsuReplayFrame(object_time[1] + hit_offset[1], circle_position, OsuAction.LeftButton));
            frames.Add(new OsuReplayFrame(frames[^1].Time, ((OsuReplayFrame)frames[^1]).Position));

            return frames;
        }

        private static List<ReplayFrame> generateTaikoFrames()
        {
            List<ReplayFrame> frames = new List<ReplayFrame>
            {
                new TaikoReplayFrame(object_time[0] + hit_offset[0], TaikoAction.LeftCentre)
            };
            frames.Add(new TaikoReplayFrame(frames[^1].Time));

            frames.Add(new TaikoReplayFrame(object_time[1] + hit_offset[1], TaikoAction.LeftCentre));
            frames.Add(new TaikoReplayFrame(frames[^1].Time));

            return frames;
        }

        private static List<ReplayFrame> generateManiaFrames()
        {
            List<ReplayFrame> frames = new List<ReplayFrame>
            {
                new ManiaReplayFrame(object_time[0] + hit_offset[0], ManiaAction.Key1)
            };
            frames.Add(new ManiaReplayFrame(frames[^1].Time));

            frames.Add(new ManiaReplayFrame(object_time[1] + hit_offset[1], ManiaAction.Key1));
            frames.Add(new ManiaReplayFrame(frames[^1].Time));

            return frames;
        }

        private static List<OsuHitObject> generateOsuHitObjects()
        {
            List<OsuHitObject> hitObjects = new List<OsuHitObject>();

            for (int i = 0; i < hit_objects_count; i++)
            {
                hitObjects.Add(new HitCircle
                {
                    StartTime = object_time[i],
                    Position = circle_position
                });
            }

            return hitObjects;
        }

        private static List<TaikoHitObject> generateTaikoHitObjects()
        {
            List<TaikoHitObject> hitObjects = new List<TaikoHitObject>();

            for (int i = 0; i < hit_objects_count; i++)
            {
                hitObjects.Add(new Hit
                {
                    StartTime = object_time[i]
                });
            }

            return hitObjects;
        }

        private static List<ManiaHitObject> generateManiaHitObjects()
        {
            List<ManiaHitObject> hitObjects = new List<ManiaHitObject>();

            for (int i = 0; i < hit_objects_count; i++)
            {
                hitObjects.Add(new Note
                {
                    StartTime = object_time[i]
                });
            }

            return hitObjects;
        }

        private void performOsuTest(float overallDifficulty)
        {
            AddStep("load player", () =>
            {
                Beatmap.Value = CreateWorkingBeatmap(new Beatmap<OsuHitObject>
                {
                    HitObjects = generateOsuHitObjects(),
                    BeatmapInfo =
                    {
                        Difficulty = new BeatmapDifficulty
                        {
                            OverallDifficulty = overallDifficulty
                        },
                        Ruleset = new OsuRuleset().RulesetInfo,
                        StackLeniency = 0.0001f
                    }
                });

                var p = new ScoreAccessibleReplayPlayer(new Score { Replay = new Replay { Frames = generateOsuFrames() } });

                p.OnLoadComplete += _ =>
                {
                    p.ScoreProcessor.NewJudgement += result =>
                    {
                        if (currentPlayer == p) judgementResults.Add(result);
                    };
                };

                LoadScreen(currentPlayer = p);
                judgementResults.Clear();
            });
            AddUntilStep("Beatmap at 0", () => Beatmap.Value.Track.CurrentTime == 0);
            AddUntilStep("Wait until player is loaded", () => currentPlayer.IsCurrentScreen());
            AddUntilStep("Wait for completion", () => currentPlayer.ScoreProcessor.HasCompleted.Value);

            for (int i = 0; i < hit_objects_count; i++) assertJudgement(i);
        }

        private void performTaikoTest(float overallDifficulty)
        {
            AddStep("load player", () =>
            {
                Beatmap.Value = CreateWorkingBeatmap(new Beatmap<TaikoHitObject>
                {
                    HitObjects = generateTaikoHitObjects(),
                    BeatmapInfo =
                    {
                        Difficulty = new BeatmapDifficulty
                        {
                            OverallDifficulty = overallDifficulty
                        },
                        Ruleset = new TaikoRuleset().RulesetInfo,
                        StackLeniency = 0.0001f
                    }
                });
                var p = new ScoreAccessibleReplayPlayer(new Score { Replay = new Replay { Frames = generateTaikoFrames() } });

                p.OnLoadComplete += _ =>
                {
                    p.ScoreProcessor.NewJudgement += result =>
                    {
                        if (currentPlayer == p) judgementResults.Add(result);
                    };
                };
                LoadScreen(currentPlayer = p);
                judgementResults.Clear();
            });
            AddUntilStep("Beatmap at 0", () => Beatmap.Value.Track.CurrentTime == 0);
            AddUntilStep("Wait until player is loaded", () => currentPlayer.IsCurrentScreen());
            AddUntilStep("Wait for completion", () => currentPlayer.ScoreProcessor.HasCompleted.Value);

            for (int i = 0; i < hit_objects_count; i++) assertJudgementTaiko(i);
        }

        private void performManiaTest(float overallDifficulty)
        {
            AddStep("load player", () =>
            {
                Beatmap.Value = CreateWorkingBeatmap(new Beatmap<ManiaHitObject>
                {
                    HitObjects = generateManiaHitObjects(),
                    BeatmapInfo =
                    {
                        Difficulty = new BeatmapDifficulty
                        {
                            OverallDifficulty = overallDifficulty
                        },
                        Ruleset = new ManiaRuleset().RulesetInfo,
                        StackLeniency = 0.0001f
                    }
                });

                var p = new ScoreAccessibleReplayPlayer(new Score { Replay = new Replay { Frames = generateManiaFrames() } });

                p.OnLoadComplete += _ =>
                {
                    p.ScoreProcessor.NewJudgement += result =>
                    {
                        if (currentPlayer == p) judgementResults.Add(result);
                    };
                };

                LoadScreen(currentPlayer = p);
                judgementResults.Clear();
            });
            AddUntilStep("Beatmap at 0", () => Beatmap.Value.Track.CurrentTime == 0);
            AddUntilStep("Wait until player is loaded", () => currentPlayer.IsCurrentScreen());
            AddUntilStep("Wait for completion", () => currentPlayer.ScoreProcessor.HasCompleted.Value);

            for (int i = 0; i < hit_objects_count; i++) assertJudgementMania(i);
        }

        private void assertJudgement(int i)
        {
            AddAssert(
                $"check judgement no. {i}",
                () => judgementResults[i].Type,
                () => Is.EqualTo(correctResults[i]));
        }

        private void assertJudgementTaiko(int i)
        {
            AddAssert(
                $"check judgement no. {i}",
                () => judgementResults[i].Type,
                () => Is.EqualTo(correctResultsTaiko[i]));
        }

        private void assertJudgementMania(int i)
        {
            AddAssert(
                $"check judgement no. {i}",
                () => judgementResults[i].Type,
                () => Is.EqualTo(correctResultsMania[i]));
        }

        private partial class ScoreAccessibleReplayPlayer : ReplayPlayer
        {
            public new ScoreProcessor ScoreProcessor => base.ScoreProcessor;

            protected override bool PauseOnFocusLost => false;

            public ScoreAccessibleReplayPlayer(Score score)
                : base(score, new PlayerConfiguration
                {
                    AllowPause = false,
                    ShowResults = false
                })
            {
            }
        }
    }
}
