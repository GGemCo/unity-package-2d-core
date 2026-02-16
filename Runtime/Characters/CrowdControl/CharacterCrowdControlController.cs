using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터에게 CrowdControl(넉백/넉다운/넉업 등)을 적용하고,
    /// 상태/애니메이션/물리 이동을 일관되게 처리하는 컨트롤러입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterCrowdControlController : MonoBehaviour
    {
        private CharacterBase _character;
        private Rigidbody2D _rigidbody2D;

        private ICrowdControlMotion _motion;

        // 애니메이션 시퀀스(이름 기반)
        private string _currentStaggerAnimationName;

        private Coroutine _stopRoutine;

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

            // 기존 CC가 진행 중이면 중단(강제)
            ForceStopInternal();

            // 방향 결정
            var direction = ResolveDirection(crowdControl, source);

            // 상태/제어
            if (crowdControl.IsUseKnockbackStatus)
                _character.SetStatusKnockback();

            if (crowdControl.IsUseDontControlStatus)
                _character.SetStatusDontControl();

            // 애니메이션(경직)
            PlayStaggerAnimation(crowdControl);

            // 시작/종료 위치 계산(기본: 수평 이동)
            var currentPos = _rigidbody2D != null ? _rigidbody2D.position : (Vector2)transform.position;
            var startPos = currentPos;
            var endPos = currentPos + (direction * crowdControl.Distance);

            // 모션 전략 선택
            _motion = CreateMotion(crowdControl, startPos, endPos);

            // 즉시 종료 케이스(모션이 null이거나, Tick 없이 끝난 경우)
            if (_motion == null)
            {
                // End 애니메이션이 없으면 즉시 상태 해제
                PlayEndAndStop(crowdControl);
            }
        }

        private void FixedUpdate()
        {
            if (_motion == null) return;

            if (_motion.Tick(Time.fixedDeltaTime, out var nextPos))
            {
                MoveTo(nextPos);
            }

            if (_motion.IsFinished)
            {
                _motion = null;
                // 이동 완료 후 End 애니메이션 -> 종료(상태 해제)
                PlayEndAndStop();
            }
        }

        private ICrowdControlMotion CreateMotion(StruckTableCrowdControl row, Vector2 startPos, Vector2 endPos)
        {
            // Duration이 0이고, Knockdown의 DownWaitTime도 0이면 이동/대기 자체가 없으므로 null 처리
            bool hasAnyTime =
                Mathf.Abs(row.Duration) > 0.0001f ||
                Mathf.Abs(row.DownWaitTime) > 0.0001f;

            bool hasDistance = Mathf.Abs(row.Distance) > 0.0001f;

            // 즉시 이동 + 종료만 원하는 경우(거리만 있고 duration 0)도 있을 수 있으므로,
            // duration=0이면 EndPos로 스냅 후 종료 시퀀스로 간다.
            if (!hasAnyTime)
            {
                if (hasDistance)
                    MoveTo(endPos);
                return null;
            }

            float duration = Mathf.Max(0f, row.Duration);

            switch (row.Type)
            {
                case CrowdControlConstants.Type.KnockUp:
                    return new KnockUpMotion(startPos, endPos, duration, row.EaseType, row.Height);

                case CrowdControlConstants.Type.KnockDown:
                    return new KnockDownMotion(startPos, endPos, duration, row.EaseType, row.DownWaitTime);

                case CrowdControlConstants.Type.KnockBack:
                default:
                    return new KnockBackMotion(startPos, endPos, duration, row.EaseType);
            }
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

            // 테이블이 비어있으면 아무 것도 재생하지 않습니다.
            // Start → Wait 전환은 Animator의 Transition으로 구성하는 전제입니다.
            _currentStaggerAnimationName = crowdControl?.StaggerAnimationName;
            if (string.IsNullOrWhiteSpace(_currentStaggerAnimationName)) return;

            if (_character.CharacterAnimationController.HasAnimation(_currentStaggerAnimationName))
            {
                _character.CharacterAnimationController.PlayCharacterAnimation(_currentStaggerAnimationName, loop: false);
            }
        }

        private void PlayEndAndStop(StruckTableCrowdControl crowdControl = null)
        {
            if (_stopRoutine != null)
            {
                StopCoroutine(_stopRoutine);
                _stopRoutine = null;
            }

            // End 애니메이션이 있으면, 클립 길이만큼 대기 후 Stop(true)로 상태를 강제 해제합니다.
            // (CharacterBase.Stop은 Knockback 상태일 때 기본적으로 return 하므로, CC 종료에는 강제가 필요합니다.)
            string endName = null;

            if (_character?.CharacterAnimationController != null && !string.IsNullOrWhiteSpace(_currentStaggerAnimationName))
            {
                endName = _currentStaggerAnimationName + StruckTableCrowdControl.StaggerAnimationEndSuffix;
                if (_character.CharacterAnimationController.HasAnimation(endName))
                {
                    _character.CharacterAnimationController.PlayCharacterAnimation(endName, loop: false);

                    float durationSec = _character.CharacterAnimationController.GetCharacterAnimationDuration(endName, isMilliseconds: false);
                    durationSec = Mathf.Max(0f, durationSec);

                    // RecoverTime을 사용하는 경우(선택): 데이터 시간이 더 길면 그 시간을 우선
                    if (crowdControl != null && crowdControl.RecoverTime > durationSec)
                        durationSec = crowdControl.RecoverTime;

                    _stopRoutine = StartCoroutine(StopAfter(durationSec));
                    return;
                }
            }

            // End 애니메이션이 없으면 즉시 정리
            _character?.Stop(isForce: true);
            _currentStaggerAnimationName = null;
        }

        private IEnumerator StopAfter(float durationSec)
        {
            if (durationSec > 0f)
                yield return new WaitForSeconds(durationSec);

            _character?.Stop(isForce: true);
            _currentStaggerAnimationName = null;
            _stopRoutine = null;
        }

        private void ForceStopInternal()
        {
            _motion = null;

            if (_stopRoutine != null)
            {
                StopCoroutine(_stopRoutine);
                _stopRoutine = null;
            }

            // 진행 중인 CC를 강제 해제
            _character?.Stop(isForce: true);
            _currentStaggerAnimationName = null;
        }

        public void ApplyCrowdControlByUid(int crowdControlUid, GameObject source)
        {
            var info = TableLoaderManager.Instance.TableCrowdControl.GetDataByUid(crowdControlUid);
            if (info == null) return;
            ApplyCrowdControl(info, source);
        }
    }
}
