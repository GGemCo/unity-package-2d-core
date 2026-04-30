using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 월드맵 정의 데이터를 UI 노드와 연결선으로 표시하는 월드맵 윈도우입니다.
    /// </summary>
    public class UIWindowWorldMap : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("월드맵 노드와 연결선이 들어갈 최상위 오브젝트")]
        public GameObject containerWorldMap;

        [Tooltip("연결선이 들어갈 레이어입니다. 비어 있으면 런타임에 자동 생성합니다.")]
        [SerializeField] private RectTransform containerLineLayer;

        [Tooltip("노드 슬롯과 아이콘이 들어갈 레이어입니다. 비어 있으면 런타임에 자동 생성합니다.")]
        [SerializeField] private RectTransform containerNodeLayer;

        [Tooltip("월드맵 배경을 표시할 Image입니다. 비어 있으면 containerWorldMap의 Image를 사용합니다.")]
        [SerializeField] private Image imageBackground;

        [Tooltip("월드맵을 표시하는 viewport입니다. 비어 있으면 containerWorldMap의 부모 RectTransform을 사용합니다.")]
        [SerializeField] private RectTransform viewportWorldMap;
        
        [Tooltip("이동하기 버튼")]
        [SerializeField] private Button buttonWarp;

        [Tooltip("일반 연결선 색상")]
        [SerializeField] private Color edgeColorNormal = new Color(0.72f, 0.72f, 0.72f, 1f);

        [Tooltip("잠긴 연결선 색상")]
        [SerializeField] private Color edgeColorLocked = new Color(0.95f, 0.62f, 0.24f, 1f);

        [Tooltip("비밀 연결선 색상")]
        [SerializeField] private Color edgeColorSecret = new Color(0.62f, 0.46f, 0.92f, 1f);

        [Tooltip("선택된 노드와 연결된 연결선 강조 색상")]
        [SerializeField] private Color edgeColorHighlighted = new Color(0.3f, 0.75f, 1f, 1f);

        [Tooltip("연결선 두께")]
        [SerializeField] private float edgeThickness = 6f;

        [Header("연결선 이미지")]
        [Tooltip("일반 연결선에 사용할 기본 스프라이트입니다. 비어 있으면 기존 색상 라인으로 표시합니다.")]
        [SerializeField] private Sprite edgeSpriteNormal;

        [Tooltip("잠김 연결선에 사용할 기본 스프라이트입니다. 비어 있으면 일반 연결선 스프라이트를 사용합니다.")]
        [SerializeField] private Sprite edgeSpriteLocked;

        [Tooltip("비밀 연결선에 사용할 기본 스프라이트입니다. 비어 있으면 일반 연결선 스프라이트를 사용합니다.")]
        [SerializeField] private Sprite edgeSpriteSecret;

        [Tooltip("선택된 노드와 연결된 선에 사용할 하이라이트 스프라이트입니다. 비어 있으면 일반 스프라이트에 하이라이트 색상을 적용합니다.")]
        [SerializeField] private Sprite edgeSpriteHighlighted;

        [Tooltip("연결선 스프라이트를 그리는 방식입니다.")]
        [SerializeField] private WorldMapEdgeSpriteDrawMode edgeSpriteDrawMode = WorldMapEdgeSpriteDrawMode.Sliced;

        [Header("포인트 상태별 이미지")]
        [Tooltip("현재 플레이어가 있는 맵일 때 보여줄 이미지")]
        [SerializeField] private Sprite spriteCurrentMap;
        [Tooltip("플레이어가 이동 가능한 맵일 때")]
        [SerializeField] private Sprite spriteMovePossible;
        [Tooltip("플레이어가 이동 불가능한 맵일 때")]
        [SerializeField] private Sprite spriteMoveImPossible;

        private readonly Dictionary<string, UIIconWorldMap> _nodeIconById = new Dictionary<string, UIIconWorldMap>();
        private readonly Dictionary<string, RectTransform> _nodeRectById = new Dictionary<string, RectTransform>();
        private readonly List<WorldMapLineRenderer> _edgeLines = new List<WorldMapLineRenderer>();

        private UIIconWorldMap _selectedUIIconWorldMap;
        private MapManager _mapManager;
        private TableMap _tableMap;
        private WorldMapDefinition _worldMapDefinition;
        private string _requestedBackgroundAddress;
        private WorldMapDragController _dragController;

        /// <summary>현재 윈도우가 표시 중인 월드맵 정의입니다.</summary>
        public WorldMapDefinition WorldMapDefinition => _worldMapDefinition;

        /// <summary>
        /// 월드맵 윈도우의 기본 의존성을 준비하고, 로드된 월드맵 정의가 있으면 초기 아이콘을 생성합니다.
        /// </summary>
        protected override void Awake()
        {
            _selectedUIIconWorldMap = null;
            uid = UIWindowConstants.WindowUid.WorldMap;

            _tableMap = TableLoaderManager.Instance != null ? TableLoaderManager.Instance.TableMap : null;
            _worldMapDefinition = ResolveDefaultWorldMapDefinition();
            maxCountIcon = GetWorldMapNodeCount(_worldMapDefinition);

            EnsureWorldMapLayers();
            EnsureWorldMapDragController();
            ApplyBackgroundSprite();

            // 순서 중요: IconPoolManager에서 사용하므로 base.Awake 호출 전에 등록합니다.
            SlotIconBuildStrategyRegistry.Register(uid, window => new SlotIconBuildStrategyWorldMap(_tableMap));

            base.Awake();

            BuildEdgeLines();
            buttonWarp?.onClick.AddListener(OnClickWarp);
        }

        /// <summary>
        /// 씬 의존성을 연결하고, Awake 시점에 월드맵 정의가 없었다면 다시 적용을 시도합니다.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            _mapManager = SceneGame.mapManager;

            if (_worldMapDefinition == null)
            {
                TryApplyDefaultWorldMap(true);
            }
            else
            {
                RepositionWorldMapNodes();
                RefreshEdgeLines();
                ClampWorldMapDragPosition();
            }

            RefreshInactiveSlotStates();
            RefreshWorldMapNodePointStates();
        }

        /// <summary>
        /// 윈도우가 제거될 때 버튼 이벤트를 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            buttonWarp?.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// 월드맵 창 표시 상태가 바뀔 때 현재 플레이어 위치 기준의 노드 포인트를 갱신합니다.
        /// </summary>
        /// <param name="show">창을 표시하면 true, 숨기면 false입니다.</param>
        public override void OnShow(bool show)
        {
            base.OnShow(show);
            if (!show)
            {
                return;
            }

            _mapManager ??= SceneGame.mapManager;
            RefreshInactiveSlotStates();
            RefreshWorldMapNodePointStates();
        }

        /// <summary>
        /// 월드맵 컨테이너 크기가 바뀌면 정규화 좌표 기반 노드 위치를 다시 계산합니다.
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            RepositionWorldMapNodes();
            RefreshEdgeLines();
            ClampWorldMapDragPosition();
        }

        /// <summary>
        /// 기본 월드맵 정의를 조회해 윈도우에 적용합니다.
        /// </summary>
        /// <param name="rebuildIcons">아이콘과 연결선을 다시 생성할지 여부입니다.</param>
        /// <returns>월드맵 정의 적용에 성공하면 true입니다.</returns>
        public bool TryApplyDefaultWorldMap(bool rebuildIcons)
        {
            WorldMapDefinition definition = ResolveDefaultWorldMapDefinition();
            if (definition == null)
            {
                return false;
            }

            ApplyWorldMapDefinition(definition, rebuildIcons);
            return true;
        }

        /// <summary>
        /// 지정한 월드맵 정의를 현재 윈도우에 적용합니다.
        /// </summary>
        /// <param name="definition">표시할 월드맵 정의입니다.</param>
        /// <param name="rebuildIcons">아이콘과 연결선을 다시 생성할지 여부입니다.</param>
        public void ApplyWorldMapDefinition(WorldMapDefinition definition, bool rebuildIcons = true)
        {
            _worldMapDefinition = definition;
            maxCountIcon = GetWorldMapNodeCount(_worldMapDefinition);
            ApplyBackgroundSprite();

            if (rebuildIcons && IconPoolManager != null)
            {
                ClearWorldMapNodeCache();
                ClearEdgeLines();
                IconPoolManager.ResetMaxCountIcon(maxCountIcon);
                BuildEdgeLines();
                RefreshWorldMapNodePointStates();
                return;
            }

            RepositionWorldMapNodes();
            RefreshEdgeLines();
            ClampWorldMapDragPosition();
            RefreshWorldMapNodePointStates();
        }

        /// <summary>
        /// 슬롯 생성 전략에서 생성한 노드 슬롯과 아이콘을 월드맵 윈도우에 등록합니다.
        /// </summary>
        /// <param name="node">등록할 월드맵 노드 정의입니다.</param>
        /// <param name="slot">노드 슬롯 컴포넌트입니다.</param>
        /// <param name="icon">노드 아이콘 컴포넌트입니다.</param>
        public void RegisterWorldMapNode(WorldMapNodeDefinition node, UISlot slot, UIIconWorldMap icon)
        {
            if (node == null || slot == null || icon == null)
            {
                return;
            }

            RectTransform slotRect = slot.GetComponent<RectTransform>();
            if (slotRect == null)
            {
                return;
            }

            _nodeRectById[node.NodeId] = slotRect;
            _nodeIconById[node.NodeId] = icon;
            PositionWorldMapSlot(slotRect, node);
            RefreshWorldMapNodePointState(node, icon);
        }

        /// <summary>
        /// 월드맵 전용 선택 규칙을 적용합니다.
        /// </summary>
        /// <param name="index">선택할 월드맵 노드 슬롯 인덱스입니다.</param>
        public override void SetSelectedIcon(int index)
        {
            if (selectedIcon != null)
            {
                selectedIcon.SetSelected(false);
                selectedIcon = null;
            }

            if (!CanSelectWorldMapNode(index))
            {
                OnClearedSelectedIcon();
                return;
            }

            GameObject icon = icons[index];
            if (icon == null)
            {
                OnClearedSelectedIcon();
                return;
            }

            selectedIcon = icon.GetComponent<UIIcon>();
            if (selectedIcon == null)
            {
                OnClearedSelectedIcon();
                return;
            }

            selectedIcon.SetSelected(true);
            OnSelectedIcon(selectedIcon);
        }

        /// <summary>
        /// 월드맵 노드의 기본 노출 여부를 유지하면서 슬롯과 아이콘의 비활성 표시 상태를 갱신합니다.
        /// </summary>
        /// <param name="slotIndex">갱신할 월드맵 노드 슬롯 인덱스입니다.</param>
        public override void RefreshInactiveSlotState(int slotIndex)
        {
            if (slotIndex < 0)
            {
                return;
            }

            WorldMapNodeDefinition node = _worldMapDefinition != null &&
                                          _worldMapDefinition.Nodes != null &&
                                          slotIndex < _worldMapDefinition.Nodes.Count
                ? _worldMapDefinition.Nodes[slotIndex]
                : null;
            if (node != null && !node.VisibleByDefault)
            {
                if (slots != null && slotIndex < slots.Length)
                {
                    slots[slotIndex]?.SetActive(false);
                }

                if (icons != null && slotIndex < icons.Length)
                {
                    icons[slotIndex]?.SetActive(false);
                }

                return;
            }

            base.RefreshInactiveSlotState(slotIndex);
            if (node != null && node.InactiveByDefault)
            {
                ApplyWorldMapNodeInactiveVisual(slotIndex);
            }
        }

        /// <summary>
        /// 월드맵 노드를 보이는 상태로 유지하면서 슬롯과 아이콘에 비활성 비주얼을 적용합니다.
        /// </summary>
        /// <param name="slotIndex">비활성 비주얼을 적용할 월드맵 노드 슬롯 인덱스입니다.</param>
        private void ApplyWorldMapNodeInactiveVisual(int slotIndex)
        {
            if (slots != null && slotIndex < slots.Length)
            {
                GameObject slotObject = slots[slotIndex];
                if (slotObject != null)
                {
                    slotObject.SetActive(true);
                    slotObject.GetComponent<UISlot>()?.SetInactiveState(true);
                }
            }

            if (icons != null && slotIndex < icons.Length)
            {
                GameObject iconObject = icons[slotIndex];
                if (iconObject != null)
                {
                    iconObject.SetActive(true);
                    iconObject.GetComponent<UIIcon>()?.SetInactiveVisualState(true, false);
                }
            }
        }

        /// <summary>
        /// 월드맵 노드 정의의 정규화 좌표를 슬롯의 anchoredPosition으로 변환해 적용합니다.
        /// </summary>
        /// <param name="slotRect">위치를 적용할 슬롯 RectTransform입니다.</param>
        /// <param name="node">위치 값을 가진 월드맵 노드 정의입니다.</param>
        public void PositionWorldMapSlot(RectTransform slotRect, WorldMapNodeDefinition node)
        {
            if (slotRect == null || node == null)
            {
                return;
            }

            RectTransform parentRect = GetNodeLayerRect();
            if (parentRect == null)
            {
                return;
            }

            Rect rect = parentRect.rect;
            slotRect.anchorMin = Vector2.zero;
            slotRect.anchorMax = Vector2.zero;
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(
                node.NormalizedPosition.x * rect.width,
                node.NormalizedPosition.y * rect.height);
        }

        /// <summary>
        /// 월드맵 노드가 들어갈 부모 Transform을 반환합니다.
        /// </summary>
        /// <returns>노드 레이어 Transform입니다.</returns>
        public Transform GetWorldMapNodeParent()
        {
            EnsureWorldMapLayers();
            return containerNodeLayer != null ? containerNodeLayer : containerWorldMap?.transform;
        }

        /// <summary>
        /// 슬롯 위치를 index 기반으로 재배치합니다.
        /// 기존 호출부 호환을 위해 유지하며, 월드맵 정의가 있으면 해당 index의 노드 위치를 사용합니다.
        /// </summary>
        /// <param name="slot">위치를 변경할 슬롯입니다.</param>
        /// <param name="index">월드맵 노드 인덱스입니다.</param>
        public void SetPositionUiSlot(UISlot slot, int index)
        {
            if (slot == null || _worldMapDefinition == null || index < 0 || index >= _worldMapDefinition.Nodes.Count)
            {
                return;
            }

            PositionWorldMapSlot(slot.GetComponent<RectTransform>(), _worldMapDefinition.Nodes[index]);
        }

        /// <summary>
        /// 월드맵 전용 선택 참조를 기본 selectedIcon 흐름과 동기화합니다.
        /// 버튼 액션은 이 참조를 사용하므로 선택 변경 시 함께 갱신합니다.
        /// </summary>
        /// <param name="icon">선택된 아이콘입니다.</param>
        protected override void OnSelectedIcon(UIIcon icon)
        {
            base.OnSelectedIcon(icon);
            _selectedUIIconWorldMap = icon as UIIconWorldMap;
            RefreshEdgeHighlight();
        }

        /// <summary>
        /// 월드맵 아이콘 선택이 해제되었을 때 선택 참조와 연결선 강조를 정리합니다.
        /// </summary>
        protected override void OnClearedSelectedIcon()
        {
            base.OnClearedSelectedIcon();
            _selectedUIIconWorldMap = null;
            RefreshEdgeHighlight();
        }

        /// <summary>
        /// 현재 선택된 월드맵 노드의 mapUid로 맵 이동을 요청합니다.
        /// </summary>
        private void OnClickWarp()
        {
            if (GcLogger.IsNull(_mapManager, nameof(MapManager))) return;
            if (GcLogger.IsNull(_selectedUIIconWorldMap, "선택된 맵이 없습니다.")) return;
            if (!CanMoveToNode(_selectedUIIconWorldMap.NodeDefinition)) return;
            if (_selectedUIIconWorldMap.uid == _mapManager.GetCurrentMapUid()) return;
            _mapManager.LoadMap(_selectedUIIconWorldMap.uid);
        }

        /// <summary>
        /// 지정한 슬롯 인덱스의 월드맵 노드를 선택할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="index">확인할 월드맵 노드 슬롯 인덱스입니다.</param>
        /// <returns>노드가 보이고 현재 맵에서 바로 이동할 수 있으면 true를 반환합니다.</returns>
        private bool CanSelectWorldMapNode(int index)
        {
            if (icons == null || index < 0 || index >= icons.Length)
            {
                return false;
            }

            if (_worldMapDefinition == null || _worldMapDefinition.Nodes == null || index >= _worldMapDefinition.Nodes.Count)
            {
                return false;
            }

            WorldMapNodeDefinition node = _worldMapDefinition.Nodes[index];
            return CanMoveToNode(node);
        }

        /// <summary>
        /// 지정한 월드맵 노드로 현재 플레이어 위치에서 이동할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="node">이동 대상 월드맵 노드입니다.</param>
        /// <returns>노드가 표시 중이고 현재 맵과 바로 연결되어 있으면 true를 반환합니다.</returns>
        private bool CanMoveToNode(WorldMapNodeDefinition node)
        {
            if (_mapManager == null || _worldMapDefinition == null || node == null)
            {
                return false;
            }

            if (!IsNodeVisible(node))
            {
                return false;
            }

            int currentMapUid = _mapManager.GetCurrentMapUid();
            if (node.MapUid == currentMapUid)
            {
                return false;
            }

            return _worldMapDefinition.TryGetNodeByMapUid(currentMapUid, out WorldMapNodeDefinition currentNode) &&
                   _worldMapDefinition.IsAdjacentNode(currentNode.NodeId, node.NodeId);
        }

        /// <summary>
        /// 월드맵 노드가 플레이어에게 표시되는 상태인지 확인합니다.
        /// </summary>
        /// <param name="node">확인할 월드맵 노드입니다.</param>
        /// <returns>노드가 월드맵에 표시되는 상태이면 true를 반환합니다.</returns>
        private static bool IsNodeVisible(WorldMapNodeDefinition node)
        {
            return node != null && node.VisibleByDefault;
        }

        /// <summary>
        /// 모든 월드맵 노드 포인트 이미지를 현재 플레이어 위치 기준으로 갱신합니다.
        /// </summary>
        private void RefreshWorldMapNodePointStates()
        {
            if (_worldMapDefinition == null || _worldMapDefinition.Nodes == null)
            {
                return;
            }

            for (int i = 0; i < _worldMapDefinition.Nodes.Count; i++)
            {
                WorldMapNodeDefinition node = _worldMapDefinition.Nodes[i];
                if (node == null)
                {
                    continue;
                }

                if (_nodeIconById.TryGetValue(node.NodeId, out UIIconWorldMap icon))
                {
                    RefreshWorldMapNodePointState(node, icon);
                }
            }
        }

        /// <summary>
        /// 지정한 월드맵 노드의 포인트 이미지를 현재 플레이어 위치 기준으로 갱신합니다.
        /// </summary>
        /// <param name="node">갱신할 월드맵 노드입니다.</param>
        /// <param name="icon">포인트 이미지를 표시할 월드맵 아이콘입니다.</param>
        private void RefreshWorldMapNodePointState(WorldMapNodeDefinition node, UIIconWorldMap icon)
        {
            if (icon == null)
            {
                return;
            }

            icon.SetPointSprite(ResolveNodePointSprite(GetNodePointState(node)));
        }

        /// <summary>
        /// 지정한 월드맵 노드의 포인트 상태를 계산합니다.
        /// </summary>
        /// <param name="node">상태를 계산할 월드맵 노드입니다.</param>
        /// <returns>현재 플레이어 위치 기준의 노드 포인트 상태입니다.</returns>
        private WorldMapNodePointState GetNodePointState(WorldMapNodeDefinition node)
        {
            if (_mapManager == null || _worldMapDefinition == null || node == null || !IsNodeVisible(node))
            {
                return WorldMapNodePointState.None;
            }

            int currentMapUid = _mapManager.GetCurrentMapUid();
            if (node.MapUid == currentMapUid)
            {
                return WorldMapNodePointState.CurrentMap;
            }

            return _worldMapDefinition.TryGetNodeByMapUid(currentMapUid, out WorldMapNodeDefinition currentNode) &&
                   _worldMapDefinition.IsAdjacentNode(currentNode.NodeId, node.NodeId)
                ? WorldMapNodePointState.MovePossible
                : WorldMapNodePointState.MoveImpossible;
        }

        /// <summary>
        /// 월드맵 노드 포인트 상태에 맞는 Sprite를 반환합니다.
        /// </summary>
        /// <param name="state">포인트에 표시할 노드 상태입니다.</param>
        /// <returns>상태에 맞는 Sprite입니다. 표시할 Sprite가 없으면 null을 반환합니다.</returns>
        private Sprite ResolveNodePointSprite(WorldMapNodePointState state)
        {
            switch (state)
            {
                case WorldMapNodePointState.CurrentMap:
                    return spriteCurrentMap;
                case WorldMapNodePointState.MovePossible:
                    return spriteMovePossible;
                case WorldMapNodePointState.MoveImpossible:
                    return spriteMoveImPossible;
                default:
                    return null;
            }
        }

        /// <summary>
        /// AddressableLoaderWorldMap에 캐싱된 기본 월드맵 정의를 조회합니다.
        /// </summary>
        /// <returns>기본 월드맵 정의입니다. 로드되지 않았으면 null입니다.</returns>
        private static WorldMapDefinition ResolveDefaultWorldMapDefinition()
        {
            if (AddressableLoaderWorldMap.Instance == null)
            {
                return null;
            }

            return AddressableLoaderWorldMap.Instance.TryGetDefaultWorldMap(out WorldMapDefinition definition)
                ? definition
                : null;
        }

        /// <summary>
        /// 월드맵 정의의 노드 개수를 안전하게 반환합니다.
        /// </summary>
        /// <param name="definition">노드 개수를 확인할 월드맵 정의입니다.</param>
        /// <returns>노드 개수입니다.</returns>
        private static int GetWorldMapNodeCount(WorldMapDefinition definition)
        {
            return definition != null && definition.Nodes != null ? definition.Nodes.Count : 0;
        }

        /// <summary>
        /// AddressableLoaderWorldMap에 캐싱된 월드맵 배경 Sprite를 배경 Image에 적용합니다.
        /// </summary>
        private void ApplyBackgroundSprite()
        {
            if (_worldMapDefinition == null || string.IsNullOrWhiteSpace(_worldMapDefinition.BackgroundAddress))
            {
                return;
            }

            Image targetImage = GetBackgroundImage();
            if (targetImage == null)
            {
                return;
            }

            string address = _worldMapDefinition.BackgroundAddress;
            _requestedBackgroundAddress = address;
            if (AddressableLoaderWorldMap.Instance == null ||
                !AddressableLoaderWorldMap.Instance.TryGetBackgroundSprite(address, out Sprite backgroundSprite) ||
                backgroundSprite == null ||
                _requestedBackgroundAddress != address)
            {
                return;
            }

            targetImage.sprite = backgroundSprite;
            if (targetImage.color.a <= 0f)
            {
                targetImage.color = Color.white;
            }

            targetImage.enabled = true;
        }

        /// <summary>
        /// 배경을 표시할 Image를 반환합니다.
        /// 명시 연결이 없으면 containerWorldMap에 붙은 Image를 재사용합니다.
        /// </summary>
        /// <returns>배경 Image입니다. 찾지 못하면 null입니다.</returns>
        private Image GetBackgroundImage()
        {
            if (imageBackground != null)
            {
                return imageBackground;
            }

            if (containerWorldMap == null)
            {
                return null;
            }

            containerWorldMap.TryGetComponent(out imageBackground);
            return imageBackground;
        }

        /// <summary>
        /// 월드맵 컨테이너에 드래그 컨트롤러를 보장하고 viewport/content 참조를 연결합니다.
        /// </summary>
        private void EnsureWorldMapDragController()
        {
            if (containerWorldMap == null)
            {
                return;
            }

            RectTransform contentRect = containerWorldMap.GetComponent<RectTransform>();
            if (contentRect == null)
            {
                contentRect = containerWorldMap.AddComponent<RectTransform>();
            }

            if (viewportWorldMap == null)
            {
                viewportWorldMap = contentRect.parent as RectTransform;
            }

            _dragController = containerWorldMap.GetComponent<WorldMapDragController>();
            if (_dragController == null)
            {
                _dragController = containerWorldMap.AddComponent<WorldMapDragController>();
            }

            _dragController.Initialize(viewportWorldMap, contentRect);
        }

        /// <summary>
        /// 월드맵 드래그 위치가 viewport 경계를 벗어나지 않도록 보정합니다.
        /// </summary>
        private void ClampWorldMapDragPosition()
        {
            _dragController?.ClampContentPosition();
        }

        /// <summary>
        /// 월드맵 노드와 연결선 레이어를 보장하고 자유 배치를 위해 LayoutGroup을 비활성화합니다.
        /// </summary>
        private void EnsureWorldMapLayers()
        {
            if (containerWorldMap == null)
            {
                return;
            }

            LayoutGroup layoutGroup = containerWorldMap.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.enabled = false;
            }

            RectTransform root = containerWorldMap.GetComponent<RectTransform>();
            if (root == null)
            {
                root = containerWorldMap.AddComponent<RectTransform>();
            }

            containerLineLayer = EnsureLayer(root, containerLineLayer, "LineLayer");
            containerNodeLayer = EnsureLayer(root, containerNodeLayer, "NodeLayer");
            containerLineLayer.SetAsFirstSibling();
            containerNodeLayer.SetAsLastSibling();
        }

        /// <summary>
        /// 지정한 이름의 월드맵 레이어 RectTransform을 찾거나 생성합니다.
        /// </summary>
        /// <param name="root">레이어를 붙일 루트 RectTransform입니다.</param>
        /// <param name="current">이미 연결된 레이어 RectTransform입니다.</param>
        /// <param name="layerName">찾거나 생성할 레이어 이름입니다.</param>
        /// <returns>보장된 레이어 RectTransform입니다.</returns>
        private static RectTransform EnsureLayer(RectTransform root, RectTransform current, string layerName)
        {
            if (current != null)
            {
                return current;
            }

            Transform found = root.Find(layerName);
            if (found != null && found.TryGetComponent(out RectTransform foundRect))
            {
                return foundRect;
            }

            GameObject layerObject = new GameObject(layerName, typeof(RectTransform));
            RectTransform layerRect = layerObject.GetComponent<RectTransform>();
            layerRect.SetParent(root, false);
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            layerRect.pivot = new Vector2(0.5f, 0.5f);
            return layerRect;
        }

        /// <summary>
        /// 노드 레이어 RectTransform을 반환합니다.
        /// </summary>
        /// <returns>노드 레이어 RectTransform입니다.</returns>
        private RectTransform GetNodeLayerRect()
        {
            EnsureWorldMapLayers();
            return containerNodeLayer != null
                ? containerNodeLayer
                : containerWorldMap != null
                    ? containerWorldMap.GetComponent<RectTransform>()
                    : null;
        }

        /// <summary>
        /// 월드맵 노드/연결선 캐시를 초기화합니다.
        /// </summary>
        private void ClearWorldMapNodeCache()
        {
            _nodeIconById.Clear();
            _nodeRectById.Clear();
        }

        /// <summary>
        /// 생성되어 있는 모든 연결선 UI를 제거합니다.
        /// </summary>
        private void ClearEdgeLines()
        {
            for (int i = _edgeLines.Count - 1; i >= 0; i--)
            {
                WorldMapLineRenderer line = _edgeLines[i];
                if (line != null)
                {
                    Destroy(line.gameObject);
                }
            }

            _edgeLines.Clear();
        }

        /// <summary>
        /// 월드맵 정의의 edge 목록을 기준으로 연결선 UI를 생성합니다.
        /// </summary>
        private void BuildEdgeLines()
        {
            ClearEdgeLines();
            EnsureWorldMapLayers();

            if (_worldMapDefinition == null || _worldMapDefinition.Edges == null || containerLineLayer == null)
            {
                return;
            }

            for (int i = 0; i < _worldMapDefinition.Edges.Count; i++)
            {
                WorldMapEdgeDefinition edge = _worldMapDefinition.Edges[i];
                if (edge == null)
                {
                    continue;
                }

                if (!_nodeRectById.TryGetValue(edge.FromNodeId, out RectTransform from) ||
                    !_nodeRectById.TryGetValue(edge.ToNodeId, out RectTransform to))
                {
                    continue;
                }

                GameObject lineObject = new GameObject("Edge_" + edge.EdgeId, typeof(RectTransform), typeof(Image), typeof(WorldMapLineRenderer));
                RectTransform lineRect = lineObject.GetComponent<RectTransform>();
                lineRect.SetParent(containerLineLayer, false);
                lineRect.anchorMin = Vector2.zero;
                lineRect.anchorMax = Vector2.zero;

                WorldMapLineRenderer line = lineObject.GetComponent<WorldMapLineRenderer>();
                line.Initialize(
                    edge,
                    from,
                    to,
                    GetEdgeColor(edge.EdgeType),
                    edgeColorHighlighted,
                    edgeThickness,
                    ResolveEdgeSprite(edge),
                    edgeSpriteHighlighted,
                    edgeSpriteDrawMode);
                lineObject.SetActive(IsEdgeVisible(edge));
                _edgeLines.Add(line);
            }

            RefreshEdgeHighlight();
        }

        /// <summary>
        /// 노드 위치를 현재 컨테이너 크기에 맞춰 다시 계산합니다.
        /// </summary>
        private void RepositionWorldMapNodes()
        {
            if (_worldMapDefinition == null || _worldMapDefinition.Nodes == null)
            {
                return;
            }

            for (int i = 0; i < _worldMapDefinition.Nodes.Count; i++)
            {
                WorldMapNodeDefinition node = _worldMapDefinition.Nodes[i];
                if (node == null)
                {
                    continue;
                }

                if (_nodeRectById.TryGetValue(node.NodeId, out RectTransform slotRect))
                {
                    PositionWorldMapSlot(slotRect, node);
                }
            }
        }

        /// <summary>
        /// 모든 연결선 UI의 위치와 회전을 즉시 갱신합니다.
        /// </summary>
        private void RefreshEdgeLines()
        {
            for (int i = 0; i < _edgeLines.Count; i++)
            {
                if (_edgeLines[i] != null)
                {
                    _edgeLines[i].Refresh();
                }
            }
        }

        /// <summary>
        /// 현재 선택된 노드와 연결된 edge만 강조 표시합니다.
        /// </summary>
        private void RefreshEdgeHighlight()
        {
            string selectedNodeId = _selectedUIIconWorldMap != null ? _selectedUIIconWorldMap.NodeId : null;

            for (int i = 0; i < _edgeLines.Count; i++)
            {
                WorldMapLineRenderer line = _edgeLines[i];
                if (line != null)
                {
                    line.SetHighlighted(line.ContainsNode(selectedNodeId));
                }
            }
        }

        /// <summary>
        /// 연결선 타입에 맞는 기본 색상을 반환합니다.
        /// </summary>
        /// <param name="edgeType">연결선 타입입니다.</param>
        /// <returns>연결선 색상입니다.</returns>
        private Color GetEdgeColor(WorldMapEdgeType edgeType)
        {
            switch (edgeType)
            {
                case WorldMapEdgeType.Locked:
                    return edgeColorLocked;
                case WorldMapEdgeType.Secret:
                    return edgeColorSecret;
                default:
                    return edgeColorNormal;
            }
        }

        /// <summary>
        /// edge의 양 끝 노드가 기본 표시 대상인지 확인합니다.
        /// </summary>
        /// <param name="edge">표시 여부를 확인할 연결선 정의입니다.</param>
        /// <returns>양 끝 노드가 표시 대상이면 true입니다.</returns>
        /// <summary>
        /// 연결선 정의와 타입별 기본값을 기준으로 사용할 연결선 스프라이트를 반환합니다.
        /// </summary>
        /// <param name="edge">스프라이트를 결정할 연결선 정의입니다.</param>
        /// <returns>사용할 연결선 스프라이트입니다. 없으면 null을 반환합니다.</returns>
        private Sprite ResolveEdgeSprite(WorldMapEdgeDefinition edge)
        {
            if (edge != null &&
                AddressableLoaderWorldMap.Instance != null &&
                AddressableLoaderWorldMap.Instance.TryGetEdgeSprite(edge, out Sprite edgeSprite))
            {
                return edgeSprite;
            }

            return edge != null ? GetDefaultEdgeSprite(edge.EdgeType) : edgeSpriteNormal;
        }

        /// <summary>
        /// 연결선 타입에 맞는 기본 스프라이트를 반환합니다.
        /// </summary>
        /// <param name="edgeType">연결선 타입입니다.</param>
        /// <returns>타입별 기본 연결선 스프라이트입니다.</returns>
        private Sprite GetDefaultEdgeSprite(WorldMapEdgeType edgeType)
        {
            switch (edgeType)
            {
                case WorldMapEdgeType.Locked:
                    return edgeSpriteLocked != null ? edgeSpriteLocked : edgeSpriteNormal;
                case WorldMapEdgeType.Secret:
                    return edgeSpriteSecret != null ? edgeSpriteSecret : edgeSpriteNormal;
                default:
                    return edgeSpriteNormal;
            }
        }

        private bool IsEdgeVisible(WorldMapEdgeDefinition edge)
        {
            if (_worldMapDefinition == null || edge == null)
            {
                return false;
            }

            return _worldMapDefinition.TryGetNode(edge.FromNodeId, out WorldMapNodeDefinition from) &&
                   _worldMapDefinition.TryGetNode(edge.ToNodeId, out WorldMapNodeDefinition to) &&
                   from.VisibleByDefault &&
                   to.VisibleByDefault;
        }
    }
}
