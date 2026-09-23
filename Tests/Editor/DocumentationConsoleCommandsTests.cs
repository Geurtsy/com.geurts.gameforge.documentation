// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.
using System;
using System.Reflection;
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
            var godPolicy = Type.GetType("Geurts.GameForge.God.ForgeDiagnostics, Geurts.GameForge.God", false);
            bool fallbackGate = godPolicy != null && !(bool)godPolicy.GetProperty("CommandPolicyAvailable", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            var diagnosticsService = Type.GetType("Geurts.GameForge.Diagnostics.DiagnosticsService, Geurts.GameForge.Diagnostics.Runtime", false);
            bool diagnosticsActive = diagnosticsService?.GetProperty("Current", BindingFlags.Static | BindingFlags.Public).GetValue(null) != null;
            string result;
            if (fallbackGate)
            {
                var denied = Assert.Throws<Exception>(() => QuantumConsoleProcessor.InvokeCommand("GeurtsGameForge.Documentation.Status"));
                Assert.That(denied.ToString(), Does.Contain("Only the read-only GeurtsGameForge.ListBricks command is available"));
                result = DocumentationConsoleCommands.ReadStatus();
            }
            else if (diagnosticsActive)
            {
                // Active Diagnostics routes ordinary QC calls to presentation and returns no value.
                // Its return-value adapter retains policy checks: this independent command is unclassified.
                var router = Type.GetType("Geurts.GameForge.Diagnostics.DiagnosticsCommandRouter, Geurts.GameForge.Diagnostics.Runtime", true);
                var invoke = router.GetMethod("InvokeForResult", BindingFlags.Static | BindingFlags.Public);
                Assert.That(invoke, Is.Not.Null, "Diagnostics must expose its policy-preserving programmatic adapter.");
                var denied = Assert.Throws<TargetInvocationException>(() => invoke.Invoke(null, new object[] { "GeurtsGameForge.Documentation.Status" }));
                Assert.That(denied.GetBaseException(), Is.TypeOf<InvalidOperationException>());
                Assert.That(denied.GetBaseException().Message, Does.Contain("This command has no Game Forge policy."));
                result = DocumentationConsoleCommands.ReadStatus();
            }
            else result = QuantumConsoleProcessor.InvokeCommand("GeurtsGameForge.Documentation.Status") as string;
            Assert.That(result, Is.Not.Null, "The direct status command must return the existing dashboard text.");
            Assert.That(result, Does.Contain("Geurts Game Forge Documentation"));
            Assert.That(result, Does.Contain(DocumentationUpdaterController.StatusMessage));
            Assert.That(result, Does.Contain(PackageSelfUpdater.instance.StatusMessage));
            Assert.That(DocumentationUpdaterController.IsBusy, Is.EqualTo(documentationBusy));
            Assert.That(PackageSelfUpdater.instance.IsBusy, Is.EqualTo(packageBusy));
        }
    }
}
