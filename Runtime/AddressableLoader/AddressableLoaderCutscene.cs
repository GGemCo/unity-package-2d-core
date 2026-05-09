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
    /// 컷신 JSON을 Addressables에서 로드하고 캐싱합니다.
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

        /// <summary>
        /// 컷신 JSON 전체를 선로드합니다.
        /// </summary>
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

                int totalCount = Mathf.Max(1, locationHandle.Result.Count);
                int loadedCount = 0;

                foreach (var location in locationHandle.Result)
                {
                    string address = location.PrimaryKey;
                    TryLoadCutsceneDataSync(address, out _);
                    loadedCount++;
                    _prefabLoadProgress = loadedCount / (float)totalCount;
                }

                Addressables.Release(locationHandle);
                _prefabLoadProgress = 1f;
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"프리팹 로딩 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 키에 해당하는 컷신 데이터를 반환합니다.
        /// 선로드되지 않은 경우에는 최초 1회 지연 로드를 수행합니다.
        /// </summary>
        /// <param name="key">컷신 JSON Addressables 키입니다.</param>
        /// <returns>컷신 데이터입니다. 없으면 null입니다.</returns>
        public CutsceneData GetCutsceneDataByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            if (_preLoadData.TryGetValue(key, out var prefab) && prefab != null)
            {
                return prefab;
            }

            if (TryLoadCutsceneDataSync(key, out var loadedData))
            {
                return loadedData;
            }

            GcLogger.LogError($"Addressables에서 {key} 프리팹을 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 시작 로딩에서 제외된 컷신 JSON을 최초 접근 시 동기적으로 로드합니다.
        /// </summary>
        /// <param name="key">컷신 JSON Addressables 키입니다.</param>
        /// <param name="cutsceneData">로드된 컷신 데이터입니다.</param>
        /// <returns>로드 성공 여부입니다.</returns>
        private bool TryLoadCutsceneDataSync(string key, out CutsceneData cutsceneData)
        {
            cutsceneData = null;

            AsyncOperationHandle<TextAsset> loadHandle = Addressables.LoadAssetAsync<TextAsset>(key);
            _activeHandles.Add(loadHandle);
            TextAsset textAsset = loadHandle.WaitForCompletion();

            if (loadHandle.Status != AsyncOperationStatus.Succeeded || textAsset == null)
            {
                return false;
            }

            try
            {
                cutsceneData = JsonConvert.DeserializeObject<CutsceneData>(textAsset.text);
                if (cutsceneData == null)
                {
                    return false;
                }

                _preLoadData[key] = cutsceneData;
                return true;
            }
            catch (Exception e)
            {
                GcLogger.LogError(e.Message);
                return false;
            }
        }

        public float GetLoadProgress() => _prefabLoadProgress;
    }
}
