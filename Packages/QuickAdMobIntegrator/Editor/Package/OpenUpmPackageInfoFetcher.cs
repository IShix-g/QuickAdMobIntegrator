
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace QuickAdMobIntegrator.Editor
{
    public sealed class OpenUpmPackageInfoFetcher
    {
        public static readonly Regex PackageRegex = new (@"https:\/\/[^\/]+\/([^\/]+)(?:\/([^\/]+))?", RegexOptions.Compiled);
        
        public bool IsProcessing{ get; private set; }
        bool _isDisposed;
        CancellationTokenSource _tokenSource;
        readonly PackageInstaller _installer;
        
        public OpenUpmPackageInfoFetcher(PackageInstaller installer) => _installer = installer;
        
        public async Task<PackageInfoDetails> FetchPackageInfo(string openUpmPackageInfoUrl, CancellationToken token = default)
        {
            if (!openUpmPackageInfoUrl.StartsWith("https://package.openupm.com"))
            {
                throw new ArgumentException("Invalid package url. Please use the following format: https://package.openupm.com/{package-name}");
            }
            
            var info = default(PackageInfo);
            var (name, version) = ExtractPackageInfo(openUpmPackageInfoUrl);
            try
            {
                IsProcessing = true;
                
                _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                info = await _installer.GetInfoByPackageId(name, _tokenSource.Token);
                var local = info != default
                    ? new PackageLocalInfo
                    {
                        name = info.name,
                        version = info.version,
                        displayName = info.displayName
                    }
                    : default;
                var server = await FetchPackageInfoFromOpenUpm(openUpmPackageInfoUrl);
                if (version != "latest")
                {
                    name += "@" + version;
                }
                return new PackageInfoDetails(local, server, name);
            }
            catch (Exception ex)
            {
                var message = ex.Message + "\n";
                if (info != default)
                {
                    message += "Package: " + info.displayName + " (" + info.name + ")\npackage.json url: " + openUpmPackageInfoUrl + "\nInstall url: " + name;
                }
                else
                {
                    message += "Install url: " + name + "\npackage.json url: " + openUpmPackageInfoUrl;
                }
                throw new PackageInstallException(message, ex);
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        public async Task<PackageRemoteInfo> FetchPackageInfoFromOpenUpm(string openUpmPackageInfoUrl)
        {
            if (!openUpmPackageInfoUrl.StartsWith("https://package.openupm.com"))
            {
                throw new ArgumentException("Invalid package url. Please use the following format: https://package.openupm.com/{package-name}");
            }
            
            IsProcessing = true;
            try
            {
                using var op = UnityWebRequest.Get(openUpmPackageInfoUrl);
                await op.SendWebRequest();
                if (op.isDone)
                {
                    return JsonConvert.DeserializeObject<PackageRemoteInfo>(op.downloadHandler.text);
                }
                throw new InvalidOperationException(op.error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        public static (string Name, string Version) ExtractPackageInfo(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return (default, default);
            }
            var match = PackageRegex.Match(url);
            var name = match.Success
                    ? match.Groups[1].Value
                    : default;
            var version = match.Success
                    && match.Groups[2].Success
                        ? match.Groups[2].Value
                        : default;
            return (name, version);
        }
        
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            
            if (_tokenSource != default)
            {
                _tokenSource.Dispose();
                _tokenSource = default;
            }
        }
    }
}