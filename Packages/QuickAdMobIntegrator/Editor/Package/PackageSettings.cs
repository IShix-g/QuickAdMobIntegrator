
using System;
using UnityEditor;
using UnityEngine;

namespace QuickAdMobIntegrator.Editor
{
    internal sealed class PackageSettings : ScriptableObject
    {
        public int SettingVersion;
        public ManifestRegistry Registry;
        public RequiredScope AdmobScope;
        public Scope[] MediationScopes;
        [TextArea] public string Notes;
        
        void OnDisable()
        {
            Save();
        }
        
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
            AssetDatabase.SaveAssetIfDirty(this);
        }
        
        public void Update130()
        {
            {
                if (AdmobScope is IUpdate130Target target)
                {
                    target.Update();
                }
            }
            foreach (var scope in MediationScopes)
            {
                if (scope is IUpdate130Target target)
                {
                    target.Update();
                }
            }
        }
        
        [Serializable]
        public class RequiredScope : IScope, IUpdate130Target
        {
            [SerializeField] string _openUpmInfoUrl;
            [SerializeField] string _helpUrl;
            [SerializeField] string _fixedVersion;

            public bool IsEnabled => true;
            public bool IsRequired => true;
            public string OpenUpmInfoUrl => _openUpmInfoUrl;
            public string HelpUrl => _helpUrl;
            public string FixedVersion
            {
                get => _fixedVersion;
                set => _fixedVersion = value;
            }

            void IUpdate130Target.Update()
            {
                var target = "/latest";
                if (_openUpmInfoUrl.EndsWith(target))
                {
                    _openUpmInfoUrl = _openUpmInfoUrl.Substring(0, _openUpmInfoUrl.Length - target.Length);
                }
            }
        }
        
        [Serializable]
        public class Scope : IScope, IUpdate130Target
        {
            [SerializeField] bool _isEnabled = true;
            [SerializeField] string _openUpmInfoUrl;
            [SerializeField] string _helpUrl;
            [SerializeField] string _fixedVersion;

            public bool IsRequired => false;
            public bool IsEnabled
            {
                get => _isEnabled;
                set => _isEnabled = value;
            }
            public string OpenUpmInfoUrl => _openUpmInfoUrl;
            public string HelpUrl => _helpUrl;
            public string FixedVersion
            {
                get => _fixedVersion;
                set => _fixedVersion = value;
            }
            
            void IUpdate130Target.Update()
            {
                var target = "/latest";
                if (_openUpmInfoUrl.EndsWith(target))
                {
                    _openUpmInfoUrl = _openUpmInfoUrl.Substring(0, _openUpmInfoUrl.Length - target.Length);
                }
            }
        }
        
        public interface IScope
        {
            public bool IsRequired { get; }
            public bool IsEnabled { get; }
            public string OpenUpmInfoUrl { get; }
            public string HelpUrl { get; }
            public string FixedVersion { get; set; }
            
            public bool HasFixedVersion => !string.IsNullOrEmpty(FixedVersion);
        }
    }
}