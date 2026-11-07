using System.Collections.Generic;
using PaintSort.Core;

namespace PaintSort.Game
{
    /// <summary>
    /// Tap-ahead: while a pour is still animating the player can already pick the next valid move; it is queued and plays by itself when the
    /// current pour ends. Queued moves are checked against a "shadow" copy of the board that already includes every queued move, so a queued move
    /// is always legal when its turn comes. The real game state only changes when a move actually starts playing, so save / undo / win logic is untouched.
    /// </summary>
    public sealed partial class App
    {
        const int MaxQueuedMoves = 6;
        readonly Queue<KeyValuePair<int, int>> moveQueue = new Queue<KeyValuePair<int, int>>();
        PuzzleState shadow;

        public int QueuedMoves { get { return moveQueue.Count; } }
        /// <summary>A pour is playing or more are waiting: anything that edits the board (undo, shuffle, restart, leaving) must wait.</summary>
        bool Animating { get { return Board.Busy || moveQueue.Count > 0; } }

        /// <summary>Lifts the picked pot - except one that is mid-pour (it is moving anyway; it lifts when the pour ends).</summary>
        void MarkSelected(int i) { if (i != Board.PouringFrom) Board.SetSelected(i, true); }

        void ClearMoveQueue() { moveQueue.Clear(); shadow = null; }

        void EnqueueMove(PuzzleState view, int from, int to)
        {
            if (moveQueue.Count >= MaxQueuedMoves) { Audio.Play("invalid"); return; }
            view.TryMove(from, to);                                        // advance the shadow board
            moveQueue.Enqueue(new KeyValuePair<int, int>(from, to));
            Board.SetSelected(from, false); selected = -1;
            Audio.Play("tap");
        }

        /// <summary>Called when a pour has finished: starts the next queued move, if any. Returns true when one was started.</summary>
        bool PlayNextQueued()
        {
            while (moveQueue.Count > 0)
            {
                var m = moveQueue.Dequeue();
                if (DoMove(m.Key, m.Value)) return true;
                ClearMoveQueue();                                           // should not happen (the shadow board validated it); drop the rest to stay safe
            }
            shadow = null;
            return false;
        }
    }
}
