// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Geurts.GameForge.Documentation
{
    internal static class DocumentationUpdaterController
    {
        private static readonly GitHubDocumentationTransport Transport = new GitHubDocumentationTransport();
        internal static DocumentationUpdateService Service { get; set; } = new DocumentationUpdateService(
            DocumentationPackageConstants.GetProjectRootFromAssetsPath(Application.dataPath),
            Transport);

        private static bool _installing;
        internal static UpdateStatus Status { get; } = new UpdateStatus();

        internal static event Action Changed;

        internal static bool IsInstalling => _installing;
        internal static bool IsBusy => _installing || Status.IsChecking;
        internal static DocumentationAvailability Availability => Status.Availability;
        internal static string StatusMessage => Status.Message;
        internal static string RemoteCommit => Status.RemoteCommit;
        internal static string InstalledCommit => Status.InstalledCommit;

        internal static async Task CheckForUpdatesAsync(bool showWindowWhenAvailable)
        {
            if (IsBusy || PackageSelfUpdater.instance.IsInstalling)
            {
                return;
            }

            Status.InstalledVersion = Service.ReadInstalledVersion();
            Status.BeginCheck();
            NotifyChanged();

            try
            {
                DocumentationCheckResult result = await Service.CheckForUpdateAsync(CancellationToken.None, ReportProgress);
                Status.InstalledVersion = result.InstalledVersion;
                Status.InstalledCommit = result.InstalledCommit;
                Status.CompleteCheck(result.AvailableVersion, result.RemoteCommit, true);
                Status.Availability = result.Availability;
                Status.Message = BuildCheckMessage(result);
            }
            catch (Exception exception)
            {
                Status.FailCheck("Documentation check failed: " + exception.Message);
            }
            finally
            {
                NotifyChanged();
            }

            if (showWindowWhenAvailable && Availability == DocumentationAvailability.UpdateAvailable)
            {
                DocumentationUpdaterWindow.ShowAfterCheck();
            }
        }

        internal static void ConfirmAndUpdate()
        {
            if (!CanStartUpdate()) return;
            if (DocumentationUpdateConfirmation.Confirm())
                EditorApplication.delayCall += BeginConfirmedUpdate;
        }

        internal static async void BeginConfirmedUpdate()
        {
            if (!CanStartUpdate()) return;

            _installing = true;
            Status.Failed = false;
            ReportProgress(new UpdateProgress("Preparing the documentation update..."));

            PreparedDocumentationUpdate prepared = null;
            try
            {
                prepared = await Service.PrepareLatestAsync(CancellationToken.None, ReportProgress);
                ReportProgress(new UpdateProgress("Replacing and verifying the five confirmed managed targets..."));
                await Task.Yield();

                DocumentationApplyResult applyResult = Service.Apply(prepared);
                Status.Availability = applyResult.CommitPersisted
                    ? DocumentationAvailability.Current
                    : DocumentationAvailability.Unknown;
                Status.RemoteCommit = prepared.Commit;
                Status.InstalledCommit = applyResult.CommitPersisted ? prepared.Commit : null;
                Status.InstalledVersion = prepared.Contract.PackageVersion;
                Status.AvailableVersion = prepared.Contract.PackageVersion;
                Status.Message = applyResult.CommitPersisted
                    ? "Geurts Game Forge documentation and AI routes are current."
                    : applyResult.Warning;
                if (applyResult.CommitPersisted)
                {
                    Debug.Log("[Geurts Documentation] Update completed at commit " + prepared.Commit + ".");
                }
                else
                {
                    Debug.LogWarning("[Geurts Documentation] " + applyResult.Warning);
                    EditorUtility.DisplayDialog(
                        "Geurts Documentation Updated With Warning",
                        applyResult.Warning + "\n\nThe content update succeeded and will not be undone.",
                        "Close");
                }
            }
            catch (Exception exception)
            {
                Status.Availability = DocumentationAvailability.Unknown;
                Status.Failed = true;
                Status.InstalledVersion = Service.ReadInstalledVersion();
                Status.Message = "Update failed: " + exception.Message;
                Debug.LogError("[Geurts Documentation] " + Status.Message);
                EditorUtility.DisplayDialog(
                    "Geurts Documentation Update Failed",
                    Status.Message +
                    "\n\nNo successful installed-commit value was written. Failures before replacement leave the existing managed targets unchanged.",
                    "Close");
            }
            finally
            {
                prepared?.Dispose();
                _installing = false;
                Status.Progress = null;
                NotifyChanged();
            }
        }

        private static void ReportProgress(UpdateProgress progress)
        {
            Status.Progress = progress;
            Status.Message = progress.Message;
            NotifyChanged();
        }

        internal static string BuildCheckMessage(DocumentationCheckResult result)
        {
            if (result.Availability == DocumentationAvailability.Current)
                return "Up to date with the checked Git revision. Local file contents have not been verified.";
            if (result.InstalledVersion == "Not installed")
                return "Documentation is not installed. Use Update to install the available Git version.";
            if (string.IsNullOrEmpty(result.InstalledCommit))
                return "The installed Git revision is not recorded, so its update status cannot be verified. " +
                       "A confirmed update will install Git's current content and record its revision.";
            if (result.InstalledVersion == result.AvailableVersion)
                return "Git has a different revision with the same version number (" + result.AvailableVersion + "). " +
                       "Update to install those changes; the version number may stay the same.";
            return "Documentation update available. Install the latest Git revision using the button below.";
        }

        private static bool CanStartUpdate()
        {
            string reason = !DocumentationDependencies.OdinInstalled ? "Odin Inspector is required."
                : IsBusy || PackageSelfUpdater.instance.IsBusy ? "Another documentation or package operation is still running."
                : PackageSelfUpdater.EditorBusy ? "Unity is compiling, importing assets, or in Play mode." : null;
            if (reason == null) return true;
            // Do not replace progress from an active operation, but never silently discard an accepted action.
            string message = "Documentation update could not start: " + reason + " Try Update again when Unity is ready.";
            if (!IsBusy)
            {
                Status.Message = message;
                Status.Failed = true;
                NotifyChanged();
            }
            Debug.LogWarning("[Geurts Documentation] " + message);
            return false;
        }

        private static void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
