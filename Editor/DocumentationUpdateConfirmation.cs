// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Geurts.GameForge.Documentation
{
    internal sealed class DocumentationUpdateConfirmation : EditorWindow
    {
        private bool _confirmed;
        internal static Action<DocumentationUpdateConfirmation> OpenedForTests;

        internal static bool Confirm()
        {
            DocumentationUpdateConfirmation window = CreateInstance<DocumentationUpdateConfirmation>();
            window.titleContent = new GUIContent("Confirm Documentation Update");
            window.minSize = new Vector2(620f, 350f);
            // Only capture the choice here. The caller starts work after the modal event loop returns.
            window.ShowModalUtility();
            return window._confirmed;
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.paddingTop = root.style.paddingBottom = 12f;
            root.style.paddingLeft = root.style.paddingRight = 12f;
            root.Add(new Label(DocumentationPackageConstants.UpdateActionLabel)
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 8f }
            });
            root.Add(new HelpBox(DocumentationPackageConstants.BuildConfirmationMessage(), HelpBoxMessageType.Warning));
            VisualElement buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.justifyContent = Justify.FlexEnd;
            buttons.style.marginTop = 12f;
            Button cancel = new Button(Close) { text = "Cancel", name = "cancel-update" };
            Button accept = new Button(() => { _confirmed = true; Close(); })
            {
                text = DocumentationPackageConstants.UpdateActionLabel, name = "confirm-update"
            };
            cancel.style.minWidth = 100f;
            accept.style.minWidth = 290f;
            cancel.style.height = accept.style.height = 28f;
            buttons.Add(cancel);
            buttons.Add(accept);
            root.Add(buttons);
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Escape)
                {
                    evt.StopImmediatePropagation();
                    Close();
                }
            }, TrickleDown.TrickleDown);
            cancel.schedule.Execute(cancel.Focus);
            if (OpenedForTests != null) root.schedule.Execute(() => OpenedForTests?.Invoke(this)).StartingIn(100);
        }
    }
}
