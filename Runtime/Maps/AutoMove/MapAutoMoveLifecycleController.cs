using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 전환 수명주기에 맞춰 플레이어 자동 이동을 제어합니다.
    /// 맵 이동 시작 시 기존 자동 이동을 취소하고, 화면 페이드 아웃이 완료되어 플레이 화면이 노출된 뒤 자동 이동을 시작합니다.
    /// </summary>
    public sealed class MapAutoMoveLifecycleController
    {
        private MapManager _mapManager;

        /// <summary>
        /// 맵 매니저 이벤트를 구독합니다.
        /// 중복 구독을 막기 위해 이전 구독이 있으면 먼저 해제합니다.
        /// </summary>
        /// <param name="mapManager">자동 이동 수명주기를 연결할 맵 매니저입니다.</param>
        public void Register(MapManager mapManager)
        {
            if (mapManager == null)
            {
                return;
            }

            Unregister(_mapManager);

            _mapManager = mapManager;
            _mapManager.OnLoadStartMap += StopAutoMoveOnMapLoadStart;
            MapManager.OnMapRevealComplete += StartAutoMoveAfterMapReveal;
        }

        /// <summary>
        /// 맵 매니저 이벤트 구독을 해제합니다.
        /// </summary>
        /// <param name="mapManager">구독 해제 대상 맵 매니저입니다.</param>
        public void Unregister(MapManager mapManager)
        {
            if (mapManager != null)
            {
                mapManager.OnLoadStartMap -= StopAutoMoveOnMapLoadStart;
            }

            MapManager.OnMapRevealComplete -= StartAutoMoveAfterMapReveal;

            if (_mapManager == mapManager)
            {
                _mapManager = null;
            }
        }

        /// <summary>
        /// 맵 이동이 시작되면 이전 맵에서 진행 중이던 자동 이동을 취소합니다.
        /// </summary>
        private void StopAutoMoveOnMapLoadStart()
        {
            PlayerAutoMoveController autoMove = FindPlayerAutoMoveController();
            autoMove?.Cancel();
        }

        /// <summary>
        /// 화면 페이드 아웃이 끝나 플레이 화면이 다시 보이면 설정값을 기준으로 자동 이동을 시작합니다.
        /// </summary>
        /// <param name="mapTileCommon">현재 로드된 맵 타일 루트입니다.</param>
        /// <param name="grid">현재 맵 Grid 오브젝트입니다.</param>
        private void StartAutoMoveAfterMapReveal(MapTileCommon mapTileCommon, GameObject grid)
        {
            GGemCoSettings settings = AddressableLoaderSettings.Instance
                ? AddressableLoaderSettings.Instance.settings
                : null;

            if (settings == null)
            {
                return;
            }

            if (!settings.autoMoveStartOnMapLoad)
            {
                return;
            }

            if (!AutoMovePolicyResolver.IsAutoMoveEnabled())
            {
                return;
            }

            PlayerAutoMoveController autoMove = FindPlayerAutoMoveController();
            if (autoMove == null)
            {
                return;
            }

            autoMove.StartAutoMove(CreateMapLoadAutoMoveRequest(settings), lockInput: true);
        }

        /// <summary>
        /// 맵 로드 완료 후 자동 이동에 사용할 요청 데이터를 생성합니다.
        /// </summary>
        /// <param name="settings">자동 이동 설정값을 가진 메인 설정입니다.</param>
        /// <returns>방향 기반 자동 이동 요청입니다.</returns>
        private static AutoMoveRequest CreateMapLoadAutoMoveRequest(GGemCoSettings settings)
        {
            return new AutoMoveRequest
            {
                moveType = AutoMoveType.Direction,
                direction = settings.autoMoveStartDirection,
                infiniteMove = settings.autoMoveStartDuration <= 0f,
                duration = Mathf.Max(0.01f, settings.autoMoveStartDuration),
                cancelPolicy = settings.autoMoveCancelPolicy,
                enableCombatTargetRecovery = settings.enableCombatTargetRecovery,
                combatTargetPassedEpsilon = settings.combatTargetPassedEpsilon
            };
        }

        /// <summary>
        /// 현재 씬 플레이어에서 자동 이동 컨트롤러를 찾습니다.
        /// </summary>
        /// <returns>플레이어 자동 이동 컨트롤러입니다. 찾지 못하면 null을 반환합니다.</returns>
        private static PlayerAutoMoveController FindPlayerAutoMoveController()
        {
            GameObject playerObject = SceneGame.Instance != null
                ? SceneGame.Instance.player
                : null;

            return playerObject != null
                ? playerObject.GetComponent<PlayerAutoMoveController>()
                : null;
        }
    }
}
