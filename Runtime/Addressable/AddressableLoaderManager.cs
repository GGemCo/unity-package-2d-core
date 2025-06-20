using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo.Scripts
{
    public static class AddressableLoaderManager
    {
        // 로드한 리소스를 추적하기 위한 딕셔너리
        private static Dictionary<object, AsyncOperationHandle> loadedResources = new Dictionary<object, AsyncOperationHandle>();

        /// <summary>
        /// key를 통해 단일 리소스를 비동기로 로드합니다.
        /// </summary>
        public static async Task<T> LoadByKeyAsync<T>(string key) where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var result = handle.Result;
                loadedResources[result] = handle;
                return result;
            }

            Debug.LogError($"[AddressableLoaderManager] Failed to load asset by key: {key}");
            return null;
        }

        /// <summary>
        /// label을 통해 리소스 목록을 비동기로 로드합니다.
        /// </summary>
        public static async Task<List<T>> LoadByLabelAsync<T>(string label) where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetsAsync<T>(label, null);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var results = handle.Result;
                foreach (var obj in results)
                {
                    loadedResources[obj] = handle;
                }
                return new List<T>(results);
            }

            Debug.LogError($"[AddressableLoaderManager] Failed to load assets by label: {label}");
            return null;
        }

        /// <summary>
        /// 개별 리소스를 해제합니다.
        /// </summary>
        public static void Release(object obj)
        {
            if (obj == null) return;

            if (loadedResources.TryGetValue(obj, out var handle))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                loadedResources.Remove(obj);
            }
            else
            {
                Debug.LogWarning($"[AddressableLoaderManager] Tried to release unknown object: {obj}");
            }
        }

        /// <summary>
        /// 모든 로드된 리소스를 해제합니다.
        /// </summary>
        public static void ReleaseAll()
        {
            foreach (var handle in loadedResources.Values)
            {
                Addressables.Release(handle);
            }
            loadedResources.Clear();
        }
    }
}
