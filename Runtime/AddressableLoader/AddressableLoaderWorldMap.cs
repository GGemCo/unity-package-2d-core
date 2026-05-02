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
        private readonly Dictionary<string, Sprite> _backgroundSpriteByAddress = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Sprite> _iconSpriteByAddress = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Sprite> _inactiveSpriteByAddress = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Sprite> _decorationSpriteByAddress = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, RuntimeAnimatorController> _decorationAnimatorByAddress = new Dictionary<string, RuntimeAnimatorController>();
        private readonly Dictionary<string, Sprite> _edgeSpriteByAddress = new Dictionary<string, Sprite>();
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
                _backgroundSpriteByAddress.Clear();
                _iconSpriteByAddress.Clear();
                _inactiveSpriteByAddress.Clear();
                _decorationSpriteByAddress.Clear();
                _decorationAnimatorByAddress.Clear();
                _edgeSpriteByAddress.Clear();

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

                await LoadSpritesAsync(definition);
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
        /// 월드맵 정의에 기록된 배경과 노드 아이콘 Sprite를 Addressables에서 로드해 캐싱합니다.
        /// </summary>
        /// <param name="definition">Sprite address를 보유한 월드맵 정의입니다.</param>
        /// <returns>비동기 로드 작업입니다.</returns>
        private async Task LoadSpritesAsync(WorldMapDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(definition.BackgroundAddress))
            {
                Sprite backgroundSprite = await LoadSpriteByAddressAsync(definition.BackgroundAddress);
                if (backgroundSprite != null)
                {
                    _backgroundSpriteByAddress[definition.BackgroundAddress] = backgroundSprite;
                }
            }

            if (definition.Nodes != null)
            {
                for (int i = 0; i < definition.Nodes.Count; i++)
                {
                    WorldMapNodeDefinition node = definition.Nodes[i];
                    if (node == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(node.IconAddress) && !_iconSpriteByAddress.ContainsKey(node.IconAddress))
                    {
                        Sprite iconSprite = await LoadSpriteByAddressAsync(node.IconAddress);
                        if (iconSprite != null)
                        {
                            _iconSpriteByAddress[node.IconAddress] = iconSprite;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(node.InactiveSpriteAddress) && !_inactiveSpriteByAddress.ContainsKey(node.InactiveSpriteAddress))
                    {
                        Sprite inactiveSprite = await LoadSpriteByAddressAsync(node.InactiveSpriteAddress);
                        if (inactiveSprite != null)
                        {
                            _inactiveSpriteByAddress[node.InactiveSpriteAddress] = inactiveSprite;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(node.DecorationSpriteAddress) && !_decorationSpriteByAddress.ContainsKey(node.DecorationSpriteAddress))
                    {
                        Sprite decorationSprite = await LoadSpriteByAddressAsync(node.DecorationSpriteAddress);
                        if (decorationSprite != null)
                        {
                            _decorationSpriteByAddress[node.DecorationSpriteAddress] = decorationSprite;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(node.DecorationAnimatorControllerAddress) ||
                        _decorationAnimatorByAddress.ContainsKey(node.DecorationAnimatorControllerAddress))
                    {
                        continue;
                    }

                    RuntimeAnimatorController decorationAnimator = await LoadAnimatorControllerByAddressAsync(
                        node.DecorationAnimatorControllerAddress);
                    if (decorationAnimator != null)
                    {
                        _decorationAnimatorByAddress[node.DecorationAnimatorControllerAddress] = decorationAnimator;
                    }
                }
            }

            if (definition.Edges == null)
            {
                return;
            }

            for (int i = 0; i < definition.Edges.Count; i++)
            {
                WorldMapEdgeDefinition edge = definition.Edges[i];
                if (edge == null || string.IsNullOrWhiteSpace(edge.EdgeSpriteAddress) || _edgeSpriteByAddress.ContainsKey(edge.EdgeSpriteAddress))
                {
                    continue;
                }

                Sprite edgeSprite = await LoadSpriteByAddressAsync(edge.EdgeSpriteAddress);
                if (edgeSprite != null)
                {
                    _edgeSpriteByAddress[edge.EdgeSpriteAddress] = edgeSprite;
                }
            }
        }

        /// <summary>
        /// Addressables 키로 Sprite를 로드하고 핸들을 해제 목록에 등록합니다.
        /// </summary>
        /// <param name="address">로드할 Sprite Addressables 키입니다.</param>
        /// <returns>로드된 Sprite입니다. 실패 시 null입니다.</returns>
        private async Task<Sprite> LoadSpriteByAddressAsync(string address)
        {
            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);
            _activeHandles.Add(handle);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }

            GcLogger.LogError($"월드맵 Sprite 로드에 실패했습니다. key: {address}");
            return null;
        }

        /// <summary>
        /// 월드맵 정의의 배경 Sprite가 캐싱되어 있는지 확인하고 반환합니다.
        /// </summary>
        /// <param name="definition">배경 address를 보유한 월드맵 정의입니다.</param>
        /// <param name="sprite">캐싱된 배경 Sprite입니다.</param>
        /// <returns>배경 Sprite가 있으면 true입니다.</returns>
        public bool TryGetBackgroundSprite(WorldMapDefinition definition, out Sprite sprite)
        {
            sprite = null;
            return definition != null && TryGetBackgroundSprite(definition.BackgroundAddress, out sprite);
        }

        /// <summary>
        /// Addressables 키로 캐싱된 월드맵 배경 Sprite를 조회합니다.
        /// </summary>
        /// <param name="address">배경 Sprite Addressables 키입니다.</param>
        /// <param name="sprite">캐싱된 배경 Sprite입니다.</param>
        /// <returns>배경 Sprite가 있으면 true입니다.</returns>
        public bool TryGetBackgroundSprite(string address, out Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                sprite = null;
                return false;
            }

            return _backgroundSpriteByAddress.TryGetValue(address, out sprite) && sprite != null;
        }

        /// <summary>
        /// 월드맵 노드 정의의 아이콘 Sprite가 캐싱되어 있는지 확인하고 반환합니다.
        /// </summary>
        /// <param name="node">아이콘 address를 보유한 노드 정의입니다.</param>
        /// <param name="sprite">캐싱된 아이콘 Sprite입니다.</param>
        /// <returns>아이콘 Sprite가 있으면 true입니다.</returns>
        public bool TryGetIconSprite(WorldMapNodeDefinition node, out Sprite sprite)
        {
            sprite = null;
            return node != null && TryGetIconSprite(node.IconAddress, out sprite);
        }

        /// <summary>
        /// Addressables 키로 캐싱된 월드맵 노드 아이콘 Sprite를 조회합니다.
        /// </summary>
        /// <param name="address">노드 아이콘 Sprite Addressables 키입니다.</param>
        /// <param name="sprite">캐싱된 아이콘 Sprite입니다.</param>
        /// <returns>아이콘 Sprite가 있으면 true입니다.</returns>
        public bool TryGetIconSprite(string address, out Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                sprite = null;
                return false;
            }

            return _iconSpriteByAddress.TryGetValue(address, out sprite) && sprite != null;
        }

        /// <summary>
        /// 월드맵 노드 정의에 연결된 비활성 Sprite가 캐싱되어 있는지 확인하고 반환합니다.
        /// </summary>
        /// <param name="node">비활성 Sprite address를 보유한 노드 정의입니다.</param>
        /// <param name="sprite">캐싱된 비활성 Sprite입니다.</param>
        /// <returns>비활성 Sprite가 있으면 true를 반환합니다.</returns>
        public bool TryGetInactiveSprite(WorldMapNodeDefinition node, out Sprite sprite)
        {
            sprite = null;
            return node != null && TryGetInactiveSprite(node.InactiveSpriteAddress, out sprite);
        }

        /// <summary>
        /// Addressables 키로 캐싱된 월드맵 노드 비활성 Sprite를 조회합니다.
        /// </summary>
        /// <param name="address">비활성 Sprite Addressables 키입니다.</param>
        /// <param name="sprite">캐싱된 비활성 Sprite입니다.</param>
        /// <returns>비활성 Sprite가 있으면 true를 반환합니다.</returns>
        public bool TryGetInactiveSprite(string address, out Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                sprite = null;
                return false;
            }

            return _inactiveSpriteByAddress.TryGetValue(address, out sprite) && sprite != null;
        }

        /// <summary>
        /// 월드맵 노드 정의의 데코레이션 Sprite가 캐싱되어 있는지 확인하고 반환합니다.
        /// </summary>
        /// <param name="node">데코레이션 Sprite address를 보유한 노드 정의입니다.</param>
        /// <param name="sprite">캐싱된 데코레이션 Sprite입니다.</param>
        /// <returns>데코레이션 Sprite가 있으면 true를 반환합니다.</returns>
        public bool TryGetDecorationSprite(WorldMapNodeDefinition node, out Sprite sprite)
        {
            sprite = null;
            return node != null && TryGetDecorationSprite(node.DecorationSpriteAddress, out sprite);
        }

        /// <summary>
        /// Addressables 키로 캐싱된 월드맵 노드 데코레이션 Sprite를 조회합니다.
        /// </summary>
        /// <param name="address">데코레이션 Sprite Addressables 키입니다.</param>
        /// <param name="sprite">캐싱된 데코레이션 Sprite입니다.</param>
        /// <returns>데코레이션 Sprite가 있으면 true를 반환합니다.</returns>
        public bool TryGetDecorationSprite(string address, out Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                sprite = null;
                return false;
            }

            return _decorationSpriteByAddress.TryGetValue(address, out sprite) && sprite != null;
        }

        /// <summary>
        /// 월드맵 노드 정의의 데코레이션 AnimatorController가 캐싱되어 있는지 확인하고 반환합니다.
        /// </summary>
        /// <param name="node">데코레이션 AnimatorController address를 보유한 노드 정의입니다.</param>
        /// <param name="animatorController">캐싱된 데코레이션 AnimatorController입니다.</param>
        /// <returns>데코레이션 AnimatorController가 있으면 true를 반환합니다.</returns>
        public bool TryGetDecorationAnimatorController(
            WorldMapNodeDefinition node,
            out RuntimeAnimatorController animatorController)
        {
            animatorController = null;
            return node != null && TryGetDecorationAnimatorController(node.DecorationAnimatorControllerAddress, out animatorController);
        }

        /// <summary>
        /// Addressables 키로 캐싱된 월드맵 노드 데코레이션 AnimatorController를 조회합니다.
        /// </summary>
        /// <param name="address">데코레이션 AnimatorController Addressables 키입니다.</param>
        /// <param name="animatorController">캐싱된 데코레이션 AnimatorController입니다.</param>
        /// <returns>데코레이션 AnimatorController가 있으면 true를 반환합니다.</returns>
        public bool TryGetDecorationAnimatorController(
            string address,
            out RuntimeAnimatorController animatorController)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                animatorController = null;
                return false;
            }

            return _decorationAnimatorByAddress.TryGetValue(address, out animatorController) && animatorController != null;
        }

        /// <summary>
        /// 월드맵 연결선 정의의 Sprite가 캐싱되어 있는지 확인하고 반환합니다.
        /// </summary>
        /// <param name="edge">연결선 Sprite address를 보유한 연결선 정의입니다.</param>
        /// <param name="sprite">캐싱된 연결선 Sprite입니다.</param>
        /// <returns>연결선 Sprite가 있으면 true를 반환합니다.</returns>
        public bool TryGetEdgeSprite(WorldMapEdgeDefinition edge, out Sprite sprite)
        {
            sprite = null;
            return edge != null && TryGetEdgeSprite(edge.EdgeSpriteAddress, out sprite);
        }

        /// <summary>
        /// Addressables 키로 캐싱된 월드맵 연결선 Sprite를 조회합니다.
        /// </summary>
        /// <param name="address">연결선 Sprite Addressables 키입니다.</param>
        /// <param name="sprite">캐싱된 연결선 Sprite입니다.</param>
        /// <returns>연결선 Sprite가 있으면 true를 반환합니다.</returns>
        public bool TryGetEdgeSprite(string address, out Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                sprite = null;
                return false;
            }

            return _edgeSpriteByAddress.TryGetValue(address, out sprite) && sprite != null;
        }

        /// <summary>
        /// 월드맵 JSON 로드 진행률을 반환합니다.
        /// </summary>
        /// <returns>0~1 범위의 로드 진행률입니다.</returns>
        public float GetLoadProgress() => _prefabLoadProgress;

        /// <summary>
        /// Addressables 키로 RuntimeAnimatorController를 로드하고 해제 대상 핸들에 등록합니다.
        /// </summary>
        /// <param name="address">로드할 AnimatorController Addressables 키입니다.</param>
        /// <returns>로드된 AnimatorController입니다. 실패하면 null을 반환합니다.</returns>
        private async Task<RuntimeAnimatorController> LoadAnimatorControllerByAddressAsync(string address)
        {
            AsyncOperationHandle<RuntimeAnimatorController> handle = Addressables.LoadAssetAsync<RuntimeAnimatorController>(address);
            _activeHandles.Add(handle);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }

            GcLogger.LogError($"월드맵 AnimatorController 로드에 실패했습니다. key: {address}");
            return null;
        }
    }
}
