
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using UnityEngine.Pool;
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
            var name = ExtractPackageInfo(openUpmPackageInfoUrl);
            try
            {
                IsProcessing = true;
                
                _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                info = await _installer.GetInfoByPackageId(name, _tokenSource.Token);
                var local = info != default
                    ? new PackageLocalInfo
                    {
                        Name = info.name,
                        Version = info.version,
                        DisplayName = info.displayName
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
                if (!request.isDone)
                {
                    throw new InvalidOperationException(request.error);
                }
            
                var jsonObject = JObject.Parse(request.downloadHandler.text);
                var result = new PackageRemoteInfo();
                if (jsonObject.TryGetValue("name", out var nameToken))
                {
                    result.Name = nameToken.ToString();
                }

                var versions = HashSetPool<string>.Get();
                try
                {
                    if (jsonObject.TryGetValue("versions", out var versionsToken)
                        && versionsToken is JObject versionsDict)
                    {
                        foreach (var versionProperty in versionsDict.Properties())
                        {
                            versions.Add(versionProperty.Name);

                            if (string.IsNullOrEmpty(result.DisplayName)
                                && versionProperty.Value is JObject versionObject
                                && versionObject.TryGetValue("displayName", out var displayName))
                            {
                                result.DisplayName = displayName.ToString();
                            }
                        }
                    }
                    result.Versions = versions.ToArray();
                }
                finally
                {
                    HashSetPool<string>.Release(versions);
                }

                if (jsonObject.TryGetValue("dist-tags", out var dist)
                    && dist is JObject distTags
                    && distTags.TryGetValue("latest", out var latest))
                {
                    result.LatestVersion = latest.ToString();
                }
                else if (versions.Count > 0)
                {
                    result.LatestVersion = versions.Max();
                }

                return result;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        public static string ExtractPackageInfo(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }
            var match = PackageRegex.Match(url);
            return match.Success
                ? match.Groups[1].Value
                : string.Empty;
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
                if (string.IsNullOrEmpty(repoName))
                {
                    return userName;
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