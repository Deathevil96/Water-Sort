using System;
using System.Collections.Generic;
using System.Text;

namespace PaintSort.Core
{
    public enum MoveFailure { None, SameContainer, BadIndex, SourceEmpty, SourceInaccessible, DestInaccessible, DestFull, ColorMismatch, Won }

    public sealed class MoveResult
    {
        public bool Valid;
        public MoveFailure Failure;
        public int From, To, Amount, Color;
        /// <summary>Container whose newly exposed hidden layer was revealed (source), or -1.</summary>
        public int RevealedPot = -1;
        public int CompletedPot = -1;
        public List<int> UnlockedPots = new List<int>();
        public bool Won;
    }

    /// <summary>Pure puzzle rules. One authoritative logical state; views only read it.</summary>
    public sealed class PuzzleState
    {
        public sealed class Pot
        {
            public int Capacity = 4;
            public List<int> Colors = new List<int>();
            public List<bool> Hidden = new List<bool>();
            public bool Covered;
            public bool Complete;
            public bool Extra;

            public int Count => Colors.Count;
            public bool IsEmpty => Colors.Count == 0;
            public bool IsFull => Colors.Count >= Capacity;
            public int TopColor => Colors.Count == 0 ? -1 : Colors[Colors.Count - 1];

            public Pot Clone() => new Pot { Capacity = Capacity, Colors = new List<int>(Colors), Hidden = new List<bool>(Hidden), Covered = Covered, Complete = Complete, Extra = Extra };
        }

        sealed class Snapshot { public List<Pot> Pots; public int Moves; public int Unlocks; }

        public readonly List<Pot> Pots = new List<Pot>();
        readonly List<Snapshot> history = new List<Snapshot>();
        public int MoveCount { get; private set; }
        public int InvalidMoveCount { get; private set; }
        public int UndoCount { get; private set; }
        public int TotalCoversUnlocked { get; private set; }
        public bool LockCompleted = true;

        public LevelDefinition Source { get; private set; }

        public PuzzleState(LevelDefinition def)
        {
            Source = def;
            foreach (var c in def.Containers)
            {
                var p = new Pot { Capacity = c.Capacity, Covered = c.Covered, Colors = new List<int>(c.Colors), Hidden = new List<bool>(c.Hidden) };
                while (p.Hidden.Count < p.Colors.Count) p.Hidden.Add(false);
                Pots.Add(p);
            }
            foreach (var p in Pots) { RevealTop(p); p.Complete = IsUniformFull(p); }
        }

        public int HistoryCount => history.Count;
        public bool CanUndo => history.Count > 0;

        static bool IsUniformFull(Pot p)
        {
            if (p.Count != p.Capacity || p.Count == 0) return false;
            int c = p.Colors[0];
            for (int i = 1; i < p.Count; i++) if (p.Colors[i] != c) return false;
            return true;
        }

        static bool RevealTop(Pot p)
        {
            int t = p.Count - 1;
            if (t >= 0 && p.Hidden[t]) { p.Hidden[t] = false; return true; }
            return false;
        }

        public bool IsAccessible(int i) => i >= 0 && i < Pots.Count && !Pots[i].Covered && !(LockCompleted && Pots[i].Complete);

        /// <summary>Contiguous matching group at top. A hidden layer ends the group (it is not selectable while something is above it).</summary>
        public int TopGroupSize(int i)
        {
            var p = Pots[i];
            if (p.IsEmpty) return 0;
            int col = p.TopColor, n = 0;
            for (int k = p.Count - 1; k >= 0; k--)
            {
                if (p.Colors[k] != col) break;
                if (n > 0 && p.Hidden[k]) break;
                n++;
            }
            return n;
        }

        public MoveFailure Check(int from, int to)
        {
            if (from < 0 || to < 0 || from >= Pots.Count || to >= Pots.Count) return MoveFailure.BadIndex;
            if (from == to) return MoveFailure.SameContainer;
            if (IsWon) return MoveFailure.Won;
            var s = Pots[from]; var d = Pots[to];
            if (s.IsEmpty) return MoveFailure.SourceEmpty;
            if (!IsAccessible(from)) return MoveFailure.SourceInaccessible;
            if (!IsAccessible(to)) return MoveFailure.DestInaccessible;
            if (d.IsFull) return MoveFailure.DestFull;
            if (!d.IsEmpty && d.TopColor != s.TopColor) return MoveFailure.ColorMismatch;
            return MoveFailure.None;
        }

        public bool CanMove(int from, int to) => Check(from, to) == MoveFailure.None;

        public MoveResult TryMove(int from, int to)
        {
            var r = new MoveResult { From = from, To = to };
            var f = Check(from, to);
            if (f != MoveFailure.None) { r.Failure = f; if (f != MoveFailure.Won) InvalidMoveCount++; return r; }

            history.Add(Capture());
            var s = Pots[from]; var d = Pots[to];
            int amount = Math.Min(TopGroupSize(from), d.Capacity - d.Count);
            int col = s.TopColor;
            for (int k = 0; k < amount; k++)
            {
                s.Colors.RemoveAt(s.Count - 1); s.Hidden.RemoveAt(s.Hidden.Count - 1);
                d.Colors.Add(col); d.Hidden.Add(false);
            }
            MoveCount++;
            r.Valid = true; r.Amount = amount; r.Color = col;
            if (RevealTop(s)) r.RevealedPot = from;
            if (IsUniformFull(d))
            {
                d.Complete = true; r.CompletedPot = to;
                for (int k = 0; k < d.Hidden.Count; k++) d.Hidden[k] = false;
                for (int i = 0; i < Pots.Count; i++)
                    if (Pots[i].Covered) { Pots[i].Covered = false; TotalCoversUnlocked++; r.UnlockedPots.Add(i); RevealTop(Pots[i]); break; }
            }
            r.Won = IsWon;
            return r;
        }

        public bool IsWon
        {
            get
            {
                bool any = false;
                foreach (var p in Pots)
                {
                    if (p.Covered && !p.IsEmpty) return false;
                    if (p.IsEmpty) continue;
                    if (!IsUniformFull(p)) return false;
                    any = true;
                }
                return any;
            }
        }

        /// <summary>True when at least one legal move exists.</summary>
        public bool HasAnyMove()
        {
            if (IsWon) return false;
            for (int i = 0; i < Pots.Count; i++)
                for (int j = 0; j < Pots.Count; j++)
                    if (i != j && CanMove(i, j)) return true;
            return false;
        }

        public bool Undo()
        {
            if (history.Count == 0) return false;
            var snap = history[history.Count - 1];
            history.RemoveAt(history.Count - 1);
            Pots.Clear();
            foreach (var p in snap.Pots) Pots.Add(p.Clone());
            MoveCount = snap.Moves; TotalCoversUnlocked = snap.Unlocks;
            UndoCount++;
            return true;
        }

        Snapshot Capture()
        {
            var s = new Snapshot { Pots = new List<Pot>(Pots.Count), Moves = MoveCount, Unlocks = TotalCoversUnlocked };
            foreach (var p in Pots) s.Pots.Add(p.Clone());
            return s;
        }

        /// <summary>Adds one empty standard container for the rest of this attempt. Clears undo history (extra container is not part of old snapshots).</summary>
        public int AddEmptyPot(int capacity = 4)
        {
            Pots.Add(new Pot { Capacity = capacity, Extra = true });
            history.Clear();
            return Pots.Count - 1;
        }

        /// <summary>Replaces contents of the given pots (used by Targeted Shuffle after validation).</summary>
        public void ApplyContents(IList<int> potIndices, IList<List<int>> colors, IList<List<bool>> hidden)
        {
            for (int k = 0; k < potIndices.Count; k++)
            {
                var p = Pots[potIndices[k]];
                p.Colors = new List<int>(colors[k]); p.Hidden = new List<bool>(hidden[k]);
                while (p.Hidden.Count < p.Colors.Count) p.Hidden.Add(false);
                RevealTop(p);
                p.Complete = IsUniformFull(p);
            }
            history.Clear();
        }

        // ------------------------------------------------------------------ resume support
        /// <summary>
        /// Compact text form of the current board, for resuming after the app closes.
        /// Format: "moves,undos,invalid,unlocks|pot,pot,..."; a pot is its colours joined by '.', 'h' marks a hidden layer,
        /// '-' is empty, a '#' prefix is a covered pot, a '+' prefix is an extra (purchased) pot. Undo history is not saved.
        /// </summary>
        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append(MoveCount).Append(',').Append(UndoCount).Append(',').Append(InvalidMoveCount).Append(',').Append(TotalCoversUnlocked).Append('|');
            for (int i = 0; i < Pots.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var p = Pots[i];
                if (p.Extra) sb.Append('+');
                if (p.Covered) sb.Append('#');
                if (p.Count == 0) sb.Append('-');
                for (int k = 0; k < p.Count; k++)
                {
                    if (k > 0) sb.Append('.');
                    if (p.Hidden[k]) sb.Append('h');
                    sb.Append(p.Colors[k]);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Rebuilds a board saved by <see cref="Serialize"/>. Returns null when the text is malformed or does not belong to this level
        /// (wrong pot count, wrong number of units per colour, overfull pot), so a bad save can never produce an unwinnable board.
        /// </summary>
        public static PuzzleState Restore(LevelDefinition def, string text)
        {
            if (def == null || string.IsNullOrEmpty(text)) return null;
            try
            {
                var halves = text.Split('|');
                if (halves.Length != 2) return null;
                var counters = halves[0].Split(',');
                if (counters.Length != 4) return null;

                var state = new PuzzleState(def);
                state.Pots.Clear();
                var expected = new Dictionary<int, int>();
                foreach (var c in def.Containers) foreach (var col in c.Colors) { expected.TryGetValue(col, out int n); expected[col] = n + 1; }
                var actual = new Dictionary<int, int>();
                int baseCount = 0;
                foreach (var token in halves[1].Split(','))
                {
                    string t = token;
                    var pot = new Pot();
                    if (t.StartsWith("+")) { pot.Extra = true; t = t.Substring(1); } else baseCount++;
                    if (t.StartsWith("#")) { pot.Covered = true; t = t.Substring(1); }
                    if (t != "-")
                        foreach (var unit in t.Split('.'))
                        {
                            bool hidden = unit.StartsWith("h");
                            int col = int.Parse(hidden ? unit.Substring(1) : unit);
                            pot.Colors.Add(col); pot.Hidden.Add(hidden);
                            actual.TryGetValue(col, out int n); actual[col] = n + 1;
                        }
                    if (pot.Count > pot.Capacity) return null;
                    pot.Complete = IsUniformFull(pot);
                    state.Pots.Add(pot);
                }
                if (baseCount != def.Containers.Count || actual.Count != expected.Count) return null;
                foreach (var kv in expected) if (!actual.TryGetValue(kv.Key, out int have) || have != kv.Value) return null;

                state.MoveCount = int.Parse(counters[0]); state.UndoCount = int.Parse(counters[1]);
                state.InvalidMoveCount = int.Parse(counters[2]); state.TotalCoversUnlocked = int.Parse(counters[3]);
                return state;
            }
            catch (System.Exception) { return null; }
        }

        public PuzzleState CloneForSearch()
        {
            var c = new PuzzleState(Source) { LockCompleted = LockCompleted };
            c.Pots.Clear();
            foreach (var p in Pots) c.Pots.Add(p.Clone());
            c.MoveCount = MoveCount; c.TotalCoversUnlocked = TotalCoversUnlocked;
            return c;
        }
    }
}
