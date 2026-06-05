using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Shake Payload를 RectTransform 흔들림으로 실행하는 Executor입니다.
    /// </summary>
    public sealed class UIEffectShakeExecutor : IUIEffectTimelineExecutor
    {
        public UIEffectTimelineEventType EventType => UIEffectTimelineEventType.Shake;

        /// <summary>
        /// Shake Payload를 실행합니다.
        /// </summary>
        public void Play(MonoBehaviour runner, UIEffectTarget target, UIEffectPayloadBase payload, float duration, UIEffectTimelineContext context)
        {
            var shakePayload = payload as UIEffectShakePayload;
            if (runner == null || target == null || shakePayload == null || target.ShakeTarget == null)
            {
                return;
            }

            UIEffectShakeUtility.Shake(
                runner,
                target.ShakeTarget,
                shakePayload.strength,
                duration,
                shakePayload.vibrato,
                shakePayload.directionMode,
                context.useUnscaledTime,
                shakePayload.axis);
        }
    }
}
