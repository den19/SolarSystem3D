using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SolarSystem.Editor
{
    /// <summary>
    /// Release Android APK into the project root as <c>com.den.kolesov.solar.system.v8.apk</c>.
    /// Bumps Version patch + Bundle Version Code, signs with densappstudio keystore.
    /// Menu: SolarSystem → Build Release APK (project root). Batch: BuildFromBatch.
    /// Invoke only when asked («собери APK»).
    /// </summary>
    public static class AndroidApkBuilder
    {
        public const string ApkFileName = "com.den.kolesov.solar.system.v8.apk";
        public const string ReleaseKeystorePath = @"C:/git/cloud/den.kolesov..keystore";
        public const string ReleaseKeyAlias = "main";

        static readonly string[] ScenePaths =
        {
            "Assets/_Scenes/MainMenu.unity",
            "Assets/_Scenes/Level1.unity"
        };

        const string KeystoreLocalRelative = "Tools/keystore.local.ps1";

        [MenuItem("SolarSystem/Build Release APK (project root)")]
        public static void BuildFromMenu()
        {
            var ok = BuildApk(exitEditorOnFinish: false);
            if (ok)
                EditorUtility.DisplayDialog(
                    "Build Release APK",
                    $"Version {PlayerSettings.bundleVersion} (code {PlayerSettings.Android.bundleVersionCode})\n\nAPK:\n{GetApkOutputPath()}",
                    "OK");
            else
                EditorUtility.DisplayDialog(
                    "Build Release APK",
                    "Build failed. See the Console for details.\n\n" +
                    "Need Tools/keystore.local.ps1 with $KeystorePassword (see .example).",
                    "OK");
        }

        /// <summary>
        /// Headless entry for Tools/build_apk.ps1 (-executeMethod).
        /// </summary>
        public static void BuildFromBatch()
        {
            var ok = BuildApk(exitEditorOnFinish: true);
            if (!ok && !Application.isBatchMode)
                Debug.LogError("Android release APK build failed.");
        }

        public static string GetApkOutputPath()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                              ?? Directory.GetCurrentDirectory();
            return Path.GetFullPath(Path.Combine(projectRoot, ApkFileName));
        }

        public static bool BuildApk(bool exitEditorOnFinish)
        {
            var outputPath = GetApkOutputPath();
            var exitCode = 0;
            var ok = false;

            try
            {
                if (!ConfigureReleaseSigning())
                {
                    exitCode = 1;
                    return false;
                }

                BumpVersionForRelease();
                EnsureBuildScenes();
                EnsureAndroidTarget();
                EditorUserBuildSettings.buildAppBundle = false;

                var scenes = EditorBuildSettings.scenes
                    .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                    .Select(s => s.path)
                    .ToArray();
                if (scenes.Length == 0)
                {
                    Debug.LogError("AndroidApkBuilder: no enabled scenes in Build Settings.");
                    exitCode = 1;
                    return false;
                }

                Debug.Log(
                    $"AndroidApkBuilder: release build → {outputPath} " +
                    $"(version {PlayerSettings.bundleVersion}, code {PlayerSettings.Android.bundleVersionCode})");

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None
                };

                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                ok = summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;

                if (ok)
                {
                    Debug.Log(
                        $"AndroidApkBuilder: succeeded in {summary.totalTime}. " +
                        $"Size≈{summary.totalSize} bytes. Output: {outputPath}");
                    return true;
                }

                Debug.LogError(
                    $"AndroidApkBuilder: failed ({summary.result}). " +
                    $"Errors={summary.totalErrors} Warnings={summary.totalWarnings}");
                exitCode = 1;
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"AndroidApkBuilder: exception — {ex}");
                exitCode = 1;
                ok = false;
                return false;
            }
            finally
            {
                if (exitEditorOnFinish && Application.isBatchMode)
                    EditorApplication.Exit(ok ? 0 : exitCode);
            }
        }

        /// <summary>
        /// Increments the last numeric segment after the last dot (8.2.6 → 8.2.7),
        /// increments Android bundleVersionCode, and writes the new version into
        /// TitleLabel of every Assets/Resources/Languages/*.json file.
        /// </summary>
        public static void BumpVersionForRelease()
        {
            var current = PlayerSettings.bundleVersion ?? "1.0.0";
            var bumped = IncrementLastVersionSegment(current, out _);
            var prevCode = PlayerSettings.Android.bundleVersionCode;
            var nextCode = prevCode < int.MaxValue ? prevCode + 1 : prevCode;

            PlayerSettings.bundleVersion = bumped;
            PlayerSettings.Android.bundleVersionCode = nextCode;
            SyncVersionInLocalizationFiles(bumped);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"AndroidApkBuilder: version {current} → {bumped}, " +
                $"bundleVersionCode {prevCode} → {nextCode}");
        }

        /// <summary>
        /// Replaces <c>v.X.Y.Z</c> inside each language file's TitleLabel with the new version.
        /// </summary>
        public static void SyncVersionInLocalizationFiles(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return;

            var languagesDir = Path.Combine(Application.dataPath, "Resources", "Languages");
            if (!Directory.Exists(languagesDir))
            {
                Debug.LogWarning($"AndroidApkBuilder: languages dir not found: {languagesDir}");
                return;
            }

            var pattern = new Regex(
                @"(""TitleLabel""\s*:\s*"")([^""]*?)(v\.)(\d+(?:\.\d+)*)([^""]*"")",
                RegexOptions.CultureInvariant);

            var updatedCount = 0;
            foreach (var path in Directory.GetFiles(languagesDir, "*.json"))
            {
                var text = File.ReadAllText(path);
                var next = pattern.Replace(
                    text,
                    m => m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value +
                         version + m.Groups[5].Value,
                    1);

                if (next == text)
                {
                    Debug.LogWarning(
                        $"AndroidApkBuilder: TitleLabel version not found in {Path.GetFileName(path)}");
                    continue;
                }

                File.WriteAllText(path, next);
                var relative = "Assets" + path.Substring(Application.dataPath.Length).Replace('\\', '/');
                AssetDatabase.ImportAsset(relative);
                updatedCount++;
            }

            Debug.Log(
                $"AndroidApkBuilder: wrote version v.{version} into TitleLabel of {updatedCount} localization file(s).");
        }

        public static string IncrementLastVersionSegment(string version, out int lastSegment)
        {
            lastSegment = 1;
            if (string.IsNullOrWhiteSpace(version))
            {
                lastSegment = 1;
                return "1.0.1";
            }

            var trimmed = version.Trim();
            var dot = trimmed.LastIndexOf('.');
            string prefix;
            string last;
            if (dot < 0)
            {
                prefix = "";
                last = trimmed;
            }
            else
            {
                prefix = trimmed.Substring(0, dot + 1);
                last = trimmed.Substring(dot + 1);
            }

            if (!int.TryParse(last, out var n) || n < 0)
            {
                lastSegment = 1;
                return string.IsNullOrEmpty(prefix) ? "1" : prefix + "1";
            }

            lastSegment = n + 1;
            return prefix + lastSegment.ToString();
        }

        static bool ConfigureReleaseSigning()
        {
            var password = ResolveKeystorePassword();
            if (string.IsNullOrEmpty(password))
            {
                Debug.LogError(
                    "AndroidApkBuilder: release signing password missing. " +
                    "Create Tools/keystore.local.ps1 from Tools/keystore.local.ps1.example " +
                    "(set $KeystorePassword), or set ANDROID_KEYSTORE_PASS. " +
                    "Debug signing is disabled.");
                return false;
            }

            if (!File.Exists(ReleaseKeystorePath))
            {
                Debug.LogError(
                    $"AndroidApkBuilder: keystore not found at {ReleaseKeystorePath}");
                return false;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = ReleaseKeystorePath;
            PlayerSettings.Android.keyaliasName = ReleaseKeyAlias;
            PlayerSettings.Android.keystorePass = password;
            PlayerSettings.Android.keyaliasPass = password;
            return true;
        }

        static string ResolveKeystorePassword()
        {
            var fromEnv = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
            if (!string.IsNullOrEmpty(fromEnv))
                return fromEnv;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
                return null;

            var localPath = Path.Combine(projectRoot, KeystoreLocalRelative);
            if (!File.Exists(localPath))
                return null;

            // Parse: $KeystorePassword = "..." or '...'
            var text = File.ReadAllText(localPath);
            var match = Regex.Match(
                text,
                @"\$KeystorePassword\s*=\s*(?:'([^']*)'|""([^""]*)"")",
                RegexOptions.CultureInvariant);
            if (!match.Success)
                return null;

            var value = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            return string.IsNullOrWhiteSpace(value) || value == "YOUR_PASSWORD" ? null : value;
        }

        static void EnsureBuildScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes != null &&
                ScenePaths.All(path => scenes.Any(s => s.enabled && s.path == path)))
                return;

            EditorBuildSettings.scenes = ScenePaths
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
            Debug.Log(
                $"AndroidApkBuilder: registered build scenes: {string.Join(", ", ScenePaths)}");
        }

        static void EnsureAndroidTarget()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                return;

            var switched = EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Android,
                BuildTarget.Android);
            if (!switched)
                throw new InvalidOperationException(
                    "Failed to switch active build target to Android. " +
                    "Install Android Build Support in Unity Hub.");
        }
    }
}
