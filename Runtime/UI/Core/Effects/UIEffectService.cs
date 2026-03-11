using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 재생 진입점입니다.
    /// 기존 유틸리티를 조합해 윈도우/리소스/아이콘 효과를 공통 방식으로 실행합니다.
    /// </summary>
    public static class UIEffectService
    {
        public static bool PlayWindow(MonoBehaviour runner, GameObject target, bool show, Action<bool> onComplete = null)
        {
            if (runner == null || target == null)
                return false;

            var effectTarget = UIEffectTarget.GetOrAdd(target);
            if (effectTarget == null)
                return false;

            if (show)
            {
                if (!target.activeSelf)
                    target.SetActive(true);

                UiFadeUtility.TryGetCanvasGroup(target, true, out _);
                target.transform.localScale = Vector3.one;

                var preset = UIEffectPreset.CreateWindowOpenDefault();
                var rectTransform = effectTarget.RectTransform;
                var endPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;

                if (preset.useMove && rectTransform != null)
                {
                    rectTransform.anchoredPosition = endPosition + preset.moveFromOffset;
                    UiMoveAnchoredPosition.MoveTo(
                        runner,
                        rectTransform,
                        endPosition,
                        preset.moveDuration,
                        preset.moveOptions);
                }

                if (preset.useFade)
                {
                    var fadeOptions = preset.fadeOptions;
                    fadeOptions.startAlpha = 0f;
                    UiFadeUtility.FadeIn(runner, target, preset.fadeDuration, fadeOptions, true);
                }

                onComplete?.Invoke(true);
                return true;
            }

            var closePreset = UIEffectPreset.CreateWindowCloseDefault();
            if (closePreset.useFade)
            {
                var callbackProxy = target.GetComponent<UIEffectWindowCallbackProxy>();
                if (callbackProxy == null)
                    callbackProxy = target.AddComponent<UIEffectWindowCallbackProxy>();

                callbackProxy.PlayClose(runner, closePreset, () =>
                {
                    onComplete?.Invoke(false);
                    target.SetActive(false);
                });
                return true;
            }

            onComplete?.Invoke(false);
            target.SetActive(false);
            return true;
        }

        public static void PlayHudResource(MonoBehaviour runner, GameObject target, UIEffectContext context)
        {
            if (runner == null || target == null || context.IsInitial)
                return;

            var effectTarget = UIEffectTarget.GetOrAdd(target);
            if (effectTarget == null)
                return;

            if (context.HasCurrentDecrease)
            {
                var preset = UIEffectPreset.CreateResourceDecreaseDefault();
                if (preset.useShake && effectTarget.ShakeTarget != null)
                {
                    UIEffectShakeUtility.PlayAnchoredPositionShake(
                        runner,
                        effectTarget.ShakeTarget,
                        preset.shakeDuration,
                        preset.shakeStrength,
                        preset.shakeVibrato,
                        preset.useUnscaledTime,
                        preset.shakeEaseType);
                }

                if (preset.useScalePulse)
                {
                    UIEffectScaleUtility.PlayPulse(
                        runner,
                        effectTarget.ScaleTarget,
                        preset.pulseScale,
                        preset.scaleDuration,
                        preset.useUnscaledTime,
                        preset.scaleEaseType);
                }

                return;
            }

            if (context.HasCurrentIncrease || context.HasTotalChanged)
            {
                var preset = UIEffectPreset.CreateResourceIncreaseDefault();
                if (preset.useScalePulse)
                {
                    UIEffectScaleUtility.PlayPulse(
                        runner,
                        effectTarget.ScaleTarget,
                        preset.pulseScale,
                        preset.scaleDuration,
                        preset.useUnscaledTime,
                        preset.scaleEaseType);
                }
            }
        }

        public static void PlayCooldownCompleted(MonoBehaviour runner, GameObject target)
        {
            if (runner == null || target == null)
                return;

            var effectTarget = UIEffectTarget.GetOrAdd(target);
            if (effectTarget == null)
                return;

            var preset = UIEffectPreset.CreateCooldownCompletedDefault();
            if (preset.useScalePulse)
            {
                UIEffectScaleUtility.PlayPulse(
                    runner,
                    effectTarget.ScaleTarget,
                    preset.pulseScale,
                    preset.scaleDuration,
                    preset.useUnscaledTime,
                    preset.scaleEaseType);
            }
        }
    }

    /// <summary>
    /// 윈도우 닫힘 효과 완료 콜백을 GameObject 수명과 함께 관리하기 위한 보조 컴포넌트입니다.
    /// </summary>
    public sealed class UIEffectWindowCallbackProxy : MonoBehaviour
    {
        private Coroutine _running;

        public void PlayClose(MonoBehaviour runner, UIEffectPreset preset, Action onComplete)
        {
            if (_running != null)
            {
                runner.StopCoroutine(_running);
                _running = null;
            }

            _running = runner.StartCoroutine(PlayCloseRoutine(runner, preset, onComplete));
        }

        private System.Collections.IEnumerator PlayCloseRoutine(MonoBehaviour runner, UIEffectPreset preset, Action onComplete)
        {
            var duration = Mathf.Max(0f, preset.fadeDuration);
            UiFadeUtility.FadeOut(runner, gameObject, duration, preset.fadeOptions, true);

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += preset.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }

            _running = null;
            onComplete?.Invoke();
        }
    }
}
