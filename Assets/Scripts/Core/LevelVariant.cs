using System;
using System.Collections.Generic;
using System.Text;

namespace PaintSort.Core
{
    /// <summary>
    /// A fresh-looking copy of a level for every attempt: the layout (pot count, empties, hidden layers, cloths) never changes, but the paint colours do,
    /// and uncovered pots with the same shape swap contents. The copy is the same puzzle in disguise (an exact relabelling), so it is exactly as
    /// solvable and exactly as hard as the audited original, and the stored solution is carried over (remapped) for tests and tools.
    /// </summary>
    public static class LevelVariant
    {
        /// <summary>First level that varies; the hand-made tutorial levels 1-4 always look the same.</summary>
        public const int FirstVariedLevel = 5;

        public static LevelDefinition Make(LevelDefinition src, Random rng)
        {
            if (src.LevelId < FirstVariedLevel) return src;
            var l = src.Clone();

            // 1. new colours: a random permutation of the colour ids in use
            var ids = new List<int>();
            foreach (var c in src.Containers) foreach (var k in c.Colors) if (!ids.Contains(k)) ids.Add(k);
            var shuffled = new List<int>(ids); Shuffle(shuffled, rng);
            var map = new Dictionary<int, int>();
            for (int i = 0; i < ids.Count; i++) map[ids[i]] = shuffled[i];
            foreach (var c in l.Containers)
            {
                for (int k = 0; k < c.Colors.Count; k++) c.Colors[k] = map[c.Colors[k]];
                if (c.CoverColor >= 0 && map.TryGetValue(c.CoverColor, out int nk)) c.CoverColor = nk;
            }

            // 2. uncovered, filled pots with the same hidden-layer pattern swap contents (their positions stay, so the layout is untouched)
            var groups = new Dictionary<string, List<int>>();
            for (int i = 0; i < l.Containers.Count; i++)
            {
                var c = l.Containers[i];
                if (c.Covered || c.Colors.Count == 0) continue;
                var key = new StringBuilder().Append(c.Capacity).Append(':');
                foreach (var h in c.Hidden) key.Append(h ? '1' : '0');
                string ks = key.ToString();
                if (!groups.TryGetValue(ks, out var g)) groups[ks] = g = new List<int>();
                g.Add(i);
            }
            var from = new int[l.Containers.Count];                 // from[i] = which old pot's contents now sit in pot i
            for (int i = 0; i < from.Length; i++) from[i] = i;
            foreach (var g in groups.Values)
            {
                var order = new List<int>(g); Shuffle(order, rng);
                for (int n = 0; n < g.Count; n++) from[g[n]] = order[n];
            }
            var snapshot = new List<ContainerDef>(l.Containers);
            for (int i = 0; i < from.Length; i++) if (from[i] != i) { l.Containers[i] = snapshot[from[i]]; }
            var where = new int[from.Length];                       // where[old] = new index of that pot's contents
            for (int i = 0; i < from.Length; i++) where[from[i]] = i;
            for (int i = 0; i < l.Witness.Count; i++) l.Witness[i] = new Move(where[l.Witness[i].From], where[l.Witness[i].To]);
            return l;
        }

        static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); T t = list[i]; list[i] = list[j]; list[j] = t; }
        }
    }
}
