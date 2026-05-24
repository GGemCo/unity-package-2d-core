using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 현재 맵의 몬스터 전멸 상태를 감시하고, 설정된 맵 종료 정책을 실행하는 컨트롤러입니다.
    /// 퀘스트 진행도와 분리하여 순수 맵 클리어 UX(Fade Out, 월드맵 오픈)만 담당합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapClearExitPolicyController : MonoBehaviour
    {
        private SceneGame _sceneGame;
        private int _currentMapUid;
        private bool _hasInitialMonsters;
        private bool _isExecuting;
        private bool _isEventRegistered;
        private Coroutine _executeRoutine;

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
        /// 이벤트 구독을 정리하고 실행 중인 맵 종료 루틴을 중단합니다.
        /// </summary>
        private void OnDestroy()
        {
            UnregisterEvents();
            StopExecuteRoutine();
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
            if (hadRunningExitPolicy)
            {
                ClearMapExitFade(GetPolicy(), forceClear: true);
            }

            _currentMapUid = eventData.MapUid;
            _isExecuting = false;
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

            if (CountAliveMonsters() > 0)
            {
                return;
            }

            _isExecuting = true;
            CancelPlayerControlOnMapClear(GetPolicy());
            _executeRoutine = StartCoroutine(ExecuteMapClearExitRoutine(_currentMapUid));
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
                _isExecuting = false;
                yield break;
            }

            if (policy.exitDelaySeconds > 0f)
            {
                yield return CreateWaitInstruction(policy.exitDelaySeconds, policy.fadeOutData);
            }

            if (!IsStillValidForExecution(mapUid, policy))
            {
                _isExecuting = false;
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
                _isExecuting = false;
                yield break;
            }

            if (policy.openWorldMap)
            {
                OpenWorldMapWindow();
            }

            if (policy.clearFadeAfterWorldMapOpen)
            {
                yield return null;
                ClearMapExitFade(policy, forceClear: false);
            }

            _executeRoutine = null;
        }

        /// <summary>
        /// 맵 종료 정책 실행 중 맵이 바뀌거나 새 몬스터가 생기지 않았는지 확인합니다.
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

            return CountAliveMonsters() <= 0;
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
        /// 월드맵 UI를 엽니다.
        /// </summary>
        private void OpenWorldMapWindow()
        {
            if (_sceneGame?.uIWindowManager == null)
            {
                GcLogger.LogWarning("월드맵 UI를 열 수 없습니다. UIWindowManager가 없습니다.");
                return;
            }

            _sceneGame.uIWindowManager.ShowWindow(UIWindowConstants.WindowUid.WorldMap, true);
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
            if (_executeRoutine == null)
            {
                return;
            }

            StopCoroutine(_executeRoutine);
            _executeRoutine = null;
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
