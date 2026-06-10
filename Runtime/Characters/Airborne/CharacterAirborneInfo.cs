namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터의 현재 지상/공중 판정 결과를 담는 읽기 전용 스냅샷입니다.
    /// </summary>
    public readonly struct CharacterAirborneInfo
    {
        /// <summary>
        /// Ground Probe 기준으로 현재 지면에 닿아 있는지 여부입니다.
        /// </summary>
        public bool IsGrounded { get; }

        /// <summary>
        /// 물리 Probe 또는 강제 공중 토큰 기준으로 공중 상태인지 여부입니다.
        /// </summary>
        public bool IsAirborne { get; }

        /// <summary>
        /// Ground Probe 결과만 기준으로 공중 상태인지 여부입니다.
        /// </summary>
        public bool IsPhysicallyAirborne { get; }

        /// <summary>
        /// Jump, CrowdControl, Lunge 같은 시스템이 명시적으로 등록한 공중 상태가 있는지 여부입니다.
        /// </summary>
        public bool IsForcedAirborne { get; }

        /// <summary>
        /// 현재 공중 상태 원인 플래그입니다.
        /// </summary>
        public CharacterAirborneSource Source { get; }

        /// <summary>
        /// 캐릭터 하단과 탐지된 지면 사이의 거리입니다. 지면을 찾지 못하면 0입니다.
        /// </summary>
        public float DistanceToGround { get; }

        /// <summary>
        /// 현재 Rigidbody2D의 Y축 속도입니다.
        /// </summary>
        public float VerticalVelocity { get; }

        /// <summary>
        /// 공중 상태 스냅샷을 생성합니다.
        /// </summary>
        /// <param name="isGrounded">지면에 닿아 있는지 여부입니다.</param>
        /// <param name="isAirborne">최종 공중 상태 여부입니다.</param>
        /// <param name="isPhysicallyAirborne">물리 Probe 기준 공중 상태 여부입니다.</param>
        /// <param name="isForcedAirborne">강제 공중 토큰 존재 여부입니다.</param>
        /// <param name="source">공중 상태 원인입니다.</param>
        /// <param name="distanceToGround">지면까지의 거리입니다.</param>
        /// <param name="verticalVelocity">Y축 속도입니다.</param>
        public CharacterAirborneInfo(
            bool isGrounded,
            bool isAirborne,
            bool isPhysicallyAirborne,
            bool isForcedAirborne,
            CharacterAirborneSource source,
            float distanceToGround,
            float verticalVelocity)
        {
            IsGrounded = isGrounded;
            IsAirborne = isAirborne;
            IsPhysicallyAirborne = isPhysicallyAirborne;
            IsForcedAirborne = isForcedAirborne;
            Source = source;
            DistanceToGround = distanceToGround;
            VerticalVelocity = verticalVelocity;
        }
    }
}
