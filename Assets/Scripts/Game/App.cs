using System;
using System.Collections;
using System.Collections.Generic;
using PaintSort.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PaintSort.Game
{
    public enum PowerUp { Undo, Shuffle, ExtraPot }

    /// <summary>Game flow + services owner. The only place that mutates persistent progress. Puzzle rules live in PaintSort.Core.</summary>
    public sealed partial class App : MonoBehaviour
    {
        public static App I;

        // ---- services
        public EconomyConfig Cfg { get; private set; }
        public SaveService Save { get; private set; }
        public OffsetTimeProvider Clock { get; private set; }
        public GoldWallet Wallet { get; private set; }
        public LivesModel Lives { get; private set; }
        public DailyRewardModel Daily { get; private set; }
        public AdPolicy AdPol { get; private set; }
        public IAdService Ads { get; private set; }
        public MockAdService MockAds { get; private set; }
        public IPurchaseService Purchases { get; private set; }
        public ILeaderboardService Leaderboard { get; private set; }
        public IAnalytics Analytics { get; private set; }
        public AudioService Audio { get; private set; }
        public LevelCatalog Catalog { get; private set; }
        public float AnimSpeed = 1f;

        // ---- current attempt
        public PuzzleState State { get; private set; }
        public LevelDefinition CurrentDef { get; private set; }
        public int CurrentLevel;
        int selected = -1;
        bool inGame, attemptEnded, winHandled;
        bool shuffleMode; HashSet<int> shuffleMarks;
        int extrasAdded;
        public int AttemptId { get; private set; }
        public bool XRay;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            new GameObject("PaintSortApp").AddComponent<App>();
        }

        public SaveData D => Save.Data;

        void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
            InputMode.FocusVisible = false;          // static state can outlive a stopped Editor play session; only keys / a TV remote may turn the cursor ring on
            Application.targetFrameRate = 60;
#if !UNITY_EDITOR
            QualitySettings.vSyncCount = 0;          // let targetFrameRate pace the game on devices
#endif
            DetectDevice();

            popups = new List<GameObject>(); popupBack = new Dictionary<GameObject, Action>(); liveTexts = new List<KeyValuePair<Text, Func<string>>>(); shuffleMarks = new HashSet<int>();
            Cfg = new EconomyConfig(); Save = new SaveService(); Clock = new OffsetTimeProvider();
            Purchases = new DisabledPurchaseService(); Leaderboard = new LocalMockLeaderboard(); Analytics = new LogAnalytics();
            Wallet = new GoldWallet(Clock); Lives = new LivesModel(Cfg, Clock); Daily = new DailyRewardModel(Cfg, Clock); AdPol = new AdPolicy(Cfg, Clock);
            Audio = new AudioService(gameObject);
            MockAds = new MockAdService(AdPol, ShowFakeAd); Ads = MockAds;
#if UNITY_EDITOR || DEBUG
            Purchases = new FakePurchaseService();
#endif
            var txt = Resources.Load<TextAsset>("Levels/levels");
            Catalog = txt != null ? LevelCatalog.Parse(txt.text) : new LevelCatalog();
            Log.I("LEVEL", "Catalog loaded: " + Catalog.Count + " levels");

            Save.Load(); RestoreFromSave();
            BuildUi();
            ShowMenu();
            if (Daily.CanClaimToday && D.stats.TotalCompleted > 0) StartCoroutine(Delay(0.5f, OpenDaily));
            Audio.StartMusic();
            booted = true;
        }

        void RestoreFromSave()
        {
            var d = D;
            Wallet.Balance = d.gold; Wallet.Ledger = d.ledger ?? new List<GoldTransaction>(); Wallet.ClaimIds = new HashSet<string>(d.claimIds ?? new List<string>());
            if (!d.startGoldGranted)
            {
                Wallet.Grant(Cfg.StartingGold, "starting_grant", 0, "starting_grant");
                d.startGoldGranted = true;
            }
            Lives.Load(d.livesLastRegenTicks == 0 ? Cfg.MaxLives : d.lives, d.livesLastRegenTicks == 0 ? Clock.UtcNow.Ticks : d.livesLastRegenTicks);
            Lives.InfiniteUntilTicks = d.infiniteLivesUntilTicks;
            Daily.WeekIndex = d.dailyWeek; Daily.ClaimedCount = d.dailyClaimed; Daily.LastClaimDay = d.dailyLastDay; Daily.BonusPaid = d.dailyBonusPaid; Daily.EverCompletedWeek = d.dailyEverCompleted;
            AdPol.LastAdTicks = d.lastAdTicks;
            Audio.SfxOn = d.sfxOn; Audio.HapticsOn = d.hapticsOn; Audio.MusicOn = d.musicOn;
            Loc.SetLanguage(d.language);
            Wallet.Changed += _ => Persist();
            Lives.Changed += Persist;
            if (d.currentLevel < 1) d.currentLevel = 1;
            Persist();
        }

        const int MaxLedgerEntries = 500;            // the ledger is an audit trail, not unbounded history
        const float SaveInterval = 1f;               // write-behind: many changes in a burst cost one disk write
        bool saveDirty; float nextSaveTime;

        /// <summary>Copies runtime state into the save data and schedules a write. The write itself is deferred (see <see cref="FlushSave"/>).</summary>
        public void Persist()
        {
            var d = D;
            if (Wallet.Ledger.Count > MaxLedgerEntries) Wallet.Ledger.RemoveRange(0, Wallet.Ledger.Count - MaxLedgerEntries);
            d.gold = Wallet.Balance; d.ledger = Wallet.Ledger; d.claimIds = new List<string>(Wallet.ClaimIds);
            d.lives = Lives.Lives; d.livesLastRegenTicks = Lives.LastRegenTicks; d.infiniteLivesUntilTicks = Lives.InfiniteUntilTicks;
            d.dailyWeek = Daily.WeekIndex; d.dailyClaimed = Daily.ClaimedCount; d.dailyLastDay = Daily.LastClaimDay; d.dailyBonusPaid = Daily.BonusPaid; d.dailyEverCompleted = Daily.EverCompletedWeek;
            d.lastAdTicks = AdPol.LastAdTicks; d.sfxOn = Audio.SfxOn; d.hapticsOn = Audio.HapticsOn; d.musicOn = Audio.MusicOn; d.language = Loc.Language;
            d.stats.WeekTrackCompleted = Daily.EverCompletedWeek;
            saveDirty = true;
            RefreshHud();
        }

        /// <summary>Writes the save file now if anything changed. Called on a timer, on pause and on quit.</summary>
        public void FlushSave()
        {
            if (!saveDirty) return;
            saveDirty = false; nextSaveTime = Time.unscaledTime + SaveInterval;
            Save.Save();
        }

        void OnApplicationPause(bool paused) { if (paused) { Persist(); FlushSave(); } else { Lives.Tick(); RefreshHud(); } }
        void OnApplicationFocus(bool focus) { if (!focus && Lives != null) { Persist(); FlushSave(); } }
        void OnApplicationQuit() { if (Lives == null) return; Persist(); FlushSave(); }

        float hudTimer;
        void Update()
        {
            if (Lives == null) { Destroy(gameObject); I = null; return; }   // stale object left over from an assembly reload
            PollScreen();
            PollInput();
            Audio.Tick();
#if UNITY_EDITOR || DEBUG
            UpdateDevTools();
#endif
            hudTimer += Time.unscaledDeltaTime;
            if (hudTimer > 0.25f) { hudTimer = 0; Lives.Tick(); RefreshHud(); TickLiveTexts(); }
            if (saveDirty && Time.unscaledTime >= nextSaveTime) FlushSave();
        }

        public IEnumerator Delay(float s, Action a) { yield return new WaitForSecondsRealtime(s); a?.Invoke(); }

        // =====================================================================================  levels / attempts
        public int MaxLevel => Catalog != null && Catalog.Count > 0 ? Catalog.MaxLevel : 1;
        public bool Completed(int level) => D.completed.Contains(level);

        /// <summary>Level the main-menu button will open: the level in progress if there is one, otherwise the furthest unlocked level.</summary>
        public bool HasResume => D.resumeLevel > 0 && !string.IsNullOrEmpty(D.resumeBoard) && Catalog.Has(D.resumeLevel);
        public int PlayTargetLevel => HasResume ? D.resumeLevel : Mathf.Min(Mathf.Max(1, D.highestUnlocked), MaxLevel);

        public void OnPlay() => StartLevel(PlayTargetLevel, false, true);

        void SaveBoard() { if (State == null || !inGame) return; D.resumeLevel = CurrentLevel; D.resumeBoard = State.Serialize(); Persist(); }
        void ClearResume() { if (D.resumeLevel == 0 && string.IsNullOrEmpty(D.resumeBoard)) return; D.resumeLevel = 0; D.resumeBoard = ""; }

        /// <summary>Starts a level. With <paramref name="resume"/> the saved board for that level is restored if it is valid; otherwise a fresh attempt starts.</summary>
        public void StartLevel(int id, bool force = false, bool resume = false)
        {
            id = Mathf.Clamp(id, 1, MaxLevel);
            if (!Catalog.Has(id)) { Toast("Level " + id + " is not available."); return; }
            if (!force && !Lives.HasLife) { OpenNoLives(); return; }
            CurrentLevel = id; D.currentLevel = id; AttemptId++;
            var original = Catalog.Get(id);
            PuzzleState restored = resume && D.resumeLevel == id ? PuzzleState.Restore(original, D.resumeBoard) : null;
            if (restored != null && restored.IsWon) restored = null;      // a board saved at the instant of winning is not worth resuming
            if (restored == null) ClearResume();
            // every new attempt (first start, retry, restart) gets the same layout with different paint colours; a resumed board keeps its own colours
            CurrentDef = restored != null ? original : LevelVariant.Make(original, new System.Random());
            State = restored ?? new PuzzleState(CurrentDef);
            selected = -1; attemptEnded = false; winHandled = false; shuffleMode = false; shuffleMarks.Clear();
            extrasAdded = 0; foreach (var p in State.Pots) if (p.Extra) extrasAdded++;
            inGame = true; cursor = 0;
            ShowGame();
            Board.Build(State, boardSize);
            RefreshHud(); SetShuffleBar(false);
            Analytics.Event("level_started", id.ToString());
            Log.I("LEVEL", "Started level " + id + " seed " + CurrentDef.Seed + " diff " + CurrentDef.Difficulty + " mech H" + CurrentDef.HiddenCount + " C" + CurrentDef.CoveredCount);
            ClearPowerHighlight();
            if (restored == null) { ShowLevelHints(id); MaybeIntroducePowerUp(id); } else { hintBar.gameObject.SetActive(false); Toast("Welcome back! Your board was saved."); }
            Persist();
        }

        public void RestartLevel(bool analytics = true)
        {
            if (analytics) Analytics.Event("level_restarted", CurrentLevel.ToString());
            StartLevel(CurrentLevel, true);
        }

        public void LeaveToMenu()
        {
            inGame = false; Board.Clear(); CloseAllPopups(); ShowMenu(); Persist();
        }

        // ------------------------------------------------------------------ input
        void TapPot(int i)
        {
            if (!inGame || State == null || attemptEnded || winHandled || Board.Busy || PopupOpen) return;
            if (i < 0 || i >= State.Pots.Count) return;
            var pot = State.Pots[i];

            if (shuffleMode)
            {
                if (!TargetedShuffle.IsEligible(State, i)) { Audio.Play("invalid"); Board.Shake(i); return; }
                if (!shuffleMarks.Add(i)) shuffleMarks.Remove(i);
                Board.SetMarked(i, shuffleMarks.Contains(i)); Audio.Play("select"); UpdateShuffleBar();
                return;
            }

            if (selected < 0)
            {
                if (pot.Covered) { Audio.Play("invalid"); Board.Shake(i); Toast("Covered! Finish the colour shown on the cloth."); return; }
                if (pot.IsEmpty || !State.IsAccessible(i)) { Audio.Play("invalid"); Board.Shake(i); return; }
                selected = i; Board.SetSelected(i, true); Audio.Play("select");
                if (hintBar.gameObject.activeSelf) hintBar.gameObject.SetActive(false);      // the player has understood: get the hint out of the way
                return;
            }
            if (i == selected) { Board.SetSelected(i, false); selected = -1; Audio.Play("tap"); return; }

            var check = State.Check(selected, i);
            if (check == MoveFailure.None) { DoMove(selected, i); return; }
            // tapping another non-empty, usable pot that cannot receive the paint simply changes the source
            if (check == MoveFailure.ColorMismatch)
            {
                Board.SetSelected(selected, false); selected = i; Board.SetSelected(i, true); Audio.Play("select"); return;
            }
            State.TryMove(selected, i);                 // registers as an invalid move (state unchanged)
            Audio.Play("invalid"); Audio.Vibrate(); Board.Shake(i, selected);
            Analytics.Event("move_invalid", check.ToString());
        }

        void DoMove(int from, int to)
        {
            var before = BoardView.UnitsOf(State.Pots[from]); var dstBefore = BoardView.UnitsOf(State.Pots[to]);
            var r = State.TryMove(from, to);
            if (!r.Valid) { Board.Shake(to, from); Audio.Play("invalid"); return; }
            Board.SetSelected(from, false); selected = -1;
            if (!r.Won) SaveBoard();          // the logical state is already final, so it is safe to save before the animation plays
            Log.I("MOVE", from + ">" + to + " x" + r.Amount + (r.CompletedPot >= 0 ? " complete" : "") + (r.UnlockedPots.Count > 0 ? " unlock" : ""));
            Board.PlayMove(r, before, dstBefore, () => AfterMove(r));
            RefreshHud();
        }

        void AfterMove(MoveResult r)
        {
            RefreshHud();
            if (r.Won) { StartCoroutine(HandleWin()); return; }
            if (!State.HasAnyMove()) StartCoroutine(Delay(0.35f, () => { if (!attemptEnded && !winHandled && !PopupOpen) OpenStuckPrompt(); }));
        }

        // ------------------------------------------------------------------ win
        IEnumerator HandleWin()
        {
            if (winHandled) yield break;
            winHandled = true; ClearResume();
            int level = CurrentLevel;
            bool first = !D.completed.Contains(level);
            int basePay = Cfg.LevelReward, bonus = Cfg.MilestoneBonus(level);
            if (first)
            {
                // persisted + idempotent (claim id) so closing the app mid-animation can neither lose nor duplicate it
                Wallet.Grant(basePay, "level_reward", level, "levelwin_" + level);
                if (bonus > 0) Wallet.Grant(bonus, "milestone_bonus", level, "milestone_" + level);
                D.completed.Add(level);
                var st = D.stats;
                st.TotalCompleted++; if (level > st.HighestCompleted) st.HighestCompleted = level;
                if (CurrentDef.Difficulty == Difficulty.Hard) st.HardCompleted++;
                if (CurrentDef.Difficulty == Difficulty.SuperHard) st.SuperHardCompleted++;
                if (CurrentDef.HiddenCount > 0) st.HiddenLevelsCompleted++;
                st.CoversUnlocked += CurrentDef.CoveredCount;
                if (State.InvalidMoveCount == 0) st.NoInvalidMoveWin = true;
                if (State.UndoCount == 0) st.NoUndoWin = true;
                if (level + 1 > D.highestUnlocked) D.highestUnlocked = Mathf.Min(level + 1, MaxLevel);
                if (level == Cfg.LeaderboardUnlockLevel) Analytics.Event("leaderboard_unlocked");
            }
            D.currentLevel = Mathf.Min(MaxLevel, level + 1);
            var newlyUnlocked = CheckAchievements();
            Analytics.Event("level_completed", level + " moves " + State.MoveCount);
            Analytics.Event("gold_earned", (first ? basePay + bonus : 0).ToString());
            Persist();
            Audio.Play("win"); Haptics.Heavy(); Board.PlayWinFx();
            yield return new WaitForSecondsRealtime(0.9f);
            OpenLevelComplete(level, first ? basePay : 0, first ? bonus : 0, newlyUnlocked);
        }

        List<AchievementDef> CheckAchievements()
        {
            var list = new List<AchievementDef>();
            foreach (var a in Achievements.All)
            {
                if (D.achievements.Contains(a.Id) || !a.Condition(D.stats)) continue;
                D.achievements.Add(a.Id); list.Add(a);
                if (a.Reward > 0) Wallet.Grant(a.Reward, "achievement", 0, "ach_" + a.Id);
                Analytics.Event("achievement_unlocked", a.Id); Log.I("ACHIEVEMENT", a.Title);
            }
            return list;
        }

        // ------------------------------------------------------------------ power-ups
        public static int UnlockLevel(PowerUp p) => p == PowerUp.Undo ? Progression.UndoUnlockLevel : p == PowerUp.Shuffle ? Progression.ShuffleUnlockLevel : Progression.ExtraPotUnlockLevel;

        /// <summary>A power-up is available once the player has reached its level (so replaying an early level later does not lock it again).</summary>
        public bool IsUnlocked(PowerUp p) => Mathf.Max(D.highestUnlocked, inGame ? CurrentLevel : 0) >= UnlockLevel(p);

        bool Locked(PowerUp p, string name)
        {
            if (IsUnlocked(p)) return false;
            Toast(Loc.F("{0} unlocks at level {1}", Loc.T(name), UnlockLevel(p))); Audio.Play("invalid");
            return true;
        }

        public void OnUndo()
        {
            if (!CanAct()) return;
            if (Locked(PowerUp.Undo, "Undo")) return;
            if (!State.CanUndo) { Toast("Nothing to undo yet."); Audio.Play("invalid"); return; }
            if (Cfg.UndoPrice > 0 && !Wallet.TrySpend(Cfg.UndoPrice, "undo", CurrentLevel)) { OpenInsufficient("Undo", Cfg.UndoPrice, null); return; }
            if (selected >= 0) { Board.SetSelected(selected, false); selected = -1; }
            State.Undo(); Board.RenderAll(); Audio.Play("tap"); Analytics.Event("undo_used"); OnPowerUsed(PowerUp.Undo); SaveBoard(); RefreshHud();
            ShowOnce("undo", "Undo rewinds your last pour — it's free, use it as much as you like!");
        }

        bool CanAct() => inGame && State != null && !attemptEnded && !winHandled && !Board.Busy && !PopupOpen;

        public void OnShuffleButton()
        {
            if (!CanAct()) return;
            if (Locked(PowerUp.Shuffle, "Shuffle")) return;
            if (shuffleMode) { SetShuffleBar(false); return; }
            int eligible = 0; for (int i = 0; i < State.Pots.Count; i++) if (TargetedShuffle.IsEligible(State, i) && !State.Pots[i].IsEmpty) eligible++;
            if (eligible < 2) { Toast("Need at least two containers with paint to shuffle."); return; }
            if (D.freeShuffleTokens <= 0 && !Wallet.CanAfford(Cfg.ShufflePrice)) { OpenInsufficient("Targeted Shuffle", Cfg.ShufflePrice, () => { D.freeShuffleTokens++; Persist(); OnShuffleButton(); }); return; }
            SetShuffleBar(true);
            ShowOnce("shuffle", "Pick the pots to mix up. Cost: " + Cfg.ShufflePrice + " Gold, charged only if a fair rearrangement is found.");
        }

        void ConfirmShuffle()
        {
            if (!shuffleMode || shuffleMarks.Count < 2) { Toast("Select at least two containers."); return; }
            var sel = new List<int>(shuffleMarks); sel.Sort();
            var o = TargetedShuffle.Compute(State, sel, CurrentLevel * 997 + State.MoveCount * 31 + extrasAdded);
            if (!o.Ok) { Toast(o.Message); Audio.Play("invalid"); return; }
            bool free = D.freeShuffleTokens > 0;
            if (free) D.freeShuffleTokens--;
            else if (!Wallet.TrySpend(Cfg.ShufflePrice, "shuffle_purchase", CurrentLevel, "shuffle_" + Guid.NewGuid().ToString("N"))) { Toast("Not enough Gold."); return; }
            State.ApplyContents(o.Pots, o.Colors, o.Hidden);
            foreach (var i in sel) Board.SetMarked(i, false);
            SetShuffleBar(false);
            Board.RenderAll();
            foreach (var i in sel) { Board.Shake(i); Fx.Burst(Board.Root, Board.Pots[i].RestPos, Art.Paint[UnityEngine.Random.Range(0, Art.Paint.Length)], 6, 300, 24); }
            Audio.Play("reveal");
            OnPowerUsed(PowerUp.Shuffle); Analytics.Event("shuffle_used"); Analytics.Event("gold_spent", free ? "0" : Cfg.ShufflePrice.ToString()); SaveBoard();
            Toast(free ? "Shuffled! (free ad reward used)" : "Shuffled! -" + Cfg.ShufflePrice + " Gold");
            Persist();
        }

        public void OnExtraPotButton()
        {
            if (!CanAct()) return;
            if (Locked(PowerUp.ExtraPot, "Extra pot")) return;
            if (extrasAdded >= 3) { Toast("That's the maximum number of extra containers."); return; }
            if (D.freeExtraPotTokens > 0) { ConfirmAddPot(true); return; }
            if (!Wallet.CanAfford(Cfg.ExtraContainerPrice)) { OpenInsufficient("Extra Container", Cfg.ExtraContainerPrice, () => { D.freeExtraPotTokens++; Persist(); ConfirmAddPot(true); }); return; }
            OpenConfirmPurchase("Add One Empty Container", "Adds a spare empty paint pot until this level ends.", Cfg.ExtraContainerPrice, () => ConfirmAddPot(false));
        }

        void ConfirmAddPot(bool free)
        {
            if (!inGame || attemptEnded || winHandled) return;
            if (!free && !Wallet.CanAfford(Cfg.ExtraContainerPrice)) { Toast("Not enough Gold."); return; }
            State.AddEmptyPot(); Board.AddPot(); extrasAdded++;               // charge only after the pot actually exists
            if (free) D.freeExtraPotTokens--;
            else Wallet.TrySpend(Cfg.ExtraContainerPrice, "extra_container", CurrentLevel, "extra_" + Guid.NewGuid().ToString("N"));
            OnPowerUsed(PowerUp.ExtraPot); SaveBoard(); Audio.Play("complete"); Analytics.Event("extra_container_used"); Toast(free ? "Extra container added (free)!" : "Extra container added! -" + Cfg.ExtraContainerPrice + " Gold");
            Persist(); RefreshHud();
        }

        // ------------------------------------------------------------------ fail / lives flow (two confirmations)
        public void OnRestartButton()
        {
            if (!inGame || Board.Busy || PopupOpen) return;
            if (winHandled) return;
            if (!State.HasAnyMove()) { OpenLeaveConfirm(); return; }              // abandoning a dead board → life flow
            if (State.MoveCount == 0) { RestartLevel(); return; }
            OpenDialog("Restart level?", "The board goes back to the start. This does not cost a life.", ("Cancel", UI.Grey, null), ("Restart", UI.Orange, () => RestartLevel()));
        }

        public void OnBackButton()
        {
            if (!inGame) return;
            if (Board.Busy || PopupOpen) return;
            if (winHandled) { LeaveToMenu(); return; }
            if (!State.HasAnyMove()) { OpenLeaveConfirm(); return; }
            if (State.MoveCount == 0) { ClearResume(); LeaveToMenu(); return; }
            OpenDialog("Leave level?", "Your board is saved, so you can continue it from the menu. No life is lost.", ("Stay", UI.Grey, null), ("Leave", UI.Orange, LeaveToMenu));
        }

        /// <summary>Prompt 1: rescue offers. Prompt 2: life-loss confirmation.</summary>
        public void OpenStuckPrompt()
        {
            if (!inGame || attemptEnded) return;
            OpenRescue();
        }

        public void ConsumeLifeAndLeave(int attemptId)
        {
            if (attemptEnded || attemptId != AttemptId) return;   // duplicate / stale callbacks can never consume a second life
            attemptEnded = true; ClearResume();
            bool had = Lives.Consume();
            Analytics.Event("level_failed", CurrentLevel.ToString()); if (had) Analytics.Event("life_consumed");
            Log.I("LIVES", "Life consumed, now " + Lives.Lives);
            Persist();
            if (Lives.Lives > 0) StartLevel(CurrentLevel, true);
            else { LeaveToMenu(); OpenNoLives(); }
        }

        public void DebugForceWin() { if (inGame && State != null) { winHandled = false; StartCoroutine(HandleWin()); } }

        // ------------------------------------------------------------------ rewarded ads
        public void WatchRewarded(string placement, Action onReward, Action onFail = null)
        {
            Analytics.Event("ad_requested", placement);
            Ads.ShowRewarded(placement, r =>
            {
                Log.I("ADS", placement + " -> " + r);
                if (r == AdResult.Completed) { Analytics.Event("ad_completed", placement); Persist(); onReward?.Invoke(); }
                else
                {
                    Analytics.Event("ad_failed", placement + ":" + r);
                    Toast(r == AdResult.CooldownActive ? "Next ad available in " + Mathf.CeilToInt((float)AdPol.SecondsLeft) + "s." : r == AdResult.ClosedEarly ? "Ad closed early — no reward." : "No ad available right now. Nothing was charged.");
                    onFail?.Invoke();
                }
            });
        }

        // ------------------------------------------------------------------ daily + misc actions
        public void ClaimDaily()
        {
            var r = Daily.Claim(Wallet);
            if (!r.Ok) { Toast("Already claimed today. Come back tomorrow!"); return; }
            Audio.Play("coin"); Analytics.Event("daily_reward_claimed", r.Index.ToString());
            if (r.WeekComplete) { Analytics.Event("weekly_reward_completed"); D.stats.WeekTrackCompleted = true; CheckAchievements(); Toast("Full week! +" + r.Bonus + " bonus Gold"); }
            Persist();
        }

        /// <summary>Test hook: points persistence at a throw-away file and discards the loaded progress, so automated tests never touch the player's real save.</summary>
        public void UseIsolatedSaveForTests(string path)
        {
            Save = new SaveService(path); Save.Delete(); saveDirty = false;
            ResetAllProgress();
        }

        public void ResetAllProgress()
        {
            Save.Delete(); saveDirty = false;
            Wallet.Balance = 0; Wallet.Ledger = new List<GoldTransaction>(); Wallet.ClaimIds = new HashSet<string>();
            Lives.Set(Cfg.MaxLives); Daily.WeekIndex = -1; Daily.ClaimedCount = 0; Daily.LastClaimDay = -1; Daily.BonusPaid = false; Daily.EverCompletedWeek = false; AdPol.Reset();
            D.startGoldGranted = false; Wallet.Grant(Cfg.StartingGold, "starting_grant", 0, "starting_grant"); D.startGoldGranted = true;
            Persist(); FlushSave(); LeaveToMenu(); Toast("Progress reset.");
        }
    }
}
