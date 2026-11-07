using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaintSort.Game
{
    /// <summary>
    /// Cartoon art library. Every sprite is first looked up in Resources/Art/&lt;name&gt; (so AI-generated PNGs drop in with no code changes);
    /// if it is missing a procedural cartoon placeholder (thick ink outline, glossy highlights) is generated at runtime.
    /// </summary>
    public static class Art
    {
        // NOTE: must stay the first static field - the palette below calls Hex(), and static initializers run in textual order.
        static readonly Dictionary<string, Color> hexCache = new Dictionary<string, Color>();

        public static readonly Color Ink = new Color32(0x3A, 0x28, 0x4B, 255);
        public static readonly Color Cream = new Color32(0xFF, 0xF4, 0xDC, 255);

        // GDD 16.2 paint colours, indexed by colour id.
        public static readonly Color[] Paint =
        {
            Hex("E84C5B"), Hex("399CE0"), Hex("F5CE45"), Hex("54C99B"), Hex("9867D5"), Hex("F58D67"),
            Hex("E88DC3"), Hex("4D536B"), Hex("35C6C5"), Hex("9A694D"), Hex("B5A1E8")
        };
        public static readonly string[] PaintNames =
        { "Cherry Red", "Ocean Blue", "Lemon Yellow", "Mint Green", "Grape Purple", "Coral Orange", "Bubblegum Pink", "Ink Navy", "Turquoise", "Warm Brown", "Lavender" };

        /// <summary>Parses "RRGGBB". Results are cached, so it is safe to call from UI code; per-pixel code should still hoist its colours.</summary>
        public static Color Hex(string h)
        {
            if (hexCache.TryGetValue(h, out var c)) return c;
            ColorUtility.TryParseHtmlString("#" + h, out c);
            hexCache[h] = c;
            return c;
        }

        // Colours used inside per-pixel sprite generators (hoisted so they are parsed once, not once per pixel).
        static readonly Color RimCream = Hex("FFF6E2"), CoverTan = Hex("D9B382"), RopeBrown = Hex("8B5E3C");
        static readonly Color GoldMain = Hex("FFC83D"), GoldDark = Hex("E39A1C"), GoldLight = Hex("FFF1A8");
        static readonly Color LockSilver = Hex("BFC4D6"), GearFill = Hex("D7DCEC");
        static readonly Color GlassTint = new Color(0.86f, 0.96f, 1f, 0.16f);

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        // ---- pot geometry (sprite pixel space, y up). Everything about the pot lives in this 256x384 frame ----
        public const int PotW = 256, PotH = 384;
        public const float PotInnerBottom = 24f, UnitH = 76f; // 4 units = 304px; interior ends just below the rim (procedural placeholder pot)

        /// <summary>
        /// Everything the board needs to know about the pot's shape. All values are in frame units (the pot's own W x H rectangle), measured from the
        /// frame centre unless noted. The placeholder pot and the supplied artwork have different shapes, so the right set is picked by whether
        /// Resources/Art/pot.png exists. The whole pot set (pot, pot_mask, pot_back, pot_cover, pot_shadow) must be supplied together.
        /// </summary>
        public sealed class PotGeometry
        {
            public float W, H;
            public float InnerBottom;      // where paint starts, measured from the frame's bottom edge
            public float FillHeight;       // height of a completely full pot of paint (all layers)
            public float LipX, LipY;       // the pouring spout, relative to the centre (x is mirrored for the other side)
            public float DstTopY;          // top of the rim, relative to the centre (where a pour stream aims)
            public float ShadowY, ShadowW, ShadowH;
            public float SlotWidth;        // fraction of W each pot needs side by side
            public float GlossX;           // paint gloss streak position
            public Vector2[] Corners;      // inside of the pot as a quad (bottom-left, bottom-right, top-left, top-right), for the tilted paint surface
        }

        public static readonly PotGeometry ProceduralPot = new PotGeometry
        {
            W = PotW, H = PotH, InnerBottom = PotInnerBottom, FillHeight = 304f, LipX = 100f, LipY = 178f, DstTopY = 178f,
            ShadowY = -205f, ShadowW = 300f, ShadowH = 75f, SlotWidth = 0.86f, GlossX = -62f,
            Corners = new[] { new Vector2(-88, -176), new Vector2(88, -176), new Vector2(-98, 170), new Vector2(98, 170) },
        };

        /// <summary>Measured from the supplied cup artwork (ArtSource/pot_geometry.json): frame 280 x 326.</summary>
        public static readonly PotGeometry ArtworkPot = new PotGeometry
        {
            W = 280f, H = 326.2f, InnerBottom = 16f, FillHeight = 237f, LipX = 118f, LipY = 131f, DstTopY = 157f,
            ShadowY = -172f, ShadowW = 330f, ShadowH = 70f, SlotWidth = 0.98f, GlossX = -50f,
            Corners = new[] { new Vector2(-72, -147), new Vector2(72, -147), new Vector2(-108, 90), new Vector2(108, 90) },
        };

        static PotGeometry potGeometry;
        public static PotGeometry Pot => potGeometry ?? (potGeometry = Resources.Load<Sprite>("Art/pot") != null ? ArtworkPot : ProceduralPot);

        public static Sprite Get(string name, Func<Sprite> make)
        {
            if (cache.TryGetValue(name, out var s) && s != null) return s;
            var custom = Resources.Load<Sprite>("Art/" + name);
            s = custom != null ? custom : make();
            s.name = name;
            cache[name] = s;
            return s;
        }

        public static Font GameFont
        {
            get
            {
                if (font == null)
                {
                    font = Resources.Load<Font>("Art/GameFont");
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return font;
            }
        }
        static Font font;

        // ---------------------------------------------------------------- raster helpers
        static Sprite Make(int w, int h, Func<float, float, Color> f, Vector4 border = default)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = f(x + 0.5f, y + 0.5f);
            t.SetPixels32(px); t.Apply(true, true);   // upload, then drop the CPU copy
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        static float Cov(float d) => Mathf.Clamp01(0.5f - d);               // anti-aliased coverage from signed distance in px
        static Color With(Color c, float a) { c.a *= a; return c; }
        static Color Over(Color top, Color bottom)                           // straight alpha "over"
        {
            float a = top.a + bottom.a * (1 - top.a);
            if (a <= 0.0001f) return new Color(0, 0, 0, 0);
            Color r = (top * top.a + bottom * bottom.a * (1 - top.a)) / a; r.a = a; return r;
        }

        static float SdBox(Vector2 p, Vector2 c, Vector2 half, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(r, r);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
        }
        static float SdCircle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
        static float SdSeg(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            Vector2 pa = p - a, ba = b - a; float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }
        static float SdPoly(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]); float s = 1;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i], w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }
        static Vector2[] Star(Vector2 c, float ro, float ri, int n, float rot = 90f)
        {
            var v = new Vector2[n * 2];
            for (int i = 0; i < n * 2; i++) { float a = (rot + i * 180f / n) * Mathf.Deg2Rad; float r = i % 2 == 0 ? ro : ri; v[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }
            return v;
        }
        static Vector2[] Ngon(Vector2 c, float r, int n, float rot = 90f)
        {
            var v = new Vector2[n];
            for (int i = 0; i < n; i++) { float a = (rot + i * 360f / n) * Mathf.Deg2Rad; v[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }
            return v;
        }

        /// <summary>Shape drawn as ink outline + coloured fill + soft gloss. dist(p) is the signed distance of the shape in px.</summary>
        static Sprite Icon(int size, Func<Vector2, float> dist, Color fill, float outline = 5f, bool gloss = true)
        {
            return Make(size, size, (x, y) =>
            {
                float d = dist(new Vector2(x, y));
                Color f = fill;
                if (gloss) f = Color.Lerp(f, Color.white, Mathf.Clamp01((y / size - 0.45f) * 0.7f));
                return Over(With(f, Cov(d)), With(Ink, Cov(d - outline)));   // outline = silhouette grown by `outline` px
            });
        }

        // ---------------------------------------------------------------- public sprites
        public static Sprite White => Get("white", () => Make(8, 8, (x, y) => Color.white));

        /// <summary>9-sliced white rounded rect; tint with Image.color.</summary>
        public static Sprite Round => Get("round", () => Make(96, 96, (x, y) => With(Color.white, Cov(SdBox(new Vector2(x, y), new Vector2(48, 48), new Vector2(48, 48), 36))), new Vector4(40, 40, 40, 40)));
        public static Sprite RoundSoft => Get("roundsoft", () => Make(96, 96, (x, y) => { float d = SdBox(new Vector2(x, y), new Vector2(48, 48), new Vector2(46, 46), 30); return With(Color.white, Mathf.Clamp01(0.5f - d / 6f)); }, new Vector4(40, 40, 40, 40)));
        public static Sprite Circle => Get("circle", () => Make(128, 128, (x, y) => With(Color.white, Cov(SdCircle(new Vector2(x, y), new Vector2(64, 64), 62)))));

        public static Sprite Heart => Get("heart", () => Icon(96, p =>
        {
            var q = new Vector2((p.x - 48) / 36f, (p.y - 46) / 36f);
            // implicit heart: (x²+y²-1)³ - x²y³ = 0  → approximate distance by scaling
            float v = Mathf.Pow(q.x * q.x + q.y * q.y - 1, 3) - q.x * q.x * q.y * q.y * q.y;
            return v * 22f;
        }, Hex("FF5A6E"), 4));
        public static Sprite Coin => Get("coin", () => Make(96, 96, (x, y) =>
        {
            var p = new Vector2(x, y); var c = new Vector2(48, 48);
            Color col = With(Ink, Cov(SdCircle(p, c, 44)));
            col = Over(With(GoldMain, Cov(SdCircle(p, c, 38))), col);
            col = Over(With(GoldDark, Cov(Mathf.Abs(SdCircle(p, c, 27)) - 3f)), col);
            col = Over(With(GoldLight, Cov(SdBox(p, new Vector2(48, 48), new Vector2(5, 15), 3))), col);
            col = Over(With(Color.white, 0.55f * Cov(SdCircle(p, new Vector2(32, 66), 7))), col);
            return col;
        }));
        public static Sprite StarIcon => Get("star", () => Icon(128, p => SdPoly(p, Star(new Vector2(64, 62), 50, 22, 5)) - 0f, Hex("FFD23F"), 6));
        public static Sprite Check => Get("check", () => Make(96, 96, (x, y) =>
        {
            var p = new Vector2(x, y);
            float d = Mathf.Min(SdSeg(p, new Vector2(22, 50), new Vector2(40, 30), 8), SdSeg(p, new Vector2(40, 30), new Vector2(74, 70), 8));
            return Over(With(Color.white, Cov(d)), With(Ink, Cov(d - 5)));
        }));
        public static Sprite Plus => Get("plus", () => Make(96, 96, (x, y) =>
        {
            var p = new Vector2(x, y);
            float d = Mathf.Min(SdBox(p, new Vector2(48, 48), new Vector2(26, 8), 6), SdBox(p, new Vector2(48, 48), new Vector2(8, 26), 6));
            return Over(With(Color.white, Cov(d)), With(Ink, Cov(d - 5)));
        }));
        public static Sprite Lock => Get("lock", () => Make(96, 96, (x, y) =>
        {
            var p = new Vector2(x, y);
            float body = SdBox(p, new Vector2(48, 36), new Vector2(26, 22), 8);
            float dx = Mathf.Abs(p.x - 48f);
            // shackle: a ring arc above y=62, two straight legs from y=62 down to the body, nothing below
            float shackle = p.y >= 62f ? Mathf.Abs(SdCircle(p, new Vector2(48, 62), 17f)) - 5f
                          : p.y >= 40f ? Mathf.Abs(dx - 17f) - 5f : 99f;
            Color col = With(Ink, Cov(Mathf.Min(body, shackle) - 4f));
            col = Over(With(GoldMain, Cov(body)), col);
            col = Over(With(LockSilver, Cov(shackle)), col);
            col = Over(With(Ink, Cov(SdCircle(p, new Vector2(48, 38), 5))), col);       // keyhole
            return col;
        }));
        public static Sprite Gear => Get("gear", () => Make(96, 96, (x, y) =>
        {
            var p = new Vector2(x, y) - new Vector2(48, 48);
            float angle = Mathf.Atan2(p.y, p.x), r = p.magnitude;
            float outerEdge = 36f + 7f * Mathf.Clamp(Mathf.Sin(angle * 8f) * 3f, -1f, 1f);   // toothed rim
            float shape = Mathf.Max(r - outerEdge, 14f - r);                                  // gear body with a centre hole
            return Over(With(GearFill, Cov(shape)), With(Ink, Cov(shape - 5f)));
        }));
        public static Sprite BackArrow => Get("back", () => Make(96, 96, (x, y) =>
        {
            var p = new Vector2(x, y);
            float d = Mathf.Min(SdSeg(p, new Vector2(60, 24), new Vector2(34, 48), 8), SdSeg(p, new Vector2(34, 48), new Vector2(60, 72), 8));
            return Over(With(Color.white, Cov(d)), With(Ink, Cov(d - 5)));
        }));
        public static Sprite Undo => Get("undo", () => Make(128, 128, (x, y) =>
        {
            var p = new Vector2(x, y); var c = new Vector2(66, 60);
            float ang = Mathf.Atan2(p.y - c.y, p.x - c.x) * Mathf.Rad2Deg;
            float arc = Mathf.Abs((p - c).magnitude - 30f) - 8f; if (ang > -40 && ang < 80) arc = 99f; // gap on the upper-right
            float head = SdPoly(p, new[] { new Vector2(86, 98), new Vector2(86, 62), new Vector2(54, 80) }); // arrow head on top-right
            float d = Mathf.Min(arc, head);
            return Over(With(Color.white, Cov(d)), With(Ink, Cov(d - 5)));
        }));
        public static Sprite Restart => Get("restart", () => Make(128, 128, (x, y) =>
        {
            var p = new Vector2(x, y); var c = new Vector2(64, 62);
            float ang = Mathf.Atan2(p.y - c.y, p.x - c.x) * Mathf.Rad2Deg;
            float arc = Mathf.Abs((p - c).magnitude - 34f) - 8f; if (ang > 20 && ang < 70) arc = 99f;
            float head = SdPoly(p, new[] { new Vector2(86, 104), new Vector2(104, 74), new Vector2(66, 78) });
            float d = Mathf.Min(arc, head);
            return Over(With(Color.white, Cov(d)), With(Ink, Cov(d - 5)));
        }));
        public static Sprite Shuffle => Get("shuffle", () => Make(128, 128, (x, y) =>
        {
            var p = new Vector2(x, y);
            float a = Mathf.Min(SdSeg(p, new Vector2(24, 86), new Vector2(60, 86), 7), SdSeg(p, new Vector2(60, 86), new Vector2(84, 44), 7));
            float b = Mathf.Min(SdSeg(p, new Vector2(24, 42), new Vector2(60, 42), 7), SdSeg(p, new Vector2(60, 42), new Vector2(84, 84), 7));
            float h1 = SdPoly(p, new[] { new Vector2(112, 44), new Vector2(84, 62), new Vector2(84, 26) });
            float h2 = SdPoly(p, new[] { new Vector2(112, 84), new Vector2(84, 102), new Vector2(84, 66) });
            float d = Mathf.Min(Mathf.Min(a, b), Mathf.Min(h1, h2));
            return Over(With(Color.white, Cov(d)), With(Ink, Cov(d - 5)));
        }));
        public static Sprite Trophy => Get("trophy", () => Make(128, 128, (x, y) =>
        {
            var p = new Vector2(x, y);
            float cup = SdBox(p, new Vector2(64, 78), new Vector2(30, 26), 16);
            float stem = SdBox(p, new Vector2(64, 40), new Vector2(7, 14), 2);
            float foot = SdBox(p, new Vector2(64, 22), new Vector2(26, 8), 4);
            float handles = Mathf.Min(Mathf.Abs(SdCircle(p, new Vector2(32, 84), 14)) - 4f, Mathf.Abs(SdCircle(p, new Vector2(96, 84), 14)) - 4f);
            float d = Mathf.Min(Mathf.Min(cup, stem), Mathf.Min(foot, handles));
            Color col = Over(With(GoldMain, Cov(d)), With(Ink, Cov(d - 5f)));
            return Over(With(Color.white, 0.6f * Cov(SdBox(p, new Vector2(50, 82), new Vector2(4, 16), 3))), col);   // highlight
        }));
        public static Sprite Gift => Get("gift", () => Make(128, 128, (x, y) =>
        {
            var p = new Vector2(x, y);
            float box = SdBox(p, new Vector2(64, 48), new Vector2(40, 30), 8);
            float lid = SdBox(p, new Vector2(64, 84), new Vector2(46, 12), 6);
            float rib = Mathf.Min(SdBox(p, new Vector2(64, 60), new Vector2(8, 48), 2), 99f);
            float bow = Mathf.Min(SdCircle(p, new Vector2(48, 104), 14), SdCircle(p, new Vector2(80, 104), 14));
            float d = Mathf.Min(Mathf.Min(box, lid), bow);
            Color col = With(Ink, Cov(d - 5));
            col = Over(With(Hex("FF7A8A"), Cov(d)), col);
            col = Over(With(Hex("FFE066"), Cov(Mathf.Max(SdBox(p, new Vector2(64, 60), new Vector2(8, 48), 2), d))), col);
            col = Over(With(Hex("FFE066"), Cov(Mathf.Max(bow, d))), col);
            return col;
        }));
        public static Sprite Medal => Get("medal", () => Make(128, 128, (x, y) =>
        {
            var p = new Vector2(x, y);
            float rib = SdPoly(p, new[] { new Vector2(34, 124), new Vector2(60, 124), new Vector2(66, 60), new Vector2(40, 60) });
            float rib2 = SdPoly(p, new[] { new Vector2(94, 124), new Vector2(68, 124), new Vector2(62, 60), new Vector2(88, 60) });
            float disc = SdCircle(p, new Vector2(64, 44), 34);
            Color col = With(Ink, Cov(Mathf.Min(Mathf.Min(rib, rib2), disc) - 5));
            col = Over(With(Hex("5AA9FF"), Cov(Mathf.Min(rib, rib2))), col);
            col = Over(With(Hex("FFC83D"), Cov(disc)), col);
            col = Over(With(Hex("FFF1A8"), Cov(SdPoly(p, Star(new Vector2(64, 44), 20, 9, 5)))), col);
            return col;
        }));
        public static Sprite Bag => Get("bag", () => Make(128, 128, (x, y) =>
        {
            var p = new Vector2(x, y);
            float body = SdBox(p, new Vector2(64, 50), new Vector2(38, 38), 14);
            float handle = Mathf.Abs(SdCircle(p, new Vector2(64, 90), 20)) - 5; if (p.y < 88) handle = 99f;
            float d = Mathf.Min(body, handle);
            Color col = With(Ink, Cov(d - 5));
            col = Over(With(Hex("FF9F43"), Cov(body)), col);
            col = Over(With(Hex("FFFFFF"), Cov(handle)), col);
            col = Over(With(Hex("FFE9B8"), Cov(SdCircle(p, new Vector2(64, 52), 14))), col);
            return col;
        }));

        /// <summary>Accessibility symbols for colour-blind mode (one per paint id).</summary>
        public static Sprite Symbol(int id) => Get("symbol_" + id, () =>
        {
            Func<Vector2, float> shape;
            var c = new Vector2(48, 48);
            switch (id % 11)
            {
                case 0: shape = p => SdCircle(p, c, 26); break;
                case 1: shape = p => SdPoly(p, Ngon(c + new Vector2(0, -3), 32, 3)); break;
                case 2: shape = p => SdBox(p, c, new Vector2(24, 24), 3); break;
                case 3: shape = p => SdPoly(p, Star(c, 34, 15, 5)); break;
                case 4: shape = p => SdPoly(p, Ngon(c, 34, 4, 0f)); break;
                case 5: shape = p => SdPoly(p, Ngon(c, 31, 6)); break;
                case 6: shape = p => Mathf.Min(SdBox(p, c, new Vector2(30, 10), 3), SdBox(p, c, new Vector2(10, 30), 3)); break;
                case 7: shape = p => Mathf.Abs(SdCircle(p, c, 22)) - 7; break;
                case 8: shape = p => SdPoly(p, Ngon(c, 32, 5)); break;
                case 9: shape = p => Mathf.Min(SdSeg(p, c + new Vector2(-22, -22), c + new Vector2(22, 22), 8), SdSeg(p, c + new Vector2(-22, 22), c + new Vector2(22, -22), 8)); break;
                default: shape = p => SdPoly(p, Star(c, 34, 22, 4)); break;
            }
            return Make(96, 96, (x, y) => { var p = new Vector2(x, y); float d = shape(p); return Over(With(Color.white, Cov(d)), With(Ink, Cov(d - 5))); });
        });

        // ---------------------------------------------------------------- the paint-mixing pot (acrylic mixing pot with flared rim)
        static float PotBodyHalf(float y) => Mathf.Lerp(84f, 100f, Mathf.Clamp01((y - 8f) / 330f));          // gently tapered, wider at top
        const float RimBottom = 336f, RimTop = 370f;

        static float SdPotBody(Vector2 p)
        {
            float half = PotBodyHalf(p.y);
            float top = 340f, bottom = 8f;
            float cy = (top + bottom) * 0.5f, hh = (top - bottom) * 0.5f;
            return SdBox(p, new Vector2(128, cy), new Vector2(half, hh), 52f);
        }
        static float SdPotRim(Vector2 p) => SdBox(p, new Vector2(128, (RimBottom + RimTop) * 0.5f), new Vector2(112, (RimTop - RimBottom) * 0.5f), 14f);

        /// <summary>Frontmost layer: glass tint, ink outline, rim and glossy highlights. Liquid is drawn behind this.</summary>
        public static Sprite PotGlass => Get("pot", () => Make(PotW, PotH, (x, y) =>
        {
            var p = new Vector2(x, y);
            float body = SdPotBody(p), rim = SdPotRim(p);
            float d = Mathf.Min(body, rim);
            // 9px ink band around the silhouette, a very light glass fill inside (so paint stays vivid)
            Color res = With(Ink, Cov(d) * (1f - Cov(d + 9f)));
            res = Over(With(GlassTint, Cov(d + 9f)), res);
            // cream cartoon rim drawn over the body
            res = Over(With(RimCream, Cov(rim + 9f)), res);
            res = Over(With(Ink, Cov(rim) * (1f - Cov(rim + 9f))), res);
            // gloss streaks
            float g1 = SdBox(p, new Vector2(60f, 190f), new Vector2(9f, 105f), 9f);
            float g2 = SdBox(p, new Vector2(60f, 70f), new Vector2(9f, 9f), 9f);
            float g3 = SdBox(p, new Vector2(196f, 230f), new Vector2(5f, 36f), 5f);
            return Over(With(Color.white, 0.55f * Cov(Mathf.Min(g1, Mathf.Min(g2, g3)))), res);
        }));

        /// <summary>Alpha mask matching the pot interior (used by UI Mask to clip the paint).</summary>
        public static Sprite PotMask => Get("pot_mask", () => Make(PotW, PotH, (x, y) =>
        {
            var p = new Vector2(x, y);
            return With(Color.white, Cov(SdPotBody(p) + 9f));
        }));

        /// <summary>Faint backing (shows the acrylic is clear, gives soft depth behind the paint).</summary>
        public static Sprite PotBack => Get("pot_back", () => Make(PotW, PotH, (x, y) =>
        {
            var p = new Vector2(x, y);
            return With(new Color(1f, 1f, 1f, 0.42f), Cov(SdPotBody(p) + 9f));
        }));

        public static Sprite PotShadow => Get("pot_shadow", () => Make(256, 64, (x, y) =>
        {
            var p = new Vector2(x, y); float d = (new Vector2((p.x - 128) / 112f, (p.y - 32) / 22f)).magnitude;
            return new Color(0.23f, 0.16f, 0.29f, Mathf.Clamp01(1f - d) * 0.28f);
        }));

        /// <summary>Burlap-style cover for covered containers: tan cloth, stitched stripes, rope band.</summary>
        public static Sprite PotCover => Get("pot_cover", () => Make(PotW, PotH, (x, y) =>
        {
            var p = new Vector2(x, y);
            float body = SdPotBody(p), rim = SdPotRim(p);
            float d = Mathf.Min(body, rim) + 4f;
            Color baseC = CoverTan;
            float weave = (Mathf.Sin(x * 0.55f) * Mathf.Sin(y * 0.55f)) * 0.04f;
            baseC = new Color(baseC.r + weave, baseC.g + weave, baseC.b + weave, 1);
            float stripe = Mathf.Sin((x + y) * 0.09f) > 0.82f ? 0.08f : 0f;
            baseC = new Color(baseC.r - stripe, baseC.g - stripe, baseC.b - stripe, 1);
            Color col = With(Ink, Cov(d));
            col = Over(With(baseC, Cov(d + 8f)), col);
            float rope = Mathf.Abs(p.y - 300f) - 9f;
            col = Over(With(RopeBrown, Cov(rope) * Cov(d + 8f)), col);
            float stitch = (Mathf.Repeat(x, 18f) < 9f) ? SdBox(p, new Vector2(p.x, 300f), new Vector2(99f, 2f), 0f) : 99f;
            col = Over(With(Ink, 0.5f * Cov(stitch)), col);
            return col;
        }));

        // ---------------------------------------------------------------- ui bits
        public static Sprite Sparkle => Get("sparkle", () => Make(64, 64, (x, y) =>
        {
            var p = new Vector2(x, y) - new Vector2(32, 32);
            float d = Mathf.Min(SdBox(p, Vector2.zero, new Vector2(4, 28), 3), SdBox(p, Vector2.zero, new Vector2(28, 4), 3));
            d = Mathf.Min(d, p.magnitude - 9f);
            return With(Color.white, Cov(d));
        }));
        public static Sprite Splat => Get("splat", () => Make(256, 256, (x, y) =>
        {
            var p = new Vector2(x, y) - new Vector2(128, 128); float a = Mathf.Atan2(p.y, p.x); float r = p.magnitude;
            float edge = 60 + 22 * Mathf.Sin(a * 5) + 14 * Mathf.Sin(a * 9 + 1);
            float d = r - edge;
            for (int i = 0; i < 6; i++)
            {
                float ang = i * 1.05f + 0.3f; var c = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (92 + (i % 3) * 8);
                d = Mathf.Min(d, (p - c).magnitude - (8 + (i % 2) * 6));
            }
            return With(Color.white, Cov(d));
        }));
    }
}
