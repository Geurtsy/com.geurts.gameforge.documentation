// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using UnityEngine;

namespace Geurts.GameForge.Documentation
{
    /// <summary>One status palette for update cards and required dependencies.</summary>
    internal static class DashboardColours
    {
        internal static readonly Color Ready = DocumentationEditorTheme.Green;
        internal static readonly Color Attention = DocumentationEditorTheme.Warning;
        internal static readonly Color Working = DocumentationEditorTheme.Green;
        internal static readonly Color Failed = DocumentationEditorTheme.Error;
        internal static readonly Color Unknown = DocumentationEditorTheme.Muted;
        internal static Color Tint(Color accent) => new Color(accent.r, accent.g, accent.b, .09f);
    }
}
