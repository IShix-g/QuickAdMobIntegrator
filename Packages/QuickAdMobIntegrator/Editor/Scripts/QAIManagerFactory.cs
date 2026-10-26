
using System.IO;
using UnityEditor;

namespace QuickAdMobIntegrator.Editor
{
    internal sealed class QAIManagerFactory
    {
        public const string PackageRootPath = "Packages/com.ishix.quickadmobintegrator/";
        public const string DefaultSettingPath = PackageRootPath + "Editor/DefaultSettings.asset";
        public const string SettingPath = "Assets/Editor/QuickAdMobIntegratorSettings.asset";

        static PackageSettings s_settings;

        public static QAIManager Create()
        {
            var installer = new PackageInstaller();
            var fetcher = new OpenUpmPackageInfoFetcher(installer);
            var settings = GetOrCreateSettings();
            return new QAIManager(installer, fetcher, settings);
        }

        static PackageSettings GetOrCreateSettings()
        {
            if (s_settings != default)
            {
                return s_settings;
            }
            if (!File.Exists(SettingPath))
            {
                AssetDatabaseSupport.CreateDirectories(SettingPath);
                AssetDatabase.CopyAsset(DefaultSettingPath, SettingPath);
                AssetDatabase.SaveAssets();
            }
            s_settings = AssetDatabase.LoadAssetAtPath<PackageSettings>(SettingPath);
            return s_settings;
        }
    }
}