// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.IO;

namespace Geurts.GameForge.Documentation
{
    internal sealed class DocumentationInstaller
    {
        private readonly string projectRoot;
        private readonly IProjectInstallCommitStore installCommitStore;

        internal DocumentationInstaller(
            string projectRoot,
            IProjectInstallCommitStore installCommitStore = null)
        {
            if (projectRoot == null)
            {
                throw new ArgumentNullException(nameof(projectRoot));
            }

            this.projectRoot = Path.GetFullPath(projectRoot);
            if (!Directory.Exists(this.projectRoot))
            {
                throw new DirectoryNotFoundException("The Unity project root does not exist: " + this.projectRoot);
            }

            this.installCommitStore = installCommitStore ?? new ProjectInstallCommitStore(this.projectRoot);
        }

        internal DocumentationApplyResult Apply(PreparedDocumentationUpdate prepared)
        {
            if (prepared == null)
            {
                throw new ArgumentNullException(nameof(prepared));
            }

            DocumentationCompanionContract currentContract = DocumentationContractReader.LoadAndValidate(
                prepared.Download.CandidateRoot);
            ValidatePreparedContract(prepared.Contract, currentContract);
            PreflightTargets(currentContract);

            string documentationTarget = DocumentationFileOperations.GetSafeFullPath(
                projectRoot,
                currentContract.ManagedDocumentationDestination);
            if (Directory.Exists(documentationTarget))
            {
                DocumentationFileOperations.EnsureRegularTree(documentationTarget);
                DocumentationFileOperations.DeleteDirectory(documentationTarget);
            }

            DocumentationFileOperations.CopyDirectory(
                prepared.Download.CandidateRoot,
                documentationTarget);

            foreach (ManagedAiRoute route in currentContract.ManagedAiRoutes)
            {
                string source = DocumentationFileOperations.GetSafeFullPath(documentationTarget, route.Source);
                string destination = DocumentationFileOperations.GetSafeFullPath(projectRoot, route.Destination);
                string parent = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                DocumentationFileOperations.ClearReadOnlyFile(destination);
                File.Delete(destination);
                File.Copy(source, destination, false);
            }

            VerifyInstalledTargets(documentationTarget, currentContract);
            try
            {
                installCommitStore.Write(prepared.Commit);
                return new DocumentationApplyResult(true, null);
            }
            catch (Exception exception)
            {
                return new DocumentationApplyResult(
                    false,
                    "The four managed targets updated successfully, but the per-project commit signal could not be saved: " +
                    exception.Message + " A later Unity open may offer the same update again.");
            }
        }

        internal string ReadLastSuccessfulCommit()
        {
            return installCommitStore.Read();
        }

        private void PreflightTargets(DocumentationCompanionContract contract)
        {
            DocumentationFileOperations.EnsureManagedTargetIsRegular(
                projectRoot,
                contract.ManagedDocumentationDestination,
                true);

            foreach (ManagedAiRoute route in contract.ManagedAiRoutes)
            {
                DocumentationFileOperations.EnsureManagedTargetIsRegular(
                    projectRoot,
                    route.Destination,
                    false);
            }
        }

        private void VerifyInstalledTargets(
            string documentationTarget,
            DocumentationCompanionContract expectedContract)
        {
            DocumentationCompanionContract installedContract = DocumentationContractReader.LoadAndValidate(
                documentationTarget);
            ValidatePreparedContract(expectedContract, installedContract);

            foreach (ManagedAiRoute route in installedContract.ManagedAiRoutes)
            {
                string source = DocumentationFileOperations.GetSafeFullPath(documentationTarget, route.Source);
                string destination = DocumentationFileOperations.GetSafeFullPath(projectRoot, route.Destination);
                if (!DocumentationFileOperations.FilesEqual(source, destination))
                {
                    throw new IOException("A managed AI route did not verify after replacement: " + route.Destination);
                }
            }
        }

        private static void ValidatePreparedContract(
            DocumentationCompanionContract expected,
            DocumentationCompanionContract actual)
        {
            if (!string.Equals(expected.SchemaVersion, actual.SchemaVersion, StringComparison.Ordinal) ||
                !string.Equals(expected.PackageVersion, actual.PackageVersion, StringComparison.Ordinal) ||
                !string.Equals(expected.Repository, actual.Repository, StringComparison.Ordinal) ||
                !string.Equals(expected.Branch, actual.Branch, StringComparison.Ordinal) ||
                !string.Equals(
                    expected.ManagedDocumentationDestination,
                    actual.ManagedDocumentationDestination,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException("The prepared documentation contract changed before installation.");
            }

            if (expected.ManagedAiRoutes.Count != actual.ManagedAiRoutes.Count)
            {
                throw new InvalidDataException("The prepared AI route list changed before installation.");
            }

            for (int index = 0; index < expected.ManagedAiRoutes.Count; index++)
            {
                if (!expected.ManagedAiRoutes[index].Equals(actual.ManagedAiRoutes[index]))
                {
                    throw new InvalidDataException("The prepared AI route list changed before installation.");
                }
            }
        }

    }
}
