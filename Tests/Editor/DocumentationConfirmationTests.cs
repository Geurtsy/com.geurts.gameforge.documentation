// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Geurts.GameForge.Documentation.Tests
{
    internal sealed class DocumentationConfirmationTests
    {
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator DashboardClickAcceptInstallsOnceAndThenReportsCurrent()
        {
            string root = Path.Combine(Path.GetTempPath(), "GeurtsConfirmation-" + Guid.NewGuid().ToString("N"));
            string project = Path.Combine(root, "Project");
            string candidate = Path.Combine(root, "Candidate");
            Directory.CreateDirectory(candidate);
            Directory.CreateDirectory(Path.Combine(project, "Assets"));
            DocumentationUpdateServiceTests.CandidateFixture.Write(candidate, "confirmed documentation", "confirmed route");
            var transport = new ConfirmationTransport(candidate);
            var service = new DocumentationUpdateService(project, transport);
            DocumentationUpdateService originalService = DocumentationUpdaterController.Service;
            Type windowType = typeof(DocumentationUpdateService).Assembly.GetType("Geurts.GameForge.Documentation.DocumentationUpdaterWindow", true);
            EditorWindow window = null;
            int confirmations = 0;
            bool focusedCancel = false;
            bool confirmedOutsideGui = false;
            try
            {
                DocumentationUpdaterController.Service = service;
                windowType.GetField("OpenCheckForTests", Static).SetValue(null, (Func<Task>)(() => Task.CompletedTask));
                DocumentationUpdateConfirmation.OpenedForTests = confirmation =>
                {
                    confirmations++;
                    Button cancel = confirmation.rootVisualElement.Q<Button>("cancel-update");
                    focusedCancel = confirmation.rootVisualElement.focusController.focusedElement == cancel;
                    // A UI Toolkit click exercises the actual accept control and closes the real modal window.
                    Click(confirmation.rootVisualElement.Q<Button>("confirm-update"));
                    if (confirmation != null) confirmation.rootVisualElement.schedule.Execute(() =>
                    {
                        if (confirmation != null) confirmation.Close();
                    }).StartingIn(500); // Fail the assertions rather than leave a modal open if the click is broken.
                };
                window = (EditorWindow)ScriptableObject.CreateInstance(windowType);
                window.ShowUtility();
                window.position = new Rect(100, 100, 650, 900);
                double deadline = EditorApplication.timeSinceStartup + 5;
                Rect button = default;
                while (button.width == 0 && EditorApplication.timeSinceStartup < deadline)
                {
                    window.Repaint();
                    yield return null;
                    button = (Rect)windowType.GetProperty("DocumentationUpdateButtonRect", Instance).GetValue(window);
                }
                Assert.That(button.width, Is.GreaterThan(0), "The documentation update button must be drawn.");
                LogAssert.Expect(LogType.Log, "[Geurts Documentation] Update completed at commit " + ConfirmationTransport.Commit + ".");
                window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = button.center });
                window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = button.center });
                Assert.That(confirmations, Is.Zero, "Opening a modal inside the dashboard draw corrupts Odin's layout stack.");
                deadline = EditorApplication.timeSinceStartup + 8;
                while ((transport.Downloads == 0 || DocumentationUpdaterController.IsBusy) && EditorApplication.timeSinceStartup < deadline)
                    yield return null;
                confirmedOutsideGui = transport.OutsideGui;
                Assert.That(confirmations, Is.EqualTo(1));
                Assert.That(focusedCancel, Is.True);
                Assert.That(transport.Downloads, Is.EqualTo(1), DocumentationUpdaterController.StatusMessage);
                Assert.That(confirmedOutsideGui, Is.True, "The accepted update must run after the modal event loop and GUI callback.");
                Assert.That(DocumentationUpdaterController.Availability, Is.EqualTo(DocumentationAvailability.Current));
                Assert.That(File.ReadAllText(Path.Combine(project, "GeurtsGameForgeDocumentation", "Guide.md")), Is.EqualTo("confirmed documentation"));
                foreach (ManagedAiRoute route in DocumentationPackageConstants.ExpectedManagedAiRoutes)
                    Assert.That(File.ReadAllText(Path.Combine(project, route.Destination)),
                        Is.EqualTo(File.ReadAllText(Path.Combine(candidate, route.Source))));
                Assert.That(new ProjectInstallCommitStore(project).Read(), Is.EqualTo(ConfirmationTransport.Commit));
                Task refresh = DocumentationUpdaterController.CheckForUpdatesAsync(false);
                while (!refresh.IsCompleted) yield return null;
                Assert.That(DocumentationUpdaterController.Availability, Is.EqualTo(DocumentationAvailability.Current));
                Assert.That(DocumentationUpdaterController.Status.InstalledVersion, Is.EqualTo("0.11.0"));
                Assert.That(DocumentationUpdaterController.Status.AvailableVersion, Is.EqualTo("0.11.0"));
                Assert.That(transport.Downloads, Is.EqualTo(1));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                DocumentationUpdateConfirmation.OpenedForTests = null;
                windowType.GetField("OpenCheckForTests", Static).SetValue(null, null);
                if (window != null) window.Close();
                DocumentationUpdaterController.Service = originalService;
                new ProjectInstallCommitStore(project).ClearForTests();
                DocumentationFileOperations.DeleteDirectoryBestEffort(root);
            }
        }

        [TestCase("cancel")]
        [TestCase("close")]
        [TestCase("Return")]
        [TestCase("KeypadEnter")]
        [TestCase("Escape")]
        public void DecliningTheRealConfirmationDoesNotApproveAnUpdate(string action)
        {
            try
            {
                DocumentationUpdateConfirmation.OpenedForTests = window =>
                {
                    if (action == "cancel") Click(window.rootVisualElement.Q<Button>("cancel-update"));
                    else if (action != "close")
                    {
                        using (KeyDownEvent key = KeyDownEvent.GetPooled('\0', (KeyCode)Enum.Parse(typeof(KeyCode), action), EventModifiers.None))
                        {
                            key.target = window.rootVisualElement;
                            window.rootVisualElement.SendEvent(key);
                        }
                    }
                    if (window != null) window.Close();
                };
                Assert.That(DocumentationUpdateConfirmation.Confirm(), Is.False);
            }
            finally { DocumentationUpdateConfirmation.OpenedForTests = null; }
        }

        private static void Click(Button button)
        {
            // Send the same pointer events handled by the Button's Clickable manipulator.
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

        private sealed class ConfirmationTransport : IDocumentationTransport
        {
            internal const string Commit = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
            private readonly string _candidate;
            internal int Downloads { get; private set; }
            internal bool OutsideGui { get; private set; }
            internal ConfirmationTransport(string candidate) { _candidate = candidate; }
            public Task<string> ResolveHeadCommitAsync(CancellationToken token) => Task.FromResult(Commit);
            public Task<string> ReadVersionAsync(string commit, CancellationToken token) => Task.FromResult("0.11.0");
            public Task<DocumentationDownload> DownloadCommitAsync(string commit, string workingDirectory, CancellationToken token,
                Action<UpdateProgress> progress = null)
            {
                Downloads++;
                OutsideGui = Event.current == null;
                string candidate = Path.Combine(workingDirectory, "extracted");
                DocumentationFileOperations.CopyDirectory(_candidate, candidate);
                return Task.FromResult(new DocumentationDownload(workingDirectory, candidate));
            }
        }
    }
}
