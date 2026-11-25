---
name: internet-ads-store-plan
description: "Ads (AdMob) coded with test IDs; Firebase/real IDs/privacy policy/Play Console still pending; store undecided"
metadata:
  node_type: memory
  type: project
  originSessionId: 57f04b3d-1487-4c5b-8d46-6719d0643645
  modified: 2026-11-24T04:30:53.214Z
---

Closed testing is done and production access is granted (2026-11-24). Build 1 shipped offline (no INTERNET, no ads). The user chose: do ads + Firebase now; the real store (Play Billing) is undecided, so the fake Shop stays hidden/unchanged for now.

**Done in code (2026-11-24, compiles clean, NOT yet run on a device):**
- AdMob package `com.google.ads.mobile` 11.7.0 via OpenUPM scoped registry in `Packages/manifest.json`.
- `Assets/Scripts/Game/AdMobService.cs` (banner bottom, preloaded rewarded + interstitial, UMP consent); `AdIds` holds Google TEST unit ids. `App.cs` uses it on Android devices only; Editor keeps `MockAdService`.
- `ShipWithInternet = true` (AndroidPrivacyPostProcess), `ReserveBannerInRelease = true` (App.Layout.cs).
- Menu "Paint Sort/Ads/Set AdMob test app id" (AdsSetup.cs) wrote the Google TEST app id into `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`.

**Still to do (user said they will do the account steps tonight):**
1. AdMob: add app (Android, package `com.misalinteractive.palettepour`), create Banner/Rewarded/Interstitial units, send app id + 3 unit ids -> replace `AdIds` and call `AdsSetup.SetAppId`. Browser account is the user's Misal Interactive Google account; `apps.admob.com` showed a browser error page in Claude-in-Chrome (twice), so the user may do it by hand.
2. Firebase: no Palette Pour project exists yet (account has Arrow Breaker, Gem Merge). The "Create project" dialog was left open in the first browser tab, unconfirmed (screenshots timed out). Need project + Android app, `google-services.json`, and the Firebase Unity SDK (not on OpenUPM or Google's registry; user must supply `FirebaseAnalytics.unitypackage` or approve a download). Then replace `LogAnalytics` with a Firebase `IAnalytics` implementation.
3. Rewrite `docs/privacy-policy.md` (ads + analytics, currently says no data/no network) and push it.
4. Play Console: Ads = Yes, Data safety (SDK data), Advertising ID = Yes, listing text mentions ads.
5. Set version 1.0.0 (menu "Paint Sort/Version/Set 1.0.0"), build release AAB, repeat on-device check, upload to production.
6. app-ads.txt needs a website domain plus ad publisher id.
7. Store: only if the user decides; then Play Billing, billing permission, purchase declaration, content-rating answer, hide Shop until real.

**Why:** declarations must match behaviour or the app can be rejected.

**How to apply:** do not ship a release with the Google test ad ids; never click own live ads. Related: [[release-versioning-and-builds]], [[paint-sort-project]].

**Status 2026-11-25:** all of the above code is committed and pushed (b33b3eb). User moved to a MacBook, see [[mac-handoff-2026-11-25]]. AdMob/Firebase account steps still not done as of this note.

**Status 2026-11-25 (Mac session):** user chose ads only (no IAP; Shop stays hidden). Done: AdMob app + 3 real units (ids in `AdIds`, release-only via `#if UNITY_EDITOR || DEVELOPMENT_BUILD`), real app id in GoogleMobileAdsSettings, Firebase project `palette-pour-9dab5` + google-services.json (git-ignored), `FirebaseAnalyticsService` behind `PP_FIREBASE`, privacy policy rewritten, Play Console Ads/Ad ID/Data safety/listing SAVED but not sent for review. Open items are listed in `docs/HANDOFF.md` "Still to do". Publishing the shared AdMob GDPR message was blocked by auto mode; the user must do it.

**Status 2026-11-25 23:50 (end of Mac session, user restarting for Unity MCP):**
- Unity on the Mac resolved the AdMob package (OpenUPM) + com.unity.external-dependency-manager and compiled all game/editor code with NO errors (commit 3e83a51 verified in Library/ScriptAssemblies). The "stuck on resolving" was the first EDM import, not a hang.
- Play store settings: website set to deathevil96.github.io (published, user OK'd). That site already serves the correct app-ads.txt (google.com, pub-7841394126110372, DIRECT, f08c47fec0942fa0).
- NEXT (in order): (1) import ~/Downloads/FirebaseAnalytics.unitypackage, untick `ExternalDependencyManager` (EDM comes from the Unity package), add PP_FIREBASE to Android scripting defines, run Android Resolver, check compile; (2) user publishes the AdMob GDPR message with Palette Pour added (left unconfirmed in a Chrome tab; auto mode blocked me since the message is shared with 3 live apps); (3) upload key reset in Play Console; (4) Set 1.0.0, release AAB, device test of consent/banner/rewarded/interstitial and Settings > Privacy; (5) user sends for review in Publishing overview.
- Unity MCP was not connected this session; after restart use it to check Play mode first, import the package and read the console.
