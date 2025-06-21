#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    /// <summary>
    /// AssetDatabase를 이용한 에셋 로딩 매니저
    /// - 에디터 전용. 런타임에서는 동작하지 않음.
    /// - Resources 폴더 외부의 에셋을 로드할 때 사용
    /// </summary>
    public static class AssetDatabaseLoaderManager
    {
        // 캐시된 에셋들
        private static readonly Dictionary<string, Object> CachedAssets = new Dictionary<string, UnityEngine.Object>();

        /// <summary>
        /// 특정 타입의 에셋을 로드합니다. (에디터 전용)
        /// </summary>
        /// <typeparam name="T">로드할 에셋 타입</typeparam>
        /// <param name="assetPath">"Assets/"부터 시작하는 에셋 경로</param>
        /// <param name="useCache">캐싱 사용 여부</param>
        /// <returns>로드된 에셋 또는 null</returns>
        public static T LoadAsset<T>(string assetPath, bool useCache = true) where T : Object
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning("[AssetDatabaseLoaderManager] 경로가 비어 있습니다.");
                return null;
            }

            if (useCache && CachedAssets.TryGetValue(assetPath, out var cached))
            {
                return cached as T;
            }

            if (typeof(T) == typeof(GameObject))
            {
                if (!assetPath.EndsWith(".prefab"))
                {
                    assetPath += ".prefab";
                }
            }
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (!asset)
            {
                Debug.LogWarning($"[AssetDatabaseLoaderManager] 에셋 로드 실패: {assetPath}");
                return null;
            }

            if (useCache)
                CachedAssets[assetPath] = asset;

            return asset;
        }

        /// <summary>
        /// 텍스트 파일을 문자열로 로드합니다.
        /// </summary>
        /// <param name="assetPath">"Assets/"부터 시작하는 경로</param>
        /// <returns>텍스트 내용 또는 null</returns>
        public static string LoadFileText(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            if (!assetPath.EndsWith(".txt"))
            {
                assetPath += ".txt";
            }
            TextAsset textAsset = LoadAsset<TextAsset>(assetPath);
            return textAsset ? textAsset.text : null;
        }
        public static string LoadFileJson(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            if (!assetPath.EndsWith(".json"))
            {
                assetPath += ".json";
            }
            TextAsset textAsset = LoadAsset<TextAsset>(assetPath);
            return textAsset ? textAsset.text : null;
        }

        /// <summary>
        /// 캐시를 초기화합니다.
        /// </summary>
        public static void ClearCache()
        {
            CachedAssets.Clear();
        }
    }
}
#endif
