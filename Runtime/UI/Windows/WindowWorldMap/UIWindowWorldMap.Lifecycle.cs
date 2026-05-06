using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindowWorldMap의 생명주기 책임을 분리한 partial 클래스입니다.
    /// </summary>
    public partial class UIWindowWorldMap
    {
        /// <summary>
        /// 이 월드맵 윈도우가 UIWindowManager에 등록될 WindowUid를 반환합니다.
        /// 파생 윈도우는 UID만 바꿔 같은 월드맵 렌더링 로직을 재사용할 수 있습니다.
        /// </summary>
        /// <returns>윈도우 고유 ID입니다.</returns>
        protected virtual UIWindowConstants.WindowUid ResolveWindowUid()
        {
            return UIWindowConstants.WindowUid.WorldMap;
        }

        /// <summary>
        /// 월드맵 윈도우의 기본 의존성을 준비하고, 로드된 월드맵 정의가 있으면 초기 아이콘을 생성합니다.
        /// </summary>
        protected override void Awake()
        {
            EnsurePresentationOptions();
            _selectedUIIconWorldMap = null;
            uid = ResolveWindowUid();

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
            SetCurrentMapCenter();
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
    }
}
