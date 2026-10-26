
using System;

namespace QuickAdMobIntegrator.Editor
{
    [Serializable]
    public sealed class ManifestRegistry
    {
        public string name;
        public string url;
        public string[] scopes;

        public ManifestRegistry(string name, string url, string[] scopes)
        {
            this.name = name;
            this.url = url;
            this.scopes = scopes;
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name is null or empty.");
            }
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("url is null or empty.");
            }
            if (scopes == default
                || scopes.Length == 0)
            {
                throw new ArgumentException("scopes is null or empty.");
            }
        }
    }
}