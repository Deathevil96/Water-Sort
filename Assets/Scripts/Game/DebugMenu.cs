#if UNITY_EDITOR || DEBUG
using System;
using System.IO;
using System.Text;
using PaintSort.Core;
using UnityEngine;

namespace PaintSort.Game
{
    /// <summary>
    /// Development-only QA panel (GDD section 21). The whole class is compiled out of release builds.
    /// Toggle with the DBG button, or F1 / backquote in the editor.
    /// </summary>
    public sealed class DebugMenu : MonoBehaviour
    {
        Vector2 scroll; int section = -1;
        string levelField = "1", goldField = "500", potField = "0", unitField = "0", report = "";
        float guiScale = 1f;
        string[] sections = { "Level", "Gold", "Lives", "Rewards", "Obstacles", "Ads / Shop", "UI / Anim", "Audit" };

        App A => App.I;

        // The component is disabled while the panel is closed (see App.Dev.cs), so OnGUI costs nothing during normal play.
        void OnGUI()
        {
            guiScale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1));
            float sw = Screen.width / guiScale, sh = Screen.height / guiScale;

            GUILayout.BeginArea(new Rect(4, 4, Mathf.Min(sw - 8, 520), sh - 8), GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("PAINT SORT DEBUG  (dev builds only)  |  Level " + A.CurrentLevel + "  Gold " + A.Wallet.Balance + "  Lives " + A.Lives.Lives);
            if (GUILayout.Button("Close", GUILayout.Width(70))) enabled = false;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            for (int i = 0; i < sections.Length; i++) if (GUILayout.Toggle(section == i, sections[i], GUI.skin.button)) section = i; else if (section == i) section = -1;
            GUILayout.EndHorizontal();
            scroll = GUILayout.BeginScrollView(scroll);
            switch (section)
            {
                case 0: LevelUi(); break;
                case 1: GoldUi(); break;
                case 2: LivesUi(); break;
                case 3: RewardUi(); break;
                case 4: ObstacleUi(); break;
                case 5: AdShopUi(); break;
                case 6: UiAnimUi(); break;
                case 7: AuditUi(); break;
                default: GUILayout.Label("Pick a section above."); break;
            }
            if (report.Length > 0) GUILayout.TextArea(report);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------ sections
        void LevelUi()
        {
            GUILayout.BeginHorizontal(); levelField = GUILayout.TextField(levelField, GUILayout.Width(80));
            if (GUILayout.Button("Load level by ID / jump")) { if (int.TryParse(levelField, out var id)) A.StartLevel(id, true); }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Jump to next Hard")) A.StartLevel(Next(A.CurrentLevel, 5, false), true);
            if (GUILayout.Button("Jump to next Super Hard")) A.StartLevel(Next(A.CurrentLevel, 10, true), true);
            if (GUILayout.Button("Mark current level complete (grants rewards)")) A.DebugForceWin();
            if (GUILayout.Button("Reset current level")) A.RestartLevel();
            if (GUILayout.Button("Clear ALL progress")) A.ResetAllProgress();
            if (A.CurrentDef != null)
            {
                var d = A.CurrentDef;
                GUILayout.Label("Seed " + d.Seed + " | " + Progression.Label(d.Difficulty) + " | pots " + d.Containers.Count + " colours " + d.ColorCount + " hidden " + d.HiddenCount + " covered " + d.CoveredCount + " | witness " + d.Witness.Count + " moves");
                var mech = new StringBuilder("Mechanics: "); if (d.HiddenCount > 0) mech.Append("HiddenLayers "); if (d.CoveredCount > 0) mech.Append("CoveredContainers "); if (d.HiddenCount + d.CoveredCount == 0) mech.Append("none");
                GUILayout.Label(mech.ToString());
            }
            if (GUILayout.Button("Highest unlocked = max (unlock all)")) { A.D.highestUnlocked = A.MaxLevel; A.Persist(); }
        }

        static int Next(int from, int mult, bool superOnly)
        {
            for (int i = from + 1; i < 100000; i++) if (i % mult == 0 && (superOnly || i % 10 != 0)) return i;
            return from;
        }

        void GoldUi()
        {
            GUILayout.BeginHorizontal(); goldField = GUILayout.TextField(goldField, GUILayout.Width(100));
            if (GUILayout.Button("Set exact")) { if (int.TryParse(goldField, out var g)) { A.Wallet.SetExact(g); } }
            if (GUILayout.Button("Add")) { if (int.TryParse(goldField, out var g)) A.Wallet.Grant(g, "debug_add"); }
            if (GUILayout.Button("Remove")) { if (int.TryParse(goldField, out var g)) A.Wallet.TrySpend(g, "debug_remove"); }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Reset to " + A.Cfg.StartingGold)) A.Wallet.SetExact(A.Cfg.StartingGold, "debug_reset");
            if (GUILayout.Button("Test 900-Gold Shuffle purchase flow")) A.OnShuffleButton();
            if (GUILayout.Button("Test insufficient-Gold flow (sets Gold to 0)")) { A.Wallet.SetExact(0); A.OnExtraPotButton(); }
            if (GUILayout.Button("Test duplicate reward prevention"))
            {
                int before = A.Wallet.Balance; bool a = A.Wallet.Grant(20, "debug_dup", 0, "debug_dup_claim"); bool b = A.Wallet.Grant(20, "debug_dup", 0, "debug_dup_claim");
                report = "first grant=" + a + " second grant=" + b + " (expect True/False). Gold " + before + " -> " + A.Wallet.Balance;
            }
            GUILayout.Label("Recent transactions:");
            var l = A.Wallet.Ledger; for (int i = l.Count - 1; i >= Mathf.Max(0, l.Count - 12); i--) GUILayout.Label((l[i].Amount >= 0 ? "+" : "") + l[i].Amount + "  " + l[i].Reason + (l[i].LevelId > 0 ? " (L" + l[i].LevelId + ")" : ""));
        }

        void LivesUi()
        {
            GUILayout.BeginHorizontal(); for (int i = 0; i <= 5; i++) if (GUILayout.Button(i.ToString())) { A.Lives.Set(i); A.Persist(); } GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 life")) { A.Lives.Add(1); A.Persist(); }
            if (GUILayout.Button("-1 life")) { A.Lives.Consume(); A.Persist(); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            foreach (var m in new[] { 1, 30, 60, 120 }) if (GUILayout.Button("+" + m + "m")) { A.Clock.Offset += TimeSpan.FromMinutes(m); A.Lives.Tick(); A.Persist(); }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Next life in ~1 min (set timestamp)")) { A.Lives.LastRegenTicks = A.Clock.UtcNow.Ticks - TimeSpan.FromMinutes(A.Cfg.MinutesPerLife - 1).Ticks; A.Persist(); }
            if (GUILayout.Button("Simulate close/reopen (save + reload lives)")) { A.Persist(); A.Lives.Load(A.D.lives, A.D.livesLastRegenTicks); A.Persist(); }
            if (GUILayout.Button("Force failure prompt (rescue)")) A.OpenStuckPrompt();
            if (GUILayout.Button("Force two-step life-loss prompt")) A.OpenLeaveConfirm();
            if (GUILayout.Button("Duplicate fail callback test"))
            {
                int before = A.Lives.Lives; int aid = A.AttemptId; A.ConsumeLifeAndLeave(aid); A.ConsumeLifeAndLeave(aid);
                report = "Lives " + before + " -> " + A.Lives.Lives + " (expect exactly one consumed)";
            }
            GUILayout.Label("Lives " + A.Lives.Lives + "/" + A.Cfg.MaxLives + "  next in " + A.Lives.TimeToNext.ToString(@"mm\:ss"));
        }

        void RewardUi()
        {
            if (GUILayout.Button("Claim next daily reward (forces a new day)")) { A.Daily.ForceNextDay(); A.ClaimDaily(); }
            if (GUILayout.Button("Grant claims 1-5 in a row (+1000 bonus)")) { for (int i = 0; i < 5; i++) { A.Daily.ForceNextDay(); A.ClaimDaily(); } }
            if (GUILayout.Button("Reset weekly reward state")) { A.Daily.ForceNewWeek(); A.Persist(); }
            if (GUILayout.Button("Simulate week boundary (+7 days)")) { A.Clock.Offset += TimeSpan.FromDays(7); A.Daily.Refresh(); A.Persist(); }
            if (GUILayout.Button("Open daily screen")) A.OpenDaily();
            GUILayout.Label("Milestone payouts (level reward + bonus): L9=" + A.Cfg.LevelPayout(9) + " L10=" + A.Cfg.LevelPayout(10) + " L50=" + A.Cfg.LevelPayout(50) + " L60=" + A.Cfg.LevelPayout(60));
            GUILayout.Label("Achievements (click to trigger / reset):");
            foreach (var a in Achievements.All)
            {
                bool got = A.D.achievements.Contains(a.Id);
                if (GUILayout.Button((got ? "[x] " : "[ ] ") + a.Title))
                {
                    if (got) { A.D.achievements.Remove(a.Id); A.Wallet.ClaimIds.Remove("ach_" + a.Id); }
                    else { A.D.achievements.Add(a.Id); if (a.Reward > 0) A.Wallet.Grant(a.Reward, "achievement", 0, "ach_" + a.Id); }
                    A.Persist();
                }
            }
        }

        void ObstacleUi()
        {
            if (A.State == null) { GUILayout.Label("Start a level first."); return; }
            GUILayout.BeginHorizontal(); GUILayout.Label("pot"); potField = GUILayout.TextField(potField, GUILayout.Width(40)); GUILayout.Label("unit"); unitField = GUILayout.TextField(unitField, GUILayout.Width(40)); GUILayout.EndHorizontal();
            int.TryParse(potField, out var pi); int.TryParse(unitField, out var ui);
            bool valid = pi >= 0 && pi < A.State.Pots.Count;
            if (valid && GUILayout.Button("Toggle hidden on pot/unit")) { var p = A.State.Pots[pi]; if (ui >= 0 && ui < p.Count - 1) { p.Hidden[ui] = !p.Hidden[ui]; A.Board.RenderAll(); } else report = "Only buried units (not the top one) can be hidden."; }
            if (GUILayout.Button("Reveal next hidden unit")) { foreach (var p in A.State.Pots) for (int k = p.Count - 1; k >= 0; k--) if (p.Hidden[k]) { p.Hidden[k] = false; A.Board.RenderAll(); return; } }
            if (valid && GUILayout.Button("Add cover to pot")) { if (!A.State.Pots[pi].IsEmpty) { A.State.Pots[pi].Covered = true; A.Board.RenderAll(); } }
            if (valid && GUILayout.Button("Remove cover from pot")) { A.State.Pots[pi].Covered = false; A.Board.RenderAll(); }
            if (GUILayout.Button("Unlock next covered container")) { foreach (var p in A.State.Pots) if (p.Covered) { p.Covered = false; break; } A.Board.RenderAll(); }
            if (GUILayout.Button("Force completion event (unlock one cover)")) { foreach (var p in A.State.Pots) if (p.Covered) { p.Covered = false; break; } A.Board.RenderAll(); }
            A.XRay = GUILayout.Toggle(A.XRay, "X-ray: show true colours under ? and covers");
            if (GUI.changed && A.Board != null && A.State != null) A.Board.RenderAll();
        }

        void AdShopUi()
        {
            GUILayout.Label("Next rewarded ad result: " + A.MockAds.NextResult);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Available")) { A.MockAds.NextResult = AdResult.Completed; A.MockAds.InstantComplete = false; }
            if (GUILayout.Button("No fill")) A.MockAds.NextResult = AdResult.NoFill;
            if (GUILayout.Button("Failure")) A.MockAds.NextResult = AdResult.Failed;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Instant complete")) { A.MockAds.NextResult = AdResult.Completed; A.MockAds.InstantComplete = true; }
            if (GUILayout.Button("Early close")) A.MockAds.NextResult = AdResult.ClosedEarly;
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Reset 5-minute ad cooldown")) { A.AdPol.Reset(); A.Persist(); }
            GUILayout.Label("Ad cooldown: " + (A.AdPol.CooldownReady ? "ready" : Mathf.CeilToInt((float)A.AdPol.SecondsLeft) + "s left"));
            if (GUILayout.Button("Test double-gold ad callback (idempotent)"))
            {
                int b = A.Wallet.Balance; bool x = A.Wallet.Grant(40, "debug_double", 0, "debug_double_claim"); bool y = A.Wallet.Grant(40, "debug_double", 0, "debug_double_claim");
                report = "double-gold grant 1=" + x + " 2=" + y + "  Gold " + b + " -> " + A.Wallet.Balance;
            }
            A.Cfg.ShopEnabled = GUILayout.Toggle(A.Cfg.ShopEnabled, "Shop feature flag (real billing stays off; fake provider only)");
            if (GUILayout.Button("Fake purchase: first pack")) { var p = A.Purchases.Products[0]; A.Purchases.Purchase(p, (ok, tok) => { if (ok) A.Wallet.Grant(p.Gold, "iap_purchase", 0, "iap_" + tok); report = "fake purchase ok=" + ok + " token=" + tok; }); }
            if (GUILayout.Button("Open shop")) A.OpenShop();
            GUILayout.Label("Fake purchase provider compiled in: YES (this is a dev build; it is excluded from release builds)");
        }

        void UiAnimUi()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Anim 0.25x")) A.AnimSpeed = 0.25f;
            if (GUILayout.Button("Anim 1x")) A.AnimSpeed = 1f;
            if (GUILayout.Button("Anim OFF")) A.AnimSpeed = 0f;
            GUILayout.EndHorizontal();
            GUILayout.Label("Anim speed: " + A.AnimSpeed);
            if (GUILayout.Button("Force level-complete screen")) A.OpenLevelComplete(Mathf.Max(1, A.CurrentLevel), A.Cfg.LevelReward, A.Cfg.MilestoneBonus(A.CurrentLevel), new System.Collections.Generic.List<AchievementDef>());
            if (GUILayout.Button("Force failure screen")) A.OpenStuckPrompt();
            if (GUILayout.Button("Force daily reward screen")) A.OpenDaily();
            if (GUILayout.Button("Force shop screen")) A.OpenShop();
            if (GUILayout.Button("Force leaderboard screen")) A.OpenLeaderboard();
            if (GUILayout.Button("Force 'No lives' screen")) A.OpenNoLives();
            Log.Verbose = GUILayout.Toggle(Log.Verbose, "Verbose logging");
        }

        void AuditUi()
        {
            if (GUILayout.Button("Test All Levels (witness replay + special mechanics)")) RunAudit(false);
            if (GUILayout.Button("Test All Levels + solver validation")) RunAudit(true);
            GUILayout.Label("Reports are written to: " + Application.persistentDataPath);
        }

        void RunAudit(bool solver)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var rep = LevelAudit.Run(A.Catalog.All(), solver, 60000);
            try
            {
                var dir = Path.Combine(Application.persistentDataPath, "Reports"); Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "level_audit.csv"), rep.ToCsv()); File.WriteAllText(Path.Combine(dir, "level_audit.json"), rep.ToJson());
            }
            catch (Exception e) { Log.W("LEVEL", e.Message); }
            report = rep.Summary() + (rep.FailedIds.Count > 0 ? "\nFAILED: " + string.Join(",", rep.FailedIds) : "") + "\n(" + sw.ElapsedMilliseconds + " ms)";
            Log.I("LEVEL", report);
        }
    }
}
#endif
