
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager;
using UnityEditor;
using UnityEngine;

namespace QuickAdMobIntegrator.Editor
{
    public sealed class ManifestRegistryConfigurator
    {
        public static class ManifestKeys
        {
            public const string ScopedRegistries = "scopedRegistries";
            public static class Registry
            {
                public const string Name = "name";
                public const string Url = "url";
                public const string Scopes = "scopes";
            }
        }
        
        public static readonly string ManifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        
        public static bool Contains(ManifestRegistry registry)
        {
            var manifestJson = LoadManifestJson();
            var scopedRegistries = GetScopedRegistries(manifestJson);
            if (!TryFind(scopedRegistries, registry.url, out var existingRegistry))
            {
                return false;
            }
            
            if (existingRegistry[ManifestKeys.Registry.Scopes] is JArray existingScopes)
            {
                return registry.scopes
                    .All(scope => existingScopes
                        .Any(existingScope => string.Equals(
                            existingScope.ToString(),
                            (string)scope,
                            StringComparison.OrdinalIgnoreCase)));
            }
            return false;
        }
        
        public static ManifestRegistry[] GetAll()
        {
            var manifestJson = LoadManifestJson();
            var scopedRegistries = GetScopedRegistries(manifestJson);
            return scopedRegistries?.Select(x => x.ToObject<ManifestRegistry>()).ToArray()
                    ?? Array.Empty<ManifestRegistry>();
        }
        
        public static ManifestRegistry GetByUrl(string url)
        {
            var manifestJson = LoadManifestJson();
            var scopedRegistries = GetScopedRegistries(manifestJson);
            return Find(scopedRegistries, url)?.ToObject<ManifestRegistry>();
        }
        
        public static bool TryGet(string url, out ManifestRegistry registry)
        {
            registry = GetByUrl(url);
            return registry != default;
        }
        
        public static void Add(ManifestRegistry registry)
        {
            var manifestJson = LoadManifestJson();
            var scopedRegistries = GetOrCreateScopedRegistries(manifestJson);
            var updated = false;
            
            if (TryFind(scopedRegistries, registry.url, out var existingRegistry))
            {
                if (existingRegistry[ManifestKeys.Registry.Scopes] is not JArray existingScopes)
                {
                    existingScopes = new JArray();
                    existingRegistry[ManifestKeys.Registry.Scopes] = existingScopes;
                }
                
                foreach (var scope in registry.scopes)
                {
                    if (!existingScopes.Any(
                            existingScope => string.Equals(
                                existingScope.ToString(),
                                (string) scope,
                                StringComparison.OrdinalIgnoreCase)))
                    {
                        existingScopes.Add(scope);
                        Debug.Log($"Added scope '{scope}'.");
                        updated = true;
                    }
                }
            }
            else
            {
                var newRegistry = JObject.FromObject(registry);
                scopedRegistries.Add(newRegistry);
                Debug.Log("Added a new registry. registry: '" + ToStringByRegistry(registry) + "'.");
                updated = true;
            }

            if (updated)
            {
                SaveManifestJson(manifestJson);
                Debug.Log("Added scopes to the existing registry.");
            }
        }
        
        public static void Remove(string url)
        {
            if (TryGet(url, out var registry))
            {
                Remove(registry);
            }
            else
            {
                Debug.LogWarning("Registry '" + url + "' not found.");
            }
        }

        public static void Remove(ManifestRegistry registry)
        {
            var manifestJson = LoadManifestJson();
            var scopedRegistries = GetScopedRegistries(manifestJson);
            var updated = false;
            
            if(scopedRegistries == default)
            {
                Debug.LogWarning(registry.name + " is missing scoped registries.");
                return;
            }
            
            if (TryFind(scopedRegistries, registry.url, out var existingRegistry))
            {
                if (existingRegistry[ManifestKeys.Registry.Scopes] is JArray existingScopes)
                {
                    foreach (var scope in registry.scopes)
                    {
                        var scopeToRemove = existingScopes.FirstOrDefault(
                            existingScope => string.Equals(
                                existingScope.ToString(),
                                scope,
                                StringComparison.OrdinalIgnoreCase));

                        if (scopeToRemove == default)
                        {
                            continue;
                        }
                        existingScopes.Remove(scopeToRemove);
                        Debug.Log("Removed scope: '" + scope + "' from registry: '" + ToStringByRegistry(registry) + "'.");
                        updated = true;
                    }
                    
                    if (!existingScopes.Any())
                    {
                        scopedRegistries.Remove(existingRegistry);
                        Debug.Log("Removed registry: '" + ToStringByRegistry(registry) + "' because scopes list is empty.");
                        updated = true;
                    }
                }
                else
                {
                    scopedRegistries.Remove(existingRegistry);
                    Debug.Log("Removed registry: '" + ToStringByRegistry(registry) + "' because scopes list is empty.");
                    updated = true;
                }
            }
            else
            {
                Debug.LogWarning("Registry '" + ToStringByRegistry(registry) + "' not found in scoped registries.");
            }

            if (updated)
            {
                SaveManifestJson(manifestJson);
                Debug.Log("manifest.json updated after removing the registry or scopes.");
            }
        }
        
        static JObject LoadManifestJson()
        {
            if (!File.Exists(ManifestPath))
            {
                Debug.LogError("manifest.json file not found. Path: " + ManifestPath);
                return default;
            }
            var jsonContent = File.ReadAllText(ManifestPath);
            return JObject.Parse(jsonContent);
        }
        
        static void SaveManifestJson(JObject manifestJson)
        {
            File.WriteAllText(ManifestPath, manifestJson.ToString(Formatting.Indented));
            AssetDatabase.Refresh();
            Client.Resolve();
            Debug.Log("manifest.json has been saved.");
        }
        
        static JArray GetScopedRegistries()
        {
            var manifestJson = LoadManifestJson();
            return GetScopedRegistries(manifestJson);
        }
        
        static JArray GetScopedRegistries(JObject manifestJson)
        {
            if (manifestJson.TryGetValue(ManifestKeys.ScopedRegistries, out var scopedRegistriesToken)
                && scopedRegistriesToken.Type == JTokenType.Array)
            {
                return (JArray) scopedRegistriesToken;
            }
            return default;
        }
        
        static JArray GetOrCreateScopedRegistries(JObject manifestJson)
        {
            var registries = GetScopedRegistries(manifestJson);
            if (registries != default)
            {
                return registries;
            }
            var newScopedRegistries = new JArray();
            manifestJson[ManifestKeys.ScopedRegistries] = newScopedRegistries;
            return newScopedRegistries;
        }
        
        static bool TryFind(JArray jArray, string url, out JToken token)
            => (token = Find(jArray, url)) != default;
        
        static JToken Find(JArray jArray, string url)
            => jArray?.FirstOrDefault(
                x => x[ManifestKeys.Registry.Url] != default 
                && x[ManifestKeys.Registry.Url].ToString().Contains(url));
        
        static string ToStringByRegistry(ManifestRegistry registry) => registry.name + " - " + registry.url;
    }
}