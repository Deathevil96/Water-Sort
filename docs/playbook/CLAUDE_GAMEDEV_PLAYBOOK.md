# Game Dev Playbook: Code, Optimization, Legal and Technical (2D and 3D, Unity + Claude Code)

Apply to any Unity mobile game, 2D or 3D. Items marked (used) were done in a shipped game (Arrow Breaker, Android, live on Google Play; Palette Pour, sent to production with AdMob and Firebase in Nov 2026, see section 6). Items marked (recommended) are standard practice, not yet proven in that game. Do not assume the render pipeline: check the project (URP, HDRP, Built-in, 2D or 3D) before suggesting settings.

---

## 1. Professional code practices

- **Separate logic from Unity (used).** Core rules in plain C# classes (no MonoBehaviour): board, solver, economy, stats, save. Fast to unit test, easy to reason about, hard to break.
- **One config file for every tunable number (used).** Rewards, costs, cooldowns, difficulty curves in one `Economy`/`Config` class. Balancing is then a one-file change.
- **Versioned save data (used).** Keep app, save, generator and economy version numbers; migrate old saves instead of crashing. Never change a save format without a migration.
- **Interfaces around third-party SDKs (used).** `IAdProvider` with a stub for Editor/tests and the real SDK for release. Same for analytics and IAP.
- **Compile defines for platform and build type (used).** `UNITY_ANDROID && !UNITY_EDITOR`, plus custom ones (test ads vs production). Test builds can never ship test settings by accident.
- **Self-test menu run before every release build (used).** 335 checks (rules, saves, economy, generators). Release build refuses to proceed if tests fail.
- **Test sandbox for saves (used).** Tests write to a throwaway save prefix, never the real player data.
- **Deterministic generation (used).** Procedural content from a seed + level index, validated by a solver so every level is winnable. No stored level files.
- **Small commits, feature branches for risky work (used).** Keep release branch clean; IAP lived on its own branch until tested.
- **Assembly definitions (used in SkyQueue).** Split code into asmdefs (`Core` pure model, `View` Unity layer, later Platform). Faster compile, enforced dependencies, Core stays Unity-free.
- **Data-driven config (used in SkyQueue).** Gameplay, layout, timing and colours live in a `GameConfig` ScriptableObject/scene component and definition assets, not in code. Saved scene/asset values override code defaults, so update both when changing a default.
- **Fast level validator, not bot-play (used in SkyQueue).** Validate solvability and difficulty with a cheap pure-C# validator. Bot-play and difficulty-tuning runs freeze the Editor; avoid them.
- **Fast EditMode unit tests (used in SkyQueue).** Keep the suite small and quick (209 tests in about 1.5 s) so it is run after every change.
- **Dev logging with tags and profiler markers (used in SkyQueue).** A `DevLog` with category tags, stripped from release, plus `ProfilerMarker`s around hot paths. Document the tags in `DEVLOGS.md`.
- **In-game debug menu (used in SkyQueue).** Editor and dev builds only (button or F1): add coins/boosters, unlock, force win/lose, jump levels, reset save. Makes QA on a phone fast.
- **Events or small services, not singletons everywhere (recommended).** Avoid `FindObjectOfType` and hidden global state.
- **No magic strings (recommended).** Constants or enums for event names, save keys, tags, layers.
- **Fail loudly in dev, quietly in release (recommended).** Assertions and logs in dev; try/catch and safe defaults around save load and SDK calls in release.
- **Strip logs in release (recommended).** `Debug.Log` costs time and allocations on device; wrap in a conditional attribute.

## 2. Optimization techniques

### CPU and memory
- **Do heavy work off the main thread (used).** Level generation ran on a background thread; no frame hitch. (Unity API calls must stay on main thread; pass plain data.)
- **Generate instead of store (used).** No large content files in the build.
- **Build UI once, reuse (used).** Created in code through a shared helper kit; no per-frame rebuilds.
- **Avoid per-frame allocations (recommended).** No `new` lists, LINQ, string concatenation, or boxing in `Update`. Cache components and references. Use `StringBuilder`, pre-sized lists, `NonAlloc` physics calls.
- **Object pooling (used in SkyQueue)** for VFX bursts, exhaust puffs, effects, enemies, floating text, UI rows. Check the pool empties after use.
- **Do not run logic every frame if it is not needed (recommended).** Timers, events, coroutines with cached waits, or lower-frequency ticks.
- **Cap and set frame rate (recommended).** `Application.targetFrameRate` 60 (30 for idle menus), `vSyncCount = 0` on mobile.
- **Garbage collector (recommended).** Use incremental GC; keep GC spikes out of gameplay.
- **Profile before optimizing (recommended).** Unity Profiler, Memory Profiler, Frame Debugger, and profile on a real mid-range device, not the Editor.

### Rendering (2D and 3D)
- **Cheap visuals by design (used).** Thin line art, minimal palette, low overdraw.
- **No ad or UI overdraw over gameplay (used).** One opaque reserved banner strip.
- **Batching (GPU instancing used in SkyQueue).** SRP Batcher (URP/HDRP), GPU instancing, static batching for static meshes, sprite atlases for 2D, shared materials.
- **Reduce draw calls and overdraw (recommended).** Combine meshes, atlas textures, avoid large transparent layers, avoid full-screen post effects on mobile.
- **3D specifics (recommended).** LODs, occlusion culling, baked lighting over realtime lights, 1 realtime light max on mobile, simple shadows or blob shadows, mesh and texture budgets per scene, avoid Standard-shader-heavy materials on low-end devices (use simple Lit/Unlit).
- **Textures (recommended).** ASTC compression on Android, power-aware sizes, mipmaps for 3D, disable Read/Write, max size per platform, crunch compression for size.
- **Audio (recommended).** Compressed Vorbis for music (streaming), ADPCM/short clips decompress-on-load for SFX, mono for SFX.

### Build size and startup
- **Only needed packages in the manifest (used).** Remove unused modules.
- **IL2CPP + ARM64 + strip engine code (recommended).** Required for Play (64-bit). Use Managed Stripping Low/Medium and a `link.xml` for reflection-dependent SDKs.
- **Addressables or asset bundles for big content (recommended).** Keep the base AAB small.
- **Check the Editor build report (recommended).** Find the largest assets and delete or compress them.
- **Android App Bundle (AAB) (used).** Play serves per-device splits.

### Battery, heat, network
- **Lower frame rate on static screens (recommended).**
- **Batch analytics events (used by Firebase).** Do not fire network calls per frame.
- **Do not load ads while gameplay is rendering heavy frames (recommended).** Preload at safe moments (menu, win screen).

### Economy and game design optimization (used)
- **Playtest the economy for loops.** Uncapped combo coins let a player buy refills to level 800; fixed with flat rewards per tier.
- **Cooldowns and daily caps on rewarded ads (used).** A choice, not a nag.
- **Interstitials: minimum gap, never in the first levels (used).**
- **Scale lives with difficulty (used).**

## 3. Legal checklist

| Item | Details |
|---|---|
| Privacy policy | Live public URL. Must name ads, analytics, purchases, data collected, retention, contact email, children's policy. Update whenever you add an SDK. |
| Terms of service | Needed if you sell anything: purchase terms, refund policy, governing law, seller identity. |
| Refund policy | Decide it in writing. Google refunds within 48 h regardless; consumed virtual currency is usually non-refundable after. |
| Consent (GDPR/UK/EEA) | Use Google UMP (CMP) before any ad or analytics. Collect nothing until consent is given (used: Firebase collection OFF in manifest until UMP consent). |
| COPPA / children | Decide the target audience honestly. If children are included, ads and analytics rules get much stricter (Families policy). |
| Data Safety form (Play) | Must match what your SDKs really collect (AdMob, Firebase). Re-check after every SDK add or update. |
| Ads declaration | Declare that the app contains ads. |
| app-ads.txt | Host on your developer website root, link the site in Play listing; prevents ad fraud and lost revenue. |
| Content rating | IARC questionnaire in Play Console; be truthful. |
| Trademark and assets | Do not use another game's name, icon style, audio or art. Check fonts, SFX, music and AI-generated assets for licence terms; keep a credits and licence file. |
| Open-source licences | List third-party SDK and package licences (Unity, Firebase, AdMob, plugins); include a credits screen if required. |
| Developer identity | Play requires verified identity; personal accounts have a closed-test minimum (testers and days) before production. Business accounts need D-U-N-S. |
| Tax and payments | Merchant account, bank and tax details needed before selling IAP. You own this; Claude must not fill it in. |
| Store fees | Google takes 15% (first $1M) or 30%; some regions add billing fees. Price accordingly. |
| Regional rules | Some regions restrict stores or loot-box style mechanics (hide shop in EEA/UK/CH if unsure; check local gambling-like rules). |
| Gambling-style mechanics | Random paid rewards (loot boxes) need legal review per country. |

## 4. Technical publishing checklist (Google Play)

**Project settings**
- Unique package id (cannot change later)
- Version name (human) and **version code** (integer, only goes up; bump only on release AAB builds, never on test APKs)
- Min SDK per Unity default, Target SDK meeting current Play requirement
- IL2CPP, ARM64 (and ARMv7 if you still support it), AAB output
- Keystore: **upload keystore created once, backed up in two places** (loss means you cannot update the app). Opt in to Play App Signing.
- Never commit the keystore or passwords to git (git-ignore it). To move machines, copy `Signing/` by hand or through a password manager, never through a public repo. If a key leaks, follow the steps in section 6.9.

**Dependencies**
- Android Resolver (External Dependency Manager) resolved before build; generated local repos git-ignored. Details and pitfalls in section 6.4.
- Check each SDK's required Android permissions and target SDK compatibility
- Use real AdMob app id and ad unit ids only in the production build; Google test ids in dev

**Store listing**
- 512x512 icon, 1024x500 feature graphic, 2 to 8 phone screenshots (plus 7"/10" tablet if offered), short (80 char) and full description, category, tags, support email, privacy policy URL
- Screenshots should show real gameplay, not mockups

**Testing path**
1. Internal testing (fast, small team)
2. Closed test (required tester count and duration for new personal accounts)
3. Production with **staged rollout** (start 5 to 20%), watch crashes and ANRs in Android vitals. The **first** production release of an app cannot be staged and goes to 100%; staging applies from the first update onward.
4. Google review can take days; plan buffer

**Release gate checklist**
- [ ] Self-tests pass
- [ ] Release build uses production ad ids, not test ids
- [ ] Analytics and ads gated by consent
- [ ] Version code is the next unused number
- [ ] Data Safety, privacy policy and listing match the build
- [ ] Tested on a real low-end and a real recent device
- [ ] Release notes written (and a changelog file in the repo)
- [ ] Crash reporting on (Firebase Crashlytics, recommended)

**After launch**
- In-app update API so players move to fixed builds (used)
- Analytics events for the funnel: level start, win, fail, ad shown, purchase (used)
- Optional: block sideloaded release builds that pirate or bypass ads (used, only when the production define is set)
- Watch Android vitals (crash rate, ANR rate); bad vitals reduce store visibility

## 5. Claude Code working rules (what made this efficient)

- Write a design doc first (core loop, modes, economy, monetization, UI direction); keep it updated.
- Keep memory notes of decisions, ids, links and "what the human still owns".
- Claude must **never submit store forms, releases, purchases or legal declarations without the human's explicit OK**.
- When Claude says "done", ask for proof: test count, screenshot, log line.
- Run `/code-review` and `/simplify` before a release.
- Unity Editor playing while unfocused can stall frames; write tests that call game logic directly.
- Long Unity builds time out the MCP call; do not retry, wait for the output file.
- Commit small. Use branches for risky features.
- **Check Play mode first (used in SkyQueue).** If the Editor is in Play mode, change nothing (edits are lost) and tell the human.
- **Never close or restart the Unity Editor** unless the human explicitly says so, and never run heavy main-thread jobs. No bot-play or tuning runs. Before calling Unity "hung", check the Gradle/java processes and the log; the first build after adding AdMob or Firebase sits at "Resolving Android Gradle dependencies" for several minutes (used in Palette Pour: it was called hung twice and was not). If Unity really is blocked, ask the human what dialog is showing.
- **After a force quit**, Unity asks to recover scene backups. Choose "Don't Recover" when the scene is empty or code-built, and git-ignore `Assets/_Recovery/`.
- **Back up the real save before any Play-mode test and restore it after.** Tests must never leave progress in the real save.
- **Step frames manually when the Editor is unfocused:** pause, set `Time.captureDeltaTime`, call `EditorApplication.Step()`. Screenshot with `ScreenCapture.CaptureScreenshot` into a git-ignored folder. Destroy preview objects in `finally`.
- **New shaders show cyan placeholders** until async compile finishes; step more frames before judging a screenshot.
- **Keep a "Decisions (do not re-litigate)" list** in the project `CLAUDE.md` for settled design choices, so sessions do not reopen them.
- **Keep memory in the repo** (`.claude/memory/` with an index) so a new machine or session resumes instantly; keep `BACKLOG.md` (done/open) and `DEVLOGS.md` beside it.
- **Keep the playbook in two places:** `~/.claude/GameDevPlaybook.md` (loaded by Claude Code on this machine) and the account skill `game-dev-playbook` (claude.ai > Plugins, "By you", which syncs to claude.ai, the desktop app and Claude Code everywhere). After editing the playbook, rebuild the skill (`SKILL.md` = frontmatter + this file) and upload it again via Plugins > Add > Upload plugin. Copy it to the repo too (`docs/playbook/`).
- **Report honestly what was and was not verified.** Say when something was wired but not seen on screen.
- **Art prompts must spell out delivery details:** 2D sheets on a flat solid magenta (#FF00FF) background, no checkerboard/gradient/glow, pieces separated; 3D: which parts are separate meshes, orientation, pivot. Process sheets with repo-relative scripts (`Tools/art/*.py`). When the human says "added assets": list new files, inspect, wire in, verify with a Play-mode screenshot.

## 6. Runbook: ads, analytics and the first production release (used in Palette Pour, Nov 2026)

Follow this in order whenever a game goes from offline or closed testing to a production release with AdMob and Firebase. Do the steps without asking for more instructions. Only the approval gates in 6.10 need the human's explicit yes.

### 6.1 Shared account facts (same for every game)
- Developer: Misal Interactive. Play Console developer id `7597472884201261643`. AdMob publisher `pub-7841394126110372`.
- Developer website: `deathevil96.github.io` (GitHub Pages). It already serves the app-ads.txt line `google.com, pub-7841394126110372, DIRECT, f08c47fec0942fa0`. For each new game, only set this website in Play > Store settings > Store listing contact details ("Save and publish" goes live at once).
- AdMob > Privacy & messaging > European regulations already has one published GDPR message shared by all games (it uses `%%APP_NAME%%`). Add each new app to it rather than creating a new one.
- Browser: use the Chrome extension on the MacBook (several browsers are connected; let the human pick on first use).
- Signing key backups live in `~/Documents/PalettePour-Signing-Backup/`. Use a similar per-game folder outside the repo.

### 6.2 AdMob
1. Apps > Add app > Android. If the game is not public on Play yet, answer "No, not listed" (the store search will not find it). Name it with the store title. Link the store later via "Add store" once the game is live.
2. Create three ad units (Banner, Interstitial, Rewarded). Name them `<Short> Banner` and so on. Keep the reward defaults; amounts live in game code.
3. Code: unit ids in one `AdIds` class, with `#if UNITY_EDITOR || DEVELOPMENT_BUILD` for Google's test ids and the real ids otherwise. The real **app** id goes in `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset` and is safe in dev builds, because test unit ids never serve live ads.
4. Set `MobileAds.RaiseAdEventsOnUnityMainThread = true` before `MobileAds.Initialize`. Without it, ad callbacks arrive on a Java thread and any UI they touch breaks.
5. UMP consent: `ConsentInformation.Update` then `ConsentForm.LoadAndShowConsentFormIfRequired`, and initialise ads only when `CanRequestAds()` is true. Add a Settings > Privacy button that calls `ConsentForm.ShowPrivacyOptionsForm` when `PrivacyOptionsRequirementStatus == Required`.
6. Add the app to the shared European regulations message, with its privacy policy URL, then Publish changes. **Click "Add URL" before ticking the app's checkbox:** ticking first shows a warning banner that shifts the rows, so the next click lands on another app's URL.

### 6.3 Firebase Analytics
1. Firebase console > Create project named after the game. Turn **off** "Join Google Developer Program" and Gemini, keep Analytics **on**, and pick "Default Account for Firebase".
2. Add an Android app with the exact package name and download `google-services.json` into `Assets/`. **Git-ignore it in public repos** (it holds an API key), along with `Assets/Plugins/Android/FirebaseApp.androidlib/` and `Assets/StreamingAssets/google-services-desktop.json*`. Keep a copy in the backup folder.
3. Firebase Unity SDK: download the zip from `firebase.google.com/download/unity` (about 770 MB) and import only `FirebaseAnalytics.unitypackage`. If EDM comes from a UPM package (`com.unity.external-dependency-manager` or OpenUPM), **leave out the package's `Assets/ExternalDependencyManager` folder**. Repack the `.unitypackage` without those entries (a tar.gz keyed by GUID folders, each with a `pathname` file), then `AssetDatabase.ImportPackage(path, false)`.
4. After the import, re-save every `PluginImporter` under `Assets/Firebase` (`SaveAndReimport`) to clear the "PluginImporter version 1 below minimum" warnings.
5. Git-ignore `Assets/Firebase/Plugins/x86_64/` (about 180 MB of desktop natives; one file is over 100 MB and is not covered by LFS). The game only uses Firebase on Android; re-import the package on a new machine.
6. Code: put `FirebaseAnalyticsService : IAnalytics` behind a project define (for example `PP_FIREBASE`) plus `UNITY_ANDROID && !UNITY_EDITOR`, so the project compiles before the SDK is imported. Start collection off with an Android post-process that adds `<meta-data android:name="firebase_analytics_collection_enabled" android:value="false"/>`. Call `SetAnalyticsCollectionEnabled(true)` only after the UMP flow allows ads (raise an event from the ad service).
7. Add the define in Player Settings > Android, then verify with `PlayerBuildInterface.CompilePlayerScripts` for Android, which works without switching platform. Check that the output dll contains the Firebase class and the real ad ids.

### 6.4 Android Resolver (EDM) and builds
- Switch the active platform to Android (`SwitchActiveBuildTargetAsync`) **before** Force Resolve. On the Mac platform, the resolver ignores Firebase's dependencies.
- Turn off auto-resolution prompts in `ProjectSettings/GvhProjectSettings.xml`: `AutoResolutionDisabledWarning`, `AutoResolveOnBuild`, `EnableAutoResolution` and `PromptBeforeAutoResolution` all `False`. Otherwise a modal "Enable auto-resolution?" dialog blocks builds. Keep `UseJetifier=True`.
- Check that `Assets/Plugins/Android/mainTemplate.gradle` lists `play-services-ads`, `user-messaging-platform` and `firebase-analytics`. Git-ignore `Assets/GeneratedLocalRepo/`.
- Leave AdMob's `enableGradleBuildPreProcessor` on (that is the known-good setting). Its first run is slow, not hung.
- Development builds must set `PlayerSettings.Android.useCustomKeystore = false`. A release build leaves it on, and keystore passwords are never saved, so the next dev build fails with "Unable to sign".
- Pull a new package from git into a running Editor and nothing happens until the Editor is focused. The human must click into Unity (or the MCP must request a refresh).

### 6.5 Verify the release AAB before upload
- `keytool -printcert -jarfile <aab>`: the SHA-256 must match the upload key registered in Play (App integrity).
- Unzip the AAB and search: real AdMob unit and app ids present, Google test ids (`3940256099942544`) absent, `firebase_analytics_collection_enabled` and `com.google.android.gms.permission.AD_ID` in `base/manifest/AndroidManifest.xml`.
- In the generated Gradle project, `unityLibrary/FirebaseApp.androidlib/res/values/google-services.xml` must exist, with `google_app_id`.
- Install the dev APK on a real phone if one is connected (`adb devices`): consent form, banner, rewarded, interstitial, Settings > Privacy. If none is connected, say so plainly; do not claim it was device-tested.

### 6.6 Privacy policy (one Markdown page per game in the repo, linked from Play and AdMob)
Must cover: what the game stores locally; AdMob (advertising ID, IP-based approximate location, device and app info, ad interactions); Google Analytics for Firebase (app instance id, advertising ID, device model, OS, country, in-game events); consent and the in-game Privacy button; resetting the advertising ID; a **"Delete your data"** section (steps, data types, timeline). Play's Data safety form needs a deletion URL, so link to that section's anchor. Also security, retention (at most 14 months), children (not directed at under-13s) and a contact email. Push it before submitting.

### 6.7 Play Console declarations for ads-only + Firebase Analytics, age 13+
- **Ads:** Yes.
- **Advertising ID:** Yes. Purposes: Analytics, Advertising or marketing, Fraud prevention, security and compliance.
- **Data safety:** collects data = Yes; encrypted in transit = Yes; no account creation; no external login; deletion = Yes, with the policy anchor URL. Data types: Location > Approximate location; App activity > App interactions; App info and performance > Diagnostics; Device or other IDs. For **each**: Collected + Shared, not ephemeral, required, purposes Analytics + Advertising or marketing + Fraud prevention. The category sections are an accordion (one open at a time), so open a section, tick it, then open the next.
- **Target audience:** 13-15, 16-17 and 18+ only. Under-13 brings in Families policy and certified ad SDKs.
- **Store listing:** do not claim "works offline" or "no network" once ads exist. Each declaration page shows "Save" then a "Go to Publishing overview?" dialog; choose "Not now" until everything is ready.

### 6.8 Production release
1. Test and release > Production > Create new release. **The human uploads the AAB by dragging it** (the browser tool only uploads files up to 10 MB). Run `open -R <aab>` to show it in Finder.
2. The release notes box holds template text. Select all and delete it first, then type `<en-US>` ... `</en-US>`.
3. Countries / regions starts **empty** for Production. Tick the header checkbox to select all (178), then save.
4. **An older closed-test build that lacks AD_ID blocks review** once the Advertising ID declaration says Yes ("A manifest file in one of your active artifacts doesn't include AD_ID"). Fix it by pausing closed testing (the human chose this) or by putting the new build on that track too.
5. Warnings about missing deobfuscation and native symbol files are normal for Unity and do not block review.
6. Publishing overview > "Submit N changes for review" > "Send changes for review". Quick checks take up to 14 minutes, then the review takes up to 7 days. With managed publishing off, the release goes live automatically once approved.
7. After a release build, the version auto-bumps (next 1.0.1 with the next code). Commit `version.json` and `ProjectSettings.asset`.

### 6.9 If a signing key leaks (happened once: keystore and passphrase pushed to a public repo)
1. Restore the key locally and back it up outside the repo.
2. Generate a new upload key (`keytool -genkeypair ... -alias <same>`) in `Signing/new/` and export `upload_certificate.pem`.
3. `git rm` the leaked files and push. They stay in history, so treat the old key as compromised.
4. **Upload the pending release signed with the OLD key first.** Then request Play Console > App integrity > App signing > Request upload key reset with the new `.pem`. Switch builds to the new key only after Google approves.

### 6.10 Approval gates (ask once, in one question, and give the exact effect)
Get the human's explicit yes before: publishing the shared GDPR message (it is live for every game); saving Play declarations (Ads, Advertising ID, Data safety); "Save and publish" on store settings; creating the production release, choosing countries and pausing tracks; "Send changes for review"; downloading large SDKs; force-quitting Unity. Everything else in this runbook (AdMob app and units, Firebase project, code, builds, verification, commits) needs no extra approval once the human has said "set up ads and Firebase" or "publish". Auto mode may still block an approved action. If it does, ask the human for a fresh explicit yes, or let them click it.

## 7. Starter prompt for a new project

```
I am making a [2D/3D] mobile game in Unity ([URP/HDRP/Built-in]). Follow my playbook.
First check the project's render pipeline and settings; do not assume.
Rules: core logic in plain testable C#; all tunable numbers in one config file;
third-party SDKs behind interfaces with stubs for the Editor; consent before ads or
analytics; self-test menu run before each release; no per-frame allocations; profile
on a real device before optimizing; pooling for spawned objects; versioned saves with
migration; small commits; keep a memory file; never submit store forms or releases
without my OK; only release builds bump the version code; give short, plain answers.
For ads, analytics and publishing to Play, follow playbook section 6 end to end.
```
