using NUnit.Framework;
using QuickAdMobIntegrator.Admob.Editor;
using UnityEditor;
using UnityEngine;

namespace Tests.QuickAdMobIntegrator
{
    public sealed class AdMobNextGenSettingsTests
    {
        [TestCase(23, false, 0, NextGenSdkState.MinimumApiTooLow)]
        [TestCase(23, true, 1, NextGenSdkState.MinimumApiTooLow)]
        [TestCase(24, false, 0, NextGenSdkState.Available)]
        [TestCase(24, false, 1, NextGenSdkState.Available)]
        [TestCase(24, true, 0, NextGenSdkState.Available)]
        [TestCase(24, true, 1, NextGenSdkState.Enabled)]
        [TestCase(25, true, 1, NextGenSdkState.Enabled)]
        public void SelectionRequiresBothFlagsAndMinimumApi(
            int minimumApiLevel, bool overrideSdk, int selectedSdk, object expected)
        {
            var asset = ScriptableObject.CreateInstance<SelectableSettings>();
            try
            {
                asset.overrideDefaultGmaAndroidSdk = overrideSdk;
                asset.selectedGmaAndroidSdk = selectedSdk;
                var settings = new SerializedObject(asset);
                Assert.That(AdMobSettingsSupport.GetNextGenSdkState(settings, minimumApiLevel), Is.EqualTo(expected));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [TestCase(23, false)]
        [TestCase(24, true)]
        public void EnableChangesOnlyArchitectureWhenEligible(int minimumApiLevel, bool expected)
        {
            var asset = ScriptableObject.CreateInstance<SelectableSettings>();
            try
            {
                asset.adMobAndroidAppId = "preserved-app-id";
                asset.enableGradleBuildPreProcessor = false;
                var settings = new SerializedObject(asset);
                Assert.That(AdMobSettingsSupport.TryEnableNextGenSdk(settings, minimumApiLevel), Is.EqualTo(expected));
                Assert.That(asset.overrideDefaultGmaAndroidSdk, Is.EqualTo(expected));
                Assert.That(asset.selectedGmaAndroidSdk, Is.EqualTo(expected ? 1 : 0));
                Assert.That(asset.adMobAndroidAppId, Is.EqualTo("preserved-app-id"));
                Assert.That(asset.enableGradleBuildPreProcessor, Is.False);
                Assert.That(settings.hasModifiedProperties, Is.False);
                if (expected)
                {
                    Assert.That(AdMobSettingsSupport.TryEnableNextGenSdk(settings, minimumApiLevel), Is.True);
                    Assert.That(settings.hasModifiedProperties, Is.False);
                }
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void OlderSdkSettingsAreUnsupportedAndUnchanged()
        {
            AssertUnsupportedAndUnchanged<LegacySettings>();
        }

        [Test]
        public void IncompatibleSdkSettingsAreUnsupportedAndUnchanged()
        {
            AssertUnsupportedAndUnchanged<IncompatibleSettings>();
        }

        static void AssertUnsupportedAndUnchanged<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            try
            {
                var before = EditorJsonUtility.ToJson(asset);
                var settings = new SerializedObject(asset);
                Assert.That(AdMobSettingsSupport.GetNextGenSdkState(settings, 24), Is.EqualTo(NextGenSdkState.Unsupported));
                Assert.That(AdMobSettingsSupport.TryEnableNextGenSdk(settings, 24), Is.False);
                Assert.That(EditorJsonUtility.ToJson(asset), Is.EqualTo(before));
                Assert.That(settings.hasModifiedProperties, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        public sealed class SelectableSettings : ScriptableObject
        {
            public bool overrideDefaultGmaAndroidSdk;
            public int selectedGmaAndroidSdk;
            public string adMobAndroidAppId;
            public bool enableGradleBuildPreProcessor;
        }

        public sealed class LegacySettings : ScriptableObject
        {
            public string adMobAndroidAppId = "preserved-app-id";
        }

        public sealed class IncompatibleSettings : ScriptableObject
        {
            public bool overrideDefaultGmaAndroidSdk;
            public string selectedGmaAndroidSdk = "unsupported-type";
        }
    }
}
