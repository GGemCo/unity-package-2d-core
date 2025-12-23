using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UGUI CanvasGroup 기반 Fade 유틸.
    /// - CanvasGroup.alpha 로 페이드
    /// - 필요 시 interactable / blocksRaycasts 동기화
    /// - 중복 실행 시 기존 페이드를 자동으로 중지
    /// </summary>
    public static class UiFadeUtility
    {
        [Serializable]
        public struct FadeOptions
        {
            public float Delay;
            public bool UseUnscaledTime;

            /// <summary>Fade 종료 시 Interactable 자동 설정</summary>
            public bool UpdateInteractableOnComplete;

            /// <summary>Fade 종료 시 BlocksRaycasts 자동 설정</summary>
            public bool UpdateBlocksRaycastsOnComplete;

            /// <summary>alpha가 0이면 입력을 끌지 여부</summary>
            public bool DisableInputWhenInvisible;

            /// <summary>이징(0..1 -> 0..1). null이면 Linear.</summary>
            [NonSerialized] public Func<float, float> EasingFunc;

            public static FadeOptions Default => new FadeOptions
            {
                Delay = 0f,
                UseUnscaledTime = false,
                UpdateInteractableOnComplete = true,
                UpdateBlocksRaycastsOnComplete = true,
                DisableInputWhenInvisible = true,
                EasingFunc = null
            };
        }

        private static readonly ConditionalWeakTable<CanvasGroup, FadeState> _states = new();

        private sealed class FadeState
        {
            public Coroutine Running;
        }

        public static bool TryGetCanvasGroup(Component target, bool ensureCanvasGroup, out CanvasGroup canvasGroup)
        {
            canvasGroup = null;
            if (target == null) return false;

            canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup != null) return true;

            if (!ensureCanvasGroup) return false;
            canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
            return true;
        }

        public static bool SetVisible(Component target, bool visible, bool ensureCanvasGroup = false, bool updateInput = true)
        {
            if (!TryGetCanvasGroup(target, ensureCanvasGroup, out var cg)) return false;

            cg.alpha = visible ? 1f : 0f;

            if (updateInput)
            {
                cg.interactable = visible;
                cg.blocksRaycasts = visible;
            }

            return true;
        }

        public static void StopFadeIfRunning(CanvasGroup canvasGroup, MonoBehaviour runner)
        {
            if (canvasGroup == null || runner == null) return;
            if (!_states.TryGetValue(canvasGroup, out var state)) return;

            if (state.Running != null)
            {
                runner.StopCoroutine(state.Running);
                state.Running = null;
            }
        }

        public static Coroutine FadeTo(
            MonoBehaviour runner,
            Component target,
            float toAlpha,
            float duration,
            FadeOptions options,
            bool ensureCanvasGroup = false)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));
            if (!TryGetCanvasGroup(target, ensureCanvasGroup, out var cg)) return null;

            StopFadeIfRunning(cg, runner);

            var state = _states.GetOrCreateValue(cg);
            state.Running = runner.StartCoroutine(FadeToRoutine(cg, toAlpha, duration, options));
            return state.Running;
        }

        public static Coroutine FadeIn(MonoBehaviour runner, Component target, float duration, FadeOptions options, bool ensureCanvasGroup = false)
            => FadeTo(runner, target, 1f, duration, options, ensureCanvasGroup);

        public static Coroutine FadeOut(MonoBehaviour runner, Component target, float duration, FadeOptions options, bool ensureCanvasGroup = false)
            => FadeTo(runner, target, 0f, duration, options, ensureCanvasGroup);

        public static IEnumerator FadeToRoutine(CanvasGroup cg, float toAlpha, float duration, FadeOptions options)
        {
            if (cg == null) yield break;

            if (options.Delay > 0f)
            {
                float waited = 0f;
                while (waited < options.Delay)
                {
                    waited += options.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    yield return null;
                }
            }

            float from = cg.alpha;
            toAlpha = Mathf.Clamp01(toAlpha);

            if (duration <= 0f)
            {
                cg.alpha = toAlpha;
                ApplyInputOnComplete(cg, toAlpha, options);
                yield break;
            }

            var ease = options.EasingFunc ?? Linear;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += options.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                float nt = Mathf.Clamp01(elapsed / duration);
                float et = Mathf.Clamp01(ease(nt));

                cg.alpha = Mathf.LerpUnclamped(from, toAlpha, et);
                yield return null;
            }

            cg.alpha = toAlpha;
            ApplyInputOnComplete(cg, toAlpha, options);
        }

        private static void ApplyInputOnComplete(CanvasGroup cg, float alpha, FadeOptions options)
        {
            if (cg == null) return;

            if (!options.UpdateInteractableOnComplete && !options.UpdateBlocksRaycastsOnComplete)
                return;

            if (!options.DisableInputWhenInvisible)
                return;

            bool visible = alpha > 0.0001f;

            if (options.UpdateInteractableOnComplete) cg.interactable = visible;
            if (options.UpdateBlocksRaycastsOnComplete) cg.blocksRaycasts = visible;
        }

        private static float Linear(float t) => t;
    }
}
