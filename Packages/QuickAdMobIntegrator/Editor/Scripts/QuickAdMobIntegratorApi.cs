using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuickAdMobIntegrator.Editor
{
    /// <summary>Provides non-interactive access to Quick AdMob Integrator from Unity Editor code.</summary>
    public static class QuickAdMobIntegratorApi
    {
        public static async Task<QuickAdMobPackageStatus[]> GetPackageStatusesAsync(CancellationToken cancellationToken = default)
        {
            using (var manager = QAIManagerFactory.Create())
            {
                var installed = await manager.Installer.GetInfos(cancellationToken);
                var statuses = new List<QuickAdMobPackageStatus>();
                var googleAds = await manager.FetchPackageInfoForApi(manager.Settings.AdmobScope, installed, cancellationToken);
                statuses.Add(CreateStatus(googleAds, manager.Settings.AdmobScope, manager.IsCompletedRegistrySetUp));

                foreach (var scope in manager.Settings.MediationScopes)
                {
                    if (!scope.IsEnabled)
                    {
                        continue;
                    }
                    var details = await manager.FetchPackageInfoForApi(scope, installed, cancellationToken);
                    statuses.Add(CreateStatus(details, scope, manager.IsCompletedRegistrySetUp));
                }

                return statuses.ToArray();
            }
        }

        /// <summary>Adds the OpenUPM scoped registry required by this package to Packages/manifest.json.</summary>
        public static void SetUpRegistry()
        {
            using (var manager = QAIManagerFactory.Create())
            {
                manager.SetUpRegistry();
            }
        }

        /// <summary>Enables or disables a mediation package by its UPM package ID.</summary>
        public static void SetMediationEnabled(string packageId, bool isEnabled)
        {
            using (var manager = QAIManagerFactory.Create())
            {
                ConfigureMediation(manager.Settings, packageId, isEnabled);
                manager.Settings.Save();
            }
        }

        /// <summary>Sets a fixed version, or clears the pin when version is null or empty.</summary>
        public static void SetFixedVersion(string packageId, string version)
        {
            using (var manager = QAIManagerFactory.Create())
            {
                ConfigureFixedVersion(manager.Settings, packageId, version);
                manager.Settings.Save();
            }
        }

        /// <summary>Installs or updates an enabled package using its saved version setting.</summary>
        public static async Task InstallOrUpdatePackageAsync(string packageId, CancellationToken cancellationToken = default)
        {
            using (var manager = QAIManagerFactory.Create())
            {
                var scope = GetPackageScope(manager.Settings, packageId);
                EnsureInstallable(scope, packageId);

                manager.SetUpRegistry();
                var installed = await manager.Installer.GetInfos(cancellationToken);
                var details = await manager.FetchPackageInfoForApi(scope, installed, cancellationToken);
                await manager.Installer.Install(GetInstallUrl(scope, details), cancellationToken, false);
            }
        }

        /// <summary>Installs or updates AdMob and all enabled mediation packages.</summary>
        public static async Task InstallAllAsync(CancellationToken cancellationToken = default)
        {
            using (var manager = QAIManagerFactory.Create())
            {
                manager.SetUpRegistry();
                var installed = await manager.Installer.GetInfos(cancellationToken);
                var scopes = GetInstallableScopes(manager.Settings);
                foreach (var scope in scopes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var details = await manager.FetchPackageInfoForApi(scope, installed, cancellationToken);
                    await manager.Installer.Install(GetInstallUrl(scope, details), cancellationToken, false);
                    installed = await manager.Installer.GetInfos(cancellationToken);
                }
            }
        }

        internal static QuickAdMobPackageStatus CreateStatus(PackageInfoDetails details, PackageSettings.IScope scope, bool registryConfigured)
        {
            ApplyVersionSetting(scope, details);
            var targetVersion = GetTargetVersion(details, scope);
            return new QuickAdMobPackageStatus(
                details.Remote == null ? OpenUpmPackageInfoFetcher.ExtractPackageInfo(scope.OpenUpmInfoUrl) : details.Remote.Name,
                details.Local == null ? string.Empty : details.Local.Version,
                targetVersion,
                scope.IsEnabled,
                scope.IsRequired,
                details.IsInstalled,
                details.IsInstalled && !string.IsNullOrEmpty(targetVersion) && details.Local.Version != targetVersion,
                registryConfigured);
        }

        internal static void ConfigureMediation(PackageSettings settings, string packageId, bool isEnabled)
            => GetMediationScope(settings, packageId).IsEnabled = isEnabled;

        internal static void ConfigureFixedVersion(PackageSettings settings, string packageId, string version)
            => GetPackageScope(settings, packageId).FixedVersion = string.IsNullOrWhiteSpace(version) ? string.Empty : version.Trim();

        internal static PackageSettings.IScope[] GetInstallableScopes(PackageSettings settings)
        {
            var scopes = new List<PackageSettings.IScope> { settings.AdmobScope };
            scopes.AddRange(settings.MediationScopes.Where(scope => scope.IsEnabled));
            return scopes.ToArray();
        }

        internal static void EnsureInstallable(PackageSettings.IScope scope, string packageId)
        {
            if (!scope.IsEnabled)
            {
                throw new InvalidOperationException("The requested mediation package is disabled: " + packageId);
            }
        }

        internal static string GetTargetVersion(PackageInfoDetails details, PackageSettings.IScope scope)
            => scope.HasFixedVersion
                ? scope.FixedVersion
                : details.Remote == null ? string.Empty : details.Remote.LatestVersion;

        internal static string GetInstallUrl(PackageSettings.IScope scope, PackageInfoDetails details)
        {
            ApplyVersionSetting(scope, details);
            return details.PackageInstallUrl;
        }

        static void ApplyVersionSetting(PackageSettings.IScope scope, PackageInfoDetails details)
        {
            if (scope.HasFixedVersion)
            {
                details.SetFixedVersion(scope.FixedVersion);
            }
            else
            {
                details.RemoveFixedVersion();
            }
        }

        static PackageSettings.Scope GetMediationScope(PackageSettings settings, string packageId)
        {
            var expected = settings.MediationScopes.FirstOrDefault(scope => PackageId(scope) == packageId);
            if (expected == null)
            {
                throw new ArgumentException("Unknown mediation package ID: " + packageId, nameof(packageId));
            }
            return expected;
        }

        internal static PackageSettings.IScope GetPackageScope(PackageSettings settings, string packageId)
        {
            if (PackageId(settings.AdmobScope) == packageId)
            {
                return settings.AdmobScope;
            }
            return GetMediationScope(settings, packageId);
        }

        static string PackageId(PackageSettings.IScope scope)
            => OpenUpmPackageInfoFetcher.ExtractPackageInfo(scope.OpenUpmInfoUrl);
    }

    /// <summary>Snapshot of an AdMob or enabled mediation package.</summary>
    public sealed class QuickAdMobPackageStatus
    {
        public string PackageId { get; }
        public string InstalledVersion { get; }
        public string TargetVersion { get; }
        public bool IsEnabled { get; }
        public bool IsRequired { get; }
        public bool IsInstalled { get; }
        /// <summary>True when the package is installed and its version differs from the configured target.</summary>
        public bool HasUpdate { get; }
        public bool IsRegistryConfigured { get; }

        public QuickAdMobPackageStatus(string packageId, string installedVersion, string targetVersion, bool isEnabled, bool isRequired, bool isInstalled, bool hasUpdate, bool isRegistryConfigured)
        {
            PackageId = packageId;
            InstalledVersion = installedVersion;
            TargetVersion = targetVersion;
            IsEnabled = isEnabled;
            IsRequired = isRequired;
            IsInstalled = isInstalled;
            HasUpdate = hasUpdate;
            IsRegistryConfigured = isRegistryConfigured;
        }
    }
}
