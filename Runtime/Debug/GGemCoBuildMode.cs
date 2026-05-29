namespace GGemCo2DCore
{
    /// <summary>
    /// GGemCo 런타임이 현재 어떤 빌드/테스트 정책으로 동작할지 구분하는 값입니다.
    /// </summary>
    public enum GGemCoBuildMode
    {
        /// <summary>
        /// 개발 중 테스트 모드입니다. 작업자별 Development Settings와 디버그 기능을 허용합니다.
        /// </summary>
        Development = 0,

        /// <summary>
        /// 에디터 Play Mode에서 릴리즈와 유사한 조건으로 테스트하는 모드입니다.
        /// 서비스용 Settings를 사용하고 디버그 기능을 차단합니다.
        /// </summary>
        ReleaseSimulation = 1,

        /// <summary>
        /// 실제 배포 빌드 모드입니다. 서비스용 Settings를 사용하고 디버그 기능을 차단합니다.
        /// </summary>
        Release = 2,
    }
}
