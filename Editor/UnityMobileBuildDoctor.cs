using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UnityMobileBuildDoctor
{
    /// <summary>Read-only preflight checks for Unity Android and iOS player settings.</summary>
    public sealed class BuildDoctorReport
    {
        public readonly List<BuildDoctorFinding> Findings = new List<BuildDoctorFinding>();
        public int Errors { get; private set; }
        public int Warnings { get; private set; }

        public void Add(string severity, string code, string title, string detail)
        {
            Findings.Add(new BuildDoctorFinding(severity, code, title, detail));
            if (severity == "ERROR") Errors++;
            if (severity == "WARNING") Warnings++;
        }

        public string ToMarkdown()
        {
            var b = new StringBuilder();
            b.AppendLine("# Unity Mobile Build Doctor report");
            b.AppendLine();
            b.AppendLine("Generated: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));
            b.AppendLine("Unity: " + Application.unityVersion);
            b.AppendLine("Active target: " + EditorUserBuildSettings.activeBuildTarget);
            b.AppendLine("Errors: " + Errors + " | Warnings: " + Warnings + " | Checks: " + Findings.Count);
            b.AppendLine();
            b.AppendLine("Checks are advisory and do not replace testing or store-specific review.");
            b.AppendLine();
            foreach (var f in Findings)
            {
                b.AppendLine("## [" + f.Severity + "] " + f.Title + " (`" + f.Code + "`)");
                b.AppendLine();
                b.AppendLine(f.Detail);
                b.AppendLine();
            }
            return b.ToString();
        }
    }

    public sealed class BuildDoctorFinding
    {
        public readonly string Severity, Code, Title, Detail;
        public BuildDoctorFinding(string severity, string code, string title, string detail)
        { Severity = severity; Code = code; Title = title; Detail = detail; }
    }

    public static class BuildDoctorChecks
    {
        public static BuildDoctorReport Run()
        {
            var r = new BuildDoctorReport();
            CheckCommon(r);
            CheckAndroid(r);
            CheckIos(r);
            return r;
        }

        private static void CheckCommon(BuildDoctorReport r)
        {
            string id = PlayerSettings.applicationIdentifier;
            if (string.IsNullOrWhiteSpace(id) || id.Contains("DefaultCompany") || id.EndsWith(".game", StringComparison.OrdinalIgnoreCase))
                r.Add("ERROR", "APP-001", "Review the application identifier", "Current identifier: `" + Safe(id) + "`. Set a unique reverse-DNS identifier before publishing.");
            else r.Add("PASS", "APP-001", "Application identifier is set", "Current identifier: `" + id + "`.");

            if (PlayerSettings.bundleVersion == "0.1")
                r.Add("WARNING", "APP-002", "Review the bundle version", "The version is still Unity's common starter value `0.1`. Confirm it matches your release plan.");
            else r.Add("PASS", "APP-002", "Bundle version is set", "Current version: `" + PlayerSettings.bundleVersion + "`.");

            if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.AutoRotation &&
                !PlayerSettings.allowedAutorotateToPortrait && !PlayerSettings.allowedAutorotateToPortraitUpsideDown &&
                !PlayerSettings.allowedAutorotateToLandscapeLeft && !PlayerSettings.allowedAutorotateToLandscapeRight)
                r.Add("ERROR", "APP-003", "No autorotations are enabled", "Auto Rotation is selected, but every orientation is disabled. Enable at least one orientation or choose a fixed orientation.");
            else r.Add("PASS", "APP-003", "Orientation settings are internally consistent", "Selected orientation: `" + PlayerSettings.defaultInterfaceOrientation + "`.");
        }

        private static void CheckAndroid(BuildDoctorReport r)
        {
            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.Mono2x)
                r.Add("WARNING", "ANDROID-001", "Android uses the Mono scripting backend", "Confirm that Mono is intended for this distribution and supported by your target store and release setup.");
            else r.Add("PASS", "ANDROID-001", "Android scripting backend is configured", "Backend: `" + PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) + "`.");

            int minSdk = (int)PlayerSettings.Android.minSdkVersion;
            int targetSdk = (int)PlayerSettings.Android.targetSdkVersion;
            if (targetSdk != 0 && minSdk > targetSdk)
                r.Add("ERROR", "ANDROID-002", "Android minimum SDK exceeds target SDK", "Minimum SDK: `" + minSdk + "`; target SDK: `" + targetSdk + "`. Review these values.");
            else r.Add("PASS", "ANDROID-002", "Android SDK settings are ordered", "Minimum SDK: `" + minSdk + "`; target SDK setting: `" + PlayerSettings.Android.targetSdkVersion + "`. Check current store requirements before release.");

            if (PlayerSettings.Android.useCustomKeystore)
                r.Add("PASS", "ANDROID-003", "A custom Android keystore is selected", "Unity is configured to use a custom keystore. The tool does not read or export passwords or signing keys.");
            else r.Add("WARNING", "ANDROID-003", "Android custom keystore is not selected", "For production, confirm the intended signing workflow and preserve secure backups of the upload key. This check cannot verify key ownership.");

            if (PlayerSettings.Android.targetArchitectures == AndroidArchitecture.None)
                r.Add("ERROR", "ANDROID-004", "No Android CPU architecture is selected", "Select at least one architecture required by your devices and distribution channel.");
            else r.Add("PASS", "ANDROID-004", "Android CPU architectures are selected", "Selected: `" + PlayerSettings.Android.targetArchitectures + "`.");
        }

        private static void CheckIos(BuildDoctorReport r)
        {
            string id = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
            if (string.IsNullOrWhiteSpace(id) || id.Contains("DefaultCompany"))
                r.Add("WARNING", "IOS-001", "Review the iOS bundle identifier", "Current iOS identifier: `" + Safe(id) + "`. Confirm it matches the App Store Connect app record.");
            else r.Add("PASS", "IOS-001", "iOS bundle identifier is set", "Current identifier: `" + id + "`.");

            r.Add("INFO", "IOS-002", "Signing and capabilities need a device build review", "This editor preflight cannot verify Apple certificates, provisioning profiles, entitlements, privacy declarations, or App Store Connect configuration. Validate them in Xcode and App Store Connect.");
        }

        private static string Safe(string value) { return string.IsNullOrWhiteSpace(value) ? "(empty)" : value; }
    }

    public sealed class UnityMobileBuildDoctorWindow : EditorWindow
    {
        private BuildDoctorReport report;
        private Vector2 scroll;

        [MenuItem("Tools/Build Doctor/Mobile Build Doctor")]
        public static void Open() { GetWindow<UnityMobileBuildDoctorWindow>("Mobile Build Doctor"); }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Unity Mobile Build Doctor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Read-only preflight checks for common Android and iOS release settings. Review findings; this tool does not change project settings.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run checks", GUILayout.Height(28))) report = BuildDoctorChecks.Run();
                using (new EditorGUI.DisabledScope(report == null))
                    if (GUILayout.Button("Export Markdown report", GUILayout.Height(28))) Export();
            }
            if (report == null) return;
            EditorGUILayout.LabelField("Errors: " + report.Errors + "   Warnings: " + report.Warnings + "   Checks: " + report.Findings.Count);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var f in report.Findings)
            {
                MessageType type = f.Severity == "ERROR" ? MessageType.Error : f.Severity == "WARNING" ? MessageType.Warning : MessageType.Info;
                EditorGUILayout.HelpBox(f.Severity + " · " + f.Title + "\n" + f.Detail, type);
            }
            EditorGUILayout.EndScrollView();
        }

        private void Export()
        {
            string path = EditorUtility.SaveFilePanel("Save build report", "", "unity-mobile-build-doctor-report.md", "md");
            if (string.IsNullOrEmpty(path)) return;
            File.WriteAllText(path, report.ToMarkdown(), new UTF8Encoding(false));
            EditorUtility.RevealInFinder(path);
        }
    }
}
