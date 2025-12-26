using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="RectTransform.anchoredPosition"/> 기반 UI 이동(Move) 유틸리티입니다.
    /// </summary>
    /// <remarks>
    /// - 동일 <see cref="RectTransform"/>에 대해 중복 Move 요청 시 기존 코루틴을 자동 중지합니다.
    /// - 시간 스케일 적용 여부/지연/이징/완료 스냅은 <see cref="MoveOptions"/>로 제어합니다.
    /// </remarks>
    public static class UiMoveAnchoredPosition
    {
        /// <summary>
        /// RectTransform별 실행 중인 Move 코루틴 상태 테이블.
        /// </summary>
        /// <remarks>
        /// <see cref="ConditionalWeakTable{TKey, TValue}"/>을 사용해
        /// RectTransform이 파괴되면 상태도 함께 정리되도록 합니다.
        /// </remarks>
        private static readonly ConditionalWeakTable<RectTransform, State> States = new();

        /// <summary>
        /// 단일 RectTransform에 대한 실행 상태(현재 실행 중인 코루틴 참조).
        /// </summary>
        private sealed class State
        {
            /// <summary>현재 실행 중인 Move 코루틴.</summary>
            public Coroutine running;
        }

        /// <summary>
        /// 해당 <see cref="RectTransform"/>에 대해 실행 중인 Move 코루틴이 있다면 중지합니다.
        /// </summary>
        /// <param name="rt">대상 RectTransform.</param>
        /// <param name="runner">코루틴 실행자.</param>
        public static void StopIfRunning(RectTransform rt, MonoBehaviour runner)
        {
            if (rt == null || runner == null) return;
            if (!States.TryGetValue(rt, out var state)) return;

            if (state.running != null)
            {
                runner.StopCoroutine(state.running);
                state.running = null;
            }
        }

        /// <summary>
        /// <paramref name="rt"/>를 지정한 앵커 좌표(<see cref="RectTransform.anchoredPosition"/>)로 이동시킵니다.
        /// </summary>
        /// <param name="runner">코루틴 실행자.</param>
        /// <param name="rt">이동 대상 RectTransform.</param>
        /// <param name="to">목표 앵커 좌표.</param>
        /// <param name="duration">이동 지속 시간(초).</param>
        /// <param name="options">이동 옵션(지연/시간스케일/이징/스냅).</param>
        /// <returns>실행된 Move 코루틴. rt가 null이면 null.</returns>
        /// <exception cref="ArgumentNullException">runner가 null인 경우.</exception>
        public static Coroutine MoveTo(MonoBehaviour runner, RectTransform rt, Vector2 to, float duration, MoveOptions options)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));
            if (rt == null) return null;

            // 동일 RectTransform에 대해 중복 실행을 방지하고 마지막 요청만 유지
            StopIfRunning(rt, runner);

            var state = States.GetOrCreateValue(rt);
            state.running = runner.StartCoroutine(MoveToRoutine(rt, to, duration, options));
            return state.running;
        }

        /// <summary>
        /// 현재 앵커 좌표를 기준으로 <paramref name="delta"/> 만큼 상대 이동시킵니다.
        /// </summary>
        /// <param name="runner">코루틴 실행자.</param>
        /// <param name="rt">이동 대상 RectTransform.</param>
        /// <param name="delta">상대 이동량.</param>
        /// <param name="duration">이동 지속 시간(초).</param>
        /// <param name="options">이동 옵션.</param>
        /// <returns>실행된 Move 코루틴. rt가 null이면 null.</returns>
        public static Coroutine MoveBy(MonoBehaviour runner, RectTransform rt, Vector2 delta, float duration, MoveOptions options)
        {
            if (rt == null) return null;
            return MoveTo(runner, rt, rt.anchoredPosition + delta, duration, options);
        }

        /// <summary>
        /// 실제 이동 보간을 수행하는 코루틴입니다.
        /// </summary>
        /// <param name="rt">이동 대상 RectTransform.</param>
        /// <param name="to">목표 앵커 좌표.</param>
        /// <param name="duration">이동 지속 시간(초).</param>
        /// <param name="options">이동 옵션.</param>
        /// <returns>코루틴 이터레이터.</returns>
        public static IEnumerator MoveToRoutine(RectTransform rt, Vector2 to, float duration, MoveOptions options)
        {
            if (rt == null) yield break;

            // 시작 지연 처리
            if (options.delay > 0f)
            {
                float waited = 0f;
                while (waited < options.delay)
                {
                    waited += options.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    yield return null;
                }
            }

            Vector2 from = rt.anchoredPosition;

            // 즉시 완료 케이스
            if (duration <= 0f)
            {
                rt.anchoredPosition = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += options.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                float nt = Mathf.Clamp01(elapsed / duration);
                float et = Mathf.Clamp01(Easing.Apply(nt, options.easeType));

                rt.anchoredPosition = Vector2.LerpUnclamped(from, to, et);
                yield return null;
            }

            // 부동소수 오차/프레임 드랍 등을 대비해 최종 스냅 처리
            if (options.snapToTargetOnComplete)
                rt.anchoredPosition = to;
        }
    }
}
