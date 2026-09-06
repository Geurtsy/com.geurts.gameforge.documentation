using System;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;

namespace Geurts.GameForge.Documentation.Tests
{
    internal sealed class GitHubDocumentationTransportTests
    {
        private string temporaryRoot;

        [SetUp]
        public void SetUp()
        {
            temporaryRoot = Path.Combine(
                Path.GetTempPath(),
                "GeurtsDocumentationArchiveTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryRoot);
        }

        [TearDown]
        public void TearDown()
        {
            DocumentationFileOperations.DeleteDirectoryBestEffort(temporaryRoot);
        }

        [Test]
        public void ExtractCommitArchiveAcceptsGitHubDirectoryEntries()
        {
            string commit = new string('a', 40);
            string wrapper = "GeurtsGameForge_Documentation-" + commit;
            string archivePath = Path.Combine(temporaryRoot, "documentation.zip");
            string extractionRoot = Path.Combine(temporaryRoot, "extracted");
            Directory.CreateDirectory(extractionRoot);

            using (FileStream archiveStream = File.Create(archivePath))
            using (ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, false))
            {
                archive.CreateEntry(wrapper + "/");
                archive.CreateEntry(wrapper + "/GeurtsTechniques/");
                ZipArchiveEntry contract = archive.CreateEntry(
                    wrapper + "/GeurtsTechniques/GeurtsDocumentationCompanionContract.json");
                using (StreamWriter writer = new StreamWriter(contract.Open()))
                {
                    writer.Write("{}");
                }
            }

            string candidate = GitHubDocumentationTransport.ExtractCommitArchive(
                archivePath,
                extractionRoot,
                commit);

            Assert.That(candidate, Is.EqualTo(extractionRoot));
            Assert.That(
                File.ReadAllText(Path.Combine(
                    extractionRoot,
                    "GeurtsTechniques",
                    "GeurtsDocumentationCompanionContract.json")),
                Is.EqualTo("{}"));
        }

        [Test]
        public void ExtractCommitArchiveRejectsTraversalBeforeWritingOutsideExtractionRoot()
        {
            string commit = new string('b', 40);
            string wrapper = "GeurtsGameForge_Documentation-" + commit;
            string archivePath = Path.Combine(temporaryRoot, "documentation.zip");
            string extractionRoot = Path.Combine(temporaryRoot, "extracted");
            string escapedPath = Path.Combine(temporaryRoot, "escaped.txt");
            Directory.CreateDirectory(extractionRoot);

            using (FileStream archiveStream = File.Create(archivePath))
            using (ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, false))
            {
                ZipArchiveEntry traversal = archive.CreateEntry(wrapper + "/../escaped.txt");
                using (StreamWriter writer = new StreamWriter(traversal.Open()))
                {
                    writer.Write("must not escape");
                }
            }

            Assert.Throws<InvalidDataException>(() =>
                GitHubDocumentationTransport.ExtractCommitArchive(
                    archivePath,
                    extractionRoot,
                    commit));
            Assert.That(File.Exists(escapedPath), Is.False);
        }
    }
}
