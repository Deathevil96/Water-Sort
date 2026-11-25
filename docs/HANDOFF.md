# Handoff (2026-11-25) - continuing on MacBook

State as of 2026-11-24/25. Full notes live in `docs/claude-memory/` (copy of Claude's memory files; on the Mac you can ask Claude to read them, or copy into its memory folder).

## Done in code (compiles, NOT yet run on a device)
- Closed testing done, production access granted (2026-11-24). Build 1 shipped offline (no INTERNET, no ads).
- AdMob 11.7.0 via OpenUPM scoped registry (Packages/manifest.json).
- `Assets/Scripts/Game/AdMobService.cs`: bottom banner, preloaded rewarded + interstitial, UMP consent. `AdIds` holds Google TEST unit ids. Editor uses `MockAdService`.
- `ShipWithInternet = true`, `ReserveBannerInRelease = true`.
- Menu "Paint Sort/Ads/Set AdMob test app id" (AdsSetup.cs) writes the TEST app id into `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`.
- Real store (Play Billing) undecided; fake Shop stays hidden.

## Done 2026-11-25 (Mac)
- AdMob app "Palette Pour: Sort Puzzle" (app id ca-app-pub-7841394126110372~2192223412, not linked to a store yet). Units: Banner .../6929865577, Interstitial .../2519847272, Rewarded .../4682976188. Release builds use them; Editor/dev builds keep Google test ids.
- Firebase project `palette-pour-9dab5` with Android app; `Assets/google-services.json` (git-ignored, backup in ~/Documents/PalettePour-Signing-Backup/).
- Code: FirebaseAnalyticsService (behind PP_FIREBASE define; collection off until consent), Settings > Privacy opens the UMP options form, AdMob events on main thread.
- Privacy policy rewritten for ads + analytics. Play Console saved (NOT sent for review): Ads = Yes, Advertising ID = Yes, Data safety, listing text.
- Monetization: ads only, no IAP.

## Still to do
1. Unity: focus the Editor so it resolves the AdMob package and compiles; fix any errors.
2. Import ~/Downloads/FirebaseAnalytics.unitypackage. UNTICK the `ExternalDependencyManager` folder (EDM already comes from OpenUPM). Then add `PP_FIREBASE` to Android scripting defines and run Android Resolver.
3. AdMob > Privacy & messaging > European regulations: add Palette Pour to the message (privacy URL = GitHub policy) and Publish changes (user does this; it is live for the other 3 apps).
4. app-ads.txt: host `google.com, pub-7841394126110372, DIRECT, f08c47fec0942fa0` at the root of a developer website and add that site in Play store settings.
5. Play upload key reset (see Signing keys below).
6. Set version 1.0.0, build release AAB, test on a device (consent form, banner, rewarded, interstitial), then send for review in Publishing overview.

## Mac notes
- Release signing keystore is in git-ignored `Signing/` - NOT in the repo (restored on the Mac 2026-11-25).
- Android Resolver may need to re-run (Assets > External Dependency Manager) after opening in Unity on Mac.

## Signing keys (2026-11-25)
The old upload key and its passphrase were pushed to this PUBLIC repo for the move, so treat the old key as exposed. Both files were removed from the repo, but they are still in git history.
- `Signing/palettepour-upload.jks`: OLD upload key, still the one release builds use (SHA-256 52:81:50:...:B7:33).
- `Signing/new/`: NEW upload key (alias `palettepour`, SHA-256 27:BA:1B:...:29:AD) and `upload_certificate.pem`.
- Backup of both: `~/Documents/PalettePour-Signing-Backup/` on the Mac. Keep a second copy somewhere off this machine.
- **TODO (user):** Play Console > Test and release > App integrity > App signing > Request upload key reset, attach `Signing/new/upload_certificate.pem`. Once Google approves it, move the old files to `Signing/old/` and the files in `Signing/new/` up into `Signing/`. Do not swap before approval, or uploads get rejected.
