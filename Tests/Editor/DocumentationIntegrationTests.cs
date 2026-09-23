// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Geurts.GameForge.Documentation.Tests
{
    internal sealed class DocumentationIntegrationTests
    {
        /// <summary>Public checks use the existing controller, emit status events and never download content.</summary>
        [Test] public async Task PublicCheckReusesControllerWithoutAcquisitionOrProjectWrites()
        {
            string root = Path.Combine(Path.GetTempPath(), "GeurtsIntegration" + Guid.NewGuid().ToString("N"));
            var transport = new CountingTransport();
            var original = DocumentationUpdaterController.Service;
            Directory.CreateDirectory(root);
            DocumentationUpdaterController.Service = new DocumentationUpdateService(root, transport);
            int changes = 0;
            Action handler = () => changes++;
            DocumentationIntegration.Changed += handler;
            try
            {
                await DocumentationIntegration.CheckForUpdatesAsync();
                Assert.That(transport.MetadataCalls, Is.EqualTo(2));
                Assert.That(transport.Downloads, Is.Zero);
                Assert.That(Directory.GetFileSystemEntries(root), Is.Empty);
                Assert.That(DocumentationIntegration.InstalledVersion, Is.EqualTo("Not installed"));
                Assert.That(DocumentationIntegration.AvailableVersion, Is.EqualTo("0.18.0"));
                Assert.That(changes, Is.GreaterThan(0));
                Assert.That(DocumentationIntegration.IsBusy, Is.False);
            }
            finally
            {
                DocumentationIntegration.Changed -= handler;
                DocumentationUpdaterController.Service = original;
                DeleteFixture(root);
            }
        }

        /// <summary>Declining the public content action performs no metadata request, download, queue or filesystem write.</summary>
        [Test] public void CancelledPublicUpdateDoesNothing()
        {
            var original = DocumentationUpdaterController.Service;
            var transport = new CountingTransport();
            string root = Path.Combine(Path.GetTempPath(), "GeurtsCancelledIntegration" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            DocumentationUpdaterController.Service = new DocumentationUpdateService(root, transport);
            DocumentationUpdaterController.ConfirmForTests = () => false;
            try
            {
                DocumentationIntegration.UpdateDocumentation();
                Assert.That(DocumentationIntegration.IsBusy, Is.False);
                Assert.That(transport.MetadataCalls, Is.Zero);
                Assert.That(transport.Downloads, Is.Zero);
                Assert.That(Directory.GetFileSystemEntries(root), Is.Empty);
            }
            finally { DocumentationUpdaterController.ConfirmForTests = null; DocumentationUpdaterController.Service = original; DeleteFixture(root); }
        }

        /// <summary>Accepted work reserves the delayed-start gap, and rechecks a host guard before any acquisition.</summary>
        [Test] public void QueuedUpdateReservesBusyAndRechecksHostBeforeStarting()
        {
            bool blocked = false;
            using (DocumentationIntegration.RegisterOperationGuard(() => blocked ? "Test host operation is active." : null))
            {
                DocumentationUpdaterController.ConfirmForTests = () => true;
                try
                {
                    DocumentationIntegration.UpdateDocumentation();
                    Assert.That(DocumentationIntegration.IsBusy, Is.True);
                    EditorApplication.delayCall -= DocumentationUpdaterController.BeginConfirmedUpdate;
                    blocked = true;
                    Assert.That(DocumentationIntegration.ActionUnavailableReason, Is.EqualTo("Test host operation is active."));
                    LogAssert.Expect(LogType.Warning, "[Geurts Documentation] Documentation update could not start: Test host operation is active. Try Update again when Unity is ready.");
                    DocumentationUpdaterController.BeginConfirmedUpdate();
                    Assert.That(DocumentationIntegration.IsBusy, Is.False);
                    Assert.That(DocumentationIntegration.Failed, Is.True);
                }
                finally
                {
                    DocumentationUpdaterController.ConfirmForTests = null;
                    EditorApplication.delayCall -= DocumentationUpdaterController.BeginConfirmedUpdate;
                }
            }
            Assert.That(DocumentationIntegration.ExternalOperationUnavailableReason, Is.Null);
        }

        private sealed class CountingTransport : IDocumentationTransport
        {
            internal int MetadataCalls, Downloads;
            public Task<string> ResolveHeadCommitAsync(CancellationToken token) { MetadataCalls++; return Task.FromResult(new string('a', 40)); }
            public Task<string> ReadVersionAsync(string commit, CancellationToken token) { MetadataCalls++; return Task.FromResult("0.18.0"); }
            public Task<DocumentationDownload> DownloadCommitAsync(string commit, string workingDirectory, CancellationToken token, Action<UpdateProgress> progress)
            { Downloads++; throw new InvalidOperationException("This check must not acquire content."); }
        }

        private static void DeleteFixture(string root)
        {
            string full = Path.GetFullPath(root);
            string temporary = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(full.StartsWith(temporary, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Path.GetFileName(full), Does.StartWith("Geurts"));
            Directory.Delete(full, true);
        }
    }
}
