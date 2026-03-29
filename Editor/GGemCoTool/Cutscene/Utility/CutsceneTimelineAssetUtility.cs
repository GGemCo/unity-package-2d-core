using System.IO;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Timeline 관련 에셋 경로의 폴더 보장 및 기존 에셋 삭제를 처리하는 유틸리티입니다.
    /// </summary>
    internal static class CutsceneTimelineAssetUtility
    {
        /// <summary>
        /// 지정한 에셋 경로에 필요한 Assets 하위 폴더가 모두 존재하도록 보장합니다.
        /// </summary>
        /// <param name="assetPath">생성 대상 에셋 경로입니다.</param>
        public static void EnsureFolderExistsForAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            // Unity 에셋 경로 형식에 맞게 구분자를 정규화합니다.
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

            // Assets부터 하위 폴더를 순차적으로 확인하며 누락된 폴더를 생성합니다.
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

        /// <summary>
        /// 지정한 경로에 에셋이 이미 존재하면 삭제합니다.
        /// </summary>
        /// <param name="assetPath">삭제를 확인할 에셋 경로입니다.</param>
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