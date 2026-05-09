using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    /// <summary>
    /// Vfx 프리팹 로드
    /// </summary>
    public class AddressableLoaderPrefabVfx : MonoBehaviour
    {
        public static AddressableLoaderPrefabVfx Instance { get; private set; }
        private readonly Dictionary<string, GameObject> _preLoadGamePrefabs = new Dictionary<string, GameObject>();
        private readonly HashSet<AsyncOperationHandle> _activeHandles = new HashSet<AsyncOperationHandle>();
        private readonly HashSet<string> _loadingPrefabKeys = new HashSet<string>();
        private float _prefabLoadProgress;
        private bool _isLoadingPrefabs;

        private void Awake()
        {
            _prefabLoadProgress = 0f;
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            ReleaseAll();
        }

        /// <summary>
        /// 모든 로드된 리소스를 해제합니다.
        /// </summary>
        private void ReleaseAll()
        {
            AddressableLoaderController.ReleaseByHandles(_activeHandles);
            _preLoadGamePrefabs.Clear();
            _loadingPrefabKeys.Clear();
        }

        /// <summary>
        /// VFX 라벨의 Addressables 종속성만 미리 다운로드합니다.
        /// </summary>
        /// <param name="loadObjectsInBackground">워밍 완료 후 실제 프리팹 캐시를 백그라운드에서 구성할지 여부입니다.</param>
        public async Task PrepareDependenciesAsync(bool loadObjectsInBackground = true)
        {
            _prefabLoadProgress = 0f;

            AddressableDependencyWarmupService warmupService = AddressableDependencyWarmupService.GetOrCreate();
            Task<bool> task = warmupService.WarmupAsync(ConfigAddressableLabel.Vfx);

            while (!task.IsCompleted)
            {
                _prefabLoadProgress = warmupService.GetProgress(ConfigAddressableLabel.Vfx);
                await Task.Yield();
            }

            _prefabLoadProgress = 1f;

            if (loadObjectsInBackground && task.Status == TaskStatus.RanToCompletion && task.Result)
            {
                // 기존 동기 getter 사용부를 보호하기 위해 실제 객체 캐시는 로딩 화면 밖에서 비동기로 구성합니다.
                _ = LoadPrefabsAsync();
            }
        }

        /// <summary>
        /// 특정 VFX 프리팹을 필요 시점에 비동기로 로드합니다.
        /// </summary>
        /// <param name="key">로드할 VFX 프리팹 Addressables 키입니다.</param>
        /// <returns>로드된 프리팹입니다. 실패 시 null을 반환합니다.</returns>
        public async Task<GameObject> LoadPrefabAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (_preLoadGamePrefabs.TryGetValue(key, out GameObject cached))
                return cached;

            if (_loadingPrefabKeys.Contains(key))
            {
                while (_loadingPrefabKeys.Contains(key))
                    await Task.Yield();

                return _preLoadGamePrefabs.TryGetValue(key, out cached) ? cached : null;
            }

            _loadingPrefabKeys.Add(key);
            AsyncOperationHandle<GameObject> loadHandle = Addressables.LoadAssetAsync<GameObject>(key);
            _activeHandles.Add(loadHandle);

            try
            {
                GameObject prefab = await loadHandle.Task;
                if (loadHandle.Status == AsyncOperationStatus.Succeeded && prefab != null)
                {
                    _preLoadGamePrefabs[key] = prefab;
                    return prefab;
                }

                GcLogger.LogWarning($"[AddressableLoaderPrefabVfx] VFX 프리팹 로드에 실패했습니다. key={key}");
                return null;
            }
            finally
            {
                _loadingPrefabKeys.Remove(key);
            }
        }

        /// <summary>
        /// 특정 VFX 프리팹 로드를 백그라운드로 요청합니다.
        /// </summary>
        /// <param name="key">로드할 VFX 프리팹 Addressables 키입니다.</param>
        public void RequestPrefabLoad(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || _preLoadGamePrefabs.ContainsKey(key) || _loadingPrefabKeys.Contains(key))
                return;

            _ = LoadPrefabAsync(key);
        }

        /// <summary>
        /// 캐시된 VFX 프리팹 조회를 시도합니다.
        /// </summary>
        /// <param name="prefabName">조회할 Addressables 키입니다.</param>
        /// <param name="prefab">캐시된 프리팹입니다.</param>
        /// <returns>캐시에 존재하면 true를 반환합니다.</returns>
        public bool TryGetPrefabByName(string prefabName, out GameObject prefab)
        {
            return _preLoadGamePrefabs.TryGetValue(prefabName, out prefab);
        }

        public async Task LoadPrefabsAsync()
        {
            if (_isLoadingPrefabs)
                return;

            try
            {
                _isLoadingPrefabs = true;
                _preLoadGamePrefabs.Clear();
                var locationHandle = Addressables.LoadResourceLocationsAsync(ConfigAddressableLabel.Vfx);
                await locationHandle.Task;

                if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    GcLogger.LogError($"{ConfigAddressableLabel.Vfx} 레이블을 가진 리소스를 찾을 수 없습니다.");
                    return;
                }

                int totalCount = locationHandle.Result.Count;
                int loadedCount = 0;

                foreach (var location in locationHandle.Result)
                {
                    string address = location.PrimaryKey;
                    var loadHandle = Addressables.LoadAssetAsync<GameObject>(address);

                    while (!loadHandle.IsDone)
                    {
                        _prefabLoadProgress = (loadedCount + loadHandle.PercentComplete) / totalCount;
                        await Task.Yield();
                    }
                    _activeHandles.Add(loadHandle);

                    GameObject prefab = await loadHandle.Task;
                    if (!prefab) continue;
                    _preLoadGamePrefabs[address] = prefab;
                    loadedCount++;
                }
                _activeHandles.Add(locationHandle);

                _prefabLoadProgress = 1f; // 100%
                // GcLogger.Log($"총 {loadedCount}/{totalCount}개의 프리팹을 성공적으로 로드했습니다.");
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"프리팹 로딩 중 오류 발생: {ex.Message}");
            }
            finally
            {
                _isLoadingPrefabs = false;
            }
        }

        public GameObject GetPrefabByName(string prefabName)
        {
            if (_preLoadGamePrefabs.TryGetValue(prefabName, out var prefab))
            {
                return prefab;
            }

            RequestPrefabLoad(prefabName);
            GcLogger.LogWarning($"Addressables에서 {prefabName} 프리팹 캐시를 찾을 수 없어 비동기 로드를 요청했습니다.");
            return null;
        }

        public float GetPrefabLoadProgress() => _prefabLoadProgress;
    }
}
