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

        private bool IsBusy => DocumentationUpdaterController.IsBusy;

        protected override void OnEnable()
        {
            base.OnEnable();
            WindowPadding = new Vector4(20f, 20f, 16f, 16f);
            // Removing first also makes subscription safe across repeated enable calls.
            DocumentationUpdaterController.Changed -= Repaint;
            DocumentationUpdaterController.Changed += Repaint;
        }

        protected override void OnDisable()
        {
            DocumentationUpdaterController.Changed -= Repaint;
            base.OnDisable();
        }

        // Keep status above the action groups; drawing reads only the controller's cached state.
        [OnInspectorGUI, PropertyOrder(-20)]
        private void DrawOverview()
        {
            EnsureStyles();
            GUILayout.Label("GEURTS GAME FORGE", _eyebrowStyle);
            GUILayout.Label("Documentation", _titleStyle);
            GUILayout.Label("Shared guidance for your project and AI tools.", _bodyStyle);
            GUILayout.Space(18f);

            Color accent = IsBusy
                ? new Color(0.35f, 0.68f, 1f)
                : DocumentationUpdaterController.Availability == DocumentationAvailability.Current
                    ? new Color(0.35f, 0.76f, 0.57f)
                    : DocumentationUpdaterController.Availability == DocumentationAvailability.Unknown
                        ? new Color(0.9f, 0.66f, 0.28f)
                        : new Color(0.35f, 0.68f, 1f);

            Rect card = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(card.x, card.y, 3f, card.height), accent);
            }
            GUILayout.Space(9f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12f);
            EditorGUILayout.BeginVertical();
            GUILayout.Label(StatusTitle, _statusTitleStyle);
            GUILayout.Space(4f);
            GUILayout.Label(DocumentationUpdaterController.StatusMessage, _bodyStyle);
            EditorGUILayout.EndVertical();
            GUILayout.Space(12f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(9f);
            EditorGUILayout.EndVertical();
            GUILayout.Space(16f);
        }

        private string StatusTitle => IsBusy
            ? "Working on documentation..."
            : DocumentationUpdaterController.Availability == DocumentationAvailability.Current
                ? "Source unchanged since your last update"
                : DocumentationUpdaterController.Availability == DocumentationAvailability.UpdateAvailable
                    ? "Documentation update available"
                    : DocumentationUpdaterController.Availability == DocumentationAvailability.NotInstalled
                        ? "Ready for your first update"
                        : "Update status unavailable";

        // Primary action card: the existing confirmation remains the only route to replacement.
        [BoxGroup("Documentation update", order: 0), OnInspectorGUI, PropertyOrder(0)]
        private void DrawUpdateDescription()
        {
            EnsureStyles();
            GUILayout.Label("Get the latest shared documentation and the four AI instruction files.", _bodyStyle);
            GUILayout.Space(4f);
            GUILayout.Label("Local changes in those five targets will be replaced after confirmation.", _bodyStyle);
            GUILayout.Space(6f);
        }

        [BoxGroup("Documentation update"), PropertyOrder(1)]
        [Button(DocumentationPackageConstants.UpdateActionLabel, ButtonSizes.Large)]
        [GUIColor(0.8f, 0.92f, 1f), DisableIf(nameof(IsBusy))]
        private void UpdateDocumentation()
        {
            DocumentationUpdaterController.ConfirmAndUpdate();
        }

        [OnInspectorGUI, PropertyOrder(5)]
        private void DrawActionSpacing()
        {
            GUILayout.Space(12f);
        }

        // Secondary action card: installing ignore rules is independent of documentation Update.
        [BoxGroup("Git ignore rules", order: 10), OnInspectorGUI, PropertyOrder(0)]
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
            GitIgnoreInstaller.ConfirmAndInstall();
        }

        [OnInspectorGUI, PropertyOrder(15)]
        private void DrawDetailsSpacing()
        {
            GUILayout.Space(12f);
        }

        // Advanced details are collapsed by default and derived from the supported contract constants.
        [FoldoutGroup("Source and managed files", expanded: false, order: 20)]
        [OnInspectorGUI]
        private void DrawSourceDetails()
        {
            EnsureStyles();
            GUILayout.Label("Authoritative source", EditorStyles.boldLabel);
            DrawSelectable(DocumentationPackageConstants.RepositoryUrl);
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

        [OnInspectorGUI, PropertyOrder(30)]
        private void DrawFooter()
        {
            EnsureStyles();
            GUILayout.Space(12f);
            GUILayout.Label("Startup checks the source only. You choose when to replace files.", _bodyStyle);
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
        // Odin is distributed separately; missing it must not create compilation errors or hide the menu.
        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            root.style.paddingLeft = root.style.paddingRight = 20f;
            root.style.paddingTop = root.style.paddingBottom = 20f;
            root.Add(new Label("GEURTS GAME FORGE") { style = { fontSize = 10 } });
            root.Add(new Label("Documentation") { style = { fontSize = 26, marginBottom = 16f } });
            root.Add(new HelpBox("Odin Inspector is required for the documentation dashboard.", HelpBoxMessageType.Info));
            root.Add(new Label(
                "Import your licensed Odin Inspector installation into this Unity project, then reopen this window. " +
                "Odin supplies the ODIN_INSPECTOR scripting symbol automatically.")
            {
                style = { whiteSpace = WhiteSpace.Normal, marginTop = 12f, marginBottom = 12f }
            });
            root.Add(new Button(() => Application.OpenURL("https://odininspector.com/tutorials/getting-started/installing-odin-inspector"))
            {
                text = "Odin Inspector installation guide",
                style = { height = 32f }
            });
        }
#endif
    }
}
