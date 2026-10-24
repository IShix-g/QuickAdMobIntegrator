
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace QuickAdMobIntegrator.Editor
{
    internal interface IPackageReloadSource
    {
        int MediationCount { get; }
        Task<IEnumerable<PackageInfo>> GetInstalledPackages(CancellationToken token);
        Task<PackageInfoDetails> FetchGoogleAdsPackageInfo(
            bool superReload,
            IEnumerable<PackageInfo> installedPackages,
            CancellationToken token,
            Action<PackageInfoDetails> onCacheLoaded,
            Action<PackageInfoDetails> onLoaded);
        Task FetchMediationPackage(
            Action<int, PackageInfoDetails> onLoadedAction,
            bool superReload,
            IEnumerable<PackageInfo> installedPackages,
            CancellationToken token);
    }

    internal sealed class QAIManager : IDisposable, IPackageReloadSource
    {
        public PackageInstaller Installer { get; }
        public PackageSettings Settings { get; }
        public bool IsProcessing => Installer.IsProcessing
                                    || _fetcher.IsProcessing
                                    || System.Threading.Volatile.Read(ref _activeMediationCount) > 0;
        public bool IsCompletedRegistrySetUp { get; private set; }

        readonly OpenUpmPackageInfoFetcher _fetcher;
        bool _isDisposed;
        int _activeMediationCount;

        int IPackageReloadSource.MediationCount => Settings.MediationScopes.Length;

        Task<IEnumerable<PackageInfo>> IPackageReloadSource.GetInstalledPackages(CancellationToken token)
            => Installer.GetInfos(token);

        public QAIManager(
            PackageInstaller installer,
            OpenUpmPackageInfoFetcher fetcher,
            PackageSettings settings)
        {
            Installer = installer;
            _fetcher = fetcher;
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

        public async Task<PackageInfoDetails> FetchGoogleAdsPackageInfo(
            bool superReload,
            IEnumerable<PackageInfo> installedPackages,
            CancellationToken token = default,
            Action<PackageInfoDetails> onCacheLoaded = default,
            Action<PackageInfoDetails> onLoaded = default)
        {
            var url = Settings.AdmobScope.OpenUpmInfoUrl;
            var synchronizationContext = SynchronizationContext.Current;
            var details = await _fetcher.FetchPackageInfo(
                url,
                superReload,
                installedPackages,
                token,
                cachedDetails =>
                {
                    PrepareGoogleAdsDetails(cachedDetails);
                    InvokeOnMainThread(synchronizationContext, () => onCacheLoaded?.Invoke(cachedDetails));
                });
            PrepareGoogleAdsDetails(details);
            InvokeOnMainThread(synchronizationContext, () => onLoaded?.Invoke(details));
            return details;
        }

        static void InvokeOnMainThread(SynchronizationContext context, Action action)
        {
            if (action == default)
            {
                return;
            }
            if (context == default || SynchronizationContext.Current == context)
            {
                action();
                return;
            }
            context.Post(_ => action(), default);
        }

        static void PrepareGoogleAdsDetails(PackageInfoDetails details)
        {
            if (details?.Remote == default)
            {
                return;
            }
            details.Remote.DisplayName = (details.Remote.DisplayName ?? string.Empty).Replace("for Unity", string.Empty);
        }

        public async Task FetchMediationPackage(
            Action<int, PackageInfoDetails> onLoadedAction,
            bool superReload,
            IEnumerable<PackageInfo> installedPackages,
            CancellationToken token = default)
        {
            System.Threading.Interlocked.Increment(ref _activeMediationCount);
            try
            {
                var synchronizationContext = SynchronizationContext.Current;
                var tasks = new Task<PackageInfoDetails>[Settings.MediationScopes.Length];
                for (var i = 0; i < Settings.MediationScopes.Length; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    var index = i;
                    tasks[i] = _fetcher.FetchPackageInfo(
                        Settings.MediationScopes[i].OpenUpmInfoUrl,
                        superReload,
                        installedPackages,
                        token,
                        cachedDetails =>
                        {
                            PrepareMediationDetails(cachedDetails);
                            InvokeOnMainThread(synchronizationContext, () => onLoadedAction?.Invoke(index, cachedDetails));
                        });
                    tasks[i].ContinueOnMainThread(task =>
                    {
                        var details = task.Result;
                        PrepareMediationDetails(details);
                        onLoadedAction?.Invoke(index, details);
                    }, onCancel: () => { }, cancellationToken: token);
                }
                await Task.WhenAll(tasks);
            }
            finally
            {
                System.Threading.Interlocked.Decrement(ref _activeMediationCount);
            }
        }

        static void PrepareMediationDetails(PackageInfoDetails details)
        {
            if (details?.Remote == default)
            {
                return;
            }
            details.Remote.DisplayName = (details.Remote.DisplayName ?? string.Empty)
                                        .Replace("Google Mobile Ads", string.Empty)
                                        .Replace("Mediation", string.Empty)
                                        .Replace("for Unity", string.Empty);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            Installer?.Dispose();
            _fetcher?.Dispose();
        }
    }
}
