using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PaintSort.Core;
using PaintSort.Game;
using UnityEngine;
using UnityEngine.TestTools;

namespace PaintSort.Tests
{
    /// <summary>
    /// End-to-end tests that drive the real game (the code-built UI, App flow, economy and save) the way a player would,
    /// through the same entry points the buttons call. They run against a throw-away save file, never the player's real one.
    /// </summary>
    public class GameFlowTests
    {
        static readonly string TestSave = Path.Combine(Path.GetTempPath(), "paintsort_playmode_test_save.json");

        static App A => App.I;

        static readonly MethodInfo TapPot = typeof(App).GetMethod("TapPot", BindingFlags.Instance | BindingFlags.NonPublic);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return new WaitUntil(() => App.I != null && App.I.Lives != null);
            A.CloseAllPopups();
            A.UseIsolatedSaveForTests(TestSave);  // fresh economy and progress, persisted to a temp file (the real save is never touched)
            yield return null;
            A.CloseAllPopups();
        }

        static IEnumerator WaitIdle()
        {
            float t = 0;
            while (A.Board.Busy && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsFalse(A.Board.Busy, "board animation never finished");
        }

        static IEnumerator Tap(int pot) { TapPot.Invoke(A, new object[] { pot }); yield return null; }

        static IEnumerator PlayMove(Move m)
        {
            yield return Tap(m.From);
            yield return Tap(m.To);
            yield return WaitIdle();
        }

        static IEnumerator WaitForPopup()
        {
            float t = 0;
            while (!A.PopupOpen && t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
        }

        [UnityTest]
        public IEnumerator BootsWithStartingGoldFiveLivesAndTheFullCatalog()
        {
            Assert.AreEqual(500, A.Wallet.Balance);
            Assert.AreEqual(5, A.Lives.Lives);
            Assert.GreaterOrEqual(A.MaxLevel, 5000);
            Assert.IsFalse(A.HasResume);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CompletingLevelOneGrantsTheRewardAndUnlocksLevelTwo()
        {
            A.StartLevel(1, true);
            var witness = A.CurrentDef.Witness;
            Assert.Greater(witness.Count, 0);
            foreach (var m in witness) yield return PlayMove(m);
            yield return WaitForPopup();

            Assert.IsTrue(A.PopupOpen, "level-complete popup should open");
            Assert.IsTrue(A.Completed(1));
            Assert.GreaterOrEqual(A.Wallet.Balance, 520, "level reward (plus any achievement bonus) was paid");
            Assert.GreaterOrEqual(A.D.highestUnlocked, 2);
            Assert.IsFalse(A.HasResume, "a finished level must not leave a resume board behind");
        }

        [UnityTest]
        public IEnumerator ReplayingACompletedLevelPaysNothingTheSecondTime()
        {
            int afterFirst = 0;
            for (int pass = 0; pass < 2; pass++)
            {
                A.CloseAllPopups();
                A.StartLevel(1, true);
                foreach (var m in A.CurrentDef.Witness) yield return PlayMove(m);
                yield return WaitForPopup();
                if (pass == 0) afterFirst = A.Wallet.Balance;
            }
            Assert.Greater(afterFirst, 500);
            Assert.AreEqual(afterFirst, A.Wallet.Balance, "reward is paid once per level");
        }

        [UnityTest]
        public IEnumerator LeavingMidLevelAndPressingPlayResumesTheSameBoard()
        {
            A.StartLevel(4, true);
            yield return PlayMove(A.CurrentDef.Witness[0]);
            string before = A.State.Serialize();
            Assert.AreEqual(1, A.State.MoveCount);

            A.LeaveToMenu();
            Assert.IsTrue(A.HasResume);
            Assert.AreEqual(4, A.PlayTargetLevel);

            A.OnPlay();
            Assert.AreEqual(before, A.State.Serialize());
            Assert.AreEqual(1, A.State.MoveCount);
        }

        [UnityTest]
        public IEnumerator LeavingDuringAPourStillSavesTheMove()
        {
            A.StartLevel(4, true);
            var m = A.CurrentDef.Witness[0];
            yield return Tap(m.From);
            yield return Tap(m.To);                   // the pour animation is now running
            Assert.IsTrue(A.Board.Busy);
            A.LeaveToMenu();                          // abandon mid-animation
            Assert.IsTrue(A.HasResume);
            A.OnPlay();
            Assert.AreEqual(1, A.State.MoveCount, "the move that was being animated must not be lost");
        }

        [UnityTest]
        public IEnumerator RestartingAfterResumeStartsAFreshBoardWithoutCostingALife()
        {
            A.StartLevel(4, true);
            yield return PlayMove(A.CurrentDef.Witness[0]);
            A.RestartLevel();
            Assert.AreEqual(0, A.State.MoveCount);
            Assert.IsFalse(A.HasResume);
            Assert.AreEqual(5, A.Lives.Lives);
        }

        [UnityTest]
        public IEnumerator ConsumingALifeTwiceForTheSameAttemptOnlyCostsOne()
        {
            A.StartLevel(2, true);
            int attempt = A.AttemptId;
            A.ConsumeLifeAndLeave(attempt);
            A.ConsumeLifeAndLeave(attempt);       // a duplicate / stale callback
            Assert.AreEqual(4, A.Lives.Lives);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UndoIsFreeAndRestoresTheBoard()
        {
            A.StartLevel(4, true);
            string start = A.State.Serialize();
            yield return PlayMove(A.CurrentDef.Witness[0]);
            int gold = A.Wallet.Balance;
            A.OnUndo();
            Assert.AreEqual(gold, A.Wallet.Balance);
            Assert.AreEqual(start.Split('|')[1], A.State.Serialize().Split('|')[1], "pots are back to the start");
        }

        [UnityTest]
        public IEnumerator LevelSelectOpensAndCloses()
        {
            A.OpenLevelSelect(-1);
            Assert.IsTrue(A.PopupOpen);
            A.CloseAllPopups();
            Assert.IsFalse(A.PopupOpen);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SavingAndReloadingKeepsGoldAndProgress()
        {
            A.StartLevel(1, true);
            foreach (var m in A.CurrentDef.Witness) yield return PlayMove(m);
            yield return WaitForPopup();
            A.FlushSave();

            var fresh = new SaveService(TestSave); fresh.Load();
            Assert.AreEqual(A.Wallet.Balance, fresh.Data.gold);
            CollectionAssert.Contains(fresh.Data.completed, 1);
        }
    }
}
