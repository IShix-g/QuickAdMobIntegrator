
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace QuickAdMobIntegrator.Editor
{
    internal sealed class OpenUpmPackageInfoFetcher
    {
        public static readonly Regex PackageRegex = new (@"https:\/\/[^\/]+\/([^\/]+)(?:\/([^\/]+))?", RegexOptions.Compiled);
        public const string PackageCachePath = "Library/PackageCache-QuickAdMobIntegrator";
        
        public bool IsProcessing{ get; private set; }
        bool _isDisposed;
        CancellationTokenSource _tokenSource;
        readonly PackageInstaller _installer;
        
        public OpenUpmPackageInfoFetcher(PackageInstaller installer) => _installer = installer;
        
        public async Task<PackageInfoDetails> FetchPackageInfo(string openUpmPackageInfoUrl, bool supperReload, CancellationToken token = default)
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
                
                var server = default(PackageRemoteInfo);
                var fileNameFromUrl = GenerateFileNameFromUrl(openUpmPackageInfoUrl);
                if (!supperReload)
                {
                    server = await GetPackageInfoFromCache(fileNameFromUrl, token);
                }

                if (server == default)
                {
                    server = await FetchPackageInfoFromOpenUpm(openUpmPackageInfoUrl);
                    if (server != default)
                    {
                        await SavePackageInfoToCache(fileNameFromUrl, server, token);
                    }
                }
                
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
                using var request = UnityWebRequest.Get(openUpmPackageInfoUrl);
                request.timeout = 30;
                await request.SendWebRequest();
                if (request.isDone)
                {
                    return JsonConvert.DeserializeObject<PackageRemoteInfo>(request.downloadHandler.text);
                }
                throw new InvalidOperationException(request.error);
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
        
        public static async Task<PackageRemoteInfo> GetPackageInfoFromCache(string packageName, CancellationToken token = default)
        {
            var filePath = Path.Combine(Application.dataPath + "/../", PackageCachePath, packageName + ".json");
            if (!File.Exists(filePath))
            {
                return default;
            }
            var jsonString = await File.ReadAllTextAsync(filePath, token);
            return JsonUtility.FromJson<PackageRemoteInfo>(jsonString);
        }
        
        public static async Task<bool> SavePackageInfoToCache(string packageName, PackageRemoteInfo info, CancellationToken token = default)
        {
            var filePath = Path.Combine(Application.dataPath + "/../", PackageCachePath, packageName + ".json");
            var jsonString = JsonUtility.ToJson(info);
            if (!string.IsNullOrEmpty(jsonString)
                && jsonString != "[]")
            {
                CreateDirectories(filePath);
                await File.WriteAllTextAsync(filePath, jsonString, token);
                return true;
            }
            return false;
        }
        
        public static string GenerateFileNameFromUrl(string packageInstallUrl)
        {
            try
            {
                var uri = new Uri(packageInstallUrl);
                var segments = uri.AbsolutePath.Split('/');
                var userName = segments.Length > 1 ? segments[1] : string.Empty;
                var repoName = segments.Length > 2 ? segments[^1] : string.Empty;
                if (repoName.EndsWith(".git"))
                {
                    repoName = repoName.Substring(0, repoName.Length - 4);
                }
                return $"{userName}@{repoName}";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error processing URL: {ex.Message}");
                return string.Empty;
            }
        }
        
        public static void CreateDirectories(string path)
        {
            path = Path.HasExtension(path)
                ? Path.GetDirectoryName(path)
                : path;
            
            if (!string.IsNullOrEmpty(path)
                && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
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