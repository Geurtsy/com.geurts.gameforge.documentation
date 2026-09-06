using UnityEditor;
using UnityEngine;

namespace Geurts.GameForge.Documentation
{
    internal sealed class DocumentationUpdaterWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Geurts Game Forge/Documentation";

        [MenuItem(MenuPath)]
        internal static void ShowWindow()
        {
            DocumentationUpdaterWindow window = GetWindow<DocumentationUpdaterWindow>();
            window.titleContent = new GUIContent("Geurts Documentation");
            window.minSize = new Vector2(560f, 300f);
            window.Show();
        }

        private void OnEnable()
        {
            DocumentationUpdaterController.Changed += Repaint;
        }

        private void OnDisable()
        {
            DocumentationUpdaterController.Changed -= Repaint;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(
                DocumentationPackageConstants.DisplayName,
                EditorStyles.boldLabel);
            EditorGUILayout.Space(8f);

            MessageType messageType = DocumentationUpdaterController.Availability == DocumentationAvailability.UpdateAvailable
                ? MessageType.Info
                : DocumentationUpdaterController.Availability == DocumentationAvailability.Unknown
                    ? MessageType.Warning
                    : MessageType.None;
            EditorGUILayout.HelpBox(DocumentationUpdaterController.StatusMessage, messageType);

            DrawCommit("Remote main", DocumentationUpdaterController.RemoteCommit);
            DrawCommit("Last successful install", DocumentationUpdaterController.InstalledCommit);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Managed targets", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("GeurtsGameForgeDocumentation/ and four declared AI route files.");
            EditorGUILayout.LabelField("Docs/GameDesign and every unlisted path are outside this package's boundary.");

            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(DocumentationUpdaterController.IsBusy))
            {
                if (GUILayout.Button(
                        DocumentationPackageConstants.UpdateActionLabel,
                        GUILayout.Height(30f)))
                {
                    DocumentationUpdaterController.ConfirmAndUpdate();
                }
            }

            EditorGUILayout.Space(12f);
        }

        private static void DrawCommit(string label, string commit)
        {
            string display = string.IsNullOrWhiteSpace(commit)
                ? "Unknown"
                : commit.Substring(0, Mathf.Min(12, commit.Length));
            EditorGUILayout.LabelField(label, display);
        }
    }
}
