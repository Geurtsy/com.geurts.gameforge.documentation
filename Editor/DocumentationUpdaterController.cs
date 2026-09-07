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

        private static bool isBusy;
        private static DocumentationAvailability availability = DocumentationAvailability.Unknown;
        private static string statusMessage = "Update status has not been checked.";
        private static string remoteCommit;
        private static string installedCommit;

        internal static event Action Changed;

        internal static bool IsBusy => isBusy;
        internal static DocumentationAvailability Availability => availability;
        internal static string StatusMessage => statusMessage;
        internal static string RemoteCommit => remoteCommit;
        internal static string InstalledCommit => installedCommit;

        internal static async Task CheckForUpdatesAsync(bool showWindowWhenAvailable)
        {
            if (isBusy || PackageSelfUpdater.instance.IsBusy)
            {
                return;
            }

            isBusy = true;
            statusMessage = "Checking the authoritative documentation source...";
            NotifyChanged();

            try
            {
                DocumentationCheckResult result = await Service.CheckForUpdateAsync(CancellationToken.None);
                availability = result.Availability;
                remoteCommit = result.RemoteCommit;
                installedCommit = result.InstalledCommit;
                statusMessage = availability == DocumentationAvailability.Current
                    ? "The authoritative source has not advanced since the last successful install. Local files were not inspected."
                    : "A Geurts Game Forge documentation update is available.";
            }
            catch (Exception exception)
            {
                availability = DocumentationAvailability.Unknown;
                statusMessage = "The documentation source could not be checked: " + exception.Message;
                Debug.LogWarning("[Geurts Documentation] " + statusMessage);
            }
            finally
            {
                isBusy = false;
                NotifyChanged();
            }

            if (showWindowWhenAvailable && availability == DocumentationAvailability.UpdateAvailable)
            {
                DocumentationUpdaterWindow.ShowWindow();
            }
        }

        internal static void ConfirmAndUpdate()
        {
            if (!DocumentationDependencies.OdinInstalled || isBusy || PackageSelfUpdater.instance.IsBusy || PackageSelfUpdater.EditorBusy)
            {
                return;
            }

            DocumentationUpdateConfirmation.Open();
        }

        internal static async void BeginConfirmedUpdate()
        {
            if (!DocumentationDependencies.OdinInstalled || isBusy || PackageSelfUpdater.instance.IsBusy || PackageSelfUpdater.EditorBusy)
            {
                return;
            }

            isBusy = true;
            statusMessage = "Downloading and validating the selected documentation commit...";
            NotifyChanged();

            PreparedDocumentationUpdate prepared = null;
            try
            {
                prepared = await Service.PrepareLatestAsync(CancellationToken.None);
                statusMessage = "Replacing the five confirmed managed targets...";
                NotifyChanged();

                DocumentationApplyResult applyResult = Service.Apply(prepared);
                availability = applyResult.CommitPersisted
                    ? DocumentationAvailability.Current
                    : DocumentationAvailability.Unknown;
                remoteCommit = prepared.Commit;
                installedCommit = applyResult.CommitPersisted ? prepared.Commit : null;
                statusMessage = applyResult.CommitPersisted
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
                availability = DocumentationAvailability.Unknown;
                statusMessage = "Update failed: " + exception.Message;
                Debug.LogError("[Geurts Documentation] " + statusMessage);
                EditorUtility.DisplayDialog(
                    "Geurts Documentation Update Failed",
                    statusMessage +
                    "\n\nNo successful installed-commit value was written. Failures before replacement leave the existing managed targets unchanged.",
                    "Close");
            }
            finally
            {
                prepared?.Dispose();
                isBusy = false;
                NotifyChanged();
            }
        }

        private static void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
