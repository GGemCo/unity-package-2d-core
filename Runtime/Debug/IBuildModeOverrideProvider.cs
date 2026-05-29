namespace GGemCo2DCore
{
    /// <summary>
    /// 에디터나 테스트 환경에서 현재 빌드 모드를 런타임에 공급하기 위한 계약입니다.
    /// </summary>
    public interface IBuildModeOverrideProvider
    {
        /// <summary>
        /// 현재 선택된 빌드 모드를 조회합니다.
        /// </summary>
        /// <param name="mode">현재 선택된 빌드 모드입니다.</param>
        /// <returns>빌드 모드를 정상적으로 제공할 수 있으면 true입니다.</returns>
        bool TryGetMode(out GGemCoBuildMode mode);
    }
}
