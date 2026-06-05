using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Scale Payload를 RectTransform localScale 보간으로 실행하는 Executor입니다.
    /// </summary>
    public sealed class UIEffectScaleExecutor : IUIEffectTimelineExecutor
    {
        public UIEffectTimelineEventType EventType => UIEffectTimelineEventType.Scale;

        /// <summary>
        /// Scale Payload를 실행합니다.
        /// </summary>
        public void Play(MonoBehaviour runner, UIEffectTarget target, UIEffectPayloadBase payload, float duration, UIEffectTimelineContext context)
        {
            var scalePayload = payload as UIEffectScalePayload;
            if (runner == null || target == null || scalePayload == null || target.ScaleTarget == null)
            {
                return;
            }

            UIEffectScaleUtility.CacheBaseScale(target.ScaleTarget);
            Vector3 from = scalePayload.useCurrentScaleAsFrom ? target.ScaleTarget.localScale : scalePayload.fromScale;
            UIEffectScaleUtility.AnimateTo(
                runner,
                target.ScaleTarget,
                from,
                scalePayload.toScale,
                duration,
                context.useUnscaledTime,
                scalePayload.easeType);
        }
    }
}
