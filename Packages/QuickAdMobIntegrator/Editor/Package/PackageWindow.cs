
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using QuickAdMobIntegrator.Admob.Editor;
using UnityEditor;
using UnityEngine;

namespace QuickAdMobIntegrator.Editor
{
    internal class PackageWindow : EditorWindow
    {
        const string _gitURL = "https://github.com/IShix-g/QuickAdMobIntegrator";
        const string _gitInstallUrl = _gitURL + ".git?path=Packages/QuickAdMobIntegrator";
        const string _gitBranchName = "main";
        const string _packageName = "com.ishix.quickadmobintegrator";
        
        [MenuItem("Window/Quick AdMob Integrator")]
        public static void Open() => Open(IsFirstOpen);
        
        public static void Open(bool superReload)
        {
            var window = GetWindow<PackageWindow>("Quick AdMob Integrator");
            window._superReload = superReload;
            window.Show();
        }
        
        static readonly string[] s_textureDirectories = { QAIManagerFactory.PackageRootPath };
        public static bool IsFirstOpen
        {
            get => SessionState.GetBool("QuickAdMobIntegrator_PackageWindow_IsFirstOpen", true);
            set => SessionState.SetBool("QuickAdMobIntegrator_PackageWindow_IsFirstOpen", value);
        }
        public static bool IsShowSetUpFoldout
        {
            get => SessionState.GetBool("QuickAdMobIntegrator_PackageWindow_IsShowSetUpFoldout", true);
            set => SessionState.SetBool("QuickAdMobIntegrator_PackageWindow_IsShowSetUpFoldout", value);
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
        Vector2 _textAreaScrollPos;
        QAIManager _manager;
        PackageInfoDetails _googleAdsPackageInfo;
        CancellationTokenSource _tokenSource;
        PackageInfoDetails[] _mediationPackageInfos;
        CancellationTokenSource _mediationTokenSource;
        AdMobSettingsValidator _adMobSettingsValidator;
        readonly PackageVersionChecker _versionChecker = new (_gitInstallUrl, _gitBranchName, _packageName);
        bool _isSettingMode;
        bool _superReload;
        bool _isShowSetUpFoldout;
        bool _isUpdated;

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
            _isSettingMode = false;
            _superReload = false;
            IsFirstOpen = false;
            _isShowSetUpFoldout = IsShowSetUpFoldout;
            
            EditorApplication.delayCall += Initialize;
            _versionChecker.Fetch().Handled();
        }

        void Initialize()
        {
            _manager = QAIManagerFactory.Create();
            if (_manager.IsCompletedRegistrySetUp)
            {
                ReloadPackages(_superReload);
            }
        }

        void OnDisable()
        {
            _manager?.Dispose();
            _installedIcon = default;
            _updateIcon = default;
            _refreshIcon = default;
            _settingIcon = default;
            _backIcon = default;
            _helpIcon = default;
            _logo = default;
            _tokenSource?.SafeCancelAndDispose();
            _mediationTokenSource?.SafeCancelAndDispose();
            IsShowSetUpFoldout = _isShowSetUpFoldout;
            _versionChecker?.Dispose();
        }
        
        void OnGUI()
        {
            if (_manager == null)
            {
                return;
            }
            if (_manager.IsCompletedRegistrySetUp)
            {
                EditorGUI.BeginDisabledGroup(_manager.IsProcessing || _versionChecker.IsProcessing);
                GUILayout.BeginHorizontal(new GUIStyle() { padding = new RectOffset(5, 5, 5, 5) });
            
                var width = GUILayout.Width(33);
                var height = GUILayout.Height(EditorGUIUtility.singleLineHeight + 5);
                var settingIcon = _isSettingMode ? _backIcon : _settingIcon;
                var clickedOpenSetting = GUILayout.Button(settingIcon, width, height);
                var clickedStartReload = GUILayout.Button(_refreshIcon, width, height);
                var clickedOpenManager = GUILayout.Button("Package Manager", height);
                var pluginVersion = _versionChecker.IsLoaded ? _versionChecker.LocalInfo.VersionString : "---";
                var clickedVersion = GUILayout.Button(pluginVersion, GUILayout.Width(100), height);
            
                GUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
                
                if (clickedStartReload)
                {
                    _isSettingMode = false;
                    ReloadPackages(true);
                }
                else if (clickedOpenSetting)
                {
                    if (_isSettingMode && _isUpdated)
                    {
                        _isUpdated = false;
                        ReloadPackages(false);
                    }
                    _isSettingMode = !_isSettingMode;
                }
                else if (clickedOpenManager)
                {
                    _isSettingMode = false;
                    PackageInstaller.OpenPackageManager();
                }
                else if (_versionChecker.IsLoaded
                         && clickedVersion)
                {
                    _tokenSource = new CancellationTokenSource();
                    _versionChecker.Fetch()
                        .ContinueOnMainThread(task =>
                        {
                            _tokenSource?.Dispose();
                            _tokenSource = new CancellationTokenSource();
                            _versionChecker.CheckVersion(_tokenSource.Token);
                        });
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
                _isShowSetUpFoldout = EditorGUILayout.Foldout(_isShowSetUpFoldout, "Essential Setup Steps", style);
            }

            if (_isShowSetUpFoldout)
            {
                GUILayout.Space(15);
                DrawChecklistItem(
                    $"Please install {_googleAdsPackageInfo.Remote.DisplayName} first.",
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

            if (_isSettingMode)
            {
                EditorGUILayout.HelpBox("Always latest - Will always check for and notify about new Packages.", MessageType.Info);
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
                    EditorGUI.BeginDisabledGroup(_manager.IsProcessing || !_googleAdsPackageInfo.IsInstalled || _versionChecker.IsProcessing);
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
            
            foreach (var packageInfo in _mediationPackageInfos)
            {
                if (packageInfo is {IsLoaded: true})
                {
                    DrawPackage(packageInfo, isSettingMode:_isSettingMode, isActiveButton:_googleAdsPackageInfo.IsInstalled);
                }
            }
            
            if (_isSettingMode)
            {
                EditorGUI.BeginDisabledGroup(_manager.IsProcessing || _versionChecker.IsProcessing);
                GUILayout.Space(20);
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
            else
            {
                var boxStyle = new GUIStyle()
                {
                    padding = new RectOffset(10, 10, 0, 0),
                };
                var labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(8, 78, 10, 10)
                };
                var textAreaStyle = new GUIStyle(GUI.skin.textArea)
                {
                    padding = new RectOffset(5, 5, 5, 5)
                };
                GUILayout.BeginVertical(boxStyle);

                GUILayout.Label("Notes", labelStyle, GUILayout.ExpandWidth(true), GUILayout.Height(30));
                
                EditorGUI.BeginChangeCheck();
                
                _textAreaScrollPos = EditorGUILayout.BeginScrollView(_textAreaScrollPos,GUILayout.Height(110));
                
                _manager.Settings.Notes = GUILayout.TextArea(_manager.Settings.Notes, textAreaStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                
                EditorGUILayout.EndScrollView();
                
                if (EditorGUI.EndChangeCheck())
                {
                    _manager.Settings.Save();
                }
                
                GUILayout.EndVertical();
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
                var setting = _manager.Settings.GetByName(details.Remote.Name);
                if (!setting.IsRequired
                    && !isSettingMode
                    && !setting.IsEnabled)
                {
                    return;
                }

                if (setting.HasFixedVersion)
                {
                    details.SetFixedVersion(setting.FixedVersion);
                }
                else
                {
                    details.RemoveFixedVersion();
                }
                
                GUILayout.BeginHorizontal(GUI.skin.box);

                var displayName = details.IsLoaded
                        ? details.Remote.DisplayName
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
                        _isUpdated = true;
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
                
                if(!details.IsLoaded
                   || !isSettingMode)
                {
                    var versionText = ToLocalVersionString(details);
                    if (details.HasUpdate)
                    {
                        if (details.IsInstalled)
                        {
                            versionText += " \u2192 ";
                        }
                        versionText += setting.HasFixedVersion
                            ? setting.FixedVersion
                            : ToServerVersionString(details);
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
                    
                    var color = GUI.color;
                    if (details.IsInstalled
                        && details.HasUpdate)
                    {
                        GUI.color = Color.yellow;
                    }
                    GUILayout.Label(versionText);
                    GUI.color = color;
                }
                else if(details.IsLoaded)
                {
                    var versions = details.Remote.Versions;
                    var pool = ArrayPool<string>.Shared;
                    var array = pool.Rent(versions.Length);

                    try
                    {
                        for (var i = 0; i < versions.Length; i++)
                        {
                            array[i] = "v" + versions[i];
                        }
                        array[versions.Length] = "Always latest";
                        
                        var currentVersion = setting.HasFixedVersion
                            ? setting.FixedVersion
                            : array[versions.Length];
                        
                        var index = Array.IndexOf(versions, currentVersion);
                        if (index < 0)
                        {
                            index = versions.Length;
                        }
                        var newIndex = EditorGUILayout.Popup(index, array);
                        if (index != newIndex)
                        {
                            setting.FixedVersion = newIndex == versions.Length
                                                        ? string.Empty
                                                        : versions[newIndex];
                            _isUpdated = true;
                        }
                    }
                    finally
                    {
                        pool.Return(array, clearArray: true);
                    }
                }
                
                if(!isSettingMode)
                {
                    if (details.IsInstalled)
                    {
                        var width = GUILayout.Width(22);
                        var height = GUILayout.Height(EditorGUIUtility.singleLineHeight);
                        var icon = details.HasUpdate ? _updateIcon : _installedIcon;
                        GUILayout.Label(icon, width, height);
                    }

                    var buttonText = GetButtonText(details);
                    EditorGUI.BeginDisabledGroup(!isActiveButton || !details.IsLoaded || _manager.IsProcessing || _versionChecker.IsProcessing);
                    if (GUILayout.Button(buttonText, GUILayout.Width(70)))
                    {
                        if (details.HasUpdate
                            || !details.IsInstalled)
                        {
                            InstallPackage(details.PackageInstallUrl);
                        }
                        else if(details.IsInstalled)
                        {
                            _tokenSource?.SafeCancelAndDispose();
                            _tokenSource = new CancellationTokenSource();
                            _manager.Installer.UnInstall(details.Local.Name, _tokenSource.Token)
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
            if (details.HasUpdate)
            {
                return "Update";
            }
            return "Remove";
        }
        
        string ToLocalVersionString(PackageInfoDetails details)
            => details.IsInstalled ? "v" + details.Local.Version : string.Empty;

        string ToServerVersionString(PackageInfoDetails details)
            => details.IsLoaded ? "v" + details.Remote.LatestVersion : string.Empty;
        
        string ToFixedVersion(PackageInfoDetails details)
        {
            if (details.IsInstalled)
            {
                return "v" + details.Local.Version;
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
                var setting = _manager.Settings.GetByName(details.Remote.Name);
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
                var setting = _manager.Settings.GetByName(details.Remote.Name);
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