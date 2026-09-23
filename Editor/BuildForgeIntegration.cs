// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Geurts.GameForge.Documentation
{
    /// <summary>Shares documentation-owned setup validation and installers with Build Forge without depending on God.</summary>
    public static class BuildForgeIntegration
    {
        /// <summary>Whether active work, compilation errors or missing required tools prevent setup writes.</summary>
        public static bool IsBusy => DocumentationUpdaterController.IsBusy || PackageSelfUpdater.instance.IsBusy ||
                                     PackageSelfUpdater.EditorBusy || DependencyInstallation.IsBusy || EditorUtility.scriptCompilationFailed ||
                                     !DocumentationDependencies.RequiredToolsAvailable;

        /// <summary>Loads and validates the approved Git ignore bytes from installed documentation; writes nothing.</summary>
        /// <param name="projectRoot">The absolute Unity project root.</param>
        /// <returns>The exact approved UTF-8 payload.</returns>
        public static byte[] LoadGitIgnore(string projectRoot) => GitIgnoreTemplateReader.Load(projectRoot);

        /// <summary>Loads the registered Codex guide and renders its exact project documentation entry point; writes nothing.</summary>
        /// <param name="projectRoot">The absolute Unity project root.</param>
        /// <returns>The rendered UTF-8 guide payload.</returns>
        public static byte[] LoadCodexGuide(string projectRoot) => CodexGuideInstaller.Load(projectRoot);

        /// <summary>Checks a user-selected guide against the installed template after validating its destination.</summary>
        /// <param name="projectRoot">The absolute Unity project root supplying the documentation.</param>
        /// <param name="target">The user-selected absolute AGENTS.md path, in an existing folder.</param>
        /// <returns>True only for matching bytes; false for no selection, a missing guide or different contents.</returns>
        public static bool IsCodexGuideInstalled(string projectRoot, string target)
        {
            if (string.IsNullOrWhiteSpace(target)) return false;
            target = CodexGuideInstaller.ValidateDestination(projectRoot, target);
            byte[] payload = LoadCodexGuide(projectRoot);
            return File.Exists(target) && File.ReadAllBytes(target).SequenceEqual(payload);
        }

        /// <summary>Checks the project-root Git ignore file against the validated payload without modifying it.</summary>
        /// <param name="projectRoot">The absolute Unity project root.</param>
        /// <returns>True only for an existing regular file with exactly matching bytes.</returns>
        public static bool IsGitIgnoreInstalled(string projectRoot)
        {
            GitIgnoreInstaller.ValidateDestination(projectRoot);
            byte[] payload = LoadGitIgnore(projectRoot);
            string target = Path.Combine(projectRoot, ".gitignore");
            return File.Exists(target) && File.ReadAllBytes(target).SequenceEqual(payload);
        }

        /// <summary>Installs only the selected Codex guide after the existing cancel-default confirmation.</summary>
        /// <param name="projectRoot">The absolute Unity project root supplying the documentation.</param>
        /// <param name="target">The AGENTS.md path explicitly selected through the caller's folder picker.</param>
        /// <returns>True after verified installation; false when no path is selected or the user cancels.</returns>
        public static bool InstallCodexGuide(string projectRoot, string target)
        {
            EnsureReady();
            return CodexGuideInstaller.Install(projectRoot, target, CodexGuideConfirmation.Confirm);
        }

        /// <summary>Creates a missing Git ignore file after confirmation, preserving every existing file unchanged.</summary>
        /// <param name="projectRoot">The absolute Unity project root.</param>
        /// <returns>True for verified or already identical bytes; false for cancellation or a preserved differing file.</returns>
        public static bool InstallGitIgnore(string projectRoot)
        {
            EnsureReady();
            return GitIgnoreInstaller.InstallWithConfirmation(projectRoot, GitIgnoreInstallConfirmation.Confirm);
        }

        private static void EnsureReady()
        {
            if (!DocumentationDependencies.RequiredToolsAvailable)
                throw new InvalidOperationException("Odin Inspector and Quantum Console are required before project setup.");
            if (IsBusy)
                throw new InvalidOperationException("Wait for documentation, package and Unity operations to finish, then retry setup.");
        }
    }
}
