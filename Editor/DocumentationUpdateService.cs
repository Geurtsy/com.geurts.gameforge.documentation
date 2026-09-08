// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Geurts.GameForge.Documentation
{
    internal sealed class DocumentationUpdateService
    {
        private readonly string projectRoot;
        private readonly IDocumentationTransport transport;
        private readonly DocumentationInstaller installer;

        internal DocumentationUpdateService(
            string projectRoot,
            IDocumentationTransport transport,
            IProjectInstallCommitStore installCommitStore = null)
        {
            if (projectRoot == null)
            {
                throw new ArgumentNullException(nameof(projectRoot));
            }

            this.projectRoot = Path.GetFullPath(projectRoot);
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            installer = new DocumentationInstaller(this.projectRoot, installCommitStore);
        }

        internal async Task<DocumentationCheckResult> CheckForUpdateAsync(
            CancellationToken cancellationToken, Action<UpdateProgress> progress = null)
        {
            string installedVersion = ReadInstalledVersion();
            progress?.Invoke(new UpdateProgress("Resolving the latest documentation commit on main..."));
            string remoteCommit = await transport.ResolveHeadCommitAsync(cancellationToken);
            progress?.Invoke(new UpdateProgress("Reading the documentation version at Git commit " + remoteCommit.Substring(0, 8) + "..."));
            string remoteVersion = await transport.ReadVersionAsync(remoteCommit, cancellationToken);
            string installedCommit = installer.ReadLastSuccessfulCommit();
            DocumentationAvailability availability =
                GitVersionMetadata.IsVersion(installedVersion) &&
                string.Equals(installedVersion, remoteVersion, StringComparison.Ordinal) &&
                string.Equals(remoteCommit, installedCommit, StringComparison.OrdinalIgnoreCase)
                    ? DocumentationAvailability.Current
                    : DocumentationAvailability.UpdateAvailable;

            return new DocumentationCheckResult(availability, remoteCommit, installedCommit, installedVersion, remoteVersion);
        }

        internal string ReadInstalledVersion() => GitVersionMetadata.ReadInstalledDocumentationVersion(projectRoot);

        internal async Task<PreparedDocumentationUpdate> PrepareLatestAsync(
            CancellationToken cancellationToken, Action<UpdateProgress> progress = null)
        {
            progress?.Invoke(new UpdateProgress("Resolving the latest documentation revision for installation..."));
            string commit = await transport.ResolveHeadCommitAsync(cancellationToken);
            string workingDirectory = Path.Combine(
                Path.GetTempPath(),
                "GGFDocs-" + Guid.NewGuid().ToString("N"));

            DocumentationDownload download = null;
            try
            {
                download = await transport.DownloadCommitAsync(
                    commit,
                    workingDirectory,
                    cancellationToken, progress);
                progress?.Invoke(new UpdateProgress("Validating the documentation contract and required files..."));
                DocumentationCompanionContract contract = DocumentationContractReader.LoadAndValidate(
                    download.CandidateRoot);
                return new PreparedDocumentationUpdate(commit, download, contract);
            }
            catch
            {
                DocumentationFileOperations.DeleteDirectoryBestEffort(
                    download != null ? download.WorkingDirectory : workingDirectory);
                throw;
            }
        }

        internal DocumentationApplyResult Apply(PreparedDocumentationUpdate prepared)
        {
            return installer.Apply(prepared);
        }
    }
}
