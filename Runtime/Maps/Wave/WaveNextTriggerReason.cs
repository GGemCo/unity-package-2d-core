namespace GGemCo2DCore
{
    /// <summary>
    /// 웨이브 그룹이 다음 그룹 전환을 요청한 원인입니다.
    /// 디버그 로그에서 시간 전환과 전체 처치 전환을 구분하기 위해 사용합니다.
    /// </summary>
    public enum WaveNextTriggerReason
    {
        /// <summary>
        /// 원인을 명시하지 않은 전환 요청입니다.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 그룹의 모든 몬스터가 사망하여 전환을 요청했습니다.
        /// </summary>
        AllMonstersDead = 1,

        /// <summary>
        /// 그룹의 시간 전환 기준이 지나 전환을 요청했습니다.
        /// </summary>
        TimerElapsed = 2
    }
}
