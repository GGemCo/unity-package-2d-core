using System;
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

        public void Cancel()
        {
            if (!_isActive) return;
            _isActive = false;
            _lockInput = false;

            RestoreMoveStep();

            if (_character != null)
            {
                _character.Stop();
            }
        }

        private void Complete()
        {
            if (!_isActive) return;
            _isActive = false;
            _lockInput = false;

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
        /// 현재 자동 이동 요청과 맵 정책을 기준으로 이동 벡터를 계산합니다.
        /// </summary>
        /// <returns>자동 이동에 사용할 정규화된 이동 벡터입니다.</returns>
        public Vector2 GetMoveVector()
        {
            if (!IsAutoMoveActive || _request == null || _character == null) return Vector2.zero;

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

                    float dirX = (float)_request.direction;
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
        /// <returns>복귀 이동을 적용해야 하면 true를 반환합니다.</returns>
        private bool TryResolveCombatRecoveryMoveVector(out Vector2 moveVector)
        {
            moveVector = Vector2.zero;

            if (!CanUseCombatTargetRecovery())
            {
                return false;
            }

            if (!TryResolveCombatRecoveryTargetPosition(out Vector2 targetPosition))
            {
                return false;
            }

            Vector2 currentPosition = _character.transform.position;
            if (!IsPassedCombatTarget(currentPosition, targetPosition))
            {
                return false;
            }

            float deltaX = targetPosition.x - currentPosition.x;
            if (Mathf.Abs(deltaX) <= 0.0001f)
            {
                return false;
            }

            // 플랫포머 이동 정책을 유지하기 위해 X축 방향만 반전한다.
            moveVector = new Vector2(Mathf.Sign(deltaX), 0f);
            return true;
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
        /// 전투 복귀 판정에 사용할 타겟 월드 좌표를 계산합니다.
        /// </summary>
        /// <param name="targetPosition">복귀 기준 타겟 좌표입니다.</param>
        /// <returns>유효한 타겟을 찾으면 true를 반환합니다.</returns>
        private bool TryResolveCombatRecoveryTargetPosition(out Vector2 targetPosition)
        {
            targetPosition = Vector2.zero;

            Vector2? requestTargetPosition = ResolveRequestTargetPosition();
            if (requestTargetPosition.HasValue)
            {
                targetPosition = requestTargetPosition.Value;
                return true;
            }

            Transform targetTransform = ResolveCombatRecoveryTargetTransform();
            if (targetTransform == null)
            {
                return false;
            }

            targetPosition = targetTransform.position;
            return true;
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

            switch (_request.direction)
            {
                case AutoMoveDirection.Right:
                    return currentPosition.x > targetPosition.x + epsilon;

                case AutoMoveDirection.Left:
                    return currentPosition.x < targetPosition.x - epsilon;

                default:
                    return false;
            }
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
