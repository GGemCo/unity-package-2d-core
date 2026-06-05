using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Fade Payload를 CanvasGroup 알파 보간으로 실행하는 Executor입니다.
    /// </summary>
    public sealed class UIEffectFadeExecutor : IUIEffectTimelineExecutor
    {
        public UIEffectTimelineEventType EventType => UIEffectTimelineEventType.Fade;

        /// <summary>
        /// Fade Payload를 실행합니다.
        /// </summary>
        public void Play(MonoBehaviour runner, UIEffectTarget target, UIEffectPayloadBase payload, float duration, UIEffectTimelineContext context)
        {
            var fadePayload = payload as UIEffectFadePayload;
            if (runner == null || target == null || fadePayload == null)
            {
                return;
            }

            var options = UiFadeUtility.FadeOptions.Default;
            options.useUnscaledTime = context.useUnscaledTime;
            options.startAlpha = fadePayload.useCurrentAlphaAsFrom ? null : Mathf.Clamp01(fadePayload.fromAlpha);
            options.easeType = fadePayload.easeType;
            options.updateInteractableOnComplete = fadePayload.updateInteractableOnComplete;
            options.updateBlocksRaycastsOnComplete = fadePayload.updateBlocksRaycastsOnComplete;
            options.disableInputWhenInvisible = fadePayload.disableInputWhenInvisible;

            UiFadeUtility.FadeTo(runner, target.gameObject, Mathf.Clamp01(fadePayload.toAlpha), duration, options, true);
        }
    }
}
