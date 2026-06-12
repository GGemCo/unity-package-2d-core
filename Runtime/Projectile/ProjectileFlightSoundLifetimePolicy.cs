namespace GGemCo2DCore
{
    /// <summary>
    /// 프로젝타일 비행 사운드를 어떤 수명 기준으로 유지할지 결정합니다.
    /// </summary>
    public enum ProjectileFlightSoundLifetimePolicy
    {
        /// <summary>
        /// 사운드 요청의 기본 정지 정책을 따릅니다.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Hit 여부와 관계없이 프로젝타일 GameObject가 파괴될 때까지 루프 재생합니다.
        /// </summary>
        LoopUntilProjectileDestroyed = 1,
    }
}
