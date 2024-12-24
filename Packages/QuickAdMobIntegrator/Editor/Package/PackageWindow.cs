
using System;
using System.Collections.Generic;
using System.Threading;
using QuickAdMobIntegrator.Admob.Editor;
using UnityEditor;
using UnityEngine;

namespace QuickAdMobIntegrator.Editor
{
    public class PackageWindow : EditorWindow
    {
        static readonly string[] s_textureDirectories = { QAIManagerFactory.PackageRootPath };
        
        [MenuItem("Window/Quick AdMob Integrator")]
        public static void Open() => Open(IsFirstOpen);
        
        public static void Open(bool superReload)
        {
            var window = GetWindow<PackageWindow>("Quick AdMob Integrator");
            window._superReload = superReload;
            window.Show();
        }
        
        public static bool IsFirstOpen
        {
            get => SessionState.GetBool("QuickAdMobIntegrator_PackageWindow_IsFirstOpen", true);
            set => SessionState.SetBool("QuickAdMobIntegrator_PackageWindow_IsFirstOpen", value);
        }
        
        GUIContent _installedIcon;
        GUIContent _updateIcon;
        GUIContent _refreshIcon;
        GUIContent _settingIcon;
        GUIContent _backIcon;
        GUIContent _helpIcon;
        GUIContent _completedIcon;
        GUIContent _notCompletedIcon;
        Texture2D _logo;
        Vector2 _scrollPos;
        QAIManager _manager;
        PackageInfoDetails _googleAdsPackageInfo;
        CancellationTokenSource _tokenSource;
        PackageInfoDetails[] _mediationPackageInfos;
        CancellationTokenSource _mediationTokenSource;
        AdMobSettingsValidator _adMobSettingsValidator;
        bool _isSettingMode;
        bool _superReload;
        bool _showFoldout = true;

        void OnEnable()
        {
            _installedIcon = EditorGUIUtility.IconContent("Progress");
            _updateIcon = EditorGUIUtility.IconContent("Update-Available");
            _refreshIcon = EditorGUIUtility.IconContent("Refresh");
            _settingIcon = EditorGUIUtility.IconContent("Settings");
            _backIcon = EditorGUIUtility.IconContent("back");
            _helpIcon = EditorGUIUtility.IconContent("_Help");
            _completedIcon = EditorGUIUtility.IconContent("winbtn_mac_max");
            _notCompletedIcon = EditorGUIUtility.IconContent("winbtn_mac_close");
            _logo = GetLogo();
            
            _adMobSettingsValidator = new AdMobSettingsValidator();
            _manager = QAIManagerFactory.Create();
            if (_manager.IsCompletedRegistrySetUp)
            {
                ReloadPackages(_superReload);
            }
            _isSettingMode = false;
            _superReload = false;
            IsFirstOpen = false;
        }
        
        void OnDisable()
        {
            _manager.Dispose();
            _installedIcon = default;
            _updateIcon = default;
            _refreshIcon = default;
            _settingIcon = default;
            _backIcon = default;
            _helpIcon = default;
            _logo = default;
            _tokenSource?.SafeCancelAndDispose();
            _mediationTokenSource?.SafeCancelAndDispose();
            EditorApplication.delayCall -= ReloadPackagesNextFrame;
        }
        
        void OnGUI()
        {
            if (_manager.IsCompletedRegistrySetUp)
            {
                EditorGUI.BeginDisabledGroup(_manager.IsProcessing);
                GUILayout.BeginHorizontal(new GUIStyle() { padding = new RectOffset(5, 5, 5, 5) });
            
                var width = GUILayout.Width(33);
                var height = GUILayout.Height(EditorGUIUtility.singleLineHeight + 5);
                var settingIcon = _isSettingMode ? _backIcon : _settingIcon;
                var clickedOpenSetting = GUILayout.Button(settingIcon, width, height);
                var clickedStartReload = GUILayout.Button(_refreshIcon, width, height);
                var clickedOpenManager = GUILayout.Button("Package Manager", height);
            
                GUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
                
                if (clickedStartReload)
                {
                    _isSettingMode = false;
                    ReloadPackages(true);
                }
                else if (clickedOpenSetting)
                {
                    _isSettingMode = !_isSettingMode;
                }
                else if (clickedOpenManager)
                {
                    _isSettingMode = false;
                    PackageInstaller.OpenPackageManager();
                }
            }
            
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    padding = new RectOffset(5, 5, 5, 5),
                    alignment = TextAnchor.MiddleCenter,
                };
                GUILayout.Label(_logo, style, GUILayout.MinWidth(430), GUILayout.Height(75));
            }
            
            if (!_manager.IsCompletedRegistrySetUp)
            {
                GUILayout.Space(20);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                var width = GUILayout.MaxWidth(250);
                var height = GUILayout.Height(50);
                if (GUILayout.Button("Set up required registries\nfor Scoped Registries", width, height))
                {
                    _manager.SetUpRegistry();
                    ReloadPackages(true);
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return;
            }
            
            GUILayout.Space(5);
            
            if (_googleAdsPackageInfo == default)
            {
                GUILayout.Label("Now Loading...");
                return;
            }
            
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Width(position.width));
            
            {
                GUILayout.Space(10);
                var style = new GUIStyle(EditorStyles.foldout)
                {
                    fontSize = 13,
                    margin = new RectOffset(5, 0 ,0 ,0)
                };
                _showFoldout = EditorGUILayout.Foldout(_showFoldout, "Essential Setup Steps", style);
            }

            if (_showFoldout)
            {
                GUILayout.Space(15);
                DrawChecklistItem(
                    $"Please install {_googleAdsPackageInfo.Remote.displayName} first.",
                    _googleAdsPackageInfo.IsInstalled,
                    !_manager.IsProcessing && !_googleAdsPackageInfo.IsInstalled,
                    _googleAdsPackageInfo.IsInstalled ? "Installed" : "Install",
                    () =>
                    {
                        if (!_googleAdsPackageInfo.IsInstalled)
                        {
                            InstallPackage(_googleAdsPackageInfo.PackageInstallUrl);
                        }
                    }
                );

                GUILayout.Space(5);
            
                DrawChecklistItem(
                    "Set up the Google Mobile Ads App ID", 
                    _adMobSettingsValidator.IsValid,
                    !_manager.IsProcessing && _googleAdsPackageInfo.IsInstalled,
                    _adMobSettingsValidator.IsValid ? "Configured" : "Set Up",
                    () => _adMobSettingsValidator.OpenSettings()
                );
                
                GUILayout.Space(20);
            }
            
            {
                var boxStyle = new GUIStyle()
                {
                    padding = new RectOffset(8, _isSettingMode ? 78 : 8, 0, 0),
                };
                var textStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fixedHeight = 30,
                    wordWrap = true,
                };
                GUILayout.BeginHorizontal(boxStyle);
                
                GUILayout.Label("SDK", textStyle);
                
                if (!_isSettingMode)
                {
                    EditorGUI.BeginDisabledGroup(_manager.IsProcessing || !_googleAdsPackageInfo.IsInstalled);
                    if (GUILayout.Button("Install All", GUILayout.Width(70)))
                    {
                        var userAgreed = EditorUtility.DisplayDialog(
                            "Install All",
                            "This will install all the SDKs and Mediations listed below. If there are updates available, they will be applied as well.",
                            "Install All",
                            "Close"
                        );

                        if (userAgreed)
                        {
                            var packages = GetEnablePackages();
                            if (packages.Length > 0)
                            {
                                _tokenSource = new CancellationTokenSource();
                                _manager.Installer.Install(packages, _tokenSource.Token)
                                    .Handled(_ =>
                                    {
                                        _tokenSource?.Dispose();
                                        _tokenSource = default;
                                    });
                            }
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                GUILayout.EndHorizontal();
            }
            
            DrawPackage(_googleAdsPackageInfo, isSettingMode:_isSettingMode, isActiveButton:true);

            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(8, 78, 10, 10)
                };
                GUILayout.Label("Mediation", style, GUILayout.ExpandWidth(true), GUILayout.Height(30));
            }
            
            EditorGUI.BeginChangeCheck();
            
            foreach (var packageInfo in _mediationPackageInfos)
            {
                if (packageInfo is {IsLoaded: true})
                {
                    DrawPackage(packageInfo, isSettingMode:_isSettingMode, isActiveButton:_googleAdsPackageInfo.IsInstalled);
                }
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                _manager.Settings.Save();
            }
            
            if (_isSettingMode)
            {
                EditorGUI.BeginDisabledGroup(_manager.IsProcessing);
                GUILayout.Space(10);
                if (GUILayout.Button("Remove all installed packages.", GUILayout.Height(EditorGUIUtility.singleLineHeight + 4)))
                {
                    var userAgreed = EditorUtility.DisplayDialog(
                        "Remove All",
                        "Remove all installed SDKs and Mediations listed.",
                        "Remove All",
                        "Close"
                    );

                    if (userAgreed)
                    {
                        var packages = GetInstalledPackages();
                        if (packages.Length > 0)
                        {
                            _tokenSource = new CancellationTokenSource();
                            _manager.Installer.UnInstall(packages, _tokenSource.Token)
                                .Handled(_ =>
                                {
                                    _tokenSource?.Dispose();
                                    _tokenSource = default;
                                });
                        }
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            
            GUILayout.EndScrollView();
        }
        
        void ReloadPackages(bool superReload)
        {
            _adMobSettingsValidator.Validate();
            
            _tokenSource = new CancellationTokenSource();
            _manager.FetchGoogleAdsPackageInfo(superReload, _tokenSource.Token)
                .Handled(task =>
                {
                    _googleAdsPackageInfo = task.Result;
                    _tokenSource?.Dispose();
                    _tokenSource = default;
                    Repaint();
                });

            var length = _manager.Settings.MediationScopes.Length;
            _mediationPackageInfos = new PackageInfoDetails[length];
            _mediationTokenSource = new CancellationTokenSource();
            _manager.FetchMediationPackage((index, details) =>
                {
                    _mediationPackageInfos[index] = details;
                    Repaint();
                },
                superReload,
                _mediationTokenSource.Token)
                .Handled(_ =>
                {
                    _mediationTokenSource?.Dispose();
                    _mediationTokenSource = default;
                });
        }
        
        void DrawPackage(PackageInfoDetails details, bool isSettingMode, bool isActiveButton)
        {
                var setting = _manager.Settings.GetByName(details.Remote.name);
                if (!setting.IsRequired
                    && !isSettingMode
                    && !setting.IsEnabled)
                {
                    return;
                }

                GUILayout.BeginHorizontal(GUI.skin.box);
                
                var versionText = ToLocalVersionString(details);
                if (details.HasUpdate
                    && !details.IsFixedVersion)
                {
                    if (details.IsInstalled)
                    {
                        versionText += " \u2192 ";
                    }
                    versionText += ToServerVersionString(details);
                }
                
                if (string.IsNullOrEmpty(versionText))
                {
                    versionText = details.IsFixedVersion
                        ? ToFixedVersion(details)
                        : " v---";
                }
                
                if (details.IsFixedVersion)
                {
                    versionText += " (Fixed)";
                }
                
                var displayName = details.IsLoaded
                        ? details.Remote.displayName
                        : "Unknown";
                
                if (isSettingMode)
                {
                    var color = GUI.color;
                    if (setting.IsRequired)
                    {
                        GUI.color = Color.gray;
                    }
                    else if (details.IsInstalled)
                    {
                        GUI.color = Color.green;
                    }
                    
                    var isEnabled = GUILayout.Toggle(
                            setting.IsRequired
                            || details.IsInstalled
                            || setting.IsEnabled,
                            " " + displayName,
                            GUILayout.Width(180));

                    if (isEnabled != setting.IsEnabled
                        && setting is PackageSettings.Scope scope)
                    {
                        scope.IsEnabled = isEnabled;
                    }
                    GUI.color = color;
                }
                else if (!string.IsNullOrEmpty(setting.HelpUrl))
                {
                    GUILayout.BeginHorizontal(GUILayout.Width(170));
                    var buttonStyle = new GUIStyle(GUI.skin.label)
                    {
                        fixedWidth = 150,
                        fixedHeight = EditorGUIUtility.singleLineHeight,
                    };
                    var clickedHelp = GUILayout.Button(displayName, buttonStyle);
                    var iconButtonStyle = new GUIStyle(GUI.skin.label)
                    {
                        fixedWidth = 20,
                        fixedHeight = EditorGUIUtility.singleLineHeight,
                    };
                    clickedHelp |= GUILayout.Button(_helpIcon, iconButtonStyle);
                    GUILayout.EndHorizontal();
                    
                    var lastRect = GUILayoutUtility.GetLastRect();
                    EditorGUIUtility.AddCursorRect(lastRect, MouseCursor.Link);
                    
                    if (clickedHelp)
                    {
                        Application.OpenURL(setting.HelpUrl);
                    }
                }
                else
                {
                    GUILayout.Label(displayName, GUILayout.Width(170));
                }
                
                {
                    var color = GUI.color;
                    if (details.IsInstalled
                        && details.HasUpdate
                        && !details.IsFixedVersion)
                    {
                        GUI.color = Color.yellow;
                    }
                    GUILayout.Label(versionText);
                    GUI.color = color;
                }
                
                if (details.IsInstalled)
                {
                    var width = GUILayout.Width(22);
                    var height = GUILayout.Height(EditorGUIUtility.singleLineHeight);
                    var icon = details.HasUpdate && !details.IsFixedVersion ? _updateIcon : _installedIcon;
                    GUILayout.Label(icon, width, height);
                }
                
                if(!isSettingMode)
                {
                    var buttonText = GetButtonText(details);
                    EditorGUI.BeginDisabledGroup(!isActiveButton || !details.IsLoaded || _manager.IsProcessing);
                    if (GUILayout.Button(buttonText, GUILayout.Width(70)))
                    {
                        if (details.HasUpdate
                            && !details.IsFixedVersion
                            || !details.IsInstalled)
                        {
                            InstallPackage(details.PackageInstallUrl);
                        }
                        else if(details.IsInstalled)
                        {
                            _tokenSource?.SafeCancelAndDispose();
                            _tokenSource = new CancellationTokenSource();
                            _manager.Installer.UnInstall(details.Local.name, _tokenSource.Token)
                                .Handled(_ =>
                                {
                                    _tokenSource?.Dispose();
                                    _tokenSource = default;
                                    EditorApplication.delayCall += ReloadPackagesNextFrame;
                                });
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                GUILayout.EndHorizontal();
        }

        void InstallPackage(string packageInstallUrl)
        {
            _tokenSource?.SafeCancelAndDispose();
            _tokenSource = new CancellationTokenSource();
            _manager.Installer.Install(packageInstallUrl, _tokenSource.Token)
                .Handled(_ =>
                {
                    _tokenSource?.Dispose();
                    _tokenSource = default;
                    EditorApplication.delayCall += ReloadPackagesNextFrame;
                });
        }
        
        void DrawChecklistItem(string label, bool isComplete, bool buttonActive, string buttonName, Action onClick)
        {
            var style = new GUIStyle() {padding = new RectOffset(22, 8, 0, 3)};
            GUILayout.BeginHorizontal(style);
            GUILayout.Label(isComplete ? _completedIcon : _notCompletedIcon, GUILayout.Width(15), GUILayout.Height(15));
            GUILayout.Label(label);

            EditorGUI.BeginDisabledGroup(!buttonActive);
            if (GUILayout.Button(buttonName, GUILayout.Width(75)))
            {
                onClick?.Invoke();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();
        }
        
        void ReloadPackagesNextFrame()
        {
            EditorApplication.delayCall -= ReloadPackagesNextFrame;
            ReloadPackages(false);
        }
        
        string GetButtonText(PackageInfoDetails details)
        {
            if (!details.IsInstalled)
            {
                return "Install";
            }
            if (details.HasUpdate
                && !details.IsFixedVersion)
            {
                return "Update";
            }
            return "Remove";
        }
        
        string ToLocalVersionString(PackageInfoDetails details)
            => details.IsInstalled ? "v" + details.Local.version : string.Empty;

        string ToServerVersionString(PackageInfoDetails details)
            => details.IsLoaded ? "v" + details.Remote.version : string.Empty;
        
        string ToFixedVersion(PackageInfoDetails details)
        {
            if (details.IsInstalled)
            {
                return "v" + details.Local.version;
            }
            return "v" + details.GetVersionParam();
        }


        string[] GetEnablePackages()
        {
            var packages = new List<string>
            {
                _googleAdsPackageInfo.PackageInstallUrl
            };
            
            foreach (var details in _mediationPackageInfos)
            {
                var setting = _manager.Settings.GetByName(details.Remote.name);
                if (setting.IsEnabled)
                {
                    packages.Add(details.PackageInstallUrl);
                }
            }
            return packages.ToArray();
        }
        
        string[] GetInstalledPackages()
        {
            var packages = new List<string>();
            if (_googleAdsPackageInfo.IsInstalled)
            {
                packages.Add(_googleAdsPackageInfo.PackageInstallUrl);
            }
            
            foreach (var details in _mediationPackageInfos)
            {
                var setting = _manager.Settings.GetByName(details.Remote.name);
                if (setting.IsEnabled
                    && details.IsInstalled)
                {
                    packages.Add(details.PackageInstallUrl);
                }
            }
            return packages.ToArray();
        }
        
        static Texture2D GetLogo()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D header", s_textureDirectories);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            return default;
        }
    }
}