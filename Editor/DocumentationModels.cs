using System;
using System.Collections.Generic;

namespace Geurts.GameForge.Documentation
{
    internal sealed class ManagedAiRoute : IEquatable<ManagedAiRoute>
    {
        internal ManagedAiRoute(string source, string destination)
        {
            Source = source;
            Destination = destination;
        }

        internal string Source { get; }
        internal string Destination { get; }

        public bool Equals(ManagedAiRoute other)
        {
            return other != null &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                   string.Equals(Destination, other.Destination, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ManagedAiRoute);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Source != null ? Source.GetHashCode() : 0) * 397) ^
                       (Destination != null ? Destination.GetHashCode() : 0);
            }
        }
    }

    internal sealed class DocumentationCompanionContract
    {
        internal DocumentationCompanionContract(
            string schemaVersion,
            string packageVersion,
            string repository,
            string branch,
            string managedDocumentationDestination,
            IReadOnlyList<string> requiredCandidateEntries,
            IReadOnlyList<ManagedAiRoute> managedAiRoutes)
        {
            SchemaVersion = schemaVersion;
            PackageVersion = packageVersion;
            Repository = repository;
            Branch = branch;
            ManagedDocumentationDestination = managedDocumentationDestination;
            RequiredCandidateEntries = requiredCandidateEntries;
            ManagedAiRoutes = managedAiRoutes;
        }

        internal string SchemaVersion { get; }
        internal string PackageVersion { get; }
        internal string Repository { get; }
        internal string Branch { get; }
        internal string ManagedDocumentationDestination { get; }
        internal IReadOnlyList<string> RequiredCandidateEntries { get; }
        internal IReadOnlyList<ManagedAiRoute> ManagedAiRoutes { get; }
    }

    internal sealed class DocumentationDownload
    {
        internal DocumentationDownload(string workingDirectory, string candidateRoot)
        {
            WorkingDirectory = workingDirectory;
            CandidateRoot = candidateRoot;
        }

        internal string WorkingDirectory { get; }
        internal string CandidateRoot { get; }
    }

    internal sealed class PreparedDocumentationUpdate : IDisposable
    {
        private bool disposed;

        internal PreparedDocumentationUpdate(
            string commit,
            DocumentationDownload download,
            DocumentationCompanionContract contract)
        {
            Commit = commit;
            Download = download;
            Contract = contract;
        }

        internal string Commit { get; }
        internal DocumentationDownload Download { get; }
        internal DocumentationCompanionContract Contract { get; }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            DocumentationFileOperations.DeleteDirectoryBestEffort(Download.WorkingDirectory);
        }
    }

    internal sealed class DocumentationApplyResult
    {
        internal DocumentationApplyResult(bool commitPersisted, string warning)
        {
            CommitPersisted = commitPersisted;
            Warning = warning;
        }

        internal bool CommitPersisted { get; }
        internal string Warning { get; }
    }

    internal enum DocumentationAvailability
    {
        Unknown,
        NotInstalled,
        Current,
        UpdateAvailable
    }

    internal sealed class DocumentationCheckResult
    {
        internal DocumentationCheckResult(
            DocumentationAvailability availability,
            string remoteCommit,
            string installedCommit)
        {
            Availability = availability;
            RemoteCommit = remoteCommit;
            InstalledCommit = installedCommit;
        }

        internal DocumentationAvailability Availability { get; }
        internal string RemoteCommit { get; }
        internal string InstalledCommit { get; }
    }

}
