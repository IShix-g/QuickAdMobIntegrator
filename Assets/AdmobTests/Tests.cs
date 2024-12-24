
using UnityEngine;
using QuickAdMobIntegrator.Admob.Editor;
using UnityEditor;

namespace Tests.Admob
{
    public sealed class Tests
    {
        [MenuItem("Tests/Admob/Load userLanguage")]
        public static void OpenAdmobSettings()
        {
            AdMobSettingsSupport.OpenSettings();
            Debug.Log(AdMobSettingsSupport.GetStringProperty("userLanguage"));
        }
    }
}