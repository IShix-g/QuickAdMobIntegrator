
using System.Text.RegularExpressions;

namespace QuickAdMobIntegrator.Editor
{
    internal sealed class PackageInfoDetails
    {
        public PackageLocalInfo Local { get; private set; }
        public PackageRemoteInfo Remote { get; private set; }
        public string PackageInstallUrl { get; private set; }
        public bool HasUpdate { get; private set; }
        public bool IsInstalled => Local != default;
        public bool IsLoaded => Remote != default;
        public bool IsFixedVersion { get; private set; }
        
        public PackageInfoDetails(PackageLocalInfo local, PackageRemoteInfo remote, string packageInstallUrl)
        {
            Remote = remote;
            PackageInstallUrl = packageInstallUrl;
            Installed(local);
            IsFixedVersion = !string.IsNullOrEmpty(GetVersionParam());
        }

        public void SetFixedVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                RemoveFixedVersion();
                return;
            }
            
            var currentVersion = GetVersionParam();
            if (!string.IsNullOrEmpty(currentVersion))
            {
                if (currentVersion != version)
                {
                    PackageInstallUrl = PackageInstallUrl.Replace(currentVersion, version);
                }
            }
            else
            {
                PackageInstallUrl += "@" + version;
            }
            IsFixedVersion = true;
            HasUpdate = HasUpdateInternal();
        }

        public void RemoveFixedVersion()
        {
            var currentVersion = GetVersionParam();
            if (!string.IsNullOrEmpty(currentVersion))
            {
                PackageInstallUrl = PackageInstallUrl.Replace(currentVersion, string.Empty);
            }
        }
        
        public void Installed(PackageLocalInfo info)
        {
            Local = info;
            HasUpdate = HasUpdateInternal();
        }

        bool HasUpdateInternal()
        {
            try
            {
                if (!IsInstalled || !IsLoaded)
                {
                    return true;
                }

                if (IsFixedVersion)
                {
                    return Local.Version != GetVersionParam();
                }
                return Local.Version != Remote.LatestVersion;
            }
            catch
            {
                return false;
            }
        }
        
        public string GetVersionParam() => GetVersionParam(PackageInstallUrl);
        
        public static string GetVersionParam(string packageInstallUrl)
        {
            var match = Regex.Match(packageInstallUrl, @"@v?([\d.]+)$");
            return match.Success
                ? match.Groups[1].Value
                : string.Empty;
        }
    }
}