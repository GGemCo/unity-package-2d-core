namespace GGemCo2DCore
{
    /// <summary>
    /// 맵에서 몬스터를 생성한 뒤 어떤 리젠/소유권 목록에 등록할지 결정하는 정책입니다.
    /// </summary>
    public enum MonsterSpawnRegistrationPolicy
    {
        /// <summary>
        /// 기존 맵 배치 몬스터처럼 사망 후 개별 리젠 대상에 등록합니다.
        /// </summary>
        NormalRespawn = 0,

        /// <summary>
        /// 맵에는 배치하지만 사망 후 개별 리젠 대상에는 등록하지 않습니다.
        /// </summary>
        NoRespawn = 1,

        /// <summary>
        /// 웨이브 시스템이 수명과 재스폰을 별도로 관리하는 몬스터로 취급합니다.
        /// </summary>
        WaveManaged = 2
    }
}
