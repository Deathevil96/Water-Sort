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
        public const string Product = "Paint Magic Sort";

        /// <summary>
        /// Android package name. Reverse-domain style; <b>cannot be changed after the app is published on Google Play</b>, so settle it first.
        /// Lowercase letters and digits only in each segment.
        /// </summary>
        public const string AndroidPackage = "com.misalinteractive.paintmagicsort";

        [MenuItem("Paint Sort/Apply Player Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = Company;
            PlayerSettings.productName = Product;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackage);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AndroidPackage);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AndroidPackage);

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
    }
}
