---
name: release-versioning-and-builds
description: "Build naming, version scheme (versionName auto-patch, 1.0.0 for production) and where the signed AAB lives"
metadata:
  node_type: memory
  type: project
  originSessionId: 57f04b3d-1487-4c5b-8d46-6719d0643645
  modified: 2026-11-07T22:44:03.601Z
---

Build 1 = `Builds/PalettePour_1_release.aab`: versionName 0.1.0, version code 1, signed with the upload key in git-ignored `Signing/` (user must back up the keystore and password), no INTERNET permission, no DBG/dev banner. Built 2026-11-08 and handed to the user for the closed-testing draft.

Naming: `PalettePour_<code>_release.aab` or `PalettePour_<code>_debug.apk` (development builds use `_debug` and reuse the last release's code).

Versioning (`version.json`, logic in `Assets/Scripts/Editor/BuildTools.cs`):
- After every successful RELEASE build the patch number auto-increments, so the next closed-testing release is already **0.1.1** (version.json now says 0.1.1, build code 1 used).
- Production launch = **1.0.0**: run menu "Paint Sort/Version/Set 1.0.0 (production release)" before that build.
- Play version code (`buildNumber`) goes up by 1 on every release build; it can never repeat.
- Release builds go through the menu "Paint Sort/Build/Android AAB (release, bumps build code)" (not `manage_build`, which skips signing and versioning). The tool call times out but the build finishes (about 1-2 min).

**Why:** the user wants releases ready to go without hand-editing versions.

**How to apply:** before building, check `version.json`; do not edit it by hand unless moving to 1.0.0. See [[internet-ads-store-plan]] for what changes when ads/analytics/store arrive.
