using System;
using System.Collections.Generic;
using System.IO;
using PaintSort.Core;
using UnityEngine;

namespace PaintSort.Game
{
    /// <summary>Structured, category-tagged logging. Verbosity configurable; nothing logs per frame.</summary>
    public static class Log
    {
        public static bool Verbose = false;
        public static void I(string cat, string msg) { if (Verbose || Application.isEditor || Debug.isDebugBuild) Debug.Log("[" + cat + "] " + msg); }
        public static void W(string cat, string msg) { Debug.LogWarning("[" + cat + "] " + msg); }
    }

    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public bool startGoldGranted;
        public int currentLevel = 1;
        public int highestUnlocked = 1;
        public List<int> completed = new List<int>();
        public int gold;
        public List<GoldTransaction> ledger = new List<GoldTransaction>();
        public List<string> claimIds = new List<string>();
        public int lives = 5;
        public long livesLastRegenTicks;
        public int dailyWeek = -1, dailyClaimed, dailyLastDay = -1;
        public bool dailyBonusPaid, dailyEverCompleted;
        public List<string> achievements = new List<string>();
        public PlayerStats stats = new PlayerStats();
        public long lastAdTicks;
        public bool musicOn = true, sfxOn = true, hapticsOn = true, colorAssist, reducedMotion;
        public List<string> seen = new List<string>();
        public bool leaderboardUnlockedLogged;
        public int freeShuffleTokens, freeExtraPotTokens;
        public long infiniteLivesUntilTicks;
        // resume-after-restart: the level being played and its serialized board (see PuzzleState.Serialize)
        public int resumeLevel;
        public string resumeBoard = "";
        public string language = "en";
    }

    public sealed class SaveService
    {
        readonly string path;
        public SaveData Data = new SaveData();

        /// <param name="pathOverride">Alternate file (used by tests so the player's real save is never touched).</param>
        public SaveService(string pathOverride = null) { path = pathOverride ?? Path.Combine(Application.persistentDataPath, "paintsort_save.json"); }

        public void Load()
        {
            try
            {
                if (!File.Exists(path)) return;
                var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (d != null) { Data = d; Migrate(); }
            }
            catch (Exception e)
            {
                Log.W("SAVE", "Corrupt save, backing up and starting fresh: " + e.Message);
                try { File.Copy(path, path + ".corrupt", true); } catch { }
                Data = new SaveData();
            }
        }

        void Migrate() { if (Data.version < 1) Data.version = 1; }

        public void Save()
        {
            try { File.WriteAllText(path + ".tmp", JsonUtility.ToJson(Data)); File.Copy(path + ".tmp", path, true); File.Delete(path + ".tmp"); }
            catch (Exception e) { Log.W("SAVE", "Save failed: " + e.Message); }
        }

        public void Delete() { try { if (File.Exists(path)) File.Delete(path); } catch { } Data = new SaveData(); }
    }

    // ----------------------------------------------------------------------- analytics
    public interface IAnalytics { void Event(string name, string detail = null); }
    public sealed class LogAnalytics : IAnalytics
    {
        public void Event(string name, string detail = null) { if (Log.Verbose) Debug.Log("[ANALYTICS] " + name + (detail != null ? " " + detail : "")); }
    }

    // ----------------------------------------------------------------------- ads
    public enum AdResult { Completed, NoFill, Failed, ClosedEarly, CooldownActive }

    public interface IAdService
    {
        bool BannerVisible { get; }
        void ShowRewarded(string placement, Action<AdResult> done);
        void ShowInterstitial(Action done);
    }

    /// <summary>Development stand-in: shows a clearly labelled fake ad. Replace with a real SDK implementation of IAdService; test ad units only until release.</summary>
    public sealed class MockAdService : IAdService
    {
        public AdResult NextResult = AdResult.Completed;
        public bool InstantComplete;
        public bool BannerVisible => true;
        readonly AdPolicy policy; readonly Func<Action<AdResult>, Action> fakeAdUi;
        readonly HashSet<string> paid = new HashSet<string>();
        public MockAdService(AdPolicy p, Func<Action<AdResult>, Action> showFakeAd) { policy = p; fakeAdUi = showFakeAd; }

        public void ShowRewarded(string placement, Action<AdResult> done)
        {
            if (!policy.CooldownReady) { done(AdResult.CooldownActive); return; }
            if (NextResult == AdResult.NoFill || NextResult == AdResult.Failed) { done(NextResult); return; }
            if (NextResult == AdResult.ClosedEarly) { done(AdResult.ClosedEarly); return; }
            if (InstantComplete) { policy.MarkShown(); done(AdResult.Completed); return; }
            bool delivered = false;
            fakeAdUi(r =>
            {
                if (delivered) return; delivered = true;               // reward callbacks are idempotent
                if (r == AdResult.Completed) policy.MarkShown();
                done(r);
            });
        }

        public void ShowInterstitial(Action done)
        {
            if (!policy.CooldownReady) { done?.Invoke(); return; }
            fakeAdUi(r => { if (r == AdResult.Completed) policy.MarkShown(); done?.Invoke(); });
        }
    }

    // ----------------------------------------------------------------------- purchases (scaffold; real billing stays off behind a flag)
    public sealed class GoldPack
    {
        public string ProductId, Title; public int Gold; public string PlaceholderPrice; public bool OneTime;
        /// <summary>Power-ups and perks bundled with the gold (all zero for a plain gold pack).</summary>
        public int Shuffles, ExtraPots, InfiniteMinutes;
        public bool IsBundle => Shuffles > 0 || ExtraPots > 0 || InfiniteMinutes > 0;
    }

    public interface IPurchaseService
    {
        bool Available { get; }
        List<GoldPack> Products { get; }
        void Purchase(GoldPack pack, Action<bool, string> done);
        void Restore(Action<string> done);
    }

    public sealed class DisabledPurchaseService : IPurchaseService
    {
        public bool Available => false;
        public List<GoldPack> Products { get; } = new List<GoldPack>
        {
            // bundles: gold + power-up tokens + a timed infinite-lives perk (placeholder prices; the store supplies the real ones)
            new GoldPack{ProductId="bundle_starter",    Title="Starter Bundle",    Gold=1000,  Shuffles=1,  ExtraPots=1,  InfiniteMinutes=60,   PlaceholderPrice="$0.99", OneTime=true},
            new GoldPack{ProductId="bundle_apprentice", Title="Apprentice Bundle", Gold=2000,  Shuffles=1,  ExtraPots=1,  InfiniteMinutes=180,  PlaceholderPrice="$4.99"},
            new GoldPack{ProductId="bundle_magician",   Title="Magician Bundle",   Gold=4000,  Shuffles=3,  ExtraPots=3,  InfiniteMinutes=360,  PlaceholderPrice="$9.99"},
            new GoldPack{ProductId="bundle_wizard",     Title="Wizard Bundle",     Gold=10000, Shuffles=6,  ExtraPots=6,  InfiniteMinutes=720,  PlaceholderPrice="$19.99"},
            new GoldPack{ProductId="bundle_archmage",   Title="Archmage Bundle",   Gold=25000, Shuffles=12, ExtraPots=12, InfiniteMinutes=1440, PlaceholderPrice="$49.99"},
            // plain gold
            new GoldPack{ProductId="gold_small",  Title="Small Gold Pack",  Gold=1000,  PlaceholderPrice="$0.99"},
            new GoldPack{ProductId="gold_medium", Title="Medium Gold Pack", Gold=3000,  PlaceholderPrice="$2.99"},
            new GoldPack{ProductId="gold_large",  Title="Large Gold Pack",  Gold=8000,  PlaceholderPrice="$7.99"},
            new GoldPack{ProductId="gold_mega",   Title="Mega Gold Pack",   Gold=20000, PlaceholderPrice="$19.99"},
        };
        public void Purchase(GoldPack pack, Action<bool, string> done) { done(false, "Store billing is not enabled yet."); }
        public void Restore(Action<string> done) { done("Nothing to restore (billing disabled)."); }
    }

#if UNITY_EDITOR || DEBUG
    /// <summary>Development-only. Compiled out of release builds, so it can never ship.</summary>
    public sealed class FakePurchaseService : IPurchaseService
    {
        readonly DisabledPurchaseService baseSvc = new DisabledPurchaseService();
        public bool Available => true;
        public List<GoldPack> Products => baseSvc.Products;
        public void Purchase(GoldPack pack, Action<bool, string> done) { done(true, "FAKE-" + Guid.NewGuid().ToString("N")); }
        public void Restore(Action<string> done) { done("Fake provider: nothing to restore."); }
    }
#endif

    // ----------------------------------------------------------------------- leaderboard
    public sealed class LeaderboardEntry { public string Name; public int HighestLevel, TotalLevels; public bool IsYou; }

    public interface ILeaderboardService
    {
        bool Connected { get; }
        List<LeaderboardEntry> GetEntries(int myHighest, int myTotal);
    }

    /// <summary>Local mock for editor / offline. Real Google Play Games / Game Center implementations plug in here.</summary>
    public sealed class LocalMockLeaderboard : ILeaderboardService
    {
        public bool Connected => false;
        static readonly string[] Names = { "BrushBuddy", "Pigment Pete", "Canvas Cat", "Splatter", "Mixy", "Palette Pal", "Dabber", "Hue Hero" };
        public List<LeaderboardEntry> GetEntries(int myHighest, int myTotal)
        {
            var list = new List<LeaderboardEntry>();
            var rng = new DetRandom(77);
            for (int i = 0; i < Names.Length; i++) list.Add(new LeaderboardEntry { Name = Names[i], HighestLevel = 80 + rng.Range(0, 400), TotalLevels = 70 + rng.Range(0, 380) });
            list.Add(new LeaderboardEntry { Name = "You", HighestLevel = myHighest, TotalLevels = myTotal, IsYou = true });
            list.Sort((a, b) => a.HighestLevel != b.HighestLevel ? b.HighestLevel.CompareTo(a.HighestLevel) : b.TotalLevels.CompareTo(a.TotalLevels));
            return list;
        }
    }

    // ----------------------------------------------------------------------- audio + haptics (procedural placeholders so the game has feedback without imported assets)
    public sealed class AudioService
    {
        readonly AudioSource src, musicSrc;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        System.Threading.Tasks.Task<float[]> musicTask;
        bool musicOn = true;
        public bool SfxOn = true;
        public bool HapticsOn { get => Haptics.Enabled; set => Haptics.Enabled = value; }

        public AudioService(GameObject host)
        {
            src = host.AddComponent<AudioSource>(); src.playOnAwake = false;
            musicSrc = host.AddComponent<AudioSource>(); musicSrc.playOnAwake = false; musicSrc.loop = true; musicSrc.volume = 0.32f;
        }

        public bool MusicOn
        {
            get => musicOn;
            set { musicOn = value; if (musicSrc.clip != null) { if (value) musicSrc.Play(); else musicSrc.Pause(); } }
        }

        /// <summary>Starts the background loop: a clip named "music" in Resources/Audio if present, otherwise a synthesized one rendered on a worker thread.</summary>
        public void StartMusic()
        {
            var custom = Resources.Load<AudioClip>("Audio/music");
            if (custom != null) { musicSrc.clip = custom; if (musicOn) musicSrc.Play(); return; }
            musicTask = System.Threading.Tasks.Task.Run(() => MusicSynth.Render());
        }

        /// <summary>Call once per frame; finishes music start-up when the worker thread is done (AudioClip must be created on the main thread).</summary>
        public void Tick()
        {
            if (musicTask == null || !musicTask.IsCompleted) return;
            var task = musicTask; musicTask = null;
            if (task.IsFaulted) { Log.W("AUDIO", "Music synthesis failed: " + task.Exception?.GetBaseException().Message); return; }
            var data = task.Result;
            var clip = AudioClip.Create("music", data.Length, 1, MusicSynth.SampleRate, false); clip.SetData(data, 0);
            musicSrc.clip = clip; if (musicOn) musicSrc.Play();
        }

        AudioClip Get(string name)
        {
            if (clips.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + name);
            if (c == null)
            {
                switch (name)
                {
                    case "tap": c = Tone(new[] { (700f, 0.05f) }, 0.35f); break;
                    case "select": c = Tone(new[] { (520f, 0.05f), (780f, 0.07f) }, 0.35f); break;
                    case "pour": c = Noise(0.55f, 0.18f); break;
                    case "invalid": c = Tone(new[] { (200f, 0.09f), (150f, 0.12f) }, 0.4f); break;
                    case "complete": c = Tone(new[] { (660f, 0.08f), (880f, 0.08f), (1100f, 0.14f) }, 0.4f); break;
                    case "win": c = Tone(new[] { (523f, 0.12f), (659f, 0.12f), (784f, 0.12f), (1046f, 0.3f) }, 0.45f); break;
                    case "coin": c = Tone(new[] { (1200f, 0.05f), (1600f, 0.12f) }, 0.35f); break;
                    case "reveal": c = Tone(new[] { (400f, 0.06f), (900f, 0.12f) }, 0.35f); break;
                    default: c = Tone(new[] { (600f, 0.05f) }, 0.3f); break;
                }
            }
            clips[name] = c; return c;
        }

        static AudioClip Tone(IList<(float f, float d)> notes, float vol)
        {
            int rate = 22050; float total = 0; foreach (var n in notes) total += n.d;
            var data = new float[(int)(total * rate)]; int idx = 0;
            foreach (var n in notes)
            {
                int len = (int)(n.d * rate);
                for (int i = 0; i < len && idx < data.Length; i++, idx++)
                {
                    float t = i / (float)rate; float env = Mathf.Clamp01(1 - i / (float)len); env *= Mathf.Clamp01(i / 120f);
                    data[idx] = Mathf.Sin(2 * Mathf.PI * n.f * t) * env * vol;
                }
            }
            var clip = AudioClip.Create("tone", data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }

        static AudioClip Noise(float dur, float vol)
        {
            int rate = 22050; var data = new float[(int)(dur * rate)]; var rng = new System.Random(5); float lp = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float env = Mathf.Sin(Mathf.PI * i / data.Length);
                lp += ((float)rng.NextDouble() * 2 - 1 - lp) * 0.12f; // low-passed noise ≈ trickling paint
                data[i] = lp * env * vol * 3f;
            }
            var clip = AudioClip.Create("noise", data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }

        public void Play(string name, float pitch = 1f)
        {
            if (!SfxOn) return;
            src.pitch = pitch; src.PlayOneShot(Get(name));
        }

        public void Vibrate() => Haptics.Light();
    }
}
