// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#else
using UnityEngine.UIElements;
#endif

namespace Geurts.GameForge.Documentation
{
    /// <summary>Odin dashboard for the documentation tools and their current source status.</summary>
    internal sealed class DocumentationUpdaterWindow :
#if ODIN_INSPECTOR
        OdinEditorWindow
#else
        EditorWindow
#endif
    {
        private static bool _openingAfterCheck;
        internal static System.Func<System.Threading.Tasks.Task> OpenCheckForTests;

        // Modal windows and package operations must start after the current Odin/IMGUI draw has finished.
        internal static void DeferAction(System.Action action)
        {
            EditorApplication.delayCall += () => action();
        }

        internal static void ShowAfterCheck()
        {
            _openingAfterCheck = true;
            try { ShowWindow(); }
            finally { _openingAfterCheck = false; }
        }

        private async void CheckOnOpen()
        {
            if (this == null) return;
            if (PackageSelfUpdater.instance.IsInstalling || DocumentationUpdaterController.IsInstalling)
            {
                EditorApplication.delayCall += CheckOnOpen;
                return;
            }
            await (OpenCheckForTests != null ? OpenCheckForTests() : DocumentationUpdateChecks.CheckAllAsync());
        }

        private void ScheduleOpenCheck()
        {
            if (!_openingAfterCheck && (!Application.isBatchMode || OpenCheckForTests != null))
                EditorApplication.delayCall += CheckOnOpen;
        }

        private const string MenuPath = "Tools/Geurts Game Forge/Documentation";

        [MenuItem(MenuPath)]
        internal static void ShowWindow()
        {
            DocumentationUpdaterWindow window = GetWindow<DocumentationUpdaterWindow>();
            window.titleContent = new GUIContent("Geurts Documentation", EditorGUIUtility.IconContent("TextAsset Icon").image);
            window.minSize = new Vector2(540f, 560f);
            window.Show();
        }

#if ODIN_INSPECTOR
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _eyebrowStyle;
        private GUIStyle _statusTitleStyle;
        private bool _dependencyCheckWasBusy;
        internal Rect DocumentationUpdateButtonRect { get; private set; }

        private bool IsBusy => DocumentationUpdaterController.IsBusy || PackageSelfUpdater.instance.IsBusy || PackageSelfUpdater.EditorBusy;
        private bool CanUpdatePackage => PackageSelfUpdater.instance.CanUpdate;

        protected override void OnEnable()
        {
            base.OnEnable();
            WindowPadding = new Vector4(20f, 20f, 16f, 16f);
            // Removing first also makes subscription safe across repeated enable calls.
            DocumentationUpdaterController.Changed -= Repaint;
            DocumentationUpdaterController.Changed += Repaint;
            PackageSelfUpdater.Changed -= Repaint;
            PackageSelfUpdater.Changed += Repaint;
            DependencyInstallation.Changed -= Repaint;
            DependencyInstallation.Changed += Repaint;
            EditorApplication.update -= RepaintWhileBusy;
            EditorApplication.update += RepaintWhileBusy;
            ScheduleOpenCheck();
        }

        protected override void OnDisable()
        {
            DocumentationUpdaterController.Changed -= Repaint;
            PackageSelfUpdater.Changed -= Repaint;
            DependencyInstallation.Changed -= Repaint;
            EditorApplication.update -= RepaintWhileBusy;
            EditorApplication.delayCall -= CheckOnOpen;
            base.OnDisable();
        }

        private void RepaintWhileBusy()
        {
            bool checkingDependencies = EditorApplication.isCompiling || EditorApplication.isUpdating;
            if (DocumentationUpdaterController.IsBusy || PackageSelfUpdater.instance.IsBusy ||
                checkingDependencies || _dependencyCheckWasBusy) Repaint();
            _dependencyCheckWasBusy = checkingDependencies;
        }

        [OnInspectorGUI, PropertyOrder(-20)]
        private void DrawOverview()
        {
            EnsureStyles();
            GUILayout.Label("GEURTS GAME FORGE", _eyebrowStyle);
            GUILayout.Label("Documentation", _titleStyle);
            GUILayout.Label("Installed versions and the latest from Git, in one place.", _bodyStyle);
            GUILayout.Space(8f);
            GUILayout.Label("Both sources are checked automatically whenever this window opens.", _bodyStyle);
        }

        [Button("Check for updates", ButtonSizes.Medium), PropertyOrder(-10), DisableIf(nameof(IsBusy))]
        private async void CheckForUpdates()
        {
            await DocumentationUpdateChecks.CheckAllAsync();
        }

        [OnInspectorGUI, PropertyOrder(0)]
        private void DrawDocumentationCard()
        {
            DrawUpdateCard("Documentation update", DocumentationUpdaterController.Status,
                DocumentationUpdaterController.IsInstalling,
                "Checks the official documentation repository on main.",
                "Replaces the shared documentation and four AI instruction files after confirmation.",
                DocumentationPackageConstants.UpdateActionLabel, !IsBusy,
                DocumentationUpdaterController.ConfirmAndUpdate);
        }

        [OnInspectorGUI, PropertyOrder(10)]
        private void DrawPackageCard()
        {
            PackageSelfUpdater updater = PackageSelfUpdater.instance;
            DrawUpdateCard("Package update", updater.Status, updater.IsInstalling,
                updater.Installed?.source == UnityEditor.PackageManager.PackageSource.Git
                    ? "Checks your configured Git source. Branches, tags and pinned commits are preserved."
                    : "Available version: official Git repository. Local installations have no Git commit to compare.",
                updater.GitReference == null ? updater.SourceDescription
                    : "Updates this editor package through Unity. Scripts may recompile.",
                PackageSelfUpdater.ActionLabel, CanUpdatePackage, updater.BeginUpdate);
            if (updater.HasUpdateResult && !updater.IsBusy)
                EditorGUILayout.HelpBox(updater.StatusMessage, updater.Failed ? MessageType.Error : MessageType.Info);
        }

        private void DrawUpdateCard(string title, UpdateStatus status, bool installing,
            string source, string explanation, string actionLabel, bool enabled, System.Action action)
        {
            EnsureStyles();
            GUILayout.Space(12f);
            bool busy = installing || status.IsChecking;
            Color accent = busy ? DashboardColours.Working
                : status.Failed ? DashboardColours.Failed
                : status.Availability == DocumentationAvailability.UpdateAvailable ? DashboardColours.Attention
                : status.Availability == DocumentationAvailability.Current
                    ? DashboardColours.Ready : DashboardColours.Unknown;
            Rect card = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(card, DashboardColours.Tint(accent));
                EditorGUI.DrawRect(new Rect(card.x, card.y, 4f, card.height), accent);
            }
            GUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            EditorGUILayout.BeginVertical();
            GUILayout.Label(title, _statusTitleStyle);
            string badge = installing ? "INSTALLING" : status.IsChecking ? "CHECKING GIT"
                : status.Failed ? "NEEDS ATTENTION"
                : status.Availability == DocumentationAvailability.UpdateAvailable ? "UPDATE AVAILABLE"
                : status.Availability == DocumentationAvailability.Current ? "UP TO DATE" : "STATUS UNKNOWN";
            Color previous = GUI.contentColor;
            GUI.contentColor = accent;
            GUILayout.Label(badge, _eyebrowStyle);
            GUI.contentColor = previous;
            GUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            DrawVersion("INSTALLED", status.InstalledVersion, status.InstalledCommit);
            GUILayout.Space(12f);
            DrawVersion("AVAILABLE ON GIT", status.AvailableVersion, status.RemoteCommit);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(7f);
            GUILayout.Label(source, _bodyStyle);
            GUILayout.Space(7f);
            if (busy) DrawProgress(status.Progress, accent);
            GUILayout.Label(busy ? status.Progress?.Message ?? status.Message : status.Message, _bodyStyle);
            if (!busy) GUILayout.Label("Last check: " + status.LastChecked, EditorStyles.miniLabel);
            GUILayout.Space(8f);
            GUILayout.Label(explanation, _bodyStyle);
            GUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(!enabled))
            {
                if (GUILayout.Button(actionLabel, GUILayout.Height(34f))) DeferAction(action);
                if (Event.current.type == EventType.Repaint && actionLabel == DocumentationPackageConstants.UpdateActionLabel)
                    DocumentationUpdateButtonRect = GUILayoutUtility.GetLastRect();
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(10f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10f);
            EditorGUILayout.EndVertical();
        }

        private void DrawVersion(string label, string value, string commit)
        {
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(0f));
            GUILayout.Label(label, _eyebrowStyle);
            GUILayout.Label(value, _statusTitleStyle);
            GUILayout.Label(string.IsNullOrEmpty(commit) ? "Revision not recorded" : "Revision " + commit.Substring(0, System.Math.Min(8, commit.Length)),
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private static void DrawProgress(UpdateProgress progress, Color accent)
        {
            Rect bar = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bar, EditorGUIUtility.isProSkin ? new Color(0.12f, 0.13f, 0.15f) : new Color(0.8f, 0.82f, 0.84f));
            float fraction = progress?.Fraction ?? -1f;
            Rect fill = bar;
            if (fraction < 0f)
            {
                fill.width *= 0.23f;
                fill.x += (bar.width - fill.width) * (float)(0.5 + 0.5 * System.Math.Sin(EditorApplication.timeSinceStartup * 2.5));
            }
            else fill.width *= Mathf.Clamp01(fraction);
            EditorGUI.DrawRect(fill, new Color(accent.r, accent.g, accent.b, 0.65f));
            GUI.Label(bar, fraction < 0f ? "Working..." : Mathf.RoundToInt(Mathf.Clamp01(fraction) * 100f) + "% downloaded",
                EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(6f);
        }

        [OnInspectorGUI, PropertyOrder(15)]
        private void DrawDependencySpacing()
        {
            GUILayout.Space(12f);
        }

        // Required dependency status stays visible even when there is nothing to install.
        [BoxGroup("Dependencies", order: 20), OnInspectorGUI, PropertyOrder(0)]
        private void DrawDependencies()
        {
            EnsureStyles();
            GUILayout.Label("Required tools · licensed and installed separately", _bodyStyle);
            foreach (string tool in DocumentationDependencies.RequiredExternalTools)
                DrawDependencyCard(tool);
        }

        private void DrawDependencyCard(string tool)
        {
            var status = DocumentationDependencies.ToolStatus(tool);
            Color accent = status.Background;
            GUILayout.Space(8f);
            Rect card = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(card, DashboardColours.Tint(accent));
                EditorGUI.DrawRect(new Rect(card.x, card.y, 4f, card.height), accent);
            }
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12f);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(8f);
            GUILayout.Label(tool, _statusTitleStyle);
            Color previous = GUI.contentColor;
            GUI.contentColor = accent;
            GUILayout.Label(status.Message, _eyebrowStyle);
            GUI.contentColor = previous;
            GUILayout.Space(6f);
            GUILayout.Label(DocumentationDependencies.Description(tool), _bodyStyle);
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) DrawProgress(null, accent);
            string message = DependencyInstallation.Message(tool);
            if (!string.IsNullOrEmpty(message)) GUILayout.Label(message, _bodyStyle);
            GUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(DependencyInstallation.IsBusy))
            {
                if (GUILayout.Button("Download / import owned copy in My Assets", GUILayout.Height(30f)))
                    DeferAction(() => { DependencyInstallation.OpenOwnedAssets(tool); Repaint(); });
                if (GUILayout.Button("Import licensed .unitypackage…", GUILayout.Height(26f)))
                    DeferAction(() => { DependencyInstallation.ImportLicensedCopy(tool); Repaint(); });
            }
            if (tool == "Odin Inspector" && GUILayout.Button("Installation guide", EditorStyles.linkLabel))
                Application.OpenURL(DocumentationDependencies.OdinGuideUrl);
            GUILayout.Space(8f);
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        [OnInspectorGUI, PropertyOrder(25)]
        private void DrawGitIgnoreSpacing()
        {
            GUILayout.Space(12f);
        }

        // Secondary action card: installing ignore rules is independent of documentation Update.
        [BoxGroup("Git ignore rules", order: 30), OnInspectorGUI, PropertyOrder(0)]
        private void DrawGitIgnoreDescription()
        {
            EnsureStyles();
            GUILayout.Label("Use the approved Git ignore rules from your installed documentation.", _bodyStyle);
            GUILayout.Space(4f);
            GUILayout.Label("Replaces your project .gitignore and its custom rules after a separate confirmation.", _bodyStyle);
            GUILayout.Space(6f);
        }

        [BoxGroup("Git ignore rules"), PropertyOrder(1)]
        [Button(GitIgnoreInstaller.ActionLabel, ButtonSizes.Medium), DisableIf(nameof(IsBusy))]
        private void InstallGitIgnore()
        {
            DeferAction(GitIgnoreInstaller.ConfirmAndInstall);
        }

        [OnInspectorGUI, PropertyOrder(35)]
        private void DrawDetailsSpacing()
        {
            GUILayout.Space(12f);
        }

        // Advanced details are collapsed by default and derived from the supported contract constants.
        [FoldoutGroup("Source and managed files", expanded: false, order: 40)]
        [OnInspectorGUI]
        private void DrawSourceDetails()
        {
            EnsureStyles();
            GUILayout.Label("Authoritative source", EditorStyles.boldLabel);
            DrawSelectable(DocumentationPackageConstants.RepositoryUrl);
            GUILayout.Label("Package source", EditorStyles.boldLabel);
            DrawSelectable(PackageSelfUpdater.instance.SourceDescription);
            DrawCommit("Installed package commit", PackageSelfUpdater.instance.Status.InstalledCommit);
            DrawCommit("Available package commit", PackageSelfUpdater.instance.Status.RemoteCommit);
            GUILayout.Space(8f);
            GUILayout.Label("Branch: " + DocumentationPackageConstants.RepositoryBranch, _bodyStyle);
            GUILayout.Space(8f);
            DrawCommit("Latest source commit", DocumentationUpdaterController.RemoteCommit);
            DrawCommit("Last successful update", DocumentationUpdaterController.InstalledCommit);
            GUILayout.Label("Commit comparison does not check the contents of your local files.", _bodyStyle);
            GUILayout.Space(12f);
            GUILayout.Label("Replaced by documentation Update", EditorStyles.boldLabel);
            DrawSelectable(DocumentationPackageConstants.ManagedDocumentationDirectory + "/");
            foreach (ManagedAiRoute route in DocumentationPackageConstants.ExpectedManagedAiRoutes)
            {
                DrawSelectable(route.Destination);
            }
            GUILayout.Space(6f);
            GUILayout.Label("Docs/GameDesign/ and every unlisted path are outside this update.", _bodyStyle);
            GUILayout.Label("AI tools must support these instruction files or be told to read AGENTS.md.", _bodyStyle);
        }

        [OnInspectorGUI, PropertyOrder(50)]
        private void DrawFooter()
        {
            EnsureStyles();
            GUILayout.Space(12f);
            GUILayout.Label("Checks read version metadata only. You choose when to install updates.", _bodyStyle);
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }
            _titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 26, fixedHeight = 36f };
            _bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { fontSize = 12, richText = false };
            _eyebrowStyle = new GUIStyle(EditorStyles.miniBoldLabel) { fontSize = 10 };
            _statusTitleStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { fontSize = 14, fontStyle = FontStyle.Bold };
        }

        private static void DrawSelectable(string text)
        {
            EditorGUILayout.SelectableLabel(text, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        private void DrawCommit(string label, string commit)
        {
            GUILayout.Label(label, EditorStyles.boldLabel);
            DrawSelectable(string.IsNullOrWhiteSpace(commit) ? "Not recorded" : commit);
        }
#else
        private void OnEnable() => ScheduleOpenCheck();
        private void OnDisable() => EditorApplication.delayCall -= CheckOnOpen;

        // Odin is distributed separately; missing it must not create compilation errors or hide the menu.
        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            root.style.paddingLeft = root.style.paddingRight = 20f;
            root.style.paddingTop = root.style.paddingBottom = 20f;
            root.Add(new Label("GEURTS GAME FORGE") { style = { fontSize = 10 } });
            root.Add(new Label("Documentation") { style = { fontSize = 26, marginBottom = 16f } });
            root.Add(new Label("Dependencies") { style = { fontSize = 16, marginBottom = 8f } });
            root.Add(new Label(DocumentationDependencies.OdinStatus) { name = "odin-status", style = { marginBottom = 8f } });
            root.Add(new HelpBox(DocumentationDependencies.OdinDescription, HelpBoxMessageType.Error));
            var dependencies = new DocumentationPackageManagerExtension();
            root.Add(dependencies.CreateExtensionUI());
            dependencies.OnPackageSelectionChange(UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DocumentationUpdaterWindow).Assembly));
            root.Add(new Label("All documentation tools require Odin Inspector. Odin supplies the ODIN_INSPECTOR scripting symbol automatically.")
            {
                style = { whiteSpace = WhiteSpace.Normal, marginTop = 12f, marginBottom = 12f }
            });
            root.Add(new Button(() => Application.OpenURL(DocumentationDependencies.OdinGuideUrl))
            {
                text = "Odin Inspector installation guide",
                style = { height = 32f }
            });
        }
#endif
    }
}
