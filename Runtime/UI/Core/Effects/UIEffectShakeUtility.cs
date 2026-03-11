using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// RectTransform 기반 UI 흔들림 유틸리티입니다.
    /// </summary>
    public static class UIEffectShakeUtility
    {
        private static readonly Dictionary<RectTransform, Coroutine> RunningTable = new();

        public static Coroutine PlayAnchoredPositionShake(
            MonoBehaviour runner,
            RectTransform target,
            float duration,
            float strength,
            int vibrato,
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

            var coroutine = runner.StartCoroutine(ShakeRoutine(target, duration, strength, vibrato, useUnscaledTime, easeType));
            RunningTable[target] = coroutine;
            return coroutine;
        }

        private static IEnumerator ShakeRoutine(
            RectTransform target,
            float duration,
            float strength,
            int vibrato,
            bool useUnscaledTime,
            Easing.EaseType easeType)
        {
            var original = target.anchoredPosition;
            if (duration <= 0f || strength <= 0f || vibrato <= 0)
            {
                target.anchoredPosition = original;
                RunningTable.Remove(target);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / duration);
                var attenuation = 1f - Mathf.Clamp01(Easing.Apply(normalized, easeType));
                var offset = Random.insideUnitCircle * strength * attenuation;
                target.anchoredPosition = original + offset;
                yield return null;
            }

            target.anchoredPosition = original;
            RunningTable.Remove(target);
        }
    }
}
