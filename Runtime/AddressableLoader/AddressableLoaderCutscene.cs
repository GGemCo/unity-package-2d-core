using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    /// <summary>
    /// Vfx 프리팹 로드
    /// </summary>
    public class AddressableLoaderCutscene : MonoBehaviour
    {
        public static AddressableLoaderCutscene Instance { get; private set; }
        private readonly Dictionary<string, CutsceneData> _preLoadData = new Dictionary<string, CutsceneData>();
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
        public async Task LoadAsync()
        {
            try
            {
                _preLoadData.Clear();
                var locationHandle = Addressables.LoadResourceLocationsAsync(ConfigAddressableLabel.Cutscene);
                await locationHandle.Task;

                if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    GcLogger.LogError($"{ConfigAddressableLabel.Cutscene} 레이블을 가진 리소스를 찾을 수 없습니다.");
                    return;
                }

                int totalCount = locationHandle.Result.Count;
                int loadedCount = 0;

                foreach (var location in locationHandle.Result)
                {
                    string address = location.PrimaryKey;
                    var loadHandle = Addressables.LoadAssetAsync<TextAsset>(address);

                    while (!loadHandle.IsDone)
                    {
                        _prefabLoadProgress = (loadedCount + loadHandle.PercentComplete) / totalCount;
                        await Task.Yield();
                    }
                    _activeHandles.Add(loadHandle);

                    TextAsset prefab = await loadHandle.Task;
                    if (prefab == null) continue;
                    try
                    {
                        var currentCutscene = JsonConvert.DeserializeObject<CutsceneData>(prefab.text);
                        _preLoadData[address] = currentCutscene;
                        loadedCount++;
                    }
                    catch (Exception e)
                    {
                        GcLogger.LogError(e.Message);
                    }
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

        public CutsceneData GetCutsceneDataByKey(string key)
        {
            if (_preLoadData.TryGetValue(key, out var prefab))
            {
                return prefab;
            }

            GcLogger.LogError($"Addressables에서 {key} 프리팹을 찾을 수 없습니다.");
            return null;
        }

        public float GetLoadProgress() => _prefabLoadProgress;
    }
}
