using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="Transform.position"/> (World Space) 기반 Move 유틸리티입니다.
    /// </summary>
    /// <remarks>
    /// - 월드 좌표로 이동시키며, UI가 아닌 일반 오브젝트 이동에도 사용 가능합니다.
    /// - 동일 <see cref="Transform"/>에 대해 중복 Move 요청 시 기존 코루틴을 자동 중지합니다.
    /// - 지연/시간 스케일/이징/완료 스냅은 <see cref="MoveOptions"/>로 제어합니다.
    /// </remarks>
    public static class UiMoveTransform
    {
        /// <summary>
        /// Transform별 실행 중 Move 코루틴 상태 테이블.
        /// </summary>
        /// <remarks>
        /// <see cref="ConditionalWeakTable{TKey, TValue}"/>을 사용해
        /// Transform이 파괴되면 상태도 함께 정리되도록 합니다.
        /// </remarks>
        private static readonly ConditionalWeakTable<Transform, State> States = new();

        /// <summary>
        /// 단일 Transform에 대한 실행 상태(현재 실행 중인 코루틴 참조).
        /// </summary>
        private sealed class State
        {
            /// <summary>현재 실행 중인 Move 코루틴.</summary>
            public Coroutine running;
        }

        /// <summary>
        /// 해당 <see cref="Transform"/>에 대해 실행 중인 Move 코루틴이 있다면 중지합니다.
        /// </summary>
        /// <param name="tr">대상 Transform.</param>
        /// <param name="runner">코루틴 실행자.</param>
        public static void StopIfRunning(Transform tr, MonoBehaviour runner)
        {
            if (tr == null || runner == null) return;
            if (!States.TryGetValue(tr, out var state)) return;

            if (state.running != null)
            {
                runner.StopCoroutine(state.running);
                state.running = null;
            }
        }

        /// <summary>
        /// <paramref name="tr"/>를 지정한 월드 좌표(<see cref="Transform.position"/>)로 이동시킵니다.
        /// </summary>
        /// <param name="runner">코루틴 실행자.</param>
        /// <param name="tr">이동 대상 Transform.</param>
        /// <param name="toWorld">목표 월드 좌표.</param>
        /// <param name="duration">이동 지속 시간(초).</param>
        /// <param name="options">이동 옵션(지연/시간스케일/이징/스냅).</param>
        /// <returns>실행된 Move 코루틴. tr이 null이면 null.</returns>
        /// <exception cref="ArgumentNullException">runner가 null인 경우.</exception>
        public static Coroutine MoveTo(MonoBehaviour runner, Transform tr, Vector3 toWorld, float duration, MoveOptions options)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));
            if (tr == null) return null;

            // 동일 Transform에 대한 중복 실행을 방지하고 마지막 요청만 유지
            StopIfRunning(tr, runner);

            var state = States.GetOrCreateValue(tr);
            state.running = runner.StartCoroutine(MoveToRoutine(tr, toWorld, duration, options));
            return state.running;
        }

        /// <summary>
        /// 현재 월드 좌표를 기준으로 <paramref name="deltaWorld"/> 만큼 상대 이동시킵니다.
        /// </summary>
        /// <param name="runner">코루틴 실행자.</param>
        /// <param name="tr">이동 대상 Transform.</param>
        /// <param name="deltaWorld">상대 이동량(월드 기준).</param>
        /// <param name="duration">이동 지속 시간(초).</param>
        /// <param name="options">이동 옵션.</param>
        /// <returns>실행된 Move 코루틴. tr이 null이면 null.</returns>
        public static Coroutine MoveBy(MonoBehaviour runner, Transform tr, Vector3 deltaWorld, float duration, MoveOptions options)
        {
            if (tr == null) return null;
            return MoveTo(runner, tr, tr.position + deltaWorld, duration, options);
        }

        /// <summary>
        /// 실제 월드 이동 보간을 수행하는 코루틴입니다.
        /// </summary>
        /// <param name="tr">이동 대상 Transform.</param>
        /// <param name="toWorld">목표 월드 좌표.</param>
        /// <param name="duration">이동 지속 시간(초).</param>
        /// <param name="options">이동 옵션.</param>
        /// <returns>코루틴 이터레이터.</returns>
        public static IEnumerator MoveToRoutine(Transform tr, Vector3 toWorld, float duration, MoveOptions options)
        {
            if (tr == null) yield break;

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

            Vector3 from = tr.position;

            // 즉시 완료 케이스
            if (duration <= 0f)
            {
                tr.position = toWorld;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += options.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                float nt = Mathf.Clamp01(elapsed / duration);
                float et = Mathf.Clamp01(Easing.Apply(nt, options.easeType));

                tr.position = Vector3.LerpUnclamped(from, toWorld, et);
                yield return null;
            }

            // 부동소수 오차/프레임 드랍 등을 대비해 최종 스냅 처리
            if (options.snapToTargetOnComplete)
                tr.position = toWorld;
        }
    }
}
