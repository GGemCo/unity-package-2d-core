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
    public sealed class CharacterCrowdControlController : MonoBehaviour, IMonsterPoolLifecycle
    {
        private static readonly List<CharacterCrowdControlController> ActiveControllers = new();

        private CharacterBase _character;
        private Rigidbody2D _rigidbody2D;
        private ICharacterMotionController _motionController;
        private CharacterMotionController2D _motionController2D;
        private GameObject _activeSource;
        private CharacterAirborneHandle _crowdControlAirborneHandle;
        private bool _isActive;
        public bool IsActive => _isActive;

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
        private CrowdControlAirborneAnimationPhase _currentAirborneAnimationPhase;
        /// <summary>
        /// 현재 CrowdControl 적용 사이클에서 애니메이션을 강제로 첫 프레임부터 재생할지 여부입니다.
        /// </summary>
        private bool _forceRefreshAnimationOnCurrentCrowdControl;

        private Coroutine _stopRoutine;
        private Coroutine _animationEaseRoutine;
        internal const float Epsilon = 0.0001f;
        private const float GroundProbeDefaultHeight = 2f;
        private const float GroundProbeDefaultDistance = 12f;
        /// <summary>
        /// Ground probe 시작 위치를 캐릭터 하단보다 약간 위로 올리기 위한 오프셋.
        /// 지면과 거의 붙어있는 상태에서 Raycast가 시작 지점에서 바로 히트되는 것을 방지하고,
        /// 안정적인 착지 판정을 위해 사용됩니다.
        /// </summary>
        internal const float KnockUpLandingProbeUpOffset = 0.1f;

        /// <summary>
        /// FallLoop 단계에서 지면과의 거리가 이 값 이하가 되면,
        /// 아직 Arc 모션이 끝나지 않았더라도 강제로 착지(LandEnd) 단계로 전환하기 위한 임계 거리입니다.
        /// 너무 크면 공중에서 조기 종료되고, 너무 작으면 여전히 부자연스러운 끊김이 발생할 수 있습니다.
        /// </summary>
        internal const float KnockUpLandingTriggerDistance = 0.2f;

        /// <summary>
        /// Arc 모션이 종료된 이후, 캐릭터를 최종적으로 지면에 스냅시키기 위한 최대 거리입니다.
        /// 지면과 약간의 오차가 있는 경우에도 확실히 바닥에 붙도록 보정하기 위한 값입니다.
        /// </summary>
        internal const float KnockUpLandingFinalSnapDistance = 0.75f;

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

        /// <summary>
        /// 활성화된 Crowd Control 컨트롤러를 전역 조회 목록에 등록합니다.
        /// </summary>
        /// <remarks>
        /// 공격자 피격 시, 해당 공격자가 적용한 CC를 다른 대상에서 찾아 중단하기 위해 사용합니다.
        /// </remarks>
        private void OnEnable()
        {
            if (!ActiveControllers.Contains(this))
                ActiveControllers.Add(this);
        }

        /// <summary>
        /// 비활성화된 Crowd Control 컨트롤러를 전역 조회 목록에서 제거합니다.
        /// </summary>
        private void OnDisable()
        {
            ActiveControllers.Remove(this);
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
            _motionController2D = _motionController as CharacterMotionController2D ?? GetComponent<CharacterMotionController2D>();
            if (_motionController2D != null)
                _motionController2D.WallImpacted += OnMotionWallImpacted;
        }

        private void OnDestroy()
        {
            ActiveControllers.Remove(this);

            if (_motionController2D != null)
                _motionController2D.WallImpacted -= OnMotionWallImpacted;
        }

        /// <summary>
        /// CrowdControl 테이블 정의를 기반으로 CrowdControl을 적용합니다.
        /// </summary>
        /// <param name="crowdControl">적용할 CC(넉백/넉다운/넉업 등) 데이터입니다.</param>
        /// <param name="source">방향 계산에 사용할 공격/발생 원본 오브젝트입니다(없을 수 있음).</param>
        /// <param name="isEndCharacterStop">종료 후 Character.Stop 처리 여부.</param>
        /// <remarks>
        /// - 기존 CC가 진행 중이면 즉시 중단 후 새 CC로 교체합니다. <br/>
        /// - Duration/DownWaitTime이 0이면 즉시 이동(거리 존재 시) 후 종료 처리합니다. <br/>
        /// - 이동은 <see cref="MotionChannel.CrowdControl"/> 채널로 요청되며, 종료 시 End 애니메이션 및 상태 해제를 수행합니다.
        /// </remarks>
        public void ApplyCrowdControl(StruckTableCrowdControl crowdControl, GameObject source, bool isEndCharacterStop = false)
        {
            ApplyCrowdControl(crowdControl, source, isEndCharacterStop, forceRefreshAnimation: false);
        }

        /// <summary>
        /// CrowdControl 테이블 정의를 기반으로 CrowdControl을 적용합니다.
        /// </summary>
        /// <param name="crowdControl">적용할 CC(넉백/넉다운/넉업 등) 데이터입니다.</param>
        /// <param name="source">방향 계산에 사용할 공격/발생 원본 오브젝트입니다(없을 수 있음).</param>
        /// <param name="isEndCharacterStop">종료 후 Character.Stop 처리 여부.</param>
        /// <param name="forceRefreshAnimation">
        /// <see langword="true"/>면 동일한 애니메이션 상태를 재적용할 때도
        /// 애니메이션을 첫 프레임부터 강제로 다시 재생합니다.
        /// </param>
        public void ApplyCrowdControl(StruckTableCrowdControl crowdControl, GameObject source, bool isEndCharacterStop, bool forceRefreshAnimation)
        {
            CrowdControlRuntimeData runtimeData = CrowdControlRuntimeDataResolver.Resolve(TableLoaderManager.Instance, crowdControl);
            if (runtimeData == null) return;
            runtimeData.IsEndCharacterStop = isEndCharacterStop;
            ApplyCrowdControl(runtimeData, source, forceRefreshAnimation);
        }

        /// <summary>
        /// 런타임 CrowdControl 데이터를 즉시 적용합니다.
        /// </summary>
        /// <param name="crowdControl">적용할 런타임 CrowdControl 데이터입니다.</param>
        /// <param name="source">방향 계산에 사용할 공격/발생 원본 오브젝트입니다(없을 수 있음).</param>
        public void ApplyCrowdControl(CrowdControlRuntimeData crowdControl, GameObject source)
        {
            ApplyCrowdControl(crowdControl, source, forceRefreshAnimation: false);
        }

        /// <summary>
        /// 런타임 CrowdControl 데이터를 즉시 적용합니다.
        /// </summary>
        /// <param name="crowdControl">적용할 런타임 CrowdControl 데이터입니다.</param>
        /// <param name="source">방향 계산에 사용할 공격/발생 원본 오브젝트입니다(없을 수 있음).</param>
        /// <param name="forceRefreshAnimation">
        /// <see langword="true"/>면 동일 상태명 애니메이션도 강제로 재시작합니다.
        /// </param>
        public void ApplyCrowdControl(CrowdControlRuntimeData crowdControl, GameObject source, bool forceRefreshAnimation)
        {
            ClearQueuedSequence();
            ApplyCrowdControlInternal(crowdControl, source, forceReplaceCurrent: true, forceRefreshAnimation: forceRefreshAnimation);
        }

        private void ApplyCrowdControlInternal(
            CrowdControlRuntimeData crowdControl,
            GameObject source,
            bool forceReplaceCurrent,
            bool forceRefreshAnimation)
        {
            if (crowdControl == null) return;
            if (_character == null) return;

            if (!IsCrowdControlStartStateAllowed(crowdControl))
                return;

            if (forceReplaceCurrent)
                ForceStopInternal(clearSequence: false, crowdControl.IsEndCharacterStop);

            _activeCrowdControl = crowdControl;
            _activeSource = source;
            _isActive = true;
            AcquireCrowdControlAirborneState(crowdControl);
            // 이번 CC 적용 사이클 동안 phase/end 애니메이션까지 동일한 강제 재생 정책을 유지합니다.
            _forceRefreshAnimationOnCurrentCrowdControl = forceRefreshAnimation;

            // 방향 결정
            var direction = ResolveDirection(crowdControl, source);

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
                _activeSource = null;
                return;
            }

            if (_motionController == null)
            {
                if (hasDistance)
                    MoveTo(endPos);

                PlayEndAndStop(crowdControl);
                _activeCrowdControl = null;
                _activeSource = null;
                return;
            }

            if (!TryBuildMotionRequest(crowdControl, travelDirection, travelDistance, out var req) ||
                !_motionController.TryStartMotion(in req))
            {
                if (hasDistance)
                    MoveTo(endPos);

                PlayEndAndStop(crowdControl);
                _activeCrowdControl = null;
                _activeSource = null;
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
                allowReplace: true,
                stopOnWall: crowdControl.IsStopOnWall);
            return true;
        }

        private static Dictionary<CrowdControlConstants.Type, ICrowdControlHandler> CreateHandlers()
        {
            return new Dictionary<CrowdControlConstants.Type, ICrowdControlHandler>
            {
                { CrowdControlConstants.Type.KnockBack, new CrowdControlHandlerKnockBack() },
                { CrowdControlConstants.Type.KnockDown, new CrowdControlHandlerKnockDown() },
                { CrowdControlConstants.Type.KnockUp, new CrowdControlHandlerKnockUp() },
                { CrowdControlConstants.Type.KnockDownAir, new CrowdControlHandlerKnockDownAir() },
            };
        }

        private void Update()
        {
            if (_activeCrowdControl == null) return;
            if (_motionController == null)
            {
                _activeCrowdControl = null;
                _activeSource = null;
                _isActive = false;
                ReleaseCrowdControlAirborneState();
                TryStartNextQueuedCrowdControl();
                return;
            }

            GetHandler(_activeCrowdControl)?.UpdateRuntime(this, _activeCrowdControl);

            if (GetHandler(_activeCrowdControl)?.TryHandleActiveLanding(this, _activeCrowdControl) == true)
            {
                var finishedActive = _activeCrowdControl;
                _activeCrowdControl = null;
                _activeSource = null;
                PlayEndAndStop(finishedActive);
                return;
            }

            // CC 채널 모션이 끝나면 종료 시퀀스
            if (!_motionController.IsPlaying(MotionChannel.CrowdControl))
            {
                var finished = _activeCrowdControl;
                var handler = GetHandler(finished);
                if (finished != null && handler != null && handler.IsLandingDriven(finished))
                {
                    if (!handler.TryHandleCompletedLanding(this, finished))
                        return;
                }
                else if (finished != null && finished.Type == CrowdControlConstants.Type.KnockUp)
                {
                    SnapKnockUpToGroundIfNear(KnockUpLandingFinalSnapDistance);
                }

                _activeCrowdControl = null;
                _activeSource = null;
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
        /// 플레이어 화면 경계 정책이 활성화된 경우 X축을 먼저 보정하여
        /// GroundAtEndX 지면 탐색도 보정된 X 좌표를 사용하도록 처리합니다.
        /// </summary>
        /// <param name="crowdControl">종료 위치 정책이 포함된 CrowdControl 런타임 데이터입니다.</param>
        /// <param name="startPos">CrowdControl 시작 시점의 캐릭터 위치입니다.</param>
        /// <param name="rawEndPos">방향과 거리를 적용한 원시 종료 위치입니다.</param>
        /// <returns>Y 위치 정책과 화면 경계 정책이 반영된 최종 종료 위치입니다.</returns>
        private Vector2 ResolveEndPosition(CrowdControlRuntimeData crowdControl, Vector2 startPos, Vector2 rawEndPos)
        {
            bool useViewportClamp = CrowdControlEndViewportResolver.TryCreateContext(
                _character,
                _rigidbody2D,
                crowdControl,
                out CrowdControlViewportClampContext viewportContext);

            Vector2 adjustedRawEndPos = useViewportClamp
                ? viewportContext.ClampHorizontal(rawEndPos)
                : rawEndPos;

            Vector2 resolvedEndPos;
            switch (crowdControl.EndYMode)
            {
                case CrowdControlConstants.EndYMode.KeepStartY:
                    resolvedEndPos = new Vector2(adjustedRawEndPos.x, startPos.y);
                    break;

                case CrowdControlConstants.EndYMode.AddOffsetFromStart:
                    resolvedEndPos = new Vector2(adjustedRawEndPos.x, startPos.y + crowdControl.EndYOffset);
                    break;

                case CrowdControlConstants.EndYMode.Absolute:
                    resolvedEndPos = new Vector2(adjustedRawEndPos.x, crowdControl.EndYAbsolute);
                    break;

                case CrowdControlConstants.EndYMode.GroundAtEndX:
                {
                    float groundY = ResolveGroundYAtEndX(adjustedRawEndPos, startPos.y);
                    resolvedEndPos = new Vector2(adjustedRawEndPos.x, groundY + crowdControl.EndYOffset);
                    break;
                }

                case CrowdControlConstants.EndYMode.None:
                default:
                    resolvedEndPos = adjustedRawEndPos;
                    break;
            }

            return useViewportClamp
                ? viewportContext.ClampVertical(resolvedEndPos)
                : resolvedEndPos;
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
            _currentAirborneAnimationPhase = CrowdControlAirborneAnimationPhase.None;

            if (TryPlayInitialAnimationOverride(crowdControl))
                return;

            var handler = GetHandler(crowdControl);
            if (handler != null && handler.TryGetInitialAnimation(this, crowdControl, out string initialAnimationName, out bool loop, out CrowdControlAirborneAnimationPhase initialPhase))
            {
                if (_character.CharacterAnimationController.HasAnimation(initialAnimationName))
                {
                    StopAnimationEaseRoutine(resetPlaybackTimeScale: false);
                    _character.CharacterAnimationController.PlayCharacterAnimation(
                        initialAnimationName,
                        loop,
                        timeScale: 1f,
                        forceReset: _forceRefreshAnimationOnCurrentCrowdControl);
                    _currentPhaseAnimationName = initialAnimationName;
                    _currentAirborneAnimationPhase = initialPhase;
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(_currentStaggerAnimationName)) return;

            if (_character.CharacterAnimationController.HasAnimation(_currentStaggerAnimationName))
            {
                StopAnimationEaseRoutine(resetPlaybackTimeScale: false);
                _character.CharacterAnimationController.PlayCharacterAnimation(
                    _currentStaggerAnimationName,
                    loop: false,
                    timeScale: 1f,
                    forceReset: _forceRefreshAnimationOnCurrentCrowdControl);
            }
        }

        /// <summary>
        /// Crowd Control에 1회성 초기 애니메이션 오버라이드가 설정되어 있으면 재생합니다.
        /// </summary>
        /// <param name="crowdControl">현재 적용 중인 Crowd Control 데이터입니다.</param>
        /// <returns>오버라이드 애니메이션을 재생했으면 <see langword="true"/>입니다.</returns>
        private bool TryPlayInitialAnimationOverride(CrowdControlRuntimeData crowdControl)
        {
            if (crowdControl == null)
                return false;

            CrowdControlAnimationOverride animationOverride = crowdControl.AnimationOverride;
            if (!animationOverride.IsValid)
                return false;

            ICharacterAnimationController animationController = _character?.CharacterAnimationController;
            if (animationController == null)
                return false;
            if (!animationController.HasAnimation(animationOverride.InitialAnimationName))
                return false;

            float clipDurationSeconds = Mathf.Max(
                0f,
                animationController.GetCharacterAnimationDuration(animationOverride.InitialAnimationName, isMilliseconds: false));
            float timeScale = animationOverride.ResolveTimeScale(clipDurationSeconds);

            StopAnimationEaseRoutine(resetPlaybackTimeScale: false);
            animationController.PlayCharacterAnimation(
                animationOverride.InitialAnimationName,
                animationOverride.Loop,
                timeScale,
                forceReset: animationOverride.ForceReset || _forceRefreshAnimationOnCurrentCrowdControl);

            _currentStaggerAnimationName = animationOverride.InitialAnimationName;
            _currentPhaseAnimationName = animationOverride.InitialAnimationName;
            _currentAirborneAnimationPhase = animationOverride.SuppressRuntimePhaseAnimations
                ? CrowdControlAirborneAnimationPhase.None
                : _currentAirborneAnimationPhase;

            if (animationOverride.UseEasing && !animationOverride.Loop)
            {
                float playbackDurationSeconds = animationOverride.ResolvePlaybackDuration(clipDurationSeconds);
                if (playbackDurationSeconds > Epsilon)
                {
                    _animationEaseRoutine = StartCoroutine(ApplyAnimationEaseRoutine(
                        animationController,
                        playbackDurationSeconds,
                        timeScale,
                        animationOverride.EaseType));
                }
            }

            return true;
        }

        /// <summary>
        /// 현재 재생 중인 애니메이션에 CC Easing과 같은 속도감을 주도록 TimeScale을 동적으로 보정합니다.
        /// </summary>
        /// <param name="animationController">재생 속도를 보정할 애니메이션 컨트롤러입니다.</param>
        /// <param name="playbackDurationSeconds">보정 재생 시간입니다.</param>
        /// <param name="baseTimeScale">Duration 맞춤으로 계산된 기본 TimeScale입니다.</param>
        /// <param name="easeType">적용할 Easing 타입입니다.</param>
        /// <returns>코루틴 열거자입니다.</returns>
        private IEnumerator ApplyAnimationEaseRoutine(
            ICharacterAnimationController animationController,
            float playbackDurationSeconds,
            float baseTimeScale,
            Easing.EaseType easeType)
        {
            if (animationController == null)
                yield break;

            float duration = Mathf.Max(Epsilon, playbackDurationSeconds);
            float elapsed = 0f;
            float previousEased = 0f;
            float safeBaseTimeScale = Mathf.Max(0.0001f, baseTimeScale);

            while (elapsed < duration)
            {
                float deltaTime = Mathf.Max(0f, Time.deltaTime);
                if (deltaTime <= 0f)
                {
                    yield return null;
                    continue;
                }

                float previousNormalized = Mathf.Clamp01(elapsed / duration);
                elapsed = Mathf.Min(duration, elapsed + deltaTime);
                float normalized = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.Clamp01(Easing.Apply(normalized, easeType));

                float normalizedDelta = Mathf.Max(Epsilon, normalized - previousNormalized);
                float easedDelta = Mathf.Max(0f, eased - previousEased);
                float easeSpeedScale = easedDelta / normalizedDelta;
                animationController.SetPlaybackTimeScale(safeBaseTimeScale * Mathf.Max(0f, easeSpeedScale));
                previousEased = eased;

                yield return null;
            }

            animationController.SetPlaybackTimeScale(safeBaseTimeScale);
            _animationEaseRoutine = null;
        }

        /// <summary>
        /// 진행 중인 애니메이션 Easing 보정 코루틴을 중지하고 필요 시 기본 재생 속도로 복구합니다.
        /// </summary>
        /// <param name="resetPlaybackTimeScale">true이면 현재 애니메이션 재생 속도를 1로 되돌립니다.</param>
        private void StopAnimationEaseRoutine(bool resetPlaybackTimeScale)
        {
            if (_animationEaseRoutine != null)
            {
                StopCoroutine(_animationEaseRoutine);
                _animationEaseRoutine = null;
            }

            if (resetPlaybackTimeScale)
            {
                _character?.CharacterAnimationController?.SetPlaybackTimeScale(1f);
            }
        }

        /// <summary>
        /// 공중형 Crowd Control이 시작될 때 공통 공중 상태를 등록합니다.
        /// </summary>
        /// <param name="crowdControl">현재 적용할 Crowd Control 데이터입니다.</param>
        private void AcquireCrowdControlAirborneState(CrowdControlRuntimeData crowdControl)
        {
            if (_character == null || crowdControl == null)
                return;

            if (!IsAirborneCrowdControl(crowdControl))
                return;

            ReleaseCrowdControlAirborneState();
            _crowdControlAirborneHandle = _character.AcquireAirborne(
                CharacterAirborneSource.CrowdControl,
                $"CrowdControl:{crowdControl.Type}");
        }

        /// <summary>
        /// 현재 Crowd Control이 등록한 공통 공중 상태를 해제합니다.
        /// </summary>
        private void ReleaseCrowdControlAirborneState()
        {
            if (!_crowdControlAirborneHandle.IsValid || _character == null)
                return;

            _character.ReleaseAirborne(_crowdControlAirborneHandle);
            _crowdControlAirborneHandle = default;
        }

        /// <summary>
        /// 지정한 Crowd Control이 캐릭터를 공중 상태로 취급해야 하는지 확인합니다.
        /// </summary>
        /// <param name="crowdControl">확인할 Crowd Control 데이터입니다.</param>
        /// <returns>공중형 Crowd Control이면 <see langword="true"/>를 반환합니다.</returns>
        private static bool IsAirborneCrowdControl(CrowdControlRuntimeData crowdControl)
        {
            return crowdControl != null
                   && (crowdControl.Type == CrowdControlConstants.Type.KnockUp
                       || crowdControl.Type == CrowdControlConstants.Type.KnockDownAir);
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
        /// - KnockUp 전용 추가 대기 시간
        /// 을 반영한 최종 시간만큼 대기한 뒤 <c>Stop(isForce: true)</c>로 상태를 강제 해제합니다.
        /// </remarks>
        private void PlayEndAndStop(CrowdControlRuntimeData crowdControl = null)
        {
            StopAnimationEaseRoutine(resetPlaybackTimeScale: true);

            if (_stopRoutine != null)
            {
                StopCoroutine(_stopRoutine);
                _stopRoutine = null;
            }
            
            // GcLogger.Log($"status: {_character.GetCurrentStatus()}");

            // End 애니메이션이 있으면, 클립 길이만큼 대기 후 Stop(true)로 상태를 강제 해제합니다.
            // (CharacterBase.Stop은 Knockback 상태일 때 기본적으로 return 하므로, CC 종료에는 강제가 필요합니다.)
            string endName = null;

            if (_character?.CharacterAnimationController != null)
            {
                bool isEndCharacterStop = crowdControl?.IsEndCharacterStop ?? false;
                // 캐릭터가 사망한 경우에는, Die 애니메이션 재생
                if (_character.IsStatusDead())
                {
                    endName = ICharacterAnimationController.DeadAnim;
                    if (!string.IsNullOrWhiteSpace(endName) && _character.CharacterAnimationController.HasAnimation(endName))
                    {
                        _character.CharacterAnimationController.PlayCharacterAnimation(
                            endName,
                            loop: false,
                            timeScale: 1f,
                            forceReset: _forceRefreshAnimationOnCurrentCrowdControl);

                        float durationSec = _character.CharacterAnimationController.GetCharacterAnimationDuration(endName, isMilliseconds: false);
                        durationSec = Mathf.Max(0f, durationSec);

                        // RecoverTime을 사용하는 경우(선택): 데이터 시간이 더 길면 그 시간을 우선
                        if (crowdControl != null && crowdControl.RecoverTime > durationSec)
                            durationSec = crowdControl.RecoverTime;

                        durationSec += GetAdditionalLandEndWaitTime(crowdControl);

                        _stopRoutine = StartCoroutine(StopAfter(durationSec, isEndCharacterStop));
                        return;
                    }
                }
                
                endName = ResolveEndAnimationName(crowdControl);
                if (!string.IsNullOrWhiteSpace(endName) && _character.CharacterAnimationController.HasAnimation(endName))
                {
                    _character.CharacterAnimationController.PlayCharacterAnimation(
                        endName,
                        loop: false,
                        timeScale: 1f,
                        forceReset: _forceRefreshAnimationOnCurrentCrowdControl);

                    float durationSec = _character.CharacterAnimationController.GetCharacterAnimationDuration(endName, isMilliseconds: false);
                    durationSec = Mathf.Max(0f, durationSec);

                    // RecoverTime을 사용하는 경우(선택): 데이터 시간이 더 길면 그 시간을 우선
                    if (crowdControl != null && crowdControl.RecoverTime > durationSec)
                        durationSec = crowdControl.RecoverTime;

                    durationSec += GetAdditionalLandEndWaitTime(crowdControl);

                    _stopRoutine = StartCoroutine(StopAfter(durationSec, isEndCharacterStop));
                    return;
                }
            }

            // End 애니메이션이 없으면 즉시 정리
            CharacterStop(crowdControl is { IsEndCharacterStop: true });
            ReleaseCrowdControlAirborneState();
            ResetAnimationState();
            TryStartNextQueuedCrowdControl();
        }

        private float GetAdditionalLandEndWaitTime(CrowdControlRuntimeData crowdControl)
        {
            return GetHandler(crowdControl)?.GetAdditionalEndWaitTime(crowdControl) ?? 0f;
        }

        /// <summary>
        /// 지정된 시간만큼 대기한 뒤 CC 상태를 강제 해제합니다.
        /// </summary>
        /// <param name="durationSec">대기할 시간(초)입니다.</param>
        /// <param name="isEndCharacterStop">종료 시 Character.Stop을 강제 호출할지 여부입니다.</param>
        /// <returns>코루틴 열거자입니다.</returns>
        private IEnumerator StopAfter(float durationSec, bool isEndCharacterStop)
        {
            if (durationSec > 0f)
                yield return new WaitForSeconds(durationSec);

            CharacterStop(isEndCharacterStop);
            ReleaseCrowdControlAirborneState();
            ResetAnimationState();
            _stopRoutine = null;
            TryStartNextQueuedCrowdControl();
        }

        /// <summary>
        /// 지정한 원본 오브젝트가 적용한 Crowd Control을 모든 활성 캐릭터에서 찾아 중단합니다.
        /// </summary>
        /// <param name="source">Crowd Control을 발생시킨 공격자 또는 원본 오브젝트입니다.</param>
        /// <param name="reason">Crowd Control 중단 요청 사유입니다.</param>
        /// <param name="isEndCharacterStop">중단 후 대상 캐릭터의 <see cref="CharacterBase.Stop(bool)"/>을 강제로 호출할지 여부입니다.</param>
        /// <returns>하나 이상의 Crowd Control을 중단했으면 <see langword="true"/>를 반환합니다.</returns>
        /// <remarks>
        /// 기본 공격 콤보 중 공격자가 피격되었을 때, 공격자가 몬스터에게 적용해 둔 넉백/넉업 같은 CC를
        /// 대상 캐릭터 쪽 컨트롤러에서 안전하게 회수하기 위한 표준 진입점입니다.
        /// </remarks>
        public static bool TryStopCrowdControlsBySource(GameObject source, CrowdControlStopReason reason, bool isEndCharacterStop = false)
        {
            if (source == null)
                return false;

            bool stoppedAny = false;
            for (int i = ActiveControllers.Count - 1; i >= 0; i--)
            {
                CharacterCrowdControlController controller = ActiveControllers[i];
                if (controller == null)
                {
                    ActiveControllers.RemoveAt(i);
                    continue;
                }

                if (!controller.IsControlledBySource(source))
                    continue;

                stoppedAny |= controller.TryStopCrowdControl(reason, isEndCharacterStop);
            }

            return stoppedAny;
        }

        /// <summary>
        /// 현재 컨트롤러의 활성 또는 예약된 Crowd Control이 지정한 원본 오브젝트에서 발생했는지 확인합니다.
        /// </summary>
        /// <param name="source">비교할 Crowd Control 원본 오브젝트입니다.</param>
        /// <returns>같은 원본 오브젝트가 적용한 Crowd Control이면 <see langword="true"/>를 반환합니다.</returns>
        private bool IsControlledBySource(GameObject source)
        {
            if (source == null)
                return false;

            return IsSameSourceOrChild(_activeSource, source) || IsSameSourceOrChild(_sequenceSource, source);
        }

        /// <summary>
        /// 두 원본 오브젝트가 같거나, 한쪽이 다른 쪽의 하위 오브젝트인지 확인합니다.
        /// </summary>
        /// <param name="candidate">현재 Crowd Control에 기록된 원본 오브젝트입니다.</param>
        /// <param name="source">중단 요청에서 전달된 기준 원본 오브젝트입니다.</param>
        /// <returns>동일 원본으로 볼 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        /// <remarks>
        /// 실제 데미지 메타데이터가 플레이어 루트가 아니라 공격 판정용 하위 오브젝트를 전달하는 경우도
        /// 같은 공격자에서 발생한 Crowd Control로 판단하기 위해 Transform 부모 관계까지 확인합니다.
        /// </remarks>
        private static bool IsSameSourceOrChild(GameObject candidate, GameObject source)
        {
            if (candidate == null || source == null)
                return false;

            if (candidate == source)
                return true;

            Transform candidateTransform = candidate.transform;
            Transform sourceTransform = source.transform;
            return candidateTransform.IsChildOf(sourceTransform) || sourceTransform.IsChildOf(candidateTransform);
        }

        /// <summary>
        /// 진행 중인 Crowd Control 또는 예약된 Crowd Control 시퀀스를 외부 요청으로 즉시 중단합니다.
        /// </summary>
        /// <param name="reason">Crowd Control 중단 요청 사유입니다.</param>
        /// <param name="isEndCharacterStop">중단 후 <see cref="CharacterBase.Stop(bool)"/>을 강제로 호출할지 여부입니다.</param>
        /// <returns>중단할 Crowd Control 상태가 존재하여 정리를 수행했으면 <see langword="true"/>를 반환합니다.</returns>
        /// <remarks>
        /// Control, Skill, Affect 같은 상위 계층은 CC 내부 모션/코루틴 구현을 직접 알지 않고
        /// 이 함수만 호출하여 Core가 소유한 Crowd Control 상태를 안전하게 정리합니다.
        /// </remarks>
        public bool TryStopCrowdControl(CrowdControlStopReason reason, bool isEndCharacterStop = false)
        {
            if (!HasActiveOrQueuedCrowdControl)
                return false;

            ForceStopInternal(clearSequence: true, isEndCharacterStop);
            return true;
        }

        /// <summary>
        /// 현재 실행 중이거나 예약된 Crowd Control이 하나라도 있는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 긴급 회복 스킬은 단순 컷신·UI 입력 잠금을 CC로 오인하지 않도록 이 상태를 사용합니다.
        /// </remarks>
        public bool HasActiveOrQueuedCrowdControl =>
            _isActive ||
            _activeCrowdControl != null ||
            _stopRoutine != null ||
            _isSequenceRunning ||
            _queuedCrowdControls.Count > 0;

        /// <summary>
        /// 진행 중인 CC를 즉시 중단하고, 모션/코루틴/상태를 강제 정리합니다.
        /// </summary>
        /// <remarks>
        /// 외부에 공개하지 않고, 새 CC 적용 전에 내부적으로 교체(replace) 처리할 때 사용합니다.
        /// </remarks>
        private void ForceStopInternal(bool clearSequence = true, bool isEndCharacterStop = false)
        {
            if (_stopRoutine != null)
            {
                StopCoroutine(_stopRoutine);
                _stopRoutine = null;
            }

            // 진행 중인 CC 모션을 강제 취소
            _motionController?.CancelMotion(MotionChannel.CrowdControl, reason: 200);

            // 진행 중인 CC를 강제 해제
            CharacterStop(isEndCharacterStop);
            ReleaseCrowdControlAirborneState();
            ResetAnimationState();
            _activeCrowdControl = null;
            _activeSource = null;
            _isActive = false;
            
            if (clearSequence)
                ClearQueuedSequence();
        }

        private void CharacterStop(bool isEndCharacterStop)
        {
            if (!isEndCharacterStop) return;
            _character?.Stop(isForce: true);
        }

        private void OnMotionWallImpacted(MotionWallImpactInfo wallImpactInfo)
        {
            if (_activeCrowdControl == null)
                return;

            if (wallImpactInfo.Channel != MotionChannel.CrowdControl)
                return;

            if (!_activeCrowdControl.IsStopOnWall)
                return;

            if (_activeCrowdControl.Type != CrowdControlConstants.Type.KnockBack)
                return;

            if (!ShouldTriggerWallImpactReaction(_activeCrowdControl, wallImpactInfo))
                return;

            CrowdControlRuntimeData followUp = BuildWallImpactFollowUp(_activeCrowdControl, wallImpactInfo);
            if (followUp == null)
                return;

            GameObject source = _activeSource;
            ForceStopInternal(clearSequence: false, false);
            ApplyCrowdControlInternal(
                followUp,
                source,
                forceReplaceCurrent: false,
                forceRefreshAnimation: _forceRefreshAnimationOnCurrentCrowdControl);
        }

        private static bool ShouldTriggerWallImpactReaction(CrowdControlRuntimeData crowdControl, MotionWallImpactInfo wallImpactInfo)
        {
            if (crowdControl == null)
                return false;

            if (!crowdControl.UseWallImpactReaction)
                return false;

            if (crowdControl.WallImpactCrowdControlUid <= 0)
                return false;

            return wallImpactInfo.ImpactSpeed >= Mathf.Max(0f, crowdControl.WallImpactMinSpeed);
        }

        private CrowdControlRuntimeData BuildWallImpactFollowUp(CrowdControlRuntimeData crowdControl, MotionWallImpactInfo wallImpactInfo)
        {
            if (crowdControl == null)
                return null;

            if (crowdControl.WallImpactCrowdControlUid <= 0)
                return null;

            var runtime = TableLoaderManager.Instance != null
                ? TableLoaderManager.Instance.GetCrowdControlRuntimeData(crowdControl.WallImpactCrowdControlUid, logIfMissing: false)
                : null;
            if (runtime == null)
                return null;
            runtime.IsEndCharacterStop = crowdControl.IsEndCharacterStop;

            CrowdControlRuntimeData cloned = runtime.Clone();
            Vector2 sourceDirection = wallImpactInfo.RequestedDelta.sqrMagnitude > Epsilon
                ? wallImpactInfo.RequestedDelta.normalized
                : ResolveFacingDirection();
            Vector2 reflectionDirection = Vector2.Reflect(sourceDirection, wallImpactInfo.Normal);
            if (reflectionDirection.sqrMagnitude <= Epsilon)
                reflectionDirection = ResolveFacingDirection();

            if (reflectionDirection.y < 0f)
                reflectionDirection.y = Mathf.Abs(reflectionDirection.y);

            reflectionDirection.Normalize();
            cloned.DirectionType = CrowdControlConstants.DirectionType.Fixed;
            cloned.FixedDirectionX = reflectionDirection.x;
            cloned.FixedDirectionY = reflectionDirection.y;
            return cloned;
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

        internal bool IsCurrentlyGrounded(float maxGroundDistance)
        {
            if (maxGroundDistance < 0f)
                maxGroundDistance = 0f;

            return CharacterGroundProbeUtility.IsCurrentlyGrounded(this, _rigidbody2D, GetGroundProbeMask(), maxGroundDistance);
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

        internal bool TryProbeGroundBelow(out float groundY, out float bottomY)
        {
            float probeDistance = Mathf.Max(KnockUpLandingFinalSnapDistance, KnockUpLandingTriggerDistance);
            return TryProbeGroundBelow(probeDistance, out groundY, out bottomY);
        }
        internal bool TryProbeGroundBelow(float maxGroundDistance, out float groundY, out float bottomY)
        {
            return CharacterGroundProbeUtility.TryProbeGroundBelow(this, _rigidbody2D, maxGroundDistance, GetGroundProbeMask(), out groundY, out bottomY);
        }
        internal void SnapCharacterBottomToGround(float groundY, float currentBottomY)
        {
            float deltaY = groundY - currentBottomY;
            if (Mathf.Abs(deltaY) <= Epsilon)
                return;

            Vector2 currentPos = _rigidbody2D != null ? _rigidbody2D.position : (Vector2)transform.position;
            MoveTo(currentPos + new Vector2(0f, deltaY));
        }


        private string ResolveEndAnimationName(CrowdControlRuntimeData crowdControl)
        {
            return GetHandler(crowdControl)?.ResolveEndAnimationName(this, crowdControl);
        }

        private void ResetAnimationState()
        {
            StopAnimationEaseRoutine(resetPlaybackTimeScale: true);
            _currentStaggerAnimationName = null;
            _currentPhaseAnimationName = null;
            _currentAirborneAnimationPhase = CrowdControlAirborneAnimationPhase.None;
            _forceRefreshAnimationOnCurrentCrowdControl = false;
        }


        internal ICharacterAnimationController AnimationController => _character?.CharacterAnimationController;
        internal string CurrentStaggerAnimationName => _currentStaggerAnimationName;
        /// <summary>
        /// 현재 CrowdControl 적용 사이클에 애니메이션 강제 재시작 정책이 활성화되었는지 여부입니다.
        /// </summary>
        internal bool ForceRefreshAnimationOnCurrentCrowdControl => _forceRefreshAnimationOnCurrentCrowdControl;
        internal string CurrentPhaseAnimationName
        {
            get => _currentPhaseAnimationName;
            set => _currentPhaseAnimationName = value;
        }

        internal CrowdControlAirborneAnimationPhase CurrentAirborneAnimationPhase
        {
            get => _currentAirborneAnimationPhase;
            set => _currentAirborneAnimationPhase = value;
        }

        internal bool TryGetCrowdControlMotionProgress(out float progress01)
        {
            progress01 = 0f;
            return _motionController != null && _motionController.TryGetMotionProgress(MotionChannel.CrowdControl, out progress01);
        }

        internal void CancelCrowdControlMotion(int reason)
        {
            _motionController?.CancelMotion(MotionChannel.CrowdControl, reason);
        }

        /// <summary>
        /// 착지 기반 Crowd Control이 Arc 모션 완료 후에도 공중에 남아있을 때 전용 하강 모션을 시작합니다.
        /// </summary>
        /// <param name="fallSpeed">하강 속도입니다.</param>
        /// <param name="stopOnWall">하강 중 벽 충돌 시 모션을 중단할지 여부입니다.</param>
        /// <returns>하강 모션을 시작했으면 <see langword="true"/>입니다.</returns>
        /// <remarks>
        /// KnockUp의 FallTime은 Arc 보간 시간이므로 실제 지형 높이가 맞지 않으면 모션 종료 후 공중에 남을 수 있습니다.
        /// 이 경우 Unity 중력에만 의존하지 않고 동일한 CrowdControl 채널에서 아래 방향 모션을 이어서 실행하여,
        /// Kinematic Rigidbody 캐릭터도 Ground Probe 기반 착지까지 안정적으로 내려오게 합니다.
        /// </remarks>
        internal bool TryStartCrowdControlLandingFall(float fallSpeed, bool stopOnWall)
        {
            if (_motionController == null)
                return false;

            float safeFallSpeed = Mathf.Max(Epsilon, fallSpeed);
            var request = new MotionRequest(
                MotionChannel.CrowdControl,
                MotionKind.KnockDownAir,
                Vector2.down,
                durationSeconds: 0f,
                distance: 0f,
                easeType: Easing.EaseType.Linear,
                stopAtEnd: true,
                useMovePosition: true,
                allowReplace: true,
                fallSpeed: safeFallSpeed,
                stopOnWall: stopOnWall);

            return _motionController.TryStartMotion(in request);
        }

        private ICrowdControlHandler GetHandler(CrowdControlRuntimeData crowdControl)
        {
            if (crowdControl == null || _handlers == null)
                return null;

            _handlers.TryGetValue(crowdControl.Type, out var handler);
            return handler;
        }

        private void TryStartNextQueuedCrowdControl()
        {
            if (_activeCrowdControl != null)
            {
                _isActive = false;
                return;   
            }
                

            if (!_isSequenceRunning)
            {
                _isActive = false;
                return;   
            }

            if (_sequenceTarget != null && _character != null && _sequenceTarget != _character.gameObject)
            {
                _isActive = false;
                ClearQueuedSequence();
                return;
            }

            while (_queuedCrowdControls.Count > 0)
            {
                var next = _queuedCrowdControls.Dequeue();
                if (next.CrowdControl == null)
                    continue;

                ApplyCrowdControlInternal(
                    next.CrowdControl,
                    _sequenceSource,
                    forceReplaceCurrent: false,
                    forceRefreshAnimation: false);
                if (_activeCrowdControl != null)
                    return;
            }

            ClearQueuedSequence();
            _isActive = false;
        }

        private bool HasCrowdControlStateToReplace()
        {
            if (_activeCrowdControl != null)
                return true;

            if (_stopRoutine != null)
                return true;

            if (_motionController != null && _motionController.IsPlaying(MotionChannel.CrowdControl))
                return true;

            if (_isSequenceRunning)
                return true;

            return _queuedCrowdControls.Count > 0;
        }

        private void ClearQueuedSequence()
        {
            _queuedCrowdControls.Clear();
            _isSequenceRunning = false;
            _sequenceSource = null;
            _sequenceTarget = null;
        }

        public void ApplyCrowdControlSequenceByUid(IReadOnlyList<int> crowdControlUids, GameObject source, GameObject target, bool isEndCharacterStop)
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

            ApplyCrowdControlSequence(runtimeList, source, target, isEndCharacterStop);
        }

        public void ApplyCrowdControlSequence(IReadOnlyList<CrowdControlRuntimeData> crowdControls, GameObject source, GameObject target, bool isEndCharacterStop)
        {
            if (crowdControls == null || crowdControls.Count == 0)
                return;

            if (HasCrowdControlStateToReplace())
                ForceStopInternal(isEndCharacterStop: true);

            _isSequenceRunning = true;
            _sequenceSource = source;
            _sequenceTarget = target;

            for (int i = 0; i < crowdControls.Count; i++)
            {
                var crowdControl = crowdControls[i];
                if (crowdControl == null)
                    continue;

                crowdControl.IsEndCharacterStop = false;
                if (i == crowdControls.Count - 1)
                    crowdControl.IsEndCharacterStop = isEndCharacterStop;
                _queuedCrowdControls.Enqueue(new QueuedCrowdControl(crowdControl));
            }

            TryStartNextQueuedCrowdControl();
        }

        /// <summary>
        /// 몬스터가 풀로 반납되거나 다시 대여될 때 Crowd Control 런타임 상태를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// <see cref="ForceStopInternal(bool, bool)"/> 내부에서 모션, 코루틴, 애니메이션 보정 상태를 함께 정리하므로
        /// 동일한 프레임에 애니메이션 초기화가 중복 실행되지 않도록 별도의 <c>ResetAnimationState</c> 호출은 수행하지 않습니다.
        /// </remarks>
        public void ResetForPoolReturn()
        {
            ForceStopInternal(clearSequence: true);
        }

        /// <summary>
        /// 몬스터가 풀에서 다시 대여될 때 Crowd Control 잔여 상태를 제거합니다.
        /// </summary>
        /// <param name="owner">풀에서 대여되는 몬스터입니다.</param>
        public void OnPoolRent(Monster owner)
        {
            ResetForPoolReturn();
        }

        /// <summary>
        /// 몬스터가 풀로 반환될 때 Crowd Control 잔여 상태를 제거합니다.
        /// </summary>
        /// <param name="owner">풀로 반환되는 몬스터입니다.</param>
        public void OnPoolReturn(Monster owner)
        {
            ResetForPoolReturn();
        }

        /// <summary>
        /// CC UID를 통해 테이블에서 데이터를 조회한 뒤 CrowdControl을 적용합니다.
        /// </summary>
        /// <param name="crowdControlUid">조회할 CC 테이블 UID입니다.</param>
        /// <param name="source">방향 계산에 사용할 공격/발생 원본 오브젝트입니다(없을 수 있음).</param>
        /// <param name="isEndCharacterStop">종료 시 Character.Stop 강제 호출 여부입니다.</param>
        public void ApplyCrowdControlByUid(int crowdControlUid, GameObject source, bool isEndCharacterStop = false)
        {
            ApplyCrowdControlByUid(crowdControlUid, source, isEndCharacterStop, forceRefreshAnimation: false);
        }

        /// <summary>
        /// CC UID를 통해 테이블에서 데이터를 조회한 뒤 CrowdControl을 적용합니다.
        /// </summary>
        /// <param name="crowdControlUid">조회할 CC 테이블 UID입니다.</param>
        /// <param name="source">방향 계산에 사용할 공격/발생 원본 오브젝트입니다(없을 수 있음).</param>
        /// <param name="isEndCharacterStop">종료 시 Character.Stop 강제 호출 여부입니다.</param>
        /// <param name="forceRefreshAnimation">
        /// <see langword="true"/>면 같은 상태명 애니메이션도 첫 프레임부터 재시작합니다.
        /// </param>
        public void ApplyCrowdControlByUid(int crowdControlUid, GameObject source, bool isEndCharacterStop, bool forceRefreshAnimation)
        {
            CrowdControlRuntimeData info = TableLoaderManager.Instance != null
                ? TableLoaderManager.Instance.GetCrowdControlRuntimeData(crowdControlUid, logIfMissing: false)
                : null;
            if (info == null) return;
            info.IsEndCharacterStop = isEndCharacterStop;
            ApplyCrowdControl(info, source, forceRefreshAnimation);
        }
    }
}
