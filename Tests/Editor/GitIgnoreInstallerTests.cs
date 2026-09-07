using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Geurts.GameForge.Documentation.Tests
{
    internal sealed class GitIgnoreInstallerTests
    {
        private string _projectRoot;
        private string _target;
        private string _documentPath;
        private string _manifestPath;
        private static readonly UTF8Encoding _utf8 = new UTF8Encoding(false);

        [SetUp]
        public void SetUp()
        {
            _projectRoot = Path.Combine(Path.GetTempPath(), "GeurtsGitIgnoreTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_projectRoot);
            _target = Path.Combine(_projectRoot, ".gitignore");
            string documentation = Path.Combine(_projectRoot, DocumentationPackageConstants.ManagedDocumentationDirectory);
            _documentPath = Path.Combine(documentation, GitIgnoreTemplateReader.TechniquePath);
            _manifestPath = Path.Combine(documentation, "GeurtsTechniqueManifest.md");
        }

        [TearDown]
        public void TearDown()
        {
            DocumentationFileOperations.DeleteDirectoryBestEffort(_projectRoot);
        }

        [Test]
        public void CancelWarnsAboutTheExactTargetAndPreservesExistingBytesAndTimestamp()
        {
            WriteExistingTarget();
            byte[] before = File.ReadAllBytes(_target);
            DateTime timestamp = File.GetLastWriteTimeUtc(_target);
            string warning = null;

            Assert.That(GitIgnoreInstaller.InstallWithConfirmation(_projectRoot, message =>
            {
                warning = message;
                return false;
            }), Is.False);

            Assert.That(warning, Does.Contain(_target));
            Assert.That(warning, Does.Contain("will be overwritten in full"));
            Assert.That(warning, Does.Contain("All custom rules in that file will be lost"));
            Assert.That(File.ReadAllBytes(_target), Is.EqualTo(before));
            Assert.That(File.GetLastWriteTimeUtc(_target), Is.EqualTo(timestamp));
        }

        [Test]
        public void CancelDoesNotRequireDocumentationOrCreateAMissingTarget()
        {
            string missingProject = Path.Combine(_projectRoot, "does-not-exist");
            Assert.That(GitIgnoreInstaller.InstallWithConfirmation(missingProject, _ => false), Is.False);
            Assert.That(Directory.Exists(missingProject), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingDocumentationDoesNotCreateOrOverwriteTarget(bool existing)
        {
            if (existing)
            {
                WriteExistingTarget();
            }

            Assert.That(() => GitIgnoreInstaller.InstallWithConfirmation(_projectRoot, _ => true),
                Throws.TypeOf<InvalidDataException>().With.Message.Contains("Update Geurts Game Forge Documentation"));
            Assert.That(File.Exists(_target), Is.EqualTo(existing));
            if (existing)
            {
                Assert.That(File.ReadAllText(_target), Is.EqualTo("# custom rules\r\nkeep-private/\r\n"));
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void ConfirmedInstallUsesTheActualDocumentationPayload(bool existing, bool crlfWithBom)
        {
            CopyDocumentationFixture();
            foreach (string file in new[] { _documentPath, _manifestPath })
            {
                string text = File.ReadAllText(file).Replace("\r\n", "\n");
                if (crlfWithBom)
                {
                    text = text.Replace("\n", "\r\n");
                }
                File.WriteAllText(file, text, new UTF8Encoding(crlfWithBom));
            }
            if (existing)
            {
                WriteExistingTarget();
            }
            string sentinel = Path.Combine(_projectRoot, "unrelated.txt");
            File.WriteAllText(sentinel, "user content");
            byte[] documentBefore = File.ReadAllBytes(_documentPath);
            byte[] manifestBefore = File.ReadAllBytes(_manifestPath);

            Assert.That(GitIgnoreInstaller.InstallWithConfirmation(_projectRoot, _ => true), Is.True);

            byte[] installed = File.ReadAllBytes(_target);
            using (SHA256 sha256 = SHA256.Create())
            {
                Assert.That(BitConverter.ToString(sha256.ComputeHash(installed)).Replace("-", "").ToLowerInvariant(),
                    Is.EqualTo("7223a9449718942d3a5cad00cf4d4e0dee9c89eb64951541fa4ebfb803acb45b"));
            }
            string payload = _utf8.GetString(installed);
            Assert.That(payload.Count(character => character == '\n'), Is.EqualTo(376));
            Assert.That(payload, Does.Not.Contain("\r").And.Not.Contain("GEURTS-GITIGNORE-BEGIN").And.Not.Contain("```"));
            Assert.That(installed.Take(3), Is.Not.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }));
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("user content"));
            Assert.That(File.ReadAllBytes(_documentPath), Is.EqualTo(documentBefore));
            Assert.That(File.ReadAllBytes(_manifestPath), Is.EqualTo(manifestBefore));

            Assert.That(GitIgnoreInstaller.InstallWithConfirmation(_projectRoot, _ => true), Is.True);
            Assert.That(File.ReadAllBytes(_target), Is.EqualTo(installed));
        }

        [TestCase("payload")]
        [TestCase("duplicate-marker")]
        [TestCase("fence")]
        [TestCase("manifest-version")]
        [TestCase("technique-version")]
        [TestCase("invalid-utf8")]
        public void InvalidDocumentationPreservesExistingTargetBeforeReplacement(string fault)
        {
            CopyDocumentationFixture();
            WriteExistingTarget();
            string document = File.ReadAllText(_documentPath);
            switch (fault)
            {
                case "payload":
                    document = document.Replace(".geurts/", "changed-rule/");
                    break;
                case "duplicate-marker":
                    document += "\n<!-- GEURTS-GITIGNORE-END -->\n";
                    break;
                case "fence":
                    document = document.Replace("```gitignore", "```text");
                    break;
                case "manifest-version":
                    File.WriteAllText(_manifestPath, File.ReadAllText(_manifestPath).Replace(
                        "`GeurtsTechniques/GeurtsGitIgnoreTechnique.md` | 1.0.0",
                        "`GeurtsTechniques/GeurtsGitIgnoreTechnique.md` | 9.0.0"), _utf8);
                    break;
                case "technique-version":
                    document = document.Replace("**Version:** 1.0.0", "**Version:** 9.0.0");
                    break;
            }
            File.WriteAllText(_documentPath, document, _utf8);
            if (fault == "invalid-utf8")
            {
                File.WriteAllBytes(_documentPath, new byte[] { 0xc3, 0x28 });
            }
            byte[] before = File.ReadAllBytes(_target);
            DateTime timestamp = File.GetLastWriteTimeUtc(_target);

            Assert.That(() => GitIgnoreInstaller.InstallWithConfirmation(_projectRoot, _ => true), Throws.Exception);

            Assert.That(File.ReadAllBytes(_target), Is.EqualTo(before));
            Assert.That(File.GetLastWriteTimeUtc(_target), Is.EqualTo(timestamp));
        }

        [Test]
        public void DirectoryAtTargetIsPreserved()
        {
            CopyDocumentationFixture();
            Directory.CreateDirectory(_target);
            string sentinel = Path.Combine(_target, "keep.txt");
            File.WriteAllText(sentinel, "preserve");

            Assert.Throws<IOException>(() => GitIgnoreInstaller.InstallWithConfirmation(_projectRoot, _ => true));
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("preserve"));
        }

        [Test]
        public void ReplacingHardLinkedTargetPreservesTheOtherFile()
        {
            CopyDocumentationFixture();
            string otherFile = Path.Combine(_projectRoot, "other-file.txt");
            File.WriteAllText(otherFile, "unrelated original");
            Assert.That(CreateHardLink(_target, otherFile, IntPtr.Zero), Is.True,
                "Could not create hard-link fixture: " + Marshal.GetLastWin32Error());

            Assert.That(GitIgnoreInstaller.InstallWithConfirmation(_projectRoot, _ => true), Is.True);
            Assert.That(File.ReadAllText(otherFile), Is.EqualTo("unrelated original"));
            Assert.That(File.ReadAllText(_target), Is.Not.EqualTo("unrelated original"));
        }

        private void WriteExistingTarget()
        {
            File.WriteAllText(_target, "# custom rules\r\nkeep-private/\r\n", _utf8);
            File.SetLastWriteTimeUtc(_target, new DateTime(2024, 1, 2, 3, 4, 6, DateTimeKind.Utc));
        }

        private void CopyDocumentationFixture()
        {
            string source = Path.Combine(Path.GetDirectoryName(Application.dataPath),
                DocumentationPackageConstants.ManagedDocumentationDirectory);
            if (!File.Exists(Path.Combine(source, GitIgnoreTemplateReader.TechniquePath)))
            {
                Assert.Ignore("The integration fixture requires installed documentation. " +
                              "Run Tools/ValidatePackage.ps1 with -DocumentationPath pointing to GeurtsGameForgeDocumentation.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_documentPath));
            File.Copy(Path.Combine(source, GitIgnoreTemplateReader.TechniquePath), _documentPath);
            File.Copy(Path.Combine(source, "GeurtsTechniqueManifest.md"), _manifestPath);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateHardLinkW")]
        private static extern bool CreateHardLink(string fileName, string existingFileName, IntPtr securityAttributes);
    }
}
