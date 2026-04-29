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
    /// 월드맵 JSON을 Addressables에서 로드하고 런타임 월드맵 정의로 캐싱합니다.
    /// </summary>
    public class AddressableLoaderWorldMap : MonoBehaviour
    {
        public static AddressableLoaderWorldMap Instance { get; private set; }
        private readonly Dictionary<string, WorldMapDefinition> _dicWorldMap = new Dictionary<string, WorldMapDefinition>();
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
        /// 기본 월드맵 JSON(world_map_main.json)을 로드하고 WorldMapDefinition으로 변환합니다.
        /// </summary>
        /// <returns>비동기 로드 작업입니다.</returns>
        public async Task LoadAsync()
        {
            try
            {
                _dicWorldMap.Clear();

                string address = ConfigAddressableWorldMap.GetDefaultKey();
                var loadHandle = Addressables.LoadAssetAsync<TextAsset>(address);

                while (!loadHandle.IsDone)
                {
                    _prefabLoadProgress = loadHandle.PercentComplete;
                    await Task.Yield();
                }
                _activeHandles.Add(loadHandle);

                if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    GcLogger.LogError($"월드맵 JSON 로드에 실패했습니다. key: {address}");
                    return;
                }

                TextAsset textAsset = await loadHandle.Task;
                if (textAsset == null)
                {
                    GcLogger.LogError($"월드맵 JSON TextAsset을 로드하지 못했습니다. key: {address}");
                    return;
                }

                string error;
                WorldMapDefinition definition = FromJson(textAsset.text, out error);
                if (definition == null)
                {
                    GcLogger.LogError(error);
                    return;
                }

                _dicWorldMap[address] = definition;

                _prefabLoadProgress = 1f; // 100%
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"월드맵 로딩 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// JSON 문자열을 월드맵 정의 객체로 변환합니다.
        /// </summary>
        /// <param name="json">월드맵 JSON 문자열입니다.</param>
        /// <param name="error">실패 시 반환할 오류 메시지입니다.</param>
        /// <returns>변환된 월드맵 정의입니다. 실패 시 null입니다.</returns>
        public static WorldMapDefinition FromJson(string json, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "월드맵 JSON 문자열이 비어 있습니다.";
                return null;
            }

            try
            {
                WorldMapGraphJson graphJson = JsonConvert.DeserializeObject<WorldMapGraphJson>(json);
                if (graphJson == null)
                {
                    error = "월드맵 JSON 파싱 결과가 비어 있습니다.";
                    return null;
                }

                return WorldMapDefinition.FromJson(graphJson);
            }
            catch (Exception e)
            {
                error = "월드맵 JSON 파싱에 실패했습니다. " + e.Message;
                return null;
            }
        }

        /// <summary>
        /// 기본 월드맵 정의를 반환합니다.
        /// </summary>
        /// <returns>기본 월드맵 정의입니다. 로드되지 않았으면 null입니다.</returns>
        public WorldMapDefinition GetDefaultWorldMap()
        {
            return GetWorldMapByKey(ConfigAddressableWorldMap.GetDefaultKey());
        }

        /// <summary>
        /// 기본 월드맵 정의가 캐싱되어 있는지 확인하고 반환합니다.
        /// </summary>
        /// <param name="definition">캐싱된 기본 월드맵 정의입니다.</param>
        /// <returns>기본 월드맵 정의가 있으면 true입니다.</returns>
        public bool TryGetDefaultWorldMap(out WorldMapDefinition definition)
        {
            return TryGetWorldMapByKey(ConfigAddressableWorldMap.GetDefaultKey(), out definition);
        }

        /// <summary>
        /// Addressables 키로 캐싱된 월드맵 정의가 있는지 확인하고 반환합니다.
        /// </summary>
        /// <param name="key">조회할 월드맵 JSON Addressables 키입니다.</param>
        /// <param name="definition">캐싱된 월드맵 정의입니다.</param>
        /// <returns>월드맵 정의가 있으면 true입니다.</returns>
        public bool TryGetWorldMapByKey(string key, out WorldMapDefinition definition)
        {
            return _dicWorldMap.TryGetValue(key, out definition) && definition != null;
        }

        /// <summary>
        /// Addressables 키로 캐싱된 월드맵 정의를 조회합니다.
        /// </summary>
        /// <param name="key">조회할 월드맵 JSON Addressables 키입니다.</param>
        /// <returns>월드맵 정의입니다. 없으면 null입니다.</returns>
        public WorldMapDefinition GetWorldMapByKey(string key)
        {
            if (_dicWorldMap.TryGetValue(key, out var worldMapDefinition))
            {
                return worldMapDefinition;
            }

            GcLogger.LogError($"Addressables에서 {key} 월드맵을 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 월드맵 JSON 로드 진행률을 반환합니다.
        /// </summary>
        /// <returns>0~1 범위의 로드 진행률입니다.</returns>
        public float GetLoadProgress() => _prefabLoadProgress;
    }
}
