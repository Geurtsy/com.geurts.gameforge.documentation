// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.
using QFSW.QC;
using UnityEngine.Scripting;

namespace Geurts.GameForge.Documentation
{
    /// <summary>Read-only editor status exposed through the required Quantum Console integration.</summary>
    internal static class DocumentationConsoleCommands
    {
        [Command("GeurtsGameForge.Documentation.Status", "Shows current documentation and package update status in the Unity Editor."), Preserve]
        internal static string ReadStatus()
        {
            return "Geurts Game Forge Documentation\nDocumentation: " + DocumentationUpdaterController.StatusMessage +
                   "\nPackage: " + PackageSelfUpdater.instance.StatusMessage;
        }
    }
}
