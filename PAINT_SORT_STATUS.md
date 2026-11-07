# Paint Sort — Implementation Status

## How to run
1. Open the project in Unity 6000.6, open `Assets/Scenes/SampleScene`, press **Play**. The whole game builds itself from code (`App.Boot`).
2. Dev tools (Editor / development builds only): **DBG** button bottom-left, or F1 / `` ` `` / three-finger tap. The bottom-right readout shows fps, worst frame, batches, GC bytes per frame, resolution and layout class. Menu **Paint Sort ▸** has level generation, level audit and builds.
3. Art: see `ART_PROMPTS.md`. Drop PNGs into `Assets/Resources/Art/` to override the procedural placeholders. Optional: `Resources/Audio/music.wav` replaces the synthesized music.
4. Command line (close the Editor first): `tools\build.ps1 -Target Android -Development`, `tools\test.ps1`. Set `UNITY_PATH` if Unity is not in the default Hub folder.

## Screens and devices
Layout is responsive (`App.Layout.cs`): the canvas scales so the logical size is always at least 1080x1920 (portrait) or 1920x1080 (landscape), content sits inside a safe-area root, and `Relayout()` repositions everything when the shape changes (live, e.g. rotation).
- Phone portrait (any aspect), tablet portrait, landscape (tablet / desktop / TV). Landscape puts power-ups in a 2x2 column beside the board and the menu in two columns.
- **Android TV**: detected at runtime; landscape forced, no banner, focus ring on, D-pad / remote / gamepad navigation; Escape / Android Back / gamepad B goes back. Player Settings set: auto-rotation, Android TV compatibility, `androidIsGame`. **You still need** a 320x180 TV banner image and a test on a real TV or emulator.
- Phones are locked to portrait; tablets auto-rotate; TVs are landscape.

## Levels
**5,000** deterministic levels, decoded lazily. 500 Hard, 500 Super Hard.
- Levels 1-4 are hand-designed tutorials (one mechanic each); level 5 onward are generated.
- Difficulty is shaped by a measurable "careful player" bot (`DifficultyProbe`): for every generated level the generator makes several solvable candidates and picks the friendliest for Normal, a harder one for Hard and the hardest for Super Hard. Severity ramps from gentle (level ~10) to a wall (level 200+). Measured bot progress (share of colours sorted before getting stuck): levels 51-150 Normal 75% / Hard 18% / Super Hard 3%; levels 151-400 Normal 41% / Hard 4% / Super Hard 3%.
- Past level 750 there are seven "flavours" (mystery-heavy, cloth-heavy, light, plus an easier breather every 10th Normal level) so late levels differ in more than size.
- Audit (witness replay + mechanic checks + independent solver): 5000 audited, 0 failed, 1 solver-inconclusive (the second solver hit its search budget; its saved solution replays correctly). Regenerate with the menu item or `harness gen`.

## Features added in this pass
Resume after restart (board saved after every move, also mid-pour) · level select (paged grid, replay any unlocked level) · screen fades · win-wave hop on the board · procedural background music loop (toggle in Settings) · graded Android haptics · localization (English + Spanish; Settings button) · popups pop in.

## Tests (all run inside Unity)
- 32 EditMode tests (rules, economy, resume serialization, tutorials, difficulty ordering, generator determinism).
- 10 PlayMode tests that drive the real game end to end: boot, complete level 1 (reward, unlock), no double reward on replay, resume after leave, leave during a pour, restart, duplicate life consumption, undo, level select, save/reload. They use an isolated temp save and never touch the real one. The PlayMode suite found and fixed a real bug (pour animation kept running on destroyed pots when the board was torn down mid-pour).
- A real Windows release build succeeds (0 errors, ~36 s, 102 MB) and runs 15 s without exceptions; the release assembly contains no debug menu or fake-purchase code.

## Performance work
- Idle UI no longer touches any transform; measured idle rebuilds: 1 tiny graphic every 0.5 s. Separate canvases, fewer raycasters, pooled particles, no per-frame allocations in the pour animation, write-behind saving, capped ledger, per-pixel colour parsing removed from art generation.
- In the Editor the readout still shows ~200 KB/frame of managed allocation at idle; game scripts, raycasts and UI rebuilds each measured ~0, so it appears to come from the Editor (Device Simulator / MCP bridge). Check a development build on a device for the real figure.

## NOT verified / not possible from here
- Real devices: touch, real notches, TV remote, gamepad, audio quality, haptics, startup time, battery.
- Keyboard / gamepad navigation: reviewed and the focus ring confirmed, but D-pad movement was not exercised with real key events.
- Android and Android TV builds (only a Windows build was run). Spanish: translations were written by the assistant, not reviewed by a native speaker; only the main screens, dialogs and tutorial hints are translated, other texts fall back to English; the language fully applies after a restart.
- Image / audio generation: the Editor's providers need your own fal.ai or OpenRouter API key. Use `ART_PROMPTS.md`.

## Still open (needs accounts, SDKs or decisions)
- Real rewarded ads / banner (ad SDK + account), consent flow, real store billing, analytics provider, cloud save. All are behind interfaces (`IAdService`, `IPurchaseService`, `IAnalytics`) and currently mock / log-only.
- Real art and sound (see above). TV launcher banner.
- Tutorial hints still appear as text boxes (the boards themselves are hand-designed).
- Text uses uGUI `Text`.
