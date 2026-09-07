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
        internal const string MenuPath = "Tools/Geurts Game Forge/" + ActionLabel;

        [MenuItem(MenuPath, false, 101)]
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

        [MenuItem(MenuPath, true)]
        private static bool CanInstall()
        {
            return DocumentationDependencies.OdinInstalled && !DocumentationUpdaterController.IsBusy && !PackageSelfUpdater.instance.IsBusy &&
                   !PackageSelfUpdater.EditorBusy;
        }

        internal static bool InstallWithConfirmation(string projectRoot, Func<string, bool> confirm)
        {
            string fullRoot = Path.GetFullPath(projectRoot);
            string target = Path.Combine(fullRoot, ".gitignore");
            string message = "This installs the .gitignore template from GeurtsGameForgeDocumentation at:\n\n" +
                             target + "\n\n" +
                             "WARNING: Your existing .gitignore will be overwritten in full. " +
                             "All custom rules in that file will be lost. No backup is created.\n\n" +
                             "If the file does not exist, it will be created.";
            if (!confirm(message))
            {
                return false;
            }

            // Load and validate the documentation-owned payload before touching the destination.
            byte[] payload = GitIgnoreTemplateReader.Load(fullRoot);
            DocumentationFileOperations.EnsureManagedTargetIsRegular(fullRoot, ".gitignore", false);

            // Replace the directory entry so a hard-linked destination cannot overwrite another file.
            File.Delete(target);
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
    }
}
