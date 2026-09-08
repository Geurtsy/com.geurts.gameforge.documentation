// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;

namespace Geurts.GameForge.Documentation
{
    internal sealed class UpdateProgress
    {
        internal UpdateProgress(string message, float fraction = -1f)
        {
            Message = message;
            Fraction = fraction;
        }

        internal string Message { get; }
        // A negative fraction means the operation has no measurable total.
        internal float Fraction { get; }
    }

    internal sealed class UpdateStatus
    {
        internal DocumentationAvailability Availability { get; set; }
        internal string InstalledVersion { get; set; } = "Unknown";
        internal string AvailableVersion { get; set; } = "Not checked";
        internal string InstalledCommit { get; set; }
        internal string RemoteCommit { get; set; }
        internal string Message { get; set; } = "Open the window or choose Check for updates to refresh Git versions.";
        internal string LastChecked { get; private set; } = "Not checked yet";
        internal bool IsChecking { get; private set; }
        internal bool Failed { get; set; }
        internal UpdateProgress Progress { get; set; }

        internal void BeginCheck()
        {
            IsChecking = true;
            Failed = false;
            Availability = DocumentationAvailability.Unknown;
            AvailableVersion = "Checking...";
            RemoteCommit = null;
            Message = "Connecting to Git and resolving the selected revision...";
            Progress = new UpdateProgress(Message);
        }

        internal void CompleteCheck(string version, string commit, bool canCompare)
        {
            AvailableVersion = version;
            RemoteCommit = commit;
            Availability = Compare(InstalledCommit, commit, canCompare);
            Message = Availability == DocumentationAvailability.Current
                ? "Up to date with the checked Git revision."
                : canCompare
                    ? "Update available. Git differs from the installed revision, or no successful install is recorded."
                    : "Git version retrieved. This installation has no Git revision to compare.";
            FinishCheck();
        }

        internal void FailCheck(string message)
        {
            AvailableVersion = "Unavailable";
            RemoteCommit = null;
            Availability = DocumentationAvailability.Unknown;
            Failed = true;
            Message = message + " Use Check for updates to retry.";
            FinishCheck();
        }

        internal static DocumentationAvailability Compare(string installed, string remote, bool canCompare)
        {
            if (!canCompare || string.IsNullOrEmpty(remote))
                return DocumentationAvailability.Unknown;
            return string.Equals(installed, remote, StringComparison.OrdinalIgnoreCase)
                ? DocumentationAvailability.Current : DocumentationAvailability.UpdateAvailable;
        }

        private void FinishCheck()
        {
            IsChecking = false;
            Progress = null;
            LastChecked = DateTime.Now.ToString("HH:mm:ss, d MMM yyyy");
        }
    }
}
