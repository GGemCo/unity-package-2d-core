using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터에게 CrowdControl(넉백/넉다운/넉업 등)을 적용하고,
    /// 상태/애니메이션/물리 이동을 일관되게 처리하는 컨트롤러입니다.
    /// </summary>
    /// <remarks>
    /// 이동은 가능하면 <see cref="ICharacterMotionController"/>에 위임하며,
    /// 모션 컨트롤러가 없거나 모션 시작에 실패하면 위치 스냅 방식으로 대체 처리합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterCrowdControlController : MonoBehaviour
    {
        private CharacterBase _character;
        private Rigidbody2D _rigidbody2D;
        private ICharacterMotionController _motionController;

        // 현재 적용된 CC(한 번에 1개 정책: 새 CC가 오면 기존을 강제 중단)
        private StruckTableCrowdControl _activeCrowdControl;

        // 애니메이션 시퀀스(이름 기반)
        private string _currentStaggerAnimationName;

        private Coroutine _stopRoutine;
        private const float Epsilon = 0.0001f;
        
        private void Awake()
        {
            _character = GetComponent<CharacterBase>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            _motionController = GetComponent<ICharacterMotionController>();
        }

        /// <summary>
        /// CrowdControl 테이블 정의를 기반으로 CrowdControl을 적용합니다.
        /// </summary>
        /// <param name="crowdControl">적용할 CC(넉백/넉다운/넉업 등) 데이터입니다.</param>
        /// <param name="source">방향 계산에 사용할 공격/발생 원본 오브젝트입니다(없을 수 있음).</param>
        /// <remarks>
        /// - 기존 CC가 진행 중이면 즉시 중단 후 새 CC로 교체합니다. <br/>
        /// - Duration/DownWaitTime이 0이면 즉시 이동(거리 존재 시) 후 종료 처리합니다. <br/>
        /// - 이동은 <see cref="MotionChannel.CrowdControl"/> 채널로 요청되며, 종료 시 End 애니메이션 및 상태 해제를 수행합니다.
        /// </remarks>
        public void ApplyCrowdControl(StruckTableCrowdControl crowdControl, GameObject source)
        {
            if (crowdControl == null) return;
            if (_character == null) return;

            // 기존 CC가 진행 중이면 중단(강제)
            ForceStopInternal();

            _activeCrowdControl = crowdControl;

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

            // 이동/대기 여부 판단
            bool hasAnyTime =
                Mathf.Abs(crowdControl.Duration) > Epsilon ||
                Mathf.Abs(crowdControl.DownWaitTime) > Epsilon;

            bool hasDistance = Mathf.Abs(crowdControl.Distance) > Epsilon;

            // 즉시 스냅 + 종료 시퀀스만 원하는 경우
            if (!hasAnyTime)
            {
                if (hasDistance)
                    MoveTo(endPos);

                // End 애니메이션이 없으면 즉시 상태 해제
                PlayEndAndStop(crowdControl);
                _activeCrowdControl = null;
                return;
            }

            // 모션 요청 생성(실제 이동은 CharacterMotionController2D로 위임)
            if (_motionController == null)
            {
                // 모션 컨트롤러가 없으면 즉시 종료 처리
                PlayEndAndStop(crowdControl);
                _activeCrowdControl = null;
                return;
            }

            float duration = Mathf.Max(0f, crowdControl.Duration);

            MotionKind kind = MotionKind.Linear;
            float arcHeight = 0f;
            MotionArcMode arcMode = MotionArcMode.LegacyTimeSine;
            float holdAfter = 0f;

            switch (crowdControl.Type)
            {
                case CrowdControlConstants.Type.KnockUp:
                    kind = MotionKind.Arc;
                    arcHeight = Mathf.Max(0f, crowdControl.Height);
                    arcMode = MotionArcMode.DistancePhased;
                    break;

                case CrowdControlConstants.Type.KnockDown:
                    holdAfter = Mathf.Max(0f, crowdControl.DownWaitTime);
                    break;

                case CrowdControlConstants.Type.KnockBack:
                default:
                    break;
            }

            var req = new MotionRequest(
                MotionChannel.CrowdControl,
                kind,
                direction,
                duration,
                Mathf.Max(0f, crowdControl.Distance),
                crowdControl.EaseType,
                stopAtEnd: true,
                useMovePosition: true,
                allowReplace: true,
                holdSecondsAfter: holdAfter,
                arcHeight: arcHeight,
                arcMode: arcMode,
                arcRiseEaseType: crowdControl.EaseType,
                arcFallEaseType: crowdControl.EaseType,
                arcApexHoldNormalized: 0f);

            if (!_motionController.TryStartMotion(in req))
            {
                PlayEndAndStop(crowdControl);
                _activeCrowdControl = null;
            }
        }

        private void Update()
        {
            if (_activeCrowdControl == null) return;
            if (_motionController == null)
            {
                _activeCrowdControl = null;
                return;
            }

            // CC 채널 모션이 끝나면 종료 시퀀스
            if (!_motionController.IsPlaying(MotionChannel.CrowdControl))
            {
                var finished = _activeCrowdControl;
                _activeCrowdControl = null;
                PlayEndAndStop(finished);
            }
        }

        /// <summary>
        /// 지정한 위치로 캐릭터를 이동(스냅)시킵니다.
        /// </summary>
        /// <param name="position">이동할 월드 좌표입니다.</param>
        /// <remarks>
        /// <see cref="Rigidbody2D"/>가 존재하면 물리 이동(<see cref="Rigidbody2D.MovePosition"/>)을 사용하고,
        /// 없으면 <see cref="Transform.position"/>을 직접 변경합니다.
        /// </remarks>
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

        /// <summary>
        /// CC 데이터와 소스 오브젝트를 바탕으로 이동 방향(단위 벡터)을 계산합니다.
        /// </summary>
        /// <param name="crowdControl">방향 타입 및 고정 방향 값을 포함한 CC 데이터입니다.</param>
        /// <param name="source">FromSourceToTarget 계산에 사용할 원본 오브젝트입니다(없을 수 있음).</param>
        /// <returns>계산된 이동 방향(정규화된 벡터)입니다.</returns>
        /// <remarks>
        /// 유효한 방향을 얻지 못하면 캐릭터의 현재 바라보는 방향을 fallback으로 사용합니다.
        /// </remarks>
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
                        if (dir.sqrMagnitude > Epsilon)
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
                    if (v.sqrMagnitude > Epsilon) return v.normalized;
                    return ResolveFacingDirection();
                }

                default:
                    return ResolveFacingDirection();
            }
        }

        /// <summary>
        /// 캐릭터의 현재 바라보는 방향을 수평 방향(Vector2.left/right)으로 변환합니다.
        /// </summary>
        /// <returns>왼쪽 또는 오른쪽을 나타내는 방향 벡터입니다.</returns>
        private Vector2 ResolveFacingDirection()
        {
            if (_character == null) return Vector2.right;

            // Left: x 음수, Right: x 양수
            return _character.CurrentFacing == CharacterConstants.FacingDirection8.Left ? Vector2.left : Vector2.right;
        }

        /// <summary>
        /// CC 시작 시 재생할 경직(Stagger) 애니메이션을 재생합니다.
        /// </summary>
        /// <param name="crowdControl">경직 애니메이션 이름을 포함한 CC 데이터입니다.</param>
        /// <remarks>
        /// 테이블의 애니메이션 이름이 비어있으면 아무 것도 재생하지 않으며,
        /// Start → Wait 전환은 Animator의 Transition으로 구성되어 있다고 가정합니다.
        /// </remarks>
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

        /// <summary>
        /// CC 종료 애니메이션(있다면)을 재생하고, 최종적으로 CC 상태를 정리합니다.
        /// </summary>
        /// <param name="crowdControl">RecoverTime 등 종료 대기 시간 보정에 사용할 CC 데이터입니다(없을 수 있음).</param>
        /// <remarks>
        /// End 애니메이션이 존재하면 클립 길이(또는 RecoverTime 중 더 큰 값)만큼 대기한 뒤
        /// <c>Stop(isForce: true)</c>로 상태를 강제 해제합니다.
        /// </remarks>
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

        /// <summary>
        /// 지정된 시간만큼 대기한 뒤 CC 상태를 강제 해제합니다.
        /// </summary>
        /// <param name="durationSec">대기할 시간(초)입니다.</param>
        /// <returns>코루틴 열거자입니다.</returns>
        private IEnumerator StopAfter(float durationSec)
        {
            if (durationSec > 0f)
                yield return new WaitForSeconds(durationSec);

            _character?.Stop(isForce: true);
            _currentStaggerAnimationName = null;
            _stopRoutine = null;
        }

        /// <summary>
        /// 진행 중인 CC를 즉시 중단하고, 모션/코루틴/상태를 강제 정리합니다.
        /// </summary>
        /// <remarks>
        /// 외부에 공개하지 않고, 새 CC 적용 전에 내부적으로 교체(replace) 처리할 때 사용합니다.
        /// </remarks>
        private void ForceStopInternal()
        {
            if (_stopRoutine != null)
            {
                StopCoroutine(_stopRoutine);
                _stopRoutine = null;
            }

            // 진행 중인 CC 모션을 강제 취소
            _motionController?.CancelMotion(MotionChannel.CrowdControl, reason: 200);

            // 진행 중인 CC를 강제 해제
            _character?.Stop(isForce: true);
            _currentStaggerAnimationName = null;
            _activeCrowdControl = null;
        }

        /// <summary>
        /// CC UID를 통해 테이블에서 데이터를 조회한 뒤 CrowdControl을 적용합니다.
        /// </summary>
        /// <param name="crowdControlUid">조회할 CC 테이블 UID입니다.</param>
        /// <param name="source">방향 계산에 사용할 공격/발생 원본 오브젝트입니다(없을 수 있음).</param>
        public void ApplyCrowdControlByUid(int crowdControlUid, GameObject source)
        {
            var info = TableLoaderManager.Instance.TableCrowdControl.GetDataByUid(crowdControlUid);
            if (info == null) return;
            ApplyCrowdControl(info, source);
        }
    }
}