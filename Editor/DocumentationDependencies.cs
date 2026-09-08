// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Geurts.GameForge.Documentation
{
    /// <summary>Required dashboard dependency status shared by the UI and action entry points.</summary>
    internal static class DocumentationDependencies
    {
        internal static readonly System.Collections.Generic.IReadOnlyList<string> RequiredExternalTools =
            System.Array.AsReadOnly(new[] { "Odin Inspector", "Quantum Console" });

        // Unity reloads these checks with the assemblies after a tool is imported or removed.
        private static readonly bool _odinAvailable = HasType("Sirenix.OdinInspector.Editor", "Sirenix.OdinInspector.Editor.OdinEditorWindow") &&
            HasType("Sirenix.OdinInspector.Editor", "Sirenix.OdinInspector.Editor.PropertyTree");
        private static readonly bool _quantumAvailable = HasType("QFSW.QC", "QFSW.QC.QuantumConsole") &&
            HasType("QFSW.QC", "QFSW.QC.QuantumConsoleProcessor");

        internal static (string Message, Color Background) ToolStatus(string tool)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return ("Checking — Unity is compiling or importing", DashboardColours.Working);
            if (EditorUtility.scriptCompilationFailed)
                return ("Needs attention — fix Unity script errors", DashboardColours.Failed);
            if (DependencyInstallation.Failed(tool))
                return ("Needs attention — installation action failed", DashboardColours.Failed);
            bool available;
            switch (tool)
            {
                case "Odin Inspector": available = OdinInstalled && _odinAvailable; break;
                case "Quantum Console": available = _quantumAvailable; break;
                default: return ("Not verified", DashboardColours.Unknown);
            }
            return available ? ("Installed and ready", DashboardColours.Ready)
                : ("Missing — required", DashboardColours.Attention);
        }

        internal static string Description(string tool) => tool == "Odin Inspector"
            ? "Required for the clearer layout, styled controls and enhanced documentation dashboard."
            : "Required for Geurts Game Forge's console integration and documentation status command.";

        private static bool HasType(string assemblyName, string typeName) =>
            AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == assemblyName &&
                assembly.GetType(typeName, false) != null);

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
