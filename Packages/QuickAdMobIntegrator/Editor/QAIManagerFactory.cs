

using System.IO;
using UnityEditor;

namespace QuickAdMobIntegrator.Editor
{
    public sealed class QAIManagerFactory
    {
        public const string PackageRootPath = "Packages/com.ishix.quickadmobintegrator/";
        public const string DefaultSettingPath = PackageRootPath + "Editor/DefaultSettings.asset";
        public const string SettingPath = "Assets/Editor/QuickAdMobIntegratorSettings.asset";
        
        public static QAIManager Create()
        {
            var installer = new PackageInstaller();
            var ads = new OpenUpmPackageInfoFetcher(installer);
            if (!File.Exists(SettingPath))
            {
                AssetDatabaseSupport.CreateDirectories(SettingPath);
                AssetDatabase.CopyAsset(DefaultSettingPath, SettingPath);
                AssetDatabase.SaveAssets();
            }

            var settings = AssetDatabase.LoadAssetAtPath<PackageSettings>(SettingPath);
            return new QAIManager(installer, ads, settings);
        }
    }
}