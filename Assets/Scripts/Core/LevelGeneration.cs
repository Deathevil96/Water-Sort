using System;
using System.Collections.Generic;
using System.Text;

namespace PaintSort.Core
{
    /// <summary>Deterministic xorshift RNG so the same seed always yields the same level on every platform.</summary>
    public sealed class DetRandom
    {
        uint s;
        public DetRandom(int seed) { s = (uint)seed * 2654435761u + 0x9E3779B9u; if (s == 0) s = 1; for (int i = 0; i < 8; i++) Next(); }
        public uint Next() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return s; }
        public int Range(int minInclusive, int maxExclusive) => minInclusive + (int)(Next() % (uint)(maxExclusive - minInclusive));
        public void Shuffle<T>(IList<T> list) { for (int i = list.Count - 1; i > 0; i--) { int j = Range(0, i + 1); (list[i], list[j]) = (list[j], list[i]); } }
    }

    public struct LevelSpec
    {
        public int Colors, Empties, Hidden, Covers;
    }

    /// <summary>Configurable difficulty curve (section 6.4 of the GDD).</summary>
    public static class LevelCurve
    {
        struct Row { public int From, To, ColMin, ColMax; }
        static readonly Row[] Rows =
        {
            new Row{From=1,   To=5,    ColMin=3,  ColMax=3},
            new Row{From=6,   To=10,   ColMin=4,  ColMax=4},
            new Row{From=11,  To=20,   ColMin=4,  ColMax=5},
            new Row{From=21,  To=30,   ColMin=4,  ColMax=5},
            new Row{From=31,  To=50,   ColMin=5,  ColMax=6},
            new Row{From=51,  To=100,  ColMin=5,  ColMax=7},
            new Row{From=101, To=200,  ColMin=6,  ColMax=7},
            new Row{From=201, To=350,  ColMin=7,  ColMax=8},
            new Row{From=351, To=500,  ColMin=8,  ColMax=9},
            new Row{From=501, To=750,  ColMin=9,  ColMax=10},
            new Row{From=751, To=100000, ColMin=10, ColMax=11},
        };

        /// <summary>Hard / Super Hard boards only lose a spare pot from this level on; the opening levels stay roomy so they are easy to finish.</summary>
        const int TightBoardsFrom = 60;

        public static LevelSpec For(int level)
        {
            Row r = Rows[Rows.Length - 1];
            foreach (var row in Rows) if (level >= row.From && level <= row.To) { r = row; break; }
            int span = Math.Min(r.To, 1200) - r.From + 1;
            int span2 = r.ColMax - r.ColMin + 1;
            int colors = r.ColMin + Math.Min(span2 - 1, (level - r.From) * span2 / Math.Max(1, span));
            var diff = Progression.DifficultyFor(level);
            var spec = new LevelSpec { Colors = colors, Empties = 2 };
            if (level >= TightBoardsFrom && diff != Difficulty.Normal && colors <= 9) spec.Empties = 1; // fewer free containers on Hard / Super Hard (not in the opening levels)
            if (level >= Progression.HiddenFromLevel)
            {
                if (level == 21) spec.Hidden = 1;
                else if (level <= 30) spec.Hidden = 1 + (level % 2);
                else spec.Hidden = Math.Min(colors + 3, 2 + (level - 21) / 60 + (diff == Difficulty.Normal ? 0 : 2));
            }
            if (level >= Progression.CoversFromLevel)
            {
                if (level == 31) spec.Covers = 1;
                else if (level <= 60) spec.Covers = (level % 3 == 0) ? 0 : 1;
                else if (level <= 150) spec.Covers = (level % 4 == 0) ? 0 : 1 + (level % 2);
                else spec.Covers = (level % 5 == 0) ? 0 : 1 + (level % 3);
            }
            if (level > LateGameFrom) ApplyLateGameVariety(level, diff, ref spec);
            return spec;
        }

        // Past the end of the colour ramp every board has 10-11 colours, so variety has to come from the mix of obstacles.
        // Each level id maps deterministically to a "flavour"; levels before LateGameFrom are untouched.
        const int LateGameFrom = 750, MaxCovers = 3;      // the design allows at most 3 covered pots per level

        static void ApplyLateGameVariety(int level, Difficulty diff, ref LevelSpec spec)
        {
            switch (level % 7)
            {
                case 0: spec.Hidden = Math.Min(spec.Colors + 3, spec.Hidden + 3); spec.Covers = Math.Max(0, spec.Covers - 1); break;     // mystery-heavy
                case 3: spec.Covers = Math.Min(MaxCovers, spec.Covers + 2); spec.Hidden = Math.Max(1, spec.Hidden - 2); break;                    // cloth-heavy
                case 5: spec.Hidden = 1; spec.Covers = Math.Max(1, spec.Covers); break;                                                    // light on mystery, plain sorting
            }
            // an occasional breather: a Normal level with a spare pot, so long streaks of brutal boards still have some relief
            if (diff == Difficulty.Normal && level % 10 == 4) { spec.Empties = 3; spec.Hidden = Math.Max(0, spec.Hidden - 2); }
        }
    }

    public static class LevelGenerator
    {
        public sealed class Result { public LevelDefinition Level; public int SolutionLength; public int Attempts; public int Explored; }

        // How many solvable candidates are generated per difficulty, and how the final level is picked among them by measured playability.
        // Progress = share of colours a careful bot has sorted when it gets stuck (see DifficultyProbe); lower means harder to play.
        const int NormalCandidates = 3, HardCandidates = 8, SuperHardCandidates = 16, ProbeTrials = 10;
        // Pick position among candidates sorted hardest-first, as a fraction of the pool: 0 = the very hardest, 1 = the friendliest. Normal always takes the friendliest.
        // No difficulty spike in the opening levels: severity stays 0 (the friendliest candidate, like a Normal level) until GraceLevels, then ramps up.
        const int GraceLevels = 25, RampLevels = 225;
        const float HardStartQuantile = 1f, HardEndQuantile = 0.25f, SuperHardStartQuantile = 1f;
        static float Lerp(float a, float b, float t) => a + (b - a) * t;

        sealed class Candidate { public LevelDefinition Level; public int Moves, Explored; public float Progress; }

        /// <summary>
        /// Deterministic: same levelId always gives the same level. Every candidate is verified by the real solver (and its witness replayed against
        /// PuzzleState), so any level that is picked is solvable. Difficulty is then shaped by measured playability, not just by size.
        /// </summary>
        public static Result Generate(int levelId, int maxStates = 150000)
        {
            var diff = Progression.DifficultyFor(levelId);
            if (TutorialLevels.TryCreate(levelId, out var tutorial))
            {
                var tsol = PuzzleSolver.Solve(new PuzzleState(tutorial), maxStates);
                if (tsol.Status != SolverStatus.Solved) throw new InvalidOperationException("Tutorial level " + levelId + " is not solvable");
                tutorial.Witness = tsol.Moves;
                if (!LevelAudit.ReplayWitness(tutorial, out _)) throw new InvalidOperationException("Tutorial level " + levelId + " witness does not replay");
                return new Result { Level = tutorial, SolutionLength = tsol.Moves.Count, Attempts = 1, Explored = tsol.ExploredStates };
            }
            var spec = LevelCurve.For(levelId);
            int candidates = levelId <= 5 ? 1 : diff == Difficulty.SuperHard ? SuperHardCandidates : diff == Difficulty.Hard ? HardCandidates : NormalCandidates;

            var pool = new List<Candidate>();
            int totalAttempts = 0, explored = 0;
            for (int attempt = 0; attempt < 400 && pool.Count < candidates; attempt++)
            {
                totalAttempts++;
                var s = spec;
                if (attempt >= 150) s.Empties = 2; // fall back to roomier board if a tight one is stubborn
                if (attempt >= 250) { s.Covers = Math.Max(0, s.Covers - 1); }
                int seed = levelId * 100003 + attempt * 7919 + 13;
                var lvl = Build(levelId, diff, s, seed);
                if (lvl == null) continue;
                var state = new PuzzleState(lvl);
                var sol = PuzzleSolver.Solve(state, maxStates);
                explored += sol.ExploredStates;
                if (sol.Status != SolverStatus.Solved) continue;
                lvl.Witness = sol.Moves;
                LevelCovers.Assign(lvl);
                if (!LevelAudit.ReplayWitness(lvl, out _)) continue;
                float progress = candidates > 1 ? DifficultyProbe.CarefulProgress(lvl, ProbeTrials, seed) : 1f;
                pool.Add(new Candidate { Level = lvl, Moves = sol.Moves.Count, Explored = sol.ExploredStates, Progress = progress });
            }
            if (pool.Count == 0) throw new InvalidOperationException("Could not generate level " + levelId);

            // hardest first (lowest progress; more solver effort breaks ties)
            pool.Sort((x, y) => x.Progress != y.Progress ? x.Progress.CompareTo(y.Progress) : y.Explored.CompareTo(x.Explored));
            // severity ramps up with the level number so a "Hard" level at level 15 is a gentle bump and one at level 500 is a real wall
            float severity = Math.Min(1f, Math.Max(0f, (levelId - GraceLevels) / (float)RampLevels));
            int pick = diff == Difficulty.SuperHard ? (int)(pool.Count * Lerp(SuperHardStartQuantile, 0f, severity))
                     : diff == Difficulty.Hard ? (int)(pool.Count * Lerp(HardStartQuantile, HardEndQuantile, severity))
                     : pool.Count - 1;
            var best = pool[Math.Min(pick, pool.Count - 1)];
            return new Result { Level = best.Level, SolutionLength = best.Moves, Attempts = totalAttempts, Explored = explored };
        }

        static int CountInitialMoves(PuzzleState st)
        {
            int n = 0;
            for (int i = 0; i < st.Pots.Count; i++) for (int j = 0; j < st.Pots.Count; j++) if (i != j && st.CanMove(i, j)) n++;
            return n;
        }

        static LevelDefinition Build(int levelId, Difficulty diff, LevelSpec spec, int seed)
        {
            var rng = new DetRandom(seed);
            int filled = spec.Colors, total = spec.Colors + spec.Empties;
            var units = new List<int>();
            for (int c = 0; c < spec.Colors; c++) for (int k = 0; k < 4; k++) units.Add(c);
            rng.Shuffle(units);

            // colour identities are also shuffled so level 1 isn't always red/blue/yellow in the same order
            var palette = new List<int>(); for (int i = 0; i < spec.Colors; i++) palette.Add(i);
            rng.Shuffle(palette);

            var lvl = new LevelDefinition { LevelId = levelId, Difficulty = diff, Seed = seed };
            for (int i = 0; i < total; i++)
            {
                var c = new ContainerDef { Capacity = 4 };
                if (i < filled)
                    for (int k = 0; k < 4; k++) { c.Colors.Add(palette[units[i * 4 + k]]); c.Hidden.Add(false); }
                lvl.Containers.Add(c);
            }

            // no container starts complete, and no container is a trivial 3+1 gift on the easiest tutorial levels
            foreach (var c in lvl.Containers)
                if (c.Colors.Count == 4 && c.Colors[0] == c.Colors[1] && c.Colors[1] == c.Colors[2] && c.Colors[2] == c.Colors[3]) return null;

            // empty containers are placed at random positions rather than always at the end
            var order = new List<int>(); for (int i = 0; i < total; i++) order.Add(i);
            rng.Shuffle(order);
            var placed = new List<ContainerDef>(); foreach (var i in order) placed.Add(lvl.Containers[i]);
            lvl.Containers = placed;

            // hidden layers: only buried layers (never the top) so the player always sees something to act on
            if (spec.Hidden > 0)
            {
                var cand = new List<KeyValuePair<int, int>>();
                for (int i = 0; i < lvl.Containers.Count; i++)
                    for (int k = 0; k < lvl.Containers[i].Colors.Count - 1; k++) cand.Add(new KeyValuePair<int, int>(i, k));
                rng.Shuffle(cand);
                int h = Math.Min(spec.Hidden, cand.Count);
                for (int n = 0; n < h; n++) lvl.Containers[cand[n].Key].Hidden[cand[n].Value] = true;
            }

            // covered containers: full pots only, never empties
            if (spec.Covers > 0)
            {
                var full = new List<int>();
                for (int i = 0; i < lvl.Containers.Count; i++) if (lvl.Containers[i].Colors.Count == 4) full.Add(i);
                rng.Shuffle(full);
                int cv = Math.Min(spec.Covers, Math.Max(0, full.Count - 2));
                for (int n = 0; n < cv; n++) lvl.Containers[full[n]].Covered = true;
            }
            return lvl;
        }
    }

    /// <summary>
    /// Gives every cloth-covered pot the colour that unlocks it. The colour is taken from the verified solution: replaying it, the first completed colour
    /// uncovers the first cover, and so on. So the key colour is always completable WITHOUT the covered pot (a colour that lives inside the pot can never
    /// be its own key), and every level stays solvable. Deterministic, so the catalogue file needs no extra data.
    /// </summary>
    public static class LevelCovers
    {
        public static void Assign(LevelDefinition l)
        {
            if (l.CoveredCount == 0) return;
            foreach (var c in l.Containers) c.CoverColor = -1;
            var st = new PuzzleState(l);
            foreach (var m in l.Witness)
            {
                var r = st.TryMove(m.From, m.To);
                if (!r.Valid) return;
                foreach (int u in r.UnlockedPots) l.Containers[u].CoverColor = r.Color;
            }
        }
    }

    /// <summary>Compact text serialisation of the catalog (version-controlled content, loaded from Resources).</summary>
    public static class LevelCodec
    {
        // line: id;difficultyChar;seed;pot,pot,...;from>to from>to ...
        // pot: "-" for empty, otherwise colour ids separated by '.', prefix 'h' on hidden units, prefix '#' on a covered pot
        public static string Encode(LevelDefinition l)
        {
            var sb = new StringBuilder();
            sb.Append(l.LevelId).Append(';').Append((int)l.Difficulty).Append(';').Append(l.Seed).Append(';');
            for (int i = 0; i < l.Containers.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var c = l.Containers[i];
                if (c.Covered) sb.Append('#');
                if (c.Colors.Count == 0) sb.Append('-');
                for (int k = 0; k < c.Colors.Count; k++)
                {
                    if (k > 0) sb.Append('.');
                    if (c.Hidden[k]) sb.Append('h');
                    sb.Append(c.Colors[k]);
                }
            }
            sb.Append(';');
            for (int i = 0; i < l.Witness.Count; i++) { if (i > 0) sb.Append(' '); sb.Append(l.Witness[i].From).Append('>').Append(l.Witness[i].To); }
            return sb.ToString();
        }

        public static LevelDefinition Decode(string line)
        {
            var parts = line.Split(';');
            var l = new LevelDefinition { LevelId = int.Parse(parts[0]), Difficulty = (Difficulty)int.Parse(parts[1]), Seed = int.Parse(parts[2]) };
            foreach (var pot in parts[3].Split(','))
            {
                var c = new ContainerDef();
                string p = pot;
                if (p.StartsWith("#")) { c.Covered = true; p = p.Substring(1); }
                if (p != "-")
                    foreach (var u in p.Split('.'))
                    {
                        bool h = u.StartsWith("h");
                        c.Hidden.Add(h); c.Colors.Add(int.Parse(h ? u.Substring(1) : u));
                    }
                l.Containers.Add(c);
            }
            if (parts.Length > 4 && parts[4].Length > 0)
                foreach (var m in parts[4].Split(' ')) { var ft = m.Split('>'); l.Witness.Add(new Move(int.Parse(ft[0]), int.Parse(ft[1]))); }
            LevelCovers.Assign(l);
            return l;
        }
    }

    public sealed class LevelCatalog
    {
        // Lines are indexed by id up-front (cheap) and decoded on first use, so a 5,000-level catalog costs almost nothing at startup.
        readonly Dictionary<int, string> lines = new Dictionary<int, string>();
        readonly Dictionary<int, LevelDefinition> decoded = new Dictionary<int, LevelDefinition>();
        public int Count => lines.Count;
        public int MaxLevel { get; private set; }

        public static LevelCatalog Parse(string text)
        {
            var cat = new LevelCatalog();
            int pos = 0;
            while (pos < text.Length)
            {
                int end = text.IndexOf('\n', pos);
                if (end < 0) end = text.Length;
                int start = pos; pos = end + 1;
                while (start < end && char.IsWhiteSpace(text[start])) start++;
                while (end > start && char.IsWhiteSpace(text[end - 1])) end--;
                if (end <= start || text[start] == '#') continue;
                int semi = text.IndexOf(';', start, end - start);
                if (semi < 0 || !int.TryParse(text.Substring(start, semi - start), out int id)) continue;
                cat.lines[id] = text.Substring(start, end - start);
                if (id > cat.MaxLevel) cat.MaxLevel = id;
            }
            return cat;
        }

        public bool Has(int id) => lines.ContainsKey(id);

        /// <summary>Returns a fresh copy so the stored definition stays immutable.</summary>
        public LevelDefinition Get(int id)
        {
            if (!decoded.TryGetValue(id, out var def)) { def = LevelCodec.Decode(lines[id]); decoded[id] = def; }
            return def.Clone();
        }

        public IEnumerable<LevelDefinition> All() { for (int i = 1; i <= MaxLevel; i++) if (lines.ContainsKey(i)) yield return Get(i); }
    }
}
