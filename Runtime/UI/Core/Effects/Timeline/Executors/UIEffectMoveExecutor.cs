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
        /// <param name="runner">코루틴 실행에 사용할 MonoBehaviour입니다.</param>
        /// <param name="target">Move 효과를 적용할 UI 대상입니다.</param>
        /// <param name="payload">Move 효과에 사용할 Payload입니다.</param>
        /// <param name="duration">위치 보간에 사용할 재생 시간입니다.</param>
        /// <param name="context">타임라인 재생 컨텍스트입니다.</param>
        public void Play(MonoBehaviour runner, UIEffectTarget target, UIEffectPayloadBase payload, float duration, UIEffectTimelineContext context)
        {
            var movePayload = payload as UIEffectMovePayload;
            if (runner == null || target == null || movePayload == null || target.MoveTarget == null)
            {
                return;
            }

            Vector2 occurrencePosition = target.MoveTarget.anchoredPosition;
            UIEffectMoveUtility.CacheBasePosition(target.MoveTarget);
            Vector2 basePosition = UIEffectMoveUtility.GetOrCacheBasePosition(target.MoveTarget);

            Vector2 from = movePayload.useCurrentPositionAsFrom
                ? occurrencePosition
                : basePosition + movePayload.fromOffset;
            Vector2 to = ResolveDestination(movePayload, basePosition, occurrencePosition);

            target.MoveTarget.anchoredPosition = from;

            var options = MoveOptions.Default;
            options.useUnscaledTime = context.useUnscaledTime;
            options.easeType = movePayload.easeType;
            options.snapToTargetOnComplete = movePayload.snapToTargetOnComplete;
            UiMoveAnchoredPosition.MoveTo(runner, target.MoveTarget, to, duration, options);
        }

        /// <summary>
        /// Payload의 종료 위치 정책에 따라 최종 anchoredPosition을 계산합니다.
        /// </summary>
        /// <param name="payload">종료 위치 계산에 사용할 Move Payload입니다.</param>
        /// <param name="basePosition">대상 RectTransform의 최초 기준 위치입니다.</param>
        /// <param name="occurrencePosition">효과가 발생한 시점의 현재 위치입니다.</param>
        /// <returns>최종 이동 대상 anchoredPosition입니다.</returns>
        private static Vector2 ResolveDestination(UIEffectMovePayload payload, Vector2 basePosition, Vector2 occurrencePosition)
        {
            switch (payload.destinationPolicy)
            {
                case UIEffectMoveDestinationPolicy.AbsoluteAnchoredPosition:
                    return payload.toOffset;
                case UIEffectMoveDestinationPolicy.CurrentPositionOffset:
                    return occurrencePosition + payload.toOffset;
                case UIEffectMoveDestinationPolicy.InitialPositionOffset:
                default:
                    return basePosition + payload.toOffset;
            }
        }
    }
}
