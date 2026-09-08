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
                VisualElement card = new VisualElement { name = "geurts-dependency-card-" + _dependencyLabels.Count };
                card.style.marginTop = 8f;
                card.style.paddingLeft = card.style.paddingRight = 12f;
                card.style.paddingTop = card.style.paddingBottom = 10f;
                card.style.borderLeftWidth = 4f;
                Label label = new Label { name = "geurts-dependency-" + _dependencyLabels.Count, userData = dependency };
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                _dependencyLabels.Add(label);
                card.Add(label);
                card.Add(new Label(DocumentationDependencies.Description(dependency))
                {
                    style = { whiteSpace = WhiteSpace.Normal, marginTop = 6f }
                });
                card.Add(new Label { name = "installation-status", style = { whiteSpace = WhiteSpace.Normal, marginTop = 6f } });
                card.Add(new Button(() => EditorApplication.delayCall += () => DependencyInstallation.OpenOwnedAssets(dependency))
                {
                    text = "Download / import owned copy in My Assets", style = { height = 30f, marginTop = 8f }
                });
                card.Add(new Button(() => EditorApplication.delayCall += () => DependencyInstallation.ImportLicensedCopy(dependency))
                {
                    text = "Import licensed .unitypackage…", style = { height = 26f }
                });
                _root.Add(card);
            }
            Label guidance = new Label("My Assets uses your signed-in Unity account to download owned assets inside the Editor. Choose Download, then Import there. Green means the required assemblies are ready; it does not verify ownership or the latest vendor version.");
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
                label.style.color = status.Background;
                label.parent.style.backgroundColor = DashboardColours.Tint(status.Background);
                label.parent.style.borderLeftColor = status.Background;
                label.parent.Q<Label>("installation-status").text = DependencyInstallation.Message(tool);
                foreach (Button button in label.parent.Query<Button>().ToList())
                    button.SetEnabled(!DependencyInstallation.IsBusy);
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
