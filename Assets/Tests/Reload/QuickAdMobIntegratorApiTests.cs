using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using QuickAdMobIntegrator.Editor;
using UnityEngine;

namespace Tests.QuickAdMobIntegrator
{
    public sealed class QuickAdMobIntegratorApiTests
    {
        PackageSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<PackageSettings>();
            _settings.AdmobScope = new PackageSettings.RequiredScope();
            SetUrl(_settings.AdmobScope, "com.google.ads.mobile");
            _settings.MediationScopes = new[]
            {
                CreateMediation("com.example.enabled", true),
                CreateMediation("com.example.disabled", false)
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (_settings != null)
            {
                UnityEngine.Object.DestroyImmediate(_settings);
            }
        }

        [Test]
        public void PublicApiExposesNonInteractiveConfigurationAndInstallMethods()
        {
            Assert.That(typeof(QuickAdMobIntegratorApi).IsPublic, Is.True);
            AssertMethod("GetPackageStatusesAsync", typeof(Task<QuickAdMobPackageStatus[]>));
            AssertMethod("SetUpRegistry", typeof(void));
            AssertMethod("SetMediationEnabled", typeof(void));
            AssertMethod("SetFixedVersion", typeof(void));
            AssertMethod("InstallOrUpdatePackageAsync", typeof(Task));
            AssertMethod("InstallAllAsync", typeof(Task));
        }

        [Test]
        public void PackageStatusExposesVersionsAndInstallState()
        {
            var status = new QuickAdMobPackageStatus("com.example", "1.0.0", "2.0.0", true, false, true, true, false);

            Assert.That(status.PackageId, Is.EqualTo("com.example"));
            Assert.That(status.InstalledVersion, Is.EqualTo("1.0.0"));
            Assert.That(status.TargetVersion, Is.EqualTo("2.0.0"));
            Assert.That(status.IsEnabled, Is.True);
            Assert.That(status.IsRequired, Is.False);
            Assert.That(status.IsInstalled, Is.True);
            Assert.That(status.HasUpdate, Is.True);
            Assert.That(status.IsRegistryConfigured, Is.False);
        }

        [Test]
        public void MediationAndFixedVersionSettingsMutateOnlyMatchingInMemoryScope()
        {
            QuickAdMobIntegratorApi.ConfigureMediation(_settings, "com.example.disabled", true);
            QuickAdMobIntegratorApi.ConfigureFixedVersion(_settings, "com.google.ads.mobile", " 2.1.0 ");
            QuickAdMobIntegratorApi.ConfigureFixedVersion(_settings, "com.example.enabled", "3.4.5");

            Assert.That(_settings.MediationScopes[0].IsEnabled, Is.True);
            Assert.That(_settings.MediationScopes[1].IsEnabled, Is.True);
            Assert.That(_settings.AdmobScope.FixedVersion, Is.EqualTo("2.1.0"));
            Assert.That(_settings.MediationScopes[0].FixedVersion, Is.EqualTo("3.4.5"));

            QuickAdMobIntegratorApi.ConfigureMediation(_settings, "com.example.enabled", false);
            QuickAdMobIntegratorApi.ConfigureFixedVersion(_settings, "com.google.ads.mobile", null);
            QuickAdMobIntegratorApi.ConfigureFixedVersion(_settings, "com.example.enabled", "");

            Assert.That(_settings.MediationScopes[0].IsEnabled, Is.False);
            Assert.That(_settings.AdmobScope.FixedVersion, Is.Empty);
            Assert.That(_settings.MediationScopes[0].FixedVersion, Is.Empty);
        }

        [Test]
        public void StatusUsesPinnedOrLatestTargetAndDistinguishesInstallFromUpdate()
        {
            var latestDetails = Details("com.google.ads.mobile", "1.0.0", "2.0.0");
            var latestStatus = QuickAdMobIntegratorApi.CreateStatus(latestDetails, _settings.AdmobScope, true);

            Assert.That(latestStatus.TargetVersion, Is.EqualTo("2.0.0"));
            Assert.That(latestStatus.IsInstalled, Is.True);
            Assert.That(latestStatus.HasUpdate, Is.True);
            Assert.That(latestStatus.IsRegistryConfigured, Is.True);
            Assert.That(QuickAdMobIntegratorApi.GetInstallUrl(_settings.AdmobScope, Details("com.google.ads.mobile", "1.0.0", "2.0.0")),
                Is.EqualTo("com.google.ads.mobile"));

            QuickAdMobIntegratorApi.ConfigureFixedVersion(_settings, "com.google.ads.mobile", "1.0.0");
            var pinnedDetails = Details("com.google.ads.mobile", "1.0.0", "2.0.0");
            var pinnedStatus = QuickAdMobIntegratorApi.CreateStatus(pinnedDetails, _settings.AdmobScope, false);

            Assert.That(pinnedStatus.TargetVersion, Is.EqualTo("1.0.0"));
            Assert.That(pinnedStatus.HasUpdate, Is.False);
            Assert.That(QuickAdMobIntegratorApi.GetInstallUrl(_settings.AdmobScope, Details("com.google.ads.mobile", "1.0.0", "2.0.0")),
                Is.EqualTo("com.google.ads.mobile@1.0.0"));

            var absentStatus = QuickAdMobIntegratorApi.CreateStatus(Details("com.google.ads.mobile", null, "2.0.0"), _settings.AdmobScope, false);
            Assert.That(absentStatus.IsInstalled, Is.False);
            Assert.That(absentStatus.InstalledVersion, Is.Empty);
            Assert.That(absentStatus.HasUpdate, Is.False);
        }

        [Test]
        public void PackageIdRoutingAndBulkSelectionRespectEnabledMediations()
        {
            var selected = QuickAdMobIntegratorApi.GetPackageScope(_settings, "com.example.enabled");
            Assert.That(selected, Is.SameAs(_settings.MediationScopes[0]));

            var installScopes = QuickAdMobIntegratorApi.GetInstallableScopes(_settings);
            Assert.That(installScopes, Has.Length.EqualTo(2));
            Assert.That(installScopes[0], Is.SameAs(_settings.AdmobScope));
            Assert.That(installScopes[1], Is.SameAs(_settings.MediationScopes[0]));
            Assert.DoesNotThrow(() => QuickAdMobIntegratorApi.EnsureInstallable(selected, "com.example.enabled"));
            Assert.Throws<InvalidOperationException>(() => QuickAdMobIntegratorApi.EnsureInstallable(
                QuickAdMobIntegratorApi.GetPackageScope(_settings, "com.example.disabled"), "com.example.disabled"));
            Assert.Throws<ArgumentException>(() => QuickAdMobIntegratorApi.GetPackageScope(_settings, "com.unknown.package"));
        }

        static void AssertMethod(string name, System.Type returnType)
        {
            var method = typeof(QuickAdMobIntegratorApi).GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, name + " should be public");
            Assert.That(method.ReturnType, Is.EqualTo(returnType), name + " return type");
        }

        static PackageSettings.Scope CreateMediation(string packageId, bool isEnabled)
        {
            var scope = new PackageSettings.Scope();
            SetUrl(scope, packageId);
            scope.IsEnabled = isEnabled;
            return scope;
        }

        static void SetUrl(PackageSettings.IScope scope, string packageId)
        {
            var field = scope.GetType().GetField("_openUpmInfoUrl", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(scope, "https://package.openupm.com/" + packageId);
        }

        static PackageInfoDetails Details(string packageId, string installedVersion, string latestVersion)
        {
            var local = string.IsNullOrEmpty(installedVersion)
                ? null
                : new PackageLocalInfo { Name = packageId, DisplayName = packageId, Version = installedVersion };
            var remote = new PackageRemoteInfo
            {
                Name = packageId,
                DisplayName = packageId,
                LatestVersion = latestVersion,
                Versions = new[] { latestVersion }
            };
            return new PackageInfoDetails(local, remote, packageId);
        }
    }
}
