namespace GGemCo2DCore
{
    /// <summary>
    /// 같은 대상에 UI 효과가 재생 중일 때 처리 정책입니다.
    /// </summary>
    public enum UIEffectPlayPolicy
    {
        /// <summary>
        /// 같은 채널에 재생 중인 효과가 있으면 새 요청을 무시합니다.
        /// </summary>
        IgnoreIfPlaying = 0,

        /// <summary>
        /// 대상에 재생 중인 모든 UI 효과를 중지하고 새 효과를 시작합니다.
        /// </summary>
        Restart = 1,

        /// <summary>
        /// 같은 채널의 UI 효과만 중지하고 새 효과를 시작합니다.
        /// </summary>
        StopSameChannelAndPlay = 2,

        /// <summary>
        /// 기존 효과와 병렬로 새 효과를 재생합니다.
        /// </summary>
        Parallel = 3,
    }
}
