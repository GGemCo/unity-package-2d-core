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
            buttonCancel?.onClick.AddListener(OnClickCancel);
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
        /// 윈도우가 제거될 때 버튼 이벤트와 대기 중인 닫힘 요청을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            buttonWarp?.onClick.RemoveAllListeners();
            buttonCancel?.onClick.RemoveAllListeners();

            // 씬 전환이나 오브젝트 제거로 닫기 연출이 완료되지 못한 경우에도 대기 중인 호출자를 정리합니다.
            CompleteCloseRequest(
                _hasPendingCloseReason
                    ? _pendingCloseReason
                    : WorldMapWindowCloseReason.Dismissed);
        }

        /// <summary>
        /// 월드맵 창 표시 상태가 바뀔 때 현재 플레이어 위치 기준의 노드 포인트를 갱신합니다.
        /// </summary>
        /// <param name="show">창을 표시하면 true, 숨기면 false입니다.</param>
        public override void OnShow(bool show)
        {
            if (show)
            {
                // 생명주기 이벤트 구독자가 갱신된 표시 정책을 확인할 수 있도록 공용 이벤트 발행 전에 적용합니다.
                ApplyPresentationOptionsForShow();
            }

            base.OnShow(show);
            if (!show)
            {
                ResetSelectionStateForWindowLifecycle();
                CompleteCloseRequest(
                    _hasPendingCloseReason
                        ? _pendingCloseReason
                        : WorldMapWindowCloseReason.Dismissed);
                return;
            }

            ResetSelectionStateForWindowLifecycle();
            _mapManager ??= SceneGame.mapManager;
            RefreshInactiveSlotStates();
            RefreshWorldMapNodePointStates();
            SetCurrentMapCenter();
        }

        /// <summary>
        /// 월드맵을 열고, 창이 닫힐 때 종료 사유를 한 번 전달받을 콜백을 등록합니다.
        /// 이미 열려 있거나 표시 요청이 실패한 경우에는 기존 창 상태와 콜백을 변경하지 않습니다.
        /// </summary>
        /// <param name="closeCallback">월드맵 종료 사유를 전달받을 일회성 콜백입니다.</param>
        /// <returns>월드맵을 새로 열고 콜백을 등록했으면 <see langword="true"/>입니다.</returns>
        public bool ShowWithCloseCallback(
            System.Action<WorldMapWindowCloseReason> closeCallback)
        {
            if (closeCallback == null || IsOpen())
            {
                return false;
            }

            PrepareCloseRequest(closeCallback);
            Show(true);
            if (IsOpen())
            {
                return true;
            }

            ClearCloseRequest();
            return false;
        }

        /// <summary>
        /// 닫힘 콜백을 등록하고 애니메이션 없이 월드맵을 즉시 표시합니다.
        /// 화면 페이드 뒤 월드맵을 노출하는 등 호출자가 표시 시점을 직접 제어할 때 사용합니다.
        /// </summary>
        /// <param name="closeCallback">월드맵 종료 사유를 전달받을 일회성 콜백입니다.</param>
        /// <param name="followLinkedWindows">Window 테이블에 연결된 윈도우도 함께 표시할지 여부입니다.</param>
        /// <returns>월드맵을 새로 열고 콜백을 등록했으면 <see langword="true"/>입니다.</returns>
        public bool ShowImmediateWithCloseCallback(
            System.Action<WorldMapWindowCloseReason> closeCallback,
            bool followLinkedWindows)
        {
            if (closeCallback == null || IsOpen())
            {
                return false;
            }

            PrepareCloseRequest(closeCallback);
            SetVisibleImmediate(
                show: true,
                invokeOnShow: true,
                followLinkedWindows: followLinkedWindows);
            if (IsOpen())
            {
                return true;
            }

            ClearCloseRequest();
            return false;
        }

        /// <summary>
        /// 현재 월드맵 표시 요청에 사용할 일회성 닫힘 콜백을 준비합니다.
        /// </summary>
        /// <param name="closeCallback">종료 사유를 전달받을 콜백입니다.</param>
        private void PrepareCloseRequest(
            System.Action<WorldMapWindowCloseReason> closeCallback)
        {
            _closeCallback = closeCallback;
            _hasPendingCloseReason = false;
        }

        /// <summary>
        /// 등록된 일회성 닫힘 콜백을 실행하고 종료 요청 상태를 초기화합니다.
        /// 콜백을 먼저 분리하여 콜백 내부에서 월드맵을 다시 열더라도 이전 요청과 섞이지 않게 합니다.
        /// </summary>
        /// <param name="closeReason">호출자에게 전달할 최종 종료 사유입니다.</param>
        private void CompleteCloseRequest(
            WorldMapWindowCloseReason closeReason)
        {
            System.Action<WorldMapWindowCloseReason> callback = _closeCallback;
            ClearCloseRequest();
            callback?.Invoke(closeReason);
        }

        /// <summary>
        /// 실행하지 않은 월드맵 닫힘 요청과 종료 사유를 초기화합니다.
        /// </summary>
        private void ClearCloseRequest()
        {
            _closeCallback = null;
            _pendingCloseReason = default;
            _hasPendingCloseReason = false;
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
