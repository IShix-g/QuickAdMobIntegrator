
using System;
using UnityEditor;
using UnityEngine;

namespace QuickAdMobIntegrator.Editor
{
    internal sealed class PackageSettings : ScriptableObject
    {
        public string SettingVersion;
        public ManifestRegistry Registry;
        public RequiredScope AdmobScope;
        public Scope[] MediationScopes;

        public IScope GetByName(string name)
        {
            if (AdmobScope.OpenUpmInfoUrl.Contains(name))
            {
                return AdmobScope;
            }
            foreach (var scope in MediationScopes)
            {
                if (scope.OpenUpmInfoUrl.Contains(name))
                {
                    return scope;
                }
            }
            throw new ArgumentException("Scope with url " + name + " not found");
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        
        [Serializable]
        public class RequiredScope : IScope
        {
            [SerializeField] string _openUpmInfoUrl;
            [SerializeField] string _helpUrl;

            public bool IsEnabled => true;
            public bool IsRequired => true;
            public string OpenUpmInfoUrl => _openUpmInfoUrl;

            public string HelpUrl => _helpUrl;
        }
        
        [Serializable]
        public class Scope : IScope
        {
            [SerializeField] bool _isEnabled = true;
            [SerializeField] string _openUpmInfoUrl;
            [SerializeField] string _helpUrl;

            public bool IsRequired => false;
            public bool IsEnabled
            {
                get => _isEnabled;
                set => _isEnabled = value;
            }
            public string OpenUpmInfoUrl => _openUpmInfoUrl;

            public string HelpUrl => _helpUrl;
        }
        
        public interface IScope
        {
            public bool IsRequired { get; }
            public bool IsEnabled { get; }
            public string OpenUpmInfoUrl { get; }
            public string HelpUrl { get; }
        }
    }
}