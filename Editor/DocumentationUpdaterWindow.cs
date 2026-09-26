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
            DocumentationUpdaterWindow window = DocumentationEditorTheme.OpenWindow<DocumentationUpdaterWindow>(
                new Vector2(540f, 560f), "Geurts Documentation");
            window.titleContent = new GUIContent("Geurts Documentation", EditorGUIUtility.IconContent("TextAsset Icon").image);
            window.Show();
        }

#if ODIN_INSPECTOR
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _eyebrowStyle;
        private GUIStyle _statusTitleStyle;
        private GUIStyle _badgeStyle;
        private GUIStyle _progressStyle;
        private DocumentationEditorTheme _theme;
        private bool _dependencyCheckWasBusy;
        internal Rect DocumentationUpdateButtonRect { get; private set; }

        private bool IsBusy => DocumentationUpdaterController.IsBusy || PackageSelfUpdater.instance.IsBusy || PackageSelfUpdater.EditorBusy;
        private bool CanUpdatePackage => PackageSelfUpdater.instance.CanUpdate;

        protected override void OnEnable()
        {
            base.OnEnable();
            WindowPadding = new Vector4(20f, 20f, 16f, 16f);
            OnBeginGUI -= DrawCanvas;
            OnBeginGUI += DrawCanvas;
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
            OnBeginGUI -= DrawCanvas;
            _theme?.Dispose();
            _theme = null;
            _titleStyle = _bodyStyle = _eyebrowStyle = _statusTitleStyle = _progressStyle = _badgeStyle = null;
            base.OnDisable();
        }

        private void DrawCanvas()
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), DocumentationEditorTheme.Background);
        }

        // Odin retains the groups, validation and action drawers; temporary styles never escape this window.
        protected override void DrawEditors()
        {
            EnsureStyles();
            using (_theme.Scope()) base.DrawEditors();
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
            using (var header = new EditorGUILayout.VerticalScope(_theme.Card))
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(new Rect(header.rect.x, header.rect.y, 4f, header.rect.height), DocumentationEditorTheme.Green);
                GUILayout.Label("GEURTS  /  GAME FORGE", _eyebrowStyle);
                GUILayout.Label("DOCUMENTATION", _titleStyle);
                GUILayout.Label("Installed versions and the latest from Git, in one place.", _bodyStyle);
            }
            GUILayout.Space(12f);
        }

        [BoxGroup("Check for updates", order: -10), OnInspectorGUI, PropertyOrder(0)]
        private void DrawUpdateCheckDescription()
        {
            EnsureStyles();
            GUILayout.Label("Refresh the package and documentation versions from Git.", _bodyStyle);
            GUILayout.Label("Both sources are checked automatically whenever this window opens.", _bodyStyle);
            GUILayout.Space(6f);
        }

        [BoxGroup("Check for updates", order: -10)]
        [Button("Check for updates", ButtonSizes.Large), PropertyOrder(1), DisableIf(nameof(IsBusy))]
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
                "Replaces the shared documentation and three Copilot instruction files after confirmation.",
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
            Rect card = EditorGUILayout.BeginVertical(_theme.Card);
            if (Event.current.type == EventType.Repaint)
            {
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
            GUILayout.Label(badge, _badgeStyle);
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
            if (!busy) GUILayout.Label("Last check: " + status.LastChecked, _theme.Small);
            GUILayout.Space(8f);
            GUILayout.Label(explanation, _bodyStyle);
            GUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(!enabled))
            {
                if (GUILayout.Button(actionLabel, _theme.Button, GUILayout.Height(34f))) DeferAction(action);
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
                _theme.Small);
            EditorGUILayout.EndVertical();
        }

        private void DrawProgress(UpdateProgress progress, Color accent)
        {
            Rect bar = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bar, DocumentationEditorTheme.Background);
            float fraction = progress?.Fraction ?? -1f;
            Rect fill = bar;
            if (fraction < 0f)
            {
                fill.width *= 0.23f;
                fill.x += (bar.width - fill.width) * (float)(0.5 + 0.5 * System.Math.Sin(EditorApplication.timeSinceStartup * 2.5));
            }
            else fill.width *= Mathf.Clamp01(fraction);
            EditorGUI.DrawRect(fill, new Color(accent.r, accent.g, accent.b, 0.28f));
            GUI.Label(bar, fraction < 0f ? "Working..." : Mathf.RoundToInt(Mathf.Clamp01(fraction) * 100f) + "% downloaded",
                _progressStyle);
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
            Rect card = EditorGUILayout.BeginVertical(_theme.Card);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(card.x, card.y, 4f, card.height), accent);
            }
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12f);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(8f);
            GUILayout.Label(tool, _statusTitleStyle);
            Color previous = GUI.contentColor;
            GUI.contentColor = accent;
            GUILayout.Label(status.Message, _badgeStyle);
            GUI.contentColor = previous;
            GUILayout.Space(6f);
            GUILayout.Label(DocumentationDependencies.Description(tool), _bodyStyle);
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) DrawProgress(null, accent);
            string message = DependencyInstallation.Message(tool);
            if (!string.IsNullOrEmpty(message)) GUILayout.Label(message, _bodyStyle);
            GUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(DependencyInstallation.IsBusy))
            {
                if (GUILayout.Button("Download / import owned copy in My Assets", _theme.Button, GUILayout.Height(30f)))
                    DeferAction(() => { DependencyInstallation.OpenOwnedAssets(tool); Repaint(); });
                if (GUILayout.Button("Import licensed .unitypackage…", _theme.Button, GUILayout.Height(26f)))
                    DeferAction(() => { DependencyInstallation.ImportLicensedCopy(tool); Repaint(); });
            }
            if (tool == "Odin Inspector" && GUILayout.Button("Installation guide", _theme.Button))
                Application.OpenURL(DocumentationDependencies.OdinGuideUrl);
            GUILayout.Space(8f);
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static bool HasBuildForge => System.Type.GetType(
            "Geurts.GameForge.God.Editor.BuildForgeWindow, Geurts.GameForge.God.Editor") != null;

        [BoxGroup("Project setup", order: 25), ShowIf(nameof(HasBuildForge)), OnInspectorGUI, PropertyOrder(0)]
        private void DrawBuildForgeDescription()
        {
            EnsureStyles();
            GUILayout.Label("Build Forge guides you through Git ignore rules, the Codex guide, folders and bootstrap scenes, with a tick for each completed step.", _bodyStyle);
        }

        [BoxGroup("Project setup"), ShowIf(nameof(HasBuildForge)), PropertyOrder(1)]
        [Button("Open Build Forge", ButtonSizes.Large), DisableIf(nameof(IsBusy))]
        private void OpenBuildForge()
        {
            DeferAction(() => EditorApplication.ExecuteMenuItem("Tools/Geurts Game Forge/Build Forge"));
        }

        [OnInspectorGUI, PropertyOrder(25), HideIf(nameof(HasBuildForge))]
        private void DrawGitIgnoreSpacing()
        {
            GUILayout.Space(12f);
        }

        [BoxGroup("Codex guide", order: 26), HideIf(nameof(HasBuildForge)), OnInspectorGUI, PropertyOrder(0)]
        private void DrawCodexGuideDescription()
        {
            EnsureStyles();
            GUILayout.Label("Choose where to install a guide that points the AI directly to your installed Geurts documentation.", _bodyStyle);
            GUILayout.Label("Installs AGENTS.md for automatic Codex discovery. You choose the folder; existing contents are replaced after confirmation.", _bodyStyle);
            GUILayout.Space(6f);
        }

        [BoxGroup("Codex guide"), HideIf(nameof(HasBuildForge)), PropertyOrder(1)]
        [Button(CodexGuideInstaller.ActionLabel, ButtonSizes.Large), DisableIf(nameof(IsBusy))]
        private void InstallCodexGuide()
        {
            DeferAction(CodexGuideInstaller.ChooseAndInstall);
        }

        // Secondary action card: installing ignore rules is independent of documentation Update.
        [BoxGroup("Git ignore rules", order: 30), HideIf(nameof(HasBuildForge)), OnInspectorGUI, PropertyOrder(0)]
        private void DrawGitIgnoreDescription()
        {
            EnsureStyles();
            GUILayout.Label("Use the approved Git ignore rules from your installed documentation.", _bodyStyle);
            GUILayout.Space(4f);
            GUILayout.Label("Creates a missing project .gitignore after confirmation. Existing custom rules are preserved.", _bodyStyle);
            GUILayout.Space(6f);
        }

        [BoxGroup("Git ignore rules"), HideIf(nameof(HasBuildForge)), PropertyOrder(1)]
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
            GUILayout.Label("Use Install Codex guide to choose a guide location. AI tools must support their instruction files or read the documentation entry explicitly.", _bodyStyle);
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
            _theme = new DocumentationEditorTheme();
            _theme.IncludePanelStyles(EditorStyles.helpBox);
            _titleStyle = new GUIStyle(_theme.Title) { fontSize = 26, fixedHeight = 36f };
            _bodyStyle = _theme.Body;
            _eyebrowStyle = _theme.Eyebrow;
            // GUI.contentColor supplies semantic warning/error colours, so this base must remain white.
            _badgeStyle = new GUIStyle(_theme.Eyebrow) { normal = { textColor = Color.white } };
            _statusTitleStyle = _theme.Section;
            _progressStyle = new GUIStyle(_theme.Small) { alignment = TextAnchor.MiddleCenter };
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
            rootVisualElement.Clear();
            DocumentationEditorTheme.ApplyToolkit(rootVisualElement);
            VisualElement root = new ScrollView();
            root.style.flexGrow = 1f;
            rootVisualElement.Add(root);
            root.style.paddingLeft = root.style.paddingRight = 20f;
            root.style.paddingTop = root.style.paddingBottom = 20f;
            var header = new VisualElement();
            header.AddToClassList("forge-header");
            var eyebrow = new Label("GEURTS  /  GAME FORGE");
            eyebrow.AddToClassList("forge-eyebrow");
            var title = new Label("DOCUMENTATION");
            title.AddToClassList("forge-title");
            header.Add(eyebrow); header.Add(title); root.Add(header);
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
