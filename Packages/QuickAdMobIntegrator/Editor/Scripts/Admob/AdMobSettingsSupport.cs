
using UnityEditor;
#if ENABLE_ADMOB
using GoogleMobileAds.Editor;
#endif

namespace QuickAdMobIntegrator.Admob.Editor
{
    internal enum NextGenSdkState
    {
        NotInstalled,
        Unsupported,
        MinimumApiTooLow,
        Available,
        Enabled
    }

    internal static class AdMobSettingsSupport
    {
        public const int NextGenMinimumApiLevel = 24;

        public static NextGenSdkState GetNextGenSdkState()
        {
#if ENABLE_ADMOB
            var settings = new SerializedObject(GoogleMobileAdsSettings.LoadInstance());
            return GetNextGenSdkState(settings, (int)PlayerSettings.Android.minSdkVersion);
#else
            return NextGenSdkState.NotInstalled;
#endif
        }

        internal static NextGenSdkState GetNextGenSdkState(SerializedObject settings, int minimumApiLevel)
        {
            var overrideSdk = settings.FindProperty("overrideDefaultGmaAndroidSdk");
            var selectedSdk = settings.FindProperty("selectedGmaAndroidSdk");
            // Older plugins don't expose architecture selection in their settings.
            if (overrideSdk == null || overrideSdk.propertyType != SerializedPropertyType.Boolean
                || selectedSdk == null || selectedSdk.propertyType != SerializedPropertyType.Integer)
            {
                return NextGenSdkState.Unsupported;
            }
            if (minimumApiLevel < NextGenMinimumApiLevel)
            {
                return NextGenSdkState.MinimumApiTooLow;
            }
            return overrideSdk.boolValue && selectedSdk.intValue == 1
                ? NextGenSdkState.Enabled
                : NextGenSdkState.Available;
        }

        public static bool TryEnableNextGenSdk()
        {
#if ENABLE_ADMOB
            var settings = new SerializedObject(GoogleMobileAdsSettings.LoadInstance());
            if (!TryEnableNextGenSdk(settings, (int)PlayerSettings.Android.minSdkVersion))
            {
                return false;
            }
            AssetDatabase.SaveAssetIfDirty(settings.targetObject);
            return true;
#else
            return false;
#endif
        }

        internal static bool TryEnableNextGenSdk(SerializedObject settings, int minimumApiLevel)
        {
            var state = GetNextGenSdkState(settings, minimumApiLevel);
            if (state == NextGenSdkState.Enabled)
            {
                return true;
            }
            if (state != NextGenSdkState.Available)
            {
                return false;
            }
            settings.FindProperty("overrideDefaultGmaAndroidSdk").boolValue = true;
            settings.FindProperty("selectedGmaAndroidSdk").intValue = 1;
            settings.ApplyModifiedProperties();
            return true;
        }

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
