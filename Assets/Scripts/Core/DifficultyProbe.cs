using System;
using System.Collections.Generic;

namespace PaintSort.Core
{
    /// <summary>
    /// Objective difficulty measurement: a "casual player" bot that plays with simple, human-like heuristics (no planning, no undo)
    /// and reports how often it clears a level. A level the bot rarely clears is a level that punishes careless play.
    /// This is independent of the solver, which always finds an answer and so says nothing about how hard a level feels.
    /// </summary>
    public static class DifficultyProbe
    {
        const int MaxSteps = 220;

        /// <summary>Fraction of playouts (0..1) in which the casual bot completes the level.</summary>
        public static float CasualWinRate(LevelDefinition def, int trials, int seed) => WinRate(def, trials, seed, false);

        /// <summary>Same, for a "careful" player that looks one move ahead and prefers tidy boards (still no undo, no deep planning).</summary>
        public static float CarefulWinRate(LevelDefinition def, int trials, int seed) => WinRate(def, trials, seed, true);

        /// <summary>
        /// Average fraction of the colours the careful bot has fully sorted when it gets stuck (a win counts as 1.0).
        /// Unlike a plain win rate this still separates levels when every playout fails, so it can rank hard late-game boards.
        /// Lower = harder to play.
        /// </summary>
        public static float CarefulProgress(LevelDefinition def, int trials, int seed)
        {
            var rng = new DetRandom(seed);
            var moves = new List<Move>(64); var weights = new List<int>(64);
            float sum = 0;
            for (int t = 0; t < trials; t++) sum += Playout(def, rng, moves, weights, true);
            return sum / trials;
        }

        static float WinRate(LevelDefinition def, int trials, int seed, bool careful)
        {
            int wins = 0;
            var rng = new DetRandom(seed);
            var moves = new List<Move>(64);
            var weights = new List<int>(64);
            for (int t = 0; t < trials; t++)
                if (Playout(def, rng, moves, weights, careful) >= 1f) wins++;
            return wins / (float)trials;
        }

        static int ColourCount(LevelDefinition def)
        {
            var seen = new HashSet<int>();
            foreach (var c in def.Containers) foreach (var col in c.Colors) seen.Add(col);
            return Math.Max(1, seen.Count);
        }

        static float Progress(PuzzleState st, int colours)
        {
            int done = 0; foreach (var p in st.Pots) if (p.Complete) done++;
            return Math.Min(1f, done / (float)colours);
        }

        /// <summary>How "tidy" a board is: fewer colour blocks, more empty pots and more finished pots are better.</summary>
        static int Tidiness(PuzzleState st)
        {
            int blocks = 0, empties = 0, done = 0;
            foreach (var p in st.Pots)
            {
                if (p.IsEmpty) { if (!p.Covered) empties++; continue; }
                for (int i = 0; i < p.Count; i++) if (i == 0 || p.Colors[i] != p.Colors[i - 1]) blocks++;
                if (p.Complete) done++;
            }
            return -blocks * 10 + empties * 6 + done * 8;
        }

        static float Playout(LevelDefinition def, DetRandom rng, List<Move> moves, List<int> weights, bool careful)
        {
            var st = new PuzzleState(def);
            int colours = ColourCount(def);
            int lastFrom = -1, lastTo = -1;
            for (int step = 0; step < MaxSteps; step++)
            {
                if (st.IsWon) return 1f;
                moves.Clear(); weights.Clear();
                int total = 0;
                int n = st.Pots.Count;
                for (int i = 0; i < n; i++)
                {
                    var src = st.Pots[i];
                    if (src.IsEmpty) continue;
                    int group = st.TopGroupSize(i);
                    for (int j = 0; j < n; j++)
                    {
                        if (i == j || !st.CanMove(i, j)) continue;
                        var dst = st.Pots[j];
                        if (i == lastTo && j == lastFrom) continue;                       // a person doesn't immediately undo their own pour
                        if (dst.IsEmpty && group == src.Count) continue;                  // moving a whole pot into an empty one gains nothing
                        int w = dst.IsEmpty ? 1 : 6;                                      // people prefer stacking onto a matching colour
                        int amount = Math.Min(group, dst.Capacity - dst.Count);
                        if (!dst.IsEmpty && dst.Count + amount == dst.Capacity && AllSame(dst, src.TopColor)) w += 12;   // completing a pot is very attractive
                        if (amount == group && src.Count > group && src.Hidden[src.Count - group - 1]) w += 3;            // uncovering a hidden layer is attractive
                        moves.Add(new Move(i, j)); weights.Add(w); total += w;
                    }
                }
                if (moves.Count == 0) return Progress(st, colours);
                int k = 0;
                if (careful && rng.Range(0, 100) < 80)
                {
                    // look one move ahead and take the tidiest result (ties broken by the stacking preference above)
                    int bestScore = int.MinValue;
                    for (int c = 0; c < moves.Count; c++)
                    {
                        var probe = st.CloneForSearch();
                        probe.TryMove(moves[c].From, moves[c].To);
                        int sc = Tidiness(probe) * 4 + weights[c];
                        if (sc > bestScore) { bestScore = sc; k = c; }
                    }
                }
                else
                {
                    int pick = rng.Range(0, total);
                    while (pick >= weights[k]) { pick -= weights[k]; k++; }
                }
                var m = moves[k];
                st.TryMove(m.From, m.To);
                lastFrom = m.From; lastTo = m.To;
            }
            return st.IsWon ? 1f : Progress(st, colours);
        }

        static bool AllSame(PuzzleState.Pot p, int color)
        {
            for (int i = 0; i < p.Count; i++) if (p.Colors[i] != color) return false;
            return true;
        }
    }
}
