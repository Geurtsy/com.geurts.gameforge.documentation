using UnityEditor;
using UnityEngine;

namespace Geurts.GameForge.Documentation
{
    internal sealed class DocumentationUpdateConfirmation : EditorWindow
    {
        private bool focusedDefault;

        internal static void Open()
        {
            DocumentationUpdateConfirmation window = CreateInstance<DocumentationUpdateConfirmation>();
            window.titleContent = new GUIContent("Confirm Documentation Update");
            window.minSize = new Vector2(620f, 330f);
            window.maxSize = window.minSize;
            window.ShowModalUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(
                DocumentationPackageConstants.UpdateActionLabel,
                EditorStyles.boldLabel);
            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                DocumentationPackageConstants.BuildConfirmationMessage(),
                MessageType.Warning);

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.SetNextControlName("CancelDocumentationUpdate");
            if (GUILayout.Button("Cancel", GUILayout.Width(120f), GUILayout.Height(28f)))
            {
                Close();
            }

            if (GUILayout.Button(
                    DocumentationPackageConstants.UpdateActionLabel,
                    GUILayout.Width(290f),
                    GUILayout.Height(28f)))
            {
                Close();
                DocumentationUpdaterController.BeginConfirmedUpdate();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(12f);

            if (Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Escape || Event.current.keyCode == KeyCode.Return))
            {
                Event.current.Use();
                Close();
            }

            if (!focusedDefault && Event.current.type == EventType.Repaint)
            {
                focusedDefault = true;
                GUI.FocusControl("CancelDocumentationUpdate");
            }
        }
    }
}
