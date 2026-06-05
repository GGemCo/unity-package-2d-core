using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// RectTransform anchoredPosition 기반 흔들기 효과 유틸리티입니다.
    /// </summary>
    public static class UIEffectShakeUtility
    {
        private sealed class State
        {
            public Coroutine running;
            public Vector2 basePosition;
            public bool hasBasePosition;
        }

        private static readonly ConditionalWeakTable<RectTransform, State> States = new();

        public static void StopIfRunning(RectTransform target, MonoBehaviour runner)
        {
            if (target == null || runner == null) return;
            if (!States.TryGetValue(target, out var state)) return;

            if (state.running != null)
            {
                runner.StopCoroutine(state.running);
                state.running = null;
            }
        }

        public static void CacheBasePosition(RectTransform target)
        {
            if (target == null) return;
            var state = States.GetOrCreateValue(target);
            if (state.hasBasePosition) return;
            state.basePosition = target.anchoredPosition;
            state.hasBasePosition = true;
        }

        public static Coroutine Shake(
            MonoBehaviour runner,
            RectTransform target,
            float strength,
            float duration,
            int vibrato,
            UIEffectShakeDirectionMode directionMode,
            bool useUnscaledTime)
        {
            return Shake(runner, target, strength, duration, vibrato, directionMode, useUnscaledTime, UIEffectShakeAxis.XY);
        }

        /// <summary>
        /// 지정한 축 정책을 적용하여 UI 흔들림 효과를 실행합니다.
        /// </summary>
        /// <param name="runner">코루틴 실행자입니다.</param>
        /// <param name="target">흔들림을 적용할 RectTransform입니다.</param>
        /// <param name="strength">흔들림 강도입니다.</param>
        /// <param name="duration">흔들림 지속 시간입니다.</param>
        /// <param name="vibrato">진동 횟수입니다.</param>
        /// <param name="directionMode">수평 흔들림 시작 방향 정책입니다.</param>
        /// <param name="useUnscaledTime">TimeScale 영향을 받지 않는 시간 사용 여부입니다.</param>
        /// <param name="axis">흔들림을 적용할 축입니다.</param>
        /// <returns>실행된 코루틴입니다.</returns>
        public static Coroutine Shake(
            MonoBehaviour runner,
            RectTransform target,
            float strength,
            float duration,
            int vibrato,
            UIEffectShakeDirectionMode directionMode,
            bool useUnscaledTime,
            UIEffectShakeAxis axis)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));
            if (target == null) return null;

            StopIfRunning(target, runner);
            var state = States.GetOrCreateValue(target);
            if (!state.hasBasePosition)
            {
                state.basePosition = target.anchoredPosition;
                state.hasBasePosition = true;
            }

            float horizontalSign = ResolveHorizontalSign(directionMode);
            state.running = runner.StartCoroutine(ShakeRoutine(target, state.basePosition, strength, duration, vibrato, horizontalSign, useUnscaledTime, axis));
            return state.running;
        }

        private static IEnumerator ShakeRoutine(
            RectTransform target,
            Vector2 basePosition,
            float strength,
            float duration,
            int vibrato,
            float horizontalSign,
            bool useUnscaledTime,
            UIEffectShakeAxis axis)
        {
            if (target == null) yield break;

            if (duration <= 0f || strength <= 0f)
            {
                target.anchoredPosition = basePosition;
                yield break;
            }

            float elapsed = 0f;
            int safeVibrato = Mathf.Max(1, vibrato);
            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float nt = Mathf.Clamp01(elapsed / duration);
                float attenuation = 1f - nt;
                float angle = nt * safeVibrato * Mathf.PI * 2f;
                float x = Mathf.Sin(angle) * strength * attenuation * horizontalSign;
                float y = Mathf.Cos(angle * 0.73f) * strength * 0.5f * attenuation;

                if (axis == UIEffectShakeAxis.X)
                {
                    y = 0f;
                }
                else if (axis == UIEffectShakeAxis.Y)
                {
                    x = 0f;
                }

                target.anchoredPosition = basePosition + new Vector2(x, y);
                yield return null;
            }

            target.anchoredPosition = basePosition;
        }

        private static float ResolveHorizontalSign(UIEffectShakeDirectionMode directionMode)
        {
            switch (directionMode)
            {
                case UIEffectShakeDirectionMode.Left:
                    return -1f;

                case UIEffectShakeDirectionMode.Right:
                    return 1f;

                case UIEffectShakeDirectionMode.RandomHorizontal:
                default:
                    return UnityEngine.Random.value < 0.5f ? -1f : 1f;
            }
        }
    }
}
