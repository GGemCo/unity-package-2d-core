using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// RectTransform의 localScale 기반 UI 스케일 효과 유틸리티입니다.
    /// </summary>
    public static class UIEffectScaleUtility
    {
        private sealed class State
        {
            public Coroutine running;
            public Vector3 baseScale;
            public bool hasBaseScale;
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

        public static void CacheBaseScale(RectTransform target)
        {
            if (target == null) return;
            var state = States.GetOrCreateValue(target);
            if (state.hasBaseScale) return;
            state.baseScale = target.localScale;
            state.hasBaseScale = true;
        }

        public static Coroutine AnimateTo(
            MonoBehaviour runner,
            RectTransform target,
            Vector3 from,
            Vector3 to,
            float duration,
            bool useUnscaledTime,
            Easing.EaseType easeType)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));
            if (target == null) return null;

            StopIfRunning(target, runner);
            var state = States.GetOrCreateValue(target);
            state.running = runner.StartCoroutine(AnimateToRoutine(target, from, to, duration, useUnscaledTime, easeType));
            return state.running;
        }

        public static Coroutine Punch(
            MonoBehaviour runner,
            RectTransform target,
            Vector3 punchDelta,
            float duration,
            bool useUnscaledTime,
            Easing.EaseType easeType)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));
            if (target == null) return null;

            StopIfRunning(target, runner);
            var state = States.GetOrCreateValue(target);
            if (!state.hasBaseScale)
            {
                state.baseScale = target.localScale;
                state.hasBaseScale = true;
            }

            state.running = runner.StartCoroutine(PunchRoutine(target, state.baseScale, punchDelta, duration, useUnscaledTime, easeType));
            return state.running;
        }

        private static IEnumerator AnimateToRoutine(
            RectTransform target,
            Vector3 from,
            Vector3 to,
            float duration,
            bool useUnscaledTime,
            Easing.EaseType easeType)
        {
            if (target == null) yield break;

            target.localScale = from;
            if (duration <= 0f)
            {
                target.localScale = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float nt = Mathf.Clamp01(elapsed / duration);
                float et = Mathf.Clamp01(Easing.Apply(nt, easeType));
                target.localScale = Vector3.LerpUnclamped(from, to, et);
                yield return null;
            }

            target.localScale = to;
        }

        private static IEnumerator PunchRoutine(
            RectTransform target,
            Vector3 baseScale,
            Vector3 punchDelta,
            float duration,
            bool useUnscaledTime,
            Easing.EaseType easeType)
        {
            if (target == null) yield break;

            Vector3 peakScale = baseScale + punchDelta;
            if (duration <= 0f)
            {
                target.localScale = baseScale;
                yield break;
            }

            float halfDuration = duration * 0.5f;
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float nt = Mathf.Clamp01(elapsed / halfDuration);
                float et = Mathf.Clamp01(Easing.Apply(nt, easeType));
                target.localScale = Vector3.LerpUnclamped(baseScale, peakScale, et);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float nt = Mathf.Clamp01(elapsed / halfDuration);
                float et = Mathf.Clamp01(Easing.Apply(nt, Easing.EaseType.EaseOutCubic));
                target.localScale = Vector3.LerpUnclamped(peakScale, baseScale, et);
                yield return null;
            }

            target.localScale = baseScale;
        }
    }
}
