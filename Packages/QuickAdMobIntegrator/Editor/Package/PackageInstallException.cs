
using System;

namespace QuickAdMobIntegrator.Editor
{
    public sealed class PackageInstallException : Exception
    {
        public PackageInstallException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}