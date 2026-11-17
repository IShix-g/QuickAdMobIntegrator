using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using QuickAdMobIntegrator.Editor;

namespace Tests.QuickAdMobIntegrator
{
    public sealed class QuickAdMobIntegratorApiTests
    {
        [Test]
        public void PublicApiExposesNonInteractiveConfigurationAndInstallMethods()
        {
            Assert.That(typeof(QuickAdMobIntegratorApi).IsPublic, Is.True);
            AssertMethod("GetPackageStatusesAsync", typeof(Task<QuickAdMobPackageStatus[]>));
            AssertMethod("SetUpRegistry", typeof(void));
            AssertMethod("SetMediationEnabled", typeof(void));
            AssertMethod("SetFixedVersion", typeof(void));
            AssertMethod("InstallOrUpdatePackageAsync", typeof(Task));
            AssertMethod("InstallAllAsync", typeof(Task));
        }

        [Test]
        public void PackageStatusExposesVersionsAndInstallState()
        {
            var status = new QuickAdMobPackageStatus("com.example", "1.0.0", "2.0.0", true, false, true, true, false);

            Assert.That(status.PackageId, Is.EqualTo("com.example"));
            Assert.That(status.InstalledVersion, Is.EqualTo("1.0.0"));
            Assert.That(status.TargetVersion, Is.EqualTo("2.0.0"));
            Assert.That(status.IsEnabled, Is.True);
            Assert.That(status.IsRequired, Is.False);
            Assert.That(status.IsInstalled, Is.True);
            Assert.That(status.HasUpdate, Is.True);
            Assert.That(status.IsRegistryConfigured, Is.False);
        }

        static void AssertMethod(string name, System.Type returnType)
        {
            var method = typeof(QuickAdMobIntegratorApi).GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, name + " should be public");
            Assert.That(method.ReturnType, Is.EqualTo(returnType), name + " return type");
        }
    }
}
