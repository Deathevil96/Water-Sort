using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PaintSort.Game
{
    /// <summary>Small code-driven cartoon UI toolkit (thick ink outlines, glossy fills, chunky text).</summary>
    public static class UI
    {
        public static readonly Color Ink = Art.Ink;
        public static readonly Color Cream = Art.Cream;
        public static readonly Color Green = Art.Hex("5BD48A");
        public static readonly Color Orange = Art.Hex("FF9F43");
        public static readonly Color Blue = Art.Hex("4DA8FF");
        public static readonly Color Pink = Art.Hex("FF6FA8");
        public static readonly Color Purple = Art.Hex("9B6BFF");
        public static readonly Color Red = Art.Hex("FF5A6E");
        public static readonly Color Grey = Art.Hex("B9B3C6");
        public static readonly Color Gold = Art.Hex("FFC83D");
        public static readonly Color GoldEdge = Art.Hex("F4B02A");
        public static readonly Color FocusRing = Art.Hex("FFE066");

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform r, float l = 0, float b = 0, float rt = 0, float t = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rt, -t);
        }

        public static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size;
        }

        /// <summary>
        /// Gives a UI subtree its own Canvas so a change inside it only rebuilds that subtree's mesh, not the whole screen.
        /// Use on groups that update at different rates (HUD vs board vs popups).
        /// </summary>
        public static Canvas SubCanvas(RectTransform r, bool raycast = true)
        {
            var c = r.gameObject.GetComponent<Canvas>() ?? r.gameObject.AddComponent<Canvas>();
            if (raycast && r.GetComponent<GraphicRaycaster>() == null) r.gameObject.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static Image Img(Transform parent, string name, Sprite s, Color c, bool sliced = false, bool raycast = false)
        {
            var r = Rect(parent, name); var i = r.gameObject.AddComponent<Image>();
            i.sprite = s; i.color = c; i.raycastTarget = raycast; if (sliced) i.type = Image.Type.Sliced; i.preserveAspect = !sliced;
            return i;
        }

        public static Text Txt(Transform parent, string text, int size, Color col, TextAnchor anchor = TextAnchor.MiddleCenter, bool outline = true)
        {
            var r = Rect(parent, "Text"); var t = r.gameObject.AddComponent<Text>();
            t.font = Art.GameFont; t.fontStyle = Art.CustomFont ? FontStyle.Normal : FontStyle.Bold; t.text = Loc.T(text); t.fontSize = size; t.color = col; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            t.resizeTextForBestFit = false;
            if (outline) { var o = r.gameObject.AddComponent<Outline8>(); o.effectColor = Ink; o.effectDistance = new Vector2(Mathf.Max(2, size / 15f), Mathf.Max(2, size / 15f)); o.useGraphicAlpha = true; }
            Stretch(r);
            return t;
        }

        /// <summary>Rounded card with ink outline and soft drop shadow. Content goes into the returned inner rect.</summary>
        public static RectTransform Card(Transform parent, Color face, Vector2 size, float border = 8f, bool shadow = true)
        {
            var root = Rect(parent, "Card"); root.sizeDelta = size;
            if (shadow) { var sh = Img(root, "Shadow", Art.RoundSoft, new Color(Ink.r, Ink.g, Ink.b, 0.35f), true); Stretch(sh.rectTransform, -6, -16, -6, 6); }
            var o = Img(root, "Outline", Art.Round, Ink, true, true); Stretch(o.rectTransform);
            var f = Img(root, "Face", Art.Round, face, true); Stretch(f.rectTransform, border, border, border, border);
            var hi = Img(root, "Shine", Art.Round, new Color(1, 1, 1, 0.22f), true); Stretch(hi.rectTransform, border + 8, border + size.y * 0.55f, border + 8, border + 6);
            var inner = Rect(root, "Content"); Stretch(inner, border, border, border, border);
            return inner;
        }

        /// <summary>The glossy candy look shared by every button: focus ring, soft shadow, gold edge (returned, it is the tap target), dark rim, colour face, light top half and a thin glint.</summary>
        static Image GlossLayers(RectTransform root, Color face, Vector2 size, out Image gold)
        {
            var ring = Img(root, "Focus", Art.Round, FocusRing, true); Stretch(ring.rectTransform, -14, -14, -14, -14); ring.gameObject.SetActive(false);   // shown while focused by keyboard / gamepad / D-pad
            var sh = Img(root, "Shadow", Art.RoundSoft, new Color(0.15f, 0.05f, 0.25f, 0.45f), true); Stretch(sh.rectTransform, -4, -16, -4, 6);
            float e = Mathf.Max(3f, Mathf.Min(size.y, size.x) * 0.04f);                                  // edge thickness scales with the button
            gold = Img(root, "Outline", Art.Round, GoldEdge, true, true); Stretch(gold.rectTransform);
            var rim = Img(root, "Rim", Art.Round, Color.Lerp(face, Color.black, 0.55f), true); Stretch(rim.rectTransform, e, e * 0.9f, e, e);
            var f = Img(root, "Face", Art.Round, face, true); Stretch(f.rectTransform, e * 1.8f, e * 1.9f, e * 1.8f, e * 1.8f);
            var top = Img(root, "Top", Art.Round, new Color(1, 1, 1, 0.13f), true); Stretch(top.rectTransform, e * 2.2f, size.y * 0.46f, e * 2.2f, e * 2.2f);
            var glint = Img(root, "Shine", Art.Round, new Color(1, 1, 1, 0.32f), true); Stretch(glint.rectTransform, size.x * 0.2f, size.y * 0.8f, size.x * 0.2f, e * 2.8f);
            return ring;
        }

        /// <summary>A glossy banner with a label that is NOT a button (section headers): same look, no tap target.</summary>
        public static RectTransform Banner(Transform parent, string label, Color face, Vector2 size, int fontSize = 46)
        {
            var root = Rect(parent, "Banner_" + label); root.sizeDelta = size;
            GlossLayers(root, face, size, out var gold);
            foreach (var g in root.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            var t = Txt(root, label, fontSize, Color.white); t.rectTransform.offsetMin = new Vector2(8, 4);
            var ol = t.GetComponent<Outline>(); if (ol != null) ol.effectColor = Color.Lerp(face, Color.black, 0.65f);
            return root;
        }

        public static Button Btn(Transform parent, string label, Color face, Vector2 size, Action onClick, Sprite icon = null, int fontSize = 44)
        {
            var root = Rect(parent, "Btn_" + label); root.sizeDelta = size;
            var ring = GlossLayers(root, face, size, out var o);
            var b = root.gameObject.AddComponent<Button>(); b.targetGraphic = o; b.transition = Selectable.Transition.None;
            var fx = root.gameObject.AddComponent<PressFx>(); fx.Ring = ring.gameObject;
            float iconPad = 0;
            if (icon != null)
            {
                var ic = Img(root, "Icon", icon, Color.white); float s = Mathf.Min(size.y * 0.72f, size.x * 0.6f);
                if (string.IsNullOrEmpty(label)) Place(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s, s));
                else { Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(size.y * 0.22f, 0), new Vector2(s, s)); iconPad = s * 0.5f; }
            }
            if (!string.IsNullOrEmpty(label))
            {
                var t = Txt(root, label, fontSize, Color.white); t.rectTransform.offsetMin = new Vector2(iconPad * 1.4f, 4); t.rectTransform.offsetMax = new Vector2(0, 0);
                var ol = t.GetComponent<Outline>(); if (ol != null) ol.effectColor = Color.Lerp(face, Color.black, 0.65f);
            }
            if (onClick != null) b.onClick.AddListener(() => { App.I?.Audio.Play("tap"); onClick(); });
            return b;
        }

        /// <summary>
        /// Big glossy "candy" button: gold edge, dark rim, colour face with a lighter top half. The face is named "Face" so SetEnabled / SetLocked still tint it.
        /// </summary>
        public static Button GlossBtn(Transform parent, string label, Color face, Vector2 size, Action onClick, int fontSize = 56)
        {
            var root = Rect(parent, "Btn_" + label); root.sizeDelta = size;
            var ring = GlossLayers(root, face, size, out var gold);
            float e = Mathf.Max(5f, size.y * 0.06f);
            var b = root.gameObject.AddComponent<Button>(); b.targetGraphic = gold; b.transition = Selectable.Transition.None;
            var fx = root.gameObject.AddComponent<PressFx>(); fx.Ring = ring.gameObject;
            if (!string.IsNullOrEmpty(label))
            {
                var t = Txt(root, label, fontSize, Color.white); t.rectTransform.offsetMin = new Vector2(8, e); t.rectTransform.offsetMax = new Vector2(-8, 0);
                var ol = t.GetComponent<Outline>(); if (ol != null) { ol.effectColor = Color.Lerp(face, Color.black, 0.65f); ol.effectDistance = new Vector2(Mathf.Max(2.5f, fontSize / 11f), -Mathf.Max(2.5f, fontSize / 11f)); }
            }
            if (onClick != null) b.onClick.AddListener(() => { App.I?.Audio.Play("tap"); onClick(); });
            return b;
        }

        public static void SetEnabled(Button b, bool on)
        {
            b.interactable = on;
            var face = b.transform.Find("Face");
            if (face == null) return;
            var img = face.GetComponent<Image>();
            var m = b.GetComponent<ButtonMemo>() ?? b.gameObject.AddComponent<ButtonMemo>();
            if (m.face == default) m.face = img.color;
            img.color = on ? m.face : Grey;
            var edge = b.transform.Find("Rim") != null ? b.transform.Find("Outline")?.GetComponent<Image>() : null;      // glossy buttons: gold edge goes dull when disabled
            if (edge != null) edge.color = on ? GoldEdge : Art.Hex("A9A3B8");
            TintRim(b, img.color);
        }

        /// <summary>Greys a button and shows a padlock badge while keeping it clickable (so tapping it can explain when it unlocks).</summary>
        public static void SetLocked(Button b, bool locked)
        {
            var face = b.transform.Find("Face"); if (face == null) return;
            var img = face.GetComponent<Image>();
            var m = b.GetComponent<ButtonMemo>() ?? b.gameObject.AddComponent<ButtonMemo>();
            if (m.face == default) m.face = img.color;
            var badge = b.transform.Find("LockBadge");
            if (locked)
            {
                img.color = Grey; TintRim(b, Grey);
                if (badge == null)
                {
                    var lk = Img(b.transform, "LockBadge", Art.Lock, Color.white);
                    Place(lk.rectTransform, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-22, 26), new Vector2(60, 60));
                    badge = lk.transform;
                }
                badge.gameObject.SetActive(true);
            }
            else
            {
                img.color = m.face; TintRim(b, m.face);
                if (badge != null) badge.gameObject.SetActive(false);
            }
        }

        /// <summary>Keeps a glossy button's dark rim in step with its face colour (so a greyed button does not keep a blue rim).</summary>
        static void TintRim(Button b, Color face) { var rim = b.transform.Find("Rim"); if (rim != null) rim.GetComponent<Image>().color = Color.Lerp(face, Color.black, 0.55f); }

        public static void SetLabel(Button b, string text) { var t = b.GetComponentInChildren<Text>(); if (t != null) t.text = text; }

        public static Image Dim(Transform parent, float alpha = 0.55f)
        {
            var d = Img(parent, "Dim", Art.White, new Color(Ink.r, Ink.g, Ink.b, alpha), false, true); Stretch(d.rectTransform); d.preserveAspect = false; return d;
        }
    }

    /// <summary>
    /// Smooth text outline: the standard Outline only copies the text to the four corners, which leaves jagged notches on big letters.
    /// This draws eight copies (corners and sides), so thick outlines look round and clean. Same API as Outline (effectColor / effectDistance).
    /// </summary>
    public sealed class Outline8 : Outline
    {
        static readonly Vector2[] Dirs = { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1), new Vector2(0.7071f, 0.7071f), new Vector2(-0.7071f, 0.7071f), new Vector2(0.7071f, -0.7071f), new Vector2(-0.7071f, -0.7071f) };
        static readonly List<UIVertex> Buffer = new List<UIVertex>(512);

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            Buffer.Clear(); vh.GetUIVertexStream(Buffer);
            int n = Buffer.Count; if (n == 0) return;
            var all = new List<UIVertex>(n * 9);
            float d = Mathf.Abs(effectDistance.x);
            for (int k = 0; k < Dirs.Length; k++)
                for (int i = 0; i < n; i++)
                {
                    var v = Buffer[i]; v.position += new Vector3(Dirs[k].x * d, Dirs[k].y * d, 0);
                    var c = effectColor; if (useGraphicAlpha) c.a = (byte)(c.a * v.color.a / 255); v.color = c; all.Add(v);
                }
            for (int i = 0; i < n; i++) all.Add(Buffer[i]);
            vh.Clear(); vh.AddUIVertexTriangleStream(all);
        }
    }

    public sealed class ButtonMemo : MonoBehaviour { public Color face; }

    /// <summary>
    /// Pulsing highlight ring that draws attention to a newly introduced button. It lives in its own sub-canvas, so animating it every frame
    /// only rebuilds this tiny ring and never the rest of the screen.
    /// </summary>
    public sealed class PulseRing : MonoBehaviour
    {
        void Update() { transform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f)); }
    }

    /// <summary>Which input device the player last used. Focus rings and the board cursor are only drawn after keyboard / gamepad / D-pad input.</summary>
    public static class InputMode
    {
        public static bool FocusVisible;
    }

    /// <summary>
    /// Squishy press feedback and a focus highlight. Idle buttons do no work: the transform is only written while it is still moving,
    /// because writing a transform every frame would dirty the canvas and force a full rebuild each frame.
    /// </summary>
    public sealed class PressFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        const float PressedScale = 0.94f, FocusedScale = 1.07f, Speed = 22f, Epsilon = 0.0008f;
        public GameObject Ring;
        Vector3 baseScale = Vector3.one;
        bool down, focused, settled = true;

        void Awake() { baseScale = transform.localScale; }

        void Update()
        {
            if (settled) return;
            float factor = down ? PressedScale : focused ? FocusedScale : 1f;
            var target = baseScale * factor;
            var cur = transform.localScale;
            if ((cur - target).sqrMagnitude < Epsilon * Epsilon) { transform.localScale = target; settled = true; return; }
            transform.localScale = Vector3.Lerp(cur, target, 1 - Mathf.Exp(-Time.unscaledDeltaTime * Speed));
        }

        void Retarget() { settled = false; }
        public void OnPointerDown(PointerEventData e) { down = true; InputMode.FocusVisible = false; Retarget(); }
        public void OnPointerUp(PointerEventData e) { down = false; Retarget(); }
        public void OnPointerExit(PointerEventData e) { if (!down) return; down = false; Retarget(); }
        public void OnSelect(BaseEventData e) { focused = InputMode.FocusVisible; if (Ring != null) Ring.SetActive(focused); Retarget(); }
        public void OnDeselect(BaseEventData e) { focused = false; if (Ring != null) Ring.SetActive(false); Retarget(); }
        void OnDisable() { down = false; focused = false; if (Ring != null) Ring.SetActive(false); if (transform != null) transform.localScale = baseScale; settled = true; }
    }

    /// <summary>
    /// Pooled UI particles (paint dots, sparkles, confetti). One component ticks every particle, nothing is instantiated after warm-up,
    /// and nothing runs while no particle is alive.
    /// </summary>
    public sealed class FxHost : MonoBehaviour
    {
        sealed class Particle
        {
            public RectTransform Rt; public Image Img;
            public Vector2 Vel, Gravity; public float Life, Age, Spin, StartScale; public Color Base;
        }

        public static FxHost Instance { get; private set; }

        readonly List<Particle> live = new List<Particle>(128);
        readonly Stack<Particle> pool = new Stack<Particle>(128);
        RectTransform poolRoot;

        public static FxHost Create(Transform canvasRoot)
        {
            var go = new GameObject("FxHost", typeof(RectTransform));
            go.transform.SetParent(canvasRoot, false);
            var host = go.AddComponent<FxHost>();
            host.poolRoot = UI.Rect(go.transform, "Pool"); host.poolRoot.gameObject.SetActive(false);
            Instance = host;
            return host;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Spawn(Transform parent, Sprite sprite, Color color, float size, Vector2 localPos, Vector2 vel, Vector2 gravity, float life, float spin, float startScale)
        {
            Particle p;
            if (pool.Count > 0) p = pool.Pop();
            else
            {
                var img = UI.Img(poolRoot, "fx", sprite, color);
                p = new Particle { Img = img, Rt = img.rectTransform };
            }
            p.Img.sprite = sprite; p.Img.color = color;
            p.Rt.SetParent(parent, false); p.Rt.SetAsLastSibling();
            p.Rt.sizeDelta = new Vector2(size, size); p.Rt.localPosition = localPos; p.Rt.localRotation = Quaternion.identity; p.Rt.localScale = Vector3.zero;
            p.Vel = vel; p.Gravity = gravity; p.Life = life; p.Age = 0; p.Spin = spin; p.StartScale = startScale; p.Base = color;
            p.Rt.gameObject.SetActive(true);
            live.Add(p);
        }

        /// <summary>Returns every live particle to the pool (used when a level is torn down).</summary>
        public void ReleaseAll() { for (int i = live.Count - 1; i >= 0; i--) Release(i); }

        void Release(int index)
        {
            var p = live[index];
            live[index] = live[live.Count - 1]; live.RemoveAt(live.Count - 1);
            p.Rt.gameObject.SetActive(false); p.Rt.SetParent(poolRoot, false);
            pool.Push(p);
        }

        void Update()
        {
            if (live.Count == 0) return;
            float dt = Time.unscaledDeltaTime;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var p = live[i];
                p.Age += dt;
                if (p.Age >= p.Life || p.Rt == null) { if (p.Rt != null) Release(i); else live.RemoveAt(i); continue; }
                p.Vel += p.Gravity * dt;
                p.Rt.localPosition += (Vector3)(p.Vel * dt);
                p.Rt.Rotate(0, 0, p.Spin * dt);
                float k = p.Age / p.Life;
                p.Rt.localScale = Vector3.one * (p.StartScale * (k < 0.15f ? k / 0.15f : 1f - Mathf.Clamp01((k - 0.6f) / 0.4f) * 0.6f));
                var c = p.Base; c.a = p.Base.a * (1f - Mathf.Clamp01((k - 0.7f) / 0.3f)); p.Img.color = c;
            }
        }
    }

    public static class Fx
    {
        public static void Burst(Transform parent, Vector2 localPos, Color color, int count = 10, float speed = 420f, float size = 28f, bool sparkle = false, float life = 0.8f)
        {
            var host = FxHost.Instance; if (host == null) return;
            var sprite = sparkle ? Art.Sparkle : Art.Circle;
            for (int i = 0; i < count; i++)
            {
                float a = UnityEngine.Random.Range(0f, Mathf.PI * 2), s = UnityEngine.Random.Range(0.4f, 1f) * speed;
                host.Spawn(parent, sprite, color, size, localPos,
                    new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s, new Vector2(0, -900f),
                    life * UnityEngine.Random.Range(0.7f, 1.2f), UnityEngine.Random.Range(-240f, 240f), UnityEngine.Random.Range(0.7f, 1.2f));
            }
        }

        public static void PaintRain(Transform parent, Vector2 area, int count = 40)
        {
            var host = FxHost.Instance; if (host == null) return;
            for (int i = 0; i < count; i++)
            {
                var col = Art.Paint[UnityEngine.Random.Range(0, Art.Paint.Length)];
                host.Spawn(parent, UnityEngine.Random.value > 0.5f ? Art.Splat : Art.Circle, col, UnityEngine.Random.Range(24f, 60f),
                    new Vector2(UnityEngine.Random.Range(-area.x, area.x) * 0.5f, area.y * 0.5f + UnityEngine.Random.Range(0, 300)),
                    new Vector2(UnityEngine.Random.Range(-120f, 120f), UnityEngine.Random.Range(-200f, -700f)), new Vector2(0, -300f),
                    UnityEngine.Random.Range(1.2f, 2.2f), UnityEngine.Random.Range(-300f, 300f), 1f);
            }
        }

        public static void ReleaseAll() { if (FxHost.Instance != null) FxHost.Instance.ReleaseAll(); }
    }
}
