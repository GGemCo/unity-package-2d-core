using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIEffectPreset을 해석하여 공용 UI 연출을 실행하는 서비스입니다.
    /// </summary>
    public static class UIEffectService
    {
        private sealed class RunningEffectHandle
        {
            public MonoBehaviour Runner;
            public Coroutine Coroutine;
            public UIEffectTarget Target;
        }

        private static readonly Dictionary<int, RunningEffectHandle> RunningEffects = new Dictionary<int, RunningEffectHandle>();

        public static Coroutine Play(MonoBehaviour runner, UIEffectTarget target, UIEffectPreset preset, Action onComplete = null)
        {
            return Play(runner, target, preset, default, onComplete);
        }

        public static Coroutine Play(MonoBehaviour runner, UIEffectTarget target, UIEffectPreset preset, UIEffectContext context, Action onComplete = null)
        {
            if (runner == null || target == null || preset == null)
            {
                onComplete?.Invoke();
                return null;
            }

            target.AutoBind();
            int targetId = target.GetInstanceID();

            if (preset.playPolicy == UIEffectPlayPolicy.IgnoreIfPlaying && RunningEffects.ContainsKey(targetId))
            {
                return null;
            }

            if (preset.playPolicy == UIEffectPlayPolicy.Restart)
            {
                Stop(target);
            }

            Coroutine coroutine = runner.StartCoroutine(PlayRoutine(runner, target, preset, context, onComplete));

            if (preset.playPolicy != UIEffectPlayPolicy.Parallel)
            {
                RunningEffects[targetId] = new RunningEffectHandle
                {
                    Runner = runner,
                    Coroutine = coroutine,
                    Target = target
                };
            }

            return coroutine;
        }

        public static void Stop(UIEffectTarget target)
        {
            if (target == null)
            {
                return;
            }

            int targetId = target.GetInstanceID();
            if (!RunningEffects.TryGetValue(targetId, out var handle))
            {
                return;
            }

            if (handle.Runner != null && handle.Coroutine != null)
            {
                handle.Runner.StopCoroutine(handle.Coroutine);
            }

            RunningEffects.Remove(targetId);
        }

        private static IEnumerator PlayRoutine(MonoBehaviour runner, UIEffectTarget target, UIEffectPreset preset, UIEffectContext context, Action onComplete)
        {
            if (target == null || preset == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            UIEffectScaleUtility.CacheBaseScale(target.ScaleTarget);
            UIEffectShakeUtility.CacheBasePosition(target.ShakeTarget);

            float maxDuration = 0f;
            if (preset.useFade)
            {
                maxDuration = Mathf.Max(maxDuration, preset.fadeDuration);
                var fadeOptions = UiFadeUtility.FadeOptions.Default;
                fadeOptions.useUnscaledTime = preset.useUnscaledTime;
                fadeOptions.startAlpha = preset.fadeStartAlpha >= 0f ? preset.fadeStartAlpha : null;
                fadeOptions.easeType = preset.fadeEaseType;
                fadeOptions.updateInteractableOnComplete = preset.fadeUpdateInteractableOnComplete;
                fadeOptions.updateBlocksRaycastsOnComplete = preset.fadeUpdateBlocksRaycastsOnComplete;
                fadeOptions.disableInputWhenInvisible = preset.fadeDisableInputWhenInvisible;

                if (preset.fadeStartAlpha >= 0f && UiFadeUtility.TryGetCanvasGroup(target.gameObject, true, out var canvasGroup))
                    canvasGroup.alpha = Mathf.Clamp01(preset.fadeStartAlpha);

                if (preset.fadeTargetAlpha >= 0.5f)
                    UiFadeUtility.FadeIn(runner, target.gameObject, preset.fadeDuration, fadeOptions, true);
                else
                    UiFadeUtility.FadeOut(runner, target.gameObject, preset.fadeDuration, fadeOptions, true);
            }

            if (preset.useMove && target.MoveTarget != null)
            {
                maxDuration = Mathf.Max(maxDuration, preset.moveDuration);
                Vector2 basePosition = target.MoveTarget.anchoredPosition;
                target.MoveTarget.anchoredPosition = basePosition + preset.moveFromOffset;
                var moveOptions = MoveOptions.Default;
                moveOptions.useUnscaledTime = preset.useUnscaledTime;
                moveOptions.easeType = preset.moveEaseType;
                moveOptions.snapToTargetOnComplete = preset.moveSnapToTargetOnComplete;
                UiMoveAnchoredPosition.MoveTo(runner, target.MoveTarget, basePosition, preset.moveDuration, moveOptions);
            }

            if (preset.useScale && target.ScaleTarget != null)
            {
                maxDuration = Mathf.Max(maxDuration, preset.scaleDuration);
                UIEffectScaleUtility.AnimateTo(
                    runner,
                    target.ScaleTarget,
                    preset.scaleFrom,
                    preset.scaleTo,
                    preset.scaleDuration,
                    preset.useUnscaledTime,
                    preset.scaleEaseType);
            }

            if (preset.usePunchScale && target.ScaleTarget != null)
            {
                maxDuration = Mathf.Max(maxDuration, preset.punchDuration);
                UIEffectScaleUtility.Punch(
                    runner,
                    target.ScaleTarget,
                    preset.punchScale,
                    preset.punchDuration,
                    preset.useUnscaledTime,
                    preset.punchEaseType);
            }

            if (preset.useShake && target.ShakeTarget != null)
            {
                maxDuration = Mathf.Max(maxDuration, preset.shakeDuration);
                UIEffectShakeUtility.Shake(
                    runner,
                    target.ShakeTarget,
                    preset.shakeStrength,
                    preset.shakeDuration,
                    preset.shakeVibrato,
                    preset.useUnscaledTime);
            }

            if (preset.useFlash && target.FlashTargetGraphic != null)
            {
                maxDuration = Mathf.Max(maxDuration, preset.flashDuration);
                runner.StartCoroutine(PlayFlashRoutine(target.FlashTargetGraphic, preset));
            }

            if (maxDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < maxDuration)
                {
                    if (target == null || runner == null)
                    {
                        break;
                    }

                    elapsed += preset.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    yield return null;
                }
            }

            Complete(target, onComplete);
        }

        private static IEnumerator PlayFlashRoutine(Graphic targetGraphic, UIEffectPreset preset)
        {
            if (targetGraphic == null || preset == null)
            {
                yield break;
            }

            Color baseColor = targetGraphic.color;
            Color flashColor = preset.flashColor;
            flashColor.a = Mathf.Clamp01(preset.flashPeakAlpha);

            float duration = Mathf.Max(0.0001f, preset.flashDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (targetGraphic == null)
                {
                    yield break;
                }

                elapsed += preset.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pingPong = t <= 0.5f ? t * 2f : (1f - t) * 2f;
                float eased = Easing.Apply(pingPong, preset.flashEaseType);
                targetGraphic.color = Color.LerpUnclamped(baseColor, flashColor, eased);
                yield return null;
            }

            if (targetGraphic != null)
            {
                targetGraphic.color = baseColor;
            }
        }

        private static void Complete(UIEffectTarget target, Action onComplete)
        {
            if (target != null)
            {
                int targetId = target.GetInstanceID();
                RunningEffects.Remove(targetId);
            }

            onComplete?.Invoke();
        }
    }
}
