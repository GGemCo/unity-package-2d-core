namespace GGemCo2DCore
{
    /// <summary>
    /// 웨이브 그룹이 다음 그룹으로 전환되는 조건입니다.
    /// </summary>
    public enum WaveNextPolicy
    {
        /// <summary>
        /// 현재 그룹에서 생성된 모든 몬스터가 사망하면 다음 그룹으로 전환합니다.
        /// </summary>
        WhenAllDead = 0,

        /// <summary>
        /// 현재 그룹 시작 후 지정 시간이 지나면 생존 몬스터가 있어도 다음 그룹으로 전환합니다.
        /// </summary>
        AfterSeconds = 1,

        /// <summary>
        /// 모든 몬스터 사망 또는 지정 시간 경과 중 먼저 만족한 조건으로 다음 그룹으로 전환합니다.
        /// </summary>
        AllDeadOrAfterSeconds = 2,

        /// <summary>
        /// 코드나 외부 이벤트가 직접 전환을 요청할 때까지 대기합니다.
        /// </summary>
        Manual = 3
    }
}
