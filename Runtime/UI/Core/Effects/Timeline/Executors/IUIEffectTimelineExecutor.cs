namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 타임라인 이벤트를 실제 런타임 효과로 실행하는 Executor 인터페이스입니다.
    /// </summary>
    public interface IUIEffectTimelineExecutor
    {
        /// <summary>
        /// 이 Executor가 처리하는 이벤트 종류입니다.
        /// </summary>
        UIEffectTimelineEventType EventType { get; }

        /// <summary>
        /// Payload를 해석하여 대상 UI에 효과를 실행합니다.
        /// </summary>
        /// <param name="runner">코루틴 실행자입니다.</param>
        /// <param name="target">효과 적용 대상입니다.</param>
        /// <param name="payload">효과 상세 데이터입니다.</param>
        /// <param name="duration">효과 지속 시간입니다.</param>
        /// <param name="context">타임라인 실행 문맥입니다.</param>
        void Play(UnityEngine.MonoBehaviour runner, UIEffectTarget target, UIEffectPayloadBase payload, float duration, UIEffectTimelineContext context);
    }
}
