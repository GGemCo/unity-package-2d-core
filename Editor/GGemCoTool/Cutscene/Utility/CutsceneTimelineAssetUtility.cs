using System.IO;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal static class CutsceneTimelineAssetUtility
    {
        public static void EnsureFolderExistsForAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            var normalizedPath = assetPath.Replace("\\", "/");
            var directoryPath = Path.GetDirectoryName(normalizedPath);
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return;
            }

            var segments = directoryPath.Replace("\\", "/").Split('/');
            if (segments.Length == 0 || segments[0] != "Assets")
            {
                return;
            }

            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = segments[i];
                var combined = $"{current}/{next}";
                if (!AssetDatabase.IsValidFolder(combined))
                {
                    AssetDatabase.CreateFolder(current, next);
                }

                current = combined;
            }
        }

        public static void DeleteAssetIfExists(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }
    }
}
