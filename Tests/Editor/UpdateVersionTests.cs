// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Geurts.GameForge.Documentation.Tests
{
    internal sealed class UpdateVersionTests
    {
        [TestCase("https://github.com/Geurtsy/com.geurts.gameforge.documentation.git", "HEAD", "")]
        [TestCase("git+https://github.com/Geurtsy/com.geurts.gameforge.documentation.git#main", "main", "")]
        [TestCase("git@github.com:Other/Fork.git?path=/Packages/docs#preview/ui", "preview%2Fui", "Packages/docs/")]
        [TestCase("ssh://git@github.com/Other/Fork.git#v0.4.0", "v0.4.0", "")]
        [TestCase("https://github.com/Other/Fork.git#aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "")]
        public void ChecksPreserveSelectedRepositoryRevisionAndPackagePath(string url, string revision, string path)
        {
            GitHubPackageSource source = GitHubPackageSource.Parse(url);
            Assert.That(source.CommitUrl, Does.EndWith("/commits/" + revision));
            Assert.That(source.VersionUrl("commit"), Does.EndWith("/commit/" + path + "package.json"));
            if (url.Contains("Other")) Assert.That(source.CommitUrl, Does.Contain("/Other/Fork/"));
        }

        [TestCase("git+file:///C:/fixture#main")]
        [TestCase("https://github.com.example.com/Owner/Repo.git")]
        [TestCase("https://github.com/Owner/Repo.git?path=../../private#main")]
        public void UnsupportedChecksNeverSilentlySwitchToTheOfficialSource(string url)
        {
            Assert.That(() => GitHubPackageSource.Parse(url), Throws.InstanceOf<NotSupportedException>());
        }

        [Test]
        public async Task PackageVersionIsFetchedAtResolvedCommitWithCompactMetadata()
        {
            string commit = new string('a', 40);
            var handler = new FixtureHandler(commit, "{\"name\":\"com.geurts.gameforge.documentation\",\"version\":\"0.5.0\"}");
            var progress = new List<UpdateProgress>();
            using (var metadata = new GitVersionMetadata(handler))
            {
                GitPackageVersion result = await metadata.ReadPackageAsync(GitVersionMetadata.OfficialPackageUrl,
                    CancellationToken.None, progress.Add);
                Assert.That(result.Commit, Is.EqualTo(commit));
                Assert.That(result.Version, Is.EqualTo("0.5.0"));
                Assert.That(handler.Urls[1], Does.Contain("/" + commit + "/package.json"));
                Assert.That(handler.Accept[0], Is.EqualTo("application/vnd.github.sha"));
                Assert.That(progress.Count, Is.EqualTo(2));
                Assert.That(progress[0].Fraction, Is.LessThan(0), "Unknown-duration steps must not invent percentages.");
            }
        }

        [TestCase("{\"name\":\"wrong.package\",\"version\":\"0.5.0\"}")]
        [TestCase("{\"name\":\"com.geurts.gameforge.documentation\"}")]
        public void InvalidRemotePackageVersionCannotReportCurrent(string json)
        {
            using (var metadata = new GitVersionMetadata(new FixtureHandler(new string('a', 40), json)))
                Assert.ThrowsAsync<InvalidDataException>(async () => await metadata.ReadPackageAsync(
                    GitVersionMetadata.OfficialPackageUrl, CancellationToken.None));
        }

        [Test]
        public void OversizedMetadataIsRejectedBeforeParsing()
        {
            using (var metadata = new GitVersionMetadata(new FixtureHandler(new string('a', DocumentationPackageConstants.MetadataLimitBytes + 1))))
                Assert.ThrowsAsync<InvalidDataException>(async () => await metadata.ReadPackageAsync(
                    GitVersionMetadata.OfficialPackageUrl, CancellationToken.None));
        }

        [Test]
        public void HttpFailureIsReportedAndCanBeRetried()
        {
            using (var metadata = new GitVersionMetadata(new FixtureHandler { ResponseStatus = HttpStatusCode.ServiceUnavailable }))
                Assert.ThrowsAsync<HttpRequestException>(async () => await metadata.ReadPackageAsync(
                    GitVersionMetadata.OfficialPackageUrl, CancellationToken.None));
            var status = new UpdateStatus { InstalledVersion = "0.4.0", InstalledCommit = new string('a', 40) };
            status.BeginCheck();
            status.CompleteCheck("0.5.0", new string('b', 40), true);
            status.BeginCheck();
            Assert.That(status.AvailableVersion, Is.EqualTo("Checking..."));
            Assert.That(status.RemoteCommit, Is.Null);
            status.FailCheck("Offline");
            Assert.That(status.AvailableVersion, Is.EqualTo("Unavailable"));
            Assert.That(status.InstalledVersion, Is.EqualTo("0.4.0"));
            Assert.That(status.Availability, Is.EqualTo(DocumentationAvailability.Unknown));
            Assert.That(status.IsChecking, Is.False);
            status.BeginCheck();
            status.CompleteCheck("0.4.0", new string('a', 40), true);
            Assert.That(status.Availability, Is.EqualTo(DocumentationAvailability.Current));
            Assert.That(status.Failed, Is.False);
        }

        [Test]
        public void SameVersionDifferentCommitStillOffersAnUpdate()
        {
            var status = new UpdateStatus { InstalledVersion = "0.5.0", InstalledCommit = new string('a', 40) };
            status.BeginCheck();
            status.CompleteCheck("0.5.0", new string('b', 40), true);
            Assert.That(status.Availability, Is.EqualTo(DocumentationAvailability.UpdateAvailable));
            status.CompleteCheck("0.5.0", new string('a', 40), false);
            Assert.That(status.Availability, Is.EqualTo(DocumentationAvailability.Unknown), "Local installs cannot claim Git equality.");
        }

        [Test]
        public void DocumentationExplainsSameVersionRevisionChangesAndMissingRecordsSeparately()
        {
            var changed = new DocumentationCheckResult(DocumentationAvailability.UpdateAvailable,
                new string('b', 40), new string('a', 40), "0.11.0", "0.11.0");
            Assert.That(DocumentationUpdaterController.BuildCheckMessage(changed), Does.Contain("same version number (0.11.0)"));
            var unrecorded = new DocumentationCheckResult(DocumentationAvailability.UpdateAvailable,
                new string('b', 40), null, "0.11.0", "0.11.0");
            Assert.That(DocumentationUpdaterController.BuildCheckMessage(unrecorded), Does.Contain("cannot be verified"));
            Assert.That(DocumentationUpdaterController.BuildCheckMessage(unrecorded), Does.Not.Contain("different revision"));
        }

        [Test]
        public void InstalledDocumentationReadsOnlyItsVersionManifest()
        {
            string root = Path.Combine(Path.GetTempPath(), "GeurtsVersions-" + Guid.NewGuid().ToString("N"));
            string docs = Path.Combine(root, DocumentationPackageConstants.ManagedDocumentationDirectory);
            try
            {
                Assert.That(GitVersionMetadata.ReadInstalledDocumentationVersion(root), Is.EqualTo("Not installed"));
                Directory.CreateDirectory(docs);
                string manifest = Path.Combine(docs, GitVersionMetadata.ManifestPath);
                File.WriteAllText(manifest, "# Manifest\r\n\r\n**Version:** 0.11.0\r\n");
                Assert.That(GitVersionMetadata.ReadInstalledDocumentationVersion(root), Is.EqualTo("0.11.0"));
                File.WriteAllText(manifest, "no version");
                Assert.That(GitVersionMetadata.ReadInstalledDocumentationVersion(root), Does.StartWith("Unknown"));
                File.WriteAllText(manifest, new string('x', DocumentationPackageConstants.MetadataLimitBytes + 1));
                Assert.That(GitVersionMetadata.ReadInstalledDocumentationVersion(root), Does.Contain("too large"));
            }
            finally { DocumentationFileOperations.DeleteDirectoryBestEffort(root); }
        }

        [Test]
        public async Task OverlappingOpenAndManualChecksShareWorkAndLaterRefreshRunsBothAgain()
        {
            int packages = 0, documents = 0;
            var pending = new TaskCompletionSource<bool>();
            var coordinator = new UpdateCheckCoordinator(
                () => { packages++; return pending.Task; },
                () => { documents++; return pending.Task; });
            Task first = coordinator.CheckAsync();
            Assert.That(coordinator.CheckAsync(), Is.SameAs(first));
            Assert.That(packages, Is.EqualTo(1));
            Assert.That(documents, Is.EqualTo(1));
            pending.SetResult(true);
            await first;
            await coordinator.CheckAsync();
            Assert.That(packages, Is.EqualTo(2));
            Assert.That(documents, Is.EqualTo(2));
        }

        private sealed class FixtureHandler : HttpMessageHandler
        {
            private readonly Queue<string> _responses;
            internal readonly List<string> Urls = new List<string>();
            internal readonly List<string> Accept = new List<string>();
            internal HttpStatusCode ResponseStatus = HttpStatusCode.OK;
            internal FixtureHandler(params string[] responses) { _responses = new Queue<string>(responses); }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Urls.Add(request.RequestUri.AbsoluteUri);
                Accept.Add(request.Headers.Accept.ToString());
                return Task.FromResult(new HttpResponseMessage(ResponseStatus)
                { Content = new StringContent(_responses.Count > 0 ? _responses.Dequeue() : "") });
            }
        }
    }
}
