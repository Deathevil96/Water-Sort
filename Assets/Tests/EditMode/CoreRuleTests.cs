using System.Collections.Generic;
using NUnit.Framework;
using PaintSort.Core;

namespace PaintSort.Tests
{
    public class CoreRuleTests
    {
        // pots given bottom->top; ints are colours; negative = hidden colour (-(c+1))
        static LevelDefinition Make(params int[][] pots)
        {
            var l = new LevelDefinition { LevelId = 1 };
            foreach (var p in pots)
            {
                var c = new ContainerDef();
                foreach (var v in p) { c.Colors.Add(v < 0 ? -v - 1 : v); c.Hidden.Add(v < 0); }
                l.Containers.Add(c);
            }
            return l;
        }

        [Test] public void EmptySourceCannotPour() { var s = new PuzzleState(Make(new int[0], new int[0])); Assert.AreEqual(MoveFailure.SourceEmpty, s.Check(0, 1)); }
        [Test] public void CannotPourIntoSelf() { var s = new PuzzleState(Make(new[] { 0 }, new int[0])); Assert.AreEqual(MoveFailure.SameContainer, s.Check(0, 0)); }
        [Test] public void CompletedDestinationIsLocked() { var s = new PuzzleState(Make(new[] { 1 }, new[] { 1, 1, 1, 1 })); Assert.AreEqual(MoveFailure.DestInaccessible, s.Check(0, 1)); }
        [Test] public void FullNonUniformDestinationRejects() { var s = new PuzzleState(Make(new[] { 1 }, new[] { 1, 0, 1, 1 })); Assert.AreEqual(MoveFailure.DestFull, s.Check(0, 1)); }
        [Test] public void DifferentColoursRejected() { var s = new PuzzleState(Make(new[] { 0 }, new[] { 1 })); Assert.AreEqual(MoveFailure.ColorMismatch, s.Check(0, 1)); }
        [Test] public void MatchingAndEmptyAccepted() { var s = new PuzzleState(Make(new[] { 0 }, new[] { 0, 1, 0 }, new int[0])); Assert.IsTrue(s.CanMove(0, 1)); Assert.IsTrue(s.CanMove(0, 2)); }

        [Test]
        public void OnlyContiguousTopGroupMoves()
        {
            var s = new PuzzleState(Make(new[] { 0, 1, 1, 1 }, new int[0]));
            var r = s.TryMove(0, 1);
            Assert.IsTrue(r.Valid); Assert.AreEqual(3, r.Amount);
            Assert.AreEqual(new[] { 0 }, s.Pots[0].Colors.ToArray());
        }

        [Test]
        public void PartialPourWhenDestinationLimited()
        {
            var s = new PuzzleState(Make(new[] { 2, 1, 1, 1 }, new[] { 0, 0, 1 }));
            var r = s.TryMove(0, 1);
            Assert.AreEqual(1, r.Amount);
            Assert.AreEqual(3, s.Pots[0].Count); Assert.AreEqual(4, s.Pots[1].Count);
        }

        [Test]
        public void CapacityNeverExceeded()
        {
            var s = new PuzzleState(Make(new[] { 1, 1, 1 }, new[] { 0, 1, 1 }));
            s.TryMove(0, 1); Assert.LessOrEqual(s.Pots[1].Count, 4);
        }

        [Test]
        public void CompletionAndWinDetected()
        {
            var s = new PuzzleState(Make(new[] { 0, 0, 0 }, new[] { 0 }, new int[0]));
            var r = s.TryMove(1, 0);
            Assert.AreEqual(0, r.CompletedPot); Assert.IsTrue(r.Won); Assert.IsTrue(s.IsWon);
            Assert.IsFalse(s.IsAccessible(0)); // completed pots lock
        }

        [Test]
        public void InvalidMoveChangesNothing()
        {
            var s = new PuzzleState(Make(new[] { 0 }, new[] { 1 }));
            int hist = s.HistoryCount;
            s.TryMove(0, 1);
            Assert.AreEqual(hist, s.HistoryCount); Assert.AreEqual(0, s.MoveCount);
            Assert.AreEqual(1, s.InvalidMoveCount);
        }

        [Test]
        public void UndoRestoresExactStateMultipleTimes()
        {
            var lvl = Make(new[] { 0, 1, 0, 0 }, new[] { 1, 1, 0 }, new int[0]);
            var s = new PuzzleState(lvl);
            string Dump() { var parts = new List<string>(); foreach (var p in s.Pots) parts.Add(string.Join(".", p.Colors) + "/" + p.Complete + p.Covered); return string.Join("|", parts); }
            string a = Dump();
            s.TryMove(0, 2); string b = Dump();
            Assert.IsTrue(s.TryMove(1, 2).Valid);
            Assert.IsTrue(s.Undo()); Assert.AreEqual(b, Dump());
            Assert.IsTrue(s.Undo()); Assert.AreEqual(a, Dump());
            Assert.IsFalse(s.Undo()); Assert.AreEqual(0, s.MoveCount);
        }

        [Test]
        public void RestartUsesImmutableOriginal()
        {
            var lvl = Make(new[] { 0, 1 }, new int[0]);
            var s = new PuzzleState(lvl); s.TryMove(0, 1);
            var fresh = new PuzzleState(lvl);
            Assert.AreEqual(new[] { 0, 1 }, fresh.Pots[0].Colors.ToArray());
        }

        [Test]
        public void HiddenLayerRevealsOnlyWhenTopAndIsNotAWildcard()
        {
            // pot0 bottom->top: hidden colour 2, then colour 0. pot1 holds colour 2 on top (a wildcard would accept)
            var s = new PuzzleState(Make(new[] { -3, 0 }, new[] { 2 }, new int[0]));
            Assert.IsTrue(s.Pots[0].Hidden[0]);
            Assert.AreEqual(MoveFailure.ColorMismatch, s.Check(0, 1));
            var r = s.TryMove(0, 2);
            Assert.AreEqual(0, r.RevealedPot); Assert.IsFalse(s.Pots[0].Hidden[0]);
            Assert.IsTrue(s.CanMove(0, 1));
        }

        [Test]
        public void HiddenLayerEndsMovableGroup()
        {
            var s = new PuzzleState(Make(new[] { -1, 0 }, new int[0]));
            Assert.AreEqual(1, s.TopGroupSize(0));
        }

        [Test]
        public void CoverUnlocksOnePerCompletion()
        {
            var l = Make(new[] { 0, 0, 0 }, new[] { 0 }, new[] { 1, 1, 1, 1 }, new[] { 2, 2, 2, 2 }, new int[0]);
            l.Containers[2].Covered = true; l.Containers[3].Covered = true;
            var s = new PuzzleState(l);
            Assert.AreEqual(MoveFailure.SourceInaccessible, s.Check(2, 4));
            Assert.AreEqual(MoveFailure.DestInaccessible, s.Check(1, 2));
            var r = s.TryMove(1, 0);
            Assert.AreEqual(1, r.UnlockedPots.Count); Assert.AreEqual(2, r.UnlockedPots[0]);
            Assert.IsFalse(s.Pots[2].Covered); Assert.IsTrue(s.Pots[3].Covered);
            Assert.IsFalse(s.IsWon);
        }

        [Test]
        public void UndoReversesCoverUnlock()
        {
            var l = Make(new[] { 0, 0, 0 }, new[] { 0 }, new[] { 1, 1, 1, 1 }, new int[0]);
            l.Containers[2].Covered = true;
            var s = new PuzzleState(l);
            s.TryMove(1, 0);
            Assert.IsFalse(s.Pots[2].Covered);
            s.Undo();
            Assert.IsTrue(s.Pots[2].Covered); Assert.IsFalse(s.Pots[0].Complete); Assert.AreEqual(0, s.TotalCoversUnlocked);
        }

        [Test]
        public void DifficultyCadence()
        {
            for (int i = 1; i <= 1200; i++)
            {
                var d = Progression.DifficultyFor(i);
                if (i % 10 == 0) Assert.AreEqual(Difficulty.SuperHard, d, "lvl " + i);
                else if (i % 5 == 0) Assert.AreEqual(Difficulty.Hard, d, "lvl " + i);
                else Assert.AreEqual(Difficulty.Normal, d, "lvl " + i);
            }
        }

        [Test]
        public void GeneratorIsDeterministicAndVerified()
        {
            foreach (int id in new[] { 1, 5, 12, 21, 31, 60 })
            {
                var a = LevelGenerator.Generate(id).Level; var b = LevelGenerator.Generate(id).Level;
                Assert.AreEqual(LevelCodec.Encode(a), LevelCodec.Encode(b), "determinism " + id);
                Assert.IsTrue(LevelAudit.ReplayWitness(a, out var err), err);
                var row = LevelAudit.Check(a, false, 0);
                Assert.IsTrue(row.Passed, row.Error);
            }
        }

        [Test]
        public void ObstaclesRespectUnlockLevels()
        {
            for (int id = 1; id < 21; id++) Assert.AreEqual(0, LevelCurve.For(id).Hidden);
            for (int id = 1; id < 31; id++) Assert.AreEqual(0, LevelCurve.For(id).Covers);
            Assert.GreaterOrEqual(LevelCurve.For(21).Hidden, 1); Assert.GreaterOrEqual(LevelCurve.For(31).Covers, 1);
        }

        [Test]
        public void CodecRoundTrips()
        {
            var l = LevelGenerator.Generate(33).Level;
            var s = LevelCodec.Encode(l);
            Assert.AreEqual(s, LevelCodec.Encode(LevelCodec.Decode(s)));
        }

        [Test]
        public void MilestoneSchedule()
        {
            var c = new EconomyConfig();
            Assert.AreEqual(20, c.LevelPayout(9)); Assert.AreEqual(70, c.LevelPayout(10));
            Assert.AreEqual(70, c.LevelPayout(50)); Assert.AreEqual(40, c.LevelPayout(60)); Assert.AreEqual(40, c.LevelPayout(100));
        }

        [Test]
        public void WalletIdempotentAndNeverNegative()
        {
            var w = new GoldWallet(new SystemTimeProvider());
            Assert.IsTrue(w.Grant(500, "start", 0, "start"));
            Assert.IsFalse(w.Grant(500, "start", 0, "start")); Assert.AreEqual(500, w.Balance);
            Assert.IsFalse(w.TrySpend(900, "shuffle")); Assert.AreEqual(500, w.Balance);
            w.Grant(400, "x"); Assert.IsTrue(w.TrySpend(900, "shuffle")); Assert.AreEqual(0, w.Balance);
        }

        [Test]
        public void LivesRegenerateEveryThirtyMinutesAndCap()
        {
            var t = new OffsetTimeProvider(); var cfg = new EconomyConfig(); var lv = new LivesModel(cfg, t);
            Assert.AreEqual(5, lv.Lives);
            lv.Consume(); lv.Consume(); Assert.AreEqual(3, lv.Lives);
            t.Offset = System.TimeSpan.FromMinutes(31); lv.Tick(); Assert.AreEqual(4, lv.Lives);
            t.Offset = System.TimeSpan.FromMinutes(500); lv.Tick(); Assert.AreEqual(5, lv.Lives);
            for (int i = 0; i < 9; i++) lv.Consume();
            Assert.AreEqual(0, lv.Lives); Assert.IsFalse(lv.Consume());
        }

        [Test]
        public void DailyRewardsFollowTrackAndPayBonusOnce()
        {
            var t = new OffsetTimeProvider(); var cfg = new EconomyConfig(); var w = new GoldWallet(t); var d = new DailyRewardModel(cfg, t);
            while (d.Today % cfg.WeekLengthDays != 0) t.Offset += System.TimeSpan.FromDays(1);
            int[] expect = { 50, 100, 200, 300, 400 };
            for (int i = 0; i < 5; i++)
            {
                var r = d.Claim(w); Assert.IsTrue(r.Ok); Assert.AreEqual(expect[i], r.Gold);
                Assert.IsFalse(d.Claim(w).Ok, "one claim per day");
                if (i == 4) Assert.AreEqual(1000, r.Bonus);
                t.Offset += System.TimeSpan.FromDays(1);
            }
            Assert.AreEqual(50 + 100 + 200 + 300 + 400 + 1000, w.Balance);
            t.Offset += System.TimeSpan.FromDays(7); d.Refresh(); Assert.AreEqual(0, d.ClaimedCount);
        }

        [Test]
        public void AdCooldownIsFiveMinutes()
        {
            var t = new OffsetTimeProvider(); var p = new AdPolicy(new EconomyConfig(), t);
            Assert.IsTrue(p.CooldownReady); p.MarkShown(); Assert.IsFalse(p.CooldownReady);
            t.Offset = System.TimeSpan.FromMinutes(4.9); Assert.IsFalse(p.CooldownReady);
            t.Offset = System.TimeSpan.FromMinutes(5.1); Assert.IsTrue(p.CooldownReady);
        }

        [Test]
        public void TargetedShufflePreservesColoursAndStaysSolvable()
        {
            var lvl = LevelGenerator.Generate(12).Level;
            var s = new PuzzleState(lvl);
            var sel = new List<int>(); for (int i = 0; i < s.Pots.Count && sel.Count < 3; i++) if (s.Pots[i].Count > 0) sel.Add(i);
            var before = new SortedDictionary<int, int>(); foreach (var i in sel) foreach (var c in s.Pots[i].Colors) { before.TryGetValue(c, out var n); before[c] = n + 1; }
            var o = TargetedShuffle.Compute(s, sel, 99);
            Assert.IsTrue(o.Ok, o.Message);
            s.ApplyContents(o.Pots, o.Colors, o.Hidden);
            var after = new SortedDictionary<int, int>(); foreach (var i in sel) foreach (var c in s.Pots[i].Colors) { after.TryGetValue(c, out var n); after[c] = n + 1; }
            CollectionAssert.AreEqual(before, after);
            Assert.AreEqual(SolverStatus.Solved, PuzzleSolver.Solve(s, 80000).Status);
        }

        // ------------------------------------------------------------------ resume (serialize / restore)
        [Test]
        public void SerializeRestoreRoundTripsAMidGameBoard()
        {
            var lvl = LevelGenerator.Generate(40).Level;                 // has hidden layers and covered pots
            var s = new PuzzleState(lvl);
            for (int n = 0; n < 6; n++)
            {
                bool moved = false;
                for (int i = 0; i < s.Pots.Count && !moved; i++) for (int j = 0; j < s.Pots.Count && !moved; j++) if (s.CanMove(i, j)) { s.TryMove(i, j); moved = true; }
            }
            s.AddEmptyPot();
            var r = PuzzleState.Restore(LevelGenerator.Generate(40).Level, s.Serialize());
            Assert.IsNotNull(r);
            Assert.AreEqual(s.Serialize(), r.Serialize());
            Assert.AreEqual(s.MoveCount, r.MoveCount);
            Assert.AreEqual(s.Pots.Count, r.Pots.Count);
            Assert.IsTrue(r.Pots[r.Pots.Count - 1].Extra);
        }

        [Test]
        public void RestoreRejectsBoardsThatDoNotBelongToTheLevel()
        {
            var lvl = LevelGenerator.Generate(12).Level;
            var good = new PuzzleState(lvl).Serialize();
            Assert.IsNull(PuzzleState.Restore(lvl, ""));
            Assert.IsNull(PuzzleState.Restore(lvl, "garbage"));
            Assert.IsNull(PuzzleState.Restore(lvl, good.Replace("0", "9")));           // wrong colours
            Assert.IsNull(PuzzleState.Restore(LevelGenerator.Generate(300).Level, good)); // a level with a different shape
            Assert.IsNull(PuzzleState.Restore(lvl, good + ",0.0.0.0.0"));              // overfull extra pot
        }

        // ------------------------------------------------------------------ tutorial + difficulty
        [Test]
        public void TutorialLevelsAreSolvableAndShort()
        {
            for (int id = 1; id <= 4; id++)
            {
                Assert.IsTrue(TutorialLevels.TryCreate(id, out var lvl), "tutorial " + id);
                var sol = PuzzleSolver.Solve(new PuzzleState(lvl), 50000);
                Assert.AreEqual(SolverStatus.Solved, sol.Status, "tutorial " + id);
                Assert.LessOrEqual(sol.Moves.Count, 12, "tutorial " + id + " should be short");
            }
        }

        [Test]
        public void DifficultyProbeIsDeterministic()
        {
            var lvl = LevelGenerator.Generate(200).Level;
            Assert.AreEqual(DifficultyProbe.CarefulProgress(lvl, 8, 5), DifficultyProbe.CarefulProgress(lvl, 8, 5));
        }

        [Test]
        public void SuperHardLevelsPlayHarderThanNormalOnes()
        {
            float normal = 0, superHard = 0; int n = 0, h = 0;
            for (int id = 200; id < 400; id++)
            {
                var lvl = LevelGenerator.Generate(id).Level;
                if (lvl.Difficulty == Difficulty.Normal && id % 6 == 1 && n < 10) { normal += DifficultyProbe.CarefulProgress(lvl, 20, id); n++; }
                else if (lvl.Difficulty == Difficulty.SuperHard && h < 10) { superHard += DifficultyProbe.CarefulProgress(lvl, 20, id); h++; }
            }
            Assert.Greater(n, 5); Assert.Greater(h, 5);
            Assert.Greater(normal / n, superHard / h, "Super Hard levels should be measurably harder to play than Normal ones");
        }
    }
}
