using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 이미지 로드
    /// </summary>
    public class AddressableLoaderItem : MonoBehaviour
    {
        public static AddressableLoaderItem Instance { get; private set; }
        private readonly Dictionary<string, SpriteAtlas> _dicImageDrop = new Dictionary<string, SpriteAtlas>();
        private readonly Dictionary<string, SpriteAtlas> _dicImageIcon = new Dictionary<string, SpriteAtlas>();
        private readonly Dictionary<string, SpriteAtlas> _dicImageEquip = new Dictionary<string, SpriteAtlas>();
        private readonly HashSet<AsyncOperationHandle> _activeHandles = new HashSet<AsyncOperationHandle>();
        private readonly HashSet<string> _loadingAtlasKeys = new HashSet<string>();
        private const string WarmupGroupId = "core.item";
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
            _dicImageDrop.Clear();
            _dicImageIcon.Clear();
            _dicImageEquip.Clear();
            _loadingAtlasKeys.Clear();
        }

        /// <summary>
        /// 아이템 이미지 아틀라스 라벨의 Addressables 종속성만 미리 다운로드합니다.
        /// </summary>
        /// <param name="loadObjectsInBackground">워밍 완료 후 실제 아틀라스 캐시를 백그라운드에서 구성할지 여부입니다.</param>
        public async Task PrepareDependenciesAsync(bool loadObjectsInBackground = true)
        {
            _prefabLoadProgress = 0f;

            var labels = new List<string>
            {
                ConfigAddressableLabel.ImageItemIcon,
                ConfigAddressableLabel.ImageItemDrop,
                ConfigAddressableLabel.ImageItemEquip
            };

            AddressableDependencyWarmupService warmupService = AddressableDependencyWarmupService.GetOrCreate();
            Task<bool> task = warmupService.WarmupManyAsync(labels, WarmupGroupId);

            while (!task.IsCompleted)
            {
                _prefabLoadProgress = warmupService.GetGroupProgress(WarmupGroupId);
                await Task.Yield();
            }

            _prefabLoadProgress = 1f;

            if (loadObjectsInBackground && task.Status == TaskStatus.RanToCompletion && task.Result)
            {
                // 기존 UI가 동기 Sprite 조회를 사용하므로, 캐시 구성은 로딩 화면을 막지 않는 백그라운드 작업으로 이어갑니다.
                _ = LoadPrefabsAsync();
            }
        }

        /// <summary>
        /// 특정 아이템 아틀라스를 필요 시점에 비동기로 로드합니다.
        /// </summary>
        /// <param name="key">로드할 SpriteAtlas Addressables 키입니다.</param>
        /// <param name="target">로드 결과를 저장할 캐시 딕셔너리입니다.</param>
        /// <returns>로드된 SpriteAtlas입니다. 실패 시 null을 반환합니다.</returns>
        private async Task<SpriteAtlas> LoadAtlasAsync(string key, Dictionary<string, SpriteAtlas> target)
        {
            if (string.IsNullOrWhiteSpace(key) || target == null)
                return null;

            if (target.TryGetValue(key, out SpriteAtlas cached))
                return cached;

            if (_loadingAtlasKeys.Contains(key))
            {
                while (_loadingAtlasKeys.Contains(key))
                    await Task.Yield();

                return target.TryGetValue(key, out cached) ? cached : null;
            }

            _loadingAtlasKeys.Add(key);
            AsyncOperationHandle<SpriteAtlas> loadHandle = Addressables.LoadAssetAsync<SpriteAtlas>(key);
            _activeHandles.Add(loadHandle);

            try
            {
                SpriteAtlas atlas = await loadHandle.Task;
                if (loadHandle.Status == AsyncOperationStatus.Succeeded && atlas != null)
                {
                    target[key] = atlas;
                    return atlas;
                }

                GcLogger.LogWarning($"[AddressableLoaderItem] 아이템 아틀라스 로드에 실패했습니다. key={key}");
                return null;
            }
            finally
            {
                _loadingAtlasKeys.Remove(key);
            }
        }

        /// <summary>
        /// 특정 아이템 아틀라스 로드를 백그라운드로 요청합니다.
        /// </summary>
        /// <param name="key">로드할 SpriteAtlas Addressables 키입니다.</param>
        /// <param name="target">로드 결과를 저장할 캐시 딕셔너리입니다.</param>
        private void RequestAtlasLoad(string key, Dictionary<string, SpriteAtlas> target)
        {
            if (string.IsNullOrWhiteSpace(key) || target == null || target.ContainsKey(key) || _loadingAtlasKeys.Contains(key))
                return;

            _ = LoadAtlasAsync(key, target);
        }

        public async Task LoadPrefabsAsync()
        {
            if (_isLoadingPrefabs)
                return;

            try
            {
                _isLoadingPrefabs = true;
                // 아이콘 이미지
                _dicImageIcon.Clear();
                var locationHandle = Addressables.LoadResourceLocationsAsync(ConfigAddressableLabel.ImageItemIcon);
                await locationHandle.Task;

                if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    GcLogger.LogError($"{ConfigAddressableLabel.ImageItemIcon} 레이블을 가진 리소스를 찾을 수 없습니다.");
                    return;
                }

                int totalCount = locationHandle.Result.Count;
                int loadedCount = 0;

                foreach (var location in locationHandle.Result)
                {
                    string address = location.PrimaryKey;
                    var loadHandle = Addressables.LoadAssetAsync<SpriteAtlas>(address);

                    while (!loadHandle.IsDone)
                    {
                        _prefabLoadProgress = (loadedCount + loadHandle.PercentComplete) / totalCount;
                        await Task.Yield();
                    }
                    _activeHandles.Add(loadHandle);

                    SpriteAtlas prefab = await loadHandle.Task;
                    if (!prefab) continue;
                    _dicImageIcon[address] = prefab;
                    loadedCount++;
                }
                _activeHandles.Add(locationHandle);
                
                // 드랍 아이템 이미지
                {
                    _dicImageDrop.Clear();
                    locationHandle = Addressables.LoadResourceLocationsAsync(ConfigAddressableLabel.ImageItemDrop);
                    await locationHandle.Task;

                    if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                    {
                        GcLogger.LogError($"{ConfigAddressableLabel.ImageItemDrop} 레이블을 가진 리소스를 찾을 수 없습니다.");
                        return;
                    }

                    totalCount = locationHandle.Result.Count;
                    loadedCount = 0;

                    foreach (var location in locationHandle.Result)
                    {
                        string address = location.PrimaryKey;
                        var loadHandle = Addressables.LoadAssetAsync<SpriteAtlas>(address);

                        while (!loadHandle.IsDone)
                        {
                            _prefabLoadProgress = (loadedCount + loadHandle.PercentComplete) / totalCount;
                            await Task.Yield();
                        }
                        _activeHandles.Add(loadHandle);

                        SpriteAtlas prefab = await loadHandle.Task;
                        if (!prefab) continue;
                        _dicImageDrop[address] = prefab;
                        loadedCount++;
                    }
                    _activeHandles.Add(locationHandle);
                }
                
                // 장착 이미지 
                {
                    _dicImageEquip.Clear();
                    locationHandle = Addressables.LoadResourceLocationsAsync(ConfigAddressableLabel.ImageItemEquip);
                    await locationHandle.Task;

                    if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                    {
                        GcLogger.LogError($"{ConfigAddressableLabel.ImageItemEquip} 레이블을 가진 리소스를 찾을 수 없습니다.");
                        return;
                    }

                    totalCount = locationHandle.Result.Count;
                    loadedCount = 0;

                    foreach (var location in locationHandle.Result)
                    {
                        string address = location.PrimaryKey;
                        var loadHandle = Addressables.LoadAssetAsync<SpriteAtlas>(address);

                        while (!loadHandle.IsDone)
                        {
                            _prefabLoadProgress = (loadedCount + loadHandle.PercentComplete) / totalCount;
                            await Task.Yield();
                        }
                        _activeHandles.Add(loadHandle);

                        SpriteAtlas prefab = await loadHandle.Task;
                        if (!prefab) continue;
                        _dicImageEquip[address] = prefab;
                        loadedCount++;
                    }
                    _activeHandles.Add(locationHandle);
                }

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

        public Sprite GetImageIconItemByName(string prefabName)
        {
            if (_dicImageIcon.TryGetValue(ConfigAddressableLabel.ImageItemIcon, out var prefab))
            {
                return prefab.GetSprite(prefabName);
            }

            RequestAtlasLoad(ConfigAddressableLabel.ImageItemIcon, _dicImageIcon);
            GcLogger.LogWarning($"Addressables에서 아이템 아이콘 아틀라스를 찾을 수 없어 비동기 로드를 요청했습니다. sprite={prefabName}");
            return null;
        }
        public Sprite GetImageDropByName(string prefabName)
        {
            if (_dicImageDrop.TryGetValue(ConfigAddressableLabel.ImageItemDrop, out var prefab))
            {
                return prefab.GetSprite(prefabName);
            }

            RequestAtlasLoad(ConfigAddressableLabel.ImageItemDrop, _dicImageDrop);
            GcLogger.LogWarning($"Addressables에서 아이템 드랍 아틀라스를 찾을 수 없어 비동기 로드를 요청했습니다. sprite={prefabName}");
            return null;
        }

        public Sprite GetImageEquipByName(string prefabName)
        {
            if (_dicImageEquip.TryGetValue(ConfigAddressableLabel.ImageItemEquip, out var prefab))
            {
                return prefab.GetSprite(prefabName);
            }

            RequestAtlasLoad(ConfigAddressableLabel.ImageItemEquip, _dicImageEquip);
            GcLogger.LogWarning($"Addressables에서 아이템 장착 아틀라스를 찾을 수 없어 비동기 로드를 요청했습니다. sprite={prefabName}");
            return null;
        }
        public float GetPrefabLoadProgress() => _prefabLoadProgress;

    }
}
