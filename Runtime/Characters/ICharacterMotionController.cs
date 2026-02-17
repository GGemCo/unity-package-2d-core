using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 스킬/AI 등 외부 시스템이 캐릭터의 "짧은 모션 이동(전진/대시/러시 등)"을 요청하기 위한 공용 인터페이스입니다.
    /// - Skill 패키지는 <see cref="CharacterBase"/>에 직접 의존하지 않고, 이 인터페이스로 이동을 요청합니다.
    /// - 구현체는 Rigidbody2D(특히 Kinematic) 기반 이동을 권장합니다.
    /// </summary>
    public interface ICharacterMotionController
    {
        /// <summary>
        /// 전방/지정 방향으로 일정 시간 전진(모션 이동)을 시작합니다.
        /// </summary>
        /// <returns>요청을 수락하여 적용했으면 true</returns>
        bool TryStartLunge(in LungeRequest request);

        /// <summary>
        /// 진행 중인 모션 이동을 중단합니다(스킬 캔슬/경직 등).
        /// </summary>
        /// <param name="reason">호출 측이 필요 시 이유 코드를 전달할 수 있습니다.</param>
        void CancelLunge(int reason = 0);
    }

    /// <summary>
    /// 모션 이동(전진/대시) 요청 데이터입니다.
    /// - 이벤트 구간(DurationSeconds) 동안, Distance 만큼 이동합니다.
    /// - 시간(0~1) → 진행률(0~1)은 EaseType으로 매핑합니다.
    /// </summary>
    public readonly struct LungeRequest
    {
        public Vector2 Direction { get; }
        public float DurationSeconds { get; }

        /// <summary>
        /// 총 이동 거리(월드 단위)
        /// </summary>
        public float Distance { get; }

        /// <summary>
        /// 시간→진행률 Easing
        /// </summary>
        public Easing.EaseType EaseType { get; }

        /// <summary>
        /// 종료 시 속도를 0으로 정지시킬지 여부(구현체가 velocity 기반일 때 유효).
        /// </summary>
        public bool StopAtEnd { get; }

        /// <summary>
        /// Kinematic Rigidbody일 때 MovePosition 기반 이동을 사용할지 여부.
        /// </summary>
        public bool UseMovePosition { get; }

        public LungeRequest(
            Vector2 direction,
            float durationSeconds,
            float distance,
            Easing.EaseType easeType,
            bool stopAtEnd = true,
            bool useMovePosition = true)
        {
            Direction = direction.sqrMagnitude < 1e-6f ? Vector2.right : direction.normalized;
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            Distance = Mathf.Max(0f, distance);
            EaseType = easeType;
            StopAtEnd = stopAtEnd;
            UseMovePosition = useMovePosition;
        }
    }
}
