
using UnityEditor;

namespace QuickAdMobIntegrator.Editor
{
    internal sealed class EditorInitializationExecuter
    {
        const string _key = "QuickAdMobIntegrator_EditorInitializationExecuter_FirstInit";

        public static bool IsFirstInit
        {
            get => SessionState.GetBool(_key, false);
            set => SessionState.SetBool(_key, value);
        }

        [InitializeOnLoadMethod]
        static void DetectEditorStartup()
        {
            if (!IsFirstInit)
            {
                IsFirstInit = true;
                EditorApplication.delayCall += FirstInit;
            }
        }

        static void FirstInit()
        {
            VersionDataUpdater.UpdateTo130();
        }
    }
}