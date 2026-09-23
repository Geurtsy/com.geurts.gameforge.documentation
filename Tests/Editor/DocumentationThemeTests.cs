// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Geurts.GameForge.Documentation.Tests
{
    internal sealed class DocumentationThemeTests
    {
        [Test]
        public void ThemeStylesOnlyItsOwnedSubtreeAndAttachesOnce()
        {
            var host = new VisualElement();
            var owned = new VisualElement();
            host.Add(owned);
            DocumentationEditorTheme.ApplyToolkit(owned);
            DocumentationEditorTheme.ApplyToolkit(owned);
            Assert.That(host.ClassListContains(DocumentationEditorTheme.RootClass), Is.False);
            Assert.That(host.styleSheets.count, Is.Zero, "The host may be Unity Package Manager and must retain its own theme.");
            Assert.That(owned.ClassListContains(DocumentationEditorTheme.RootClass), Is.True);
            Assert.That(owned.styleSheets.count, Is.EqualTo(1));
            Assert.That(owned.styleSheets[0], Is.SameAs(AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.geurts.gameforge.documentation/Editor/DocumentationEditorTheme.uss")));
        }

        [Test]
        public void CompanionAssemblyDoesNotReferenceGod()
        {
            Assert.That(typeof(DocumentationEditorTheme).Assembly.GetReferencedAssemblies()
                .Any(assembly => assembly.Name.StartsWith("Geurts.GameForge.God")), Is.False);
        }
    }
}
