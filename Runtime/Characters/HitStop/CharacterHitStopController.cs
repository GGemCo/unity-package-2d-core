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

                if (request.LockControl)
                {
                    _character.SetStatusDontControl();
                }
                else if (request.LockMovement)
                {
                    _character.SetStatusDontMove();
                }
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

        private void EndInternal()
        {
            if (_savedAnimationSpeedValid && _animationController != null)
            {
                _animationController.SetPlaybackTimeScale(_savedAnimationSpeed);
            }

            if (_savedPhysicsValid && _rigidbody2D != null)
            {
                _rigidbody2D.constraints = _savedConstraints;
                _rigidbody2D.SetLinearVelocity(_savedVelocity);
                _rigidbody2D.angularVelocity = _savedAngularVelocity;
            }

            if (_savedStatusValid && _character != null)
            {
                RestoreStatus(_savedStatus);
            }

            _remainingSeconds = 0f;
            _isActive = false;
            _savedStatusValid = false;
            _savedAnimationSpeedValid = false;
            _savedPhysicsValid = false;
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
                case CharacterConstants.CharacterStatus.DontControl:
                    _character.SetStatusDontControl();
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
                case CharacterConstants.CharacterStatus.Knockback:
                    _character.SetStatusKnockback();
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
