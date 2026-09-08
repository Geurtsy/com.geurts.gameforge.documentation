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
            Color waiting = new Color(.46f, .32f, .08f);
            Color unavailable = new Color(.52f, .16f, .16f);
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return ("Checking — Unity is compiling or importing", waiting);
            if (EditorUtility.scriptCompilationFailed)
                return ("Unavailable — fix Unity script errors", unavailable);
            bool available;
            switch (tool)
            {
                case "Odin Inspector": available = _odinAvailable; break;
                case "Quantum Console": available = _quantumAvailable; break;
                default: return ("Not verified", waiting);
            }
            return available ? ("Installed and ready", new Color(.12f, .38f, .20f))
                : ("Missing or unavailable", unavailable);
        }

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
