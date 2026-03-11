using System;
using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 실행 진입점입니다.
    /// 프리셋을 해석하여 기존 Fade/Move 유틸리티와 Scale/Shake 유틸리티를 조합해 재생합니다.
    /// </summary>
    public static class UIEffectService
    {
        public static bool Play(MonoBehaviour runner, GameObject target, UIEffectPreset preset, Action onComplete = null)
        {
            if (runner == null || target == null || preset == null)
                return false;

            var effectTarget = UIEffectTarget.GetOrAdd(target);
            if (effectTarget == null)
                return false;

            runner.StartCoroutine(PlayRoutine(runner, effectTarget, preset, onComplete));
            return true;
        }

        public static bool PlayWindow(
            MonoBehaviour runner,
            GameObject target,
            bool show,
            UIEffectPreset openPreset,
            UIEffectPreset closePreset,
            Action<bool> onComplete = null)
        {
            if (runner == null || target == null)
                return false;

            var effectTarget = UIEffectTarget.GetOrAdd(target);
            if (effectTarget == null)
                return false;

            if (show)
            {
                var preset = openPreset != null ? openPreset : UIEffectPreset.WindowOpenFallback;
                if (!target.activeSelf)
                    target.SetActive(true);

                runner.StartCoroutine(PlayWindowOpenRoutine(runner, effectTarget, preset, () => onComplete?.Invoke(true)));
                return true;
            }

            var close = closePreset != null ? closePreset : UIEffectPreset.WindowCloseFallback;
            runner.StartCoroutine(PlayWindowCloseRoutine(runner, effectTarget, close, () => onComplete?.Invoke(false)));
            return true;
        }

        public static void PlayHudResource(
            MonoBehaviour runner,
            GameObject target,
            UIEffectContext context,
            UIEffectPreset increasePreset,
            UIEffectPreset decreasePreset,
            UIEffectPreset maxChangedPreset)
        {
            if (runner == null || target == null || context.IsInitial)
                return;

            if (context.HasCurrentDecrease)
            {
                Play(runner, target, decreasePreset != null ? decreasePreset : UIEffectPreset.ResourceDecreaseFallback);
                return;
            }

            if (context.HasCurrentIncrease)
            {
                Play(runner, target, increasePreset != null ? increasePreset : UIEffectPreset.ResourceIncreaseFallback);
                return;
            }

            if (context.HasTotalChanged)
            {
                Play(runner, target, maxChangedPreset != null ? maxChangedPreset : UIEffectPreset.ResourceMaxChangedFallback);
            }
        }

        public static void PlayCooldownCompleted(MonoBehaviour runner, GameObject target, UIEffectPreset preset = null)
        {
            if (runner == null || target == null)
                return;

            Play(runner, target, preset != null ? preset : UIEffectPreset.CooldownCompletedFallback);
        }

        private static IEnumerator PlayRoutine(MonoBehaviour runner, UIEffectTarget target, UIEffectPreset preset, Action onComplete)
        {
            if (runner == null || target == null || preset == null)
                yield break;

            var maxDuration = 0f;

            if (preset.UseFade)
            {
                var fadeOptions = preset.FadeOptions;
                fadeOptions.useUnscaledTime = preset.UseUnscaledTime || fadeOptions.useUnscaledTime;
                if (fadeOptions.startAlpha.HasValue)
                    UiFadeUtility.FadeIn(runner, target.gameObject, preset.FadeDuration, fadeOptions, true);
                else
                    UiFadeUtility.FadeOut(runner, target.gameObject, preset.FadeDuration, fadeOptions, true);

                maxDuration = Mathf.Max(maxDuration, GetTotalDuration(preset.FadeDuration, fadeOptions.delay));
            }

            if (preset.UseScalePulse && target.ScaleTarget != null)
            {
                UIEffectScaleUtility.PlayPulse(
                    runner,
                    target.ScaleTarget,
                    preset.PulseScale,
                    preset.ScaleDuration,
                    preset.UseUnscaledTime,
                    preset.ScaleEaseType);
                maxDuration = Mathf.Max(maxDuration, preset.ScaleDuration);
            }

            if (preset.UseShake && target.ShakeTarget != null)
            {
                UIEffectShakeUtility.PlayAnchoredPositionShake(
                    runner,
                    target.ShakeTarget,
                    preset.ShakeDuration,
                    preset.ShakeStrength,
                    preset.ShakeVibrato,
                    preset.UseUnscaledTime,
                    preset.ShakeEaseType);
                maxDuration = Mathf.Max(maxDuration, preset.ShakeDuration);
            }

            if (maxDuration > 0f)
            {
                yield return WaitForDuration(maxDuration, preset.UseUnscaledTime);
            }
            else
            {
                yield return null;
            }

            onComplete?.Invoke();
        }

        private static IEnumerator PlayWindowOpenRoutine(MonoBehaviour runner, UIEffectTarget target, UIEffectPreset preset, Action onComplete)
        {
            var maxDuration = 0f;
            var moveTarget = target.MoveTarget;

            if (preset.UseMove && moveTarget != null)
            {
                var endPosition = moveTarget.anchoredPosition;
                moveTarget.anchoredPosition = endPosition + preset.MoveFromOffset;
                UiMoveAnchoredPosition.MoveTo(runner, moveTarget, endPosition, preset.MoveDuration, preset.MoveOptions);
                maxDuration = Mathf.Max(maxDuration, GetTotalDuration(preset.MoveDuration, preset.MoveOptions.delay));
            }

            if (preset.UseFade)
            {
                var fadeOptions = preset.FadeOptions;
                fadeOptions.useUnscaledTime = preset.UseUnscaledTime || fadeOptions.useUnscaledTime;
                UiFadeUtility.FadeIn(runner, target.gameObject, preset.FadeDuration, fadeOptions, true);
                maxDuration = Mathf.Max(maxDuration, GetTotalDuration(preset.FadeDuration, fadeOptions.delay));
            }

            if (preset.UseScalePulse && target.ScaleTarget != null)
            {
                UIEffectScaleUtility.PlayPulse(runner, target.ScaleTarget, preset.PulseScale, preset.ScaleDuration, preset.UseUnscaledTime, preset.ScaleEaseType);
                maxDuration = Mathf.Max(maxDuration, preset.ScaleDuration);
            }

            if (maxDuration > 0f)
                yield return WaitForDuration(maxDuration, preset.UseUnscaledTime);
            else
                yield return null;

            onComplete?.Invoke();
        }

        private static IEnumerator PlayWindowCloseRoutine(MonoBehaviour runner, UIEffectTarget target, UIEffectPreset preset, Action onComplete)
        {
            var maxDuration = 0f;
            var moveTarget = target.MoveTarget;

            if (preset.UseMove && moveTarget != null)
            {
                var startPosition = moveTarget.anchoredPosition;
                var endPosition = startPosition + preset.MoveFromOffset;
                UiMoveAnchoredPosition.MoveTo(runner, moveTarget, endPosition, preset.MoveDuration, preset.MoveOptions);
                maxDuration = Mathf.Max(maxDuration, GetTotalDuration(preset.MoveDuration, preset.MoveOptions.delay));
            }

            if (preset.UseFade)
            {
                var fadeOptions = preset.FadeOptions;
                fadeOptions.useUnscaledTime = preset.UseUnscaledTime || fadeOptions.useUnscaledTime;
                UiFadeUtility.FadeOut(runner, target.gameObject, preset.FadeDuration, fadeOptions, true);
                maxDuration = Mathf.Max(maxDuration, GetTotalDuration(preset.FadeDuration, fadeOptions.delay));
            }

            if (maxDuration > 0f)
                yield return WaitForDuration(maxDuration, preset.UseUnscaledTime);
            else
                yield return null;

            onComplete?.Invoke();
        }

        private static IEnumerator WaitForDuration(float duration, bool useUnscaledTime)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }

        private static float GetTotalDuration(float duration, float delay)
        {
            return Mathf.Max(0f, duration) + Mathf.Max(0f, delay);
        }
    }
}
