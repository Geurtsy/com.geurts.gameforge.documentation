// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Geurts.GameForge.Documentation
{
    internal static class CodexGuideInstaller
    {
        internal const string ActionLabel = "Install Codex guide";
        internal const string TechniquePath = "GeurtsTechniques/GeurtsAgentTechnique.md";
        internal const string EntryPath = "AI_READ_FIRST.md";
        internal const string EntryPlaceholder = "{{GEURTS_DOCUMENTATION_ENTRY_POINT}}";
        private const string MenuPath = "Tools/Geurts Game Forge/" + ActionLabel;
        private static readonly UTF8Encoding _utf8 = new UTF8Encoding(false, true);

        [MenuItem(MenuPath, false, 102)]
        internal static void ChooseAndInstall()
        {
            if (!CanInstall()) return;
            string projectRoot = DocumentationPackageConstants.GetProjectRootFromAssetsPath(Application.dataPath);
            string folder = EditorUtility.OpenFolderPanel("Choose where to install AGENTS.md", projectRoot, "");
            if (string.IsNullOrEmpty(folder)) return;
            string target = Path.Combine(folder, "AGENTS.md");
            try
            {
                if (!Install(projectRoot, target, CodexGuideConfirmation.Confirm)) return;
                string message = "Installed the Codex guide at:\n" + target + "\n\nDocumentation entry point:\n" +
                                 GetEntryPoint(projectRoot) + "\n\n" + DiscoveryNotice();
                Debug.Log("[Geurts Documentation] " + message);
                EditorUtility.DisplayDialog("Codex Guide Installed", message, "Close");
            }
            catch (Exception exception)
            {
                Debug.LogError("[Geurts Documentation] Codex guide installation failed: " + exception.Message);
                EditorUtility.DisplayDialog("Codex Guide Installation Failed", exception.Message, "Close");
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool CanInstall()
        {
            return DocumentationDependencies.OdinInstalled && !DocumentationUpdaterController.IsBusy &&
                   !PackageSelfUpdater.instance.IsBusy && !PackageSelfUpdater.EditorBusy;
        }

        internal static bool Install(string projectRoot, string target, Func<string, bool> confirm)
        {
            if (string.IsNullOrWhiteSpace(target)) return false;
            target = Path.GetFullPath(target);
            string name = Path.GetFileName(target);
            if (name != "AGENTS.md")
                throw new InvalidDataException("The Codex guide must be named AGENTS.md.");
            string folder = Path.GetDirectoryName(target);
            if (!Directory.Exists(folder)) throw new DirectoryNotFoundException("Choose an existing destination folder.");
            string fullRoot = Path.GetFullPath(projectRoot);
            string documentation = Path.Combine(fullRoot, DocumentationPackageConstants.ManagedDocumentationDirectory);
            if (IsWithin(target, documentation) || IsWithin(target, Path.Combine(fullRoot, "Docs", "GameDesign")))
                throw new InvalidDataException("Choose a location outside the managed documentation and Docs/GameDesign folders.");
            ValidateTarget(target);
            // Prepare from the installed, manifest-selected technique before asking to overwrite anything.
            byte[] payload = Load(fullRoot);
            string message = "Create the Codex guide at:\n" + target + "\n\nIt will direct the AI to:\n" +
                             GetEntryPoint(fullRoot) + "\n\n" + DiscoveryNotice() +
                             "\n\nWARNING: If this guide already exists, installation will overwrite all of its contents. " +
                             "Local changes will be lost. No backup is created.";
            if (!confirm(message)) return false;
            ValidateTarget(target);
            // Replace the directory entry rather than writing through a possible hard link.
            File.Delete(target);
            using (FileStream stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write))
                stream.Write(payload, 0, payload.Length);
            if (!File.ReadAllBytes(target).SequenceEqual(payload))
                throw new IOException("The installed guide could not be verified. Run Install Codex guide again.");
            return true;
        }

        internal static string GetEntryPoint(string projectRoot)
        {
            return Path.GetFullPath(Path.Combine(projectRoot,
                DocumentationPackageConstants.ManagedDocumentationDirectory, EntryPath)).Replace('\\', '/');
        }

        internal static byte[] Load(string projectRoot)
        {
            string entry = Read(projectRoot, EntryPath);
            string manifest = Read(projectRoot, "GeurtsTechniqueManifest.md");
            string technique = Read(projectRoot, TechniquePath);
            if (string.IsNullOrWhiteSpace(entry)) throw new InvalidDataException("The documentation entry point is empty.");
            Match registryBlock = Regex.Match(manifest,
                @"(?s)<!-- GEURTS-PACKAGE-FILES:BEGIN -->(?<body>.*?)<!-- GEURTS-PACKAGE-FILES:END -->");
            MatchCollection registry = Regex.Matches(registryBlock.Groups["body"].Value,
                @"(?m)^\|[ \t]*`" + Regex.Escape(TechniquePath) + @"`[ \t]*\|[ \t]*1\.0\.0[ \t]*\|");
            MatchCollection templates = Regex.Matches(technique,
                @"(?m)^<!-- GEURTS-CODEX-GUIDE-BEGIN version=""1\.0\.0"" -->\n```markdown\n(?<body>[\s\S]*?)^```\n<!-- GEURTS-CODEX-GUIDE-END -->$");
            if (registry.Count != 1 || templates.Count != 1 ||
                Regex.Matches(technique, "GEURTS-CODEX-GUIDE-BEGIN").Count != 1 ||
                Regex.Matches(technique, "GEURTS-CODEX-GUIDE-END").Count != 1 ||
                !Regex.IsMatch(technique, @"(?m)^\*\*Version:\*\* 1\.0\.0$"))
                throw new InvalidDataException("The installed documentation must contain the registered Codex guide technique v1.0.0. Update documentation and try again.");
            string body = templates[0].Groups["body"].Value;
            if (Regex.Matches(body, Regex.Escape(EntryPlaceholder)).Count != 1)
                throw new InvalidDataException("The Codex guide template must contain exactly one documentation entry point placeholder.");
            string entryPoint = GetEntryPoint(projectRoot);
            if (entryPoint.IndexOfAny(new[] { '\r', '\n', '`' }) >= 0)
                throw new InvalidDataException("The project path cannot be represented safely in the guide.");
            return _utf8.GetBytes(body.Replace(EntryPlaceholder, entryPoint));
        }

        private static string Read(string projectRoot, string relativePath)
        {
            string relative = DocumentationPackageConstants.ManagedDocumentationDirectory + "/" + relativePath;
            DocumentationFileOperations.EnsureManagedTargetIsRegular(projectRoot, relative, false);
            string path = DocumentationFileOperations.GetSafeFullPath(projectRoot, relative);
            if (!File.Exists(path)) throw new FileNotFoundException("Update Geurts Game Forge Documentation first. Missing " + relative);
            return _utf8.GetString(File.ReadAllBytes(path)).TrimStart('\uFEFF').Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private static bool IsWithin(string path, string root)
        {
            return path.StartsWith(root.TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateTarget(string target)
        {
            DocumentationFileOperations.EnsureManagedTargetIsRegular(Path.GetPathRoot(target),
                target.Substring(Path.GetPathRoot(target).Length).Replace('\\', '/'), false);
        }

        private static string DiscoveryNotice()
        {
            return "Start a new Codex task in this folder or a descendant within the same project to load the guide automatically.";
        }
    }
}
