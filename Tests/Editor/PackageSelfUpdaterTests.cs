// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Geurts.GameForge.Documentation.Tests
{
    internal sealed class PackageSelfUpdaterTests
    {
        [TestCase("https://github.com/Geurtsy/com.geurts.gameforge.documentation.git")]
        [TestCase("https://github.com/Geurtsy/com.geurts.gameforge.documentation.git#main")]
        [TestCase("https://github.com/Geurtsy/com.geurts.gameforge.documentation.git#codex/package-self-update")]
        [TestCase("https://github.com/Geurtsy/com.geurts.gameforge.documentation.git#v0.4.0")]
        [TestCase("https://github.com/Geurtsy/com.geurts.gameforge.documentation.git#0592549b676e3c5145a53b2a66c2cf8dd43aaffd")]
        [TestCase("ssh://git@github.com/studio/fork.git?path=/Packages/Documentation#preview")]
        [TestCase("git+file:///C:/Packages/Documentation#main")]
        public void UpdatePreservesTheExactConfiguredGitReference(string reference)
        {
            Assert.That(PackageSelfUpdater.GetGitReference(DocumentationPackageConstants.PackageName,
                PackageSource.Git, true, DocumentationPackageConstants.PackageName + "@" + reference), Is.EqualTo(reference));
        }

        [TestCase(PackageSource.Local)]
        [TestCase(PackageSource.Embedded)]
        [TestCase(PackageSource.Registry)]
        [TestCase(PackageSource.Unknown)]
        public void NonGitInstallationsAreNotReplaced(PackageSource source)
        {
            Assert.That(PackageSelfUpdater.GetGitReference(DocumentationPackageConstants.PackageName,
                source, true, DocumentationPackageConstants.PackageName + "@file:C:/Source"), Is.Null);
        }

        [Test]
        public void IndirectOrDifferentPackagesAreNotChanged()
        {
            string packageId = DocumentationPackageConstants.PackageName + "@https://example.com/package.git";
            Assert.That(PackageSelfUpdater.GetGitReference(DocumentationPackageConstants.PackageName,
                PackageSource.Git, false, packageId), Is.Null);
            Assert.That(PackageSelfUpdater.GetGitReference("com.other.package", PackageSource.Git, true, packageId), Is.Null);
            Assert.That(PackageSelfUpdater.GetGitReference(DocumentationPackageConstants.PackageName,
                PackageSource.Git, true, "com.other.package@https://example.com/package.git"), Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("com.geurts.gameforge.documentation@")]
        public void MissingGitReferencesCannotStartAnUpdate(string packageId)
        {
            Assert.That(PackageSelfUpdater.GetGitReference(DocumentationPackageConstants.PackageName,
                PackageSource.Git, true, packageId), Is.Null);
        }

        [Test]
        public void CompletionDistinguishesUnchangedCommitsFromSuccessfulUpdates()
        {
            Assert.That(PackageSelfUpdater.BuildSuccessMessage("abc", "ABC", "0.4.0"), Does.Contain("already matches"));
            Assert.That(PackageSelfUpdater.BuildSuccessMessage("abc", "def", "0.4.0"), Does.Contain("update completed"));
            Assert.That(PackageSelfUpdater.BuildSuccessMessage(null, null, "0.4.0"), Does.Not.Contain("already matches"));
        }

#if !ODIN_INSPECTOR
        [Test]
        public void MissingRequiredOdinBlocksAllToolEntryPoints()
        {
            Assert.That(DocumentationDependencies.OdinInstalled, Is.False);
            Assert.That(PackageSelfUpdater.instance.CanUpdate, Is.False);
            PackageSelfUpdater.instance.BeginUpdate();
            DocumentationUpdaterController.ConfirmAndUpdate();
            DocumentationUpdaterController.BeginConfirmedUpdate();
            GitIgnoreInstaller.ConfirmAndInstall();
            Assert.That(PackageSelfUpdater.instance.IsBusy, Is.False);
            Assert.That(DocumentationUpdaterController.IsBusy, Is.False);
        }
#endif
    }
}
