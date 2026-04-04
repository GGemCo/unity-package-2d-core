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
            state.running = runner.StartCoroutine(ShakeRoutine(target, state.basePosition, strength, duration, vibrato, horizontalSign, useUnscaledTime));
            return state.running;
        }

        private static IEnumerator ShakeRoutine(
            RectTransform target,
            Vector2 basePosition,
            float strength,
            float duration,
            int vibrato,
            float horizontalSign,
            bool useUnscaledTime)
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
