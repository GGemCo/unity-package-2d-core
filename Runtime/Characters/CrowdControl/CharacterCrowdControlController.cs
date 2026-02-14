using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터에게 CrowdControl(넉백/넉다운 등)을 적용하고,
    /// 상태/애니메이션/물리 이동을 일관되게 처리하는 컨트롤러입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterCrowdControlController : MonoBehaviour
    {
        private CharacterBase _character;
        private Rigidbody2D _rigidbody2D;

        private bool _isRunning;
        private float _remainingTime;

        // 이동 보간용(거리 기반)
        private Vector2 _startPos;
        private Vector2 _endPos;
        private float _elapsed;
        private float _duration;
        private Easing.EaseType _easeType;

        private void Awake()
        {
            _character = GetComponent<CharacterBase>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        /// <summary>
        /// CrowdControl 테이블 정의를 기반으로 CrowdControl을 적용합니다.
        /// </summary>
        public void ApplyCrowdControl(StruckTableCrowdControl crowdControl, GameObject source)
        {
            if (crowdControl == null) return;
            if (_character == null) return;

            // 적용 조건(선택)
            // GroundOnly/AirOnly 판단은 프로젝트의 "지상/공중" 판정 시스템에 따라 달라질 수 있으므로
            // 현재는 옵션으로 남겨두고, 필요 시 CharacterBase의 점프/중력/바닥 감지와 연계해 확장합니다.

            // 방향 결정
            var direction = ResolveDirection(crowdControl, source);

            // 상태/제어
            if (crowdControl.IsUseKnockbackStatus)
                _character.SetStatusKnockback();

            if (crowdControl.IsUseDontControlStatus)
                _character.SetStatusDontControl();

            // 애니메이션(경직)
            PlayStaggerAnimation(crowdControl);

            // 이동 정책
            // - Strength(힘/속도) 기반이 아니라, Duration 동안 "총 이동 거리"를 Easing으로 보간합니다.
            _duration = Mathf.Max(0f, crowdControl.Duration);
            _remainingTime = _duration;
            _elapsed = 0f;
            _easeType = crowdControl.EaseType;

            var currentPos = _rigidbody2D != null ? _rigidbody2D.position : (Vector2)transform.position;
            _startPos = currentPos;
            _endPos = currentPos + (direction * crowdControl.Distance);

            if (_duration <= 0f || Mathf.Abs(crowdControl.Distance) <= 0.0001f)
            {
                // 즉시 이동(옵션)
                MoveTo(_endPos);
                _isRunning = false;
                return;
            }

            _isRunning = true;
        }

        private void FixedUpdate()
        {
            if (!_isRunning) return;

            _elapsed += Time.fixedDeltaTime;
            _remainingTime = _duration - _elapsed;
            if (_remainingTime <= 0f)
            {
                MoveTo(_endPos);
                _isRunning = false;
                return;
            }

            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float easedT = Mathf.Clamp01(Easing.Apply(t, _easeType));
            var nextPos = Vector2.LerpUnclamped(_startPos, _endPos, easedT);
            MoveTo(nextPos);
        }

        private void MoveTo(Vector2 position)
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.MovePosition(position);
            }
            else
            {
                transform.position = position;
            }
        }

        private Vector2 ResolveDirection(StruckTableCrowdControl crowdControl, GameObject source)
        {
            switch (crowdControl.DirectionType)
            {
                case CrowdControlConstants.DirectionType.FromSourceToTarget:
                {
                    if (source != null)
                    {
                        var a = source.transform.position;
                        var b = transform.position;
                        var dir = (b - a);
                        if (dir.sqrMagnitude > 0.0001f)
                            return ((Vector2)dir).normalized;
                    }
                    // fallback: target facing
                    return ResolveFacingDirection();
                }

                case CrowdControlConstants.DirectionType.FromTargetFacing:
                    return ResolveFacingDirection();

                case CrowdControlConstants.DirectionType.Fixed:
                {
                    var v = new Vector2(crowdControl.FixedDirectionX, crowdControl.FixedDirectionY);
                    if (v.sqrMagnitude > 0.0001f) return v.normalized;
                    return ResolveFacingDirection();
                }

                default:
                    return ResolveFacingDirection();
            }
        }

        private Vector2 ResolveFacingDirection()
        {
            if (_character == null) return Vector2.right;

            // Left: x 음수, Right: x 양수
            return _character.CurrentFacing == CharacterConstants.FacingDirection8.Left ? Vector2.left : Vector2.right;
        }

        private void PlayStaggerAnimation(StruckTableCrowdControl crowdControl)
        {
            if (_character?.CharacterAnimationController == null) return;

            switch (crowdControl.StaggerAnimationType)
            {
                case CrowdControlConstants.StaggerAnimationType.Damage:
                    _character.CharacterAnimationController.PlayDamageAnimation();
                    break;

                case CrowdControlConstants.StaggerAnimationType.Groggy:
                    _character.CharacterAnimationController.PlayAnimationGroggy();
                    break;

                default:
                    break;
            }
        }

        public void ApplyCrowdControlByUid(int crowdControlUid, GameObject source)
        {
            var info = TableLoaderManager.Instance.TableCrowdControl.GetDataByUid(crowdControlUid);
            if (info == null) return;
            ApplyCrowdControl(info, source);
        }
    }
}
