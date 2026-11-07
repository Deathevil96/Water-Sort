using System;
using System.Collections.Generic;

namespace PaintSort.Core
{
    public interface ITimeProvider { DateTime UtcNow { get; } }
    public sealed class SystemTimeProvider : ITimeProvider { public DateTime UtcNow => DateTime.UtcNow; }
    /// <summary>Test / debug clock: real time plus an adjustable offset.</summary>
    public sealed class OffsetTimeProvider : ITimeProvider
    {
        public TimeSpan Offset;
        public DateTime UtcNow => DateTime.UtcNow + Offset;
    }

    /// <summary>Every tunable number lives here (GDD 0.1 #6). Serialisable defaults; the game can load overrides.</summary>
    public sealed class EconomyConfig
    {
        public int StartingGold = 500;
        public int LevelReward = 20;
        public int EarlyMilestoneBonus = 50;
        public int LateMilestoneBonus = 20;
        public int EarlyMilestoneLastLevel = 50;
        public int MilestoneEvery = 10;
        public int ShufflePrice = 900;
        public int ExtraContainerPrice = 900;
        public int UndoPrice = 0;
        public int MaxLives = 5;
        public int MinutesPerLife = 30;
        public int[] DailyRewards = { 50, 100, 200, 300, 400 };
        public int WeeklyBonus = 1000;
        public int WeekLengthDays = 7;
        public int AdCooldownSeconds = 300;
        public int LeaderboardUnlockLevel = 100;
        public bool ShopEnabled = false;
        public bool RewardedLifeEnabled = true;
        public int AdLifeReward = 1;

        public int MilestoneBonus(int level)
        {
            if (level <= 0 || level % MilestoneEvery != 0) return 0;
            return level <= EarlyMilestoneLastLevel ? EarlyMilestoneBonus : LateMilestoneBonus;
        }
        public int LevelPayout(int level) => LevelReward + MilestoneBonus(level);
    }

    [Serializable]
    public sealed class GoldTransaction
    {
        public string Id; public int Amount; public string Reason; public int LevelId; public long TimestampTicks; public string ClaimId;
    }

    /// <summary>Gold wallet with an idempotent ledger. Gold never goes negative; a claim id can only be applied once.</summary>
    public sealed class GoldWallet
    {
        public int Balance;
        public List<GoldTransaction> Ledger = new List<GoldTransaction>();
        public HashSet<string> ClaimIds = new HashSet<string>();
        readonly ITimeProvider time;
        public event Action<int> Changed;
        public GoldWallet(ITimeProvider t) { time = t; }

        public bool Grant(int amount, string reason, int levelId = 0, string claimId = null)
        {
            if (amount <= 0) return false;
            if (claimId != null && !ClaimIds.Add(claimId)) return false;
            Apply(amount, reason, levelId, claimId);
            return true;
        }

        public bool CanAfford(int amount) => Balance >= amount;

        public bool TrySpend(int amount, string reason, int levelId = 0, string claimId = null)
        {
            if (amount < 0 || Balance < amount) return false;
            if (claimId != null && !ClaimIds.Add(claimId)) return false;
            if (amount == 0) return true;
            Apply(-amount, reason, levelId, claimId);
            return true;
        }

        public void SetExact(int value, string reason = "debug_set")
        {
            int d = Math.Max(0, value) - Balance;
            if (d != 0) Apply(d, reason, 0, null);
        }

        void Apply(int amount, string reason, int levelId, string claimId)
        {
            Balance = Math.Max(0, Balance + amount);
            Ledger.Add(new GoldTransaction { Id = Guid.NewGuid().ToString("N"), Amount = amount, Reason = reason, LevelId = levelId, TimestampTicks = time.UtcNow.Ticks, ClaimId = claimId });
            if (Ledger.Count > 500) Ledger.RemoveRange(0, Ledger.Count - 500);
            Changed?.Invoke(Balance);
        }
    }

    public sealed class LivesModel
    {
        readonly EconomyConfig cfg; readonly ITimeProvider time;
        public int Lives;
        public long LastRegenTicks;
        public event Action Changed;
        public LivesModel(EconomyConfig c, ITimeProvider t) { cfg = c; time = t; Lives = c.MaxLives; LastRegenTicks = t.UtcNow.Ticks; }

        public void Load(int lives, long lastRegenTicks) { Lives = Math.Max(0, Math.Min(cfg.MaxLives, lives)); LastRegenTicks = lastRegenTicks; Tick(); }

        public void Tick()
        {
            if (Lives >= cfg.MaxLives) { LastRegenTicks = time.UtcNow.Ticks; return; }
            var now = time.UtcNow.Ticks;
            if (now < LastRegenTicks) { LastRegenTicks = now; return; } // clock moved backwards: never grant free lives
            long per = TimeSpan.FromMinutes(cfg.MinutesPerLife).Ticks;
            long gained = (now - LastRegenTicks) / per;
            if (gained <= 0) return;
            int before = Lives;
            Lives = (int)Math.Min(cfg.MaxLives, Lives + gained);
            LastRegenTicks = Lives >= cfg.MaxLives ? now : LastRegenTicks + gained * per;
            if (Lives != before) Changed?.Invoke();
        }

        public bool HasLife { get { Tick(); return Lives > 0; } }

        public TimeSpan TimeToNext
        {
            get
            {
                Tick();
                if (Lives >= cfg.MaxLives) return TimeSpan.Zero;
                var left = new TimeSpan(LastRegenTicks) + TimeSpan.FromMinutes(cfg.MinutesPerLife) - new TimeSpan(time.UtcNow.Ticks);
                return left < TimeSpan.Zero ? TimeSpan.Zero : left;
            }
        }

        public bool Consume()
        {
            Tick();
            if (Lives <= 0) return false;
            if (Lives >= cfg.MaxLives) LastRegenTicks = time.UtcNow.Ticks;
            Lives--; Changed?.Invoke(); return true;
        }

        public void Add(int n) { Lives = Math.Min(cfg.MaxLives, Lives + n); Changed?.Invoke(); }
        public void Set(int n) { Lives = Math.Max(0, Math.Min(cfg.MaxLives, n)); if (Lives < cfg.MaxLives) LastRegenTicks = time.UtcNow.Ticks; Changed?.Invoke(); }
    }

    public enum DailyTileState { Claimed, Available, Upcoming, Missed }

    /// <summary>
    /// Weekly login track: five base rewards inside a 7-day week (two catch-up days), then +1000 once.
    /// Weeks are counted from a fixed epoch so the boundary is configurable (WeekLengthDays).
    /// </summary>
    public sealed class DailyRewardModel
    {
        readonly EconomyConfig cfg; readonly ITimeProvider time;
        public int WeekIndex = -1;
        public int ClaimedCount;
        public int LastClaimDay = -1;
        public bool BonusPaid;
        public bool EverCompletedWeek;
        static readonly DateTime Epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DailyRewardModel(EconomyConfig c, ITimeProvider t) { cfg = c; time = t; }

        public int Today => (int)Math.Floor((time.UtcNow - Epoch).TotalDays);
        public int CurrentWeek => Today / cfg.WeekLengthDays;

        public void Refresh()
        {
            if (WeekIndex != CurrentWeek) { WeekIndex = CurrentWeek; ClaimedCount = 0; BonusPaid = false; LastClaimDay = -1; }
        }

        public bool CanClaimToday { get { Refresh(); return ClaimedCount < cfg.DailyRewards.Length && LastClaimDay != Today; } }

        public TimeSpan TimeToReset
        {
            get { Refresh(); var end = Epoch.AddDays((WeekIndex + 1) * cfg.WeekLengthDays); return end - time.UtcNow; }
        }

        public DailyTileState TileState(int i)
        {
            Refresh();
            if (i < ClaimedCount) return DailyTileState.Claimed;
            if (i == ClaimedCount) return CanClaimToday ? DailyTileState.Available : DailyTileState.Upcoming;
            return DailyTileState.Upcoming;
        }

        public sealed class ClaimResult { public bool Ok; public int Gold; public int Bonus; public int Index; public bool WeekComplete; }

        public ClaimResult Claim(GoldWallet wallet)
        {
            Refresh();
            var r = new ClaimResult();
            if (!CanClaimToday) return r;
            int idx = ClaimedCount;
            r.Index = idx; r.Gold = cfg.DailyRewards[idx];
            wallet.Grant(r.Gold, "daily_login", 0, "daily_" + WeekIndex + "_" + idx);
            ClaimedCount++; LastClaimDay = Today; r.Ok = true;
            if (ClaimedCount >= cfg.DailyRewards.Length && !BonusPaid)
            {
                BonusPaid = true; EverCompletedWeek = true; r.WeekComplete = true; r.Bonus = cfg.WeeklyBonus;
                wallet.Grant(cfg.WeeklyBonus, "daily_week_bonus", 0, "weekbonus_" + WeekIndex);
            }
            return r;
        }

        public void ForceNewWeek() { WeekIndex = -1; Refresh(); }
        public void ForceNextDay() { LastClaimDay = -1; }
    }

    public sealed class AchievementDef
    {
        public string Id, Title, Description; public int Reward;
        public Func<PlayerStats, bool> Condition;
        public AchievementDef(string id, string title, string desc, int reward, Func<PlayerStats, bool> cond) { Id = id; Title = title; Description = desc; Reward = reward; Condition = cond; }
    }

    [Serializable]
    public sealed class PlayerStats
    {
        public int HighestCompleted;
        public int TotalCompleted;
        public int HardCompleted;
        public int SuperHardCompleted;
        public int HiddenLevelsCompleted;
        public int CoversUnlocked;
        public bool NoInvalidMoveWin;
        public bool NoUndoWin;
        public bool WeekTrackCompleted;
    }

    public static class Achievements
    {
        public static readonly List<AchievementDef> All = new List<AchievementDef>
        {
            new AchievementDef("first_pour", "First Pour", "Complete Level 1", 20, s => s.HighestCompleted >= 1),
            new AchievementDef("hang_of_it", "Getting the Hang of It", "Complete Level 10", 50, s => s.HighestCompleted >= 10),
            new AchievementDef("collector", "Colour Collector", "Complete Level 25", 75, s => s.HighestCompleted >= 25),
            new AchievementDef("apprentice", "Paint Apprentice", "Complete Level 50", 100, s => s.HighestCompleted >= 50),
            new AchievementDef("regular", "Studio Regular", "Complete Level 100", 150, s => s.HighestCompleted >= 100),
            new AchievementDef("master_mixer", "Master Mixer", "Complete Level 250", 250, s => s.HighestCompleted >= 250),
            new AchievementDef("colour_expert", "Colour Expert", "Complete Level 500", 400, s => s.HighestCompleted >= 500),
            new AchievementDef("grand_artist", "Grand Artist", "Complete Level 1000", 1000, s => s.HighestCompleted >= 1000),
            new AchievementDef("no_mistakes", "No Mistakes", "Win a level without an invalid move", 50, s => s.NoInvalidMoveWin),
            new AchievementDef("careful_planner", "Careful Planner", "Win a level without using Undo", 50, s => s.NoUndoWin),
            new AchievementDef("hard_work", "Hard Work", "Complete 10 Hard levels", 100, s => s.HardCompleted >= 10),
            new AchievementDef("super_artist", "Super Artist", "Complete 5 Super Hard levels", 150, s => s.SuperHardCompleted >= 5),
            new AchievementDef("hidden_talent", "Hidden Talent", "Complete 10 levels with hidden layers", 100, s => s.HiddenLevelsCompleted >= 10),
            new AchievementDef("cover_breaker", "Cover Breaker", "Unlock 10 covered containers", 100, s => s.CoversUnlocked >= 10),
            new AchievementDef("daily_dedication", "Daily Dedication", "Complete a full weekly login track", 0, s => s.WeekTrackCompleted),
        };
    }

    /// <summary>Single central cooldown for every non-banner ad (rewarded + interstitial).</summary>
    public sealed class AdPolicy
    {
        readonly EconomyConfig cfg; readonly ITimeProvider time;
        public long LastAdTicks;
        public AdPolicy(EconomyConfig c, ITimeProvider t) { cfg = c; time = t; }
        public bool CooldownReady => LastAdTicks == 0 || (time.UtcNow - new DateTime(LastAdTicks, DateTimeKind.Utc)).TotalSeconds >= cfg.AdCooldownSeconds;
        public double SecondsLeft => CooldownReady ? 0 : cfg.AdCooldownSeconds - (time.UtcNow - new DateTime(LastAdTicks, DateTimeKind.Utc)).TotalSeconds;
        public void MarkShown() { LastAdTicks = time.UtcNow.Ticks; }
        public void Reset() { LastAdTicks = 0; }
    }

    public static class EconomySimulator
    {
        /// <summary>Best/average/heavy booster scenarios from level 1 upwards, with and without optional daily/achievement income. Never assumes real-money gold.</summary>
        public static string Summarise(int levels)
        {
            if (levels <= 0) return "";
            var cfg = new EconomyConfig();
            var sb = new System.Text.StringBuilder();
            sb.Append("Economy simulation (").Append(levels).Append(" levels, start ").Append(cfg.StartingGold).Append("g, earned from play only):\n");
            foreach (var sc in new[] { ("best (no boosters)", 0), ("average (1 booster / 25 levels)", 25), ("heavy (1 booster / 8 levels)", 8) })
            {
                long gold = cfg.StartingGold; int bought = 0, blocked = 0; long lowest = gold;
                for (int lv = 1; lv <= levels; lv++)
                {
                    gold += cfg.LevelPayout(lv);
                    if (sc.Item2 > 0 && lv % sc.Item2 == 0)
                    {
                        if (gold >= cfg.ShufflePrice) { gold -= cfg.ShufflePrice; bought++; } else blocked++;
                    }
                    if (gold < lowest) lowest = gold;
                }
                sb.Append("  ").Append(sc.Item1).Append(": final gold ").Append(gold).Append(", boosters bought ").Append(bought).Append(", unaffordable attempts ").Append(blocked).Append(", lowest ").Append(lowest).Append("\n");
            }
            sb.Append("  Boosters are optional; every level is solvable with ordinary pours, so no progression path requires real-money gold.");
            return sb.ToString();
        }
    }
}
