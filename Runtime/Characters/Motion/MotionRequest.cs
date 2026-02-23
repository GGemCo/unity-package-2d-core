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
            float arcApexHoldNormalized = 0f)
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
        }
    }
}
