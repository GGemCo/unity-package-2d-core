﻿using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    public static class AddressableLoaderController
    {
        private static readonly Dictionary<object, AsyncOperationHandle> LoadedResources = new Dictionary<object, AsyncOperationHandle>();
        private static readonly HashSet<AsyncOperationHandle> ActiveHandles = new HashSet<AsyncOperationHandle>();

        /// <summary>
        /// key를 통해 단일 리소스를 비동기로 로드합니다.
        /// </summary>
        public static async Task<T> LoadByKeyAsync<T>(string key) where T : Object
        {
            // 이미 로드된 리소스가 있는 경우 반환
            foreach (var pair in LoadedResources)
            {
                if (pair.Value.IsValid() && pair.Value.DebugName == key && pair.Key is T loaded)
                {
                    return loaded;
                }
            }
            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var result = handle.Result;
                LoadedResources[result] = handle;
                ActiveHandles.Add(handle);
                return result;
            }

            Debug.LogError($"[AddressableLoaderManager] Failed to load asset by key: {key}");
            return null;
        }

        /// <summary>
        /// label을 통해 여러 리소스를 비동기로 로드합니다.
        /// </summary>
        public static async Task<Dictionary<string, T>> LoadByLabelAsync<T>(string label) where T : Object
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
                
                // 이미 로드된 오브젝트가 있는 경우 캐시에서 꺼냄
                bool alreadyLoaded = false;
                foreach (var pair in LoadedResources)
                {
                    if (pair.Value.IsValid() && pair.Value.DebugName == key && pair.Key is T cachedObj)
                    {
                        result[key] = cachedObj;
                        alreadyLoaded = true;
                        break;
                    }
                }
                if (alreadyLoaded) continue;
                
                var handle = Addressables.LoadAssetAsync<T>(key);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    T obj = handle.Result;
                    result[key] = obj;

                    LoadedResources[obj] = handle;
                    ActiveHandles.Add(handle);
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
                LoadedResources[instance] = handle;
                ActiveHandles.Add(handle);
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

            if (LoadedResources.TryGetValue(obj, out var handle))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                LoadedResources.Remove(obj);
                ActiveHandles.Remove(handle);
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
            foreach (var handle in ActiveHandles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            ActiveHandles.Clear();
            LoadedResources.Clear();
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
