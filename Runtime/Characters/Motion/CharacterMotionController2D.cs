using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 2D 캐릭터의 공용 모션 이동(전진/대시/러시/CC 이동 등)을 Distance 기반으로 처리하는 컨트롤러입니다.
    /// </summary>
    /// <remarks>
    /// - 입력(플레이어 조작)과 무관하게 동작하도록 설계되어 플레이어/몬스터 공용으로 사용할 수 있습니다. <br/>
    /// - 모션은 채널(<see cref="MotionChannel"/>) 단위로 관리되며, 기본적으로 CrowdControl 채널이 Skill 채널보다 우선합니다. <br/>
    /// - 이동 계산 로직은 Solver(<see cref="IMotionSolver"/>)로 분리되어, 모션 종류가 늘어나도 컨트롤러 복잡도가 증가하지 않도록 설계합니다. <br/>
    /// - <see cref="Rigidbody2D"/>가 Kinematic이면 <see cref="Rigidbody2D.MovePosition"/> 기반 이동을 권장합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterMotionController2D : MonoBehaviour, ICharacterMotionController
    {
        private const float DefaultWallCollisionSkin = 0.02f;

        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private CharacterPhysicsOverrideController physicsOverrideController;
        [SerializeField] private CharacterHitStopController hitStopController;
        [SerializeField] private CharacterBase character;
        [SerializeField] private CharacterCollisionController collisionController;

        private MotionState _skill;
        private MotionState _crowdControl;

        private readonly RaycastHit2D[] _wallCastHits = new RaycastHit2D[8];

        private const float GravityDisableValue = 0f;

        public event System.Action<MotionWallImpactInfo> WallImpacted;

        private void Reset()
        {
            rb = GetComponentInParent<Rigidbody2D>();
            physicsOverrideController = GetComponentInParent<CharacterPhysicsOverrideController>();
            hitStopController = GetComponentInParent<CharacterHitStopController>();
            character = GetComponentInParent<CharacterBase>();
            collisionController = GetComponentInParent<CharacterCollisionController>();
        }

        private void Awake()
        {
            if (rb == null)
                rb = GetComponentInParent<Rigidbody2D>();

            if (physicsOverrideController == null)
                physicsOverrideController = GetComponentInParent<CharacterPhysicsOverrideController>();

            if (physicsOverrideController == null)
                physicsOverrideController = gameObject.AddComponent<CharacterPhysicsOverrideController>();

            if (hitStopController == null)
                hitStopController = GetComponentInParent<CharacterHitStopController>();

            if (character == null)
                character = GetComponentInParent<CharacterBase>();

            if (collisionController == null)
                collisionController = GetComponentInParent<CharacterCollisionController>();
        }

        private void OnDisable()
        {
            RestoreMotionPhysics(ref _crowdControl, zeroVerticalVelocity: true);
            _crowdControl.Stop();
            RestoreMotionPhysics(ref _skill, zeroVerticalVelocity: true);
            _skill.Stop();
        }

        /// <summary>
        /// 요청된 모션을 시작합니다.
        /// </summary>
        public bool TryStartMotion(in MotionRequest request)
        {
            if (rb == null) return false;
            bool hasGroundSlamTravel = request.Kind == MotionKind.GroundSlam && (request.StartPosition - request.TargetPosition).sqrMagnitude > 1e-8f;
            bool hasPositionHold = request.Kind == MotionKind.PositionHold;
            bool hasKnockDownAirTravel = request.Kind == MotionKind.KnockDownAir
                && (request.Distance > 0f || request.ArcHeight > 0f || request.DurationSeconds > 0f || request.FallSpeed > 0f);
            if (request.Distance <= 0f && request.HoldSecondsAfter <= 0f && request.ArcHeight <= 0f && !hasGroundSlamTravel && !hasPositionHold && !hasKnockDownAirTravel) return false;

            ref MotionState state = ref GetStateRef(request.Channel);

            if (state.IsPlaying && !request.AllowReplace)
            {
                GcLogger.LogWarning($"Motion is already playing on channel {request.Channel}.");
                return false;                
            }

            if (state.IsPlaying)
            {
                bool zeroVerticalVelocity = request.Kind == MotionKind.PositionHold
                    || state.Kind == MotionKind.PositionHold
                    || state.Kind == MotionKind.Arc
                    || state.Kind == MotionKind.GroundSlam
                    || state.Kind == MotionKind.KnockDownAir;

                RestoreMotionPhysics(ref state, zeroVerticalVelocity: zeroVerticalVelocity);

                if (request.Kind == MotionKind.PositionHold)
                {
                    ZeroDynamicVelocity();
                }

                state.Stop();
            }

            MotionRequest requestToUse = request;
            if (request.Kind == MotionKind.GroundSlam || request.Kind == MotionKind.PositionHold || request.Kind == MotionKind.KnockDownAir)
            {
                requestToUse = new MotionRequest(
                    request.Channel,
                    request.Kind,
                    request.Direction,
                    request.DurationSeconds,
                    request.Distance,
                    request.EaseType,
                    stopAtEnd: request.StopAtEnd,
                    useMovePosition: request.UseMovePosition,
                    allowReplace: request.AllowReplace,
                    holdSecondsAfter: request.HoldSecondsAfter,
                    arcHeight: request.ArcHeight,
                    arcMode: request.ArcMode,
                    arcRiseEaseType: request.ArcRiseEaseType,
                    arcFallEaseType: request.ArcFallEaseType,
                    arcApexHoldNormalized: request.ArcApexHoldNormalized,
                    arcRiseRatioNormalized: request.ArcRiseRatioNormalized,
                    arcFallRatioNormalized: request.ArcFallRatioNormalized,
                    fallSpeed: request.FallSpeed,
                    startPosition: rb.position,
                    targetPosition: request.TargetPosition,
                    groundSnapDistance: request.GroundSnapDistance,
                    stopOnWall: request.StopOnWall,
                    wallCollisionSkin: request.WallCollisionSkin,
                    collisionPolicy: request.CollisionPolicy,
                    collisionTarget: request.CollisionTarget,
                    bodyCollisionPolicy: request.BodyCollisionPolicy,
                    bodySeparationMultiplier: request.BodySeparationMultiplier,
                    bodySeparationDuration: request.BodySeparationDuration,
                    positionConstraint: request.PositionConstraint);
            }

            state.Start(requestToUse);
            PrepareForMotionStart(ref state);
            return true;
        }

        /// <summary>
        /// 지정한 채널의 모션을 중단합니다.
        /// </summary>
        public void CancelMotion(MotionChannel channel, int reason = 0)
        {
            ref MotionState state = ref GetStateRef(channel);
            if (!state.IsPlaying) return;

            bool stopAtEnd = state.StopAtEnd;
            RestoreMotionPhysics(ref state, zeroVerticalVelocity: true);
            state.Stop();

            // velocity 기반 구현을 사용하는 경우를 대비해 정지 정책 제공
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                if (state.Kind == MotionKind.PositionHold)
                {
                    rb.SetLinearVelocity(Vector2.zero);
                }
                else if (stopAtEnd)
                {
                    rb.SetLinearVelocity(new Vector2(0f, rb.GetLinearVelocity().y));
                }
            }
        }

        /// <summary>
        /// 지정한 채널의 모션이 재생 중인지 확인합니다.
        /// </summary>
        public bool IsPlaying(MotionChannel channel)
        {
            ref MotionState state = ref GetStateRef(channel);
            return state.IsPlaying;
        }

        public bool TryGetMotionProgress(MotionChannel channel, out float progress01)
        {
            ref MotionState state = ref GetStateRef(channel);
            return state.TryGetProgress(out progress01);
        }

        private void FixedUpdate()
        {
            if (rb == null)
            {
                _skill.Stop();
                _crowdControl.Stop();
                return;
            }

            if (hitStopController == null)
                hitStopController = GetComponentInParent<CharacterHitStopController>();

            if (hitStopController != null && hitStopController.IsActive)
                return;

            float dt = Time.fixedDeltaTime;

            // CrowdControl 모션이 Skill 모션보다 우선합니다.
            if (_crowdControl.IsPlaying)
            {
                Tick(ref _crowdControl, dt);
                return;
            }

            if (_skill.IsPlaying)
            {
                Tick(ref _skill, dt);
            }
        }

        /// <summary>
        /// 모션 상태를 한 프레임 진행시키고, 증분 이동을 적용합니다.
        /// </summary>
        private void Tick(ref MotionState state, float dt)
        {
            if (!state.IsPlaying) return;

            if (state.Kind == MotionKind.PositionHold)
            {
                TickPositionHold(ref state, dt);
                return;
            }

            if (state.Kind == MotionKind.KnockDownAir)
            {
                TickKnockDownAir(ref state, dt);
                return;
            }

            state.Solver.Tick(ref state, dt, out Vector2 delta);
            ApplyDelta(ref state, delta, state.UseMovePosition);

            if (state.WasWallImpacted)
            {
                bool stopAtEnd = state.StopAtEnd;
                RestoreMotionPhysics(ref state, zeroVerticalVelocity: true);
                state.Stop();
                StopVelocityIfNeeded(stopAtEnd, state.Kind);
                return;
            }

            if (state.IsComplete)
            {
                bool stopAtEnd = state.StopAtEnd;
                RestoreMotionPhysics(ref state, zeroVerticalVelocity: true);
                state.Stop();
                StopVelocityIfNeeded(stopAtEnd, state.Kind);
            }
        }

        private void TickKnockDownAir(ref MotionState state, float dt)
        {
            if (rb == null)
            {
                state.Stop();
                return;
            }

            float previousElapsed = state.Elapsed;
            state.Elapsed += dt;

            float riseRatio = Mathf.Max(0f, state.ArcRiseRatioNormalized);
            float airRatio = Mathf.Max(0f, state.ArcApexHoldNormalized);
            float totalRatio = riseRatio + airRatio;
            float normalizedRise = totalRatio > 1e-6f ? (riseRatio / totalRatio) : 1f;

            bool isFallPhase = state.Duration <= 1e-6f || previousElapsed >= state.Duration;
            if (!isFallPhase)
            {
                float clampedElapsed = Mathf.Min(state.Elapsed, state.Duration);
                float t = state.Duration <= 1e-6f ? 1f : Mathf.Clamp01(clampedElapsed / state.Duration);
                float eased = Easing.Apply(t, state.EaseType);

                float targetDistance = state.Distance * eased;
                float deltaDistance = targetDistance - state.MovedDistance;
                state.MovedDistance = targetDistance;

                Vector2 delta = state.Direction * deltaDistance;

                float height = Mathf.Max(0f, state.ArcHeight);
                if (height > 0f)
                {
                    float y;
                    if (normalizedRise <= 1e-6f)
                    {
                        y = height;
                    }
                    else if (t < normalizedRise)
                    {
                        float riseT = Mathf.Clamp01(t / normalizedRise);
                        float riseEased = Easing.Apply(riseT, state.ArcRiseEaseType);
                        y = height * riseEased;
                    }
                    else
                    {
                        y = height;
                    }

                    float deltaY = y - state.AppliedArcY;
                    state.AppliedArcY = y;
                    delta += Vector2.up * deltaY;
                }

                ApplyDelta(ref state, delta, state.UseMovePosition);
                if (state.WasWallImpacted)
                {
                    bool stopAtEnd = state.StopAtEnd;
                    RestoreMotionPhysics(ref state, zeroVerticalVelocity: true);
                    state.Stop();
                    StopVelocityIfNeeded(stopAtEnd, state.Kind);
                    return;
                }

                state.CurrentPosition = rb.position;
                return;
            }

            float fallSpeed = Mathf.Max(0f, state.FallSpeed);
            if (fallSpeed <= 1e-6f)
                fallSpeed = Mathf.Max(1f, state.ArcHeight);

            float fallStep = fallSpeed * dt;
            if (fallStep <= 1e-6f)
                fallStep = 0.01f;

            if (CharacterGroundProbeUtility.TryProbeGroundBelow(this, rb, fallStep, out float groundY, out float bottomY))
            {
                float distanceToGround = bottomY - groundY;
                if (distanceToGround >= -CharacterGroundProbeUtility.ProbeUpOffset && distanceToGround <= fallStep)
                {
                    Vector2 currentPos = rb.position;
                    float deltaY = groundY - bottomY;
                    Vector2 snapDelta = ResolvePositionConstraint(ref state, new Vector2(0f, deltaY));
                    Vector2 snapped = currentPos + snapDelta;
                    if (state.UseMovePosition || rb.bodyType != RigidbodyType2D.Dynamic)
                    {
                        rb.MovePosition(snapped);
                    }
                    else
                    {
                        rb.position = snapped;
                        ZeroDynamicVelocity();
                    }

                    state.CurrentPosition = snapped;
                    state.AppliedArcY = 0f;
                    RequestCharacterBodyMotionSeparation(ref state);
                    state.MarkComplete();
                    bool stopAtEnd = state.StopAtEnd;
                    RestoreMotionPhysics(ref state, zeroVerticalVelocity: true);
                    state.Stop();
                    StopVelocityIfNeeded(stopAtEnd, state.Kind);
                    return;
                }
            }

            Vector2 fallDelta = Vector2.down * fallStep;
            ApplyDelta(ref state, fallDelta, state.UseMovePosition);
            if (state.WasWallImpacted)
            {
                bool stopAtEnd = state.StopAtEnd;
                RestoreMotionPhysics(ref state, zeroVerticalVelocity: true);
                state.Stop();
                StopVelocityIfNeeded(stopAtEnd, state.Kind);
                return;
            }

            state.CurrentPosition = rb.position;
            state.AppliedArcY = Mathf.Max(0f, state.AppliedArcY - fallStep);
        }

        private void TickPositionHold(ref MotionState state, float dt)
        {
            if (rb == null)
            {
                state.Stop();
                return;
            }

            state.Elapsed += dt;

            Vector2 desiredDelta = state.StartPosition - rb.position;
            Vector2 desired = rb.position + ResolvePositionConstraint(ref state, desiredDelta);
            if (state.UseMovePosition || rb.bodyType != RigidbodyType2D.Dynamic)
            {
                rb.MovePosition(desired);
            }
            else
            {
                rb.position = desired;
            }

            ZeroDynamicVelocity();
            state.CurrentPosition = desired;

            // duration <= 0 이면 무기한 hold
            if (state.Duration > 1e-6f && state.Elapsed >= state.Duration)
            {
                state.MarkComplete();
                bool stopAtEnd = state.StopAtEnd;
                RestoreMotionPhysics(ref state, zeroVerticalVelocity: true);
                state.Stop();
                StopVelocityIfNeeded(stopAtEnd, state.Kind);
            }
        }

        /// <summary>
        /// 계산된 증분 이동량을 <see cref="Rigidbody2D"/>에 적용하고, 모션 Body 충돌 보정을 요청합니다.
        /// </summary>
        private void ApplyDelta(ref MotionState state, Vector2 delta, bool useMovePosition)
        {
            if (delta.sqrMagnitude <= 1e-12f) return;

            Vector2 appliedDelta = delta;
            if (state.StopOnWall && TryResolveWallImpact(state, delta, out MotionWallImpactInfo wallImpactInfo, out appliedDelta))
            {
                state.WasWallImpacted = true;
                state.LastWallImpact = wallImpactInfo;
                WallImpacted?.Invoke(wallImpactInfo);
            }

            if (appliedDelta.sqrMagnitude <= 1e-12f)
                return;

            appliedDelta = ResolveCharacterBodyMotionDelta(ref state, appliedDelta);
            if (appliedDelta.sqrMagnitude <= 1e-12f)
                return;

            appliedDelta = ResolvePositionConstraint(ref state, appliedDelta);
            if (appliedDelta.sqrMagnitude <= 1e-12f)
                return;

            ApplyRigidbodyDelta(appliedDelta, useMovePosition);
            RequestCharacterBodyMotionSeparation(ref state);
        }

        /// <summary>
        /// 모션 요청에 등록된 위치 제약을 적용하고, 제한된 축의 Dynamic Rigidbody 속도를 정리합니다.
        /// </summary>
        /// <param name="state">현재 실행 중인 모션 상태입니다.</param>
        /// <param name="requestedDelta">충돌 정책 적용 이후의 요청 이동량입니다.</param>
        /// <returns>위치 제약이 반영된 최종 이동량입니다.</returns>
        private Vector2 ResolvePositionConstraint(ref MotionState state, Vector2 requestedDelta)
        {
            if (rb == null || state.PositionConstraint == null)
                return requestedDelta;

            if (!state.PositionConstraint.TryConstrain(
                    rb.position,
                    requestedDelta,
                    out MotionPositionConstraintResult result))
            {
                return requestedDelta;
            }

            StopConstrainedVelocity(result);
            return result.AppliedDelta;
        }

        /// <summary>
        /// 화면 또는 기타 위치 제약에 의해 차단된 축의 잔여 속도를 제거합니다.
        /// </summary>
        /// <param name="result">위치 제약 축 정보가 포함된 결과입니다.</param>
        private void StopConstrainedVelocity(MotionPositionConstraintResult result)
        {
            if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic ||
                (!result.IsHorizontalConstrained && !result.IsVerticalConstrained))
            {
                return;
            }

            Vector2 velocity = rb.GetLinearVelocity();
            if (result.IsHorizontalConstrained)
                velocity.x = 0f;
            if (result.IsVerticalConstrained)
                velocity.y = 0f;
            rb.SetLinearVelocity(velocity);
        }

        /// <summary>
        /// 모션 전용 Body 충돌 정책에 따라 이동 적용 전 증분 이동량을 보정합니다.
        /// </summary>
        /// <param name="state">현재 모션 상태입니다.</param>
        /// <param name="delta">벽 충돌 보정 이후의 요청 이동량입니다.</param>
        /// <returns>캐릭터 Body 충돌 정책까지 반영한 이동량입니다.</returns>
        private Vector2 ResolveCharacterBodyMotionDelta(ref MotionState state, Vector2 delta)
        {
            CharacterCollisionController controller = ResolveCharacterCollisionController();
            if (controller == null)
                return delta;

            bool canMove = controller.TryResolveMotionMove(
                new Vector3(delta.x, delta.y, 0f),
                state.Channel,
                state.BodyCollisionPolicy,
                out Vector3 resolvedDelta);

            if (!canMove)
                return Vector2.zero;

            return new Vector2(resolvedDelta.x, resolvedDelta.y);
        }

        /// <summary>
        /// Rigidbody2D의 타입과 모션 설정에 맞춰 최종 이동량을 적용합니다.
        /// </summary>
        /// <param name="appliedDelta">실제로 적용할 월드 기준 이동량입니다.</param>
        /// <param name="useMovePosition">Kinematic Rigidbody에서 MovePosition을 사용할지 여부입니다.</param>
        private void ApplyRigidbodyDelta(Vector2 appliedDelta, bool useMovePosition)
        {
            // Kinematic: MovePosition 권장
            if (useMovePosition && rb.bodyType == RigidbodyType2D.Kinematic)
            {
                rb.MovePosition(rb.position + appliedDelta);
                return;
            }

            // Dynamic 또는 정책상 velocity 사용
            float dt = Time.fixedDeltaTime;
            float vx = dt > 1e-6f ? (appliedDelta.x / dt) : 0f;
            float vy = dt > 1e-6f ? (appliedDelta.y / dt) : rb.GetLinearVelocity().y;
            rb.SetLinearVelocity(new Vector2(vx, vy));
        }

        /// <summary>
        /// 모션 이동 후 일정 시간 동안 캐릭터 Body 겹침 해소를 반복하도록 요청합니다.
        /// </summary>
        /// <param name="state">현재 모션 상태입니다.</param>
        private void RequestCharacterBodyMotionSeparation(ref MotionState state)
        {
            CharacterCollisionController controller = ResolveCharacterCollisionController();
            if (controller == null)
                return;

            controller.RequestMotionSeparation(
                state.Channel,
                state.BodyCollisionPolicy,
                state.BodySeparationDuration,
                state.BodySeparationMultiplier);
        }

        /// <summary>
        /// 캐릭터 충돌 컨트롤러 참조를 지연 해석합니다.
        /// </summary>
        /// <returns>현재 캐릭터의 충돌 컨트롤러입니다. 찾을 수 없으면 null입니다.</returns>
        private CharacterCollisionController ResolveCharacterCollisionController()
        {
            if (collisionController != null)
                return collisionController;

            if (character == null)
                character = GetComponentInParent<CharacterBase>();

            if (character != null)
            {
                collisionController = character.CollisionController;
                if (collisionController != null)
                    return collisionController;

                character.RefreshCharacterBodyCollision();
                collisionController = character.CollisionController;
                if (collisionController != null)
                    return collisionController;
            }

            collisionController = GetComponentInParent<CharacterCollisionController>();
            return collisionController;
        }

        private bool TryResolveWallImpact(MotionState state, Vector2 requestedDelta, out MotionWallImpactInfo impactInfo, out Vector2 appliedDelta)
        {
            impactInfo = default;
            appliedDelta = requestedDelta;

            if (rb == null)
                return false;

            float distance = requestedDelta.magnitude;
            if (distance <= 1e-6f)
                return false;

            int wallMask = GetWallProbeMask();
            if (wallMask == 0)
                return false;

            ContactFilter2D filter = default;
            filter.useLayerMask = true;
            filter.layerMask = wallMask;
            filter.useTriggers = false;

            int hitCount = rb.Cast(requestedDelta.normalized, filter, _wallCastHits, distance + Mathf.Max(DefaultWallCollisionSkin, state.WallCollisionSkin));
            if (hitCount <= 0)
                return false;

            RaycastHit2D bestHit = default;
            bool hasHit = false;
            float minDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = _wallCastHits[i];
                if (hit.collider == null)
                    continue;

                if (!hasHit || hit.distance < minDistance)
                {
                    minDistance = hit.distance;
                    bestHit = hit;
                    hasHit = true;
                }
            }

            if (!hasHit)
                return false;

            float safeDistance = Mathf.Max(0f, bestHit.distance - Mathf.Max(DefaultWallCollisionSkin, state.WallCollisionSkin));
            Vector2 normalized = requestedDelta.normalized;
            appliedDelta = normalized * Mathf.Min(distance, safeDistance);

            float dt = Time.fixedDeltaTime;
            float impactSpeed = dt > 1e-6f ? (requestedDelta.magnitude / dt) : 0f;
            impactInfo = new MotionWallImpactInfo(
                state.Channel,
                state.Kind,
                bestHit.point,
                bestHit.normal,
                impactSpeed,
                requestedDelta,
                bestHit.collider);
            return true;
        }

        private static int GetWallProbeMask()
        {
            string wallLayerName = ConfigLayer.GetValue(ConfigLayer.Keys.TileMapGround);
            if (string.IsNullOrWhiteSpace(wallLayerName))
                return 0;

            return LayerMask.GetMask(wallLayerName);
        }

        private void PrepareForMotionStart(ref MotionState state)
        {
            if (rb == null)
                return;

            if (state.CollisionPolicy == MotionCollisionPolicy.IgnoreTargetCharacter && state.CollisionTarget != null)
            {
                state.CollisionIgnoreScope = MotionCollisionIgnoreScope2D.Create(gameObject, state.CollisionTarget);
            }

            if (rb.bodyType != RigidbodyType2D.Dynamic)
                return;

            if (state.Kind != MotionKind.Arc && state.Kind != MotionKind.GroundSlam && state.Kind != MotionKind.PositionHold && state.Kind != MotionKind.KnockDownAir)
                return;

            state.IsGravitySuspended = true;

            if (physicsOverrideController != null)
            {
                int priority = state.Channel == MotionChannel.CrowdControl
                    ? CharacterPhysicsOverridePriority.MotionCrowdControl
                    : CharacterPhysicsOverridePriority.MotionSkill;

                state.GravityOverrideHandle = physicsOverrideController.AcquireGravityOverride(
                    ownerKey: this,
                    lifecycleOwner: this,
                    channel: state.Channel == MotionChannel.CrowdControl
                        ? CharacterPhysicsOverrideChannel.CrowdControl
                        : CharacterPhysicsOverrideChannel.Skill,
                    priority: priority,
                    gravityScale: GravityDisableValue,
                    reason: $"Motion:{state.Channel}:{state.Kind}");

                state.HasSavedGravityScale = false;
                state.SavedGravityScale = 0f;
            }
            else
            {
                state.HasSavedGravityScale = true;
                state.SavedGravityScale = rb.gravityScale;
                rb.gravityScale = GravityDisableValue;
            }
            if (state.Kind == MotionKind.PositionHold)
            {
                ZeroDynamicVelocity();
                return;
            }

            if (state.Kind == MotionKind.KnockDownAir)
            {
                ZeroDynamicVelocity();
                return;
            }

            ZeroDynamicVerticalVelocity();
        }

        private void RestoreMotionPhysics(ref MotionState state, bool zeroVerticalVelocity)
        {
            if (rb == null)
                return;

            if (!state.IsGravitySuspended)
                return;

            if (state.GravityOverrideHandle.IsValid && physicsOverrideController != null)
            {
                physicsOverrideController.ReleaseGravityOverride(ref state.GravityOverrideHandle);
            }
            else if (state.HasSavedGravityScale)
            {
                rb.gravityScale = state.SavedGravityScale;
            }

            state.IsGravitySuspended = false;
            state.HasSavedGravityScale = false;
            state.SavedGravityScale = 0f;

            if (zeroVerticalVelocity)
            {
                ZeroDynamicVerticalVelocity();
            }
        }

        private void ZeroDynamicVerticalVelocity()
        {
            if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic)
                return;

            Vector2 velocity = rb.GetLinearVelocity();
            if (Mathf.Abs(velocity.y) <= 1e-6f)
                return;

            velocity.y = 0f;
            rb.SetLinearVelocity(velocity);
        }

        private void ZeroDynamicVelocity()
        {
            if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic)
                return;

            Vector2 velocity = rb.GetLinearVelocity();
            if (velocity.sqrMagnitude <= 1e-6f)
                return;

            rb.SetLinearVelocity(Vector2.zero);
        }

        private void StopVelocityIfNeeded(bool stopAtEnd, MotionKind kind)
        {
            if (rb == null) return;
            if (rb.bodyType != RigidbodyType2D.Dynamic) return;

            if (kind == MotionKind.PositionHold)
            {
                rb.SetLinearVelocity(Vector2.zero);
                return;
            }

            if (!stopAtEnd) return;

            rb.SetLinearVelocity(new Vector2(0f, rb.GetLinearVelocity().y));
        }

        private ref MotionState GetStateRef(MotionChannel channel)
        {
            return ref (channel == MotionChannel.CrowdControl ? ref _crowdControl : ref _skill);
        }

        /// <summary>
        /// 단일 모션 채널의 실행 상태(진행 시간, 누적 이동, 홀드 등)를 보관하는 내부 상태 구조체입니다.
        /// </summary>
        internal struct MotionState
        {
            public bool IsPlaying;
            public bool IsComplete;

            public MotionChannel Channel;
            public MotionKind Kind;
            public IMotionSolver Solver;

            public Vector2 Direction;
            public float Duration;
            public float Elapsed;
            public float Distance;
            public float MovedDistance;
            public Easing.EaseType EaseType;

            public float ArcHeight;

            /// <summary>Arc 누적 적용값(y)</summary>
            public float AppliedArcY;

            /// <summary>Arc 진행(상승/낙하) easing</summary>
            public Easing.EaseType ArcRiseEaseType;
            public Easing.EaseType ArcFallEaseType;

            /// <summary>Apex 유지 구간 폭(정규화 0..1). 0이면 ApexHold 없이 Rise->Fall로 전환됩니다.</summary>
            public float ArcApexHoldNormalized;

            /// <summary>Arc 상승 구간 비율(정규화 전 원본 값).</summary>
            public float ArcRiseRatioNormalized;

            /// <summary>Arc 하강 구간 비율(정규화 전 원본 값).</summary>
            public float ArcFallRatioNormalized;
            public float FallSpeed;

            public float HoldSecondsAfter;
            public float HoldRemaining;

            public bool StopAtEnd;
            public bool UseMovePosition;

            public Vector2 StartPosition;
            public Vector2 TargetPosition;
            public Vector2 CurrentPosition;
            public float GroundSnapDistance;

            public bool StopOnWall;
            public float WallCollisionSkin;
            public bool WasWallImpacted;
            public MotionWallImpactInfo LastWallImpact;

            public MotionCollisionPolicy CollisionPolicy;
            public GameObject CollisionTarget;
            public MotionBodyCollisionPolicy BodyCollisionPolicy;
            public float BodySeparationMultiplier;
            public float BodySeparationDuration;
            public MotionCollisionIgnoreScope2D CollisionIgnoreScope;
            public ICharacterMotionPositionConstraint2D PositionConstraint;

            public bool IsGravitySuspended;
            public bool HasSavedGravityScale;
            public float SavedGravityScale;
            public CharacterPhysicsOverrideHandle GravityOverrideHandle;

            public void Start(in MotionRequest req)
            {
                IsPlaying = true;
                IsComplete = false;

                Channel = req.Channel;
                Kind = req.Kind;
                Direction = req.Direction;
                Duration = req.DurationSeconds;
                Elapsed = 0f;
                Distance = req.Distance;
                MovedDistance = 0f;
                EaseType = req.EaseType;

                ArcHeight = req.ArcHeight;
                AppliedArcY = 0f;
                ArcRiseEaseType = req.ArcRiseEaseType;
                ArcFallEaseType = req.ArcFallEaseType;
                ArcApexHoldNormalized = req.ArcApexHoldNormalized;
                ArcRiseRatioNormalized = req.ArcRiseRatioNormalized;
                ArcFallRatioNormalized = req.ArcFallRatioNormalized;
                FallSpeed = req.FallSpeed;

                HoldSecondsAfter = req.HoldSecondsAfter;
                HoldRemaining = req.HoldSecondsAfter;

                StopAtEnd = req.StopAtEnd;
                UseMovePosition = req.UseMovePosition;

                StartPosition = req.StartPosition;
                TargetPosition = req.TargetPosition;
                CurrentPosition = req.StartPosition;
                GroundSnapDistance = req.GroundSnapDistance;
                StopOnWall = req.StopOnWall;
                WallCollisionSkin = req.WallCollisionSkin;
                WasWallImpacted = false;
                LastWallImpact = default;
                CollisionPolicy = req.CollisionPolicy;
                CollisionTarget = req.CollisionTarget;
                BodyCollisionPolicy = req.BodyCollisionPolicy;
                BodySeparationMultiplier = req.BodySeparationMultiplier;
                BodySeparationDuration = req.BodySeparationDuration;
                CollisionIgnoreScope = null;
                PositionConstraint = req.PositionConstraint;

                IsGravitySuspended = false;
                HasSavedGravityScale = false;
                SavedGravityScale = 0f;
                GravityOverrideHandle = default;

                // Solver 선택
                Solver = SelectSolver(req);
            }

            public void MarkComplete()
            {
                IsComplete = true;
            }

            public bool TryGetProgress(out float progress01)
            {
                progress01 = 0f;
                if (!IsPlaying)
                    return false;

                if (Duration <= 1e-6f)
                {
                    progress01 = 1f;
                    return true;
                }

                progress01 = Mathf.Clamp01(Elapsed / Duration);
                return true;
            }

            public void Stop()
            {
                IsPlaying = false;
                IsComplete = false;

                Elapsed = 0f;
                Duration = 0f;

                Distance = 0f;
                MovedDistance = 0f;

                AppliedArcY = 0f;
                FallSpeed = 0f;

                HoldSecondsAfter = 0f;
                HoldRemaining = 0f;

                StartPosition = Vector2.zero;
                TargetPosition = Vector2.zero;
                CurrentPosition = Vector2.zero;
                GroundSnapDistance = 0f;
                StopOnWall = false;
                WallCollisionSkin = 0f;
                WasWallImpacted = false;
                LastWallImpact = default;

                if (CollisionIgnoreScope != null)
                {
                    CollisionIgnoreScope.Dispose();
                    CollisionIgnoreScope = null;
                }

                CollisionPolicy = MotionCollisionPolicy.Default;
                CollisionTarget = null;
                BodyCollisionPolicy = MotionBodyCollisionPolicy.UseCharacterDefault;
                BodySeparationMultiplier = -1f;
                BodySeparationDuration = -1f;
                PositionConstraint = null;

                IsGravitySuspended = false;
                HasSavedGravityScale = false;
                SavedGravityScale = 0f;
                GravityOverrideHandle = default;
            }

            private static IMotionSolver SelectSolver(in MotionRequest req)
            {
                // Linear
                if (req.Kind == MotionKind.Linear)
                {
                    if (req.HoldSecondsAfter > 0f)
                        return MotionSolverLinearMoveHold.Instance;

                    return MotionSolverLinearMove.Instance;
                }

                // Arc
                if (req.Kind == MotionKind.Arc)
                {
                    if (req.ArcMode == MotionArcMode.DistancePhased)
                        return MotionSolverArcPhased.Instance;

                    return MotionSolverArcLegacySine.Instance;
                }

                if (req.Kind == MotionKind.GroundSlam)
                    return MotionSolverGroundSlam.Instance;

                if (req.Kind == MotionKind.PositionHold)
                    return MotionSolverLinearMove.Instance;

                if (req.Kind == MotionKind.KnockDownAir)
                    return MotionSolverLinearMove.Instance;

                return MotionSolverLinearMove.Instance;
            }
        }
    }
}
