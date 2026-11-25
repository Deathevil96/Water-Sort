# CLAUDE GAMEDEV PLAYBOOK (from Palette Pour)

Instructions for Claude Code working on a new Unity mobile game for this user (Misal Interactive). Follow them unless the new game's spec says otherwise. Reference project: `J:\Unity Projects HDD\Water Sort` (Palette Pour). Read files from there when a step says "copy".

## 1. Start of project
1. Read the game's spec `<GAME>_GDD_CLAUDE_IMPLEMENTATION_SPEC.md` completely; it is the source of truth. If it does not exist, help the user write it first, using `Water Sort/PAINT_SORT_GDD_CLAUDE_IMPLEMENTATION_SPEC.md` as the template (same section structure: priorities, rules, progression, economy, ads, shop, UI §16, animation §17, architecture §18, save, debug menu, validator, accessibility/performance §24, phases, test checklist, acceptance criteria, non-goals).
2. Inspect the repo, Unity version, packages, git status and the MCP tools that actually exist. Do not guess tool arguments. Do not invent SDKs, API keys, product IDs or backends.
3. Check the game name against store and trademark search before making a logo or store art. Never reuse a trademarked name of the genre leader. Keep the name and package in one place (`Editor/ProjectSetup.cs`).
4. Implement phase by phase. Verify each phase with tests before moving on. Do not stop at a plan. Never report something as done unless it is built and tested; keep `<GAME>_STATUS.md` updated with what is verified, what is not, and what is open.

## 2. Architecture (required)
- `Assets/Scripts/Core/` — pure C# asmdef: rules, state, solver/generator, economy. No UnityEngine UI. EditMode-testable.
- `Assets/Scripts/Game/` — one `App` MonoBehaviour split into partial classes by concern (`App.cs`, `App.Layout.cs`, `App.Input.cs`, `AppUI.cs`, `App.Dev.cs`, …). `App.Boot` builds everything; the scene only needs the boot object.
- `Assets/Scripts/Editor/` — `ProjectSetup` (player settings, privacy), `ArtImporter`, content tools, `BuildTools` (signing, versioning).
- Ads, purchases, analytics, leaderboards behind interfaces (`IAdService`, `IPurchaseService`, `IAnalytics`) with mock / log-only implementations until the user has accounts.
- All economy and difficulty numbers configurable in one place.
- Content (levels) generated offline into deterministic data under `Resources/`, decoded lazily. Every level verified solvable (witness replay + independent solver). Tutorial levels hand-made and proven dead-end-free by exhaustive search, guarded by a test.
- Save after every move (resume mid-level), write-behind. Tests use an isolated temp save.

## 3. UI system (copy and adapt from Palette Pour)
UI is built entirely in C# (uGUI), no hand-placed scene UI, no prefabs for screens.

Copy these files, change the namespace, remove game-specific parts:
| File | Keep |
|---|---|
| `Scripts/Game/UiKit.cs` | `UI.Rect/Place/Stretch/Img/Txt/Card/Btn/GlossBtn/Banner/Dim/SubCanvas/SetEnabled/SetLocked`, `Outline8`, `PressFx`, `PulseRing`, `InputMode`, `FxHost`/`Fx` |
| `Scripts/Game/Art.cs` | `Get(name, make)` override loader, `Make()`, SDF helpers, `Hex()` cache, `GameFont`, `Round`, `RoundSoft`, `Circle`, `White`, generic icons. Delete pot/paint sprites and `PotGeometry`. |
| `Scripts/Editor/ArtImporter.cs` | whole file; update folder constants and the 9-slice name list |
| `Scripts/Game/App.Layout.cs` | device detection (Phone / Tablet ≥7" / Android TV), orientation policy, canvas scaling (min 1080×1920 portrait / 1920×1080 landscape), safe-area root, `PollScreen` → `Relayout` only on change |
| `Scripts/Game/AppUI.cs` | popup system: `Panel`, `PopupCard`, `FitPopup`, `OpenDialog`, `Toast`, popup stack with Back/Escape/gamepad-B handling |
| `Scripts/Game/Loc.cs` + `Resources/Lang/` | `Loc.T()` localisation |
| `ArtSource/process_art.py` | helpers `key_light_background`, `trim`, `square`, `slice_grid`, `flood_outside`; rewrite the per-asset sections |

Before copying, move every colour constant from `UI` and `Art` into one `Theme` class so the new game edits only that file. `UiKit` must not reference `App`.

UI rules:
- **Placeholder first.** Every sprite comes from `Art.Get("<name>", proceduralFallback)`: it loads `Resources/Art/<name>.png` if present, else draws a placeholder. The game must be fully playable before any real art exists. Dropping in a PNG with the exact name must need no code change.
- **Two tintable shapes skin all UI:** `round.png` (white glossy rounded rect, 9-slice) and `roundsoft.png` (soft blurred rect, drop shadow). Buttons, cards, pills, popups are stacked tinted layers of these. Glossy button layers: focus ring → soft shadow → gold edge (tap target) → dark rim (face × 0.45) → face → light top half → glint; edge thickness scales with size.
- One button style everywhere. Disabled = grey face + dull edge. Locked = grey + padlock badge, still tappable so it explains when it unlocks.
- Text: uGUI `Text` + `Outline8`, font at `Resources/Art/GameFont.ttf` (chunky OFL font, e.g. Lilita One / Fredoka / Baloo 2). Do not switch to TextMeshPro unless the user asks.
- Keep a `GraphicRaycaster` on the **root** canvas (nested canvases sort taps wrongly without it). Use sub-canvases per update rate (HUD / board / popups). Decorative images `raycastTarget = false`. Hints and overlays must never block taps.
- Idle UI writes no transforms (`PressFx` only animates while moving). Pool particles (`FxHost`). No per-frame allocations.
- Accessibility: colour is never the only cue (symbols/shapes), sound/music/haptics toggles, reduced motion where possible, focus ring + D-pad for keyboard/TV.
- Phones portrait-locked, tablets auto-rotate, TV landscape with focus visible. Landscape gets its own arrangement (side column, two-column menu). Backdrop scales to cover; separate wide backdrop.

## 4. Art pipeline (ChatGPT images, made by the user)
Claude cannot generate final art (no image provider keys). Write prompts; the user runs them in ChatGPT.
1. Write `ART_PROMPTS.md` (template: `Water Sort/ART_PROMPTS.md`) or an interactive prompt artifact with copy buttons. Contents:
   - **Style Bible** pasted at the start of every chat: look in words, outline colour and thickness, one upper-left highlight, darker bottom edge, soft contact shadow, palette hexes, "no text, no letters, no brands, no characters, single centred object, generous margin".
   - One ChatGPT chat per asset family (gameplay objects / UI / icons / marketing).
   - **Layout contract** for gameplay art: exact canvas size, silhouette position, where content goes; all images of a set on the identical canvas.
   - UI art as **white** shapes with built-in gloss (tinted in game).
   - Small icons as a 4×4 grid of equal cells on white, then slice.
   - Table of exact file names + sizes + prompt; transparent or flat white background; quality checklist.
   - Style reference: describe the genre look in words and tell the user to attach their own screenshots with "match mood and polish, make everything original".
2. User drops raw images into `ArtSource/ChatGPT/` (git-ignored). Adapt and run `process_art.py` (Pillow): background removal, trim, shared-frame crops, mask from outline, measured geometry to JSON, grid slicing, store art to `Assets/Art/Store/`.
3. `ArtImporter` applies: Sprite, no mips, bilinear, clamp, alpha is transparency, full-rect mesh, CompressedHQ (store art uncompressed), 9-slice border = half the min side and PPU so corners are ~45 canvas px.
4. Put measured art geometry in code (like `Art.ArtworkPot`); choose it by whether the real art file exists.
5. Polish loop: screenshot at phone portrait (1080×2400), tablet, landscape → list issues → fix → re-shoot. For MCP screenshots switch the overlay canvas to ScreenSpaceCamera temporarily and revert; change Game view size by reflection.
6. Store art: app icon 1024², adaptive icon foreground/background 432² (subject within centre 288²), feature graphic 1024×500, TV banner 320×180, real gameplay screenshots. Keep in `Assets/Art/Store/` (outside Resources).
7. Audio: same override idea, `Resources/Audio/<name>.wav`; procedural placeholder music/sfx until then.

## 5. Unity + MCP working rules
- After editing scripts, read the console for compile errors before Play mode or tests.
- Never call Unity APIs in MonoBehaviour field initializers.
- Static field order matters (caches used by static initializers must be declared first).
- Release builds only via the project's own menu item (signing + version bump), not the generic build tool. The tool call may time out while the build still finishes.
- `com.coplaydev.unity-mcp` is a dev tool; remove it before a store release.

## 6. Tests (required)
- EditMode: rules, economy, serialization, tutorial dead-end freedom, difficulty ordering, generator determinism.
- PlayMode, driving the real game: boot, finish level, reward granted once, resume after restart, leave mid-animation, undo, locks/unlocks, popups.
- **Pointer-reachability audit:** raycast through the real EventSystem at the centre of every button and gameplay target on every screen and popup; check popups swallow taps meant for what is behind them.
- Headless runner script `tools/test.ps1`; build script `tools/build.ps1`.
- Say clearly what was not verified (real devices, touch, notches, haptics, TV remote).

## 7. Dev tools
- Debug menu compiled only under `#if UNITY_EDITOR || DEBUG`; never in release. Level jump, currency/lives, rewards, fake ads/shop, animation speed.
- On-screen readout: fps, worst frame, batches, GC/frame, resolution, layout class.
- Editor menu: generate content, audit content, apply player settings, builds, set version.

## 8. Feel
Squishy press, eased animations, tap-ahead input queue during animations, win celebration, pooled particles, screen fades, popups pop in, graded haptics. Introduce each power-up on its unlock level with one free use; locked buttons explain unlock level; rescue prompts offer only unlocked tools. Loading screen for heavy work on slow phones. Studio splash; Unity splash off.

## 9. Privacy and release
- First closed-test build offline: Unity analytics/diagnostics off; release build strips INTERNET (`AndroidPrivacyPostProcess`, `ShipWithInternet = false`); Play Console declarations match (no ads, no data); privacy policy page in `docs/`.
- Verify: `aapt2 dump permissions <apk>` has no INTERNET; on device `/proc/net/tcp` shows no ESTABLISHED lines for the app uid.
- Signing keystore in git-ignored `Signing/`; remind the user to back it up.
- `version.json`: each release build bumps version code and auto-increments patch; production = 1.0.0. Names `<Game>_<code>_release.aab`, `<Game>_<code>_debug.apk`.
- LICENSE all rights reserved; README.
- Adding ads/analytics/billing later, in order: enable INTERNET + reserve banner space → rewrite privacy policy → update Play Console (Ads, Data safety, Advertising ID) → real billing (hide shop until real) → consent flow → socket check → new build code → app-ads.txt.

## 10. Communication
- Use plain, short English with the user. They make the art and own the accounts; ask only for things you cannot do.
- Commit only when asked; end commit messages with the attribution line the harness gives.
