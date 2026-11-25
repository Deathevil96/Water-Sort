using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PaintSort.EditorTools
{
    /// <summary>Writes the AdMob app id into the Google Mobile Ads settings asset (its settings class is internal, hence reflection).</summary>
    public static class AdsSetup
    {
        public const string TestAndroidAppId = "ca-app-pub-3940256099942544~3347511713";
        const string AssetPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";

        [MenuItem("Paint Sort/Ads/Set AdMob test app id")]
        public static void SetTestAppId() => SetAppId(TestAndroidAppId);

        public static void SetAppId(string appId)
        {
            var type = Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor");
            if (type == null) { Debug.LogError("[ADS] GoogleMobileAdsSettings type not found"); return; }
            var settings = AssetDatabase.LoadAssetAtPath(AssetPath, type) as ScriptableObject;
            if (settings == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                settings = ScriptableObject.CreateInstance(type);
                AssetDatabase.CreateAsset(settings, AssetPath);
            }
            type.GetProperty("GoogleMobileAdsAndroidAppId", BindingFlags.Public | BindingFlags.Instance).SetValue(settings, appId);
            EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
            Debug.Log("[ADS] AdMob Android app id set to " + appId);
        }
    }
}
