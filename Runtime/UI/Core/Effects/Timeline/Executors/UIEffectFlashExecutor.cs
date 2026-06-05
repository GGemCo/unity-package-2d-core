using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// Flash Payload를 Graphic 색상 보간으로 실행하는 Executor입니다.
    /// </summary>
    public sealed class UIEffectFlashExecutor : IUIEffectTimelineExecutor
    {
        public UIEffectTimelineEventType EventType => UIEffectTimelineEventType.Flash;

        /// <summary>
        /// Flash Payload를 실행합니다.
        /// </summary>
        public void Play(MonoBehaviour runner, UIEffectTarget target, UIEffectPayloadBase payload, float duration, UIEffectTimelineContext context)
        {
            var flashPayload = payload as UIEffectFlashPayload;
            if (runner == null || target == null || flashPayload == null || target.FlashTargetGraphic == null)
            {
                return;
            }

            runner.StartCoroutine(FlashRoutine(target.FlashTargetGraphic, flashPayload, duration, context.useUnscaledTime));
        }

        /// <summary>
        /// Graphic 색상을 플래시 색상으로 보간한 뒤 필요 시 원래 색상으로 복구합니다.
        /// </summary>
        private static IEnumerator FlashRoutine(Graphic graphic, UIEffectFlashPayload payload, float duration, bool useUnscaledTime)
        {
            if (graphic == null || payload == null)
            {
                yield break;
            }

            Color baseColor = graphic.color;
            Color flashColor = payload.flashColor;
            flashColor.a = Mathf.Clamp01(payload.peakAlpha);

            float safeDuration = Mathf.Max(0.0001f, duration);
            int repeatCount = Mathf.Max(1, payload.repeatCount);
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / safeDuration);
                float repeated = Mathf.Repeat(normalized * repeatCount, 1f);
                float pingPong = repeated <= 0.5f ? repeated * 2f : (1f - repeated) * 2f;
                float eased = Mathf.Clamp01(Easing.Apply(pingPong, payload.easeType));
                graphic.color = Color.LerpUnclamped(baseColor, flashColor, eased);
                yield return null;
            }

            if (payload.restoreOriginalColorOnComplete && graphic != null)
            {
                graphic.color = baseColor;
            }
        }
    }
}
