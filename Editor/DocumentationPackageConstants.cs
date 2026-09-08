// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.Collections.Generic;
using System.IO;

namespace Geurts.GameForge.Documentation
{
    internal static class DocumentationPackageConstants
    {
        internal const string PackageName = "com.geurts.gameforge.documentation";
        internal const string DisplayName = "Geurts Game Forge Documentation";
        internal const string RepositoryUrl = "https://github.com/Geurtsy/GeurtsGameForge_Documentation.git";
        internal const string RepositoryBranch = "main";
        internal const string HeadCommitApiUrl = "https://api.github.com/repos/Geurtsy/GeurtsGameForge_Documentation/git/ref/heads/main";
        internal const string ArchiveUrlFormat = "https://codeload.github.com/Geurtsy/GeurtsGameForge_Documentation/zip/{0}";
        internal const string ContractRelativePath = "GeurtsTechniques/GeurtsDocumentationCompanionContract.json";
        internal const string ManagedDocumentationDirectory = "GeurtsGameForgeDocumentation";
        internal const string ExpectedSchemaVersion = "2.0.0";
        internal const string UpdateActionLabel = "Update Geurts Game Forge Documentation";
        internal const int MetadataLimitBytes = 128 * 1024;
        internal const long ArchiveLimitBytes = 64L * 1024L * 1024L;
        internal const long ExtractedLimitBytes = 128L * 1024L * 1024L;

        internal static readonly IReadOnlyList<ManagedAiRoute> ExpectedManagedAiRoutes =
            Array.AsReadOnly(new[]
            {
                new ManagedAiRoute(
                    "Tools/AIAgentInstructionTemplates/copilot-instructions.md",
                    ".github/copilot-instructions.md"),
                new ManagedAiRoute(
                    "Tools/AIAgentInstructionTemplates/instructions/geurts-unity.instructions.md",
                    ".github/instructions/geurts-unity.instructions.md"),
                new ManagedAiRoute(
                    "Tools/AIAgentInstructionTemplates/instructions/geurts-game-design.instructions.md",
                    ".github/instructions/geurts-game-design.instructions.md")
            });

        internal static readonly IReadOnlyList<string> RequiredRoutingEntries =
            Array.AsReadOnly(new[]
            {
                "AI_READ_FIRST.md",
                CodexGuideInstaller.TechniquePath,
                "GeurtsTechniqueManifest.md"
            });

        internal static string GetProjectRootFromAssetsPath(string assetsPath)
        {
            if (string.IsNullOrWhiteSpace(assetsPath))
            {
                throw new ArgumentException("The Unity Assets path is unavailable.", nameof(assetsPath));
            }

            DirectoryInfo parent = Directory.GetParent(Path.GetFullPath(assetsPath));
            if (parent == null)
            {
                throw new InvalidOperationException("The Unity project root could not be resolved.");
            }

            return parent.FullName;
        }

        internal static string BuildConfirmationMessage()
        {
            return
                "This update will directly replace the managed documentation folder and these system-managed project AI instruction files:\n\n" +
                "- GeurtsGameForgeDocumentation/\n" +
                "- .github/copilot-instructions.md\n" +
                "- .github/instructions/geurts-unity.instructions.md\n" +
                "- .github/instructions/geurts-game-design.instructions.md\n\n" +
                "Existing content at those paths will be overwritten and lost. There is no backup or rollback. " +
                "Docs/GameDesign/ and every unlisted project path will not be accessed or changed.";
        }
    }
}
