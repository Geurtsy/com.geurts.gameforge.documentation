using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Geurts.GameForge.Documentation.Tests
{
    internal sealed class DocumentationUpdateServiceTests
    {
        private string temporaryRoot;
        private string projectRoot;
        private string candidateTemplate;
        private FixtureTransport transport;
        private DocumentationUpdateService service;

        [SetUp]
        public void SetUp()
        {
            temporaryRoot = Path.Combine(
                Path.GetTempPath(),
                "GeurtsDocumentationTests-" + Guid.NewGuid().ToString("N"));
            projectRoot = Path.Combine(temporaryRoot, "BareUnityProject");
            candidateTemplate = Path.Combine(temporaryRoot, "CandidateTemplate");
            Directory.CreateDirectory(Path.Combine(projectRoot, "Assets"));
            Directory.CreateDirectory(candidateTemplate);
            CandidateFixture.Write(candidateTemplate, "new documentation", "new route");

            transport = new FixtureTransport(candidateTemplate, Commit('a'));
            service = new DocumentationUpdateService(projectRoot, transport);
        }

        [TearDown]
        public void TearDown()
        {
            new ProjectInstallCommitStore(projectRoot).ClearForTests();
            DocumentationFileOperations.DeleteDirectoryBestEffort(temporaryRoot);
        }

        [Test]
        public async Task BareProjectInstallCreatesDocumentationAndEveryDeclaredRoute()
        {
            await InstallAsync(service);

            string documentation = Path.Combine(projectRoot, "GeurtsGameForgeDocumentation");
            Assert.That(File.ReadAllText(Path.Combine(documentation, "Guide.md")), Is.EqualTo("new documentation"));

            foreach (ManagedAiRoute route in DocumentationPackageConstants.ExpectedManagedAiRoutes)
            {
                string source = DocumentationFileOperations.GetSafeFullPath(documentation, route.Source);
                string target = DocumentationFileOperations.GetSafeFullPath(projectRoot, route.Destination);
                Assert.That(File.Exists(target), Is.True, route.Destination);
                Assert.That(DocumentationFileOperations.FilesEqual(source, target), Is.True, route.Destination);
            }

            Assert.That(ReadInstalledCommit(), Is.EqualTo(Commit('a')));
        }

        [Test]
        public async Task ConfirmedUpdateReplacesOnlyDocumentationAndDeclaredAiRoutes()
        {
            string documentation = Path.Combine(projectRoot, "GeurtsGameForgeDocumentation");
            Directory.CreateDirectory(documentation);
            File.WriteAllText(Path.Combine(documentation, "obsolete.txt"), "remove me");
            File.WriteAllText(Path.Combine(documentation, "Guide.md"), "old documentation");

            foreach (ManagedAiRoute route in DocumentationPackageConstants.ExpectedManagedAiRoutes)
            {
                string target = DocumentationFileOperations.GetSafeFullPath(projectRoot, route.Destination);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.WriteAllText(target, "old managed route");
            }

            string gameDesign = Path.Combine(projectRoot, "Docs", "GameDesign", "Design.md");
            Directory.CreateDirectory(Path.GetDirectoryName(gameDesign));
            byte[] gameDesignBytes = Encoding.UTF8.GetBytes("user-authored design \u2603");
            File.WriteAllBytes(gameDesign, gameDesignBytes);
            DateTime gameDesignWriteTime = new DateTime(2024, 3, 4, 5, 6, 8, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(gameDesign, gameDesignWriteTime);

            string unlistedInstruction = Path.Combine(projectRoot, ".github", "instructions", "user.instructions.md");
            File.WriteAllText(unlistedInstruction, "user owned");
            DateTime unlistedWriteTime = new DateTime(2024, 5, 6, 7, 8, 10, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(unlistedInstruction, unlistedWriteTime);

            await InstallAsync(service);

            Assert.That(File.Exists(Path.Combine(documentation, "obsolete.txt")), Is.False);
            Assert.That(File.ReadAllText(Path.Combine(documentation, "Guide.md")), Is.EqualTo("new documentation"));
            Assert.That(File.ReadAllBytes(gameDesign), Is.EqualTo(gameDesignBytes));
            Assert.That(File.GetLastWriteTimeUtc(gameDesign), Is.EqualTo(gameDesignWriteTime));
            Assert.That(File.ReadAllText(unlistedInstruction), Is.EqualTo("user owned"));
            Assert.That(File.GetLastWriteTimeUtc(unlistedInstruction), Is.EqualTo(unlistedWriteTime));
        }

        [Test]
        public async Task ExistingRootAgentsIsReplacedAndMissingRootAgentsIsCreated()
        {
            string agents = Path.Combine(projectRoot, "AGENTS.md");
            File.WriteAllText(agents, "existing user content");

            await InstallAsync(service);

            Assert.That(File.ReadAllText(agents), Is.EqualTo("new route: AGENTS.md"));

            string secondProject = Path.Combine(temporaryRoot, "SecondBareUnityProject");
            Directory.CreateDirectory(Path.Combine(secondProject, "Assets"));
            FixtureTransport secondTransport = new FixtureTransport(candidateTemplate, Commit('b'));
            DocumentationUpdateService secondService = new DocumentationUpdateService(secondProject, secondTransport);
            await InstallAsync(secondService);

            Assert.That(
                File.ReadAllText(Path.Combine(secondProject, "AGENTS.md")),
                Is.EqualTo("new route: AGENTS.md"));
            new ProjectInstallCommitStore(secondProject).ClearForTests();
        }

        [Test]
        public async Task StartupMetadataCheckDoesNotDownloadOrMutateProjectFiles()
        {
            string documentation = Path.Combine(projectRoot, "GeurtsGameForgeDocumentation");
            Directory.CreateDirectory(documentation);
            string existing = Path.Combine(documentation, "existing.md");
            File.WriteAllText(existing, "existing docs");
            DateTime existingWriteTime = new DateTime(2024, 1, 2, 3, 4, 6, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(existing, existingWriteTime);

            string gameDesign = Path.Combine(projectRoot, "Docs", "GameDesign", "Design.md");
            Directory.CreateDirectory(Path.GetDirectoryName(gameDesign));
            File.WriteAllText(gameDesign, "design");
            DateTime gameDesignWriteTime = new DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(gameDesign, gameDesignWriteTime);

            DocumentationCheckResult result = await service.CheckForUpdateAsync(CancellationToken.None);

            Assert.That(result.Availability, Is.EqualTo(DocumentationAvailability.UpdateAvailable));
            Assert.That(transport.ResolveCalls, Is.EqualTo(1));
            Assert.That(transport.DownloadCalls, Is.EqualTo(0));
            Assert.That(File.ReadAllText(existing), Is.EqualTo("existing docs"));
            Assert.That(File.GetLastWriteTimeUtc(existing), Is.EqualTo(existingWriteTime));
            Assert.That(File.ReadAllText(gameDesign), Is.EqualTo("design"));
            Assert.That(File.GetLastWriteTimeUtc(gameDesign), Is.EqualTo(gameDesignWriteTime));
        }

        [Test]
        public async Task MatchingInstalledCommitNeedsNoArchiveRequest()
        {
            await InstallAsync(service);
            string installedGuide = Path.Combine(
                projectRoot,
                "GeurtsGameForgeDocumentation",
                "Guide.md");
            string installedAgents = Path.Combine(projectRoot, "AGENTS.md");
            File.WriteAllText(installedGuide, "local drift remains uninspected");
            File.Delete(installedAgents);
            transport.ResetCounts();

            DocumentationCheckResult result = await service.CheckForUpdateAsync(CancellationToken.None);

            Assert.That(result.Availability, Is.EqualTo(DocumentationAvailability.Current));
            Assert.That(transport.ResolveCalls, Is.EqualTo(1));
            Assert.That(transport.DownloadCalls, Is.EqualTo(0));
            Assert.That(File.ReadAllText(installedGuide), Is.EqualTo("local drift remains uninspected"));
            Assert.That(File.Exists(installedAgents), Is.False);
        }

        [Test]
        public async Task LastSuccessfulCommitSignalIsScopedPerUnityProject()
        {
            await InstallAsync(service);

            string otherProject = Path.Combine(temporaryRoot, "OtherUnityProject");
            Directory.CreateDirectory(Path.Combine(otherProject, "Assets"));
            FixtureTransport otherTransport = new FixtureTransport(candidateTemplate, Commit('a'));
            DocumentationUpdateService otherService = new DocumentationUpdateService(otherProject, otherTransport);
            try
            {
                DocumentationCheckResult result = await otherService.CheckForUpdateAsync(CancellationToken.None);
                Assert.That(result.Availability, Is.EqualTo(DocumentationAvailability.UpdateAvailable));
                Assert.That(result.InstalledCommit, Is.Null);
                Assert.That(otherTransport.DownloadCalls, Is.EqualTo(0));
            }
            finally
            {
                new ProjectInstallCommitStore(otherProject).ClearForTests();
            }
        }

        [Test]
        public async Task RemoteAcquisitionFailurePreservesAllManagedTargetsAndInstallSignal()
        {
            await InstallAsync(service);
            Dictionary<string, FileSnapshot> before = CaptureManagedFiles(projectRoot);
            string beforeCommit = ReadInstalledCommit();

            FixtureTransport failingTransport = new FixtureTransport(candidateTemplate, Commit('b'))
            {
                DownloadFailure = new IOException("simulated remote failure")
            };
            DocumentationUpdateService failingService = new DocumentationUpdateService(projectRoot, failingTransport);

            Assert.ThrowsAsync<IOException>(async () =>
                await failingService.PrepareLatestAsync(CancellationToken.None));

            AssertSnapshotsEqual(before, CaptureManagedFiles(projectRoot));
            Assert.That(ReadInstalledCommit(), Is.EqualTo(beforeCommit));
        }

        [Test]
        public void InvalidContractFailsBeforeAnyProjectMutation()
        {
            string contract = Path.Combine(
                candidateTemplate,
                "GeurtsTechniques",
                "GeurtsDocumentationCompanionContract.json");
            File.WriteAllText(contract, File.ReadAllText(contract).Replace(
                "\"schemaVersion\": \"1.0.0\"",
                "\"schemaVersion\": \"9.0.0\""));

            string existingAgents = Path.Combine(projectRoot, "AGENTS.md");
            File.WriteAllText(existingAgents, "untouched");

            InvalidDataException exception = Assert.ThrowsAsync<InvalidDataException>(async () =>
                await service.PrepareLatestAsync(CancellationToken.None));

            Assert.That(exception.Message, Does.Contain("Unsupported documentation contract value for schemaVersion"));
            Assert.That(File.ReadAllText(existingAgents), Is.EqualTo("untouched"));
            Assert.That(Directory.Exists(Path.Combine(projectRoot, "GeurtsGameForgeDocumentation")), Is.False);
            Assert.That(ReadInstalledCommit(), Is.Null);
        }

        [Test]
        public void RemoteManagedTargetMismatchFailsBeforeAnyProjectMutation()
        {
            string contract = Path.Combine(
                candidateTemplate,
                "GeurtsTechniques",
                "GeurtsDocumentationCompanionContract.json");
            File.WriteAllText(contract, File.ReadAllText(contract).Replace(
                "\"target\": \".github/copilot-instructions.md\"",
                "\"target\": \".github/unlisted.md\""));

            string existingAgents = Path.Combine(projectRoot, "AGENTS.md");
            File.WriteAllText(existingAgents, "still untouched");

            Assert.ThrowsAsync<InvalidDataException>(async () =>
                await service.PrepareLatestAsync(CancellationToken.None));

            Assert.That(File.ReadAllText(existingAgents), Is.EqualTo("still untouched"));
            Assert.That(Directory.Exists(Path.Combine(projectRoot, "GeurtsGameForgeDocumentation")), Is.False);
            Assert.That(ReadInstalledCommit(), Is.Null);
        }

        [Test]
        public void RemoteConfirmationTargetMismatchFailsBeforeAnyProjectMutation()
        {
            string contract = Path.Combine(
                candidateTemplate,
                "GeurtsTechniques",
                "GeurtsDocumentationCompanionContract.json");
            File.WriteAllText(contract, File.ReadAllText(contract).Replace(
                "{ \"path\": \".github/copilot-instructions.md\", \"effect\": \"replace-complete-file\" }",
                "{ \"path\": \".github/unlisted.md\", \"effect\": \"replace-complete-file\" }"));

            Assert.ThrowsAsync<InvalidDataException>(async () =>
                await service.PrepareLatestAsync(CancellationToken.None));
            Assert.That(Directory.Exists(Path.Combine(projectRoot, "GeurtsGameForgeDocumentation")), Is.False);
        }

        [Test]
        public void ContractAndManifestVersionMismatchFailsBeforeAnyProjectMutation()
        {
            string contract = Path.Combine(
                candidateTemplate,
                "GeurtsTechniques",
                "GeurtsDocumentationCompanionContract.json");
            File.WriteAllText(contract, File.ReadAllText(contract).Replace(
                "\"packageVersion\": \"0.11.0\"",
                "\"packageVersion\": \"0.12.0\""));

            Assert.ThrowsAsync<InvalidDataException>(async () =>
                await service.PrepareLatestAsync(CancellationToken.None));
            Assert.That(Directory.Exists(Path.Combine(projectRoot, "GeurtsGameForgeDocumentation")), Is.False);
        }

        [Test]
        public async Task CommitPreferenceFailureDoesNotFailOrUndoVerifiedContentUpdate()
        {
            ThrowingCommitStore failingStore = new ThrowingCommitStore();
            DocumentationUpdateService targetService = new DocumentationUpdateService(
                projectRoot,
                transport,
                failingStore);

            DocumentationApplyResult result;
            using (PreparedDocumentationUpdate prepared =
                   await targetService.PrepareLatestAsync(CancellationToken.None))
            {
                result = targetService.Apply(prepared);
            }

            Assert.That(result.CommitPersisted, Is.False);
            Assert.That(result.Warning, Does.Contain("updated successfully"));
            Assert.That(result.Warning, Does.Contain("may offer the same update again"));
            Assert.That(
                File.ReadAllText(Path.Combine(projectRoot, "GeurtsGameForgeDocumentation", "Guide.md")),
                Is.EqualTo("new documentation"));
            Assert.That(File.ReadAllText(Path.Combine(projectRoot, "AGENTS.md")), Is.EqualTo("new route: AGENTS.md"));
        }

        [Test]
        public async Task ReadOnlyManagedAiFileIsDirectlyReplacedAfterConfirmation()
        {
            string agentsPath = Path.Combine(projectRoot, "AGENTS.md");
            File.WriteAllText(agentsPath, "old route");
            File.SetAttributes(agentsPath, File.GetAttributes(agentsPath) | FileAttributes.ReadOnly);

            await InstallAsync(service);

            Assert.That(File.ReadAllText(agentsPath), Is.EqualTo("new route: AGENTS.md"));
        }

        [Test]
        public async Task NewerPackageVersionAndAdditionalValidationEntryRemainSupported()
        {
            string contractPath = Path.Combine(
                candidateTemplate,
                "GeurtsTechniques",
                "GeurtsDocumentationCompanionContract.json");
            string contract = File.ReadAllText(contractPath)
                .Replace("\"packageVersion\": \"0.11.0\"", "\"packageVersion\": \"0.12.0\"")
                .Replace(
                    "\"Tools/AIAgentInstructionTemplates/instructions/geurts-game-design.instructions.md\"\n  ],\n  \"updateUi\"",
                    "\"Tools/AIAgentInstructionTemplates/instructions/geurts-game-design.instructions.md\",\n    \"Guide.md\"\n  ],\n  \"updateUi\"");
            File.WriteAllText(contractPath, contract);

            string manifestPath = Path.Combine(candidateTemplate, "GeurtsTechniqueManifest.md");
            File.WriteAllText(
                manifestPath,
                File.ReadAllText(manifestPath).Replace(
                    "**Version:** 0.11.0",
                    "**Version:** 0.12.0"));

            await InstallAsync(service);

            Assert.That(
                File.ReadAllText(Path.Combine(projectRoot, "GeurtsGameForgeDocumentation", "Guide.md")),
                Is.EqualTo("new documentation"));
        }

        [Test]
        public async Task LaterValidationListMayReplaceAnInitialReleaseEntry()
        {
            string contractPath = Path.Combine(
                candidateTemplate,
                "GeurtsTechniques",
                "GeurtsDocumentationCompanionContract.json");
            File.WriteAllText(
                contractPath,
                File.ReadAllText(contractPath).Replace(
                    "\"GeurtsTechniques/GeurtsDocumentationCompanionTechnique.md\"",
                    "\"Guide.md\""));

            await InstallAsync(service);

            Assert.That(
                File.ReadAllText(Path.Combine(projectRoot, "GeurtsGameForgeDocumentation", "Guide.md")),
                Is.EqualTo("new documentation"));
        }

        [Test]
        public void UnknownContractFieldFailsBeforeAnyProjectMutation()
        {
            string contractPath = Path.Combine(
                candidateTemplate,
                "GeurtsTechniques",
                "GeurtsDocumentationCompanionContract.json");
            File.WriteAllText(
                contractPath,
                File.ReadAllText(contractPath).Replace(
                    "{\n  \"schemaVersion\"",
                    "{\n  \"contractVersion\": \"1.0.0\",\n  \"schemaVersion\""));

            string existingAgents = Path.Combine(projectRoot, "AGENTS.md");
            File.WriteAllText(existingAgents, "untouched");

            InvalidDataException exception = Assert.ThrowsAsync<InvalidDataException>(async () =>
                await service.PrepareLatestAsync(CancellationToken.None));

            Assert.That(exception.Message, Does.Contain("Unknown contract field"));
            Assert.That(File.ReadAllText(existingAgents), Is.EqualTo("untouched"));
            Assert.That(Directory.Exists(Path.Combine(projectRoot, "GeurtsGameForgeDocumentation")), Is.False);
        }

        [Test]
        public void BackslashValidationPathFailsBeforeAnyProjectMutation()
        {
            string contractPath = Path.Combine(
                candidateTemplate,
                "GeurtsTechniques",
                "GeurtsDocumentationCompanionContract.json");
            File.WriteAllText(
                contractPath,
                File.ReadAllText(contractPath).Replace(
                    "GeurtsTechniques/GeurtsDocumentationCompanionTechnique.md",
                    "GeurtsTechniques\\\\GeurtsDocumentationCompanionTechnique.md"));

            InvalidDataException exception = Assert.ThrowsAsync<InvalidDataException>(async () =>
                await service.PrepareLatestAsync(CancellationToken.None));
            Assert.That(exception.Message, Does.Contain("use '/' separators"));
            Assert.That(Directory.Exists(Path.Combine(projectRoot, "GeurtsGameForgeDocumentation")), Is.False);
        }

        [Test]
        public void GitMetadataFileFailsBeforeAnyProjectMutation()
        {
            File.WriteAllText(Path.Combine(candidateTemplate, ".git"), "gitdir: elsewhere");

            InvalidDataException exception = Assert.ThrowsAsync<InvalidDataException>(async () =>
                await service.PrepareLatestAsync(CancellationToken.None));
            Assert.That(exception.Message, Does.Contain("Git metadata"));
            Assert.That(Directory.Exists(Path.Combine(projectRoot, "GeurtsGameForgeDocumentation")), Is.False);
        }

        [Test]
        public async Task ManagedHardLinkCannotRedirectAWriteOutsideTheProject()
        {
            string agentsPath = Path.Combine(projectRoot, "AGENTS.md");
            string escapedPath = Path.Combine(temporaryRoot, "escaped-agents.md");
            File.WriteAllText(escapedPath, "outside content");
            bool created = CreateHardLink(agentsPath, escapedPath, IntPtr.Zero);
            Assert.That(
                created,
                Is.True,
                "The hard-link test fixture could not be created. Win32 error: " + Marshal.GetLastWin32Error());

            using (PreparedDocumentationUpdate prepared =
                   await service.PrepareLatestAsync(CancellationToken.None))
            {
                service.Apply(prepared);
            }

            Assert.That(File.ReadAllText(escapedPath), Is.EqualTo("outside content"));
            Assert.That(File.ReadAllText(agentsPath), Is.EqualTo("new route: AGENTS.md"));
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateHardLinkW")]
        private static extern bool CreateHardLink(
            string fileName,
            string existingFileName,
            IntPtr securityAttributes);

        private async Task InstallAsync(DocumentationUpdateService targetService)
        {
            using (PreparedDocumentationUpdate prepared =
                   await targetService.PrepareLatestAsync(CancellationToken.None))
            {
                targetService.Apply(prepared);
            }
        }

        private string ReadInstalledCommit()
        {
            return new DocumentationInstaller(projectRoot).ReadLastSuccessfulCommit();
        }

        private static Dictionary<string, FileSnapshot> CaptureManagedFiles(string root)
        {
            List<string> paths = new List<string>
            {
                "GeurtsGameForgeDocumentation/Guide.md"
            };
            paths.AddRange(DocumentationPackageConstants.ExpectedManagedAiRoutes.Select(route => route.Destination));

            return paths.ToDictionary(
                path => path,
                path => FileSnapshot.Capture(DocumentationFileOperations.GetSafeFullPath(root, path)),
                StringComparer.Ordinal);
        }

        private static void AssertSnapshotsEqual(
            IReadOnlyDictionary<string, FileSnapshot> expected,
            IReadOnlyDictionary<string, FileSnapshot> actual)
        {
            Assert.That(actual.Keys, Is.EquivalentTo(expected.Keys));
            foreach (string path in expected.Keys)
            {
                Assert.That(actual[path].Bytes, Is.EqualTo(expected[path].Bytes), path);
                Assert.That(actual[path].LastWriteUtc, Is.EqualTo(expected[path].LastWriteUtc), path);
                Assert.That(actual[path].Attributes, Is.EqualTo(expected[path].Attributes), path);
            }
        }

        private static string Commit(char character)
        {
            return new string(character, 40);
        }

        private sealed class FixtureTransport : IDocumentationTransport
        {
            private readonly string templateRoot;

            internal FixtureTransport(string templateRoot, string headCommit)
            {
                this.templateRoot = templateRoot;
                HeadCommit = headCommit;
            }

            internal string HeadCommit { get; set; }
            internal Exception ResolveFailure { get; set; }
            internal Exception DownloadFailure { get; set; }
            internal int ResolveCalls { get; private set; }
            internal int DownloadCalls { get; private set; }

            public Task<string> ResolveHeadCommitAsync(CancellationToken cancellationToken)
            {
                ResolveCalls++;
                if (ResolveFailure != null)
                {
                    throw ResolveFailure;
                }

                return Task.FromResult(HeadCommit);
            }

            public Task<DocumentationDownload> DownloadCommitAsync(
                string commit,
                string workingDirectory,
                CancellationToken cancellationToken)
            {
                DownloadCalls++;
                if (DownloadFailure != null)
                {
                    throw DownloadFailure;
                }

                string candidate = Path.Combine(workingDirectory, "extracted");
                DocumentationFileOperations.CopyDirectory(templateRoot, candidate);
                return Task.FromResult(new DocumentationDownload(workingDirectory, candidate));
            }

            internal void ResetCounts()
            {
                ResolveCalls = 0;
                DownloadCalls = 0;
            }
        }

        private sealed class ThrowingCommitStore : IProjectInstallCommitStore
        {
            public string Read()
            {
                return null;
            }

            public void Write(string commit)
            {
                throw new IOException("simulated preference failure");
            }
        }

        private sealed class FileSnapshot
        {
            private FileSnapshot(byte[] bytes, DateTime lastWriteUtc, FileAttributes attributes)
            {
                Bytes = bytes;
                LastWriteUtc = lastWriteUtc;
                Attributes = attributes;
            }

            internal byte[] Bytes { get; }
            internal DateTime LastWriteUtc { get; }
            internal FileAttributes Attributes { get; }

            internal static FileSnapshot Capture(string path)
            {
                return new FileSnapshot(
                    File.ReadAllBytes(path),
                    File.GetLastWriteTimeUtc(path),
                    File.GetAttributes(path));
            }
        }

        private static class CandidateFixture
        {
            internal static void Write(string root, string guideText, string routePrefix)
            {
                Dictionary<string, string> files = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["AGENTS.md"] = "# Documentation package entry",
                    ["AI_READ_FIRST.md"] = "Read GeurtsTechniqueManifest.md next.",
                    ["README.md"] = "**Version:** 0.11.0",
                    ["Guide.md"] = guideText,
                    ["GeurtsTechniques/GeurtsDocumentationCompanionTechnique.md"] = "# Companion technique",
                    ["GeurtsTechniques/GeurtsDocumentationCompanionContract.json"] = ContractJson,
                    ["Tools/AIAgentInstructionTemplates/AGENTS.md"] = routePrefix + ": AGENTS.md",
                    ["Tools/AIAgentInstructionTemplates/copilot-instructions.md"] = routePrefix + ": copilot-instructions.md",
                    ["Tools/AIAgentInstructionTemplates/instructions/geurts-unity.instructions.md"] = routePrefix + ": geurts-unity.instructions.md",
                    ["Tools/AIAgentInstructionTemplates/instructions/geurts-game-design.instructions.md"] = routePrefix + ": geurts-game-design.instructions.md"
                };

                List<string> registeredPaths = files.Keys.ToList();
                registeredPaths.Add("GeurtsTechniqueManifest.md");
                files["GeurtsTechniqueManifest.md"] = BuildManifest(registeredPaths);
                foreach (KeyValuePair<string, string> file in files)
                {
                    DocumentationFileOperations.WriteUtf8(
                        DocumentationFileOperations.GetSafeFullPath(root, file.Key),
                        file.Value);
                }
            }

            private static string BuildManifest(IEnumerable<string> paths)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("# Manifest");
                builder.AppendLine("**Version:** 0.11.0");
                builder.AppendLine("<!-- GEURTS-PACKAGE-FILES:BEGIN -->");
                builder.AppendLine("| Path | Version | Role |");
                builder.AppendLine("|---|---:|---|");
                foreach (string path in paths.OrderBy(path => path, StringComparer.Ordinal))
                {
                    builder.Append("| `").Append(path).AppendLine("` | 0.11.0 | Test fixture. |");
                }

                builder.AppendLine("<!-- GEURTS-PACKAGE-FILES:END -->");
                return builder.ToString();
            }

            private const string ContractJson = @"{
  ""schemaVersion"": ""1.0.0"",
  ""packageVersion"": ""0.11.0"",
  ""source"": {
    ""repository"": ""https://github.com/Geurtsy/GeurtsGameForge_Documentation.git"",
    ""branch"": ""main"",
    ""selection"": ""exact-resolved-head-commit-archive""
  },
  ""destination"": {
    ""projectRelativePath"": ""GeurtsGameForgeDocumentation"",
    ""replacement"": ""complete-directory"",
    ""access"": ""logically-read-only""
  },
  ""validationEntries"": [
    ""AGENTS.md"",
    ""AI_READ_FIRST.md"",
    ""GeurtsTechniqueManifest.md"",
    ""GeurtsTechniques/GeurtsDocumentationCompanionTechnique.md"",
    ""GeurtsTechniques/GeurtsDocumentationCompanionContract.json"",
    ""Tools/AIAgentInstructionTemplates/AGENTS.md"",
    ""Tools/AIAgentInstructionTemplates/copilot-instructions.md"",
    ""Tools/AIAgentInstructionTemplates/instructions/geurts-unity.instructions.md"",
    ""Tools/AIAgentInstructionTemplates/instructions/geurts-game-design.instructions.md""
  ],
  ""updateUi"": {
    ""actionLabel"": ""Update Geurts Game Forge Documentation"",
    ""confirmationDefault"": ""cancel"",
    ""cancelResult"": ""no-network-or-filesystem-change"",
    ""confirmationTargets"": [
      { ""path"": ""GeurtsGameForgeDocumentation"", ""effect"": ""replace-complete-directory"" },
      { ""path"": ""AGENTS.md"", ""effect"": ""replace-complete-file"" },
      { ""path"": "".github/copilot-instructions.md"", ""effect"": ""replace-complete-file"" },
      { ""path"": "".github/instructions/geurts-unity.instructions.md"", ""effect"": ""replace-complete-file"" },
      { ""path"": "".github/instructions/geurts-game-design.instructions.md"", ""effect"": ""replace-complete-file"" }
    ]
  },
  ""routeMappings"": [
    { ""template"": ""Tools/AIAgentInstructionTemplates/AGENTS.md"", ""target"": ""AGENTS.md"" },
    { ""template"": ""Tools/AIAgentInstructionTemplates/copilot-instructions.md"", ""target"": "".github/copilot-instructions.md"" },
    { ""template"": ""Tools/AIAgentInstructionTemplates/instructions/geurts-unity.instructions.md"", ""target"": "".github/instructions/geurts-unity.instructions.md"" },
    { ""template"": ""Tools/AIAgentInstructionTemplates/instructions/geurts-game-design.instructions.md"", ""target"": "".github/instructions/geurts-game-design.instructions.md"" }
  ]
}";
        }
    }
}
