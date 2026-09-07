// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Geurts.GameForge.Documentation
{
    internal sealed class GitIgnoreInstallConfirmation : EditorWindow
    {
        private string _message;
        private bool _confirmed;

        internal static bool Confirm(string message)
        {
            GitIgnoreInstallConfirmation window = CreateInstance<GitIgnoreInstallConfirmation>();
            window._message = message;
            window.titleContent = new GUIContent("Confirm .gitignore Installation");
            window.minSize = new Vector2(580f, 280f);
            window.ShowModalUtility();
            return window._confirmed;
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.paddingTop = root.style.paddingBottom = 12f;
            root.style.paddingLeft = root.style.paddingRight = 12f;
            root.Add(new HelpBox(_message, HelpBoxMessageType.Warning));

            VisualElement buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.justifyContent = Justify.FlexEnd;
            buttons.style.marginTop = 12f;
            Button cancel = new Button(Close) { text = "Cancel", name = "cancel-install" };
            Button install = new Button(() =>
            {
                _confirmed = true;
                Close();
            }) { text = "Install and Overwrite", name = "confirm-install" };
            cancel.style.minWidth = 100f;
            install.style.minWidth = 170f;
            buttons.Add(cancel);
            buttons.Add(install);
            root.Add(buttons);

            // Enter, Escape, and closing the window decline; installation requires an explicit choice.
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape || evt.keyCode == KeyCode.Return ||
                    evt.keyCode == KeyCode.KeypadEnter)
                {
                    evt.StopImmediatePropagation();
                    Close();
                }
            }, TrickleDown.TrickleDown);
            cancel.schedule.Execute(cancel.Focus);
        }
    }
}
