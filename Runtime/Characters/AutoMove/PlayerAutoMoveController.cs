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
        public bool IsAutoMoveActive => _isActive;
        public bool IsInputLocked => _isActive && _lockInput;

        public bool IsAutoMoveSuspended => _suspendCount > 0;

        private CharacterBase _character;
        private CharacterBaseController _controller;
#if ENABLE_INPUT_SYSTEM
        private UnityEngine.InputSystem.PlayerInput _playerInput;
#endif

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
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
#endif
            _originalMoveStep = _character != null ? _character.currentMoveStep : 0f;

            // Suspend 토큰은 런타임에만 사용되며, 일반적으로 동시에 1~2개(컷씬/벽액션) 수준이므로
            // 작은 고정 배열로 관리합니다(추가 필요 시 자동 확장).
            _nextSuspendId = 1;
            _suspendCount = 0;
            _suspendTokens = new AutoMoveSuspendToken[4];
        }

        private void FixedUpdate()
        {
            if (!_isActive) return;

            // 전역 설정이 꺼져 있으면 즉시 중단
            var settings = AddressableLoaderSettings.Instance ? AddressableLoaderSettings.Instance.settings : null;
            if (settings != null && !settings.enableAutoMove)
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
            bool isDrivenByControl = false;
#if ENABLE_INPUT_SYSTEM
            isDrivenByControl = _playerInput != null;
#endif

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
        /// 자동 이동을 시작합니다.
        /// </summary>
        public void StartAutoMove(AutoMoveRequest request, bool lockInput = true)
        {
            if (request == null)
            {
                GcLogger.LogWarning($"{nameof(PlayerAutoMoveController)} StartAutoMove failed: request is null");
                return;
            }

            var settings = AddressableLoaderSettings.Instance ? AddressableLoaderSettings.Instance.settings : null;
            if (settings != null && !settings.enableAutoMove)
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


        public bool ShouldBlockInput(AutoMoveInputType inputType)
        {
            if (!_isActive || !_lockInput) return false;

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

        public Vector2 GetMoveVector()
        {
            if (!_isActive || _request == null || _character == null) return Vector2.zero;

            // Pause 상태에서는 이동 벡터를 제공하지 않는다.
            if (IsAutoMoveSuspended) return Vector2.zero;

            switch (_request.moveType)
            {
                case AutoMoveType.Direction:
                {
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

        private Vector2? ResolveTargetPosition()
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

        public void NotifyPlayerInput(AutoMoveInputType inputType, Vector2 value)
        {
            if (!_isActive || _request == null) return;

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
