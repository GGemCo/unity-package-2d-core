using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 모션 요청 채널.
    /// - 서로 다른 채널은 우선순위/캔슬 정책을 분리할 수 있습니다.
    /// </summary>
    public enum MotionChannel
    {
        Skill = 0,
        CrowdControl = 10,
    }

    /// <summary>
    /// 모션 종류.
    /// </summary>
    public enum MotionKind
    {
        Linear = 0,
        Arc = 1,
        GroundSlam = 2,
        PositionHold = 3,
        KnockDownAir = 4,
    }

    /// <summary>
    /// Arc 모션 구현 모드.
    /// </summary>
    public enum MotionArcMode
    {
        /// <summary>
        /// 기존 구현: 시간(t) 기반 + sin(pi*t) 형태.
        /// </summary>
        LegacyTimeSine = 0,

        /// <summary>
        /// Distance/Height 진행률 기반 Phase Arc.
        /// - 기본 진행축은 수평 누적 거리(0..Distance)이며, Distance가 0이면 시간 진행률로 폴백합니다.
        /// - Phase는 u 구간(0..1) 기반으로 Rise → ApexHold → Fall로 동작합니다.
        /// </summary>
        DistancePhased = 1,
    }


    /// <summary>
    /// 모션 중 대상 캐릭터와의 충돌 처리 정책입니다.
    /// </summary>
    public enum MotionCollisionPolicy
    {
        Default = 0,
        IgnoreTargetCharacter = 1,
    }

    /// <summary>
    /// 모션 중 벽 충돌이 감지되었을 때 전달되는 정보입니다.
    /// </summary>
    public readonly struct MotionWallImpactInfo
    {
        public MotionChannel Channel { get; }
        public MotionKind Kind { get; }
        public Vector2 Point { get; }
        public Vector2 Normal { get; }
        public float ImpactSpeed { get; }
        public Vector2 RequestedDelta { get; }
        public Collider2D Collider { get; }

        public MotionWallImpactInfo(
            MotionChannel channel,
            MotionKind kind,
            Vector2 point,
            Vector2 normal,
            float impactSpeed,
            Vector2 requestedDelta,
            Collider2D collider)
        {
            Channel = channel;
            Kind = kind;
            Point = point;
            Normal = normal;
            ImpactSpeed = Mathf.Max(0f, impactSpeed);
            RequestedDelta = requestedDelta;
            Collider = collider;
        }
    }

    /// <summary>
    /// 캐릭터 모션 위치 제약을 적용한 결과입니다.
    /// </summary>
    public readonly struct MotionPositionConstraintResult
    {
        /// <summary>
        /// 위치 제약을 반영한 최종 이동량입니다.
        /// </summary>
        public Vector2 AppliedDelta { get; }

        /// <summary>
        /// X축 이동량이 제약되었는지 여부입니다.
        /// </summary>
        public bool IsHorizontalConstrained { get; }

        /// <summary>
        /// Y축 이동량이 제약되었는지 여부입니다.
        /// </summary>
        public bool IsVerticalConstrained { get; }

        /// <summary>
        /// 캐릭터 모션 위치 제약 결과를 생성합니다.
        /// </summary>
        /// <param name="appliedDelta">제약을 반영한 최종 이동량입니다.</param>
        /// <param name="isHorizontalConstrained">X축 이동량 제약 여부입니다.</param>
        /// <param name="isVerticalConstrained">Y축 이동량 제약 여부입니다.</param>
        public MotionPositionConstraintResult(
            Vector2 appliedDelta,
            bool isHorizontalConstrained,
            bool isVerticalConstrained)
        {
            AppliedDelta = appliedDelta;
            IsHorizontalConstrained = isHorizontalConstrained;
            IsVerticalConstrained = isVerticalConstrained;
        }
    }

    /// <summary>
    /// 모션 시스템이 최종 위치를 반영하기 전에 증분 이동량을 제한하는 공통 포트입니다.
    /// </summary>
    /// <remarks>
    /// 구현체는 프레임마다 호출될 수 있으므로 불필요한 객체 생성이나 컬렉션 할당을 피해야 합니다.
    /// </remarks>
    public interface ICharacterMotionPositionConstraint2D
    {
        /// <summary>
        /// 현재 위치와 요청 이동량을 기준으로 적용 가능한 이동량을 계산합니다.
        /// </summary>
        /// <param name="currentPosition">모션 적용 전 Rigidbody2D 기준 위치입니다.</param>
        /// <param name="requestedDelta">충돌 보정 이후 적용하려는 증분 이동량입니다.</param>
        /// <param name="result">위치 제약이 반영된 결과입니다.</param>
        /// <returns>제약 계산을 수행했으면 <see langword="true"/>입니다.</returns>
        bool TryConstrain(
            Vector2 currentPosition,
            Vector2 requestedDelta,
            out MotionPositionConstraintResult result);
    }

    /// <summary>
    /// 캐릭터 모션(짧은 이동/대시/넉백/점프형 회피 등) 요청 데이터.
    /// - DurationSeconds 동안 Distance 만큼 이동합니다(진행률은 Easing으로 변환).
    /// - Arc는 수직 오프셋(0→정점→0)을 추가합니다.
    /// </summary>
    public readonly struct MotionRequest
    {
        public MotionChannel Channel { get; }
        public MotionKind Kind { get; }

        /// <summary>이동 방향(정규화됨)</summary>
        public Vector2 Direction { get; }

        public float DurationSeconds { get; }

        /// <summary>총 이동 거리(월드 단위)</summary>
        public float Distance { get; }

        public Easing.EaseType EaseType { get; }

        public bool StopAtEnd { get; }
        public bool UseMovePosition { get; }

        /// <summary>
        /// true면 동일 채널의 진행 중인 모션을 덮어쓸 수 있습니다.
        /// </summary>
        public bool AllowReplace { get; }

        /// <summary>
        /// 모션 종료 후, 해당 채널을 일정 시간 유지합니다(예: KnockDown의 DownWaitTime).
        /// </summary>
        public float HoldSecondsAfter { get; }

        /// <summary>
        /// Arc 모션의 높이(월드 단위). Kind가 Arc일 때만 유효합니다.
        /// </summary>
        public float ArcHeight { get; }

        /// <summary>
        /// Arc 구현 모드. 기본값은 <see cref="MotionArcMode.LegacyTimeSine"/>입니다.
        /// </summary>
        public MotionArcMode ArcMode { get; }

        /// <summary>
        /// Arc(상승/낙하) Phase에서 사용할 easing. <see cref="MotionArcMode.DistancePhased"/>에서 사용됩니다.
        /// </summary>
        public Easing.EaseType ArcRiseEaseType { get; }

        /// <summary>
        /// Arc(낙하) Phase에서 사용할 easing. <see cref="MotionArcMode.DistancePhased"/>에서 사용됩니다.
        /// </summary>
        public Easing.EaseType ArcFallEaseType { get; }

        /// <summary>
        /// Apex(정점) 유지 구간 폭(정규화 0..1).
        /// - 0이면 ApexHold 없이 Rise → Fall로 바로 전환됩니다.
        /// - 0.1이면 u=0.45~0.55 구간에서 y=H를 유지하는 형태가 됩니다.
        /// </summary>
        public float ArcApexHoldNormalized { get; }

        /// <summary>
        /// Arc 상승 구간 비율입니다.
        /// <see cref="MotionArcMode.DistancePhased"/> 에서만 사용되며,
        /// Rise/ApexHold/Fall 비율 합은 런타임에서 자동 정규화됩니다.
        /// </summary>
        public float ArcRiseRatioNormalized { get; }

        /// <summary>
        /// Arc 하강 구간 비율입니다.
        /// <see cref="MotionArcMode.DistancePhased"/> 에서만 사용되며,
        /// Rise/ApexHold/Fall 비율 합은 런타임에서 자동 정규화됩니다.
        /// </summary>
        public float ArcFallRatioNormalized { get; }

        /// <summary>
        /// KnockDownAir 하강 단계에서 사용할 낙하 속도(월드 단위/초)입니다.
        /// Kind가 KnockDownAir일 때만 유효합니다.
        /// </summary>
        public float FallSpeed { get; }

        /// <summary>
        /// PositionHold / GroundSlam 시작 위치입니다.
        /// </summary>
        public Vector2 StartPosition { get; }

        /// <summary>
        /// GroundSlam 목표 위치(착지 위치)입니다.
        /// </summary>
        public Vector2 TargetPosition { get; }

        /// <summary>
        /// GroundSlam 종료 시 목표 Y에 스냅하는 허용 거리입니다.
        /// </summary>
        public float GroundSnapDistance { get; }

        /// <summary>
        /// 벽에 충돌하면 이동을 중지하고 충돌 이벤트를 발생시킬지 여부입니다.
        /// </summary>
        public bool StopOnWall { get; }

        /// <summary>
        /// 벽 충돌 탐지에 사용할 skin width 입니다.
        /// </summary>
        public float WallCollisionSkin { get; }

        /// <summary>
        /// 모션 중 대상 캐릭터와의 충돌 처리 정책입니다.
        /// </summary>
        public MotionCollisionPolicy CollisionPolicy { get; }

        /// <summary>
        /// 충돌 정책이 타겟을 필요로 할 때 사용되는 대상 GameObject 입니다.
        /// </summary>
        public GameObject CollisionTarget { get; }

        /// <summary>
        /// 모션 이동 중 캐릭터 Body 충돌을 어떻게 처리할지 결정하는 정책입니다.
        /// </summary>
        public MotionBodyCollisionPolicy BodyCollisionPolicy { get; }

        /// <summary>
        /// 모션 이동 후 겹침 해소에 사용할 배율입니다. 0 이하이면 설정 기본값을 사용합니다.
        /// </summary>
        public float BodySeparationMultiplier { get; }

        /// <summary>
        /// 모션 이동 후 겹침 해소를 유지할 시간입니다. 0 미만이면 설정 기본값을 사용합니다.
        /// </summary>
        public float BodySeparationDuration { get; }

        /// <summary>
        /// 벽 및 캐릭터 충돌 보정 이후 최종 이동량에 적용할 선택적 위치 제약입니다.
        /// </summary>
        public ICharacterMotionPositionConstraint2D PositionConstraint { get; }

        public MotionRequest(
            MotionChannel channel,
            MotionKind kind,
            Vector2 direction,
            float durationSeconds,
            float distance,
            Easing.EaseType easeType,
            bool stopAtEnd = true,
            bool useMovePosition = true,
            bool allowReplace = false,
            float holdSecondsAfter = 0f,
            float arcHeight = 0f,
            MotionArcMode arcMode = MotionArcMode.LegacyTimeSine,
            Easing.EaseType arcRiseEaseType = Easing.EaseType.Linear,
            Easing.EaseType arcFallEaseType = Easing.EaseType.Linear,
            float arcApexHoldNormalized = 0f,
            float arcRiseRatioNormalized = 0.5f,
            float arcFallRatioNormalized = 0.5f,
            float fallSpeed = 0f,
            Vector2? startPosition = null,
            Vector2? targetPosition = null,
            float groundSnapDistance = 0.15f,
            bool stopOnWall = false,
            float wallCollisionSkin = 0.02f,
            MotionCollisionPolicy collisionPolicy = MotionCollisionPolicy.Default,
            GameObject collisionTarget = null,
            MotionBodyCollisionPolicy bodyCollisionPolicy = MotionBodyCollisionPolicy.UseCharacterDefault,
            float bodySeparationMultiplier = -1f,
            float bodySeparationDuration = -1f,
            ICharacterMotionPositionConstraint2D positionConstraint = null)
        {
            Channel = channel;
            Kind = kind;
            Direction = direction.sqrMagnitude < 1e-6f ? Vector2.right : direction.normalized;
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            Distance = Mathf.Max(0f, distance);
            EaseType = easeType;
            StopAtEnd = stopAtEnd;
            UseMovePosition = useMovePosition;
            AllowReplace = allowReplace;
            HoldSecondsAfter = Mathf.Max(0f, holdSecondsAfter);
            ArcHeight = Mathf.Max(0f, arcHeight);

            ArcMode = arcMode;
            ArcRiseEaseType = arcRiseEaseType;
            ArcFallEaseType = arcFallEaseType;
            ArcApexHoldNormalized = Mathf.Clamp01(arcApexHoldNormalized);
            ArcRiseRatioNormalized = Mathf.Max(0f, arcRiseRatioNormalized);
            ArcFallRatioNormalized = Mathf.Max(0f, arcFallRatioNormalized);
            FallSpeed = Mathf.Max(0f, fallSpeed);
            StartPosition = startPosition ?? Vector2.zero;
            TargetPosition = targetPosition ?? Vector2.zero;
            GroundSnapDistance = Mathf.Max(0f, groundSnapDistance);
            StopOnWall = stopOnWall;
            WallCollisionSkin = Mathf.Max(0f, wallCollisionSkin);
            CollisionPolicy = collisionPolicy;
            CollisionTarget = collisionTarget;
            BodyCollisionPolicy = bodyCollisionPolicy;
            BodySeparationMultiplier = bodySeparationMultiplier;
            BodySeparationDuration = bodySeparationDuration;
            PositionConstraint = positionConstraint;
        }

        /// <summary>
        /// 현재 요청의 모든 설정을 유지하면서 위치 제약만 교체한 새 요청을 반환합니다.
        /// </summary>
        /// <param name="positionConstraint">모션 이동에 적용할 위치 제약입니다.</param>
        /// <returns>지정한 위치 제약이 반영된 모션 요청입니다.</returns>
        public MotionRequest WithPositionConstraint(ICharacterMotionPositionConstraint2D positionConstraint)
        {
            return new MotionRequest(
                Channel,
                Kind,
                Direction,
                DurationSeconds,
                Distance,
                EaseType,
                StopAtEnd,
                UseMovePosition,
                AllowReplace,
                HoldSecondsAfter,
                ArcHeight,
                ArcMode,
                ArcRiseEaseType,
                ArcFallEaseType,
                ArcApexHoldNormalized,
                ArcRiseRatioNormalized,
                ArcFallRatioNormalized,
                FallSpeed,
                StartPosition,
                TargetPosition,
                GroundSnapDistance,
                StopOnWall,
                WallCollisionSkin,
                CollisionPolicy,
                CollisionTarget,
                BodyCollisionPolicy,
                BodySeparationMultiplier,
                BodySeparationDuration,
                positionConstraint);
        }
    }
}
