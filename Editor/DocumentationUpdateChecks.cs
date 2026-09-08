// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.Threading.Tasks;

namespace Geurts.GameForge.Documentation
{
    internal sealed class UpdateCheckCoordinator
    {
        private readonly Func<Task> _package;
        private readonly Func<Task> _documentation;
        private Task _active;

        internal UpdateCheckCoordinator(Func<Task> package, Func<Task> documentation)
        { _package = package; _documentation = documentation; }

        internal Task CheckAsync()
        {
            if (_active != null && !_active.IsCompleted) return _active;
            // Both controllers contain their own error handling; one failed source does not hide the other.
            return _active = Task.WhenAll(_package(), _documentation());
        }
    }

    internal static class DocumentationUpdateChecks
    {
        private static readonly UpdateCheckCoordinator Coordinator = new UpdateCheckCoordinator(
            () => PackageSelfUpdater.instance.CheckForUpdatesAsync(),
            () => DocumentationUpdaterController.CheckForUpdatesAsync(false));

        internal static Task CheckAllAsync()
        {
            if (PackageSelfUpdater.instance.IsInstalling || DocumentationUpdaterController.IsInstalling)
                return Task.CompletedTask;
            return Coordinator.CheckAsync();
        }
    }
}
