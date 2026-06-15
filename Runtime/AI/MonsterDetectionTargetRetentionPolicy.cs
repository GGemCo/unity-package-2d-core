namespace GGemCo2DCore
{
    /// <summary>
    /// 감지 범위에서 획득한 전투 타겟을 감지 이탈 후에도 유지할지 결정하는 정책입니다.
    /// </summary>
    public enum MonsterDetectionTargetRetentionPolicy
    {
        /// <summary>
        /// 감지 이탈 범위와 추적 거리 조건에 따라 감지 기반 Threat를 제거합니다.
        /// </summary>
        DistanceBased = 0,

        /// <summary>
        /// 감지 범위를 벗어나더라도 명시적인 전투 종료가 발생할 때까지 타겟을 유지합니다.
        /// </summary>
        UntilCombatReleased = 1,
    }
}
