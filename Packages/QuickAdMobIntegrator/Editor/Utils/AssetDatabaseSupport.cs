
using System.IO;
using UnityEngine;
using UnityEditor;

namespace QuickAdMobIntegrator.Editor
{
    internal static class AssetDatabaseSupport
    {
        public static void CreateDirectories(string path)
        {
            if (!path.StartsWith("Assets"))
            {
                Debug.LogError("The path must start with 'Assets/' ");
                return;
            }

            path = Path.HasExtension(path) ? Path.GetDirectoryName(path) : path;
            path = path?.Replace("\\", "/");

            if (string.IsNullOrEmpty(path)
                || AssetDatabase.IsValidFolder(path))
            {
                return;
            }
    
            var folders = path.Split('/');
            var parentFolder = folders[0];
    
            for (var i = 1; i < folders.Length; i++)
            {
                var newFolder = parentFolder + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newFolder))
                {
                    AssetDatabase.CreateFolder(parentFolder, folders[i]);
                }
                parentFolder = newFolder;
            }
        }
    }
}