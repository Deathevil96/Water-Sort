# Paint Sort — Cartoon Art Prompt Pack (for ChatGPT image generation)

The game already runs with **procedural cartoon placeholder art** (generated in code). To upgrade it, generate the PNGs below and drop them into
`Assets/Resources/Art/` using **exactly these file names**. Any file present overrides the placeholder automatically — no code changes. Delete a file to go back to the placeholder.

## 0. Global style guide (paste this at the start of EVERY prompt)

> **Style:** cute, bright, glossy **2D cartoon mobile-game art**, like a polished casual puzzle game. **Thick dark-purple outline (#3A284B), uniform ~6px line**, flat saturated colours with one soft lighter highlight and a subtle darker shade at the bottom. Rounded, chunky, friendly shapes. No gradients that look photographic, no realistic textures, no text, no watermark, no signature. Warm artist-studio mood (ivory/cream, soft pastels). Single object, centred, fully visible, generous empty margin. **Transparent background** (if you can't do transparency, use a flat pure white #FFFFFF background and I will cut it out).

Palette to respect: Cherry Red `#E84C5B`, Ocean Blue `#399CE0`, Lemon Yellow `#F5CE45`, Mint Green `#54C99B`, Grape Purple `#9867D5`, Coral Orange `#F58D67`, Bubblegum Pink `#E88DC3`, Ink Navy `#4D536B`, Turquoise `#35C6C5`, Warm Brown `#9A694D`, Lavender `#B5A1E8`, outline `#3A284B`, cream `#FFF4DC`.

**Tips**
- Ask ChatGPT for **one asset per image** for the pot set. For small icons, ask for a **4×4 grid on a white background, equal cells**, then slice the grid.
- Remove white backgrounds with any bg-remover (remove.bg, Photoshop "Remove Background", Krita). Export **PNG-32**.
- Unity import settings for every file: *Texture Type = Sprite (2D and UI)*, *Mesh Type = Full Rect*, *Compression = None/High Quality*, *Alpha Is Transparency = on*.

---

## 1. The paint-mixing pot (most important asset)

The vessel is a **clear acrylic paint-mixing pot** — NOT a test tube, NOT a potion bottle. Wide, softly tapered, rounded base, flared cream rim with a small pouring lip.

**Layout contract (so the paint lines up):** canvas **256 × 384 px** (or any 2:3 multiple, e.g. 512×768). Pot body fills roughly x 12–244, y 8–340 from the bottom; the **cream rim band sits in the top 9%**; paint area (the "inside") runs from about **6% to 88% of the height** and is split into **4 equal layers**. Keep the pot symmetric.

### `pot.png` — glass front layer (transparent body!)
> *[Global style]* A **clear acrylic artist paint-mixing pot**, front view, slightly wider at the top than the bottom, rounded base, thick dark-purple outline, **fully transparent see-through body** (only the outline, the rim, and a few glossy white highlight streaks are painted — the middle must be empty/transparent so paint colour behind it shows through). A chunky **cream-coloured rim band** around the top with a small pouring spout notch on both sides. Two curved white shine streaks on the left side and a small one on the right. Portrait 2:3, transparent background.

### `pot_mask.png` — interior silhouette (used to clip the paint)
> A pure **solid white silhouette** of the inside of that same pot shape (body only, no rim, no outline), on a transparent background, exactly the same canvas size and position as `pot.png`.

### `pot_back.png` — faint glass back
> The same pot silhouette filled with **very faint white (about 40% opacity) glass tint**, no outline, transparent background, same canvas.

### `pot_cover.png` — cloth cover for "covered" pots
> *[Global style]* A **burlap / canvas artist cloth** tightly wrapped over the same pot shape, tan colour `#D9B382`, visible weave texture drawn in cartoon style, a brown **rope tie** around the upper third with a little knot, thick dark-purple outline, same silhouette and canvas as `pot.png`, fully opaque. No text.

### `pot_shadow.png`
> A soft, flat, **semi-transparent dark-purple oval ground shadow**, wide and short (256 × 64), transparent background, no outline.

---

## 2. Icons (128 × 128 each, transparent)

Use the style guide plus: "single chunky cartoon **[X]** icon, game UI style".

| File | Prompt noun |
|---|---|
| `coin.png` | a shiny **gold coin**, inner ring, a small sparkle |
| `heart.png` | a plump glossy **red-pink heart** (lives) |
| `star.png` | a fat five-point **yellow star** with soft highlight |
| `check.png` | a bold **white check-mark** with dark outline (will be tinted green in-game) |
| `plus.png` | a bold **white plus sign** with dark outline |
| `lock.png` | a cute **golden padlock** with silver shackle |
| `gear.png` | a chunky **light-grey cog / settings gear** |
| `back.png` | a bold **white left arrow chevron** with dark outline |
| `undo.png` | a bold **white curved undo arrow** (counter-clockwise) |
| `shuffle.png` | a bold **white shuffle icon** (two crossing arrows) |
| `restart.png` | a bold **white circular restart arrow** |
| `trophy.png` | a **golden trophy cup** with two handles |
| `gift.png` | a **pink gift box with yellow ribbon and bow** (daily reward) |
| `medal.png` | a **gold medal with blue ribbon** and a star in the middle |
| `bag.png` | an **orange shopping bag** with a cream circle badge (shop) |
| `sparkle.png` | a plain **white four-point sparkle**, no outline (particle) |
| `splat.png` | a plain **white paint splat blob** with droplets, no outline (tinted in-game) |

## 3. Colour-blind symbols (96 × 96 each, white fill + dark outline)

`symbol_0.png` … `symbol_10.png` — eleven **simple, instantly distinguishable geometric shapes**: circle, triangle, square, star, diamond, hexagon, plus, ring, pentagon, cross (×), four-point star. White fill, thick dark-purple outline, transparent background. (They sit on top of each paint layer when the colour-blind option is on.)

## 4. UI frames (optional — placeholders are already good)

- `round.png` — 96×96 **white** rounded rectangle, corner radius ~36px, no outline. **In Unity set Sprite Editor → Border L/B/R/T = 40** (9-slice). It is tinted by code.
- `backdrop.png` — 1080×1920 portrait **ivory-to-peach artist-studio backdrop**: soft cream wall, faint pastel paint splashes at the corners, a wooden shelf edge at the very bottom with a few blurred paint jars. Calm, low contrast so pots stay readable. No characters, no text.
- `logo.png` — 1000×420 **"PAINT SORT" logo**: chunky bubble letters, each letter a different paint colour from the palette, thick dark-purple outline, glossy highlights, a small paintbrush and a paint drip. Transparent background. (The title letters in the game text are placeholders until this exists.)

## 5. Font

A chunky rounded display font suits the look. Free (OFL) options: **Lilita One**, **Fredoka**, **Baloo 2**, **Luckiest Guy**. Put the `.ttf` at `Assets/Resources/Art/GameFont.ttf` and the whole UI switches to it.

## 6. Sound (optional) — drop `.wav` into `Assets/Resources/Audio/`

Names: `tap`, `select`, `pour`, `invalid`, `complete`, `win`, `coin`, `reveal`. Prompts for an audio generator:
- `tap` — tiny soft wooden "pop" UI click, 80ms.
- `select` — light glassy "tink" rising slightly, 150ms.
- `pour` — thick paint pouring and glugging, 0.6s, smooth and satisfying, no splashing.
- `invalid` — gentle low "bonk" rejection, 200ms, not harsh.
- `complete` — sparkly 3-note ascending chime, 400ms.
- `win` — cheerful 4-note fanfare with a tiny drum roll, 1.2s.
- `coin` — bright coin "ka-ching" blip, 300ms.
- `reveal` — magical "whoosh-pop" reveal, 300ms.
- Background music (not wired yet): relaxed, bouncy ukulele + marimba loop, 90 BPM, 60-90s seamless.

## 7. App icon / store art

- App icon 1024×1024: the glossy clear acrylic pot, half-full of three paint layers (red, blue, yellow), a drip of paint pouring in from a tilted second pot, cream background, same cartoon style, no text.
