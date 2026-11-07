using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PaintSort.EditorTools
{
    /// <summary>
    /// Studio / product identity and store-required player settings in one repeatable place (menu: Paint Sort / Apply Player Settings).
    /// Change the constants here, run the menu item, and every platform setting follows.
    /// </summary>
    public static class ProjectSetup
    {
        public const string Company = "Misal Interactive";
        public const string Product = "Palette Pour";

        /// <summary>
        /// Android package name. Reverse-domain style; <b>cannot be changed after the app is published on Google Play</b>, so settle it first.
        /// Lowercase letters and digits only in each segment.
        /// </summary>
        public const string AndroidPackage = "com.misalinteractive.palettepour";

        [MenuItem("Paint Sort/Apply Player Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = Company;
            PlayerSettings.productName = Product;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackage);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AndroidPackage);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AndroidPackage);

            ApplyPrivacySettings();

            // the studio splash is drawn by the game itself (App.Splash.cs); switch Unity's own splash off (Unity 6 allows it on every plan)
            PlayerSettings.SplashScreen.show = false;

            // orientation: the game picks the orientation per device class at runtime; allow all three here so tablets and TV can rotate
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Google Play requires 64-bit; that needs the IL2CPP scripting backend
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;

            // app icon: the default icon applies to every platform (Android adaptive layers can be added in Player Settings > Icons)
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Store/app_icon.png");
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            else Debug.LogWarning("[SETUP] Assets/Art/Store/app_icon.png not found; app icon left unchanged");

            // Android 8+ draws launcher icons from adaptive layers (background + foreground); without them the phone shows a generic icon
            var bg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Store/icon_background.png");
            var fg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Store/icon_foreground.png");
            if (bg != null && fg != null)
            {
                foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
                {
                    var slots = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                    foreach (var slot in slots)
                    {
                        int layers = Mathf.Max(slot.GetTextures().Length, 2);
                        var set = new Texture2D[layers];
                        set[0] = bg; if (layers > 1) set[1] = fg;
                        slot.SetTextures(set);
                    }
                    PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, slots);
                }
            }
            else Debug.LogWarning("[SETUP] adaptive icon layers not found in Assets/Art/Store");

            AssetDatabase.SaveAssets();
            Debug.Log("[SETUP] " + Company + " / " + Product + " / " + AndroidPackage + " (version comes from version.json on build)");
        }

        /// <summary>
        /// Privacy: the game must not talk to anyone except (later) the ad network. Unity's own engine diagnostics / analytics / crash upload endpoints
        /// are switched off, and the INTERNET permission is not forced. An Android app without that permission cannot open a network connection at all,
        /// so until an ad SDK is added the shipped game is physically unable to expose the player's IP. When an ad SDK is added it brings its own
        /// permission; re-run the on-device socket check described in PAINT_SORT_STATUS.md afterwards.
        /// </summary>
        static void ApplyPrivacySettings()
        {
            PlayerSettings.Android.forceInternetPermission = false;
            SetBool("ProjectSettings/UnityConnectSettings.asset", "InsightsSettings.m_EngineDiagnosticsEnabled", false);
            SetBool("ProjectSettings/UnityConnectSettings.asset", "InsightsSettings.m_Enabled", false);
            SetBool("ProjectSettings/UnityConnectSettings.asset", "CrashReportingSettings.m_EnableCloudDiagnosticsReporting", false);
            SetBool("ProjectSettings/UnityConnectSettings.asset", "UnityAnalyticsSettings.m_Enabled", false);
            SetBool("ProjectSettings/UnityConnectSettings.asset", "UnityAdsSettings.m_Enabled", false);
            SetBool("ProjectSettings/UnityConnectSettings.asset", "UnityPurchasingSettings.m_Enabled", false);
            SetBool("ProjectSettings/ProjectSettings.asset", "submitAnalytics", false);
            PlayerSettings.enableCrashReportAPI = false;
        }

        static void SetBool(string assetPath, string property, bool value)
        {
            var objs = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (objs == null || objs.Length == 0) { Debug.LogWarning("[SETUP] privacy: cannot load " + assetPath); return; }
            var so = new SerializedObject(objs[0]); var p = so.FindProperty(property);
            if (p == null) { Debug.LogWarning("[SETUP] privacy: no property " + property); return; }
            p.boolValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
