using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 스케일 펄스 연출 유틸리티입니다.
    /// </summary>
    public static class UIEffectScaleUtility
    {
        private static readonly Dictionary<Transform, Coroutine> RunningTable = new();

        public static Coroutine PlayPulse(
            MonoBehaviour runner,
            Transform target,
            Vector3 pulseScale,
            float duration,
            bool useUnscaledTime,
            Easing.EaseType easeType)
        {
            if (runner == null || target == null)
                return null;

            if (RunningTable.TryGetValue(target, out var running) && running != null)
            {
                runner.StopCoroutine(running);
                RunningTable.Remove(target);
            }

            var coroutine = runner.StartCoroutine(PulseRoutine(target, pulseScale, duration, useUnscaledTime, easeType));
            RunningTable[target] = coroutine;
            return coroutine;
        }

        private static IEnumerator PulseRoutine(
            Transform target,
            Vector3 pulseScale,
            float duration,
            bool useUnscaledTime,
            Easing.EaseType easeType)
        {
            if (duration <= 0f)
            {
                target.localScale = Vector3.one;
                RunningTable.Remove(target);
                yield break;
            }

            var originalScale = target.localScale;
            var peakScale = Vector3.Scale(originalScale, pulseScale);
            var halfDuration = duration * 0.5f;
            var elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / halfDuration);
                var eased = Easing.Apply(t, easeType);
                target.localScale = Vector3.LerpUnclamped(originalScale, peakScale, eased);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / halfDuration);
                var eased = Easing.Apply(t, Easing.EaseType.EaseOutQuad);
                target.localScale = Vector3.LerpUnclamped(peakScale, originalScale, eased);
                yield return null;
            }

            target.localScale = originalScale;
            RunningTable.Remove(target);
        }
    }
}
