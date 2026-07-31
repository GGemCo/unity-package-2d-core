using System;
using R3;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 자동 이동(오토 워크)
    /// - Control 패키지 사용 시: 이동 벡터 오버라이드(IAutoMoveVectorProvider)로 연동
    /// - Core 단독(Old/New Input) 사용 시: FixedUpdate에서 직접 이동 실행
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAutoMoveController : MonoBehaviour, IAutoMoveVectorProvider, IAutoMoveSuspendService
    {
        private const string DiagnosticLogPrefix = "[WaitAnimationDebug]";

        /// <summary>
        /// 자동 이동 요청이 활성 상태이고 현재 맵 정책상 자동 이동을 사용할 수 있는지 여부입니다.
        /// </summary>
        public bool IsAutoMoveActive => _isActive && AutoMovePolicyResolver.IsAutoMoveEnabled();

        /// <summary>
        /// 자동 이동이 입력 잠금 상태로 동작 중인지 여부입니다.
        /// </summary>
        public bool IsInputLocked => IsAutoMoveActive && _lockInput;

        public bool IsAutoMoveSuspended => _suspendCount > 0;

        private CharacterBase _character;
        private CharacterBaseController _controller;

        // Control 등 외부 시스템이 "이동 실행"을 담당하는지 여부
        private IAutoMoveMovementDriver _movementDriver;

        private AutoMoveRequest _request;
        private bool _isActive;
        private bool _lockInput;
        private int _nextSuspendId;
        private int _suspendCount;
        private AutoMoveSuspendToken[] _suspendTokens;
        private float _elapsed;
        private float _originalMoveStep;
        private bool _isCombatTargetRecovering;
        private Transform _combatRecoveryTargetTransform;
        private float _combatRecoveryCooldownUntil;
        private bool _pendingCompleteAfterCombatTargetRecovery;
        private float _runtimeDirectionX;
        private CharacterConstants.BattleStatus _lastObservedBattleStatus;
        private bool _searchedNextTargetAfterCombatEnd;

        private void Awake()
        {
            _character = GetComponent<CharacterBase>();
            _controller = GetComponent<CharacterBaseController>();
            _originalMoveStep = _character != null ? _character.currentMoveStep : 0f;

            // Suspend 토큰은 런타임에만 사용되며, 일반적으로 동시에 1~2개(컷씬/벽액션) 수준이므로
            // 작은 고정 배열로 관리합니다(추가 필요 시 자동 확장).
            _nextSuspendId = 1;
            _suspendCount = 0;
            _suspendTokens = new AutoMoveSuspendToken[4];
        }

        private void Start()
        {
            _movementDriver = GetComponent<IAutoMoveMovementDriver>();
            BindBattleStatusChanged();
        }

        private void FixedUpdate()
        {
            if (!_isActive) return;

            // 전역 설정과 현재 맵 정책을 조합한 결과가 꺼져 있으면 즉시 중단합니다.
            if (!AutoMovePolicyResolver.IsAutoMoveEnabled())
            {
                Cancel();
                return;
            }

            if (_character == null || _controller == null)
            {
                Cancel();
                return;
            }

            // Control 패키지 사용(= PlayerInput 존재) 시에는 InputManager가 Move를 실행하므로,
            // 여기서는 완료 조건(거리/시간)만 판단한다.
            // Core 단독 사용 시에는 직접 Run()까지 수행한다.
            bool isDrivenByControl = _movementDriver is { DrivesAutoMoveMovement: true };

            // Suspend 중에는 이동/완료 판정을 진행하지 않는다(Pause).
            if (IsAutoMoveSuspended)
            {
                if (!isDrivenByControl && _character != null)
                {
                    _character.directionNormalize = Vector2.zero;
                    _character.Stop();
                }
                return;
            }

            Vector2 moveVector = GetMoveVector();

            if (!isDrivenByControl)
            {
                _character.directionNormalize = moveVector;
                if (moveVector != Vector2.zero)
                {
                    _controller.Run();
                }
                else
                {
                    _character.Stop();
                }
            }

            if (_pendingCompleteAfterCombatTargetRecovery)
            {
                Complete();
                return;
            }

            TickCompletion();
        }

        public AutoMoveSuspendToken AcquireSuspend(AutoMoveSuspendReason reason)
        {
            // AutoMove가 비활성이라도, 잠금은 누적해두었다가 활성화 시 즉시 반영되도록 한다.
            // (Wall 액션 중 AutoMove 시작 요청이 들어오는 케이스 대비)
            int id = _nextSuspendId++;
            if (_nextSuspendId == int.MaxValue) _nextSuspendId = 1;

            EnsureSuspendCapacity(_suspendCount + 1);
            var token = new AutoMoveSuspendToken(id, reason);
            _suspendTokens[_suspendCount++] = token;
            return token;
        }

        public void ReleaseSuspend(AutoMoveSuspendToken token)
        {
            if (!token.IsValid || _suspendCount <= 0) return;

            for (int i = 0; i < _suspendCount; i++)
            {
                if (_suspendTokens[i].id != token.id) continue;

                // swap-remove
                int last = _suspendCount - 1;
                _suspendTokens[i] = _suspendTokens[last];
                _suspendTokens[last] = default;
                _suspendCount--;
                return;
            }
        }

        private void EnsureSuspendCapacity(int needed)
        {
            if (_suspendTokens == null)
            {
                _suspendTokens = new AutoMoveSuspendToken[Mathf.Max(4, needed)];
                return;
            }

            if (_suspendTokens.Length >= needed) return;

            int newSize = Mathf.Max(_suspendTokens.Length * 2, needed);
            var next = new AutoMoveSuspendToken[newSize];
            Array.Copy(_suspendTokens, next, _suspendTokens.Length);
            _suspendTokens = next;
        }

        /// <summary>
        /// 전역 설정과 현재 맵 정책을 확인한 뒤 자동 이동을 시작합니다.
        /// </summary>
        /// <param name="request">자동 이동 요청 데이터입니다.</param>
        /// <param name="lockInput">자동 이동 중 플레이어 입력을 잠글지 여부입니다.</param>
        public void StartAutoMove(AutoMoveRequest request, bool lockInput = true)
        {
            if (request == null)
            {
                GcLogger.LogWarning($"{nameof(PlayerAutoMoveController)} StartAutoMove failed: request is null");
                return;
            }

            if (!AutoMovePolicyResolver.IsAutoMoveEnabled())
            {
                return;
            }

            _request = request;
            _elapsed = 0f;
            _isActive = true;
            _lockInput = lockInput;
            InitializeRuntimeDirection(request.direction);
            ResetCombatTargetRecovery();
            ResetNextTargetSearchStateForCurrentBattleStatus();

            if (_character != null)
            {
                _originalMoveStep = _character.currentMoveStep;
                if (_request.speedScale > 0f && Mathf.Abs(_request.speedScale - 1f) > 0.0001f)
                {
                    _character.currentMoveStep = _originalMoveStep * _request.speedScale;
                }
                _character.SetStatusMoveForce();
            }
        }

        /// <summary>
        /// 활성화된 자동 이동을 취소하고 캐릭터를 기본 대기 상태로 전환합니다.
        /// </summary>
        public void Cancel()
        {
            if (!_isActive) return;
            LogStopDiagnostic("AutoMoveCancel");
            _isActive = false;
            _lockInput = false;
            ResetCombatTargetRecovery();
            _searchedNextTargetAfterCombatEnd = false;

            RestoreMoveStep();

            if (_character != null)
            {
                _character.Stop();
            }
        }

        /// <summary>
        /// 자동 이동 완료 상태를 정리하고 캐릭터를 기본 대기 상태로 전환합니다.
        /// </summary>
        private void Complete()
        {
            if (!_isActive) return;
            LogStopDiagnostic("AutoMoveComplete");
            _isActive = false;
            _lockInput = false;
            ResetCombatTargetRecovery();
            _searchedNextTargetAfterCombatEnd = false;

            RestoreMoveStep();

            if (_character != null)
            {
                _character.Stop();
            }

            try
            {
                _request?.onArrived?.Invoke();
            }
            catch (Exception ex)
            {
                GcLogger.LogException(ex);
            }
        }

        /// <summary>
        /// 자동 이동 종료로 캐릭터 정지가 요청되는 시점을 확인할 임시 진단 로그를 출력합니다.
        /// </summary>
        /// <param name="phase">자동 이동 종료 원인을 나타내는 처리 단계입니다.</param>
        private void LogStopDiagnostic(string phase)
        {
            string characterStatus = _character != null
                ? _character.GetCurrentStatus().ToString()
                : "Unavailable";
            CharacterConstants.BattleStatus battleStatus = _character != null
                ? _character.GetBattleStatus()
                : CharacterConstants.BattleStatus.None;
            string moveType = _request != null
                ? _request.moveType.ToString()
                : "Unavailable";
            GcLogger.Log(
                $"{DiagnosticLogPrefix} phase={phase}, frame={Time.frameCount}, " +
                $"time={Time.time:F3}, characterStatus={characterStatus}, " +
                $"battleStatus={battleStatus}, isActive={_isActive}, lockInput={_lockInput}, " +
                $"moveType={moveType}");
        }


        /// <summary>
        /// 자동 이동 중 입력 잠금 정책에 따라 지정한 입력을 차단해야 하는지 확인합니다.
        /// </summary>
        /// <param name="inputType">확인할 입력 종류입니다.</param>
        /// <returns>입력을 차단해야 하면 true를 반환합니다.</returns>
        public bool ShouldBlockInput(AutoMoveInputType inputType)
        {
            if (!IsAutoMoveActive || !_lockInput) return false;

            // 전역 설정: 이동만 잠금(공격/점프/대시/상호작용 허용)
            var settings = AddressableLoaderSettings.Instance ? AddressableLoaderSettings.Instance.settings : null;
            if (settings != null && settings.autoMoveLockMovementOnly)
            {
                return inputType == AutoMoveInputType.Move;
            }

            return true;
        }

        private void RestoreMoveStep()
        {
            if (_character == null) return;
            if (_originalMoveStep > 0f)
            {
                _character.currentMoveStep = _originalMoveStep;
            }
        }

        /// <summary>
        /// 플레이어 전투 상태 변경을 구독하여 전투 종료 시 다음 자동 이동 방향 검색을 1회 실행할 수 있도록 준비합니다.
        /// </summary>
        private void BindBattleStatusChanged()
        {
            if (_character == null)
            {
                return;
            }

            _lastObservedBattleStatus = _character.GetBattleStatus();
            _character.CurrentBattleStatus
                .Subscribe(OnBattleStatusChanged)
                .AddTo(this);
        }

        /// <summary>
        /// 현재 전투 상태를 기준으로 다음 타겟 검색 래치를 초기화합니다.
        /// </summary>
        private void ResetNextTargetSearchStateForCurrentBattleStatus()
        {
            if (_character == null)
            {
                _searchedNextTargetAfterCombatEnd = false;
                return;
            }

            _lastObservedBattleStatus = _character.GetBattleStatus();
            _searchedNextTargetAfterCombatEnd =
                _lastObservedBattleStatus != CharacterConstants.BattleStatus.InBattle;
        }

        /// <summary>
        /// 플레이어 전투 상태 전환을 감지하고, 전투 종료 시 다음 생존 몬스터 방향으로 자동 이동을 이어갈지 판단합니다.
        /// </summary>
        /// <param name="battleStatus">변경된 전투 상태입니다.</param>
        private void OnBattleStatusChanged(CharacterConstants.BattleStatus battleStatus)
        {
            CharacterConstants.BattleStatus previousStatus = _lastObservedBattleStatus;
            _lastObservedBattleStatus = battleStatus;

            if (battleStatus == CharacterConstants.BattleStatus.InBattle)
            {
                _searchedNextTargetAfterCombatEnd = false;
                return;
            }

            if (previousStatus != CharacterConstants.BattleStatus.InBattle)
            {
                return;
            }

            TrySearchNextCombatTargetDirectionOnce();
        }

        /// <summary>
        /// 전투 종료 후 현재 타겟이 없을 때 다음 생존 몬스터를 한 번만 검색하고 자동 이동 방향을 갱신합니다.
        /// </summary>
        private void TrySearchNextCombatTargetDirectionOnce()
        {
            if (_searchedNextTargetAfterCombatEnd)
            {
                return;
            }

            _searchedNextTargetAfterCombatEnd = true;

            if (!CanSearchNextCombatTargetDirection())
            {
                return;
            }

            if (HasValidCombatTargetForNextSearch())
            {
                return;
            }

            GGemCoSettings settings = GetSettings();
            MapManager mapManager = SceneGame.Instance != null ? SceneGame.Instance.mapManager : null;
            if (settings == null || mapManager == null)
            {
                return;
            }

            Vector2 origin = _character.transform.position;
            if (!mapManager.TryFindNearestAliveMonster(
                    origin,
                    settings.autoMoveNextCombatTargetIncludeInactive,
                    settings.autoMoveNextCombatTargetSearchRange,
                    out Monster nextMonster))
            {
                return;
            }

            SetRuntimeDirectionTowards(nextMonster.transform.position);
        }

        /// <summary>
        /// 다음 전투 타겟 방향 검색 정책을 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>정책을 사용할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool CanSearchNextCombatTargetDirection()
        {
            if (!_isActive || _request == null || _character == null)
            {
                return false;
            }

            if (!_character.IsPlayer() || _character.IsInBattle())
            {
                return false;
            }

            if (_request.moveType != AutoMoveType.Direction)
            {
                return false;
            }

            if (!AutoMovePolicyResolver.IsAutoMoveEnabled())
            {
                return false;
            }

            GGemCoSettings settings = GetSettings();
            return settings != null && settings.enableAutoMoveNextCombatTargetSearch;
        }

        /// <summary>
        /// 전투 종료 직후에도 유지할 수 있는 현재 전투 타겟이 있는지 확인합니다.
        /// </summary>
        /// <returns>유효한 현재 타겟이 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool HasValidCombatTargetForNextSearch()
        {
            Transform targetTransform = ResolveCombatRecoveryTargetTransform();
            return targetTransform != null;
        }

        /// <summary>
        /// 지정한 위치가 있는 방향으로 Direction 자동 이동의 런타임 방향을 갱신합니다.
        /// </summary>
        /// <param name="targetPosition">다음 타겟 위치입니다.</param>
        private void SetRuntimeDirectionTowards(Vector3 targetPosition)
        {
            if (_character == null)
            {
                return;
            }

            float deltaX = targetPosition.x - _character.transform.position.x;
            if (Mathf.Abs(deltaX) <= 0.0001f)
            {
                return;
            }

            _runtimeDirectionX = deltaX < 0f ? -1f : 1f;
        }

        /// <summary>
        /// 자동 이동 관련 전역 설정을 반환합니다.
        /// </summary>
        /// <returns>로드된 메인 설정입니다. 아직 로드되지 않았으면 null입니다.</returns>
        private static GGemCoSettings GetSettings()
        {
            return AddressableLoaderSettings.Instance != null
                ? AddressableLoaderSettings.Instance.settings
                : null;
        }

        /// <summary>
        /// 현재 자동 이동 요청과 맵 정책을 기준으로 이동 벡터를 계산합니다.
        /// </summary>
        /// <returns>자동 이동에 사용할 정규화된 이동 벡터입니다.</returns>
        public Vector2 GetMoveVector()
        {
            if (!IsAutoMoveActive || _request == null || _character == null) return Vector2.zero;
            if (_pendingCompleteAfterCombatTargetRecovery) return Vector2.zero;

            // Pause 상태에서는 이동 벡터를 제공하지 않는다.
            if (IsAutoMoveSuspended) return Vector2.zero;

            switch (_request.moveType)
            {
                case AutoMoveType.Direction:
                {
                    // 전투 중 타겟을 지나친 경우에는 기존 진행 방향 대신 타겟 방향으로 복귀시킨다.
                    if (TryResolveCombatRecoveryMoveVector(out Vector2 recoveryMoveVector))
                    {
                        return recoveryMoveVector;
                    }

                    float dirX = GetRuntimeDirectionX();
                    return new Vector2(dirX, 0f).normalized;
                }

                case AutoMoveType.Target:
                default:
                {
                    var target = ResolveTargetPosition();
                    if (!target.HasValue) return Vector2.zero;

                    Vector2 current = _character.transform.position;
                    Vector2 delta = target.Value - current;
                    if (delta.sqrMagnitude < 0.0001f) return Vector2.zero;
                    return delta.normalized;
                }
            }
        }

        /// <summary>
        /// Direction 자동 이동 중 전투 타겟 지나침 복귀 벡터를 계산합니다.
        /// </summary>
        /// <param name="moveVector">복귀 이동 벡터입니다.</param>
        /// <returns>복귀 이동 또는 정지 처리를 적용해야 하면 true를 반환합니다.</returns>
        private bool TryResolveCombatRecoveryMoveVector(out Vector2 moveVector)
        {
            moveVector = Vector2.zero;

            if (!CanUseCombatTargetRecovery())
            {
                ResetCombatTargetRecovery();
                return false;
            }

            if (!TryResolveCombatRecoveryTarget(out Transform targetTransform, out Vector2 targetPosition))
            {
                ResetCombatTargetRecovery();
                return false;
            }

            Vector2 currentPosition = _character.transform.position;
            if (_isCombatTargetRecovering)
            {
                return TickCombatTargetRecovery(targetTransform, currentPosition, targetPosition, out moveVector);
            }

            if (Time.time < _combatRecoveryCooldownUntil)
            {
                // 복귀 종료 직후에는 목표 지점 근처에서 원래 진행 방향으로 즉시 재출발하지 않도록 정지 벡터를 유지합니다.
                if (IsWithinCombatRecoveryStopDistance(currentPosition, targetPosition))
                {
                    return true;
                }

                return false;
            }

            if (!IsPassedCombatTarget(currentPosition, targetPosition))
            {
                return false;
            }

            BeginCombatTargetRecovery(targetTransform, flipRuntimeDirection: true);
            return TickCombatTargetRecovery(targetTransform, currentPosition, targetPosition, out moveVector);
        }

        /// <summary>
        /// Direction 자동 이동에서 전투 타겟 복귀 로직을 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>복귀 로직 적용 가능 상태이면 true를 반환합니다.</returns>
        private bool CanUseCombatTargetRecovery()
        {
            if (_request == null || _character == null)
            {
                return false;
            }

            if (_request.moveType != AutoMoveType.Direction)
            {
                return false;
            }

            if (!_request.enableCombatTargetRecovery)
            {
                return false;
            }

            // 요구사항: 플레이어 전투 상태에서만 지나침 복귀를 적용한다.
            if (!_character.IsPlayer() || !_character.IsInBattle())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 전투 복귀 판정에 사용할 타겟을 계산합니다.
        /// </summary>
        /// <param name="targetTransform">복귀 기준 타겟 Transform입니다.</param>
        /// <param name="targetPosition">복귀 기준 타겟 좌표입니다.</param>
        /// <returns>유효한 타겟을 찾으면 true를 반환합니다.</returns>
        private bool TryResolveCombatRecoveryTarget(out Transform targetTransform, out Vector2 targetPosition)
        {
            targetTransform = null;
            targetPosition = Vector2.zero;

            Transform resolvedTransform = ResolveCombatRecoveryTargetTransform();
            if (resolvedTransform != null)
            {
                targetTransform = resolvedTransform;
                targetPosition = resolvedTransform.position;
                return true;
            }

            Vector2? requestTargetPosition = ResolveRequestTargetPosition();
            if (requestTargetPosition.HasValue)
            {
                targetPosition = requestTargetPosition.Value;
                return true;
            }

            return false;
        }


        /// <summary>
        /// Direction 자동 이동 요청이 시작될 때 사용할 런타임 진행 방향을 초기화합니다.
        /// </summary>
        /// <param name="direction">자동 이동 요청에 설정된 초기 방향입니다.</param>
        private void InitializeRuntimeDirection(AutoMoveDirection direction)
        {
            _runtimeDirectionX = direction == AutoMoveDirection.Left ? -1f : 1f;
        }

        /// <summary>
        /// 현재 Direction 자동 이동에서 사용할 런타임 진행 방향을 반환합니다.
        /// </summary>
        /// <returns>왼쪽이면 -1, 오른쪽이면 1을 반환합니다.</returns>
        private float GetRuntimeDirectionX()
        {
            if (Mathf.Abs(_runtimeDirectionX) <= 0.0001f)
            {
                InitializeRuntimeDirection(_request != null ? _request.direction : AutoMoveDirection.Right);
            }

            return _runtimeDirectionX < 0f ? -1f : 1f;
        }

        /// <summary>
        /// 전투 타겟을 지나쳤을 때 다음 자동 이동 기준 방향을 반전합니다.
        /// </summary>
        private void FlipRuntimeDirection()
        {
            _runtimeDirectionX = -GetRuntimeDirectionX();
        }

        /// <summary>
        /// 전투 타겟 복귀 상태를 시작합니다.
        /// </summary>
        /// <param name="targetTransform">복귀 기준 타겟 Transform입니다.</param>
        /// <param name="flipRuntimeDirection">지나침 확정에 따라 런타임 진행 방향을 반전할지 여부입니다.</param>
        private void BeginCombatTargetRecovery(Transform targetTransform, bool flipRuntimeDirection)
        {
            _isCombatTargetRecovering = true;
            _combatRecoveryTargetTransform = targetTransform;

            if (flipRuntimeDirection && _request != null && _request.flipDirectionOnCombatTargetPassed)
            {
                FlipRuntimeDirection();
            }
        }

        /// <summary>
        /// 전투 타겟 복귀 중 이동 방향 또는 완료 상태를 갱신합니다.
        /// </summary>
        /// <param name="targetTransform">현재 복귀 기준 타겟 Transform입니다.</param>
        /// <param name="currentPosition">현재 플레이어 위치입니다.</param>
        /// <param name="targetPosition">현재 타겟 위치입니다.</param>
        /// <param name="moveVector">계산된 복귀 이동 벡터입니다.</param>
        /// <returns>복귀 상태가 이동 벡터를 처리했으면 true를 반환합니다.</returns>
        private bool TickCombatTargetRecovery(Transform targetTransform, Vector2 currentPosition, Vector2 targetPosition, out Vector2 moveVector)
        {
            moveVector = Vector2.zero;

            if (_combatRecoveryTargetTransform != null && targetTransform != null && _combatRecoveryTargetTransform != targetTransform)
            {
                BeginCombatTargetRecovery(targetTransform, flipRuntimeDirection: false);
            }

            if (IsWithinCombatRecoveryStopDistance(currentPosition, targetPosition))
            {
                EndCombatTargetRecovery();
                return true;
            }

            float deltaX = targetPosition.x - currentPosition.x;
            if (Mathf.Abs(deltaX) <= 0.0001f)
            {
                EndCombatTargetRecovery();
                return true;
            }

            // 플랫포머 이동 정책을 유지하기 위해 X축 방향만 반전한다.
            moveVector = new Vector2(Mathf.Sign(deltaX), 0f);
            return true;
        }

        /// <summary>
        /// 전투 타겟 복귀를 종료하고 설정에 따라 자동 이동 완료를 예약합니다.
        /// </summary>
        private void EndCombatTargetRecovery()
        {
            _isCombatTargetRecovering = false;
            _combatRecoveryTargetTransform = null;
            _combatRecoveryCooldownUntil = Time.time + GetCombatTargetRecoveryCooldownSeconds();

            if (_request != null && _request.stopAutoMoveOnCombatTargetRecovered)
            {
                _pendingCompleteAfterCombatTargetRecovery = true;
            }
        }

        /// <summary>
        /// 자동 이동 상태 변경 시 전투 타겟 복귀 상태를 초기화합니다.
        /// </summary>
        private void ResetCombatTargetRecovery()
        {
            _isCombatTargetRecovering = false;
            _combatRecoveryTargetTransform = null;
            _combatRecoveryCooldownUntil = 0f;
            _pendingCompleteAfterCombatTargetRecovery = false;
        }

        /// <summary>
        /// 전투 타겟 복귀 완료 거리 안에 들어왔는지 확인합니다.
        /// </summary>
        /// <param name="currentPosition">현재 플레이어 위치입니다.</param>
        /// <param name="targetPosition">현재 타겟 위치입니다.</param>
        /// <returns>복귀 완료 거리 이내이면 true를 반환합니다.</returns>
        private bool IsWithinCombatRecoveryStopDistance(Vector2 currentPosition, Vector2 targetPosition)
        {
            float stopDistance = GetCombatTargetRecoveryStopDistance();
            return Mathf.Abs(targetPosition.x - currentPosition.x) <= stopDistance;
        }

        /// <summary>
        /// 전투 타겟 복귀 완료 거리 설정값을 안전한 값으로 보정해 반환합니다.
        /// </summary>
        /// <returns>복귀 완료 X축 거리입니다.</returns>
        private float GetCombatTargetRecoveryStopDistance()
        {
            return Mathf.Max(0.01f, _request != null ? _request.combatTargetRecoveryStopDistance : 0.35f);
        }

        /// <summary>
        /// 전투 타겟 복귀 종료 후 재진입 방지 시간을 안전한 값으로 보정해 반환합니다.
        /// </summary>
        /// <returns>복귀 재진입 방지 시간입니다.</returns>
        private float GetCombatTargetRecoveryCooldownSeconds()
        {
            return Mathf.Max(0f, _request != null ? _request.combatTargetRecoveryCooldownSeconds : 0.2f);
        }

        /// <summary>
        /// 전투 복귀 기준이 될 타겟 Transform을 해석합니다.
        /// </summary>
        /// <returns>유효한 타겟 Transform, 없으면 null입니다.</returns>
        private Transform ResolveCombatRecoveryTargetTransform()
        {
            if (_character is Player player)
            {
                Transform playerTarget = player.GetAutoMoveTargetTransform();
                if (IsValidCombatRecoveryTarget(playerTarget))
                {
                    return playerTarget;
                }
            }

            Transform attackerTarget = _character.attackerTransform;
            if (IsValidCombatRecoveryTarget(attackerTarget))
            {
                return attackerTarget;
            }

            return null;
        }

        /// <summary>
        /// 전투 복귀 기준 타겟으로 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="targetTransform">검증할 타겟 Transform입니다.</param>
        /// <returns>사용 가능한 타겟이면 true를 반환합니다.</returns>
        private bool IsValidCombatRecoveryTarget(Transform targetTransform)
        {
            if (targetTransform == null || targetTransform == _character.transform)
            {
                return false;
            }

            if (!targetTransform.gameObject.activeInHierarchy)
            {
                return false;
            }

            CharacterBase targetCharacter = targetTransform.GetComponent<CharacterBase>();
            if (targetCharacter != null && targetCharacter.IsStatusDead())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 현재 위치가 진행 방향 기준으로 타겟을 지나쳤는지 판정합니다.
        /// </summary>
        /// <param name="currentPosition">현재 플레이어 위치입니다.</param>
        /// <param name="targetPosition">타겟 위치입니다.</param>
        /// <returns>지나침이 감지되면 true를 반환합니다.</returns>
        private bool IsPassedCombatTarget(Vector2 currentPosition, Vector2 targetPosition)
        {
            float epsilon = Mathf.Max(0.001f, _request != null ? _request.combatTargetPassedEpsilon : 0.05f);

            float directionX = GetRuntimeDirectionX();
            if (directionX > 0f)
            {
                return currentPosition.x > targetPosition.x + epsilon;
            }

            if (directionX < 0f)
            {
                return currentPosition.x < targetPosition.x - epsilon;
            }

            return false;
        }

        /// <summary>
        /// 요청에 직접 지정된 타겟 좌표를 해석합니다.
        /// </summary>
        /// <returns>요청 타겟 좌표가 있으면 반환하고, 없으면 null입니다.</returns>
        private Vector2? ResolveRequestTargetPosition()
        {
            if (_request == null) return null;
            if (_request.targetTransform != null)
            {
                return _request.targetTransform.position;
            }
            if (_request.targetPosition.HasValue)
            {
                return _request.targetPosition.Value;
            }
            return null;
        }

        private Vector2? ResolveTargetPosition()
        {
            return ResolveRequestTargetPosition();
        }

        private void TickCompletion()
        {
            if (_request == null || _character == null) return;

            if (_pendingCompleteAfterCombatTargetRecovery)
            {
                Complete();
                return;
            }

            // Pause 상태에서는 완료 조건을 진행하지 않는다.
            if (IsAutoMoveSuspended) return;

            if (_request.moveType == AutoMoveType.Direction)
            {
                if (!_request.infiniteMove)
                {
                    _elapsed += Time.fixedDeltaTime;
                    if (_elapsed >= Mathf.Max(0f, _request.duration))
                    {
                        Complete();
                    }
                }
                return;
            }

            // Target 기반: 거리 체크
            var target = ResolveTargetPosition();
            if (!target.HasValue)
            {
                Cancel();
                return;
            }

            Vector2 current = _character.transform.position;
            float stopDist = Mathf.Max(0.01f, _request.stopDistance);
            if (Vector2.Distance(current, target.Value) <= stopDist)
            {
                Complete();
            }
        }

        /// <summary>
        /// 플레이어 입력을 자동 이동 취소 정책에 전달합니다.
        /// </summary>
        /// <param name="inputType">입력 종류입니다.</param>
        /// <param name="value">입력 벡터 값입니다.</param>
        public void NotifyPlayerInput(AutoMoveInputType inputType, Vector2 value)
        {
            if (!IsAutoMoveActive || _request == null) return;

            switch (_request.cancelPolicy)
            {
                case AutoMoveCancelPolicy.NeverCancel:
                    return;

                case AutoMoveCancelPolicy.MoveInputCancel:
                    if (inputType == AutoMoveInputType.Move && value != Vector2.zero)
                    {
                        Cancel();
                    }
                    return;

                case AutoMoveCancelPolicy.AnyInputCancel:
                default:
                    // 버튼류 입력은 값이 0이어도 들어올 수 있으므로 타입만으로 취소한다.
                    if (inputType == AutoMoveInputType.Move)
                    {
                        if (value != Vector2.zero) Cancel();
                    }
                    else
                    {
                        Cancel();
                    }
                    return;
            }
        }
    }
}
