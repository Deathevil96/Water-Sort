using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace PaintSort.Game
{
    /// <summary>
    /// Loading screen for heavy work (starting a level, building a big popup). The work runs on the main thread, so the screen freezes while it runs;
    /// the loading overlay is drawn first (two frames) so a slow phone shows "Loading" with a spinner instead of looking hung.
    /// It only appears when it can matter: on low-end phones (little memory or few cores) or once that kind of work has been measured as slow.
    /// On a fast phone the work simply runs at once, with no flash and no extra frames.
    /// </summary>
    public sealed partial class App
    {
        const float LoadingMinSeconds = 0.35f, SlowWorkMs = 90f;
        readonly Dictionary<string, float> heavyMs = new Dictionary<string, float>();
        bool loadingBusy;
        /// <summary>Dev switch (debug menu): always show the loading screen, to check how it looks.</summary>
        public bool ForceLoadingScreens;
        RectTransform loadingRoot; CanvasGroup loadingGroup; Spinner loadingSpinner;

        static readonly bool LowEndDevice = SystemInfo.systemMemorySize <= 3500 || SystemInfo.processorCount <= 4;

        /// <summary>Runs <paramref name="work"/>, behind the loading screen when the device is slow or this kind of work has proved slow.</summary>
        void Heavy(string what, Action work)
        {
            bool slow = heavyMs.TryGetValue(what, out var ms) && ms > SlowWorkMs;
            if (loadingBusy || root == null || !(ForceLoadingScreens || LowEndDevice || slow)) { Measure(what, work); return; }
            StartCoroutine(HeavyRoutine(what, work));
        }

        void Measure(string what, Action work)
        {
            var sw = Stopwatch.StartNew(); work(); sw.Stop();
            float old; heavyMs[what] = heavyMs.TryGetValue(what, out old) ? old * 0.5f + sw.ElapsedMilliseconds * 0.5f : sw.ElapsedMilliseconds;
            Log.I("PERF", what + " took " + sw.ElapsedMilliseconds + " ms");
        }

        IEnumerator HeavyRoutine(string what, Action work)
        {
            loadingBusy = true;
            ShowLoading();
            float shown = Time.unscaledTime;
            yield return null; yield return null;                      // let the overlay actually be drawn before the main thread gets busy
            try { Measure(what, work); } catch (Exception e) { Log.W("PERF", what + " failed: " + e); }
            yield return null;
            while (Time.unscaledTime - shown < LoadingMinSeconds) yield return null;       // never flicker
            HideLoading();
            loadingBusy = false;
        }

        void ShowLoading()
        {
            if (loadingRoot == null) BuildLoading();
            loadingRoot.gameObject.SetActive(true); loadingGroup.alpha = 1f; loadingSpinner.enabled = true;
            loadingRoot.SetAsLastSibling();
        }

        void HideLoading()
        {
            if (loadingRoot == null) return;
            loadingSpinner.enabled = false; loadingRoot.gameObject.SetActive(false);
        }

        void BuildLoading()
        {
            loadingRoot = UI.Rect(root, "Loading"); UI.Stretch(loadingRoot);
            var c = loadingRoot.gameObject.AddComponent<Canvas>(); c.overrideSorting = true; c.sortingOrder = 4000;       // above popups and toasts, below the studio splash
            loadingRoot.gameObject.AddComponent<GraphicRaycaster>();                                                    // swallows taps while loading
            loadingGroup = loadingRoot.gameObject.AddComponent<CanvasGroup>();
            var cover = UI.Img(loadingRoot, "Cover", Art.White, Art.Cream, false, true); UI.Stretch(cover.rectTransform); cover.preserveAspect = false;
            if (backdropPortrait != null)
            {
                var bg = UI.Img(loadingRoot, "Backdrop", backdropPortrait, Color.white);
                var bs = IsLandscape && backdropLandscape != null ? backdropLandscape : backdropPortrait; bg.sprite = bs; bg.preserveAspect = false;
                var screen = root.rect.size; var rt = bg.rectTransform; rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = bs.rect.size * Mathf.Max(screen.x / bs.rect.width, screen.y / bs.rect.height);
            }
            // spinner: a ring of eight paint dots in the game's colours, turning
            var ring = UI.Rect(loadingRoot, "Spinner"); UI.Place(ring, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(200, 200));
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f; var d = UI.Img(ring, "Dot" + i, Art.Circle, Art.Paint[i % Art.Paint.Length]);
                UI.Place(d.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 76f, Vector2.one * (30f - i * 1.8f));
            }
            loadingSpinner = ring.gameObject.AddComponent<Spinner>();
            var t = UI.Txt(loadingRoot, "Loading...", 60, Color.white); t.rectTransform.anchorMin = new Vector2(0, 0.5f); t.rectTransform.anchorMax = new Vector2(1, 0.5f);
            t.rectTransform.anchoredPosition = new Vector2(0, -140); t.rectTransform.sizeDelta = new Vector2(0, 90);
        }
    }

    /// <summary>Turns its transform at a steady rate; only enabled while the loading screen shows.</summary>
    public sealed class Spinner : MonoBehaviour
    {
        void Update() { transform.Rotate(0f, 0f, -360f * Time.unscaledDeltaTime); }
    }
}
