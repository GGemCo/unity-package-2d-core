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
    public sealed class PlayerAutoMoveController : MonoBehaviour, IAutoMoveVectorProvider
    {
        public bool IsAutoMoveActive => _isActive;
        public bool IsInputLocked => _isActive && _lockInput;

        private CharacterBase _character;
        private CharacterBaseController _controller;
#if ENABLE_INPUT_SYSTEM
        private UnityEngine.InputSystem.PlayerInput _playerInput;
#endif

        private AutoMoveRequest _request;
        private bool _isActive;
        private bool _lockInput;
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
