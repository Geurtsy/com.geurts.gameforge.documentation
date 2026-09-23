// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Geurts.GameForge.Documentation
{
    internal static class GitIgnoreInstaller
    {
        internal const string ActionLabel = "Install Geurts .gitignore";
        internal static void ConfirmAndInstall()
        {
            if (!CanInstall())
            {
                return;
            }

            try
            {
                string projectRoot = DocumentationPackageConstants.GetProjectRootFromAssetsPath(Application.dataPath);
                if (!InstallWithConfirmation(projectRoot, GitIgnoreInstallConfirmation.Confirm))
                {
                    if (File.Exists(Path.Combine(projectRoot, ".gitignore")))
                        EditorUtility.DisplayDialog("Geurts .gitignore Preserved",
                            "Your existing .gitignore has different rules and was preserved. Review it before continuing project setup.", "Close");
                    return;
                }

                string message = "Installed the GeurtsGameForgeDocumentation template at:\n" +
                                 Path.Combine(projectRoot, ".gitignore");
                Debug.Log("[Geurts Documentation] " + message);
                EditorUtility.DisplayDialog("Geurts .gitignore Installed", message, "Close");
            }
            catch (Exception exception)
            {
                Debug.LogError("[Geurts Documentation] .gitignore installation failed: " + exception.Message);
                EditorUtility.DisplayDialog("Geurts .gitignore Installation Failed", exception.Message, "Close");
            }
        }

        private static bool CanInstall()
        {
            return DocumentationDependencies.OdinInstalled && !DocumentationUpdaterController.IsBusy && !PackageSelfUpdater.instance.IsBusy &&
                   !PackageSelfUpdater.EditorBusy;
        }

        internal static bool InstallWithConfirmation(string projectRoot, Func<string, bool> confirm)
        {
            string fullRoot = Path.GetFullPath(projectRoot);
            string target = Path.Combine(fullRoot, ".gitignore");
            ValidateDestination(fullRoot);
            if (File.Exists(target))
                return File.ReadAllBytes(target).SequenceEqual(GitIgnoreTemplateReader.Load(fullRoot));
            string message = "This installs the .gitignore template from GeurtsGameForgeDocumentation at:\n\n" +
                             target + "\n\n" +
                             "The file is created only when missing. Existing ignore rules are preserved unchanged. " +
                             "Git tracking state will not be changed.";
            if (!confirm(message))
            {
                return false;
            }

            // Load and validate the documentation-owned payload before touching the destination.
            byte[] payload = GitIgnoreTemplateReader.Load(fullRoot);
            ValidateDestination(fullRoot);

            // CreateNew preserves a file introduced after the confirmation, including hard links.
            using (FileStream stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write))
            {
                stream.Write(payload, 0, payload.Length);
            }

            if (!File.ReadAllBytes(target).SequenceEqual(payload))
            {
                throw new IOException("The installed .gitignore could not be verified. Run the installer again.");
            }

            return true;
        }

        internal static void ValidateDestination(string projectRoot)
        {
            string target = Path.GetFullPath(Path.Combine(projectRoot, ".gitignore"));
            string volume = Path.GetPathRoot(target);
            // Validate every ancestor, including the project root, before inspecting or creating the file.
            DocumentationFileOperations.EnsureManagedTargetIsRegular(volume,
                target.Substring(volume.Length).Replace('\\', '/'), false);
        }
    }
}
