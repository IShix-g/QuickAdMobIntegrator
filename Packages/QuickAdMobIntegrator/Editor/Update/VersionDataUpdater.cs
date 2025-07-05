
namespace QuickAdMobIntegrator.Editor
{
    public static class VersionDataUpdater
    {
        public static void UpdateTo130()
        {
            using var manager = QAIManagerFactory.Create();
            var settings = manager.Settings;
            if (manager.Settings.SettingVersion >= 2)
            {
                return;
            }
            settings.Update130();
            settings.SettingVersion = 2;
            settings.Save();
        }
    }
}