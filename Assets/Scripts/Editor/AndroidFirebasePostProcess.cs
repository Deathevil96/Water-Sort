using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace PaintSort.EditorTools
{
    /// <summary>
    /// Starts Google Analytics for Firebase with collection OFF, so nothing is sent before the player's consent choice.
    /// FirebaseAnalyticsService switches it on after the consent flow allows it.
    /// </summary>
    public sealed class AndroidFirebasePostProcess : IPostGenerateGradleAndroidProject
    {
        const string Key = "firebase_analytics_collection_enabled";
        public int callbackOrder => 10001;

        public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
        {
            string manifest = Path.Combine(unityLibraryPath, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifest)) { Debug.LogWarning("[FIREBASE] manifest not found at " + manifest); return; }

            const string androidNs = "http://schemas.android.com/apk/res/android";
            var doc = new XmlDocument(); doc.Load(manifest);
            var app = doc.DocumentElement?.SelectSingleNode("application") as XmlElement;
            if (app == null) { Debug.LogWarning("[FIREBASE] no <application> in " + manifest); return; }

            foreach (XmlNode n in app.SelectNodes("meta-data"))
                if (n is XmlElement e && e.GetAttribute("name", androidNs) == Key) app.RemoveChild(e);
            var meta = doc.CreateElement("meta-data");
            meta.SetAttribute("name", androidNs, Key);
            meta.SetAttribute("value", androidNs, "false");
            app.AppendChild(meta);
            doc.Save(manifest);
            Debug.Log("[FIREBASE] analytics collection off until consent");
        }
    }
}
