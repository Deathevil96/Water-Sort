using System;
using System.Collections.Generic;
using System.Text;

namespace PaintSort.Core
{
    public sealed class AuditRow
    {
        public int LevelId; public Difficulty Difficulty; public int Colors, Containers, Empty, Hidden, Covered;
        public bool WitnessValid; public string SolverStatus = "NotRun"; public int SolutionMoves; public bool BoosterRequired; public int Seed; public string Error = "";
        public int InitialMoves; public int Reveals; public int Unlocks;
        public bool Passed => Error.Length == 0 && WitnessValid;
    }

    public sealed class AuditReport
    {
        public List<AuditRow> Rows = new List<AuditRow>();
        public List<int> FailedIds = new List<int>();
        public List<int> InconclusiveIds = new List<int>();
        public List<int> BoosterIds = new List<int>();
        public bool Pass => FailedIds.Count == 0 && Rows.Count > 0;
        public string EconomySummary = "";

        public string Summary()
        {
            int hard = 0, sup = 0; foreach (var r in Rows) { if (r.Difficulty == Difficulty.Hard) hard++; else if (r.Difficulty == Difficulty.SuperHard) sup++; }
            return (Pass ? "PASS" : "FAIL") + ": " + Rows.Count + " levels audited, " + FailedIds.Count + " failed, " + InconclusiveIds.Count + " solver-inconclusive, "
                   + BoosterIds.Count + " need booster. Hard=" + hard + " SuperHard=" + sup + ".\n" + EconomySummary;
        }

        public string ToCsv()
        {
            var sb = new StringBuilder("levelId,difficulty,colourCount,containerCount,emptyCount,hiddenCount,coveredCount,witnessValid,solverStatus,solutionMoves,boosterRequired,seed,error\n");
            foreach (var r in Rows)
                sb.Append(r.LevelId).Append(',').Append(r.Difficulty).Append(',').Append(r.Colors).Append(',').Append(r.Containers).Append(',').Append(r.Empty).Append(',')
                  .Append(r.Hidden).Append(',').Append(r.Covered).Append(',').Append(r.WitnessValid).Append(',').Append(r.SolverStatus).Append(',').Append(r.SolutionMoves).Append(',')
                  .Append(r.BoosterRequired).Append(',').Append(r.Seed).Append(',').Append('"').Append(r.Error.Replace("\"", "'")).Append("\"\n");
            return sb.ToString();
        }

        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\n  \"pass\": ").Append(Pass ? "true" : "false").Append(",\n  \"levels\": ").Append(Rows.Count).Append(",\n");
            sb.Append("  \"failed\": [").Append(string.Join(",", FailedIds)).Append("],\n");
            sb.Append("  \"inconclusive\": [").Append(string.Join(",", InconclusiveIds)).Append("],\n");
            sb.Append("  \"boosterRequired\": [").Append(string.Join(",", BoosterIds)).Append("],\n");
            sb.Append("  \"economy\": \"").Append(EconomySummary.Replace("\n", " | ").Replace("\"", "'")).Append("\",\n  \"rows\": [\n");
            for (int i = 0; i < Rows.Count; i++)
            {
                var r = Rows[i];
                sb.Append("    {\"levelId\":").Append(r.LevelId).Append(",\"difficulty\":\"").Append(r.Difficulty).Append("\",\"colours\":").Append(r.Colors)
                  .Append(",\"containers\":").Append(r.Containers).Append(",\"empty\":").Append(r.Empty).Append(",\"hidden\":").Append(r.Hidden).Append(",\"covered\":").Append(r.Covered)
                  .Append(",\"witnessValid\":").Append(r.WitnessValid ? "true" : "false").Append(",\"solver\":\"").Append(r.SolverStatus).Append("\",\"moves\":").Append(r.SolutionMoves)
                  .Append(",\"seed\":").Append(r.Seed).Append(",\"error\":\"").Append(r.Error.Replace("\"", "'")).Append("\"}").Append(i + 1 < Rows.Count ? ",\n" : "\n");
            }
            sb.Append("  ]\n}\n");
            return sb.ToString();
        }
    }

    public static class LevelAudit
    {
        public static bool ReplayWitness(LevelDefinition level, out string error)
        {
            return Replay(level, out error, out _, out _);
        }

        static bool Replay(LevelDefinition level, out string error, out int reveals, out int unlocks)
        {
            var st = new PuzzleState(level);
            reveals = 0; unlocks = 0; error = "";
            int idx = 0;
            foreach (var m in level.Witness)
            {
                var r = st.TryMove(m.From, m.To);
                if (!r.Valid) { error = "witness move " + idx + " (" + m + ") rejected: " + r.Failure; return false; }
                if (r.RevealedPot >= 0) reveals++;
                unlocks += r.UnlockedPots.Count;
                idx++;
            }
            if (!st.IsWon) { error = "witness does not reach win state"; return false; }
            return true;
        }

        public static AuditRow Check(LevelDefinition l, bool runSolver, int solverCap)
        {
            var row = new AuditRow
            {
                LevelId = l.LevelId, Difficulty = l.Difficulty, Containers = l.Containers.Count, Colors = l.ColorCount, Empty = l.EmptyCount,
                Hidden = l.HiddenCount, Covered = l.CoveredCount, Seed = l.Seed
            };
            var err = new List<string>();
            if (l.Difficulty != Progression.DifficultyFor(l.LevelId)) err.Add("wrong difficulty label");
            var counts = new Dictionary<int, int>();
            foreach (var c in l.Containers)
            {
                if (c.Capacity != 4) err.Add("capacity != 4");
                if (c.Colors.Count > c.Capacity) err.Add("over capacity");
                if (c.Hidden.Count != c.Colors.Count) err.Add("hidden metadata length mismatch");
                if (c.Covered && c.Colors.Count == 0) err.Add("empty covered container");
                for (int k = 0; k < c.Colors.Count; k++)
                {
                    counts.TryGetValue(c.Colors[k], out int n); counts[c.Colors[k]] = n + 1;
                    if (k == c.Colors.Count - 1 && k < c.Hidden.Count && c.Hidden[k]) err.Add("hidden unit on top");
                }
            }
            foreach (var kv in counts) if (kv.Value != 4) err.Add("colour " + kv.Key + " has " + kv.Value + " units");
            if (row.Empty < 1) err.Add("no empty container");
            if (l.LevelId < Progression.HiddenFromLevel && row.Hidden > 0) err.Add("hidden layer before level 21");
            if (l.LevelId < Progression.CoversFromLevel && row.Covered > 0) err.Add("cover before level 31");
            if (l.LevelId == Progression.HiddenFromLevel && row.Hidden == 0) err.Add("level 21 must teach hidden layers");
            if (l.LevelId == Progression.CoversFromLevel && row.Covered == 0) err.Add("level 31 must teach covers");
            if (l.LevelId <= 60 && row.Covered > 1) err.Add("too many covers for range");
            if (l.LevelId <= 150 && row.Covered > 2) err.Add("too many covers for range");
            if (row.Covered > 3) err.Add("more than 3 covers");

            var st0 = new PuzzleState(l);
            for (int i = 0; i < st0.Pots.Count; i++) for (int j = 0; j < st0.Pots.Count; j++) if (i != j && st0.CanMove(i, j)) row.InitialMoves++;
            foreach (var p in st0.Pots) if (p.Complete) err.Add("container starts complete");

            bool ok = Replay(l, out string rerr, out int reveals, out int unlocks);
            row.WitnessValid = ok; row.SolutionMoves = l.Witness.Count; row.Reveals = reveals; row.Unlocks = unlocks;
            if (!ok) err.Add(rerr);
            else
            {
                // special-mechanic validation through the real state machine
                if (unlocks != row.Covered) err.Add("witness unlocked " + unlocks + " covers, expected " + row.Covered);
            }

            if (runSolver)
            {
                var sol = PuzzleSolver.Solve(new PuzzleState(l), solverCap);
                row.SolverStatus = sol.Status.ToString();
                if (sol.Status == SolverStatus.Unsolvable) err.Add("solver says unsolvable");
            }
            row.Error = string.Join("; ", err);
            return row;
        }

        public static AuditReport Run(IEnumerable<LevelDefinition> levels, bool runSolver = false, int solverCap = 60000)
        {
            var rep = new AuditReport();
            foreach (var l in levels)
            {
                var row = Check(l, runSolver, solverCap);
                rep.Rows.Add(row);
                if (!row.Passed) rep.FailedIds.Add(row.LevelId);
                if (row.SolverStatus == "Inconclusive") rep.InconclusiveIds.Add(row.LevelId);
                if (row.BoosterRequired) rep.BoosterIds.Add(row.LevelId);
            }
            rep.EconomySummary = EconomySimulator.Summarise(rep.Rows.Count);
            return rep;
        }
    }
}
