using System;
using System.Collections.Generic;

namespace PaintSort.Core
{
    public enum Difficulty { Normal, Hard, SuperHard }

    public readonly struct Move
    {
        public readonly int From, To;
        public Move(int from, int to) { From = from; To = to; }
        public override string ToString() => From + ">" + To;
    }

    public sealed class ContainerDef
    {
        public int Capacity = 4;
        public bool Covered;
        /// <summary>Bottom to top.</summary>
        public List<int> Colors = new List<int>();
        /// <summary>Parallel to Colors. true = shown as dark '?' until it becomes the top layer.</summary>
        public List<bool> Hidden = new List<bool>();

        public ContainerDef Clone()
        {
            return new ContainerDef { Capacity = Capacity, Covered = Covered, Colors = new List<int>(Colors), Hidden = new List<bool>(Hidden) };
        }
    }

    public sealed class LevelDefinition
    {
        public int LevelId;
        public Difficulty Difficulty;
        public int Seed;
        public List<ContainerDef> Containers = new List<ContainerDef>();
        public List<Move> Witness = new List<Move>();

        public int ColorCount { get { var s = new HashSet<int>(); foreach (var c in Containers) foreach (var x in c.Colors) s.Add(x); return s.Count; } }
        public int HiddenCount { get { int n = 0; foreach (var c in Containers) foreach (var h in c.Hidden) if (h) n++; return n; } }
        public int CoveredCount { get { int n = 0; foreach (var c in Containers) if (c.Covered) n++; return n; } }
        public int EmptyCount { get { int n = 0; foreach (var c in Containers) if (c.Colors.Count == 0) n++; return n; } }

        public LevelDefinition Clone()
        {
            var l = new LevelDefinition { LevelId = LevelId, Difficulty = Difficulty, Seed = Seed, Witness = new List<Move>(Witness) };
            foreach (var c in Containers) l.Containers.Add(c.Clone());
            return l;
        }
    }

    public static class Progression
    {
        public const int HiddenFromLevel = 21;
        public const int CoversFromLevel = 31;

        public static Difficulty DifficultyFor(int level)
        {
            if (level % 10 == 0) return Difficulty.SuperHard;
            if (level % 5 == 0) return Difficulty.Hard;
            return Difficulty.Normal;
        }

        /// <summary>First level at which each power-up is available. Each is introduced on its level (see the game layer); before that the button is locked.</summary>
        public const int UndoUnlockLevel = 5, ShuffleUnlockLevel = 10, ExtraPotUnlockLevel = 15;

        public static string Label(Difficulty d) => d == Difficulty.SuperHard ? "Super Hard" : d == Difficulty.Hard ? "Hard" : "Normal";
    }
}
