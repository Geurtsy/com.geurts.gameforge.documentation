using UnityEditor;
using UnityEngine;

namespace Geurts.GameForge.Documentation
{
    [InitializeOnLoad]
    internal static class DocumentationStartup
    {
        private const string SessionCheckKey =
            DocumentationPackageConstants.PackageName + ".startup-check-scheduled";

        static DocumentationStartup()
        {
            if (Application.isBatchMode || !TryMarkCheckScheduled())
            {
                return;
            }

            EditorApplication.delayCall += RunStartupCheck;
        }

        internal static bool TryMarkCheckScheduled()
        {
            if (SessionState.GetBool(SessionCheckKey, false))
            {
                return false;
            }

            SessionState.SetBool(SessionCheckKey, true);
            return true;
        }

        internal static void ResetSessionCheckForTests()
        {
            SessionState.EraseBool(SessionCheckKey);
        }

        private static async void RunStartupCheck()
        {
            await DocumentationUpdaterController.CheckForUpdatesAsync(true);
        }
    }
}
