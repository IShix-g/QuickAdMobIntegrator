
using System;

namespace QuickAdMobIntegrator.Editor
{
    internal sealed class PackageInstallException : Exception
    {
        public PackageInstallException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}