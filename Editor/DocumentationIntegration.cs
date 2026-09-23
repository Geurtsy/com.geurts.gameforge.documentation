// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;

namespace Geurts.GameForge.Documentation
{
    /// <summary>Optional public Editor integration that reuses the companion's single documentation update lifecycle.</summary>
    public static class DocumentationIntegration
    {
        private static readonly List<Func<string>> _operationGuards = new List<Func<string>>();
        internal static string ExternalOperationUnavailableReason
        {
            get
            {
                foreach (var guard in _operationGuards)
                {
                    string reason = guard();
                    if (!string.IsNullOrEmpty(reason)) return reason;
                }
                return null;
            }
        }
        /// <summary>Whether the companion is checking or changing documentation, its package, or required tools.</summary>
        public static bool IsBusy => DocumentationUpdaterController.IsBusy || PackageSelfUpdater.instance.IsBusy || DependencyInstallation.IsImporting;
        /// <summary>Current documentation action outcome or progress description.</summary>
        public static string StatusMessage => DocumentationUpdaterController.StatusMessage;
        /// <summary>Last observed installed content version; Unknown is not an integrity assertion.</summary>
        public static string InstalledVersion => DocumentationUpdaterController.Status.InstalledVersion;
        /// <summary>Version returned by the last content check, or an explicit unknown/unavailable label.</summary>
        public static string AvailableVersion => DocumentationUpdaterController.Status.AvailableVersion;
        /// <summary>Current, UpdateAvailable or Unknown, based on the recorded successful commit and remote metadata.</summary>
        public static string Availability => DocumentationUpdaterController.Availability.ToString();
        /// <summary>Whether the last content operation failed.</summary>
        public static bool Failed => DocumentationUpdaterController.Status.Failed;
        /// <summary>Actionable explanation while a content operation cannot start, otherwise null.</summary>
        public static string ActionUnavailableReason => ExternalOperationUnavailableReason ?? (IsBusy ? "Wait for the Documentation operation to finish." :
            !DocumentationDependencies.RequiredToolsAvailable ? "Install the required Odin Inspector and Quantum Console tools first." :
            EditorUtility.scriptCompilationFailed ? "Fix Unity script errors before updating documentation." :
            PackageSelfUpdater.EditorBusy ? "Exit Play Mode and wait for Unity compilation or asset imports to finish." : null);
        /// <summary>Measured progress from zero to one, or null when the total is unknown.</summary>
        public static float? Progress => DocumentationUpdaterController.Status.Progress != null &&
            DocumentationUpdaterController.Status.Progress.Fraction >= 0 ? DocumentationUpdaterController.Status.Progress.Fraction : (float?)null;

        /// <summary>Raised for content, package and required-tool state changes. Callers must unsubscribe when closed.</summary>
        public static event Action Changed
        {
            add { DocumentationUpdaterController.Changed += value; PackageSelfUpdater.Changed += value; DependencyInstallation.Changed += value; }
            remove { DocumentationUpdaterController.Changed -= value; PackageSelfUpdater.Changed -= value; DependencyInstallation.Changed -= value; }
        }

        /// <summary>Checks content metadata through the existing controller without opening another window or replacing files.</summary>
        /// <returns>A task that completes after the check reports its outcome.</returns>
        public static Task CheckForUpdatesAsync() => DocumentationUpdaterController.CheckForUpdatesAsync(false);

        /// <summary>Shows the existing cancel-default confirmation; acquisition and replacement require acceptance.</summary>
        public static void UpdateDocumentation() => DocumentationUpdaterController.ConfirmAndUpdate();

        /// <summary>Opens the existing companion window without introducing another updater.</summary>
        public static void OpenWindow() => DocumentationUpdaterWindow.ShowWindow();

        /// <summary>Registers an optional host operation guard without a dependency on that host package.</summary>
        /// <param name="unavailableReason">Returns an actionable reason while a conflicting operation is active, otherwise null.</param>
        /// <returns>A token that the host must dispose when its integration is unloaded.</returns>
        public static IDisposable RegisterOperationGuard(Func<string> unavailableReason)
        {
            if (unavailableReason == null) throw new ArgumentNullException(nameof(unavailableReason));
            _operationGuards.Add(unavailableReason);
            return new OperationGuard(unavailableReason);
        }

        private sealed class OperationGuard : IDisposable
        {
            private Func<string> _guard;
            internal OperationGuard(Func<string> guard) { _guard = guard; }
            /// <summary>Removes this optional host's operation guard.</summary>
            public void Dispose()
            {
                if (_guard == null) return;
                _operationGuards.Remove(_guard);
                _guard = null;
            }
        }
    }
}
