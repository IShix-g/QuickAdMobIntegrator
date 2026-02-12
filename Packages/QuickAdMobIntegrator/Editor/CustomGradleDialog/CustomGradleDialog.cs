
using UnityEditor;
using UnityEngine;

namespace QuickAdMobIntegrator.Editor
{
    public sealed class CustomGradleDialog : EditorWindow
    {
        CustomGradleDialogContents _contents;
        Vector2 _scrollPos;
        Texture2D _logo;
        
        public static void Open(CustomGradleDialogContents contents, Texture2D logo = default, string dialogTitle = default)
        {
            var window = GetWindow<CustomGradleDialog>(dialogTitle);
            window.minSize = new Vector2(480, 400);
            window.maxSize = new Vector2(480, 870);
            window._contents = contents;
            window._logo = logo;
            window.ShowUtility();
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
                var style = new GUIStyle(GUI.skin.label)
                {
                    padding = new RectOffset(5, 5, 5, 0),
                    alignment = TextAnchor.MiddleCenter,
                };
                GUILayout.Label(_logo, style, GUILayout.Width(470), GUILayout.Height(700));
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
                    SettingsService.OpenProjectSettings("Project/Player");
                    EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
                    EditorApplication.delayCall += Focus;
                }
                GUILayout.EndVertical();
            }
            
            EditorGUILayout.EndScrollView();
        }
    }
}