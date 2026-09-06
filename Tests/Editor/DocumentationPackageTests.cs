using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Geurts.GameForge.Documentation.Tests
{
    internal sealed class DocumentationPackageTests
    {
        [Test]
        public void PackageIsIndependentAndContainsNoGenericDocumentationPayload()
        {
            PackageInfo package = PackageInfo.FindForAssembly(typeof(DocumentationUpdateService).Assembly);

            Assert.That(package, Is.Not.Null);
            Assert.That(package.name, Is.EqualTo(DocumentationPackageConstants.PackageName));
            Assert.That(package.dependencies, Is.Empty);
            Assert.That(Directory.Exists(Path.Combine(package.resolvedPath, "GeurtsTechniques")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(package.resolvedPath, "GeurtsGameForgeDocumentation")), Is.False);
            Assert.That(
                Directory.EnumerateFiles(package.resolvedPath, "*.bat", SearchOption.AllDirectories),
                Is.Empty);
        }

        [Test]
        public void ConfirmationListsTheCompleteClosedManagedBoundary()
        {
            string message = DocumentationPackageConstants.BuildConfirmationMessage();
            string[] managedBullets = message
                .Split('\n')
                .Where(line => line.StartsWith("- ", System.StringComparison.Ordinal))
                .ToArray();

            Assert.That(managedBullets, Is.EqualTo(new[]
            {
                "- GeurtsGameForgeDocumentation/",
                "- AGENTS.md",
                "- .github/copilot-instructions.md",
                "- .github/instructions/geurts-unity.instructions.md",
                "- .github/instructions/geurts-game-design.instructions.md"
            }));

            Assert.That(message, Does.Contain("Docs/GameDesign/"));
            Assert.That(message, Does.Contain("every unlisted project path will not be accessed or changed"));
            Assert.That(message, Does.Contain("no backup or rollback"));
        }

        [Test]
        public void StartupCheckIsGatedOncePerInteractiveEditorSession()
        {
            DocumentationStartup.ResetSessionCheckForTests();
            try
            {
                Assert.That(DocumentationStartup.TryMarkCheckScheduled(), Is.True);
                Assert.That(DocumentationStartup.TryMarkCheckScheduled(), Is.False);
            }
            finally
            {
                DocumentationStartup.ResetSessionCheckForTests();
            }
        }
    }
}
