# Paint Magic Sort — ChatGPT Art Prompt Pack (v2: bright glossy casual-puzzle style, UI included)

The game already runs with **procedural placeholder art**. Every image below is an optional override: put the PNG in
`Assets/Resources/Art/` with **exactly the file name shown** and the game uses it automatically (delete the file to go back to the placeholder). No code changes needed.

> **About the style reference.** You asked for the look of *Bus Fever Party* (GridPlus). Public descriptions only say "bright, colourful cartoon casual puzzle", so this pack describes that genre look in words: saturated candy colours, chunky rounded shapes, glossy "plastic toy" highlights, soft shadows, big friendly buttons. **Best results: attach 2–4 of your own screenshots of the game to the very first ChatGPT message** and say *"match the overall mood, colour saturation and level of polish of these screenshots, but make everything original — do not copy any characters, buses, logos or exact layouts."* That keeps the look close without copying their assets.

---

## 0. Workflow (read once)

1. **Open one new ChatGPT chat per asset family** (pots / UI / icons / marketing) so the style stays consistent inside a family. In the first message paste the **Style Bible** (section 1) and your reference screenshots.
2. **Always request a transparent background.** If ChatGPT can't, ask for a **flat pure white (#FFFFFF) background** and remove it afterwards (remove.bg, Photoshop, Krita). Export **PNG-32**.
3. **Small icons:** ask for *"a 4×4 grid, equal square cells, one icon per cell, white background, no labels"*, then slice.
4. After each image, check the **Quality Checklist** (section 9). Ask ChatGPT to "regenerate with the same style but fix: …" rather than starting over.
5. Unity import settings for every sprite: *Texture Type = Sprite (2D and UI)*, *Mesh Type = Full Rect*, *Compression = None / High Quality*, *Alpha Is Transparency = on*, *Generate Mip Maps = off* (UI).
6. Game screen is portrait 1080×1920 and also landscape 1920×1080 (tablet / Android TV). Keep important art centred with a generous empty margin.

---

## 1. STYLE BIBLE (paste at the start of EVERY chat)

> **Art direction for a mobile casual puzzle game called "Paint Magic Sort".**
> Bright, saturated, **glossy casual-game cartoon**, like a polished modern hyper-casual puzzle game: chunky, rounded, friendly shapes; a "soft plastic toy" feel with one clear **specular highlight** (a glossy white streak on the upper-left of every object) and a gentle **darker shade along the bottom edge**; subtle **soft contact shadow** under objects; clean, flat, saturated colour areas (no photographic texture, no noise, no gritty detail). A **thin dark-purple outline (#3A284B, about 4–5 px at 512 px size, uniform)** around main shapes. Warm, happy, kid-friendly mood. Light **cream / peach studio backdrop** with soft pastel paint splashes. **No text, no letters, no numbers, no watermark, no signature, no real brands, no characters.** Single object per image unless a grid is requested, centred, fully visible, generous empty margin.
>
> **Palette (stay inside it):** Cherry Red `#E84C5B`, Ocean Blue `#399CE0`, Lemon Yellow `#F5CE45`, Mint Green `#54C99B`, Grape Purple `#9867D5`, Coral Orange `#F58D67`, Bubblegum Pink `#E88DC3`, Ink Navy `#4D536B`, Turquoise `#35C6C5`, Warm Brown `#9A694D`, Lavender `#B5A1E8`; outline `#3A284B`; cream `#FFF4DC`; button greens/blues/oranges as listed per UI prompt.

**Reusable suffix** (append to each prompt): *"Transparent background (or flat white if impossible). PNG. No text. Same style as the previous image in this chat."*

---

## 2. THE PAINT POT SET (most important — gameplay art)

The vessel is a **clear acrylic paint-mixing pot** — wide, softly tapered, rounded base, flared cream rim with a small pouring lip. **Not** a test tube, **not** a potion bottle.

**Layout contract (so the paint lines up in-game):** canvas **256 × 384 px** (or any 2:3 multiple, e.g. 512×768 or 1024×1536). Pot body fills roughly x 12–244 and y 8–340 (measured from the bottom). The **cream rim band is the top ~9 %**. The paint area ("inside") runs from about **6 % to 88 % of the height**, split into **4 equal layers**. Keep it **perfectly symmetric**, the same silhouette in all five images, same canvas, same position.

| File | Prompt |
|---|---|
| `pot.png` (front glass layer) | *[Style Bible]* A **clear glossy acrylic paint-mixing pot**, front view, slightly wider at the top than the bottom, rounded base. **The body must be fully transparent / see-through** — only the thin outline, a thick **cream rim band with a small pouring lip**, and a few glossy white shine streaks are painted (two curved streaks on the left, one short on the right). Nothing inside. Portrait 2:3 canvas, transparent background. |
| `pot_mask.png` | *A pure solid **white silhouette of the INSIDE** of that exact same pot (body only: no rim, no outline), on transparent background, exactly the same canvas size and position as pot.png.* (Used to clip the paint.) |
| `pot_back.png` | *The same pot silhouette filled with a **very faint white glass tint (~40 % opacity)**, no outline, transparent background, same canvas.* |
| `pot_cover.png` | *[Style Bible]* A **tan burlap / canvas cloth** (#D9B382) tightly wrapped over the same pot shape, visible cartoon weave, a brown **rope tie with a small knot** around the upper third, thin dark-purple outline, **fully opaque**, same silhouette and canvas as pot.png. No text. |
| `pot_shadow.png` | *A soft, flat, **semi-transparent dark-purple oval ground shadow**, wide and short (256 × 64), transparent background, no outline.* |

Tip: generate `pot.png` first; then upload it and ask: *"Now make pot_mask / pot_back / pot_cover on the identical canvas and silhouette."*

---

## 3. UI FRAMES AND BUTTONS

The code builds every button, card, pill and popup from **two tintable white sprites**, recoloured at runtime (green, orange, blue, purple, red, grey…). So the best UI art is **white (or very light grey) shapes with built-in gloss**, which the game tints. Make them **9-slice friendly**: the corners must stay clean when stretched.

| File | Size | Prompt |
|---|---|---|
| `round.png` | 96×96 (or 192×192) | *[Style Bible]* A **glossy rounded-rectangle button base, pure white**, corner radius about 38 % of its height, a **soft light-grey bottom shade** (gives the "plastic" depth) and a **subtle white gloss band across the upper third**. **No outline, no colour, no text, no drop shadow.** Perfectly symmetric so it can be stretched as a 9-slice. Transparent background. |
| `roundsoft.png` | 96×96 | *A white rounded rectangle with **very soft blurred edges** (a gentle glow), no outline, symmetric, transparent background.* (Used as the soft drop shadow under buttons and cards.) |

In Unity open **Sprite Editor → set Border L/B/R/T = 40** (for 96 px; 80 for 192 px) on both files, then Apply.

**Concept sheets (for your reference / future skinning, not loaded yet)** — useful to keep the whole UI on-brand:

- *[Style Bible]* **"Mobile puzzle game UI kit" sheet** on a cream background: a big green PLAY button, a smaller orange and blue and purple and red button, a white rounded card with ribbon header, a white pill (for lives and gold), a gold coin, a heart, a yellow star — all glossy, all the same style. **Use blank buttons, no text.**
- *A **popup panel***: ivory rounded card with thin dark-purple outline, a **coloured ribbon header** (blank) overlapping the top edge, a small **red round close button with a white X** at the top-right corner, soft shadow.
- *A **toggle switch** (on = green, off = grey)* and *a **progress bar** (rounded, cream trough, orange fill, gloss highlight)* — blank.

If you generate these and like them, tell me and I will wire dedicated skin sprites (`panel`, `ribbon`, `pill`, `btn_green`…) into the game.

---

## 4. BACKDROP, LOGO, MENU

| File | Size | Prompt |
|---|---|---|
| `backdrop.png` | **1080×1920** portrait (also add `backdrop_wide.png` 1920×1080 if you want a landscape version later) | *[Style Bible]* A calm **artist-studio background**: soft **cream-to-peach vertical gradient wall**, **faint pastel paint splashes** (red, blue, yellow, mint) mostly near the corners, a **wooden shelf edge at the very bottom** with a few **blurred paint jars**. **Low contrast** so the pots stay readable. No characters, no text, no foreground clutter in the centre. |
| `logo.png` | **1000×420**, transparent | *[Style Bible]* The words **"PAINT MAGIC SORT"** in **chunky bubble letters on two lines**: **"PAINT"** large on the first line and **"MAGIC SORT"** smaller on the second line, each letter a **different palette colour**, thick dark-purple outline, glossy white highlights, a few small **magic sparkles** around the letters, a tiny **paintbrush** and a **paint drip**. Transparent background. *(This is the one image where letters are required; check every letter is spelled correctly.)* |

---

## 5. ICONS (128×128 each, transparent; use grid sheets and slice)

*[Style Bible] "single chunky glossy cartoon [X] icon, game UI style, centred".*

| File | [X] |
|---|---|
| `coin.png` | a shiny **gold coin**, raised inner ring, small sparkle |
| `heart.png` | a plump glossy **red-pink heart** (lives) |
| `star.png` | a fat five-point **yellow star** with soft highlight |
| `check.png` | a bold **white check-mark** with dark outline (the game tints it green) |
| `plus.png` | a bold **white plus sign** with dark outline |
| `lock.png` | a cute **golden padlock** with silver shackle |
| `gear.png` | a chunky **light-grey settings cog** |
| `back.png` | a bold **white left-arrow chevron** with dark outline |
| `undo.png` | a bold **white curved undo arrow** (counter-clockwise) |
| `shuffle.png` | a bold **white shuffle icon** (two crossing arrows) |
| `restart.png` | a bold **white circular restart arrow** |
| `trophy.png` | a **golden trophy cup** with two handles |
| `gift.png` | a **pink gift box with yellow ribbon and bow** (daily reward) |
| `medal.png` | a **gold medal with blue ribbon** and a star in the middle |
| `bag.png` | an **orange shopping bag** with a cream circle badge (shop) |

Grid prompt example: *"A 4×4 grid of equal square cells on a pure white background, one icon per cell, no text: coin, heart, star, check, plus, padlock, gear, back arrow, undo arrow, shuffle arrows, restart arrow, trophy, gift, medal, shopping bag, and a blank cell. Same glossy cartoon style in every cell."*

---

## 6. COLOUR-BLIND SYMBOLS (96×96, `symbol_0.png` … `symbol_10.png`)

Eleven **simple, instantly distinguishable geometric shapes**, **white fill + thick dark-purple outline**, transparent background, perfectly centred:
`symbol_0` circle · `symbol_1` triangle · `symbol_2` square · `symbol_3` five-point star · `symbol_4` diamond · `symbol_5` hexagon · `symbol_6` plus · `symbol_7` ring · `symbol_8` pentagon · `symbol_9` cross (×) · `symbol_10` four-point star.

---

## 7. EFFECTS (particles — white, tinted in-game)

| File | Size | Prompt |
|---|---|---|
| `sparkle.png` | 64×64 | *A plain **white four-point sparkle**, no outline, soft glow, transparent.* |
| `splat.png` | 256×256 | *A plain **white paint splat blob with a few droplets**, no outline, organic edge, transparent.* (Used for confetti and decor; tinted in-game.) |
| `circle.png` | 128×128 | *A plain **white soft circle (paint dot)** with a tiny glossy highlight spot, no outline.* |

---

## 8. FONT, SOUND, MARKETING

**Font** — a chunky rounded display font suits the look. Free (OFL): **Lilita One**, **Fredoka**, **Baloo 2**, **Luckiest Guy**. Put the `.ttf` at `Assets/Resources/Art/GameFont.ttf` and the whole UI switches.

**Store and platform art** (not loaded by the game; used in Unity Player Settings / store listings):

| Asset | Size | Prompt |
|---|---|---|
| App icon | 1024×1024 | *[Style Bible]* The glossy clear acrylic pot, **half full of three paint layers (red, blue, yellow)**, a **ribbon of paint pouring in from a tilted second pot**, cream-peach background with soft splashes, **no text**, subject fills ~70 % of the square (leave margin for rounded corners). |
| Android adaptive icon **foreground** | 432×432 (subject inside the centre 288×288 safe zone), transparent | *Just the pot and the pouring paint, centred, transparent background.* |
| Android adaptive icon **background** | 432×432 | *A plain cream-to-peach gradient with a few faint paint splashes.* |
| **Android TV banner** | **320×180** (make 1280×720 and downscale) | *[Style Bible]* A wide banner: the pot with pouring paint on the left, the **"PAINT MAGIC SORT" logo** on the right, cream background. Text allowed. |
| Play Store **feature graphic** | 1024×500 | *Wide banner: three glossy paint pots with colourful layers and a tilted pot pouring, the "PAINT MAGIC SORT" logo, bright cream-peach background.* |
| Splash image | 1080×1920 | *The logo centred on the studio backdrop.* |
| Store screenshots | 1080×1920 | Use real in-game screenshots (take them on a device, or with Unity's Game view "Capture" / Recorder at 1080×1920), optionally framed with a coloured caption bar. |

**Sound** (drop `.wav` into `Assets/Resources/Audio/`, names exact): `tap`, `select`, `pour`, `invalid`, `complete`, `win`, `coin`, `reveal`, and optional `music` (replaces the built-in synthesized loop). Prompts for an audio generator:
- `tap` — tiny soft wooden "pop" UI click, 80 ms. · `select` — light glassy "tink", rising slightly, 150 ms.
- `pour` — thick paint pouring and glugging, 0.6 s, smooth, satisfying, no splashing.
- `invalid` — gentle low "bonk", 200 ms, not harsh. · `complete` — sparkly 3-note ascending chime, 400 ms.
- `win` — cheerful 4-note fanfare with a tiny drum roll, 1.2 s. · `coin` — bright coin "ka-ching" blip, 300 ms.
- `reveal` — magical "whoosh-pop", 300 ms. · `music` — relaxed bouncy ukulele + marimba, 92 BPM, 20–60 s seamless loop.

---

## 9. QUALITY CHECKLIST (before dropping a file in)

- [ ] Transparent background with **clean edges** (no white halo around the shape).
- [ ] Pot set: all five images share the **same canvas and silhouette**; `pot.png` body is really see-through.
- [ ] Icons and symbols are **readable at 64 px**; nothing important within 8 % of the edge.
- [ ] Style matches the rest of the set (same highlight direction, outline weight, saturation).
- [ ] No text / no letters except the logo and the TV banner. No real brands or characters.
- [ ] File names are **exact** (lowercase, as in the tables) and in `Assets/Resources/Art/`; import settings from section 0.
- [ ] Press Play and look at the menu, a level with hidden layers, a level with a cloth cover, and a popup. Send me screenshots of anything that looks off and I will adjust the layout or tint.
