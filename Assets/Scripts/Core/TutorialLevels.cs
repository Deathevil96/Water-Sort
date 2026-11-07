using System.Collections.Generic;

namespace PaintSort.Core
{
    /// <summary>
    /// Hand-designed opening levels. Each one teaches exactly one idea (the matching hint text lives in the game layer).
    /// Boards use the catalog pot syntax: colours separated by '.', bottom to top; '-' is an empty pot.
    /// They still go through the solver and witness replay, so they are audited like every other level.
    /// They must also be <b>forgiving</b> (see <see cref="DeadEndAnalysis"/>): no power-up exists yet, so no sequence of legal moves may
    /// leave the player stuck. Spare empty pots are what make that true; do not remove them when editing a board.
    /// </summary>
    public static class TutorialLevels
    {
        static readonly Dictionary<int, string> Boards = new Dictionary<int, string>
        {
            { 1, "0.0.1.1,1.1.0.0,-" },                              // pick up, pour into an empty pot
            { 2, "0.1.0.1,2.2.0.1,2.2.1.0,-,-" },                    // pour onto the same colour
            { 3, "1.2.0.0,0.0.2.2,1.1.1.2,-,-" },                    // matching top layers move together
            { 4, "0.1.1.0,1.0.0.1,2.3.3.2,3.2.2.3,-" },              // a pot that is nearly full only takes part of a pour
        };

        public static bool TryCreate(int levelId, out LevelDefinition level)
        {
            level = null;
            if (!Boards.TryGetValue(levelId, out var pots)) return false;
            level = LevelCodec.Decode(levelId + ";" + (int)Progression.DifficultyFor(levelId) + ";" + (levelId * 1000) + ";" + pots + ";");
            return true;
        }
    }
}
