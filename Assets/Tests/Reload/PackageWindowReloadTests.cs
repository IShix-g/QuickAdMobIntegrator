using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuickAdMobIntegrator.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using Object = UnityEngine.Object;

namespace Tests.QuickAdMobIntegrator
{
    public sealed class PackageWindowReloadTests
    {
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        PackageWindow _window;
        ControlledReloadSource _source;
        QueuedContext _context;
        SynchronizationContext _previousContext;
        bool _firstOpen;
        bool _showSetup;

        [SetUp]
        public void SetUp()
        {
            _previousContext = SynchronizationContext.Current;
            _context = new QueuedContext();
            SynchronizationContext.SetSynchronizationContext(_context);
            _firstOpen = PackageWindow.IsFirstOpen;
            _showSetup = PackageWindow.IsShowSetUpFoldout;
            _window = ScriptableObject.CreateInstance<PackageWindow>();
            _window.hideFlags = HideFlags.HideAndDontSave;
            // Keep Initialize from starting real Package Manager or network requests.
            var initialize = (EditorApplication.CallbackFunction)Delegate.CreateDelegate(
                typeof(EditorApplication.CallbackFunction), _window, Method("Initialize"));
            EditorApplication.delayCall -= initialize;
            _source = new ControlledReloadSource();
            Field("_reloadSource").SetValue(_window, _source);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (_window != null) Object.DestroyImmediate(_window);
                if (_source != null)
                {
                    foreach (var batch in _source.Batches) batch.Cancel();
                }
                _context.Drain();
                PackageWindow.IsFirstOpen = _firstOpen;
                PackageWindow.IsShowSetUpFoldout = _showSetup;
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(_previousContext);
            }
        }

        [Test]
        public void ReloadKeepsExistingRowsWhenOnePackageFails()
        {
            var initial = StartReload();
            var google = Details("google", "1.0.0");
            var first = Details("first-mediation", "1.0.0");
            var second = Details("second-mediation", "1.0.0");
            initial.PublishGoogle(google);
            initial.PublishMediation(0, first);
            initial.PublishMediation(1, second);
            initial.Complete();
            _context.Drain();

            var reload = StartReload();
            Assert.That(GoogleRow, Is.SameAs(google), "Google Ads must stay visible during reload");
            Assert.That(MediationRows[0], Is.SameAs(first));
            Assert.That(MediationRows[1], Is.SameAs(second));
            var updated = Details("first-mediation", "2.0.0");
            reload.PublishMediation(0, updated);
            // Only the Google Ads request fails; the mediation request succeeds.
            LogAssert.Expect(LogType.Exception, new Regex("issue-41-intentional-fetch-failure"));
            reload.FailGoogle();
            _context.Drain();

            Assert.That(GoogleRow, Is.SameAs(google), "A failed fetch must preserve its existing row");
            Assert.That(MediationRows[0], Is.SameAs(updated), "A successful fetch must still update its row");
            Assert.That(MediationRows[1], Is.SameAs(second));
        }

        [Test]
        public void DelayedCallbacksFromOldReloadCannotOverwriteNewRows()
        {
            var old = StartReload();
            var oldSource = (CancellationTokenSource)Field("_reloadTokenSource").GetValue(_window);
            // Queue callbacks before cancellation, then deliver them after the new reload.
            _context.Post(_ => old.PublishGoogle(Details("google", "0.1.0"), cached: true), null);
            _context.Post(_ => old.PublishGoogle(Details("google", "0.2.0")), null);
            _context.Post(_ => old.PublishMediation(0, Details("first-mediation", "0.1.0")), null);
            _context.Post(_ => old.PublishMediation(1, Details("second-mediation", "0.1.0")), null);

            var current = StartReload();
            Assert.That(old.Token.IsCancellationRequested, Is.True);
            var google = Details("google", "2.0.0");
            var first = Details("first-mediation", "2.0.0");
            var second = Details("second-mediation", "2.0.0");
            current.PublishGoogle(google);
            current.PublishMediation(0, first);
            current.PublishMediation(1, second);
            old.Complete();
            _context.Drain();

            Assert.Throws<ObjectDisposedException>(() => { var token = oldSource.Token; },
                "The old completion must actually run before checking the displayed rows");
            Assert.That(GoogleRow, Is.SameAs(google), "Neither old cache nor old final results may replace the current row");
            Assert.That(MediationRows[0], Is.SameAs(first));
            Assert.That(MediationRows[1], Is.SameAs(second));
            current.Complete();
            _context.Drain();
        }

        [Test]
        public void OldCompletionPreservesNewReloadCancellationUntilWindowCloses()
        {
            var old = StartReload();
            var oldSource = (CancellationTokenSource)Field("_reloadTokenSource").GetValue(_window);
            var current = StartReload();
            var currentSource = (CancellationTokenSource)Field("_reloadTokenSource").GetValue(_window);
            old.Complete();
            _context.Drain();

            Assert.Throws<ObjectDisposedException>(() => { var token = oldSource.Token; });
            Assert.That(Field("_reloadTokenSource").GetValue(_window), Is.SameAs(currentSource),
                "Old completion must not clear the new reload's cancellation source");
            Assert.That(currentSource.Token.IsCancellationRequested, Is.False,
                "Accessing Token also proves the new source was not disposed by old completion");
            var cancellationObserved = false;
            using (current.Token.Register(() => cancellationObserved = true))
            {
                Object.DestroyImmediate(_window);
                Assert.That(cancellationObserved, Is.True, "Closing must still cancel the in-flight new request");
                Assert.That(current.Token.IsCancellationRequested, Is.True);
            }
            current.Cancel();
            _context.Drain();
        }

        ReloadBatch StartReload()
        {
            Method("ReloadPackages").Invoke(_window, new object[] { true });
            Assert.That(_source.Batches.Last().OnGoogleLoaded, Is.Not.Null);
            Assert.That(_source.Batches.Last().OnMediationLoaded, Is.Not.Null);
            return _source.Batches.Last();
        }

        PackageInfoDetails GoogleRow => (PackageInfoDetails)Field("_googleAdsPackageInfo").GetValue(_window);
        PackageInfoDetails[] MediationRows => (PackageInfoDetails[])Field("_mediationPackageInfos").GetValue(_window);
        static FieldInfo Field(string name) => typeof(PackageWindow).GetField(name, PrivateInstance)
            ?? throw new MissingFieldException(typeof(PackageWindow).FullName, name);
        static MethodInfo Method(string name) => typeof(PackageWindow).GetMethod(name, PrivateInstance)
            ?? throw new MissingMethodException(typeof(PackageWindow).FullName, name);

        static PackageInfoDetails Details(string name, string version)
            => new PackageInfoDetails(default, new PackageRemoteInfo
            {
                Name = name, DisplayName = name, LatestVersion = version, Versions = new[] { version }
            }, name);

        sealed class ControlledReloadSource : IPackageReloadSource
        {
            public int MediationCount => 2;
            public readonly List<ReloadBatch> Batches = new List<ReloadBatch>();

            public Task<IEnumerable<PackageInfo>> GetInstalledPackages(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                Batches.Add(new ReloadBatch(token));
                return Task.FromResult(Enumerable.Empty<PackageInfo>());
            }

            public Task<PackageInfoDetails> FetchGoogleAdsPackageInfo(
                bool superReload, IEnumerable<PackageInfo> installedPackages, CancellationToken token,
                Action<PackageInfoDetails> onCacheLoaded, Action<PackageInfoDetails> onLoaded)
            {
                var batch = Batches.Last();
                batch.OnGoogleCacheLoaded = onCacheLoaded;
                batch.OnGoogleLoaded = onLoaded;
                return batch.GoogleCompletion.Task;
            }

            public Task FetchMediationPackage(Action<int, PackageInfoDetails> onLoadedAction,
                bool superReload, IEnumerable<PackageInfo> installedPackages, CancellationToken token)
            {
                var batch = Batches.Last();
                batch.OnMediationLoaded = onLoadedAction;
                return batch.MediationCompletion.Task;
            }
        }

        sealed class ReloadBatch
        {
            public readonly CancellationToken Token;
            public readonly TaskCompletionSource<PackageInfoDetails> GoogleCompletion = new TaskCompletionSource<PackageInfoDetails>();
            public readonly TaskCompletionSource<bool> MediationCompletion = new TaskCompletionSource<bool>();
            public Action<PackageInfoDetails> OnGoogleCacheLoaded;
            public Action<PackageInfoDetails> OnGoogleLoaded;
            public Action<int, PackageInfoDetails> OnMediationLoaded;
            PackageInfoDetails _google;

            public ReloadBatch(CancellationToken token) => Token = token;

            // Deliberately allow late callbacks after cancellation to exercise the window's own guards.
            public void PublishGoogle(PackageInfoDetails details, bool cached = false)
            {
                _google = details;
                if (cached) OnGoogleCacheLoaded(details);
                else OnGoogleLoaded(details);
            }

            public void PublishMediation(int index, PackageInfoDetails details) => OnMediationLoaded(index, details);

            public void Complete()
            {
                GoogleCompletion.TrySetResult(_google);
                MediationCompletion.TrySetResult(true);
            }

            public void FailGoogle()
            {
                GoogleCompletion.TrySetException(new InvalidOperationException("issue-41-intentional-fetch-failure"));
                MediationCompletion.TrySetResult(true);
            }

            public void Cancel()
            {
                GoogleCompletion.TrySetCanceled();
                MediationCompletion.TrySetCanceled();
            }
        }

        sealed class QueuedContext : SynchronizationContext
        {
            readonly Queue<Action> _callbacks = new Queue<Action>();

            public override void Post(SendOrPostCallback callback, object state)
            {
                lock (_callbacks) _callbacks.Enqueue(() => callback(state));
            }

            public void Drain()
            {
                for (var i = 0; i < 100; i++)
                {
                    Action callback;
                    lock (_callbacks)
                    {
                        if (_callbacks.Count == 0) return;
                        callback = _callbacks.Dequeue();
                    }
                    callback();
                }
                Assert.Fail("Reload callbacks did not settle");
            }
        }
    }
}
