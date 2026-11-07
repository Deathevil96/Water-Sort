using System.Collections.Generic;

namespace PaintSort.Core
{
    /// <summary>
    /// Exhaustive "can the player get stuck?" analysis for small boards. A level is <b>forgiving</b> when every board reachable by legal moves
    /// can still be finished, so no power-up (undo, shuffle, extra pot) is ever needed. The opening levels must be forgiving.
    /// Only practical for small boards (tens of thousands of positions); returns "unknown" if the search budget is exceeded.
    /// </summary>
    public static class DeadEndAnalysis
    {
        public sealed class Result
        {
            public bool Complete;          // false: the position budget was exceeded, the other fields are only partial
            public int Positions;          // distinct reachable boards
            public int DeadEnds;           // reachable boards from which no sequence of moves can win
            public bool Forgiving => Complete && DeadEnds == 0;
        }

        public static Result Analyse(LevelDefinition def, int maxPositions = 200000)
        {
            var index = new Dictionary<string, int>();
            var successors = new List<List<int>>();
            var winning = new List<bool>();
            var states = new List<PuzzleState>();

            int Add(PuzzleState s)
            {
                string key = Key(s);
                if (index.TryGetValue(key, out int id)) return id;
                id = states.Count; index[key] = id; states.Add(s); successors.Add(new List<int>()); winning.Add(s.IsWon);
                return id;
            }

            Add(new PuzzleState(def));
            for (int cur = 0; cur < states.Count; cur++)
            {
                if (states.Count > maxPositions) return new Result { Complete = false, Positions = states.Count };
                var s = states[cur];
                if (winning[cur]) continue;
                int n = s.Pots.Count;
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                    {
                        if (i == j || !s.CanMove(i, j)) continue;
                        var next = s.CloneForSearch();
                        next.TryMove(i, j);
                        successors[cur].Add(Add(next));
                    }
            }

            // positions that can reach a win: propagate backwards until nothing changes
            int count = states.Count;
            var canWin = new bool[count];
            for (int i = 0; i < count; i++) canWin[i] = winning[i];
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < count; i++)
                {
                    if (canWin[i]) continue;
                    foreach (var t in successors[i]) if (canWin[t]) { canWin[i] = true; changed = true; break; }
                }
            }
            int dead = 0; for (int i = 0; i < count; i++) if (!canWin[i]) dead++;
            return new Result { Complete = true, Positions = count, DeadEnds = dead };
        }

        static string Key(PuzzleState s)
        {
            // pots in order, contents only (counters are irrelevant to what can still be won)
            var text = s.Serialize();
            return text.Substring(text.IndexOf('|') + 1);
        }
    }
}
