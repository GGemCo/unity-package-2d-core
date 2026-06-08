namespace GGemCo2DCore
{
    /// <summary>
    /// Crowd Control 중단 요청이 발생한 원인을 나타냅니다.
    /// </summary>
    /// <remarks>
    /// 호출 계층은 구체적인 중단 구현을 알지 않고, 이 사유만 전달하여
    /// <see cref="CharacterCrowdControlController"/>가 내부 상태를 일관되게 정리하도록 합니다.
    /// </remarks>
    public enum CrowdControlStopReason
    {
        /// <summary>
        /// 명시적인 수동 중단 요청입니다.
        /// </summary>
        Manual,

        /// <summary>
        /// 캐릭터가 피격되어 현재 Crowd Control을 중단해야 하는 경우입니다.
        /// </summary>
        IncomingHit,

        /// <summary>
        /// 공격 액션이 외부 요인으로 인터럽트되어 중단해야 하는 경우입니다.
        /// </summary>
        AttackInterrupted,

        /// <summary>
        /// 오브젝트 풀 반환 또는 재사용 초기화로 인해 중단해야 하는 경우입니다.
        /// </summary>
        PoolReturn,
    }
}
