using System;
using System.Collections;
using System.Collections.Generic;
using PaintSort.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PaintSort.Game
{
    public struct UnitVis { public int color; public bool hidden; public float h; public UnitVis(int c, bool hid, float height = 1f) { color = c; hidden = hid; h = height; } }

    /// <summary>View of one acrylic mixing pot. Reads state only; never decides rules.</summary>
    public sealed class PotView : MonoBehaviour, IPointerClickHandler
    {
        public int Index; public BoardView Board;
        public RectTransform Rt; public Vector2 RestPos; public float Scale = 1f;
        public bool AnimControlled;
        public int Capacity = 4;
        const float LiftPixels = 60f;
        float lift, liftTarget;
        RectTransform liquidWorld;
        Image glow, focus, cover, badge, mark;
        Text coverText;
        Image[] segs; Image[] syms; Text[] qs; Image[] lines;
        float unitH;
        Color charcoal = Art.Hex("3B3548");
        RectTransform maskRt;

        public void Build(int capacity)
        {
            Capacity = capacity; unitH = Art.Pot.FillHeight / capacity;
            Rt = (RectTransform)transform; Rt.sizeDelta = new Vector2(Art.Pot.W, Art.Pot.H);
            var hit = gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, 0); hit.raycastTarget = true;
            var sh = UI.Img(Rt, "Shadow", Art.PotShadow, Color.white); UI.Place(sh.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, Art.Pot.ShadowY), new Vector2(Art.Pot.ShadowW, Art.Pot.ShadowH));
            focus = UI.Img(Rt, "Focus", Art.PotGlass, UI.FocusRing); UI.Stretch(focus.rectTransform, -26, -22, -26, -22); focus.gameObject.SetActive(false);   // keyboard / gamepad cursor
            glow = UI.Img(Rt, "Glow", Art.PotGlass, new Color(1f, 0.85f, 0.2f, 0f)); UI.Stretch(glow.rectTransform, -16, -14, -16, -14);
            var back = UI.Img(Rt, "Back", Art.PotBack, Color.white); UI.Stretch(back.rectTransform);
            var mask = UI.Img(Rt, "Mask", Art.PotMask, Color.white); UI.Stretch(mask.rectTransform); maskRt = mask.rectTransform;
            var m = mask.gameObject.AddComponent<Mask>(); m.showMaskGraphic = false;
            liquidWorld = UI.Rect(mask.transform, "Liquid"); UI.Place(liquidWorld, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            segs = new Image[capacity]; syms = new Image[capacity]; qs = new Text[capacity]; lines = new Image[capacity];
            for (int i = 0; i < capacity; i++)
            {
                var s = UI.Img(liquidWorld, "Seg" + i, Art.White, Color.white); s.preserveAspect = false;
                s.rectTransform.anchorMin = s.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); s.rectTransform.pivot = new Vector2(0.5f, 0f);
                var line = UI.Img(s.transform, "Line", Art.White, new Color(0.1f, 0.05f, 0.2f, 0.28f)); line.preserveAspect = false;
                UI.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(700, 5));
                var gl = UI.Img(s.transform, "Gloss", Art.Round, new Color(1, 1, 1, 0.3f), true); gl.preserveAspect = false;
                UI.Place(gl.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(Art.Pot.GlossX, 0), new Vector2(16, unitH - 22));
                var sy = UI.Img(s.transform, "Sym", Art.Symbol(0), new Color(1, 1, 1, 0.9f)); UI.Place(sy.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46, 46));
                var q = UI.Txt(s.transform, "?", 70, Color.white); q.rectTransform.anchorMin = q.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); q.rectTransform.sizeDelta = new Vector2(100, 100); q.rectTransform.anchoredPosition = Vector2.zero;
                segs[i] = s; syms[i] = sy; qs[i] = q; lines[i] = line;
            }
            var glass = UI.Img(Rt, "Glass", Art.PotGlass, Color.white); UI.Stretch(glass.rectTransform);
            cover = UI.Img(Rt, "Cover", Art.PotCover, Color.white); UI.Stretch(cover.rectTransform); cover.gameObject.SetActive(false);
            coverText = UI.Txt(cover.transform, "?", 130, Color.white); coverText.rectTransform.anchoredPosition = new Vector2(0, -10);
            mark = UI.Img(Rt, "Mark", Art.Check, new Color(0.3f, 0.7f, 1f, 0f)); UI.Place(mark.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(90, 90));
            badge = UI.Img(Rt, "Done", Art.Check, new Color(0.35f, 0.85f, 0.5f, 1f)); UI.Place(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(110, 110)); badge.gameObject.SetActive(false);
            ShowUnits(new List<UnitVis>());
        }

        /// <summary>Only runs real work while the lift is still moving: an idle board must not touch any transform (that would dirty the canvas every frame).</summary>
        void LateUpdate()
        {
            if (AnimControlled || lift == liftTarget) return;
            lift = Mathf.Lerp(lift, liftTarget, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 16f));
            if (Mathf.Abs(lift - liftTarget) < 0.002f) lift = liftTarget;
            ApplyRest();
        }

        /// <summary>Places the pot at its layout position plus the current selection lift.</summary>
        public void ApplyRest() { Rt.anchoredPosition = RestPos + Vector2.up * (lift * LiftPixels); }

        /// <summary>Tilts the pot while keeping the paint surface level in world space (the liquid is counter-rotated, once per change).</summary>
        public void SetTilt(float zDegrees)
        {
            Rt.localRotation = zDegrees == 0f ? Quaternion.identity : Quaternion.Euler(0, 0, zDegrees);
            if (liquidWorld != null) liquidWorld.rotation = Quaternion.identity;
        }

        public void SetFocused(bool on) { if (focus != null && focus.gameObject.activeSelf != on) focus.gameObject.SetActive(on); }

        public void OnPointerClick(PointerEventData e) { if (Board != null) Board.Tapped(Index); }

        public void SetSelected(bool on) { liftTarget = on ? 1f : 0f; var c = glow.color; c.a = on ? 0.9f : 0f; if (on) c = new Color(1f, 0.85f, 0.2f, 0.9f); glow.color = c; }
        public void SetMarked(bool on) { var c = glow.color; if (on) glow.color = new Color(0.3f, 0.7f, 1f, 0.95f); else if (liftTarget < 0.5f) glow.color = new Color(c.r, c.g, c.b, 0f); mark.color = new Color(0.3f, 0.7f, 1f, on ? 1f : 0f); mark.rectTransform.localScale = Vector3.one * 1.2f; }
        public void SetCovered(bool on) { Show(cover.gameObject, on); }
        public void SetComplete(bool on) { Show(badge.gameObject, on); }
        public void SnapLift() { lift = liftTarget; }
        public Vector2 GetRest() => RestPos;

        public void SetSymbolsVisible(bool on) { symbolsOn = on; }
        bool symbolsOn;

        static void Show(GameObject g, bool on) { if (g.activeSelf != on) g.SetActive(on); }

        public void ShowUnits(List<UnitVis> units, float? surfaceY = null)
        {
            float total = 0; for (int i = 0; i < units.Count; i++) total += units[i].h;
            float baseY = surfaceY.HasValue ? surfaceY.Value - total * unitH : Art.Pot.InnerBottom - Art.Pot.H * 0.5f;
            float cum = 0;
            for (int i = 0; i < Capacity; i++)
            {
                if (i >= units.Count || units[i].h <= 0.001f) { Show(segs[i].gameObject, false); continue; }
                var u = units[i]; var s = segs[i]; Show(s.gameObject, true);
                s.color = u.hidden ? charcoal : Art.Paint[u.color % Art.Paint.Length];
                s.rectTransform.anchoredPosition = new Vector2(0, baseY + cum * unitH);
                s.rectTransform.sizeDelta = new Vector2(700, u.h * unitH + 1f);
                Show(qs[i].gameObject, u.hidden && u.h > 0.6f);
                Show(syms[i].gameObject, symbolsOn && !u.hidden && u.h > 0.6f);
                if (symbolsOn && !u.hidden) syms[i].sprite = Art.Symbol(u.color);
                Show(lines[i].gameObject, u.h > 0.3f);
                cum += u.h;
            }
        }

        public void Pop(int unitIndex)
        {
            if (unitIndex >= 0 && unitIndex < Capacity) StartCoroutine(PopRoutine(segs[unitIndex].rectTransform));
        }
        IEnumerator PopRoutine(RectTransform r)
        {
            for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.35f; float s = 1f + 0.25f * Mathf.Sin(k * Mathf.PI); r.localScale = new Vector3(1f, s, 1f); yield return null;
            }
            r.localScale = Vector3.one;
        }

        public IEnumerator PlayUncover()
        {
            var r = cover.rectTransform; var cg = cover; float dur = 0.5f / Mathf.Max(0.05f, App.I.AnimSpeed);
            cover.gameObject.SetActive(true);
            Fx.Burst(Rt.parent, Rt.anchoredPosition, Art.Hex("D9B382"), 12, 520, 26);
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float k = t / dur; r.localScale = Vector3.one * (1f + k * 0.35f); r.localRotation = Quaternion.Euler(0, 0, k * 14f);
                var c = cg.color; c.a = 1f - k; cg.color = c; coverText.color = new Color(1, 1, 1, 1f - k); yield return null;
            }
            r.localScale = Vector3.one; r.localRotation = Quaternion.identity; cg.color = Color.white; coverText.color = Color.white;
            cover.gameObject.SetActive(false);
        }
    }

    /// <summary>Lays out pots, translates taps to the game, and plays selection / pour / reveal animations.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        public Action<int> PotTappedCallback;
        public RectTransform Root;
        public List<PotView> Pots;
        PuzzleState state;
        Image stream, streamGloss;
        public bool Busy { get; private set; }
        Vector2 area;
        readonly List<UnitVis> scratchSrc = new List<UnitVis>(8), scratchDst = new List<UnitVis>(8);   // reused every animation frame (no per-frame allocation)
        const float RowSwitchGain = 1.12f;                                                             // an extra row of pots must be at least 12% larger to win
        const float MaxPotScale = 1f;                                                                  // 1.0 = the pot sprite at native resolution

        public void Init(RectTransform root) { Root = root; Pots = new List<PotView>(); }

        /// <summary>Called when the available board rectangle changes (rotation, window resize). Re-flows the pots without touching game state.</summary>
        public void SetArea(Vector2 newArea)
        {
            if ((newArea - area).sqrMagnitude < 0.25f) return;
            area = newArea;
            if (Pots != null && Pots.Count > 0 && !Busy) Layout();
        }

        public void SetFocus(int index) { for (int i = 0; i < Pots.Count; i++) Pots[i].SetFocused(i == index); }

        public PotView NearestPot(int from, Vector2 dir)
        {
            if (Pots.Count == 0) return null;
            if (from < 0 || from >= Pots.Count) return Pots[0];
            PotView best = null; float bestScore = float.MaxValue; var origin = Pots[from].RestPos;
            for (int i = 0; i < Pots.Count; i++)
            {
                if (i == from) continue;
                var d = Pots[i].RestPos - origin; float along = Vector2.Dot(d, dir);
                if (along <= 1f) continue;
                float across = Mathf.Abs(d.x * dir.y - d.y * dir.x);
                float score = along + across * 2.2f;                               // favour pots that are more "in line" with the direction pressed
                if (score < bestScore) { bestScore = score; best = Pots[i]; }
            }
            return best;
        }

        public void Tapped(int i) { if (!Busy) PotTappedCallback?.Invoke(i); }

        public void Clear()
        {
            StopAllCoroutines(); Busy = false;
            foreach (var p in Pots) if (p != null) Destroy(p.gameObject);
            Pots.Clear();
            if (stream != null) Destroy(stream.gameObject); stream = null;
            Fx.ReleaseAll();
        }

        public void Build(PuzzleState s, Vector2 availableArea)
        {
            Clear(); state = s; area = availableArea;
            for (int i = 0; i < s.Pots.Count; i++) AddPotView(i);
            stream = UI.Img(Root, "Stream", Art.Round, Color.white, true); stream.gameObject.SetActive(false);
            streamGloss = UI.Img(stream.transform, "Gloss", Art.Round, new Color(1, 1, 1, 0.35f), true);
            Layout();
            RenderAll();
        }

        void AddPotView(int i)
        {
            var go = new GameObject("Pot" + i, typeof(RectTransform));
            go.transform.SetParent(Root, false);
            var pv = go.AddComponent<PotView>(); pv.Index = i; pv.Board = this; pv.Build(state.Pots[i].Capacity);
            Pots.Add(pv);
        }

        public void AddPot()
        {
            AddPotView(state.Pots.Count - 1);
            stream.transform.SetAsLastSibling();
            Layout(); RenderAll();
            Pots[Pots.Count - 1].Pop(0);
        }

        public void Layout()
        {
            int n = Pots.Count; float best = 0; int bestRows = 1;
            const float gapX = 28f, gapY = 70f;
            for (int rows = 1; rows <= 3; rows++)
            {
                int cols = Mathf.CeilToInt(n / (float)rows);
                float s = Mathf.Min(area.x / (cols * (Art.Pot.W * Art.Pot.SlotWidth + gapX)), area.y / (rows * (Art.Pot.H + gapY)), MaxPotScale);
                if (best <= 0f || s > best * RowSwitchGain) { best = s; bestRows = rows; }   // more rows only when the pots get clearly bigger
            }
            int perRow = Mathf.CeilToInt(n / (float)bestRows);
            float potW = Art.Pot.W * Art.Pot.SlotWidth * best, potH = Art.Pot.H * best;
            float rowH = potH + gapY * best;
            float topY = (bestRows * rowH - gapY * best) * 0.5f - potH * 0.5f;
            int idx = 0;
            for (int r = 0; r < bestRows; r++)
            {
                int inRow = Mathf.Min(perRow, n - idx);
                float rowW = inRow * potW + (inRow - 1) * gapX * best;
                for (int c = 0; c < inRow; c++, idx++)
                {
                    var pv = Pots[idx];
                    pv.Scale = best; pv.Rt.localScale = Vector3.one * best;
                    var pos = new Vector2(-rowW * 0.5f + potW * 0.5f + c * (potW + gapX * best), topY - r * rowH);
                    pv.RestPos = pos;
                    if (!pv.AnimControlled) pv.ApplyRest();
                }
            }
        }

        public void RenderAll()
        {
            for (int i = 0; i < Pots.Count && i < state.Pots.Count; i++) RenderPot(i);
        }

        public static List<UnitVis> UnitsOf(PuzzleState.Pot p)
        {
            var l = new List<UnitVis>(); for (int k = 0; k < p.Count; k++) l.Add(new UnitVis(p.Colors[k], p.Hidden[k]));
            return l;
        }

        public void RenderPot(int i)
        {
            var p = state.Pots[i]; var v = Pots[i];
            v.SetSymbolsVisible(App.I.Save.Data.colorAssist);
            var units = UnitsOf(p);
            if (App.I.XRay) for (int k = 0; k < units.Count; k++) { var u = units[k]; u.hidden = false; units[k] = u; }
            v.ShowUnits(units);
            v.SetCovered(p.Covered && !App.I.XRay); v.SetComplete(p.Complete && !p.Covered);
        }

        public void SetSelected(int i, bool on) { if (i >= 0 && i < Pots.Count) Pots[i].SetSelected(on); }
        public void SetMarked(int i, bool on) { if (i >= 0 && i < Pots.Count) Pots[i].SetMarked(on); }

        public void Shake(int i, int j = -1) { StartCoroutine(ShakeRoutine(i)); if (j >= 0) StartCoroutine(ShakeRoutine(j)); }
        IEnumerator ShakeRoutine(int i)
        {
            if (i < 0 || i >= Pots.Count) yield break;
            var v = Pots[i]; v.AnimControlled = true; var start = v.Rt.anchoredPosition; var rest = v.RestPos;
            float dur = 0.28f;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float k = t / dur; v.Rt.anchoredPosition = start + new Vector2(Mathf.Sin(k * Mathf.PI * 6f) * 14f * (1 - k) * v.Scale, 0); yield return null;
            }
            v.Rt.anchoredPosition = start; v.AnimControlled = false; v.ApplyRest();
        }

        static Vector2 Rot(Vector2 v, float deg) { float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r); return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c); }
        static float Ease(float t) => t * t * (3f - 2f * t);

        /// <summary>Animates one committed pour. `state` already holds the final logic; srcBefore/dstBefore are the pre-move layers.</summary>
        /// <summary>
        /// Starts the pour animation on this component, so tearing the board down (<see cref="Clear"/>) cancels it. Running it on another
        /// MonoBehaviour would leave it animating destroyed pots and firing its completion callback against the next level.
        /// </summary>
        public void PlayMove(MoveResult r, List<UnitVis> srcBefore, List<UnitVis> dstBefore, System.Action done) => StartCoroutine(PlayPour(r, srcBefore, dstBefore, done));

        IEnumerator PlayPour(MoveResult r, List<UnitVis> srcBefore, List<UnitVis> dstBefore, System.Action done)
        {
            Busy = true;
            var src = Pots[r.From]; var dst = Pots[r.To];
            float spd = App.I.AnimSpeed;
            if (spd <= 0.001f || App.I.Save.Data.reducedMotion)
            {
                Busy = false; RenderAll(); Finish(r); done?.Invoke(); yield break;
            }
            App.I.Audio.Play("select");
            src.AnimControlled = true; dst.SetSelected(false); src.SetSelected(false); src.SnapLift();
            src.Rt.SetAsLastSibling(); stream.transform.SetSiblingIndex(src.Rt.GetSiblingIndex()); // stream below source, above everything else
            int dir = dst.RestPos.x >= src.RestPos.x ? 1 : -1;
            float s = src.Scale;
            Vector2 lipLocal = new Vector2(Art.Pot.LipX * dir, Art.Pot.LipY) * s;
            Vector2 dstTop = dst.RestPos + new Vector2(0, Art.Pot.DstTopY * dst.Scale);
            Vector2 target = dstTop + new Vector2(-24f * dir * dst.Scale, 62f * s);
            Color col = Art.Paint[r.Color % Art.Paint.Length];
            int srcCountAfter = srcBefore.Count - r.Amount;
            float cap = src.Capacity;
            Vector2 restPos = src.RestPos;
            var srcOriginal = new List<UnitVis>(srcBefore);

            // phase 1: lift + travel + tilt
            float travel = 0.38f / spd;
            float a0 = 52f, a1 = 82f;
            for (float t = 0; t < travel; t += Time.unscaledDeltaTime)
            {
                float e = Ease(t / travel); float ang = a0 * e;
                Vector2 end = target - Rot(lipLocal, -dir * ang);
                src.Rt.anchoredPosition = Vector2.Lerp(restPos + Vector2.up * 40f * s, end, e);
                src.SetTilt(-dir * ang);
                src.ShowUnits(srcOriginal, SurfaceY(ang, dir, srcOriginal.Count, cap, e));
                yield return null;
            }
            // phase 2: pour
            float pourDur = (0.35f + 0.28f * r.Amount) / spd;
            App.I.Audio.Play("pour");
            stream.gameObject.SetActive(true); stream.color = col;
            for (float t = 0; t < pourDur; t += Time.unscaledDeltaTime)
            {
                float k = t / pourDur; float prog = k * r.Amount;
                float ang = Mathf.Lerp(a0, a1, Ease(k));
                Vector2 end = target - Rot(lipLocal, -dir * ang);
                src.Rt.anchoredPosition = end; src.SetTilt(-dir * ang);

                var sv = scratchSrc; sv.Clear(); sv.AddRange(srcBefore);
                for (int i = 0; i < r.Amount; i++) { int idx = srcBefore.Count - 1 - i; var u = sv[idx]; u.h = Mathf.Clamp01(1f - (prog - i)); sv[idx] = u; }
                float remaining = srcCountAfter + (r.Amount - prog);
                src.ShowUnits(sv, SurfaceY(ang, dir, remaining, cap, 1f));

                var dv = scratchDst; dv.Clear(); dv.AddRange(dstBefore);
                for (int i = 0; i < r.Amount; i++) dv.Add(new UnitVis(r.Color, false, Mathf.Clamp01(prog - i)));
                float dstFill = dstBefore.Count + prog;
                dst.ShowUnits(dv);

                // stream from the lip down to the destination paint surface
                Vector2 lipPos = src.Rt.anchoredPosition + Rot(lipLocal, -dir * ang);
                float surfY = dst.RestPos.y + (Art.Pot.InnerBottom - Art.Pot.H * 0.5f + dstFill * (Art.Pot.FillHeight / dst.Capacity)) * dst.Scale;
                float grow = Mathf.Clamp01(t / (0.12f / spd)); float shrink = 1f - Mathf.Clamp01((t - (pourDur - 0.12f / spd)) / (0.12f / spd));
                float topY = Mathf.Lerp(lipPos.y, surfY, 1f - shrink);
                float botY = Mathf.Lerp(lipPos.y, surfY, grow);
                float w = 24f * s * (1f + 0.15f * Mathf.Sin(t * 30f));
                stream.rectTransform.anchorMin = stream.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                stream.rectTransform.pivot = new Vector2(0.5f, 1f);
                stream.rectTransform.anchoredPosition = new Vector2(lipPos.x, topY);
                stream.rectTransform.sizeDelta = new Vector2(w, Mathf.Max(2f, topY - botY));
                streamGloss.rectTransform.anchorMin = new Vector2(0.15f, 0); streamGloss.rectTransform.anchorMax = new Vector2(0.4f, 1);
                streamGloss.rectTransform.offsetMin = streamGloss.rectTransform.offsetMax = Vector2.zero;
                yield return null;
            }
            stream.gameObject.SetActive(false);
            // phase 3: return
            float back = 0.32f / spd; Vector2 fromPos = src.Rt.anchoredPosition;
            var srcAfter = new List<UnitVis>(srcBefore); srcAfter.RemoveRange(srcAfter.Count - r.Amount, r.Amount);
            for (float t = 0; t < back; t += Time.unscaledDeltaTime)
            {
                float e = Ease(t / back);
                float ang = Mathf.Lerp(a1, 0, e);
                src.Rt.anchoredPosition = Vector2.Lerp(fromPos, restPos, e); src.SetTilt(-dir * ang);
                src.ShowUnits(srcAfter, SurfaceY(ang, dir, srcAfter.Count, cap, 1f - e));
                yield return null;
            }
            src.Rt.anchoredPosition = restPos; src.SetTilt(0f); src.AnimControlled = false;
            Busy = false;
            RenderAll();
            Finish(r);
            done?.Invoke();
        }

        void Finish(MoveResult r)
        {
            if (r.RevealedPot >= 0) { Pots[r.RevealedPot].Pop(state.Pots[r.RevealedPot].Count - 1); App.I.Audio.Play("reveal"); }
            if (r.CompletedPot >= 0)
            {
                var v = Pots[r.CompletedPot]; App.I.Audio.Play("complete"); Haptics.Medium();
                Fx.Burst(Root, v.RestPos + new Vector2(0, 40), Art.Paint[r.Color % Art.Paint.Length], 10, 480, 30);
                Fx.Burst(Root, v.RestPos + new Vector2(0, 90), Color.white, 8, 380, 40, true, 0.9f);
            }
            foreach (var u in r.UnlockedPots) StartCoroutine(Pots[u].PlayUncover());
        }

        /// <summary>Surface height (pot-centre-relative, level in the world) of the paint while tilted; blends with the resting height.</summary>
        float SurfaceY(float angDeg, int dir, float units, float cap, float blend)
        {
            float restS = Art.Pot.InnerBottom - Art.Pot.H * 0.5f + units * (Art.Pot.FillHeight / cap);
            if (angDeg < 0.5f) return restS;
            float a = -dir * angDeg;
            float lip = Rot(new Vector2(Art.Pot.LipX * dir, Art.Pot.LipY), a).y;
            float low = float.MaxValue;
            var corners = Art.Pot.Corners; for (int i = 0; i < corners.Length; i++) low = Mathf.Min(low, Rot(corners[i], a).y);
            float fill = Mathf.Clamp01(units / cap);
            float tilted = Mathf.Lerp(low + 10f, lip - 6f, fill);
            return Mathf.Lerp(restS, tilted, Mathf.Clamp01(blend));
        }

        /// <summary>Paint rain plus a Mexican-wave hop across the pots.</summary>
        public void PlayWinFx()
        {
            Fx.PaintRain(Root, area * 1.5f, 50);
            if (!App.I.Save.Data.reducedMotion) StartCoroutine(WinWave());
        }

        IEnumerator WinWave()
        {
            const float hop = 0.4f, stagger = 0.06f, height = 46f;
            float total = hop + stagger * Pots.Count;
            foreach (var p in Pots) p.AnimControlled = true;
            for (float t = 0; t < total; t += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < Pots.Count; i++)
                {
                    float k = Mathf.Clamp01((t - i * stagger) / hop);
                    Pots[i].Rt.anchoredPosition = Pots[i].RestPos + Vector2.up * (Mathf.Sin(k * Mathf.PI) * height * Pots[i].Scale);
                }
                yield return null;
            }
            foreach (var p in Pots) { p.AnimControlled = false; p.ApplyRest(); }
        }
    }
}
