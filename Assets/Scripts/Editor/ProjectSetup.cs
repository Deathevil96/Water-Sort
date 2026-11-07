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

            AssetDatabase.SaveAssets();
            Debug.Log("[SETUP] " + Company + " / " + Product + " / " + AndroidPackage + " (version comes from version.json on build)");
        }
    }
}
