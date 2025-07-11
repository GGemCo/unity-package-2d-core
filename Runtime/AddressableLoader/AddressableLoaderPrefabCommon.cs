﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    /// <summary>
    /// 게임에서 사용되는 공용 프리팹 로드
    /// Label 이 ConfigAddressables.LabelPreLoadGamePrefabs 으로 설정된 프리팹
    /// </summary>
    public class AddressableLoaderPrefabCommon : MonoBehaviour
    {
        public static AddressableLoaderPrefabCommon Instance { get; private set; }
        private readonly Dictionary<string, GameObject> _preLoadGamePrefabs = new Dictionary<string, GameObject>();
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
        /// <summary>
        /// 게임에서 사용할 프리팹을 Addressables에서 로드
        /// </summary>
        public async Task LoadAllPreLoadGamePrefabsAsync()
        {
            try
            {
                _preLoadGamePrefabs.Clear();
                var locationHandle = Addressables.LoadResourceLocationsAsync(ConfigAddressableLabel.PreLoadGamePrefabs);
                await locationHandle.Task;

                if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    GcLogger.LogError($"{ConfigAddressableLabel.PreLoadGamePrefabs} 레이블을 가진 리소스를 찾을 수 없습니다.");
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
        }

        public GameObject GetPreLoadGamePrefabByName(string prefabName)
        {
            if (_preLoadGamePrefabs.TryGetValue(prefabName, out var prefab))
            {
                return prefab;
            }

            GcLogger.LogError($"Addressables에서 {prefabName} 프리팹을 찾을 수 없습니다.");
            return null;
        }

        public float GetPrefabLoadProgress() => _prefabLoadProgress;
    }
}
