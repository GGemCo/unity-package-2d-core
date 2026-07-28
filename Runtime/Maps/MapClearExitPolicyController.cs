using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 현재 맵의 몬스터 전멸 상태를 감시하고, 설정된 맵 종료 정책을 실행하는 컨트롤러입니다.
    /// 퀘스트 진행도와 분리하여 순수 맵 클리어 UX(Fade Out, 종료 UIWindow 오픈)만 담당합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapClearExitPolicyController : MonoBehaviour
    {
        private SceneGame _sceneGame;
        private int _currentMapUid;
        private bool _hasInitialMonsters;
        private bool _isExecuting;
        private bool _isEventRegistered;
        private bool _ignoreAliveMonstersForExecution;
        private int _pendingDestinationWindowUid;
        private Coroutine _executeRoutine;
        private readonly MapClearMonsterSuspensionScope _monsterSuspensionScope = new();

        /// <summary>
        /// 컨트롤러가 사용할 게임 씬 참조를 설정하고 맵/몬스터 이벤트를 구독합니다.
        /// </summary>
        /// <param name="sceneGame">현재 게임 씬 참조입니다.</param>
        public void Initialize(SceneGame sceneGame)
        {
            _sceneGame = sceneGame;
            RegisterEvents();
        }

        /// <summary>
        /// 현재 맵에서 몬스터 리젠을 억제해야 하는지 확인합니다.
        /// 맵 종료 정책이 활성화된 전투 맵에서는 마지막 몬스터 처치 판정이 흔들리지 않도록 리젠 예약을 막습니다.
        /// </summary>
        /// <param name="mapUid">확인할 맵 UID입니다.</param>
        /// <returns>리젠을 억제해야 하면 <see langword="true"/>입니다.</returns>
        public bool ShouldSuppressMonsterRespawn(int mapUid)
        {
            MapClearExitPolicySettings policy = GetPolicy();
            if (policy == null || !policy.enabled || !policy.suppressMonsterRespawn)
            {
                return false;
            }

            return _currentMapUid > 0 && _currentMapUid == mapUid;
        }

        /// <summary>
        /// 저장 데이터에 클리어된 현재 맵의 종료 연출을 명시적으로 요청합니다.
        /// 영역 도달이나 시나리오 완료처럼 몬스터 전멸 이외의 규칙으로 맵을 종료할 때 사용합니다.
        /// </summary>
        /// <param name="mapUid">종료 연출을 실행할 현재 맵 UID입니다.</param>
        /// <returns>종료 연출 루틴을 시작했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryRequestClearedMapExit(int mapUid)
        {
            MapClearExitPolicySettings policy = GetPolicy();
            if (policy == null || !policy.enabled || _isExecuting)
            {
                return false;
            }

            if (mapUid <= 0 || _currentMapUid != mapUid || !IsPlayerAliveForMapClear())
            {
                return false;
            }

            MapProgressController progressController =
                _sceneGame?.saveDataManager?.MapProgressController;
            if (progressController == null || !progressController.IsMapCleared(mapUid))
            {
                GcLogger.LogWarning(
                    $"[MapClearExitPolicyController] 클리어되지 않은 맵의 종료 연출 요청을 거부했습니다. mapUid:{mapUid}");
                return false;
            }

            _ignoreAliveMonstersForExecution = true;
            BeginMapClearExit();
            return true;
        }

        /// <summary>
        /// 이벤트 구독을 정리하고 실행 중인 맵 종료 루틴을 중단합니다.
        /// </summary>
        private void OnDestroy()
        {
            UnregisterEvents();
            StopExecuteRoutine();
            CancelPendingDestinationWindowOpen();
        }

        /// <summary>
        /// 맵 입장과 몬스터 사망 이벤트를 중복 없이 구독합니다.
        /// </summary>
        private void RegisterEvents()
        {
            if (_isEventRegistered)
            {
                return;
            }

            GameEventManager.MapEnteredEvent += OnMapEntered;
            GameEventManager.MonsterKilledEvent += OnMonsterKilled;
            _isEventRegistered = true;
        }

        /// <summary>
        /// 맵 입장과 몬스터 사망 이벤트 구독을 해제합니다.
        /// </summary>
        private void UnregisterEvents()
        {
            if (!_isEventRegistered)
            {
                return;
            }

            GameEventManager.MapEnteredEvent -= OnMapEntered;
            GameEventManager.MonsterKilledEvent -= OnMonsterKilled;
            _isEventRegistered = false;
        }

        /// <summary>
        /// 새 맵에 입장했을 때 현재 맵의 초기 몬스터 존재 여부와 실행 상태를 초기화합니다.
        /// </summary>
        /// <param name="eventData">맵 입장 이벤트 데이터입니다.</param>
        private void OnMapEntered(MapEnteredEventData eventData)
        {
            bool hadRunningExitPolicy = _isExecuting || _executeRoutine != null;
            StopExecuteRoutine();
            CancelPendingDestinationWindowOpen();
            if (hadRunningExitPolicy)
            {
                ClearMapExitFade(GetPolicy(), forceClear: true);
            }

            _currentMapUid = eventData.MapUid;
            _isExecuting = false;
            _ignoreAliveMonstersForExecution = false;
            _hasInitialMonsters = CountAliveMonsters() > 0;

            MapClearExitPolicySettings policy = GetPolicy();
            if (policy == null || !policy.enabled)
            {
                return;
            }

            if (_hasInitialMonsters || !policy.ignoreMapWithoutInitialMonsters)
            {
                TryBeginMapClearExitIfCompleted();
            }
        }

        /// <summary>
        /// 몬스터 사망 이벤트를 받아 현재 맵의 모든 몬스터가 사망했는지 확인합니다.
        /// </summary>
        /// <param name="eventData">몬스터 사망 이벤트 데이터입니다.</param>
        private void OnMonsterKilled(MonsterKilledEventData eventData)
        {
            MapClearExitPolicySettings policy = GetPolicy();
            if (policy == null || !policy.enabled || _isExecuting)
            {
                return;
            }

            if (_currentMapUid <= 0 || eventData.mapUid != _currentMapUid)
            {
                return;
            }

            if (policy.ignoreMapWithoutInitialMonsters && !_hasInitialMonsters)
            {
                return;
            }

            if (policy.requirePlayerKill && !eventData.isPlayerKiller)
            {
                return;
            }

            TryBeginMapClearExitIfCompleted();
        }

        /// <summary>
        /// 현재 맵에 살아있는 몬스터가 더 이상 없으면 맵 종료 루틴을 시작합니다.
        /// </summary>
        private void TryBeginMapClearExitIfCompleted()
        {
            if (_isExecuting)
            {
                return;
            }

            // 마지막 몬스터의 사망 스킬로 플레이어가 함께 사망한 경우에는
            // 몬스터 전멸보다 플레이어 사망 처리를 우선하여 맵 종료 연출을 시작하지 않습니다.
            if (!IsPlayerAliveForMapClear())
            {
                return;
            }

            if (CountAliveMonsters() > 0)
            {
                return;
            }

            _ignoreAliveMonstersForExecution = false;
            BeginMapClearExit();
        }

        /// <summary>
        /// 중복 검증이 끝난 현재 맵의 종료 연출 루틴을 시작합니다.
        /// </summary>
        private void BeginMapClearExit()
        {
            MapClearExitPolicySettings policy = GetPolicy();
            _isExecuting = true;

            // 종료 지연과 Fade 연출 중에도 생존 몬스터가 플레이어를 추적하지 않도록
            // 플레이어 조작 취소보다 먼저 몬스터 Brain 및 이동 잠금을 적용합니다.
            SuspendActiveMonstersOnMapClear(policy);
            CancelPlayerControlOnMapClear(policy);
            _executeRoutine = StartCoroutine(ExecuteMapClearExitRoutine(_currentMapUid));
        }

        /// <summary>
        /// 설정에 따라 현재 맵에서 활성화된 생존 몬스터의 Brain과 이동을 중단합니다.
        /// </summary>
        /// <param name="policy">현재 맵 종료 정책 설정입니다.</param>
        private void SuspendActiveMonstersOnMapClear(MapClearExitPolicySettings policy)
        {
            if (policy == null || !policy.suspendActiveMonstersOnClear)
            {
                return;
            }

            _monsterSuspensionScope.Suspend(
                _sceneGame?.mapManager,
                this,
                policy.cancelMonsterSkillsOnClear,
                policy.switchMonstersToIdleOnClear);
        }

        /// <summary>
        /// 맵 클리어 확정 시 플레이어의 자동 이동과 잔여 조작 상태를 정리합니다.
        /// Control 패키지가 설치되어 있으면 <see cref="IMapClearActionCanceler"/> 구현을 우선 호출하고,
        /// 구현이 없으면 Core 자동 이동 컨트롤러만 직접 취소합니다.
        /// </summary>
        /// <param name="policy">현재 맵 종료 정책 설정입니다.</param>
        private void CancelPlayerControlOnMapClear(MapClearExitPolicySettings policy)
        {
            if (policy == null || !policy.cancelAutoMoveOnClear)
            {
                return;
            }

            GameObject playerObject = _sceneGame != null ? _sceneGame.player : null;
            if (playerObject == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = playerObject.GetComponents<MonoBehaviour>();
            if (behaviours != null)
            {
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IMapClearActionCanceler canceler)
                    {
                        canceler.CancelActionsOnMapClear();
                        return;
                    }
                }
            }

            PlayerAutoMoveController autoMoveController = playerObject.GetComponent<PlayerAutoMoveController>();
            autoMoveController?.Cancel();
        }

        /// <summary>
        /// 맵 클리어 후 대기, Fade Out, 월드맵 UI 오픈 순서로 맵 종료 정책을 실행합니다.
        /// </summary>
        /// <param name="mapUid">실행을 시작한 맵 UID입니다.</param>
        /// <returns>Unity 코루틴 열거자입니다.</returns>
        private IEnumerator ExecuteMapClearExitRoutine(int mapUid)
        {
            MapClearExitPolicySettings policy = GetPolicy();
            if (policy == null || !policy.enabled)
            {
                AbortMapClearExitExecution();
                yield break;
            }

            if (policy.exitDelaySeconds > 0f)
            {
                yield return CreateWaitInstruction(policy.exitDelaySeconds, policy.fadeOutData);
            }

            if (!IsStillValidForExecution(mapUid, policy))
            {
                AbortMapClearExitExecution();
                yield break;
            }

            bool fadeAccepted = PlayFadeOut(policy);
            if (fadeAccepted && policy.fadeOutDurationSeconds > 0f)
            {
                yield return CreateWaitInstruction(policy.fadeOutDurationSeconds, policy.fadeOutData);
            }

            if (!IsStillValidForExecution(mapUid, policy))
            {
                ClearMapExitFade(policy, forceClear: true);
                AbortMapClearExitExecution();
                yield break;
            }

            int destinationWindowUid = ResolveDestinationWindowUid(mapUid, policy);
            if (destinationWindowUid > 0)
            {
                OpenDestinationWindow(destinationWindowUid);
            }

            if (policy.clearFadeAfterWorldMapOpen)
            {
                if (destinationWindowUid > 0)
                {
                    yield return WaitForDestinationWindowOpenBeforeFadeClear(
                        destinationWindowUid);
                }
                else
                {
                    yield return null;
                }

                ClearMapExitFade(policy, forceClear: false);
            }

            _executeRoutine = null;
            _ignoreAliveMonstersForExecution = false;
        }

        /// <summary>
        /// 유효성 검증에 실패한 맵 종료 실행 상태와 몬스터 잠금을 함께 정리합니다.
        /// </summary>
        private void AbortMapClearExitExecution()
        {
            _executeRoutine = null;
            _isExecuting = false;
            _ignoreAliveMonstersForExecution = false;
            _monsterSuspensionScope.Release();
        }

        /// <summary>
        /// 맵 종료 정책 실행 중 플레이어가 사망하거나 맵이 바뀌거나 새 몬스터가 생기지 않았는지 확인합니다.
        /// </summary>
        /// <param name="mapUid">실행 시작 시점의 맵 UID입니다.</param>
        /// <param name="policy">현재 맵 종료 정책 설정입니다.</param>
        /// <returns>계속 실행해도 되면 <see langword="true"/>입니다.</returns>
        private bool IsStillValidForExecution(int mapUid, MapClearExitPolicySettings policy)
        {
            if (policy == null || !policy.enabled)
            {
                return false;
            }

            if (_currentMapUid != mapUid)
            {
                return false;
            }

            return IsPlayerAliveForMapClear() &&
                   (_ignoreAliveMonstersForExecution || CountAliveMonsters() <= 0);
        }

        /// <summary>
        /// 현재 플레이어가 맵 클리어 종료 연출을 진행할 수 있는 생존 상태인지 확인합니다.
        /// </summary>
        /// <returns>
        /// 플레이어 오브젝트가 활성 상태이고 사망 또는 사망 보류 상태가 아니면
        /// <see langword="true"/>입니다.
        /// </returns>
        private bool IsPlayerAliveForMapClear()
        {
            GameObject playerObject = _sceneGame != null ? _sceneGame.player : null;
            if (playerObject == null || !playerObject.activeInHierarchy)
            {
                return false;
            }

            Player player = playerObject.GetComponent<Player>();
            return player != null && !player.IsStatusDead() && !player.IsDeathPending;
        }

        /// <summary>
        /// 현재 맵에 살아있는 몬스터 수를 계산합니다.
        /// 비활성화된 몬스터도 사망 상태가 아니라면 맵에 남아있는 몬스터로 취급합니다.
        /// </summary>
        /// <returns>현재 맵의 살아있는 몬스터 수입니다.</returns>
        private int CountAliveMonsters()
        {
            return _sceneGame?.mapManager != null
                ? _sceneGame.mapManager.CountCurrentMapAliveMonsters()
                : 0;
        }

        /// <summary>
        /// 설정에 맞는 화면 Fade Out 요청을 실행합니다.
        /// </summary>
        /// <param name="policy">현재 맵 종료 정책 설정입니다.</param>
        /// <returns>Fade 요청이 수락되었으면 <see langword="true"/>입니다.</returns>
        private bool PlayFadeOut(MapClearExitPolicySettings policy)
        {
            if (policy == null)
            {
                return false;
            }

            ScreenFadeRuntimeService fadeService = ScreenFadeRuntimeService.GetOrCreate(_sceneGame);
            if (fadeService == null)
            {
                return false;
            }

            ScreenFadeRequest request = ScreenFadeRequest.FromData(
                policy.fadeOutData,
                policy.fadeOutDurationSeconds,
                ScreenFadeOwner.MapExit,
                this);
            request.replaceMode = ScreenFadeReplaceMode.IgnoreIfOwnerPriorityIsGreaterOrEqual;

            return fadeService.Play(request);
        }

        /// <summary>
        /// 맵 종료 정책에서 유지 중인 Fade 화면을 필요 시 정리합니다.
        /// </summary>
        /// <param name="policy">현재 맵 종료 정책 설정입니다.</param>
        /// <param name="forceClear">정책의 정리 여부와 무관하게 강제로 정리할지 여부입니다.</param>
        private void ClearMapExitFade(MapClearExitPolicySettings policy, bool forceClear)
        {
            if (!forceClear && (policy == null || !policy.clearFadeAfterWorldMapOpen))
            {
                return;
            }

            ScreenFadeRuntimeService fadeService = ScreenFadeRuntimeService.GetOrCreate(_sceneGame);
            fadeService?.StopIfOwnedBy(ScreenFadeOwner.MapExit, this, forceClear: true);
        }

        /// <summary>
        /// 등록된 상위 계층 정책과 Core 기본 설정을 순서대로 확인하여 종료 화면 UID를 결정합니다.
        /// </summary>
        /// <param name="mapUid">클리어한 맵 UID입니다.</param>
        /// <param name="policy">현재 맵 종료 정책 설정입니다.</param>
        /// <returns>표시할 Window 테이블 UID이며, 표시할 화면이 없으면 0입니다.</returns>
        private static int ResolveDestinationWindowUid(
            int mapUid,
            MapClearExitPolicySettings policy)
        {
            if (MapClearExitDestinationResolverRegistry.TryResolve(
                    mapUid,
                    out MapClearExitDestination destination))
            {
                return destination.WindowUid;
            }

            return policy != null && policy.openWorldMap
                ? (int)UIWindowConstants.WindowUid.WorldMap
                : 0;
        }

        /// <summary>
        /// 지정한 맵 클리어 종료 UIWindow를 엽니다.
        /// 사망 연출이나 컷신으로 표시가 억제 중이면 요청을 보류하고, 억제 해제 후 자동으로 열리게 합니다.
        /// </summary>
        /// <param name="windowUid">표시할 Window 테이블 UID입니다.</param>
        private void OpenDestinationWindow(int windowUid)
        {
            if (windowUid <= 0 || _sceneGame?.uIWindowManager == null)
            {
                GcLogger.LogWarning(
                    $"맵 클리어 종료 UI를 열 수 없습니다. windowUid:{windowUid}");
                return;
            }

            var targetWindowUid = (UIWindowConstants.WindowUid)windowUid;
            if (!_sceneGame.uIWindowManager.HasManagedWindow(targetWindowUid))
            {
                GcLogger.LogWarning(
                    $"맵 클리어 종료 UI가 UIWindowManager에 등록되어 있지 않습니다. windowUid:{windowUid}");
                return;
            }

            _pendingDestinationWindowUid = windowUid;
            _sceneGame.uIWindowManager.ShowWindowWhenAllowed(
                targetWindowUid,
                true,
                UIWindowConstants.UIWindowVisibilityApplyMode.Normal,
                this);
        }

        /// <summary>
        /// 종료 화면 표시 요청이 UI 표시 억제에 막힌 경우, 억제 해제 후 LateUpdate에서 보류 요청이 적용될 시간을 확보합니다.
        /// Fade 화면을 종료 UI 오픈 전에 먼저 정리하지 않도록 맵 클리어 루틴에서 사용합니다.
        /// </summary>
        /// <param name="windowUid">표시 적용을 기다릴 Window 테이블 UID입니다.</param>
        /// <returns>종료 화면 표시 적용 가능 시점까지 대기하는 코루틴 열거자입니다.</returns>
        private IEnumerator WaitForDestinationWindowOpenBeforeFadeClear(int windowUid)
        {
            UIWindowManager windowManager = _sceneGame?.uIWindowManager;
            var targetWindowUid = (UIWindowConstants.WindowUid)windowUid;
            if (windowManager == null || !windowManager.HasManagedWindow(targetWindowUid))
            {
                yield return null;
                yield break;
            }

            while (windowManager.IsWindowVisibilitySuppressed(targetWindowUid))
            {
                yield return null;
            }

            // 표시 억제 해제 프레임의 LateUpdate에서 지연 요청이 적용될 수 있도록 한 프레임을 양보합니다.
            yield return null;
        }

        /// <summary>
        /// 맵 종료 루틴에서 예약한 종료 화면 표시 요청을 취소합니다.
        /// 맵 전환이나 컨트롤러 파괴 시 이전 맵의 지연 요청이 다음 맵에서 실행되는 것을 방지합니다.
        /// </summary>
        private void CancelPendingDestinationWindowOpen()
        {
            if (_pendingDestinationWindowUid <= 0)
            {
                return;
            }

            _sceneGame?.uIWindowManager?.CancelDeferredWindowVisibilityRequest(
                (UIWindowConstants.WindowUid)_pendingDestinationWindowUid,
                this);
            _pendingDestinationWindowUid = 0;
        }

        /// <summary>
        /// Fade 설정의 시간 기준에 맞는 대기 명령을 생성합니다.
        /// </summary>
        /// <param name="seconds">대기 시간입니다.</param>
        /// <param name="fadeData">시간 스케일 정책을 확인할 Fade 데이터입니다.</param>
        /// <returns>Unity 대기 명령입니다.</returns>
        private static object CreateWaitInstruction(float seconds, ScreenFadeData fadeData)
        {
            if (fadeData != null && fadeData.useUnscaledTime)
            {
                return new WaitForSecondsRealtime(seconds);
            }

            return new WaitForSeconds(seconds);
        }

        /// <summary>
        /// 실행 중인 맵 종료 루틴을 중단합니다.
        /// </summary>
        private void StopExecuteRoutine()
        {
            if (_executeRoutine != null)
            {
                StopCoroutine(_executeRoutine);
            }

            _executeRoutine = null;
            _isExecuting = false;
            _ignoreAliveMonstersForExecution = false;
            _monsterSuspensionScope.Release();
        }

        /// <summary>
        /// Addressables 설정에서 현재 맵 종료 정책 설정을 가져오고 기본값을 보정합니다.
        /// </summary>
        /// <returns>현재 맵 종료 정책 설정입니다.</returns>
        private static MapClearExitPolicySettings GetPolicy()
        {
            MapClearExitPolicySettings policy = AddressableLoaderSettings.Instance?.settings?.mapClearExitPolicy;
            policy?.EnsureDefaults();
            return policy;
        }
    }
}
