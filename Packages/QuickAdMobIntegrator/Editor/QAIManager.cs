
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QuickAdMobIntegrator.Editor
{
    public sealed class QAIManager : IDisposable
    {
        public PackageInstaller Installer { get; }
        public PackageSettings Settings { get; }
        public bool IsProcessing => Installer.IsProcessing || _infoFetcher.IsProcessing || _isProcessingMediation;
        public bool IsCompletedRegistrySetUp { get; private set; }

        readonly OpenUpmPackageInfoFetcher _infoFetcher;
        bool _isDisposed;
        bool _isProcessingMediation;

        public QAIManager(
            PackageInstaller installer,
            OpenUpmPackageInfoFetcher infoFetcher,
            PackageSettings settings)
        {
            Installer = installer;
            _infoFetcher = infoFetcher;
            Settings = settings;
            IsCompletedRegistrySetUp = ManifestRegistryConfigurator.Contains(settings.Registry);
        }

        public void SetUpRegistry()
        {
            if (IsCompletedRegistrySetUp)
            {
                return;
            }
            IsCompletedRegistrySetUp = true;
            ManifestRegistryConfigurator.Add(Settings.Registry);
        }

        public async Task<PackageInfoDetails> FetchGoogleAdsPackageInfo(bool superReload, CancellationToken token = default)
        {
            var details = await FetchPackageInfo(Settings.AdmobScope.OpenUpmInfoUrl, superReload, token);
            var displayName = details.Remote.displayName;
            details.Remote.displayName = displayName.Replace("for Unity", "");
            return details;
        }

        public async Task FetchMediationPackage(Action<int, PackageInfoDetails> onLoadedAction, bool superReload, CancellationToken token = default)
        {
            _isProcessingMediation = true;
            try
            {
                var tasks = new Task<PackageInfoDetails>[Settings.MediationScopes.Length];
                for (var i = 0; i < Settings.MediationScopes.Length; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    var index = i;
                    tasks[i] = FetchPackageInfo(Settings.MediationScopes[i].OpenUpmInfoUrl, superReload, token);
                    tasks[i].Handled(task =>
                    {
                        var details = task.Result;
                        var displayName = details.Remote.displayName;
                        details.Remote.displayName = displayName.Replace("Google Mobile Ads", "").Replace("Mediation", "");
                        onLoadedAction?.Invoke(index, task.Result);
                    });
                }
                await Task.WhenAll(tasks);
            }
            finally
            {
                _isProcessingMediation = false;
            }
        }
        
        public Task<PackageInfoDetails> FetchPackageInfo(string openUpmInfoUrl, bool superReload, CancellationToken token = default)
            => _infoFetcher.FetchPackageInfo(openUpmInfoUrl, superReload, token);
        
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            Installer?.Dispose();
            _infoFetcher?.Dispose();
        }
    }
}