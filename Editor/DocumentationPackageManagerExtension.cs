// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Geurts.GameForge.Documentation
{
    /// <summary>Labels separately imported requirements without changing package resolution.</summary>
    internal sealed class DocumentationPackageManagerExtension : IPackageManagerExtension
    {
        private VisualElement _root;
        private string _selectedPackageName;
        private readonly System.Collections.Generic.List<Label> _dependencyLabels = new System.Collections.Generic.List<Label>();

        [InitializeOnLoadMethod]
        private static void Register()
        {
            PackageManagerExtensions.RegisterExtension(new DocumentationPackageManagerExtension());
        }

        /// <summary>Creates the required external dependency labels for the Documentation package.</summary>
        /// <returns>The panel shown only while Geurts Documentation is selected.</returns>
        public VisualElement CreateExtensionUI()
        {
            _root = new VisualElement { name = "geurts-documentation-required-dependencies" };
            _root.style.marginTop = _root.style.marginBottom = 8f;
            Label heading = new Label("Required external dependencies");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            _root.Add(heading);
            _dependencyLabels.Clear();
            foreach (string dependency in DocumentationDependencies.RequiredExternalTools)
            {
                Label label = new Label { name = "geurts-dependency-" + _dependencyLabels.Count, userData = dependency };
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.marginTop = 4f;
                label.style.paddingLeft = label.style.paddingRight = 8f;
                label.style.paddingTop = label.style.paddingBottom = 6f;
                label.style.borderTopLeftRadius = label.style.borderTopRightRadius = 4f;
                label.style.borderBottomLeftRadius = label.style.borderBottomRightRadius = 4f;
                label.style.color = Color.white;
                _dependencyLabels.Add(label);
                _root.Add(label);
            }
            Label guidance = new Label("Import licensed copies separately before using this package.");
            guidance.style.whiteSpace = WhiteSpace.Normal;
            guidance.style.marginTop = 6f;
            _root.Add(guidance);
            RefreshVisibility();
            RefreshDependencyStates();
            _root.schedule.Execute(RefreshDependencyStates).Every(250);
            return _root;
        }

        private void RefreshDependencyStates()
        {
            if (_root.style.display.value == DisplayStyle.None) return;
            foreach (Label label in _dependencyLabels)
            {
                string tool = (string)label.userData;
                var status = DocumentationDependencies.ToolStatus(tool);
                label.text = tool + " — Required · " + status.Message;
                label.style.backgroundColor = status.Background;
            }
        }

        /// <summary>Shows the labels only for the selected Geurts Documentation package.</summary>
        /// <param name="packageInfo">The selected package, or null when selection is cleared.</param>
        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
            _selectedPackageName = packageInfo?.name;
            RefreshVisibility();
            if (_root != null) RefreshDependencyStates();
        }

        /// <summary>Retains selection-driven labels when another package is added or updated.</summary>
        /// <param name="packageInfo">The changed package.</param>
        public void OnPackageAddedOrUpdated(PackageInfo packageInfo) { }

        /// <summary>Retains selection-driven labels when a package is removed.</summary>
        /// <param name="packageInfo">The removed package.</param>
        public void OnPackageRemoved(PackageInfo packageInfo) { }

        private void RefreshVisibility()
        {
            if (_root != null)
                _root.style.display = _selectedPackageName == DocumentationPackageConstants.PackageName
                    ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
