
using UnityEditor;
using UnityEngine;

namespace QuickAdMobIntegrator.Editor
{
    public sealed class CustomGradleDialog : EditorWindow
    {
        CustomGradleDialogContents _contents;
        Vector2 _scrollPos;
        Texture2D _logo;
        float _initialImageWidth;
        
        public static void Open(CustomGradleDialogContents contents, Texture2D logo = default, string dialogTitle = default)
        {
            OpenAndroidPlayerSettings();
            var window = CreateInstance<CustomGradleDialog>();
            window.titleContent = new GUIContent(dialogTitle ?? "Android Settings");
            window.minSize = new Vector2(480, 400);
            window.maxSize = new Vector2(480, 870);
            window._contents = contents;
            window._logo = logo;
            var editorBounds = EditorGUIUtility.GetMainWindowPosition();
            var initialHeight = Mathf.Clamp(editorBounds.height - 60, 400, 870);
            window.position = new Rect(editorBounds.center.x - 240, editorBounds.center.y - initialHeight / 2, 480, initialHeight);
            window.ShowUtility();
        }

        static void OpenAndroidPlayerSettings()
        {
            SettingsService.OpenProjectSettings("Project/Player");
            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
        }

        void OnDestroy()
        {
            _logo = default;
            _contents?.OnClose?.Invoke();
            _contents?.Dispose();
        }
        
        void OnGUI()
        {
            if (_contents == default)
            {
                Close();
                return;
            }
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            if(_logo != default)
            {
                var availableWidth = Mathf.Max(1, position.width - 25);
                if (_initialImageWidth <= 0)
                {
                    var messageHeight = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(_contents.Message), position.width - 40);
                    var availableHeight = Mathf.Max(1, position.height - messageHeight - EditorGUIUtility.singleLineHeight - 60);
                    // Fit on first open; keep this scale when the user resizes so scrolling remains available.
                    _initialImageWidth = Mathf.Min(availableWidth, availableHeight * _logo.width / _logo.height);
                }
                var imageWidth = Mathf.Min(_initialImageWidth, availableWidth);
                var imageHeight = imageWidth * _logo.height / _logo.width;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                var imageRect = GUILayoutUtility.GetRect(imageWidth, imageHeight, GUILayout.ExpandWidth(false));
                GUI.DrawTexture(imageRect, _logo, ScaleMode.ScaleToFit);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            
            {
                var style = new GUIStyle()
                {
                    padding = new RectOffset(10, 10, 10, 0)
                };
            
                GUILayout.BeginVertical(style);
                GUILayout.Label(_contents.Message, EditorStyles.wordWrappedLabel);
                GUILayout.Space(10);
                if (GUILayout.Button("Open Project Settings (Android)"))
                {
                    OpenAndroidPlayerSettings();
                    EditorApplication.delayCall += Focus;
                }
                GUILayout.EndVertical();
            }
            
            EditorGUILayout.EndScrollView();
        }
    }
}
