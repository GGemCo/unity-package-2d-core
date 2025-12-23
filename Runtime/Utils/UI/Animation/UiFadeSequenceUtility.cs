using System;
using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="UiFadeUtility"/>의 Fade 시퀀스( Fade In → Hold → Fade Out ) 전용 유틸리티.
    /// </summary>
    /// <remarks>
    /// - <see cref="CanvasGroup.alpha"/> 를 이용해 시퀀스를 수행합니다.
    /// - 동일 CanvasGroup에 대해 시퀀스 중복 요청 시 기존 코루틴을 자동 중지합니다.
    /// - <see cref="UiFadeUtility.FadeOptions"/>를 재사용하여 이징/입력/시간 정책을 일관되게 유지합니다.
    ///
    /// 설계 의도:
    /// - 단일 Fade 로직은 <see cref="UiFadeUtility"/>에 유지
    /// - 시퀀스 관련(여러 단계 조합)만 본 클래스로 분리
    /// </remarks>
    public static class UiFadeSequenceUtility
    {
        /// <summary>
        /// Fade In / Hold / Fade Out 단계별 옵션 + 시작 alpha 등을 포함하는 시퀀스 옵션.
        /// </summary>
        [Serializable]
        public struct FadeSequenceOptions
        {
            /// <summary>
            /// 시퀀스 시작 시 강제로 설정할 alpha 값.
            /// null이면 현재 <see cref="CanvasGroup.alpha"/> 값을 유지합니다.
            /// </summary>
            public float? startAlpha;

            /// <summary>Fade In 단계 옵션.</summary>
            public UiFadeUtility.FadeOptions fadeIn;

            /// <summary>
            /// Hold(유지) 단계 옵션.
            /// </summary>
            /// <remarks>
            /// - Hold는 alpha를 변경하지 않고 시간만 소비합니다.
            /// - 보통 입력 상태는 Fade In/Out 완료 시점에만 반영하므로
            ///   hold 옵션에서는 updateInteractableOnComplete/updateBlocksRaycastsOnComplete를 false로 두는 것을 권장합니다.
            /// </remarks>
            public UiFadeUtility.FadeOptions hold;

            /// <summary>Fade Out 단계 옵션.</summary>
            public UiFadeUtility.FadeOptions fadeOut;

            /// <summary>
            /// 일반적인 팝업/토스트 UI에 적합한 기본 시퀀스 옵션.
            /// </summary>
            public static FadeSequenceOptions Default => new FadeSequenceOptions
            {
                startAlpha = null,
                fadeIn = UiFadeUtility.FadeOptions.Default,
                hold = new UiFadeUtility.FadeOptions
                {
                    delay = 0f,
                    useUnscaledTime = false,
                    updateInteractableOnComplete = false,
                    updateBlocksRaycastsOnComplete = false,
                    disableInputWhenInvisible = true,
                    easingFunc = null
                },
                fadeOut = UiFadeUtility.FadeOptions.Default
            };
        }

        /// <summary>
        /// CanvasGroup별 시퀀스 실행 상태 관리 테이블.
        /// </summary>
        /// <remarks>
        /// - CanvasGroup이 파괴되면 자동으로 상태도 정리되도록 <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey,TValue}"/> 사용.
        /// - 단일 Fade(<see cref="UiFadeUtility"/>)와 상태를 공유하지 않기 때문에
        ///   "단일 Fade 실행 중 시퀀스 시작" 같은 상황을 일괄 중지하려면 외부에서 함께 Stop을 호출해야 합니다.
        ///   (원하면 두 유틸이 상태를 공유하도록도 설계할 수 있습니다.)
        /// </remarks>
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CanvasGroup, SequenceState> _states = new();

        /// <summary>
        /// 단일 CanvasGroup에 대한 실행 중 시퀀스 코루틴 상태.
        /// </summary>
        private sealed class SequenceState
        {
            public Coroutine Running;
        }

        /// <summary>
        /// 해당 <see cref="CanvasGroup"/>에 대해 실행 중인 시퀀스 코루틴이 있다면 중지합니다.
        /// </summary>
        public static void StopIfRunning(CanvasGroup canvasGroup, MonoBehaviour runner)
        {
            if (canvasGroup == null || runner == null) return;
            if (!_states.TryGetValue(canvasGroup, out var state)) return;

            if (state.Running != null)
            {
                runner.StopCoroutine(state.Running);
                state.Running = null;
            }
        }

        /// <summary>
        /// Fade In → Hold → Fade Out 시퀀스를 시작합니다.
        /// </summary>
        /// <param name="runner">코루틴 실행자.</param>
        /// <param name="target">Fade 대상(내부에서 CanvasGroup 확보).</param>
        /// <param name="fadeInDuration">Fade In 지속 시간(초).</param>
        /// <param name="holdDuration">표시 유지 시간(초).</param>
        /// <param name="fadeOutDuration">Fade Out 지속 시간(초).</param>
        /// <param name="sequenceOptions">시퀀스 옵션(단계별).</param>
        /// <param name="ensureCanvasGroup">CanvasGroup 자동 생성 여부.</param>
        /// <returns>실행된 시퀀스 코루틴.</returns>
        public static Coroutine FadeInHoldFadeOut(
            MonoBehaviour runner,
            GameObject target,
            float fadeInDuration,
            float holdDuration,
            float fadeOutDuration,
            FadeSequenceOptions sequenceOptions,
            bool ensureCanvasGroup = false)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));
            if (!UiFadeUtility.TryGetCanvasGroup(target, ensureCanvasGroup, out var cg)) return null;

            // 시퀀스 중복 실행 방지 (동일 CanvasGroup)
            StopIfRunning(cg, runner);

            var state = _states.GetOrCreateValue(cg);
            state.Running = runner.StartCoroutine(
                FadeInHoldFadeOutRoutine(cg, fadeInDuration, holdDuration, fadeOutDuration, sequenceOptions));

            return state.Running;
        }

        /// <summary>
        /// Fade In → Hold → Fade Out 시퀀스의 실제 처리 코루틴.
        /// </summary>
        /// <remarks>
        /// - 시작 alpha가 지정되면 시퀀스 시작 시 한 번만 강제 적용합니다.
        /// - 각 단계는 <see cref="UiFadeUtility.FadeToRoutine"/>를 재사용하여 로직/정책의 일관성을 유지합니다.
        /// </remarks>
        public static IEnumerator FadeInHoldFadeOutRoutine(
            CanvasGroup cg,
            float fadeInDuration,
            float holdDuration,
            float fadeOutDuration,
            FadeSequenceOptions sequenceOptions)
        {
            if (cg == null) yield break;

            // 0) 시작 alpha 강제 설정(옵션)
            if (sequenceOptions.startAlpha.HasValue)
            {
                cg.alpha = Mathf.Clamp01(sequenceOptions.startAlpha.Value);

                // 시작 alpha가 0일 때 입력 비활성화 등 정책을 적용할 필요가 있으면
                // FadeIn 옵션을 기준으로 한 번 동기화해 둡니다.
                // (예: startAlpha=0이면 blocksRaycasts/interactable을 꺼두고 싶다)
                ApplyInputSyncAtStart(cg, sequenceOptions.fadeIn);
            }

            // 1) Fade In
            yield return UiFadeUtility.FadeToRoutine(cg, 1f, fadeInDuration, sequenceOptions.fadeIn);

            // 2) Hold (알파 고정, 시간만 소비)
            if (holdDuration > 0f)
                yield return HoldRoutine(holdDuration, sequenceOptions.hold);

            // 3) Fade Out
            yield return UiFadeUtility.FadeToRoutine(cg, 0f, fadeOutDuration, sequenceOptions.fadeOut);
        }

        /// <summary>
        /// Hold 단계: 지정 시간만큼 대기합니다. (alpha 변화 없음)
        /// </summary>
        /// <remarks>
        /// - delay / useUnscaledTime 정책을 지원합니다.
        /// - 입력 상태는 일반적으로 Hold 단계에서 변경하지 않는 것이 안전합니다.
        /// </remarks>
        private static IEnumerator HoldRoutine(float duration, UiFadeUtility.FadeOptions options)
        {
            // Hold 시작 지연
            if (options.delay > 0f)
            {
                float waitedDelay = 0f;
                while (waitedDelay < options.delay)
                {
                    waitedDelay += options.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    yield return null;
                }
            }

            float waited = 0f;
            while (waited < duration)
            {
                waited += options.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// 시퀀스 시작 시점에 입력 상태를 한 번 동기화합니다.
        /// </summary>
        /// <remarks>
        /// - startAlpha를 강제 지정하는 경우, 현재 입력 상태가 alpha와 불일치할 수 있습니다.
        /// - 옵션 정책에 따라 "투명하면 입력 차단"을 맞춰주는 역할입니다.
        /// - FadeToRoutine 종료 시점에도 동일 정책이 적용되므로, 여기서는 시작 불일치만 최소화합니다.
        /// </remarks>
        private static void ApplyInputSyncAtStart(CanvasGroup cg, UiFadeUtility.FadeOptions options)
        {
            if (cg == null) return;
            if (!options.disableInputWhenInvisible) return;

            // 옵션이 "완료 시점 갱신"만 켜져 있다면, 시작 시점 동기화는 하지 않는 편이 낫습니다.
            // 하지만 startAlpha를 강제하는 시나리오에서는 '처음부터' 입력이 맞아야 하는 경우가 많아,
            // update*OnComplete 플래그가 꺼져 있더라도 startAlpha=0인 경우를 위해 최소 동기화를 제공합니다.
            // 필요 없으면 아래 조건을 더 엄격하게 바꿔도 됩니다.
            bool visible = cg.alpha > 0.0001f;

            if (options.updateInteractableOnComplete) cg.interactable = visible;
            if (options.updateBlocksRaycastsOnComplete) cg.blocksRaycasts = visible;
        }
    }
}
