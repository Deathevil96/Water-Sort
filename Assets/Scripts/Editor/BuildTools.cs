using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PaintSort.EditorTools
{
    /// <summary>
    /// One-command builds, for the menu and for CI / batch mode.
    /// <code>Unity -batchmode -nographics -quit -projectPath . -executeMethod PaintSort.EditorTools.BuildTools.BuildAndroid -output Builds/paintsort.apk -development</code>
    /// Arguments: <c>-output &lt;path&gt;</c> (default under Builds/), <c>-development</c> (development build, enables the debug menu and fps readout),
    /// <c>-aab</c> (Android App Bundle instead of APK), <c>-buildNumber &lt;n&gt;</c> (Android version code).
    /// Exit code is 0 on success and 1 on failure, so a CI job can fail on it.
    /// </summary>
    public static class BuildTools
    {
        const string OutputDir = "Builds";

        [MenuItem("Paint Sort/Build/Android APK (development)")] public static void MenuAndroidDev() => Run(BuildTarget.Android, development: true, bundle: false, interactive: true);
        [MenuItem("Paint Sort/Build/Android AAB (release)")] public static void MenuAndroidRelease() => Run(BuildTarget.Android, development: false, bundle: true, interactive: true);
        [MenuItem("Paint Sort/Build/Windows (release)")] public static void MenuWindowsRelease() => Run(BuildTarget.StandaloneWindows64, development: false, bundle: false, interactive: true);
        [MenuItem("Paint Sort/Build/Windows (development)")] public static void MenuWindowsDev() => Run(BuildTarget.StandaloneWindows64, development: true, bundle: false, interactive: true);

        public static void BuildAndroid() => Run(BuildTarget.Android, HasFlag("-development"), HasFlag("-aab"), false);
        public static void BuildWindows() => Run(BuildTarget.StandaloneWindows64, HasFlag("-development"), false, false);

        static void Run(BuildTarget target, bool development, bool bundle, bool interactive)
        {
            try
            {
                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/SampleScene.unity" };

                string output = ArgValue("-output") ?? DefaultOutput(target, bundle);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)) ?? OutputDir);

                if (target == BuildTarget.Android)
                {
                    EditorUserBuildSettings.buildAppBundle = bundle;
                    if (int.TryParse(ArgValue("-buildNumber"), out int code)) PlayerSettings.Android.bundleVersionCode = code;
                }

                var options = new BuildPlayerOptions
                {
                    scenes = scenes, target = target, locationPathName = output,
                    options = development ? BuildOptions.Development : BuildOptions.None,
                };
                Debug.Log("[BUILD] " + target + (development ? " (development)" : " (release)") + " -> " + output);
                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                Debug.Log("[BUILD] " + summary.result + ", " + summary.totalErrors + " errors, " + summary.totalWarnings + " warnings, "
                          + (summary.totalSize / (1024 * 1024)) + " MB, " + summary.totalTime.TotalSeconds.ToString("F0") + " s");
                bool ok = summary.result == BuildResult.Succeeded;
                if (!interactive) EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogError("[BUILD] failed: " + e);
                if (!interactive) EditorApplication.Exit(1);
            }
        }

        static string DefaultOutput(BuildTarget target, bool bundle)
        {
            switch (target)
            {
                case BuildTarget.Android: return Path.Combine(OutputDir, bundle ? "PaintSort.aab" : "PaintSort.apk");
                case BuildTarget.StandaloneWindows64: return Path.Combine(OutputDir, "Windows", "PaintSort.exe");
                default: return Path.Combine(OutputDir, target.ToString());
            }
        }

        static bool HasFlag(string name) => Environment.GetCommandLineArgs().Contains(name);

        static string ArgValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
