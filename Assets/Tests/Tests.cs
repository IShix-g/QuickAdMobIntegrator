
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using QuickAdMobIntegrator.Editor;
using QuickAdMobIntegrator.Admob.Editor;
using UnityEditor;

namespace Test
{
    public static class Tests
    {
        static readonly ManifestRegistry s_registry = new (
            "OpenUPM",
            "https://package.openupm.com",
            new[] { "com.google.ads.mobile", "com.google.external-dependency-manager" }
        );
        
        [MenuItem("Tests/Add AddMob Registry")]
        public static void AddAdmobRegistry()
        {
            ManifestRegistryConfigurator.Add(s_registry);
        }
        
        [MenuItem("Tests/Remove AddMob Registry")]
        public static void RemoveAdmobRegistry()
        {
            ManifestRegistryConfigurator.Remove(s_registry.url);
        }
        
        [MenuItem("Tests/Get All Registry")]
        public static void GetAllRegistry()
        {
            var registries = ManifestRegistryConfigurator.GetAll();
            Debug.Log(registries.Select(x => JsonConvert.SerializeObject(x, Formatting.Indented)).Aggregate((a, b) => a + "\n" + b));
        }
        
        [MenuItem("Tests/Has AddMob Registry")]
        public static void HasAdmobRegistry()
        {
            Debug.Log(s_registry.name + " exists: " + ManifestRegistryConfigurator.Contains(s_registry));
        }

        [MenuItem("Tests/Fetch Test")]
        public static void FetchTest()
        {
            var manager = QAIManagerFactory.Create();
            manager.SetUpRegistry();
            manager.FetchGoogleAdsPackageInfo(true)
                .Handled(task => Debug.Log(task.Result.Remote.Name + " " + task.Result.Remote.LatestVersion));
        }

        [MenuItem("Tests/Admob Setting Check")]
        public static void AdmobSettingCheck()
        {
            var obj = new AdMobSettingsValidator();
            Debug.Log(obj.IsValid + " error: " + obj.Error.Type);
        }
    }
}