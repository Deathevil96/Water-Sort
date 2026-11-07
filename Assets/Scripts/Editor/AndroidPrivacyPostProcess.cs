using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace PaintSort.EditorTools
{
    /// <summary>
    /// Keeps the shipped Android app from having the INTERNET permission. Without that permission the OS refuses every network connection, so the
    /// game physically cannot send the player's IP address or anything else to anyone. Unity adds the permission by itself whenever any code in the
    /// project touches networking, so after Gradle generates the project this adds a "remove" instruction to the launcher manifest and the manifest
    /// merger strips it from the final APK.
    ///
    /// Development builds are left alone (Unity's profiler needs the network). WHEN AN AD SDK IS ADDED set <see cref="ShipWithInternet"/> to true;
    /// the ad network needs the permission, and then the on-device check in PAINT_SORT_STATUS.md (list the app's open sockets) must be repeated.
    /// </summary>
    public sealed class AndroidPrivacyPostProcess : IPostGenerateGradleAndroidProject
    {
        public const bool ShipWithInternet = false;

        public int callbackOrder => 10000;

        public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
        {
            if (ShipWithInternet || EditorUserBuildSettings.development) return;

            // the launcher module sits next to the unityLibrary module
            string launcher = Path.Combine(Path.GetDirectoryName(unityLibraryPath) ?? "", "launcher", "src", "main", "AndroidManifest.xml");
            if (!File.Exists(launcher)) { Debug.LogWarning("[PRIVACY] launcher manifest not found at " + launcher + "; INTERNET permission not stripped"); return; }

            const string androidNs = "http://schemas.android.com/apk/res/android", toolsNs = "http://schemas.android.com/tools";
            var doc = new XmlDocument(); doc.Load(launcher);
            var root = doc.DocumentElement;
            if (root == null) return;
            if (root.GetAttribute("xmlns:tools") == "") root.SetAttribute("xmlns:tools", toolsNs);

            // drop any direct declaration, then add the merger instruction that removes inherited ones
            foreach (XmlNode n in root.SelectNodes("uses-permission"))
                if (n is XmlElement e && e.GetAttribute("name", androidNs) == "android.permission.INTERNET") root.RemoveChild(e);
            var remove = doc.CreateElement("uses-permission");
            remove.SetAttribute("name", androidNs, "android.permission.INTERNET");
            remove.SetAttribute("node", toolsNs, "remove");
            root.AppendChild(remove);
            doc.Save(launcher);
            Debug.Log("[PRIVACY] INTERNET permission removed from the release manifest");
        }
    }
}
