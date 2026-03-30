using System.Collections;
using System.Collections.Generic;
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
        private CrowdControlRuntimeData _activeCrowdControl;

        private Dictionary<CrowdControlConstants.Type, ICrowdControlHandler> _handlers;

        private readonly Queue<QueuedCrowdControl> _queuedCrowdControls = new();
        private bool _isSequenceRunning;
        private GameObject _sequenceSource;
        private GameObject _sequenceTarget;

        // 애니메이션 시퀀스(이름 기반)
        private string _currentStaggerAnimationName;
        private string _currentPhaseAnimationName;
        private KnockUpAnimationPhase _currentKnockUpAnimationPhase;

        private Coroutine _stopRoutine;
        private const float Epsilon = 0.0001f;
        private const float GroundProbeDefaultHeight = 2f;
        private const float GroundProbeDefaultDistance = 12f;
        /// <summary>
        /// Ground probe 시작 위치를 캐릭터 하단보다 약간 위로 올리기 위한 오프셋.
        /// 지면과 거의 붙어있는 상태에서 Raycast가 시작 지점에서 바로 히트되는 것을 방지하고,
        /// 안정적인 착지 판정을 위해 사용됩니다.
        /// </summary>
        private const float KnockUpLandingProbeUpOffset = 0.1f;

        /// <summary>
        /// FallLoop 단계에서 지면과의 거리가 이 값 이하가 되면,
        /// 아직 Arc 모션이 끝나지 않았더라도 강제로 착지(LandEnd) 단계로 전환하기 위한 임계 거리입니다.
        /// 너무 크면 공중에서 조기 종료되고, 너무 작으면 여전히 부자연스러운 끊김이 발생할 수 있습니다.
        /// </summary>
        private const float KnockUpLandingTriggerDistance = 0.2f;

        /// <summary>
        /// Arc 모션이 종료된 이후, 캐릭터를 최종적으로 지면에 스냅시키기 위한 최대 거리입니다.
        /// 지면과 약간의 오차가 있는 경우에도 확실히 바닥에 붙도록 보정하기 위한 값입니다.
        /// </summary>
        private const float KnockUpLandingFinalSnapDistance = 0.75f;

        /// <summary>
        /// CC 시작 가능 조건(IsGroundOnly / IsAirOnly) 판정에 사용할 지면 거리 임계값입니다.
        /// CharacterGroundProbeUtility 기본값과 동일한 기준을 사용합니다.
        /// </summary>
        private const float CrowdControlGroundedCheckDistance = CharacterGroundProbeUtility.DefaultGroundedCheckDistance;

        private readonly struct QueuedCrowdControl
        {
            public readonly CrowdControlRuntimeData CrowdControl;

            public QueuedCrowdControl(CrowdControlRuntimeData crowdControl)
            {
                CrowdControl = crowdControl;
            }
        }

        private enum KnockUpAnimationPhase
        {
            None = 0,
            Rise = 1,
            Air = 2,
            FallLoop = 3,
            LandEnd = 4,
        }
        
        private void Awake()
        {
            _character = GetComponent<CharacterBase>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _handlers = CreateHandlers();
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
            var runtimeData = CrowdControlRuntimeDataResolver.Resolve(TableLoaderManager.Instance, crowdControl);
            ApplyCrowdControl(runtimeData, source);
        }

        public void ApplyCrowdControl(CrowdControlRuntimeData crowdControl, GameObject source)
        {
            ClearQueuedSequence();
            ApplyCrowdControlInternal(crowdControl, source, forceReplaceCurrent: true);
        }

        private void ApplyCrowdControlInternal(CrowdControlRuntimeData crowdControl, GameObject source, bool forceReplaceCurrent)
        {
            if (crowdControl == null) return;
            if (_character == null) return;

            if (!IsCrowdControlStartStateAllowed(crowdControl))
                return;

            if (forceReplaceCurrent)
                ForceStopInternal(clearSequence: false);

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

            // 시작/종료 위치 계산
            var currentPos = _rigidbody2D != null ? _rigidbody2D.position : (Vector2)transform.position;
            var startPos = currentPos;
            var rawEndPos = currentPos + (direction * crowdControl.Distance);
            var endPos = ResolveEndPosition(crowdControl, startPos, rawEndPos);

            var travel = endPos - startPos;
            var travelDistance = travel.magnitude;
            var travelDirection = travelDistance > Epsilon ? (travel / travelDistance) : direction;

            bool hasAnyTime =
                Mathf.Abs(crowdControl.Duration) > Epsilon ||
                Mathf.Abs(crowdControl.DownWaitTime) > Epsilon;

            bool hasDistance = travelDistance > Epsilon;

            if (!hasAnyTime)
            {
                if (hasDistance)
                    MoveTo(endPos);

                PlayEndAndStop(crowdControl);
                _activeCrowdControl = null;
                return;
            }

            if (_motionController == null)
            {
                if (hasDistance)
                    MoveTo(endPos);

                PlayEndAndStop(crowdControl);
                _activeCrowdControl = null;
                return;
            }

            if (!TryBuildMotionRequest(crowdControl, travelDirection, travelDistance, out var req) ||
                !_motionController.TryStartMotion(in req))
            {
                if (hasDistance)
                    MoveTo(endPos);

                PlayEndAndStop(crowdControl);
                _activeCrowdControl = null;
            }
        }


        private bool TryBuildMotionRequest(
            CrowdControlRuntimeData crowdControl,
            Vector2 travelDirection,
            float travelDistance,
            out MotionRequest request)
        {
            request = default;
            if (crowdControl == null) return false;

            if (_handlers != null && _handlers.TryGetValue(crowdControl.Type, out var handler) && handler != null)
                return handler.TryBuildMotionRequest(crowdControl, travelDirection, travelDistance, out request);

            request = new MotionRequest(
                MotionChannel.CrowdControl,
                MotionKind.Linear,
                travelDirection,
                Mathf.Max(0f, crowdControl.Duration),
                Mathf.Max(0f, travelDistance),
                crowdControl.EaseType,
                stopAtEnd: true,
                useMovePosition: true,
                allowReplace: true);
            return true;
        }

        private static Dictionary<CrowdControlConstants.Type, ICrowdControlHandler> CreateHandlers()
        {
            return new Dictionary<CrowdControlConstants.Type, ICrowdControlHandler>
            {
                { CrowdControlConstants.Type.KnockBack, new KnockBackCrowdControlHandler() },
                { CrowdControlConstants.Type.KnockDown, new KnockDownCrowdControlHandler() },
                { CrowdControlConstants.Type.KnockUp, new KnockUpCrowdControlHandler() },
                { CrowdControlConstants.Type.KnockDownAir, new KnockDownAirCrowdControlHandler() },
            };
        }

        private void Update()
        {
            if (_activeCrowdControl == null) return;
            if (_motionController == null)
            {
                _activeCrowdControl = null;
                TryStartNextQueuedCrowdControl();
                return;
            }

            UpdateAirbornePhaseAnimation();

            if (TryHandleActiveAirborneLanding())
                return;

            // CC 채널 모션이 끝나면 종료 시퀀스
            if (!_motionController.IsPlaying(MotionChannel.CrowdControl))
            {
                if (_activeCrowdControl != null && IsLandingDrivenCrowdControl(_activeCrowdControl))
                {
                    TryHandleCompletedLandingDrivenCrowdControl();
                    return;
                }

                var finished = _activeCrowdControl;
                _activeCrowdControl = null;

                if (finished != null && finished.Type == CrowdControlConstants.Type.KnockUp)
                    SnapKnockUpToGroundIfNear(KnockUpLandingFinalSnapDistance);

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
        /// 시작 위치와 원시 종료 위치를 기반으로 최종 종료 위치를 계산합니다.
        /// </summary>
        private Vector2 ResolveEndPosition(CrowdControlRuntimeData crowdControl, Vector2 startPos, Vector2 rawEndPos)
        {
            switch (crowdControl.EndYMode)
            {
                case CrowdControlConstants.EndYMode.KeepStartY:
                    return new Vector2(rawEndPos.x, startPos.y);

                case CrowdControlConstants.EndYMode.AddOffsetFromStart:
                    return new Vector2(rawEndPos.x, startPos.y + crowdControl.EndYOffset);

                case CrowdControlConstants.EndYMode.Absolute:
                    return new Vector2(rawEndPos.x, crowdControl.EndYAbsolute);

                case CrowdControlConstants.EndYMode.GroundAtEndX:
                {
                    float groundY = ResolveGroundYAtEndX(rawEndPos, startPos.y);
                    return new Vector2(rawEndPos.x, groundY + crowdControl.EndYOffset);
                }

                case CrowdControlConstants.EndYMode.None:
                default:
                    return rawEndPos;
            }
        }

        /// <summary>
        /// 종료 X 지점에서 아래 방향으로 바닥을 탐색해 Y 값을 반환합니다.
        /// </summary>
        private float ResolveGroundYAtEndX(Vector2 rawEndPos, float fallbackY)
        {
            int groundMask = GetGroundProbeMask();
            if (groundMask == 0)
                return fallbackY;

            float originY = Mathf.Max(rawEndPos.y, fallbackY) + GroundProbeDefaultHeight;
            Vector2 origin = new Vector2(rawEndPos.x, originY);
            var hit = Physics2D.Raycast(origin, Vector2.down, GroundProbeDefaultDistance, groundMask);
            if (hit.collider != null)
                return hit.point.y;

            return fallbackY;
        }

        /// <summary>
        /// CrowdControl 종료 위치용 지면 탐색 마스크를 구성합니다.
        /// </summary>
        private static int GetGroundProbeMask()
        {
            return CharacterGroundProbeUtility.GetDefaultGroundProbeMask();
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
        private Vector2 ResolveDirection(CrowdControlRuntimeData crowdControl, GameObject source)
        {
            switch (crowdControl.DirectionType)
            {
                case CrowdControlConstants.DirectionType.FromSourceToTarget:
                    return ResolveDirectionBetween(sourceFirst: true, source);

                case CrowdControlConstants.DirectionType.FromTargetToSource:
                    return ResolveDirectionBetween(sourceFirst: false, source);

                case CrowdControlConstants.DirectionType.FromTargetFacing:
                    return ResolveFacingDirection();

                case CrowdControlConstants.DirectionType.Fixed:
                    return ResolveFixedDirection(crowdControl);

                default:
                    return ResolveFacingDirection();
            }
        }
        
        private Vector2 ResolveDirectionBetween(bool sourceFirst, GameObject source)
        {
            if (source != null)
            {
                Vector2 from = sourceFirst ? source.transform.position : transform.position;
                Vector2 to   = sourceFirst ? transform.position : source.transform.position;

                var dir = to - from;
                if (dir.sqrMagnitude > Epsilon)
                    return dir.normalized;
            }

            return sourceFirst ? ResolveFacingDirection() : -ResolveFacingDirection();
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

        private Vector2 ResolveFixedDirection(CrowdControlRuntimeData crowdControl)
        {
            var v = new Vector2(crowdControl.FixedDirectionX, crowdControl.FixedDirectionY);
            if (v.sqrMagnitude > Epsilon)
                return v.normalized;

            return ResolveFacingDirection();
        }
        
        /// <summary>
        /// CC 시작 시 재생할 경직(Stagger) 애니메이션을 재생합니다.
        /// </summary>
        /// <param name="crowdControl">경직 애니메이션 이름을 포함한 CC 데이터입니다.</param>
        /// <remarks>
        /// 테이블의 애니메이션 이름이 비어있으면 아무 것도 재생하지 않으며,
        /// Start → Wait 전환은 Animator의 Transition으로 구성되어 있다고 가정합니다.
        /// </remarks>
        private void PlayStaggerAnimation(CrowdControlRuntimeData crowdControl)
        {
            if (_character?.CharacterAnimationController == null) return;

            // 테이블이 비어있으면 아무 것도 재생하지 않습니다.
            // Start → Wait 전환은 Animator의 Transition으로 구성하는 전제입니다.
            _currentStaggerAnimationName = crowdControl?.StaggerAnimationName;
            _currentPhaseAnimationName = _currentStaggerAnimationName;
            _currentKnockUpAnimationPhase = KnockUpAnimationPhase.None;

            if (IsAirborneCrowdControl(crowdControl) && HasAirbornePhasedAnimation(crowdControl))
            {
                var initialPhase = EvaluateAirborneAnimationPhase(crowdControl, 0f);
                ApplyAirbornePhaseAnimation(crowdControl, initialPhase, force: true);
                return;
            }

            if (string.IsNullOrWhiteSpace(_currentStaggerAnimationName)) return;

            if (_character.CharacterAnimationController.HasAnimation(_currentStaggerAnimationName))
            {
                _character.CharacterAnimationController.PlayCharacterAnimation(_currentStaggerAnimationName, loop: false);
            }
        }

        /// <summary>
        /// CC 종료 애니메이션(있다면)을 재생하고, 최종적으로 CC 상태를 정리합니다.
        /// </summary>
        /// <param name="crowdControl">종료 대기 시간 보정에 사용할 CC 데이터입니다(없을 수 있음).</param>
        /// <remarks>
        /// End 애니메이션이 존재하면
        /// - 애니메이션 길이
        /// - RecoverTime
        /// - KnockDownAir 전용 추가 대기 시간
        /// 을 반영한 최종 시간만큼 대기한 뒤 <c>Stop(isForce: true)</c>로 상태를 강제 해제합니다.
        /// </remarks>
        private void PlayEndAndStop(CrowdControlRuntimeData crowdControl = null)
        {
            if (_stopRoutine != null)
            {
                StopCoroutine(_stopRoutine);
                _stopRoutine = null;
            }

            string endName = null;
            bool hasEndAnimation = false;
            float animationDurationSec = 0f;

            if (_character?.CharacterAnimationController != null)
            {
                endName = ResolveEndAnimationName(crowdControl);
                hasEndAnimation = !string.IsNullOrWhiteSpace(endName) &&
                                  _character.CharacterAnimationController.HasAnimation(endName);

                if (hasEndAnimation)
                {
                    _character.CharacterAnimationController.PlayCharacterAnimation(endName, loop: false);
                    animationDurationSec = _character.CharacterAnimationController.GetCharacterAnimationDuration(endName, isMilliseconds: false);
                    animationDurationSec = Mathf.Max(0f, animationDurationSec);
                }
            }

            float durationSec = ResolveEndStopDuration(crowdControl, animationDurationSec, hasEndAnimation);
            if (durationSec > 0f || hasEndAnimation)
            {
                _stopRoutine = StartCoroutine(StopAfter(durationSec));
                return;
            }

            _character?.Stop(isForce: true);
            ResetAnimationState();
            TryStartNextQueuedCrowdControl();
        }

        /// <summary>
        /// CC 종료 후 강제 해제까지의 최종 대기 시간을 계산합니다.
        /// </summary>
        private static float ResolveEndStopDuration(
            CrowdControlRuntimeData crowdControl,
            float animationDurationSec,
            bool hasEndAnimation)
        {
            float durationSec = 0f;

            if (hasEndAnimation)
                durationSec = Mathf.Max(0f, animationDurationSec);

            if (crowdControl != null)
            {
                durationSec = Mathf.Max(durationSec, Mathf.Max(0f, crowdControl.RecoverTime));

                if (crowdControl.Type == CrowdControlConstants.Type.KnockDownAir)
                    durationSec += Mathf.Max(0f, crowdControl.KnockDownAirLandEndWaitTime);
            }

            return durationSec;
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
            ResetAnimationState();
            _stopRoutine = null;
            TryStartNextQueuedCrowdControl();
        }

        /// <summary>
        /// 진행 중인 CC를 즉시 중단하고, 모션/코루틴/상태를 강제 정리합니다.
        /// </summary>
        /// <remarks>
        /// 외부에 공개하지 않고, 새 CC 적용 전에 내부적으로 교체(replace) 처리할 때 사용합니다.
        /// </remarks>
        private void ForceStopInternal(bool clearSequence = true)
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
            ResetAnimationState();
            _activeCrowdControl = null;

            if (clearSequence)
                ClearQueuedSequence();
        }



        private bool IsCrowdControlStartStateAllowed(CrowdControlRuntimeData crowdControl)
        {
            if (crowdControl == null)
                return false;

            if (crowdControl.IsGroundOnly && crowdControl.IsAirOnly)
            {
                Debug.LogWarning($"[CharacterCrowdControlController] CrowdControl UID={crowdControl.Uid} has both IsGroundOnly and IsAirOnly enabled. The crowd control will be skipped.", this);
                return false;
            }

            if (!crowdControl.IsGroundOnly && !crowdControl.IsAirOnly)
                return true;

            bool isGrounded = IsCurrentlyGrounded(CrowdControlGroundedCheckDistance);
            if (crowdControl.IsGroundOnly)
                return isGrounded;

            if (crowdControl.IsAirOnly)
                return !isGrounded;

            return true;
        }

        private bool IsCurrentlyGrounded(float maxGroundDistance)
        {
            if (maxGroundDistance < 0f)
                maxGroundDistance = 0f;

            return CharacterGroundProbeUtility.IsCurrentlyGrounded(this, _rigidbody2D, GetGroundProbeMask(), maxGroundDistance);
        }

        private bool TryHandleActiveAirborneLanding()
        {
            if (!IsAirborneCrowdControl(_activeCrowdControl))
                return false;

            if (!_motionController.IsPlaying(MotionChannel.CrowdControl))
                return false;

            if (!IsAirborneLandingPhase(_activeCrowdControl))
                return false;

            if (!TryProbeGroundBelow(out float groundY, out float bottomY))
                return false;

            float distanceToGround = bottomY - groundY;
            if (distanceToGround < -KnockUpLandingProbeUpOffset || distanceToGround > KnockUpLandingTriggerDistance)
                return false;

            SnapCharacterBottomToGround(groundY, bottomY);
            _motionController.CancelMotion(MotionChannel.CrowdControl, reason: 201);

            var finished = _activeCrowdControl;
            _activeCrowdControl = null;
            PlayEndAndStop(finished);
            return true;
        }

        private bool TryHandleCompletedLandingDrivenCrowdControl()
        {
            if (!IsLandingDrivenCrowdControl(_activeCrowdControl))
                return true;

            float snapProbeDistance = Mathf.Max(
                KnockUpLandingFinalSnapDistance,
                Mathf.Max(1f, _activeCrowdControl.Height + Mathf.Abs(_activeCrowdControl.EndYOffset)));

            if (TryProbeGroundBelow(snapProbeDistance, out float groundY, out float bottomY))
            {
                float distanceToGround = bottomY - groundY;
                if (distanceToGround >= -KnockUpLandingProbeUpOffset && distanceToGround <= snapProbeDistance)
                {
                    SnapCharacterBottomToGround(groundY, bottomY);
                    var finished = _activeCrowdControl;
                    _activeCrowdControl = null;
                    PlayEndAndStop(finished);
                    return true;
                }
            }

            if (!IsCurrentlyGrounded(KnockUpLandingTriggerDistance))
                return false;

            var landedCrowdControl = _activeCrowdControl;
            _activeCrowdControl = null;
            PlayEndAndStop(landedCrowdControl);
            return true;
        }

        private bool IsAirborneLandingPhase(CrowdControlRuntimeData crowdControl)
        {
            if (crowdControl == null)
                return false;

            if (_currentKnockUpAnimationPhase == KnockUpAnimationPhase.FallLoop)
                return true;

            if (!_motionController.TryGetMotionProgress(MotionChannel.CrowdControl, out float progress01))
                return false;

            return EvaluateAirborneAnimationPhase(crowdControl, progress01) == KnockUpAnimationPhase.FallLoop;
        }

        private void SnapKnockUpToGroundIfNear(float maxSnapDistance)
        {
            if (!TryProbeGroundBelow(out float groundY, out float bottomY))
                return;

            float distanceToGround = bottomY - groundY;
            if (distanceToGround < -KnockUpLandingProbeUpOffset || distanceToGround > maxSnapDistance)
                return;

            SnapCharacterBottomToGround(groundY, bottomY);
        }

        private bool TryProbeGroundBelow(out float groundY, out float bottomY)
        {
            float probeDistance = Mathf.Max(KnockUpLandingFinalSnapDistance, KnockUpLandingTriggerDistance);
            return TryProbeGroundBelow(probeDistance, out groundY, out bottomY);
        }

        private bool TryProbeGroundBelow(float maxGroundDistance, out float groundY, out float bottomY)
        {
            return CharacterGroundProbeUtility.TryProbeGroundBelow(this, _rigidbody2D, maxGroundDistance, GetGroundProbeMask(), out groundY, out bottomY);
        }

        private void SnapCharacterBottomToGround(float groundY, float currentBottomY)
        {
            float deltaY = groundY - currentBottomY;
            if (Mathf.Abs(deltaY) <= Epsilon)
                return;

            Vector2 currentPos = _rigidbody2D != null ? _rigidbody2D.position : (Vector2)transform.position;
            MoveTo(currentPos + new Vector2(0f, deltaY));
        }


        private void UpdateAirbornePhaseAnimation()
        {
            if (!IsAirborneCrowdControl(_activeCrowdControl))
                return;

            if (!HasAirbornePhasedAnimation(_activeCrowdControl))
                return;

            if (!_motionController.TryGetMotionProgress(MotionChannel.CrowdControl, out float progress01))
                return;

            var nextPhase = EvaluateAirborneAnimationPhase(_activeCrowdControl, progress01);
            ApplyAirbornePhaseAnimation(_activeCrowdControl, nextPhase, force: false);
        }

        private static bool HasAirbornePhasedAnimation(CrowdControlRuntimeData crowdControl)
        {
            if (crowdControl == null)
                return false;

            return !string.IsNullOrWhiteSpace(crowdControl.KnockUpRiseAnimationName)
                || !string.IsNullOrWhiteSpace(crowdControl.KnockUpAirAnimationName)
                || !string.IsNullOrWhiteSpace(crowdControl.KnockUpFallAnimationName)
                || !string.IsNullOrWhiteSpace(crowdControl.KnockUpLandEndAnimationName);
        }

        private static KnockUpAnimationPhase EvaluateAirborneAnimationPhase(CrowdControlRuntimeData crowdControl, float progress01)
        {
            if (crowdControl == null)
                return KnockUpAnimationPhase.None;

            float riseTime = Mathf.Max(0f, crowdControl.KnockUpRiseTime);
            float airTime = Mathf.Max(0f, crowdControl.KnockUpAirTime);
            float fallTime = Mathf.Max(0f, crowdControl.KnockUpFallTime);
            float totalTime = riseTime + airTime + fallTime;
            if (totalTime <= Epsilon)
                return KnockUpAnimationPhase.Rise;

            float riseEnd = riseTime / totalTime;
            float airEnd = (riseTime + airTime) / totalTime;
            float normalized = Mathf.Clamp01(progress01);

            if (normalized < riseEnd)
                return KnockUpAnimationPhase.Rise;
            if (normalized < airEnd)
                return KnockUpAnimationPhase.Air;
            return KnockUpAnimationPhase.FallLoop;
        }

        private void ApplyAirbornePhaseAnimation(CrowdControlRuntimeData crowdControl, KnockUpAnimationPhase phase, bool force)
        {
            if (_character?.CharacterAnimationController == null)
                return;

            if (!force && _currentKnockUpAnimationPhase == phase)
                return;

            string animationName = GetAirbornePhaseAnimationName(crowdControl, phase);
            if (string.IsNullOrWhiteSpace(animationName))
                return;

            if (!_character.CharacterAnimationController.HasAnimation(animationName))
                return;

            bool loop;
            if (phase == KnockUpAnimationPhase.Air)
            {
                loop = crowdControl != null
                    && crowdControl.Type == CrowdControlConstants.Type.KnockDownAir
                    && crowdControl.KnockDownAirAnimationIsLoop;
            }
            else
            {
                loop = phase == KnockUpAnimationPhase.FallLoop;
            }

            _character.CharacterAnimationController.PlayCharacterAnimation(animationName, loop);
            _currentPhaseAnimationName = animationName;
            _currentKnockUpAnimationPhase = phase;
        }

        private static string GetAirbornePhaseAnimationName(CrowdControlRuntimeData crowdControl, KnockUpAnimationPhase phase)
        {
            if (crowdControl == null)
                return string.Empty;

            switch (phase)
            {
                case KnockUpAnimationPhase.Rise:
                    return !string.IsNullOrWhiteSpace(crowdControl.KnockUpRiseAnimationName)
                        ? crowdControl.KnockUpRiseAnimationName
                        : crowdControl.StaggerAnimationName;

                case KnockUpAnimationPhase.Air:
                    return crowdControl.KnockUpAirAnimationName;

                case KnockUpAnimationPhase.FallLoop:
                    return crowdControl.KnockUpFallAnimationName;

                case KnockUpAnimationPhase.LandEnd:
                    return crowdControl.KnockUpLandEndAnimationName;

                default:
                    return crowdControl.StaggerAnimationName;
            }
        }

        private static bool IsAirborneCrowdControl(CrowdControlRuntimeData crowdControl)
        {
            if (crowdControl == null)
                return false;

            return crowdControl.Type == CrowdControlConstants.Type.KnockUp
                || crowdControl.Type == CrowdControlConstants.Type.KnockDownAir;
        }

        private static bool IsLandingDrivenCrowdControl(CrowdControlRuntimeData crowdControl)
        {
            if (crowdControl == null)
                return false;

            return crowdControl.Type == CrowdControlConstants.Type.KnockDownAir;
        }

        private string ResolveEndAnimationName(CrowdControlRuntimeData crowdControl)
        {
            if (_character?.CharacterAnimationController == null)
                return null;

            if (IsAirborneCrowdControl(crowdControl))
            {
                string knockUpEndName = GetAirbornePhaseAnimationName(crowdControl, KnockUpAnimationPhase.LandEnd);
                if (!string.IsNullOrWhiteSpace(knockUpEndName) && _character.CharacterAnimationController.HasAnimation(knockUpEndName))
                {
                    _currentKnockUpAnimationPhase = KnockUpAnimationPhase.LandEnd;
                    _currentPhaseAnimationName = knockUpEndName;
                    return knockUpEndName;
                }
            }

            if (!string.IsNullOrWhiteSpace(_currentPhaseAnimationName))
            {
                string phaseEndName = _currentPhaseAnimationName + StruckTableCrowdControl.StaggerAnimationEndSuffix;
                if (_character.CharacterAnimationController.HasAnimation(phaseEndName))
                    return phaseEndName;
            }

            if (!string.IsNullOrWhiteSpace(_currentStaggerAnimationName))
            {
                string defaultEndName = _currentStaggerAnimationName + StruckTableCrowdControl.StaggerAnimationEndSuffix;
                if (_character.CharacterAnimationController.HasAnimation(defaultEndName))
                    return defaultEndName;
            }

            return null;
        }

        private void ResetAnimationState()
        {
            _currentStaggerAnimationName = null;
            _currentPhaseAnimationName = null;
            _currentKnockUpAnimationPhase = KnockUpAnimationPhase.None;
        }


        private void TryStartNextQueuedCrowdControl()
        {
            if (_activeCrowdControl != null)
                return;

            if (!_isSequenceRunning)
                return;

            if (_sequenceTarget != null && _character != null && _sequenceTarget != _character.gameObject)
            {
                ClearQueuedSequence();
                return;
            }

            while (_queuedCrowdControls.Count > 0)
            {
                var next = _queuedCrowdControls.Dequeue();
                if (next.CrowdControl == null)
                    continue;

                ApplyCrowdControlInternal(next.CrowdControl, _sequenceSource, forceReplaceCurrent: false);
                if (_activeCrowdControl != null)
                    return;
            }

            ClearQueuedSequence();
        }

        private void ClearQueuedSequence()
        {
            _queuedCrowdControls.Clear();
            _isSequenceRunning = false;
            _sequenceSource = null;
            _sequenceTarget = null;
        }

        public void ApplyCrowdControlSequenceByUid(IReadOnlyList<int> crowdControlUids, GameObject source, GameObject target)
        {
            if (crowdControlUids == null || crowdControlUids.Count == 0)
                return;

            if (TableLoaderManager.Instance == null)
                return;

            var runtimeList = new List<CrowdControlRuntimeData>(crowdControlUids.Count);
            for (int i = 0; i < crowdControlUids.Count; i++)
            {
                int uid = crowdControlUids[i];
                if (uid <= 0)
                    continue;

                var info = TableLoaderManager.Instance.GetCrowdControlRuntimeData(uid, logIfMissing: false);
                if (info != null)
                    runtimeList.Add(info);
            }

            ApplyCrowdControlSequence(runtimeList, source, target);
        }

        public void ApplyCrowdControlSequence(IReadOnlyList<CrowdControlRuntimeData> crowdControls, GameObject source, GameObject target)
        {
            if (crowdControls == null || crowdControls.Count == 0)
                return;

            ForceStopInternal();

            _isSequenceRunning = true;
            _sequenceSource = source;
            _sequenceTarget = target;

            for (int i = 0; i < crowdControls.Count; i++)
            {
                var crowdControl = crowdControls[i];
                if (crowdControl == null)
                    continue;

                _queuedCrowdControls.Enqueue(new QueuedCrowdControl(crowdControl));
            }

            TryStartNextQueuedCrowdControl();
        }

        /// <summary>
        /// CC UID를 통해 테이블에서 데이터를 조회한 뒤 CrowdControl을 적용합니다.
        /// </summary>
        /// <param name="crowdControlUid">조회할 CC 테이블 UID입니다.</param>
        /// <param name="source">방향 계산에 사용할 공격/발생 원본 오브젝트입니다(없을 수 있음).</param>
        public void ApplyCrowdControlByUid(int crowdControlUid, GameObject source)
        {
            var info = TableLoaderManager.Instance != null
                ? TableLoaderManager.Instance.GetCrowdControlRuntimeData(crowdControlUid, logIfMissing: false)
                : null;
            if (info == null) return;
            ApplyCrowdControl(info, source);
        }
    }
}
