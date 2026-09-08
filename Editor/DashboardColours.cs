// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using UnityEngine;

namespace Geurts.GameForge.Documentation
{
    /// <summary>One status palette for update cards and required dependencies.</summary>
    internal static class DashboardColours
    {
        internal static readonly Color Ready = new Color(.35f, .76f, .57f);
        internal static readonly Color Attention = new Color(1f, .57f, .18f);
        internal static readonly Color Working = new Color(.35f, .68f, 1f);
        internal static readonly Color Failed = new Color(1f, .4f, .36f);
        internal static readonly Color Unknown = new Color(.6f, .65f, .7f);
        internal static Color Tint(Color accent) => new Color(accent.r, accent.g, accent.b, .09f);
    }
}
