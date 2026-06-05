using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UGUI <see cref="CanvasGroup"/> 기반 Fade 유틸리티 클래스.
    /// </summary>
    /// <remarks>
    /// - <see cref="CanvasGroup.alpha"/> 값을 이용해 Fade In / Out 수행
    /// - Fade 종료 시 <see cref="CanvasGroup.interactable"/> /
    ///   <see cref="CanvasGroup.blocksRaycasts"/> 자동 제어 가능
    /// - 동일 CanvasGroup에 대해 중복 Fade 요청 시 기존 코루틴을 자동 중지
    /// </remarks>
    public static class UiFadeUtility
    {
        /// <summary>
        /// Fade 동작에 대한 세부 옵션 묶음.
        /// </summary>
        [Serializable]
        public struct FadeOptions
        {
            /// <summary>Fade 시작 전 지연 시간(초).</summary>
            public float delay;

            /// <summary>TimeScale 영향을 받지 않는 시간 사용 여부.</summary>
            public bool useUnscaledTime;

            /// <summary>Fade 종료 시 <see cref="CanvasGroup.interactable"/> 자동 갱신 여부.</summary>
            public bool updateInteractableOnComplete;

            /// <summary>Fade 종료 시 <see cref="CanvasGroup.blocksRaycasts"/> 자동 갱신 여부.</summary>
            public bool updateBlocksRaycastsOnComplete;

            /// <summary>
            /// 알파 값이 0일 때 입력을 비활성화할지 여부.
            /// </summary>
            public bool disableInputWhenInvisible;
            
            /// <summary>
            /// Fade 시작 시 강제로 설정할 alpha 값.
            /// null이면 현재 alpha 유지.
            /// </summary>
            public float? startAlpha;
            
            /// <summary>
            /// 이징 타입.
            /// </summary>
            public Easing.EaseType easeType;

            /// <summary>
            /// 일반적인 UI Fade에 적합한 기본 옵션.
            /// </summary>
            public static FadeOptions Default => new FadeOptions
            {
                delay = 0f,
                startAlpha = 0f,
                useUnscaledTime = false,
                updateInteractableOnComplete = false,
                updateBlocksRaycastsOnComplete = false,
                disableInputWhenInvisible = false,
                easeType = Easing.EaseType.Linear
            };
        }

        /// <summary>
        /// CanvasGroup별 Fade 실행 상태 관리 테이블.
        /// </summary>
        /// <remarks>
        /// <see cref="ConditionalWeakTable{TKey, TValue}"/>을 사용하여
        /// CanvasGroup이 파괴되면 자동으로 상태도 정리되도록 합니다.
        /// </remarks>
        private static readonly ConditionalWeakTable<CanvasGroup, FadeState> States = new();

        /// <summary>
        /// 단일 CanvasGroup에 대한 실행 중 Fade 코루틴 상태.
        /// </summary>
        private sealed class FadeState
        {
            /// <summary>현재 실행 중인 Fade 코루틴.</summary>
            public Coroutine running;
        }

        /// <summary>
        /// 대상 컴포넌트에서 <see cref="CanvasGroup"/>을 가져오거나,
        /// 필요 시 자동으로 추가합니다.
        /// </summary>
        /// <param name="target">CanvasGroup을 찾을 대상 컴포넌트.</param>
        /// <param name="ensureCanvasGroup">
        /// true일 경우 CanvasGroup이 없으면 자동으로 추가합니다.
        /// </param>
        /// <param name="canvasGroup">찾거나 생성된 CanvasGroup.</param>
        /// <returns>CanvasGroup을 확보했으면 true.</returns>
        public static bool TryGetCanvasGroup(GameObject target, bool ensureCanvasGroup, out CanvasGroup canvasGroup)
        {
            canvasGroup = null;
            if (target == null) return false;

            canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup != null) return true;

            if (!ensureCanvasGroup) return false;
            canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
            return true;
        }

        /// <summary>
        /// 대상 UI를 즉시 표시 또는 숨김 상태로 설정합니다.
        /// </summary>
        /// <param name="target">대상 컴포넌트.</param>
        /// <param name="visible">true면 표시, false면 숨김.</param>
        /// <param name="ensureCanvasGroup">CanvasGroup 자동 생성 여부.</param>
        /// <param name="updateInput">입력(interactable, raycast) 상태 동기화 여부.</param>
        /// <returns>CanvasGroup을 찾거나 생성하지 못하면 false.</returns>
        public static bool SetVisible(GameObject target, bool visible, bool ensureCanvasGroup = false, bool updateInput = true)
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

        /// <summary>
        /// 해당 <see cref="CanvasGroup"/>에 대해 실행 중인 Fade 코루틴이 있다면 중지합니다.
        /// </summary>
        /// <param name="canvasGroup">대상 CanvasGroup.</param>
        /// <param name="runner">코루틴 실행자.</param>
        public static void StopFadeIfRunning(CanvasGroup canvasGroup, MonoBehaviour runner)
        {
            if (canvasGroup == null || runner == null) return;
            if (!States.TryGetValue(canvasGroup, out var state)) return;

            if (state.running != null)
            {
                runner.StopCoroutine(state.running);
                state.running = null;
            }
        }

        /// <summary>
        /// 대상 컴포넌트를 지정한 알파 값으로 Fade 합니다.
        /// </summary>
        /// <param name="runner">코루틴 실행자.</param>
        /// <param name="target">Fade 대상.</param>
        /// <param name="toAlpha">목표 알파 값 (0~1).</param>
        /// <param name="duration">Fade 지속 시간(초).</param>
        /// <param name="options">Fade 옵션.</param>
        /// <param name="ensureCanvasGroup">CanvasGroup 자동 생성 여부.</param>
        /// <returns>실행된 Fade 코루틴.</returns>
        /// <exception cref="ArgumentNullException">runner가 null일 경우.</exception>
        public static Coroutine FadeTo(
            MonoBehaviour runner,
            GameObject target,
            float toAlpha,
            float duration,
            FadeOptions options,
            bool ensureCanvasGroup = false)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));
            if (!TryGetCanvasGroup(target, ensureCanvasGroup, out var cg)) return null;

            // 동일 CanvasGroup에 대한 중복 Fade 방지
            StopFadeIfRunning(cg, runner);
            
            if (options.startAlpha.HasValue)
                cg.alpha = Mathf.Clamp01(options.startAlpha.Value);
            var state = States.GetOrCreateValue(cg);
            state.running = runner.StartCoroutine(FadeToRoutine(cg, toAlpha, duration, options));
            return state.running;
        }

        /// <summary>
        /// 알파 값을 1로 Fade In 합니다.
        /// </summary>
        public static Coroutine FadeIn(
            MonoBehaviour runner,
            GameObject target,
            float duration,
            FadeOptions options,
            bool ensureCanvasGroup = false)
            => FadeTo(runner, target, 1f, duration, options, ensureCanvasGroup);

        /// <summary>
        /// 알파 값을 0으로 Fade Out 합니다.
        /// </summary>
        public static Coroutine FadeOut(
            MonoBehaviour runner,
            GameObject target,
            float duration,
            FadeOptions options,
            bool ensureCanvasGroup = false)
            => FadeTo(runner, target, 0f, duration, options, ensureCanvasGroup);

        public static Coroutine FadeOutImmediately(MonoBehaviour runner, GameObject target) => FadeTo(runner, target, 0f, 0f, FadeOptions.Default);

        public static Coroutine FadeInImmediately(MonoBehaviour runner, GameObject target) => FadeTo(runner, target, 1f, 0f, FadeOptions.Default);
        /// <summary>
        /// 실제 Fade 로직을 수행하는 코루틴.
        /// </summary>
        /// <param name="cg">대상 CanvasGroup.</param>
        /// <param name="toAlpha">목표 알파 값.</param>
        /// <param name="duration">지속 시간.</param>
        /// <param name="options">Fade 옵션.</param>
        public static IEnumerator FadeToRoutine(CanvasGroup cg, float toAlpha, float duration, FadeOptions options)
        {
            if (cg == null) yield break;

            // 시작 지연 처리
            if (options.delay > 0f)
            {
                float waited = 0f;
                while (waited < options.delay)
                {
                    waited += options.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    yield return null;
                }
            }

            float from = cg.alpha;
            toAlpha = Mathf.Clamp01(toAlpha);

            // 즉시 완료 케이스
            if (duration <= 0f)
            {
                cg.alpha = toAlpha;
                ApplyInputOnComplete(cg, toAlpha, options);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += options.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                float nt = Mathf.Clamp01(elapsed / duration);
                float et = Mathf.Clamp01(Easing.Apply(nt, options.easeType));

                cg.alpha = Mathf.LerpUnclamped(from, toAlpha, et);
                yield return null;
            }

            cg.alpha = toAlpha;
            ApplyInputOnComplete(cg, toAlpha, options);
        }

        /// <summary>
        /// Fade 종료 시 입력(interactable / raycast) 상태를 옵션에 따라 적용합니다.
        /// </summary>
        private static void ApplyInputOnComplete(CanvasGroup cg, float alpha, FadeOptions options)
        {
            if (cg == null) return;

            if (options is { updateInteractableOnComplete: false, updateBlocksRaycastsOnComplete: false })
                return;

            if (!options.disableInputWhenInvisible)
                return;

            bool visible = alpha > 0.0001f;

            if (options.updateInteractableOnComplete) cg.interactable = visible;
            if (options.updateBlocksRaycastsOnComplete) cg.blocksRaycasts = visible;
        }
    }
}
