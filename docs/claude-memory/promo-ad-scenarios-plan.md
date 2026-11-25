---
name: promo-ad-scenarios-plan
description: "Planned debug-menu \"clickbait / you can't do it\" promo video scenarios; user will supply a template from another project, so do not build from scratch"
metadata:
  node_type: memory
  type: project
  originSessionId: 57f04b3d-1487-4c5b-8d46-6719d0643645
  modified: 2026-11-07T22:45:32.156Z
---

Idea (user, 2026-11-08): debug-menu scenarios that auto-play like the Instagram/YouTube "bait" / "you can't do it" ad videos, so the user can start one and screen-record it. Each ends with an end card: game name (Palette Pour), Play Store logo and "Link in Bio". Plan was a scripted solver playthrough, captions, a timer and the end card, all in the development-only debug menu (`DebugMenu.cs`, compiled only under `UNITY_EDITOR || DEBUG`, so never in release builds).

Status: NOT started. The user already built this in another Unity project and will ask Claude to fetch that template and adapt it to this game's format (portrait, code-built uGUI, `App` partial classes, pure-C# core in `PaintSort.Core`).

**Why:** reuse the user's proven template instead of redesigning it.

**How to apply:** wait for the user to point at the template; when adapting it keep it inside `#if UNITY_EDITOR || DEBUG` and use honest challenge wording (no made-up statistics or fake claims), as advised earlier.

Related: [[release-versioning-and-builds]]
