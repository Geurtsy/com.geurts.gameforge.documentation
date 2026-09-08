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
        private static readonly DocumentationUpdateService Service = new DocumentationUpdateService(
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
                Status.Message = result.Availability == DocumentationAvailability.Current
                    ? "Up to date with the checked Git revision. Local file contents have not been verified."
                    : "Documentation update available. Install the latest Git revision using the button below.";
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
            if (!DocumentationDependencies.OdinInstalled || IsBusy || PackageSelfUpdater.instance.IsBusy || PackageSelfUpdater.EditorBusy)
            {
                return;
            }

            DocumentationUpdateConfirmation.Open();
        }

        internal static async void BeginConfirmedUpdate()
        {
            if (!DocumentationDependencies.OdinInstalled || IsBusy || PackageSelfUpdater.instance.IsBusy || PackageSelfUpdater.EditorBusy)
            {
                return;
            }

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

        private static void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
