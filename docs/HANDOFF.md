# Handoff (2026-11-25) - continuing on MacBook

State as of 2026-11-24/25. Full notes live in `docs/claude-memory/` (copy of Claude's memory files; on the Mac you can ask Claude to read them, or copy into its memory folder).

## Done in code (compiles, NOT yet run on a device)
- Closed testing done, production access granted (2026-11-24). Build 1 shipped offline (no INTERNET, no ads).
- AdMob 11.7.0 via OpenUPM scoped registry (Packages/manifest.json).
- `Assets/Scripts/Game/AdMobService.cs`: bottom banner, preloaded rewarded + interstitial, UMP consent. `AdIds` holds Google TEST unit ids. Editor uses `MockAdService`.
- `ShipWithInternet = true`, `ReserveBannerInRelease = true`.
- Menu "Paint Sort/Ads/Set AdMob test app id" (AdsSetup.cs) writes the TEST app id into `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`.
- Real store (Play Billing) undecided; fake Shop stays hidden.

## Still to do
1. AdMob: add app (`com.misalinteractive.palettepour`), create Banner/Rewarded/Interstitial units; replace `AdIds` + call `AdsSetup.SetAppId` with real ids. (Do NOT ship test ids; never click own live ads.)
2. Firebase: create Palette Pour project + Android app, get `google-services.json`, add Firebase Unity SDK (`FirebaseAnalytics.unitypackage`, not on OpenUPM), replace `LogAnalytics` with a Firebase `IAnalytics`.
3. Rewrite `docs/privacy-policy.md` for ads + analytics, push.
4. Play Console: Ads = Yes, Data safety, Advertising ID = Yes, listing mentions ads.
5. Set version 1.0.0 (menu "Paint Sort/Version/Set 1.0.0"), build release AAB, on-device check, upload to production.
6. app-ads.txt: needs website domain + ad publisher id.
7. Store/Play Billing only if decided.

## Mac notes
- Release signing keystore is in git-ignored `Signing/` - NOT in the repo; copy it over manually (needed for AAB builds).
- Android Resolver may need to re-run (Assets > External Dependency Manager) after opening in Unity on Mac.
