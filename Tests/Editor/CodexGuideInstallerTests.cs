// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Geurts.GameForge.Documentation.Tests
{
    internal sealed class CodexGuideInstallerTests
    {
        private string _root;
        private string _project;
        private string _destination;
        private string _documentation;

        /// <summary>Use the real documentation-owned template in a temporary project and separate destination.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "GeurtsCodexGuide-" + Guid.NewGuid().ToString("N"));
            _project = Path.Combine(_root, "Unity project with spaces");
            _destination = Path.Combine(_root, "User selected folder", "AGENTS.md");
            _documentation = Path.Combine(_project, DocumentationPackageConstants.ManagedDocumentationDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(_destination));
            Directory.CreateDirectory(Path.Combine(_documentation, "GeurtsTechniques"));
            string installedDocs = Path.Combine(Path.GetDirectoryName(Application.dataPath),
                DocumentationPackageConstants.ManagedDocumentationDirectory);
            foreach (string relative in new[] { "AI_READ_FIRST.md", "GeurtsTechniqueManifest.md", CodexGuideInstaller.TechniquePath })
                File.Copy(Path.Combine(installedDocs, relative), Path.Combine(_documentation, relative));
        }

        /// <summary>Remove only the test-owned temporary tree.</summary>
        [TearDown]
        public void TearDown() => DocumentationFileOperations.DeleteDirectoryBestEffort(_root);

        /// <summary>The external guide points to the exact Unity project, and an accepted repeat replaces it.</summary>
        [Test]
        public void SelectedExternalFolderReceivesTheExactEntryPointAndSupportsOverwrite()
        {
            Assert.That(CodexGuideInstaller.Install(_project, _destination, message =>
            {
                Assert.That(message, Does.Contain(_destination));
                Assert.That(message, Does.Contain(CodexGuideInstaller.GetEntryPoint(_project)));
                Assert.That(message, Does.Contain("overwrite all of its contents"));
                return true;
            }), Is.True);
            string guide = File.ReadAllText(_destination);
            Assert.That(guide, Does.Contain(CodexGuideInstaller.GetEntryPoint(_project)));
            Assert.That(guide, Does.Contain("GeurtsTechniqueManifest.md"));
            Assert.That(guide, Does.Contain("Every update, however small"));
            Assert.That(guide, Does.Not.Contain("{{"));
            Assert.That(File.Exists(Path.Combine(_project, "AGENTS.md")), Is.False);
            File.WriteAllText(_destination, "local changes");
            Assert.That(CodexGuideInstaller.Install(_project, _destination, _ => true), Is.True);
            Assert.That(File.ReadAllText(_destination), Is.EqualTo(guide));
        }

        /// <summary>Explicit selection can replace the old project-root route.</summary>
        [Test]
        public void SelectedProjectRootCanReplaceTheDeprecatedGuide()
        {
            string target = Path.Combine(_project, "AGENTS.md");
            File.WriteAllText(target, "Read GeurtsGameForgeDocumentation/AGENTS.md");
            Assert.That(CodexGuideInstaller.Install(_project, target, _ => true), Is.True);
            Assert.That(File.ReadAllText(target), Does.Contain(CodexGuideInstaller.GetEntryPoint(_project)));
            Assert.That(File.ReadAllText(target), Does.Not.Contain("Documentation/AGENTS.md"));
        }

        /// <summary>Declining replacement preserves exact existing bytes and timestamps.</summary>
        [TestCase(false)]
        [TestCase(true)]
        public void CancelDoesNotCreateOrChangeTheSelectedGuide(bool exists)
        {
            byte[] original = Encoding.Unicode.GetBytes("user-owned guide");
            if (exists) File.WriteAllBytes(_destination, original);
            DateTime timestamp = File.GetLastWriteTimeUtc(_destination);
            Assert.That(CodexGuideInstaller.Install(_project, _destination, _ => false), Is.False);
            Assert.That(File.Exists(_destination), Is.EqualTo(exists));
            if (exists)
            {
                Assert.That(File.ReadAllBytes(_destination), Is.EqualTo(original));
                Assert.That(File.GetLastWriteTimeUtc(_destination), Is.EqualTo(timestamp));
            }
            Assert.That(CodexGuideInstaller.Install(_project, "", _ => throw new Exception("Must not confirm")), Is.False);
        }

        /// <summary>Missing or malformed installed documentation cannot damage an existing guide.</summary>
        [TestCase("entry")]
        [TestCase("technique")]
        [TestCase("manifest")]
        [TestCase("placeholder")]
        [TestCase("duplicate")]
        public void InvalidDocumentationFailsBeforeConfirmationOrMutation(string error)
        {
            File.WriteAllText(_destination, "keep me");
            string technique = Path.Combine(_documentation, CodexGuideInstaller.TechniquePath);
            if (error == "entry") File.Delete(Path.Combine(_documentation, "AI_READ_FIRST.md"));
            else if (error == "technique") File.Delete(technique);
            else if (error == "manifest") File.WriteAllText(Path.Combine(_documentation, "GeurtsTechniqueManifest.md"), "# unrelated");
            else if (error == "placeholder") File.WriteAllText(technique, File.ReadAllText(technique).Replace(CodexGuideInstaller.EntryPlaceholder, "missing"));
            else File.AppendAllText(technique, File.ReadAllText(technique));
            bool confirmed = false;
            Assert.Catch<Exception>(() => CodexGuideInstaller.Install(_project, _destination, _ => confirmed = true));
            Assert.That(confirmed, Is.False);
            Assert.That(File.ReadAllText(_destination), Is.EqualTo("keep me"));
        }

        /// <summary>The guide must remain outside documentation replacement and project-authored design paths.</summary>
        [TestCase("GeurtsGameForgeDocumentation")]
        [TestCase("Docs/GameDesign")]
        public void ProtectedDestinationsAreRejected(string relative)
        {
            string folder = Path.Combine(_project, relative);
            Directory.CreateDirectory(folder);
            string target = Path.Combine(folder, "AGENTS.md");
            Assert.Throws<InvalidDataException>(() => CodexGuideInstaller.Install(_project, target, _ => true));
            Assert.That(File.Exists(target), Is.False);
        }

        /// <summary>An existing directory cannot be mistaken for a guide file.</summary>
        [Test]
        public void DirectoryAtTheSelectedFileIsPreserved()
        {
            Directory.CreateDirectory(_destination);
            Assert.Throws<IOException>(() => CodexGuideInstaller.Install(_project, _destination, _ => true));
            Assert.That(Directory.Exists(_destination), Is.True);
        }

        /// <summary>The real confirmation writes only after an explicit click, never on Enter, Escape or close.</summary>
        [TestCase("cancel")]
        [TestCase("close")]
        [TestCase("Return")]
        [TestCase("KeypadEnter")]
        [TestCase("Escape")]
        [TestCase("install")]
        public void RealConfirmationHonorsCancelAndExplicitInstall(string action)
        {
            File.WriteAllText(_destination, "original guide");
            bool cancelFocused = false;
            try
            {
                CodexGuideConfirmation.OpenedForTests = window =>
                {
                    VisualElement root = window.rootVisualElement;
                    cancelFocused = root.focusController.focusedElement == root.Q<Button>("cancel-install");
                    if (action == "install" || action == "cancel")
                    {
                        Button button = root.Q<Button>(action == "install" ? "confirm-install" : "cancel-install");
                        using (PointerDownEvent down = PointerDownEvent.GetPooled(new Event
                               { type = EventType.MouseDown, button = 0, mousePosition = button.worldBound.center }))
                        {
                            down.target = button;
                            button.SendEvent(down);
                        }
                        using (PointerUpEvent up = PointerUpEvent.GetPooled(new Event
                               { type = EventType.MouseUp, button = 0, mousePosition = button.worldBound.center }))
                        {
                            up.target = button;
                            button.SendEvent(up);
                        }
                    }
                    else if (action != "close")
                    {
                        using (KeyDownEvent key = KeyDownEvent.GetPooled('\0',
                               (KeyCode)Enum.Parse(typeof(KeyCode), action), EventModifiers.None))
                        {
                            key.target = root;
                            root.SendEvent(key);
                        }
                    }
                    if (window != null) window.Close();
                };
                Assert.That(CodexGuideInstaller.Install(_project, _destination, CodexGuideConfirmation.Confirm),
                    Is.EqualTo(action == "install"));
                Assert.That(cancelFocused, Is.True);
                if (action == "install")
                    Assert.That(File.ReadAllText(_destination), Does.Contain(CodexGuideInstaller.GetEntryPoint(_project)));
                else
                    Assert.That(File.ReadAllText(_destination), Is.EqualTo("original guide"));
            }
            finally { CodexGuideConfirmation.OpenedForTests = null; }
        }
    }
}
