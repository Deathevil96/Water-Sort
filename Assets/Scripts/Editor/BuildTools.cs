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
    /// <code>Unity -batchmode -nographics -quit -projectPath . -executeMethod PaintSort.EditorTools.BuildTools.BuildAndroid -output Builds/game.aab -aab</code>
    /// Arguments: <c>-output &lt;path&gt;</c> (default under Builds/), <c>-development</c> (development build: debug menu and fps readout),
    /// <c>-aab</c> (Android App Bundle instead of APK). Exit code is 0 on success and 1 on failure.
    ///
    /// Versioning (see <c>version.json</c>): <c>versionName</c> is the human version (bump it with the Paint Sort/Version menu).
    /// <c>buildNumber</c> is the Play Store version code (1, 2, 3 ...). It goes up by one for every successful RELEASE build and is never
    /// touched by development builds, so a development build reuses the last release's code. A failed build does not use up a number.
    /// </summary>
    public static class BuildTools
    {
        const string OutputDir = "Builds";
        public const string VersionPath = "version.json";

        [Serializable] sealed class VersionFile { public string versionName = "0.1.0"; public int buildNumber; }

        [MenuItem("Paint Sort/Build/Android APK (development)")] public static void MenuAndroidDev() => Run(BuildTarget.Android, development: true, bundle: false, interactive: true);
        [MenuItem("Paint Sort/Build/Android AAB (release, bumps build code)")] public static void MenuAndroidRelease() => Run(BuildTarget.Android, development: false, bundle: true, interactive: true);
        [MenuItem("Paint Sort/Build/Windows (release, bumps build code)")] public static void MenuWindowsRelease() => Run(BuildTarget.StandaloneWindows64, development: false, bundle: false, interactive: true);
        [MenuItem("Paint Sort/Build/Windows (development)")] public static void MenuWindowsDev() => Run(BuildTarget.StandaloneWindows64, development: true, bundle: false, interactive: true);

        [MenuItem("Paint Sort/Version/Show current")] public static void ShowVersion() { var v = ReadVersion(); Debug.Log("[VERSION] " + v.versionName + "  (last release build code " + v.buildNumber + ")"); }
        [MenuItem("Paint Sort/Version/Bump patch (0.1.0 -> 0.1.1)")] public static void BumpPatch() => Bump(2);
        [MenuItem("Paint Sort/Version/Bump minor (0.1.0 -> 0.2.0)")] public static void BumpMinor() => Bump(1);
        [MenuItem("Paint Sort/Version/Bump major (0.1.0 -> 1.0.0)")] public static void BumpMajor() => Bump(0);

        public static void BuildAndroid() => Run(BuildTarget.Android, HasFlag("-development"), HasFlag("-aab"), false);
        public static void BuildWindows() => Run(BuildTarget.StandaloneWindows64, HasFlag("-development"), false, false);

        static void Run(BuildTarget target, bool development, bool bundle, bool interactive)
        {
            try
            {
                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/SampleScene.unity" };

                // version: development builds reuse the last release's code; release builds use the next one (written back only if the build succeeds)
                var version = ReadVersion();
                int code = development ? Math.Max(1, version.buildNumber) : version.buildNumber + 1;
                PlayerSettings.bundleVersion = development ? version.versionName + "-dev" : version.versionName;
                PlayerSettings.Android.bundleVersionCode = code;
                PlayerSettings.iOS.buildNumber = code.ToString();

                string output = ArgValue("-output") ?? DefaultOutput(target, bundle);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)) ?? OutputDir);
                if (target == BuildTarget.Android) EditorUserBuildSettings.buildAppBundle = bundle;

                var options = new BuildPlayerOptions
                {
                    scenes = scenes, target = target, locationPathName = output,
                    options = development ? BuildOptions.Development : BuildOptions.None,
                };
                Debug.Log("[BUILD] " + target + (development ? " (development)" : " (release)") + " version " + PlayerSettings.bundleVersion + " code " + code + " -> " + output);
                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                bool ok = summary.result == BuildResult.Succeeded;
                if (ok && !development) { version.buildNumber = code; WriteVersion(version); }
                Debug.Log("[BUILD] " + summary.result + ", " + summary.totalErrors + " errors, " + summary.totalWarnings + " warnings, "
                          + (summary.totalSize / (1024 * 1024)) + " MB, " + summary.totalTime.TotalSeconds.ToString("F0") + " s"
                          + (ok && !development ? "  | build code is now " + code : ""));
                if (!interactive) EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogError("[BUILD] failed: " + e);
                if (!interactive) EditorApplication.Exit(1);
            }
        }

        static void Bump(int part)   // 0 major, 1 minor, 2 patch
        {
            var v = ReadVersion();
            var bits = v.versionName.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).Concat(new[] { 0, 0, 0 }).Take(3).ToArray();
            bits[part]++; for (int i = part + 1; i < 3; i++) bits[i] = 0;
            v.versionName = string.Join(".", bits);
            WriteVersion(v);
            Debug.Log("[VERSION] versionName is now " + v.versionName + " (build code unchanged: " + v.buildNumber + ")");
        }

        static VersionFile ReadVersion()
        {
            try { if (File.Exists(VersionPath)) return JsonUtility.FromJson<VersionFile>(File.ReadAllText(VersionPath)) ?? new VersionFile(); }
            catch (Exception e) { Debug.LogWarning("[VERSION] could not read " + VersionPath + ": " + e.Message); }
            return new VersionFile();
        }

        static void WriteVersion(VersionFile v) => File.WriteAllText(VersionPath, JsonUtility.ToJson(v, true) + "\n");

        static string DefaultOutput(BuildTarget target, bool bundle)
        {
            switch (target)
            {
                case BuildTarget.Android: return Path.Combine(OutputDir, bundle ? "PalettePour.aab" : "PalettePour.apk");
                case BuildTarget.StandaloneWindows64: return Path.Combine(OutputDir, "Windows", "PalettePour.exe");
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
