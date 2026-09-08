// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Geurts.GameForge.Documentation.Tests
{
    internal sealed class DependencyInstallationTests
    {
        private Func<string, bool> _menu;
        private Func<string, string, string, string> _picker;
        private Action<string, bool> _import;

        [SetUp]
        public void SetUp()
        {
            _menu = DependencyInstallation.ExecuteMenu;
            _picker = DependencyInstallation.SelectFile;
            _import = DependencyInstallation.ImportFile;
            foreach (string tool in DocumentationDependencies.RequiredExternalTools)
                DependencyInstallation.SetResult(tool, string.Empty, false);
            SessionState.EraseString("Geurts.Documentation.Dependency.ImportName");
            SessionState.EraseString("Geurts.Documentation.Dependency.ImportTool");
        }

        [TearDown]
        public void TearDown()
        {
            DependencyInstallation.ExecuteMenu = _menu;
            DependencyInstallation.SelectFile = _picker;
            DependencyInstallation.ImportFile = _import;
            foreach (string tool in DocumentationDependencies.RequiredExternalTools)
                DependencyInstallation.SetResult(tool, string.Empty, false);
        }

        [Test]
        public void CancelledFileSelectionDoesNotImportOrClaimSuccess()
        {
            DependencyInstallation.SelectFile = (_, __, ___) => string.Empty;
            DependencyInstallation.ImportFile = (_, __) => Assert.Fail("Cancel must not import anything.");
            DependencyInstallation.ImportLicensedCopy("Odin Inspector");
            Assert.That(DependencyInstallation.Message("Odin Inspector"), Is.Empty);
        }

        [Test]
        public void LicensedFileImportAlwaysRequestsUnityReviewAndExplainsReplacement()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".unitypackage");
            File.WriteAllText(path, "Only an import-routing fixture; never handed to the real importer.");
            try
            {
                int imports = 0;
                DependencyInstallation.SelectFile = (_, __, ___) => path;
                DependencyInstallation.ImportFile = (selected, interactive) =>
                {
                    Assert.That(selected, Is.EqualTo(path));
                    Assert.That(interactive, Is.True, "The user must review the assets before import.");
                    imports++;
                };
                DependencyInstallation.ImportLicensedCopy("Quantum Console");
                Assert.That(imports, Is.EqualTo(1));
                Assert.That(DependencyInstallation.Message("Quantum Console"), Does.Contain("can replace existing tool files"));
                DependencyInstallation.ReportImport(Path.GetFileNameWithoutExtension(path), "Import cancelled.", false, true);
                Assert.That(DependencyInstallation.Message("Quantum Console"), Is.EqualTo("Import cancelled."));
                DependencyInstallation.ReportImport("Quantum Console", "Unity import failed: test error", true, true);
                Assert.That(DependencyInstallation.Failed("Quantum Console"), Is.True);
                DependencyInstallation.ReportImport("Quantum Console", "Unity finished importing the selected assets.", false, true);
                Assert.That(DependencyInstallation.Failed("Quantum Console"), Is.False);
            }
            finally { File.Delete(path); }
        }

        [Test]
        public void InvalidFileAndNativeActionFailureAreVisibleAndRetryClearsFailure()
        {
            DependencyInstallation.SelectFile = (_, __, ___) => "missing-file.unitypackage";
            DependencyInstallation.ImportFile = (_, __) => Assert.Fail("Invalid paths must not reach Unity import.");
            DependencyInstallation.ImportLicensedCopy("Odin Inspector");
            Assert.That(DependencyInstallation.Failed("Odin Inspector"), Is.True);
            Assert.That(DocumentationDependencies.ToolStatus("Odin Inspector").Background, Is.EqualTo(DashboardColours.Failed));
            DependencyInstallation.ExecuteMenu = _ => false;
            DependencyInstallation.OpenOwnedAssets("Odin Inspector");
            Assert.That(DependencyInstallation.Message("Odin Inspector"), Does.Contain("could not open My Assets"));
            DependencyInstallation.ExecuteMenu = menu =>
            {
                Assert.That(menu, Is.EqualTo("Window/Package Management/My Assets"));
                return true;
            };
            DependencyInstallation.OpenOwnedAssets("Odin Inspector");
            Assert.That(DependencyInstallation.Failed("Odin Inspector"), Is.False);
            Assert.That(DependencyInstallation.Message("Odin Inspector"), Does.Contain("ownership are handled by Unity"));
        }

        [Test]
        public void PackageManagerCardsShareReadyPaletteAndHideForUnrelatedSelections()
        {
            var extension = new DocumentationPackageManagerExtension();
            VisualElement root = extension.CreateExtensionUI();
            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
            extension.OnPackageSelectionChange(UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DocumentationUpdateService).Assembly));
            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            foreach (Label status in root.Query<Label>().ToList().Where(label => label.userData is string))
            {
                Assert.That(status.text, Does.Contain("Installed and ready"));
                Assert.That(status.parent.style.borderLeftColor.value, Is.EqualTo(DashboardColours.Ready));
                Assert.That(status.parent.style.backgroundColor.value, Is.EqualTo(DashboardColours.Tint(DashboardColours.Ready)));
                Assert.That(status.parent.Query<Button>().ToList(), Has.Count.EqualTo(2));
            }
            Assert.That(root.Query<Button>().ToList(), Has.Count.EqualTo(4));
            Assert.That(DocumentationDependencies.ToolStatus("Unknown vendor tool").Background, Is.EqualTo(DashboardColours.Unknown));
            extension.OnPackageSelectionChange(null);
            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator OwnedAssetsActionOpensUnitysRealPackageManager()
        {
            EditorWindow[] original = Resources.FindObjectsOfTypeAll<EditorWindow>();
            try
            {
                DependencyInstallation.OpenOwnedAssets("Odin Inspector");
                for (int frame = 0; frame < 5; frame++) yield return null;
                Assert.That(DependencyInstallation.Failed("Odin Inspector"), Is.False);
                Assert.That(Resources.FindObjectsOfTypeAll<EditorWindow>().Any(window =>
                    window.GetType().FullName == "UnityEditor.PackageManager.UI.PackageManagerWindow"), Is.True);
                Assert.That(DependencyInstallation.Message("Odin Inspector"), Does.Contain("My Assets opened"));
            }
            finally
            {
                foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>().Except(original)) window.Close();
            }
        }
    }
}
