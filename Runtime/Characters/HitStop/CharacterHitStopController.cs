using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 단위 경직(Hit Stop)을 관리합니다.
    /// 애니메이션, 상태, 물리를 잠시 멈추고 시간이 지나면 원래 상태를 복원합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHitStopController : MonoBehaviour
    {
        private CharacterBase _character;
        private Rigidbody2D _rigidbody2D;
        private ICharacterAnimationController _animationController;

        private float _remainingSeconds;
        private bool _isActive;

        private CharacterConstants.CharacterStatus _savedStatus;
        private bool _savedStatusValid;

        private float _savedAnimationSpeed;
        private bool _savedAnimationSpeedValid;

        private Vector2 _savedVelocity;
        private float _savedAngularVelocity;
        private RigidbodyConstraints2D _savedConstraints;
        private bool _savedPhysicsValid;

        public bool IsActive => _isActive;
        public float RemainingSeconds => _remainingSeconds;

        private void Awake()
        {
            _character = GetComponent<CharacterBase>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _animationController = _character != null ? _character.CharacterAnimationController : null;
        }

        private void OnDisable()
        {
            if (_isActive)
            {
                EndInternal();
            }
        }

        private void Update()
        {
            if (!_isActive)
                return;

            _remainingSeconds -= Time.unscaledDeltaTime;
            if (_remainingSeconds > 0f)
                return;

            EndInternal();
        }

        public void Apply(in HitStopRequest request)
        {
            if (_character == null || _character.IsStatusDead())
                return;

            if (_animationController == null && _character != null)
            {
                _animationController = _character.CharacterAnimationController;
            }

            if (request.DurationSeconds <= 0f)
                return;

            if (!_isActive)
            {
                BeginInternal(request);
            }

            _remainingSeconds = Mathf.Max(_remainingSeconds, request.DurationSeconds);
        }

        private void BeginInternal(in HitStopRequest request)
        {
            _isActive = true;

            if (_character != null)
            {
                _savedStatus = _character.GetCurrentStatus();
                _savedStatusValid = true;
            }

            if (request.PauseAnimation && _animationController != null)
            {
                _savedAnimationSpeed = _animationController.GetPlaybackTimeScale();
                _savedAnimationSpeedValid = true;
                _animationController.SetPlaybackTimeScale(0f);
            }

            if (request.FreezePhysics && _rigidbody2D != null)
            {
                _savedVelocity = _rigidbody2D.GetLinearVelocity();
                _savedAngularVelocity = _rigidbody2D.angularVelocity;
                _savedConstraints = _rigidbody2D.constraints;
                _savedPhysicsValid = true;

                _rigidbody2D.SetLinearVelocity(Vector2.zero);
                _rigidbody2D.angularVelocity = 0f;
                _rigidbody2D.constraints = RigidbodyConstraints2D.FreezeAll;
            }
        }

        /// <summary>
        /// 사망 진입 전에 활성 Hit Stop을 종료하고, 사망 이후 적용되면 안 되는 이전 상태와 이동 정보를 폐기합니다.
        /// </summary>
        /// <remarks>
        /// 애니메이션 재생 속도와 Rigidbody 제약은 Hit Stop 적용 전 값으로 되돌리되,
        /// 이전 캐릭터 상태와 속도는 복원하지 않습니다. 이를 통해 사망 애니메이션이 정지된 재생 속도로 시작하거나
        /// Hit Stop 종료 시 Dead 상태가 Idle 등의 과거 상태로 덮어써지는 문제를 방지합니다.
        /// </remarks>
        public void TerminateForDeath()
        {
            if (!_isActive)
                return;

            EndInternal(restorePreviousState: false);
        }

        /// <summary>
        /// 강제 스킬이나 외부 행동 전환 전에 활성 Hit Stop을 즉시 종료합니다.
        /// </summary>
        /// <remarks>
        /// 애니메이션 재생 속도와 Rigidbody 제약은 복원하지만, 강제 행동을 덮어쓰지 않도록
        /// Hit Stop 이전의 캐릭터 상태와 이동 속도는 복원하지 않습니다.
        /// </remarks>
        public void TerminateForExternalActionOverride()
        {
            if (!_isActive)
                return;

            EndInternal(restorePreviousState: false);
        }

        /// <summary>
        /// 활성 Hit Stop을 종료하고 일시 정지했던 애니메이션과 물리 상태를 정리합니다.
        /// </summary>
        /// <param name="restorePreviousState">
        /// <see langword="true"/>이면 Hit Stop 시작 전 캐릭터 상태와 이동 속도를 복원하고,
        /// <see langword="false"/>이면 사망 전환을 위해 해당 정보를 폐기합니다.
        /// </param>
        private void EndInternal(bool restorePreviousState = true)
        {
            bool canRestorePreviousState = CanRestorePreviousState(restorePreviousState);

            if (_savedAnimationSpeedValid && _animationController != null)
            {
                _animationController.SetPlaybackTimeScale(_savedAnimationSpeed);
            }

            if (_savedPhysicsValid && _rigidbody2D != null)
            {
                _rigidbody2D.constraints = _savedConstraints;
                if (canRestorePreviousState)
                {
                    _rigidbody2D.SetLinearVelocity(_savedVelocity);
                    _rigidbody2D.angularVelocity = _savedAngularVelocity;
                }
                else
                {
                    // 사망 전환 중에는 Hit Stop 이전의 이동량이 다시 적용되어 시체가 튀거나 이동하지 않도록 정지합니다.
                    _rigidbody2D.SetLinearVelocity(Vector2.zero);
                    _rigidbody2D.angularVelocity = 0f;
                }
            }

            if (_savedStatusValid && canRestorePreviousState)
            {
                RestoreStatus(_savedStatus);
            }

            _remainingSeconds = 0f;
            _isActive = false;
            _savedStatusValid = false;
            _savedAnimationSpeedValid = false;
            _savedPhysicsValid = false;
        }

        /// <summary>
        /// Hit Stop 시작 이후 캐릭터 상태가 외부 행동으로 변경되지 않았는지 확인하여
        /// 저장된 상태와 물리 속도를 복원해도 안전한지 판단합니다.
        /// </summary>
        /// <param name="restorePreviousState">호출자가 이전 상태 복원을 요청했는지 여부입니다.</param>
        /// <returns>Hit Stop 시작 당시 상태와 물리 속도를 복원해도 안전하면 <see langword="true"/>입니다.</returns>
        private bool CanRestorePreviousState(bool restorePreviousState)
        {
            if (!restorePreviousState ||
                _character == null ||
                _character.IsStatusDead() ||
                _character.IsDeathPending)
            {
                return false;
            }

            if (!_savedStatusValid)
            {
                return true;
            }

            // Hit Stop 도중 자동 이동이나 강제 행동이 새 상태를 설정했다면,
            // 오래된 상태와 속도를 복원하여 최신 행동을 덮어쓰지 않습니다.
            return _character.GetCurrentStatus() == _savedStatus;
        }

        private void RestoreStatus(CharacterConstants.CharacterStatus status)
        {
            switch (status)
            {
                case CharacterConstants.CharacterStatus.None:
                    _character.SetStatusNone();
                    break;
                case CharacterConstants.CharacterStatus.Idle:
                    _character.SetStatusIdle();
                    break;
                case CharacterConstants.CharacterStatus.Run:
                    _character.SetStatusRun();
                    break;
                case CharacterConstants.CharacterStatus.Attack:
                    _character.SetStatusAttack();
                    break;
                case CharacterConstants.CharacterStatus.AttackComboWait:
                    _character.SetStatusAttackComboWait();
                    break;
                case CharacterConstants.CharacterStatus.Dead:
                    _character.SetStatusDead();
                    break;
                case CharacterConstants.CharacterStatus.DontMove:
                    _character.SetStatusDontMove();
                    break;
                case CharacterConstants.CharacterStatus.CastingSkill:
                    _character.SetStatusCastingSkill();
                    break;
                case CharacterConstants.CharacterStatus.UseSkill:
                    _character.SetStatusUseSkill();
                    break;
                case CharacterConstants.CharacterStatus.MoveForce:
                    _character.SetStatusMoveForce();
                    break;
                case CharacterConstants.CharacterStatus.Damage:
                    _character.SetStatusDamage();
                    break;
                case CharacterConstants.CharacterStatus.Jump:
                    _character.SetStatusJump();
                    break;
                case CharacterConstants.CharacterStatus.Dash:
                    _character.SetStatusDash();
                    break;
                case CharacterConstants.CharacterStatus.Climb:
                    _character.SetStatusClimb();
                    break;
                case CharacterConstants.CharacterStatus.Push:
                    _character.SetStatusPush();
                    break;
                case CharacterConstants.CharacterStatus.SimulationTool:
                    _character.SetStatusSimulationTool();
                    break;
                default:
                    _character.SetStatusNone();
                    break;
            }
        }
    }
}
