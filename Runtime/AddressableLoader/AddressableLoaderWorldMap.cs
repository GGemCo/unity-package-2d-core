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
    /// 월드맵 JSON과 관련 스프라이트를 Addressables에서 로드하고 캐싱합니다.
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
        /// 기본 월드맵 JSON(world_map_main.json)을 비동기로 로드합니다.
        /// </summary>
        public async Task LoadAsync()
        {
            try
            {
                ClearCaches();
                await LoadWorldMapByKeyAsync(ConfigAddressableWorldMap.GetDefaultKey());
                _prefabLoadProgress = 1f;
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"월드맵 로딩 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// JSON 문자열을 월드맵 정의 객체로 변환합니다.
        /// </summary>
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
        public WorldMapDefinition GetDefaultWorldMap()
        {
            return GetWorldMapByKey(ConfigAddressableWorldMap.GetDefaultKey());
        }

        /// <summary>
        /// 기본 월드맵 정의가 있으면 반환합니다.
        /// 필요 시 최초 1회 지연 로드를 수행합니다.
        /// </summary>
        public bool TryGetDefaultWorldMap(out WorldMapDefinition definition)
        {
            return TryGetWorldMapByKey(ConfigAddressableWorldMap.GetDefaultKey(), out definition);
        }

        /// <summary>
        /// 지정 키의 월드맵 정의를 반환합니다.
        /// 필요 시 최초 1회 지연 로드를 수행합니다.
        /// </summary>
        public bool TryGetWorldMapByKey(string key, out WorldMapDefinition definition)
        {
            if (!_dicWorldMap.TryGetValue(key, out definition) || definition == null)
            {
                TryLoadWorldMapByKeySync(key, out definition);
            }

            return definition != null;
        }

        /// <summary>
        /// 지정 키의 월드맵 정의를 조회합니다.
        /// 필요 시 최초 1회 지연 로드를 수행합니다.
        /// </summary>
        public WorldMapDefinition GetWorldMapByKey(string key)
        {
            if (TryGetWorldMapByKey(key, out var worldMapDefinition))
            {
                return worldMapDefinition;
            }

            GcLogger.LogError($"Addressables에서 {key} 월드맵을 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 월드맵 정의에 연결된 관련 리소스를 비동기로 로드합니다.
        /// </summary>
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

                    TryCacheNodeAssetsSync(node);
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
        /// 월드맵 정의에 연결된 노드/연결선 리소스를 동기적으로 캐싱합니다.
        /// </summary>
        private void CacheWorldMapAssetsSync(WorldMapDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(definition.BackgroundAddress) && !_backgroundSpriteByAddress.ContainsKey(definition.BackgroundAddress))
            {
                Sprite background = LoadSpriteByAddressSync(definition.BackgroundAddress);
                if (background != null)
                {
                    _backgroundSpriteByAddress[definition.BackgroundAddress] = background;
                }
            }

            if (definition.Nodes != null)
            {
                for (int i = 0; i < definition.Nodes.Count; i++)
                {
                    TryCacheNodeAssetsSync(definition.Nodes[i]);
                }
            }

            if (definition.Edges != null)
            {
                for (int i = 0; i < definition.Edges.Count; i++)
                {
                    WorldMapEdgeDefinition edge = definition.Edges[i];
                    if (edge == null || string.IsNullOrWhiteSpace(edge.EdgeSpriteAddress) || _edgeSpriteByAddress.ContainsKey(edge.EdgeSpriteAddress))
                    {
                        continue;
                    }

                    Sprite edgeSprite = LoadSpriteByAddressSync(edge.EdgeSpriteAddress);
                    if (edgeSprite != null)
                    {
                        _edgeSpriteByAddress[edge.EdgeSpriteAddress] = edgeSprite;
                    }
                }
            }
        }

        /// <summary>
        /// 노드에 연결된 아이콘/비활성/데코레이션 리소스를 동기적으로 캐싱합니다.
        /// </summary>
        private void TryCacheNodeAssetsSync(WorldMapNodeDefinition node)
        {
            if (node == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(node.IconAddress) && !_iconSpriteByAddress.ContainsKey(node.IconAddress))
            {
                Sprite icon = LoadSpriteByAddressSync(node.IconAddress);
                if (icon != null)
                {
                    _iconSpriteByAddress[node.IconAddress] = icon;
                }
            }

            if (!string.IsNullOrWhiteSpace(node.InactiveSpriteAddress) && !_inactiveSpriteByAddress.ContainsKey(node.InactiveSpriteAddress))
            {
                Sprite inactive = LoadSpriteByAddressSync(node.InactiveSpriteAddress);
                if (inactive != null)
                {
                    _inactiveSpriteByAddress[node.InactiveSpriteAddress] = inactive;
                }
            }

            if (!string.IsNullOrWhiteSpace(node.DecorationSpriteAddress) && !_decorationSpriteByAddress.ContainsKey(node.DecorationSpriteAddress))
            {
                Sprite decoration = LoadSpriteByAddressSync(node.DecorationSpriteAddress);
                if (decoration != null)
                {
                    _decorationSpriteByAddress[node.DecorationSpriteAddress] = decoration;
                }
            }

            if (!string.IsNullOrWhiteSpace(node.DecorationAnimatorControllerAddress) &&
                !_decorationAnimatorByAddress.ContainsKey(node.DecorationAnimatorControllerAddress))
            {
                RuntimeAnimatorController controller = LoadAnimatorControllerByAddressSync(node.DecorationAnimatorControllerAddress);
                if (controller != null)
                {
                    _decorationAnimatorByAddress[node.DecorationAnimatorControllerAddress] = controller;
                }
            }
        }

        /// <summary>
        /// Addressables 키로 Sprite를 비동기로 로드합니다.
        /// </summary>
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
        /// Addressables 키로 Sprite를 동기적으로 로드합니다.
        /// </summary>
        private Sprite LoadSpriteByAddressSync(string address)
        {
            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);
            _activeHandles.Add(handle);
            Sprite sprite = handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return sprite;
            }

            GcLogger.LogError($"월드맵 Sprite 로드에 실패했습니다. key: {address}");
            return null;
        }

        public bool TryGetBackgroundSprite(WorldMapDefinition definition, out Sprite sprite)
        {
            sprite = null;
            return definition != null && TryGetBackgroundSprite(definition.BackgroundAddress, out sprite);
        }

        public bool TryGetBackgroundSprite(string address, out Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                sprite = null;
                return false;
            }

            if (!_backgroundSpriteByAddress.TryGetValue(address, out sprite) || sprite == null)
            {
                sprite = LoadSpriteByAddressSync(address);
                if (sprite != null)
                {
                    _backgroundSpriteByAddress[address] = sprite;
                }
            }

            return sprite != null;
        }

        public bool TryGetIconSprite(WorldMapNodeDefinition node, out Sprite sprite)
        {
            sprite = null;
            return node != null && TryGetIconSprite(node.IconAddress, out sprite);
        }

        public bool TryGetIconSprite(string address, out Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                sprite = null;
                return false;
            }

            if (!_iconSpriteByAddress.TryGetValue(address, out sprite) || sprite == null)
            {
                sprite = LoadSpriteByAddressSync(address);
                if (sprite != null)
                {
                    _iconSpriteByAddress[address] = sprite;
                }
            }

            return sprite != null;
        }

        public bool TryGetInactiveSprite(WorldMapNodeDefinition node, out Sprite sprite)
        {
            sprite = null;
            return node != null && TryGetInactiveSprite(node.InactiveSpriteAddress, out sprite);
        }

        public bool TryGetInactiveSprite(string address, out Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                sprite = null;
                return false;
            }

            if (!_inactiveSpriteByAddress.TryGetValue(address, out sprite) || sprite == null)
            {
                sprite = LoadSpriteByAddressSync(address);
                if (sprite != null)
                {
                    _inactiveSpriteByAddress[address] = sprite;
                }
            }

            return sprite != null;
        }

        public bool TryGetDecorationSprite(WorldMapNodeDefinition node, out Sprite sprite)
        {
            sprite = null;
            return node != null && TryGetDecorationSprite(node.DecorationSpriteAddress, out sprite);
        }

        public bool TryGetDecorationSprite(string address, out Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                sprite = null;
                return false;
            }

            if (!_decorationSpriteByAddress.TryGetValue(address, out sprite) || sprite == null)
            {
                sprite = LoadSpriteByAddressSync(address);
                if (sprite != null)
                {
                    _decorationSpriteByAddress[address] = sprite;
                }
            }

            return sprite != null;
        }

        public bool TryGetDecorationAnimatorController(WorldMapNodeDefinition node, out RuntimeAnimatorController animatorController)
        {
            animatorController = null;
            return node != null && TryGetDecorationAnimatorController(node.DecorationAnimatorControllerAddress, out animatorController);
        }

        public bool TryGetDecorationAnimatorController(string address, out RuntimeAnimatorController animatorController)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                animatorController = null;
                return false;
            }

            if (!_decorationAnimatorByAddress.TryGetValue(address, out animatorController) || animatorController == null)
            {
                animatorController = LoadAnimatorControllerByAddressSync(address);
                if (animatorController != null)
                {
                    _decorationAnimatorByAddress[address] = animatorController;
                }
            }

            return animatorController != null;
        }

        public bool TryGetEdgeSprite(WorldMapEdgeDefinition edge, out Sprite sprite)
        {
            sprite = null;
            return edge != null && TryGetEdgeSprite(edge.EdgeSpriteAddress, out sprite);
        }

        public bool TryGetEdgeSprite(string address, out Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                sprite = null;
                return false;
            }

            if (!_edgeSpriteByAddress.TryGetValue(address, out sprite) || sprite == null)
            {
                sprite = LoadSpriteByAddressSync(address);
                if (sprite != null)
                {
                    _edgeSpriteByAddress[address] = sprite;
                }
            }

            return sprite != null;
        }

        public float GetLoadProgress() => _prefabLoadProgress;

        /// <summary>
        /// Addressables 키로 RuntimeAnimatorController를 비동기로 로드합니다.
        /// </summary>
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

        /// <summary>
        /// Addressables 키로 RuntimeAnimatorController를 동기적으로 로드합니다.
        /// </summary>
        private RuntimeAnimatorController LoadAnimatorControllerByAddressSync(string address)
        {
            AsyncOperationHandle<RuntimeAnimatorController> handle = Addressables.LoadAssetAsync<RuntimeAnimatorController>(address);
            _activeHandles.Add(handle);
            RuntimeAnimatorController controller = handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return controller;
            }

            GcLogger.LogError($"월드맵 AnimatorController 로드에 실패했습니다. key: {address}");
            return null;
        }

        /// <summary>
        /// 특정 월드맵 키를 비동기로 로드하고 캐싱합니다.
        /// </summary>
        private async Task<WorldMapDefinition> LoadWorldMapByKeyAsync(string key)
        {
            AsyncOperationHandle<TextAsset> loadHandle = Addressables.LoadAssetAsync<TextAsset>(key);
            _activeHandles.Add(loadHandle);
            await loadHandle.Task;

            if (loadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                GcLogger.LogError($"월드맵 JSON 로드에 실패했습니다. key: {key}");
                return null;
            }

            TextAsset textAsset = loadHandle.Result;
            if (textAsset == null)
            {
                GcLogger.LogError($"월드맵 JSON TextAsset을 로드하지 못했습니다. key: {key}");
                return null;
            }

            string error;
            WorldMapDefinition definition = FromJson(textAsset.text, out error);
            if (definition == null)
            {
                GcLogger.LogError(error);
                return null;
            }

            await LoadSpritesAsync(definition);
            _dicWorldMap[key] = definition;
            return definition;
        }

        /// <summary>
        /// 특정 월드맵 키를 동기적으로 로드하고 캐싱합니다.
        /// </summary>
        private bool TryLoadWorldMapByKeySync(string key, out WorldMapDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            AsyncOperationHandle<TextAsset> loadHandle = Addressables.LoadAssetAsync<TextAsset>(key);
            _activeHandles.Add(loadHandle);
            TextAsset textAsset = loadHandle.WaitForCompletion();

            if (loadHandle.Status != AsyncOperationStatus.Succeeded || textAsset == null)
            {
                GcLogger.LogError($"월드맵 JSON 로드에 실패했습니다. key: {key}");
                return false;
            }

            string error;
            definition = FromJson(textAsset.text, out error);
            if (definition == null)
            {
                GcLogger.LogError(error);
                return false;
            }

            CacheWorldMapAssetsSync(definition);
            _dicWorldMap[key] = definition;
            return true;
        }

        /// <summary>
        /// 캐시를 초기화합니다.
        /// </summary>
        private void ClearCaches()
        {
            _dicWorldMap.Clear();
            _backgroundSpriteByAddress.Clear();
            _iconSpriteByAddress.Clear();
            _inactiveSpriteByAddress.Clear();
            _decorationSpriteByAddress.Clear();
            _decorationAnimatorByAddress.Clear();
            _edgeSpriteByAddress.Clear();
        }
    }
}
