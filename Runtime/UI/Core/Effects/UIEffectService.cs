using System;
using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIEffectPreset을 해석하여 공용 UI 연출을 실행하는 서비스입니다.
    /// </summary>
    public static class UIEffectService
    {
        public static Coroutine Play(MonoBehaviour runner, UIEffectTarget target, UIEffectPreset preset, Action onComplete = null)
        {
            if (runner == null || target == null || preset == null)
            {
                onComplete?.Invoke();
                return null;
            }

            target.AutoBind();
            return runner.StartCoroutine(PlayRoutine(runner, target, preset, onComplete));
        }

        private static IEnumerator PlayRoutine(MonoBehaviour runner, UIEffectTarget target, UIEffectPreset preset, Action onComplete)
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

            if (maxDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < maxDuration)
                {
                    elapsed += preset.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    yield return null;
                }
            }

            onComplete?.Invoke();
        }
    }
}
