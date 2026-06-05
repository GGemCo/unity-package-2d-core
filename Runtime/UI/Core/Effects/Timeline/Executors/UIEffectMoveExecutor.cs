using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Move Payload를 RectTransform anchoredPosition 보간으로 실행하는 Executor입니다.
    /// </summary>
    public sealed class UIEffectMoveExecutor : IUIEffectTimelineExecutor
    {
        public UIEffectTimelineEventType EventType => UIEffectTimelineEventType.Move;

        /// <summary>
        /// Move Payload를 실행합니다.
        /// </summary>
        public void Play(MonoBehaviour runner, UIEffectTarget target, UIEffectPayloadBase payload, float duration, UIEffectTimelineContext context)
        {
            var movePayload = payload as UIEffectMovePayload;
            if (runner == null || target == null || movePayload == null || target.MoveTarget == null)
            {
                return;
            }

            UIEffectMoveUtility.CacheBasePosition(target.MoveTarget);
            Vector2 basePosition = movePayload.relativeToInitialPosition
                ? UIEffectMoveUtility.GetOrCacheBasePosition(target.MoveTarget)
                : Vector2.zero;

            Vector2 from = movePayload.useCurrentPositionAsFrom
                ? target.MoveTarget.anchoredPosition
                : basePosition + movePayload.fromOffset;
            Vector2 to = basePosition + movePayload.toOffset;

            target.MoveTarget.anchoredPosition = from;

            var options = MoveOptions.Default;
            options.useUnscaledTime = context.useUnscaledTime;
            options.easeType = movePayload.easeType;
            options.snapToTargetOnComplete = movePayload.snapToTargetOnComplete;
            UiMoveAnchoredPosition.MoveTo(runner, target.MoveTarget, to, duration, options);
        }
    }
}
