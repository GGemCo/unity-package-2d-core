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
    /// 이펙트 프리팹 로드
    /// </summary>
    public class AddressableLoaderItem : MonoBehaviour
    {
        public static AddressableLoaderItem Instance { get; private set; }
        private readonly Dictionary<string, SpriteAtlas> _dicImageDrop = new Dictionary<string, SpriteAtlas>();
        private readonly Dictionary<string, SpriteAtlas> _dicImageIcon = new Dictionary<string, SpriteAtlas>();
        private readonly HashSet<AsyncOperationHandle> _activeHandles = new HashSet<AsyncOperationHandle>();
        private float _prefabLoadProgress;

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
        }
        public async Task LoadPrefabsAsync()
        {
            try
            {
                _dicImageDrop.Clear();
                _dicImageIcon.Clear();
                var locationHandle = Addressables.LoadResourceLocationsAsync(ConfigAddressableLabel.ImageItemIcon);
                await locationHandle.Task;

                if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    GcLogger.LogError($"{ConfigAddressableLabel.Effect} 레이블을 가진 리소스를 찾을 수 없습니다.");
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
                
                {
                    
                    _dicImageDrop.Clear();
                    locationHandle = Addressables.LoadResourceLocationsAsync(ConfigAddressableLabel.ImageItemDrop);
                    await locationHandle.Task;

                    if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                    {
                        GcLogger.LogError($"{ConfigAddressableLabel.Effect} 레이블을 가진 리소스를 찾을 수 없습니다.");
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

                _prefabLoadProgress = 1f; // 100%
                // GcLogger.Log($"총 {loadedCount}/{totalCount}개의 프리팹을 성공적으로 로드했습니다.");
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"프리팹 로딩 중 오류 발생: {ex.Message}");
            }
        }

        public Sprite GetImageIconByName(string prefabName)
        {
            if (_dicImageIcon.TryGetValue(ConfigAddressableLabel.ImageItemIcon, out var prefab))
            {
                return prefab.GetSprite(prefabName);
            }

            GcLogger.LogError($"Addressables에서 {prefabName} 프리팹을 찾을 수 없습니다.");
            return null;
        }
        public Sprite GetImageDropByName(string prefabName)
        {
            if (_dicImageDrop.TryGetValue(ConfigAddressableLabel.ImageItemDrop, out var prefab))
            {
                return prefab.GetSprite(prefabName);
            }

            GcLogger.LogError($"Addressables에서 {prefabName} 프리팹을 찾을 수 없습니다.");
            return null;
        }

        public float GetPrefabLoadProgress() => _prefabLoadProgress;
    }
}
