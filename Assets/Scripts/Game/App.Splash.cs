using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PaintSort.Game
{
    /// <summary>
    /// Studio splash: the Misal Interactive logo centred on the same backdrop the menu uses, fading in, holding, then fading out into the game.
    /// It is a full-screen overlay above everything (it also swallows taps while it shows). Tests switch it off with UseIsolatedSaveForTests.
    /// </summary>
    public sealed partial class App
    {
        const float SplashFadeIn = 0.35f, SplashHold = 1.3f, SplashFadeOut = 0.4f;
        Coroutine splashRun; RectTransform splashRoot; Image splashBg, splashLogo; CanvasGroup splashGroup; Sprite splashLogoSprite;

        void BuildSplash()
        {
            splashLogoSprite = Resources.Load<Sprite>("Art/misal_logo");
            if (splashLogoSprite == null) return;                                   // no logo supplied: no splash
            splashRoot = UI.Rect(root, "Splash"); UI.Stretch(splashRoot);
            var c = splashRoot.gameObject.AddComponent<Canvas>(); c.overrideSorting = true; c.sortingOrder = 5000;   // above popups, toasts and fades
            splashRoot.gameObject.AddComponent<GraphicRaycaster>();
            splashGroup = splashRoot.gameObject.AddComponent<CanvasGroup>(); splashGroup.alpha = 0f;
            var cover = UI.Img(splashRoot, "Cover", Art.White, Art.Cream, false, true); UI.Stretch(cover.rectTransform); cover.preserveAspect = false;   // plain wall colour, also blocks taps
            splashBg = UI.Img(splashRoot, "Backdrop", backdropPortrait != null ? backdropPortrait : Art.White, backdropPortrait != null ? Color.white : Art.Cream);
            splashLogo = UI.Img(splashRoot, "Logo", splashLogoSprite, Color.white);
            LayoutSplash(new Vector2(lastW, lastH) / Mathf.Max(0.01f, scaler.scaleFactor));
            splashRun = StartCoroutine(SplashRoutine());
        }

        /// <summary>Backdrop scaled to cover the screen; logo fitted into the middle (never more than 62% of the width or 34% of the height).</summary>
        void LayoutSplash(Vector2 screen)
        {
            if (splashRoot == null) return;
            var bgSprite = IsLandscape && backdropLandscape != null ? backdropLandscape : backdropPortrait;
            if (bgSprite != null)
            {
                splashBg.sprite = bgSprite; splashBg.color = Color.white; splashBg.preserveAspect = false;
                var rt = splashBg.rectTransform; rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = bgSprite.rect.size * Mathf.Max(screen.x / bgSprite.rect.width, screen.y / bgSprite.rect.height);
            }
            var ls = splashLogoSprite.rect.size; float fit = Mathf.Min(screen.x * 0.62f / ls.x, screen.y * 0.34f / ls.y);
            var lr = splashLogo.rectTransform; lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(0.5f, 0.5f); lr.anchoredPosition = new Vector2(0, screen.y * 0.02f); lr.sizeDelta = ls * fit;
        }

        IEnumerator SplashRoutine()
        {
            for (float t = 0; t < SplashFadeIn; t += Time.unscaledDeltaTime) { if (splashGroup == null) yield break; splashGroup.alpha = t / SplashFadeIn; yield return null; }
            if (splashGroup == null) yield break;
            splashGroup.alpha = 1f;
            for (float t = 0; t < SplashHold; t += Time.unscaledDeltaTime) yield return null;
            for (float t = 0; t < SplashFadeOut; t += Time.unscaledDeltaTime) { if (splashGroup == null) yield break; splashGroup.alpha = 1f - t / SplashFadeOut; yield return null; }
            HideSplashNow();
        }

        /// <summary>Removes the splash immediately (also used by tests, which must not have a full-screen overlay in front of the buttons).</summary>
        public void HideSplashNow()
        {
            if (splashRoot == null) return;
            if (splashRun != null) { StopCoroutine(splashRun); splashRun = null; }
            Destroy(splashRoot.gameObject); splashRoot = null; splashGroup = null;
        }

    }
}
