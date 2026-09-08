// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Geurts.GameForge.Documentation
{
    /// <summary>Refreshes this package's configured Git reference through Unity without opening Package Manager.</summary>
    [InitializeOnLoad]
    internal sealed class PackageSelfUpdater : ScriptableSingleton<PackageSelfUpdater>
    {
        internal const string ActionLabel = "Update package from Git";

        // Unity serializes AddRequest's operation handle so polling can resume after assembly reload.
        // No FilePath or Save call: this state lives only for the current Editor session.
        [SerializeField, Tooltip("The active Unity package update request, retained across script reloads.")]
        private AddRequest _request;
        [SerializeField, Tooltip("Whether this Editor session is waiting for its package update.")]
        private bool _pending;
        [SerializeField, Tooltip("The Git commit installed before the active update.")]
        private string _previousHash;
        [SerializeField, Tooltip("The last package update result shown in the dashboard.")]
        private string _message = "Ready to refresh the package from its configured Git source.";
        [SerializeField, Tooltip("Whether the last package update failed.")]
        private bool _failed;
        [SerializeField, Tooltip("Whether this session has a package installation result to display.")]
        private bool _hasUpdateResult;

        private PackageInfo _installed;
        private UpdateStatus _status;

        internal static event Action Changed;
        internal UpdateStatus Status => _status ?? (_status = new UpdateStatus { InstalledVersion = InstalledVersion });
        internal bool IsInstalling => _pending;
        internal bool IsBusy => _pending || Status.IsChecking;
        internal string StatusMessage => _message;
        internal bool Failed => _failed;
        internal bool HasUpdateResult => _hasUpdateResult;
        internal PackageInfo Installed => _installed ?? (_installed = PackageInfo.FindForAssembly(typeof(PackageSelfUpdater).Assembly));
        internal string InstalledVersion => Installed?.version ?? "Unknown";
        internal string GitReference => GetGitReference(Installed?.name, Installed?.source ?? PackageSource.Unknown,
            Installed != null && Installed.isDirectDependency, Installed?.packageId);
        internal bool CanUpdate => DocumentationDependencies.OdinInstalled && !EditorBusy &&
                                   !DocumentationUpdaterController.IsBusy && !IsBusy && GitReference != null;
        internal static bool EditorBusy => EditorApplication.isCompiling || EditorApplication.isUpdating ||
                                           EditorApplication.isPlayingOrWillChangePlaymode;

        internal string SourceDescription => GitReference ??
            "This is a local, embedded, or indirect installation. Update its source checkout or install this package directly from Git to enable this button.";

        static PackageSelfUpdater()
        {
            EditorApplication.delayCall += () => instance.Resume();
            UnityEditor.PackageManager.Events.registeredPackages += _ =>
            {
                instance._installed = null;
                Changed?.Invoke();
            };
        }

        internal async Task CheckForUpdatesAsync()
        {
            if (IsBusy || DocumentationUpdaterController.IsInstalling) return;
            Status.InstalledVersion = InstalledVersion;
            Status.InstalledCommit = Installed?.git?.hash;
            Status.BeginCheck();
            Changed?.Invoke();
            try
            {
                // A local checkout can display the official release version, but has no installed Git hash.
                string reference = Installed?.source == PackageSource.Git
                    ? GetGitReference(Installed.name, Installed.source, true, Installed.packageId)
                    : GitVersionMetadata.OfficialPackageUrl;
                if (reference == null) throw new InvalidOperationException("The installed Git source could not be identified.");
                using (var metadata = new GitVersionMetadata())
                {
                    GitPackageVersion result = await metadata.ReadPackageAsync(reference, CancellationToken.None,
                        progress => { Status.Progress = progress; Status.Message = progress.Message; Changed?.Invoke(); });
                    Status.CompleteCheck(result.Version, result.Commit, Installed?.source == PackageSource.Git);
                }
            }
            catch (Exception exception)
            {
                Status.FailCheck("Package check failed: " + exception.Message);
            }
            finally { Changed?.Invoke(); }
        }

        internal void BeginUpdate()
        {
            if (!CanUpdate)
            {
                return;
            }

            string reference = GitReference;
            _previousHash = Installed.git?.hash;
            _failed = false;
            _hasUpdateResult = false;
            Status.Failed = false;
            _message = "Updating the editor package from Git. Unity may recompile scripts...";
            _pending = true;
            Status.Message = _message;
            Status.Progress = new UpdateProgress("Unity is resolving, downloading and installing the package. Script reloads may briefly interrupt this display.");
            Changed?.Invoke();
            try
            {
                // Re-adding the same Git URL asks Unity to resolve its latest commit and update its lock file.
                // Preserve the selected branch/tag/commit and repository; never replace a local checkout.
                _request = Client.Add(reference);
                Resume();
            }
            catch (Exception exception)
            {
                Finish("Package update failed: " + exception.Message, true);
            }
        }

        private void Resume()
        {
            EditorApplication.update -= Poll;
            if (_pending)
            {
                Status.Progress = new UpdateProgress("Waiting for Unity to finish resolving and installing the package. Scripts may reload...");
                EditorApplication.update += Poll;
            }
        }

        private void Poll()
        {
            try
            {
                if (_request == null)
                {
                    Finish("Package update status was lost. Try Update package from Git again.", true);
                    return;
                }
                if (!_request.IsCompleted)
                {
                    return;
                }
                if (_request.Status == StatusCode.Failure)
                {
                    Finish("Package update failed: " + (_request.Error?.message ?? "Unity returned no error details."), true);
                    return;
                }

                PackageInfo result = _request.Result;
                if (result == null || result.name != DocumentationPackageConstants.PackageName)
                {
                    Finish("Unity did not return the expected documentation package. Check the project Console and try again.", true);
                    return;
                }
                _installed = result;
                Finish(BuildSuccessMessage(_previousHash, result.git?.hash, result.version), false);
            }
            catch (Exception exception)
            {
                Finish("Package update failed: " + exception.Message, true);
            }
        }

        private void Finish(string message, bool failed)
        {
            EditorApplication.update -= Poll;
            _pending = false;
            _request = null;
            _message = message;
            _failed = failed;
            _hasUpdateResult = true;
            Status.Failed = failed;
            Status.InstalledVersion = InstalledVersion;
            Status.InstalledCommit = Installed?.git?.hash;
            Status.Progress = null;
            Status.Message = message;
            Status.Availability = failed ? DocumentationAvailability.Unknown :
                UpdateStatus.Compare(Status.InstalledCommit, Status.RemoteCommit, Installed?.source == PackageSource.Git);
            Changed?.Invoke();
            if (!failed) RefreshAfterInstall();
        }

        private async void RefreshAfterInstall()
        {
            await DocumentationUpdateChecks.CheckAllAsync();
        }

        // PackageInfo.packageId retains the configured Git URL, including any path and revision selector.
        internal static string GetGitReference(string name, PackageSource source, bool direct, string packageId)
        {
            string prefix = DocumentationPackageConstants.PackageName + "@";
            if (name != DocumentationPackageConstants.PackageName || source != PackageSource.Git || !direct ||
                string.IsNullOrWhiteSpace(packageId) || !packageId.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }
            string reference = packageId.Substring(prefix.Length);
            return string.IsNullOrWhiteSpace(reference) ? null : reference;
        }

        internal static string BuildSuccessMessage(string previousHash, string installedHash, string version)
        {
            return !string.IsNullOrWhiteSpace(previousHash) &&
                   string.Equals(previousHash, installedHash, StringComparison.OrdinalIgnoreCase)
                ? "Package already matches the configured Git source (version " + version + ")."
                : "Package update completed (version " + version + ").";
        }
    }
}
