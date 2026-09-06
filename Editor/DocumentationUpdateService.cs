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
            CancellationToken cancellationToken)
        {
            string remoteCommit = await transport.ResolveHeadCommitAsync(cancellationToken);
            string installedCommit = installer.ReadLastSuccessfulCommit();
            DocumentationAvailability availability =
                string.Equals(remoteCommit, installedCommit, StringComparison.OrdinalIgnoreCase)
                    ? DocumentationAvailability.Current
                    : DocumentationAvailability.UpdateAvailable;

            return new DocumentationCheckResult(availability, remoteCommit, installedCommit);
        }

        internal async Task<PreparedDocumentationUpdate> PrepareLatestAsync(
            CancellationToken cancellationToken)
        {
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
                    cancellationToken);
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
