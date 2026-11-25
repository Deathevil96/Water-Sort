---
name: paint-sort-project
description: "Paint Sort (Unity 6, code-built UI water-sort puzzle) - where things live, how it was verified, what is still open"
metadata:
  node_type: memory
  type: project
  originSessionId: 57f04b3d-1487-4c5b-8d46-6719d0643645
  modified: 2026-11-07T07:08:50.471Z
---

Paint Sort is a Unity 6000.6.7f1 mobile puzzle (water-sort style, paint pots). Source of truth for rules: `PAINT_SORT_GDD_CLAUDE_IMPLEMENTATION_SPEC.md`. Current state and open items: `PAINT_SORT_STATUS.md`. Art prompts for the user to run in ChatGPT: `ART_PROMPTS.md`.

- Core logic is a pure-C# asmdef (`Assets/Scripts/Core`); the whole UI is built in code (`Assets/Scripts/Game`, `App.*.cs` partials), no scene setup.
- 5,000 deterministic levels in `Assets/Resources/Levels/levels.txt`; regenerate/audit with the standalone .NET harness (scratchpad) or Paint Sort menu. Levels 1-4 are hand-designed (`TutorialLevels`).
- Tests: 32 EditMode + 10 PlayMode, run via UnityMCP `run_tests`. PlayMode tests use an isolated temp save (`App.UseIsolatedSaveForTests`).

**Why:** the user asked for a full implementation, cartoon art, responsive layouts (phone/tablet/TV) and polish; image/audio generation providers in the Editor are not configured (no API keys), so real art/sound stays with the user.

**How to apply:** to see the UI through UnityMCP, the Overlay canvas is not captured by screenshot with a camera - temporarily set the canvas to ScreenSpaceCamera (revert after). Change Game view resolution by reflection (GameViewSizes). Never write Unity API calls into MonoBehaviour field initializers. After editing scripts, always check the console for compile errors before entering Play mode.

Decided against converting uGUI Text to TextMeshPro for now (high regression risk, little gain); revisit when a custom font is chosen. Ads SDK, billing, analytics, cloud save, TV banner image and real-device testing need the user's accounts/devices.
