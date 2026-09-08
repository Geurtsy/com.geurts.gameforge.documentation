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
            foreach (string dependency in DocumentationDependencies.RequiredExternalTools)
            {
                Label label = new Label(dependency + " — Required");
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.marginTop = 4f;
                _root.Add(label);
            }
            Label guidance = new Label("Import licensed copies separately before using this package.");
            guidance.style.whiteSpace = WhiteSpace.Normal;
            guidance.style.marginTop = 6f;
            _root.Add(guidance);
            RefreshVisibility();
            return _root;
        }

        /// <summary>Shows the labels only for the selected Geurts Documentation package.</summary>
        /// <param name="packageInfo">The selected package, or null when selection is cleared.</param>
        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
            _selectedPackageName = packageInfo?.name;
            RefreshVisibility();
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
