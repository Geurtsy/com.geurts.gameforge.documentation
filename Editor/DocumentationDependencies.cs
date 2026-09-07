// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

namespace Geurts.GameForge.Documentation
{
    /// <summary>Required dashboard dependency status shared by the UI and action entry points.</summary>
    internal static class DocumentationDependencies
    {
        internal const string OdinGuideUrl = "https://odininspector.com/tutorials/getting-started/installing-odin-inspector";
        internal static bool OdinInstalled
        {
            get
            {
#if ODIN_INSPECTOR
                return true;
#else
                return false;
#endif
            }
        }

        internal static string OdinStatus => OdinInstalled ? "Odin Inspector — Installed (required)" : "Odin Inspector — Missing (required)";
        internal static string OdinDescription => OdinInstalled
            ? "Odin Inspector improves UI quality with clearer groups, styled controls, and the enhanced documentation dashboard."
            : "Odin Inspector is required to use the documentation tools. It improves UI quality with clearer groups and styled controls. " +
              "Import your licensed copy into this Unity project, then reopen this window. Odin is installed separately and is not bundled with this package.";
    }
}
