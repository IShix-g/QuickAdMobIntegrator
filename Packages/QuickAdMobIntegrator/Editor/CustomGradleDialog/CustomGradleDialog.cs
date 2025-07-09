
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
            window.minSize = new Vector2(480, 800);
            window.maxSize = new Vector2(480, 800);
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
            if(_logo != default)
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    padding = new RectOffset(5, 5, 5, 0),
                    alignment = TextAnchor.MiddleCenter,
                };
                GUILayout.Label(_logo, style, GUILayout.ExpandWidth(true), GUILayout.Height(700));
            }

            {
                var style = new GUIStyle()
                {
                    padding = new RectOffset(10, 10, 10, 0)
                };
            
                GUILayout.BeginVertical(style);
                GUILayout.Label(_contents.Message, EditorStyles.wordWrappedLabel);
                GUILayout.EndVertical();
            }
        }
    }
}