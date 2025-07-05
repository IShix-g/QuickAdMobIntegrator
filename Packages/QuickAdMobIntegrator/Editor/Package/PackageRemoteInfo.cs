
using System;

namespace QuickAdMobIntegrator.Editor
{
    [Serializable]
    internal class PackageRemoteInfo
    {
        public string Name;
        public string DisplayName;
        public string[] Versions;
        public string LatestVersion;
    }
}