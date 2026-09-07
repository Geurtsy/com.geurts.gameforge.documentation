// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
#if !ODIN_INSPECTOR
using UnityEngine.UIElements;
#endif

namespace Geurts.GameForge.Documentation.Tests
{
    internal sealed class DocumentationUpdaterWindowTests
    {
        /// <summary>Open, repaint and reopen the real window without invoking either destructive action.</summary>
        [UnityTest]
        public IEnumerator DashboardOpensAndReopensWithTheInstalledInspector()
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                // Resolve through the core assembly so the test assembly needs no optional DLL reference.
                System.Type windowType = typeof(DocumentationUpdateService).Assembly.GetType(
                    "Geurts.GameForge.Documentation.DocumentationUpdaterWindow", true);
                EditorWindow window = (EditorWindow)ScriptableObject.CreateInstance(windowType);
                try
                {
                    window.Show();
                    window.Repaint();
                    yield return null;
                    yield return null;
#if ODIN_INSPECTOR
                    Assert.That(windowType.BaseType.FullName,
                        Is.EqualTo("Sirenix.OdinInspector.Editor.OdinEditorWindow"));
#else
                    HelpBox notice = window.rootVisualElement.Q<HelpBox>();
                    Assert.That(notice, Is.Not.Null);
                    Assert.That(notice.text, Does.Contain("Odin Inspector is required"));
                    Assert.That(window.rootVisualElement.Query<Button>().ToList(), Has.Count.EqualTo(1));
#endif
                    LogAssert.NoUnexpectedReceived();
                }
                finally
                {
                    window.Close();
                }
            }
        }
    }
}
