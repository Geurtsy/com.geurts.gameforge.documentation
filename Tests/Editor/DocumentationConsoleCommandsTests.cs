// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.
using NUnit.Framework;
using QFSW.QC;

namespace Geurts.GameForge.Documentation.Tests
{
    internal sealed class DocumentationConsoleCommandsTests
    {
        [Test]
        public void QuantumConsoleStatusReadsTheExistingDashboardWithoutStartingAnUpdate()
        {
            bool documentationBusy = DocumentationUpdaterController.IsBusy;
            bool packageBusy = PackageSelfUpdater.instance.IsBusy;
            var result = QuantumConsoleProcessor.InvokeCommand("GeurtsGameForge.Documentation.Status") as string;
            Assert.That(result, Does.Contain("Geurts Game Forge Documentation"));
            Assert.That(result, Does.Contain(DocumentationUpdaterController.StatusMessage));
            Assert.That(result, Does.Contain(PackageSelfUpdater.instance.StatusMessage));
            Assert.That(DocumentationUpdaterController.IsBusy, Is.EqualTo(documentationBusy));
            Assert.That(PackageSelfUpdater.instance.IsBusy, Is.EqualTo(packageBusy));
        }
    }
}
