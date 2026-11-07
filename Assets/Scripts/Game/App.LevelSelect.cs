using System.Collections;
using PaintSort.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PaintSort.Game
{
    /// <summary>Level-select popup (paged grid) and the screen-change fade.</summary>
    public sealed partial class App
    {
        const int LevelsPerPage = 30, LevelColumns = 5;
        const float FadeSeconds = 0.28f;

        Image fadeImage; Coroutine fadeRoutine; bool booted;

        // ---------------------------------------------------------------- level select
        /// <summary>Opens the level grid. A negative page opens the page that contains the level the Play button would start.</summary>
        public void OpenLevelSelect(int page)
        {
            int unlocked = Mathf.Min(Mathf.Max(1, D.highestUnlocked), MaxLevel);
            int maxPage = (unlocked - 1) / LevelsPerPage;
            if (page < 0) page = (PlayTargetLevel - 1) / LevelsPerPage;
            page = Mathf.Clamp(page, 0, maxPage);
            int first = page * LevelsPerPage + 1, last = Mathf.Min(MaxLevel, first + LevelsPerPage - 1);

            var c = Panel("Levels", new Vector2(980, 1380), out var go, true, UI.Green);
            Line(c, "Levels " + first + " - " + last + "      Page " + (page + 1) + " of " + (maxPage + 1), 36, UI.Ink, 880, 60);

            int rows = Mathf.CeilToInt(LevelsPerPage / (float)LevelColumns);
            var gridItem = Item(c, 900, rows * 150 + (rows - 1) * 14);
            var grid = gridItem.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(164, 150); grid.spacing = new Vector2(20, 14); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = LevelColumns;
            grid.childAlignment = TextAnchor.UpperCenter;

            int current = PlayTargetLevel;
            for (int id = first; id <= last; id++)
            {
                int level = id;
                bool locked = level > unlocked, done = D.completed.Contains(level);
                var diff = Progression.DifficultyFor(level);
                Color face = locked ? UI.Grey : level == current ? UI.Gold : done ? UI.Green : diff == Difficulty.SuperHard ? UI.Red : diff == Difficulty.Hard ? UI.Orange : UI.Blue;
                var b = UI.GlossBtn(gridItem, level.ToString(), face, new Vector2(164, 150), () => { ClosePopup(go); StartLevel(level, false, level == D.resumeLevel); }, 58);
                if (locked) UI.SetEnabled(b, false);
            }

            var nav = BtnRow(c, 880, 130);
            var prev = RowBtn(nav, "<", UI.Blue, 200, 110, () => OpenLevelSelect(page - 1), go, 60);
            RowBtn(nav, "Current", UI.Purple, 380, 110, () => OpenLevelSelect(-1), go, 40);
            var next = RowBtn(nav, ">", UI.Blue, 200, 110, () => OpenLevelSelect(page + 1), go, 60);
            if (page == 0) UI.SetEnabled(prev, false);
            if (page == maxPage) UI.SetEnabled(next, false);
            Line(c, "Green = painted   Gold = up next   Orange / Red = Hard / Super Hard", 28, UI.Ink, 880, 44);
        }

        // ---------------------------------------------------------------- screen fade
        void BuildFade()
        {
            fadeImage = UI.Img(root, "Fade", Art.White, Art.Cream); UI.Stretch(fadeImage.rectTransform); fadeImage.preserveAspect = false;
            fadeImage.gameObject.SetActive(false);
        }

        /// <summary>Cream wipe that fades out after a screen change, hiding the instant swap between menu and board.</summary>
        void PlayFade()
        {
            if (!booted || fadeImage == null || D.reducedMotion) return;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine());
        }

        IEnumerator FadeRoutine()
        {
            fadeImage.gameObject.SetActive(true);
            for (float t = 0; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                float k = 1f - t / FadeSeconds;
                fadeImage.color = new Color(Art.Cream.r, Art.Cream.g, Art.Cream.b, k * k);
                yield return null;
            }
            fadeImage.gameObject.SetActive(false);
            fadeRoutine = null;
        }
    }
}
