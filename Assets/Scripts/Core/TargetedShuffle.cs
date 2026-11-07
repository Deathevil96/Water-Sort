using System;
using System.Collections.Generic;

namespace PaintSort.Core
{
    public sealed class ShuffleOutcome
    {
        public bool Ok; public string Message = "";
        public List<int> Pots = new List<int>();
        public List<List<int>> Colors = new List<List<int>>();
        public List<List<bool>> Hidden = new List<List<bool>>();
    }

    public static class TargetedShuffle
    {
        public static bool IsEligible(PuzzleState s, int i) => i >= 0 && i < s.Pots.Count && !s.Pots[i].Covered && !s.Pots[i].Complete;

        /// <summary>
        /// Redistributes the paint of the chosen pots among themselves (same per-pot unit counts, so capacity and colour totals are preserved),
        /// and only returns a result if the real solver proves the new board is still solvable. Nothing is applied here.
        /// </summary>
        public static ShuffleOutcome Compute(PuzzleState s, IList<int> selected, int seed, int maxStates = 80000)
        {
            var o = new ShuffleOutcome();
            var sel = new List<int>(selected);
            if (sel.Count < 2) { o.Message = "Select at least two containers."; return o; }
            foreach (var i in sel) if (!IsEligible(s, i)) { o.Message = "Covered or completed containers cannot be shuffled."; return o; }

            var units = new List<KeyValuePair<int, bool>>();
            var counts = new List<int>();
            foreach (var i in sel)
            {
                var p = s.Pots[i]; counts.Add(p.Count);
                for (int k = 0; k < p.Count; k++) units.Add(new KeyValuePair<int, bool>(p.Colors[k], p.Hidden[k]));
            }
            if (units.Count < 2) { o.Message = "Nothing to shuffle."; return o; }

            var rng = new DetRandom(seed);
            for (int attempt = 0; attempt < 60; attempt++)
            {
                rng.Shuffle(units);
                var newColors = new List<List<int>>(); var newHidden = new List<List<bool>>();
                int pos = 0; bool changed = false, completeFound = false;
                for (int a = 0; a < sel.Count; a++)
                {
                    var cl = new List<int>(); var hl = new List<bool>();
                    for (int k = 0; k < counts[a]; k++)
                    {
                        var u = units[pos++]; cl.Add(u.Key); hl.Add(u.Value);
                    }
                    // top is always shown
                    if (hl.Count > 0) hl[hl.Count - 1] = false;
                    var old = s.Pots[sel[a]];
                    for (int k = 0; k < cl.Count; k++) if (old.Colors[k] != cl[k]) changed = true;
                    if (cl.Count == old.Capacity && cl.TrueForAll(x => x == cl[0])) completeFound = true;
                    newColors.Add(cl); newHidden.Add(hl);
                }
                if (!changed || completeFound) continue;

                var test = s.CloneForSearch();
                test.ApplyContents(sel, newColors, newHidden);
                var sol = PuzzleSolver.Solve(test, maxStates);
                if (sol.Status != SolverStatus.Solved) continue;

                o.Ok = true; o.Pots = sel; o.Colors = newColors; o.Hidden = newHidden;
                return o;
            }
            o.Message = "No valid solvable rearrangement found. You were not charged.";
            return o;
        }
    }
}
