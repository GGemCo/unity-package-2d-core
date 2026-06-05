namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 타임라인 재생에 필요한 실행 문맥입니다.
    /// </summary>
    public struct UIEffectTimelineContext
    {
        /// <summary>
        /// 효과 재생 시 <see cref="UnityEngine.Time.timeScale"/> 영향을 받지 않는 시간을 사용할지 여부입니다.
        /// </summary>
        public bool useUnscaledTime;

        /// <summary>
        /// 기본 실행 문맥을 반환합니다.
        /// </summary>
        public static UIEffectTimelineContext Default => new UIEffectTimelineContext
        {
            useUnscaledTime = true,
        };
    }
}
