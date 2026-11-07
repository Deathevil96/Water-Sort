using System;
using System.Collections.Generic;

namespace PaintSort.Core
{
    public enum SolverStatus { Solved, Unsolvable, Inconclusive }

    public sealed class SolverResult
    {
        public SolverStatus Status;
        public List<Move> Moves = new List<Move>();
        public int ExploredStates;
    }

    /// <summary>
    /// Best-first search over a compact byte-array encoding of the puzzle that mirrors PuzzleState's rules exactly.
    /// Any solution it returns is independently replayed against PuzzleState by callers (LevelAudit), so a divergence cannot slip through.
    /// Solution is valid, not guaranteed minimal.
    /// </summary>
    public static class PuzzleSolver
    {
        sealed class Layout
        {
            public int N, MaxCap, Stride;
            public int[] Cap;
        }

        sealed class Node
        {
            public byte[] D; // per pot: [count][flags: 1=covered 2=complete][hiddenMask][colors * MaxCap]
            public int Parent; public Move Via; public int G; public int H;
        }

        public static SolverResult Solve(PuzzleState state, int maxStates = 200000, bool lockCompleted = true)
        {
            var L = new Layout { N = state.Pots.Count, Cap = new int[state.Pots.Count] };
            for (int i = 0; i < L.N; i++) { L.Cap[i] = state.Pots[i].Capacity; if (L.Cap[i] > L.MaxCap) L.MaxCap = L.Cap[i]; }
            L.Stride = 3 + L.MaxCap;

            var start = new byte[L.N * L.Stride];
            for (int i = 0; i < L.N; i++)
            {
                var p = state.Pots[i]; int o = i * L.Stride;
                start[o] = (byte)p.Count;
                start[o + 1] = (byte)((p.Covered ? 1 : 0) | (p.Complete ? 2 : 0));
                int mask = 0;
                for (int k = 0; k < p.Count; k++) { start[o + 3 + k] = (byte)p.Colors[k]; if (p.Hidden[k]) mask |= 1 << k; }
                start[o + 2] = (byte)mask;
            }

            var res = new SolverResult();
            var nodes = new List<Node>();
            var seen = new HashSet<ulong>();
            var heap = new List<int>(); // node indices ordered by (H*W + G)
            nodes.Add(new Node { D = start, Parent = -1, G = 0, H = Heuristic(L, start) });
            seen.Add(Hash(start));
            Push(heap, nodes, 0);

            var buf = new byte[start.Length];
            while (heap.Count > 0)
            {
                if (nodes.Count > maxStates) { res.Status = SolverStatus.Inconclusive; res.ExploredStates = nodes.Count; return res; }
                int idx = Pop(heap, nodes);
                var cur = nodes[idx];
                if (IsWon(L, cur.D))
                {
                    var path = new List<Move>();
                    for (int n = idx; nodes[n].Parent >= 0; n = nodes[n].Parent) path.Add(nodes[n].Via);
                    path.Reverse();
                    res.Moves = path; res.Status = SolverStatus.Solved; res.ExploredStates = nodes.Count;
                    return res;
                }
                for (int f = 0; f < L.N; f++)
                {
                    int fo = f * L.Stride;
                    if (cur.D[fo] == 0 || (cur.D[fo + 1] & 1) != 0 || (lockCompleted && (cur.D[fo + 1] & 2) != 0)) continue;
                    int group = Group(L, cur.D, f);
                    bool srcUniform = group == cur.D[fo];
                    for (int t = 0; t < L.N; t++)
                    {
                        if (t == f) continue;
                        int to = t * L.Stride;
                        if ((cur.D[to + 1] & 1) != 0 || (lockCompleted && (cur.D[to + 1] & 2) != 0)) continue;
                        if (cur.D[to] >= L.Cap[t]) continue;
                        if (cur.D[to] != 0 && cur.D[to + 3 + cur.D[to] - 1] != cur.D[fo + 3 + cur.D[fo] - 1]) continue;
                        if (cur.D[to] == 0 && srcUniform) continue; // pointless: moves a whole uniform pot into an empty one
                        Buffer.BlockCopy(cur.D, 0, buf, 0, buf.Length);
                        Apply(L, buf, f, t);
                        ulong h = Hash(buf);
                        if (!seen.Add(h)) continue;
                        var nd = new byte[buf.Length]; Buffer.BlockCopy(buf, 0, nd, 0, buf.Length);
                        nodes.Add(new Node { D = nd, Parent = idx, Via = new Move(f, t), G = cur.G + 1, H = Heuristic(L, nd) });
                        Push(heap, nodes, nodes.Count - 1);
                    }
                }
            }
            res.Status = SolverStatus.Unsolvable; res.ExploredStates = nodes.Count;
            return res;
        }

        static int Group(Layout L, byte[] d, int p)
        {
            int o = p * L.Stride, cnt = d[o];
            if (cnt == 0) return 0;
            int col = d[o + 3 + cnt - 1], n = 0, mask = d[o + 2];
            for (int k = cnt - 1; k >= 0; k--)
            {
                if (d[o + 3 + k] != col) break;
                if (n > 0 && (mask & (1 << k)) != 0) break;
                n++;
            }
            return n;
        }

        static void Apply(Layout L, byte[] d, int f, int t)
        {
            int fo = f * L.Stride, to = t * L.Stride;
            int amount = Math.Min(Group(L, d, f), L.Cap[t] - d[to]);
            byte col = d[fo + 3 + d[fo] - 1];
            for (int k = 0; k < amount; k++)
            {
                d[fo]--; d[fo + 3 + d[fo]] = 0;
                d[fo + 2] = (byte)(d[fo + 2] & ~(1 << d[fo]));
                d[to + 3 + d[to]] = col; d[to]++;
            }
            if (d[fo] > 0) d[fo + 2] = (byte)(d[fo + 2] & ~(1 << (d[fo] - 1)));
            if (d[to] == L.Cap[t])
            {
                bool uni = true;
                for (int k = 1; k < d[to]; k++) if (d[to + 3 + k] != d[to + 3]) { uni = false; break; }
                if (uni)
                {
                    d[to + 1] |= 2; d[to + 2] = 0;
                    for (int i = 0; i < L.N; i++)
                    {
                        int o = i * L.Stride;
                        if ((d[o + 1] & 1) != 0)
                        {
                            d[o + 1] = (byte)(d[o + 1] & ~1);
                            if (d[o] > 0) d[o + 2] = (byte)(d[o + 2] & ~(1 << (d[o] - 1)));
                            break;
                        }
                    }
                }
            }
        }

        static bool IsWon(Layout L, byte[] d)
        {
            bool any = false;
            for (int i = 0; i < L.N; i++)
            {
                int o = i * L.Stride;
                if (d[o] == 0) continue;
                if ((d[o + 1] & 1) != 0) return false;
                if (d[o] != L.Cap[i]) return false;
                for (int k = 1; k < d[o]; k++) if (d[o + 3 + k] != d[o + 3]) return false;
                any = true;
            }
            return any;
        }

        static int Heuristic(Layout L, byte[] d)
        {
            int h = 0;
            for (int i = 0; i < L.N; i++)
            {
                int o = i * L.Stride, cnt = d[o];
                if (cnt == 0) continue;
                if ((d[o + 1] & 2) != 0) continue;
                int runs = 1;
                for (int k = 1; k < cnt; k++) if (d[o + 3 + k] != d[o + 3 + k - 1]) runs++;
                h += runs * 2 - 1;
                if ((d[o + 1] & 1) != 0) h += 2;
                if (runs == 1 && cnt < L.Cap[i]) h += 1;
            }
            return h;
        }

        static ulong Hash(byte[] d)
        {
            ulong h = 14695981039346656037UL;
            for (int i = 0; i < d.Length; i++) { h ^= d[i]; h *= 1099511628211UL; }
            return h;
        }

        // Min-heap by (H*2 + G): weighted best-first, favouring promising states while still penalising long paths.
        static int Key(List<Node> nodes, int i) => nodes[i].H * 3 + nodes[i].G;

        static void Push(List<int> heap, List<Node> nodes, int node)
        {
            heap.Add(node);
            int c = heap.Count - 1;
            while (c > 0)
            {
                int p = (c - 1) / 2;
                if (Key(nodes, heap[p]) <= Key(nodes, heap[c])) break;
                (heap[p], heap[c]) = (heap[c], heap[p]); c = p;
            }
        }

        static int Pop(List<int> heap, List<Node> nodes)
        {
            int top = heap[0];
            int last = heap[heap.Count - 1]; heap.RemoveAt(heap.Count - 1);
            if (heap.Count > 0)
            {
                heap[0] = last; int c = 0;
                while (true)
                {
                    int l = c * 2 + 1, r = l + 1, m = c;
                    if (l < heap.Count && Key(nodes, heap[l]) < Key(nodes, heap[m])) m = l;
                    if (r < heap.Count && Key(nodes, heap[r]) < Key(nodes, heap[m])) m = r;
                    if (m == c) break;
                    (heap[m], heap[c]) = (heap[c], heap[m]); c = m;
                }
            }
            return top;
        }
    }
}
