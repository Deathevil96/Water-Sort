using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PaintSort.Core;
using PaintSort.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
            A.StartLevel(Progression.UndoUnlockLevel, true);
            string start = A.State.Serialize();
            yield return PlayMove(A.CurrentDef.Witness[0]);
            A.CloseAllPopups();                       // the "new power-up" intro popup is open on this level (and correctly blocks the buttons)
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

        // ------------------------------------------------------------------ power-up progression
        [UnityTest]
        public IEnumerator PowerUpsStayLockedUntilTheirLevel()
        {
            A.StartLevel(1, true);
            Assert.IsFalse(A.IsUnlocked(PowerUp.Undo)); Assert.IsFalse(A.IsUnlocked(PowerUp.Shuffle)); Assert.IsFalse(A.IsUnlocked(PowerUp.ExtraPot));

            A.StartLevel(Progression.UndoUnlockLevel, true);
            Assert.IsTrue(A.IsUnlocked(PowerUp.Undo)); Assert.IsFalse(A.IsUnlocked(PowerUp.Shuffle));

            A.StartLevel(Progression.ShuffleUnlockLevel, true);
            Assert.IsTrue(A.IsUnlocked(PowerUp.Shuffle)); Assert.IsFalse(A.IsUnlocked(PowerUp.ExtraPot));

            A.StartLevel(Progression.ExtraPotUnlockLevel, true);
            Assert.IsTrue(A.IsUnlocked(PowerUp.ExtraPot));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LockedUndoDoesNothingOnTheOpeningLevels()
        {
            A.StartLevel(2, true);
            yield return PlayMove(A.CurrentDef.Witness[0]);
            string after = A.State.Serialize();
            A.OnUndo();
            Assert.AreEqual(after, A.State.Serialize(), "undo must stay locked before level " + Progression.UndoUnlockLevel);
        }

        [UnityTest]
        public IEnumerator EachPowerUpIsIntroducedByAPopupOnItsLevel()
        {
            foreach (int level in new[] { Progression.UndoUnlockLevel, Progression.ShuffleUnlockLevel, Progression.ExtraPotUnlockLevel })
            {
                A.CloseAllPopups();
                A.StartLevel(level, true);
                float t = 0; while (!A.PopupOpen && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
                Assert.IsTrue(A.PopupOpen, "intro popup expected on level " + level);
            }
        }

        [UnityTest]
        public IEnumerator PaidPowerUpsComeWithExactlyOneFreeTokenAndOnlyOnce()
        {
            A.StartLevel(Progression.ShuffleUnlockLevel, true);
            Assert.AreEqual(1, A.D.freeShuffleTokens);
            A.StartLevel(Progression.ShuffleUnlockLevel, true);
            Assert.AreEqual(1, A.D.freeShuffleTokens, "the intro token is granted once");

            A.StartLevel(Progression.ExtraPotUnlockLevel, true);
            Assert.AreEqual(1, A.D.freeExtraPotTokens);
            yield return null;
        }

        // ------------------------------------------------------------------ real pointer routing: nothing may sit on top of a button or a pot
        static readonly MethodInfo OpenSettings = typeof(App).GetMethod("OpenSettings", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly MethodInfo OpenAchievements = typeof(App).GetMethod("OpenAchievements", BindingFlags.Instance | BindingFlags.NonPublic);

        static string PathOf(Transform t)
        {
            var s = t.name;
            while (t.parent != null && t.parent.name != "PaintSortCanvas") { t = t.parent; s = t.name + "/" + s; }
            return s;
        }

        static void Probe(Transform target, string label, Transform onlyUnder, List<RaycastResult> hits, System.Text.StringBuilder problems)
        {
            if (onlyUnder != null && !target.IsChildOf(onlyUnder)) return;
            var es = EventSystem.current;
            var rt = (RectTransform)target;
            ScrollIntoView(rt);                                                          // a button inside a scroll list is only reachable once scrolled to
            var centre = (Vector2)rt.TransformPoint(rt.rect.center);                      // overlay canvas: world position == screen pixels
            if (centre.x < 0 || centre.y < 0 || centre.x > Screen.width || centre.y > Screen.height) { problems.AppendLine(label + " is off screen at " + centre); return; }
            hits.Clear(); es.RaycastAll(new PointerEventData(es) { position = centre }, hits);
            var top = hits.Count > 0 ? hits[0].gameObject : null;
            if (top == null || !top.transform.IsChildOf(target))
                problems.AppendLine(label + " at " + centre + " is covered by " + (top != null ? PathOf(top.transform) : "nothing (no raycast target)"));
        }

        /// <summary>Scrolls the enclosing vertical scroll list (if any) so the target sits in the middle of its viewport.</summary>
        static void ScrollIntoView(RectTransform target)
        {
            var sr = target.GetComponentInParent<ScrollRect>();
            if (sr == null || sr.content == null || sr.viewport == null) return;
            Canvas.ForceUpdateCanvases();
            float itemY = -sr.content.InverseTransformPoint(target.TransformPoint(target.rect.center)).y;      // distance below the top of the content
            float max = Mathf.Max(0f, sr.content.rect.height - sr.viewport.rect.height);
            sr.content.anchoredPosition = new Vector2(sr.content.anchoredPosition.x, Mathf.Clamp(itemY - sr.viewport.rect.height * 0.5f, 0f, max));
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>Casts a pointer ray through the real event system at the centre of every active button (and pot) and reports whatever is on top instead.</summary>
        static string FindBlockedTargets(Transform onlyUnder = null)
        {
            var hits = new List<RaycastResult>();
            var problems = new System.Text.StringBuilder();
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude)) Probe(b.transform, "Button " + PathOf(b.transform), onlyUnder, hits, problems);
            foreach (var p in Object.FindObjectsByType<PotView>(FindObjectsInactive.Exclude)) if (p.Board != null) Probe(p.transform, "Pot " + p.Index, onlyUnder, hits, problems);
            return problems.ToString();
        }

        [UnityTest]
        public IEnumerator NothingBlocksTheMainMenuButtons()
        {
            A.LeaveToMenu(); yield return null; yield return null;
            Assert.AreEqual("", FindBlockedTargets());
        }

        [UnityTest]
        public IEnumerator NothingBlocksButtonsOrPotsOnTheBoardEvenWhileAHintIsShowing()
        {
            foreach (int level in new[] { 1, 2, 4, 35 })
            {
                A.CloseAllPopups(); A.StartLevel(level, true);
                yield return null; yield return null;
                Assert.AreEqual("", FindBlockedTargets(), "level " + level);
            }
        }

        static readonly MethodInfo SetShuffleBar = typeof(App).GetMethod("SetShuffleBar", BindingFlags.Instance | BindingFlags.NonPublic);

        [UnityTest]
        public IEnumerator ShuffleModeNeverCoversAnyPotAndItsButtonsAreReachable()
        {
            foreach (int level in new[] { 12, 35, 260, 700 })        // small boards and big multi-row boards
            {
                A.CloseAllPopups(); A.StartLevel(level, true);
                yield return null; yield return null;
                SetShuffleBar.Invoke(A, new object[] { true });
                yield return null; yield return null;
                Assert.AreEqual("", FindBlockedTargets(), "shuffle mode, level " + level);
                SetShuffleBar.Invoke(A, new object[] { false });
            }
        }

        static readonly FieldInfo HintBar = typeof(App).GetField("hintBar", BindingFlags.Instance | BindingFlags.NonPublic);

        [UnityTest]
        public IEnumerator TheHintBoxNeverBlocksAPotAndHidesOnTheFirstTap()
        {
            A.StartLevel(1, true); yield return null; yield return null;
            var hint = (RectTransform)HintBar.GetValue(A);
            Assert.IsTrue(hint.gameObject.activeSelf, "level 1 shows a hint");
            foreach (var g in hint.GetComponentsInChildren<Graphic>(true)) Assert.IsFalse(g.raycastTarget, "hint graphic " + g.name + " must not block taps");
            yield return Tap(A.CurrentDef.Witness[0].From);
            Assert.IsFalse(hint.gameObject.activeSelf, "the hint hides itself on the first tap");
        }

        [UnityTest]
        public IEnumerator NothingBlocksTheButtonsInsideEachPopup()
        {
            var popups = new System.Action[]
            {
                () => A.OpenShop(), () => A.OpenDaily(), () => A.OpenLeaderboard(), () => A.OpenLevelSelect(-1),
                () => OpenSettings.Invoke(A, null), () => OpenAchievements.Invoke(A, null),
                () => A.OpenDialog("Test", "Body", ("Cancel", UI.Grey, null), ("OK", UI.Green, null)),
                () => A.OpenNoLives(),
            };
            for (int i = 0; i < popups.Length; i++)
            {
                A.CloseAllPopups(); A.LeaveToMenu();
                popups[i]();
                yield return null; yield return null; yield return null;                            // let the pop-in animation start and layout settle
                var layer = GameObject.Find("Popups").transform;
                Assert.AreEqual("", FindBlockedTargets(layer), "popup #" + i);
            }
        }

        [UnityTest]
        public IEnumerator PopupsSwallowTapsMeantForTheBoardBehindThem()
        {
            A.StartLevel(1, true); yield return null;
            A.OpenDialog("Test", "Body", ("OK", UI.Green, null));
            yield return null; yield return null; yield return null;
            var es = EventSystem.current; var hits = new List<RaycastResult>();
            var pot = Object.FindObjectsByType<PotView>(FindObjectsInactive.Exclude)[0];
            var rt = (RectTransform)pot.transform;
            es.RaycastAll(new PointerEventData(es) { position = (Vector2)rt.TransformPoint(rt.rect.center) }, hits);
            Assert.Greater(hits.Count, 0);
            Assert.IsTrue(hits[0].gameObject.transform.IsChildOf(GameObject.Find("Popups").transform), "a popup must swallow taps meant for the board behind it");
        }
    }
}
