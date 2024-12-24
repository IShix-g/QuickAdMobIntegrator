
using UnityEditor;
#if ENABLE_ADMOB
using GoogleMobileAds.Editor;
#endif

namespace QuickAdMobIntegrator.Admob.Editor
{
    internal static class AdMobSettingsSupport
    {
        public static void OpenSettings()
        {
#if ENABLE_ADMOB
            GoogleMobileAdsSettingsEditor.OpenInspector();
#endif
        }
        
        public static string GetAndroidAppId() => GetStringProperty("adMobAndroidAppId");
        
        public static string GetIOSAppId() => GetStringProperty("adMobIOSAppId");
        
        public static string GetStringProperty(string propertyName)
        {
#if ENABLE_ADMOB
            var cachedSettings = GoogleMobileAdsSettings.LoadInstance();
            var serializedObject = new SerializedObject(cachedSettings);
            var property = serializedObject.FindProperty(propertyName);
            return property?.stringValue;
#else
            return default;
#endif
        }
        
        public static bool IsAdmobInstalled()
        {
#if ENABLE_ADMOB
            return true;
#else
            return false;
#endif
        }
    }
}