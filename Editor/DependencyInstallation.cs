// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.IO;
using UnityEditor;

namespace Geurts.GameForge.Documentation
{
    /// <summary>User-initiated access to Unity's licensed asset download and interactive import flows.</summary>
    [InitializeOnLoad]
    internal static class DependencyInstallation
    {
        internal const string MyAssetsMenu = "Window/Package Management/My Assets";
        internal static Func<string, bool> ExecuteMenu = EditorApplication.ExecuteMenuItem;
        internal static Func<string, string, string, string> SelectFile = EditorUtility.OpenFilePanel;
        internal static Action<string, bool> ImportFile = AssetDatabase.ImportPackage;
        internal static event Action Changed;

        static DependencyInstallation()
        {
            AssetDatabase.importPackageStarted += name => ReportImport(name, "Unity is importing the selected assets. Follow its import progress; required assemblies are checked after compilation.", false, false);
            AssetDatabase.importPackageCompleted += name => ReportImport(name, "Unity finished importing the selected assets. Readiness refreshes after script compilation.", false, true);
            AssetDatabase.importPackageCancelled += name => ReportImport(name, "Import cancelled. The status above reflects the tools currently available in this project.", false, true);
            AssetDatabase.importPackageFailed += (name, error) => ReportImport(name, "Unity import failed: " + error, true, true);
        }

        internal static bool IsBusy => EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || DocumentationUpdaterController.IsBusy ||
            PackageSelfUpdater.instance.IsBusy;

        internal static string Message(string tool) => SessionState.GetString(Key(tool), string.Empty);
        internal static bool Failed(string tool) => SessionState.GetBool(Key(tool) + ".failed", false);
        private static string Key(string tool) => "Geurts.Documentation.Dependency." + tool;

        internal static void OpenOwnedAssets(string tool)
        {
            if (!CanStart(tool)) return;
            try
            {
                if (!ExecuteMenu(MyAssetsMenu))
                {
                    SetResult(tool, "Unity could not open My Assets. Use Window > Package Management > My Assets, then search for " + tool + ".", true);
                    return;
                }
                SetResult(tool, "My Assets opened. Sign in to the Unity account that owns " + tool +
                    ", search for it, then choose Download and Import. Download progress and ownership are handled by Unity in My Assets.", false);
            }
            catch (Exception exception) { SetResult(tool, "Could not open My Assets: " + exception.Message, true); }
        }

        internal static void ImportLicensedCopy(string tool)
        {
            if (!CanStart(tool)) return;
            try
            {
                string path = SelectFile("Select your licensed " + tool + " package", string.Empty, "unitypackage");
                if (string.IsNullOrEmpty(path)) return;
                if (!File.Exists(path) || !string.Equals(Path.GetExtension(path), ".unitypackage", StringComparison.OrdinalIgnoreCase))
                {
                    SetResult(tool, "Select an existing .unitypackage file downloaded from your licensed source.", true);
                    return;
                }
                SetResult(tool, "Opening Unity's import review for " + Path.GetFileName(path) +
                    ". Confirm the contents belong to " + tool + ", then choose Import. Reimporting can replace existing tool files. Unity shows import progress; readiness refreshes after scripts compile.", false);
                // Keep Unity's review and file selection visible; never silently import or infer ownership.
                SessionState.SetString("Geurts.Documentation.Dependency.ImportName", Path.GetFileNameWithoutExtension(path));
                SessionState.SetString("Geurts.Documentation.Dependency.ImportTool", tool);
                ImportFile(path, true);
            }
            catch (Exception exception) { SetResult(tool, "Could not open the package import: " + exception.Message, true); }
        }

        private static bool CanStart(string tool)
        {
            if (tool != "Odin Inspector" && tool != "Quantum Console") return false;
            if (!IsBusy) return true;
            SetResult(tool, "Wait for Unity to finish its current operation and leave Play mode, then try again.", false);
            return false;
        }

        internal static void SetResult(string tool, string message, bool failed)
        {
            SessionState.SetString(Key(tool), message);
            SessionState.SetBool(Key(tool) + ".failed", failed);
            Changed?.Invoke();
        }

        internal static void ReportImport(string name, string message, bool failed, bool finished)
        {
            string expected = SessionState.GetString("Geurts.Documentation.Dependency.ImportName", string.Empty);
            string tool = string.Equals(name, expected, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(expected)
                ? SessionState.GetString("Geurts.Documentation.Dependency.ImportTool", string.Empty) : string.Empty;
            // Imports started in My Assets can also report their known vendor package name.
            string normalized = (name ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
            if (string.IsNullOrEmpty(tool))
            {
                if (normalized.StartsWith("OdinInspector", StringComparison.OrdinalIgnoreCase)) tool = "Odin Inspector";
                else if (normalized.StartsWith("QuantumConsole", StringComparison.OrdinalIgnoreCase)) tool = "Quantum Console";
                else return;
            }
            SetResult(tool, message, failed);
            if (finished && string.Equals(name, expected, StringComparison.OrdinalIgnoreCase))
            {
                SessionState.EraseString("Geurts.Documentation.Dependency.ImportName");
                SessionState.EraseString("Geurts.Documentation.Dependency.ImportTool");
            }
        }
    }
}
