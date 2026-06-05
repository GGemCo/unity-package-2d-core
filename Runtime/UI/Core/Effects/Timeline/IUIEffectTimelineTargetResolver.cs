namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 타임라인 targetKey를 실제 <see cref="UIEffectTarget"/>으로 해석하는 인터페이스입니다.
    /// </summary>
    public interface IUIEffectTimelineTargetResolver
    {
        /// <summary>
        /// targetKey에 해당하는 UI 효과 대상을 조회합니다.
        /// </summary>
        /// <param name="targetKey">조회할 대상 키입니다.</param>
        /// <param name="target">조회된 UI 효과 대상입니다.</param>
        /// <returns>대상을 찾았으면 true입니다.</returns>
        bool TryResolve(string targetKey, out UIEffectTarget target);
    }
}
