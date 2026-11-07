# Paint Magic Sort — Implementation Status

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
- Levels 1-4 are hand-designed tutorials (one mechanic each) and are **proven impossible to get stuck on**: an exhaustive search (`DeadEndAnalysis`) checks that every reachable board can still be won, so no power-up is ever needed. (The first version of level 2 failed this: 26 of its 42 reachable boards were dead ends. Fixed by giving it two spare pots; a unit test now guards all four.) Level 5 onward are generated.
- **No difficulty spikes early:** Hard / Super Hard levels keep their labels but play like Normal ones until level 25, then ramp up; they only lose a spare pot from level 60. Measured casual-bot win rate for levels 1-40 is 80-100% except three levels in the 70s/50s; a unit test requires >= 60% for levels 1-30.
- Difficulty is shaped by a measurable "careful player" bot (`DifficultyProbe`): for every generated level the generator makes several solvable candidates and picks the friendliest for Normal, a harder one for Hard and the hardest for Super Hard. Severity ramps from gentle (level ~10) to a wall (level 200+). Measured bot progress (share of colours sorted before getting stuck): levels 51-150 Normal 75% / Hard 18% / Super Hard 3%; levels 151-400 Normal 41% / Hard 4% / Super Hard 3%.
- Past level 750 there are seven "flavours" (mystery-heavy, cloth-heavy, light, plus an easier breather every 10th Normal level) so late levels differ in more than size.
- Audit (witness replay + mechanic checks + independent solver): 5000 audited, 0 failed, 1 solver-inconclusive (the second solver hit its search budget; its saved solution replays correctly). Regenerate with the menu item or `harness gen`.

## Power-up progression
Undo unlocks at level 5, Shuffle at 10, + Pot at 15 (`Progression.*UnlockLevel`). Before that the button is greyed with a padlock (paid ones show "Lv 10" / "Lv 15" instead of a price) and tapping it says when it unlocks. On its level the first time, a popup introduces the power-up; Shuffle and + Pot also grant exactly one FREE token (once). The new button pulses until you use it. The stuck/rescue prompt only offers power-ups you have already unlocked. Replaying an earlier level after unlocking keeps them unlocked.

## Touch / blocker audit
- Root cause of the reported "touch issues on multiple buttons" (found with a real event-system raycast): a performance change had removed the **root canvas raycaster**. Unity only sorts hits from different nested canvases by depth when they share a root raycaster, so overlapping canvases were ordered arbitrarily and a tap meant for a popup button could land on a button behind it. Fixed (root raycaster restored, with a comment explaining why it must stay).
- Two more invisible blockers removed: the tutorial hint box (clickable, covered the bottom of the board for 7-10 s; now ignores taps and hides on the first tap) and the shuffle-mode bar (covered the top row of pots; it now replaces the power-up row while shuffling).
- New PlayMode tests raycast through the real event system at the centre of **every button and pot** on the menu, on levels 1/2/4/35, in shuffle mode on boards up to level 700, inside every popup, and check popups swallow taps meant for the board behind them.
- Not covered: the audit ran at the test runner's window shape and one portrait size; landscape / TV were verified visually and by layout review but not by raycast (the Editor's Device Simulator prevented switching sizes). Real-finger testing on a phone is still needed.

## Features added in the previous pass
Resume after restart (board saved after every move, also mid-pour) · level select (paged grid, replay any unlocked level) · screen fades · win-wave hop on the board · procedural background music loop (toggle in Settings) · graded Android haptics · localization (English + Spanish; Settings button) · popups pop in.

## Tests (all run inside Unity)
- 34 EditMode tests (rules, economy, resume serialization, tutorials, dead-end freedom of the opening levels, easy start, difficulty ordering, generator determinism).
- 20 PlayMode tests that drive the real game end to end: boot, complete level 1 (reward, unlock), no double reward on replay, resume after leave, leave during a pour, restart, duplicate life consumption, undo, level select, save/reload, power-up locks / intro popups / free tokens, and the pointer-reachability audits above. They use an isolated temp save and never touch the real one. The PlayMode suite found and fixed a real bug (pour animation kept running on destroyed pots when the board was torn down mid-pour).
- Android: a development APK builds (about 3 minutes, 74 MB, IL2CPP, ARM64 + ARMv7) but has not been run on a phone. A real Windows release build succeeds (0 errors, ~36 s, 102 MB) and runs 15 s without exceptions; the release assembly contains no debug menu or fake-purchase code.

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

## Artwork (imported)
The ChatGPT art is wired in: pots, glossy buttons and panels, icons, colour-blind symbols, effects, logo and backdrops (portrait + landscape, scaled to cover the screen without stretching). `ArtSource/process_art.py` turns the raw images into the game files (names, transparency, crops); originals live in `ArtSource/ChatGPT/` (not tracked by git). `ArtImporter` applies sprite import settings automatically, including the 9-slice borders for `round` / `roundsoft`. The pot set must be supplied together; its measured shape is in `Art.ArtworkPot` (values from `ArtSource/pot_geometry.json`). Store art (app icon, adaptive layers, TV banner, feature graphic, splash) is in `Assets/Art/Store/`; the app icon is applied by **Paint Sort > Apply Player Settings**. Not done: Android adaptive-icon layers and the TV banner are not assigned in Player Settings, and the landscape / TV layouts have not been re-checked visually with the new art.
