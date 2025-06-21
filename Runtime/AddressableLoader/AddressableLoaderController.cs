using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo.Scripts
{
    public static class AddressableLoaderController
    {
        private static Dictionary<object, AsyncOperationHandle> loadedResources = new Dictionary<object, AsyncOperationHandle>();
        private static HashSet<AsyncOperationHandle> activeHandles = new HashSet<AsyncOperationHandle>();

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
                activeHandles.Add(handle);
                return result;
            }

            Debug.LogError($"[AddressableLoaderManager] Failed to load asset by key: {key}");
            return null;
        }

        /// <summary>
        /// label을 통해 여러 리소스를 비동기로 로드합니다.
        /// </summary>
        public static async Task<Dictionary<string, T>> LoadByLabelAsync<T>(string label) where T : UnityEngine.Object
        {
            // 리소스 위치를 먼저 가져옵니다.
            var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
            await locationsHandle.Task;

            if (locationsHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AddressableLoaderManager] Failed to get resource locations for label: {label}");
                return null;
            }

            var result = new Dictionary<string, T>();
            var locations = locationsHandle.Result;

            foreach (var location in locations)
            {
                string key = location.PrimaryKey;
                var handle = Addressables.LoadAssetAsync<T>(key);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    T obj = handle.Result;
                    result[key] = obj;

                    loadedResources[obj] = handle;
                    activeHandles.Add(handle);
                }
                else
                {
                    Debug.LogWarning($"[AddressableLoaderManager] Failed to load: {key}");
                }
            }

            Addressables.Release(locationsHandle);
            return result;
        }

        /// <summary>
        /// Addressables.InstantiateAsync 를 사용하여 프리팹 인스턴스를 생성합니다.
        /// 자동으로 해제 추적에 포함됩니다.
        /// </summary>
        public static async Task<GameObject> InstantiateAsync(string key, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            var handle = Addressables.InstantiateAsync(key, position, rotation, parent);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject instance = handle.Result;
                loadedResources[instance] = handle;
                activeHandles.Add(handle);
                return instance;
            }

            Debug.LogError($"[AddressableLoaderManager] Failed to instantiate prefab with key: {key}");
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
                activeHandles.Remove(handle);
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
            foreach (var handle in activeHandles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            activeHandles.Clear();
            loadedResources.Clear();
        }
        public static void ReleaseByHandles(HashSet<AsyncOperationHandle> handles)
        {
            foreach (var handle in handles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            handles.Clear();
        }
    }
}
