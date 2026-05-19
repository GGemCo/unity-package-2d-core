using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 페이드 연출 방향을 정의합니다.
    /// </summary>
    public enum CutsceneCharacterFadeMode
    {
        /// <summary>
        /// 투명 상태에서 불투명 상태로 전환합니다.
        /// </summary>
        FadeIn = 0,

        /// <summary>
        /// 불투명 상태에서 투명 상태로 전환합니다.
        /// </summary>
        FadeOut = 1,
    }

    /// <summary>
    /// 컷신에서 캐릭터 Fade In/Out 연출에 사용되는 데이터를 정의합니다.
    /// </summary>
    [Serializable]
    public class CharacterFadeData
    {
        [Header("Target")]
        [Tooltip("캐릭터 대상 참조 정보입니다. Fixed는 직접 타입/uid를, RuntimeOverride는 런타임 키를 사용합니다.")]
        public CutsceneCharacterReference target = new CutsceneCharacterReference();

        [HideInInspector] public CharacterConstants.Type characterType;
        [HideInInspector] public int characterUid;

        [Header("Fade")]
        [Tooltip("페이드 방향입니다. useCustomAlphaRange가 false일 때 from/to 알파를 자동으로 결정합니다.")]
        public CutsceneCharacterFadeMode fadeMode = CutsceneCharacterFadeMode.FadeIn;

        [Tooltip("true이면 아래 fromAlpha/toAlpha를 직접 사용합니다.")]
        public bool useCustomAlphaRange;

        [Range(0f, 1f)]
        [Tooltip("시작 알파값입니다. useCustomAlphaRange가 true일 때 사용됩니다.")]
        public float fromAlpha = 0f;

        [Range(0f, 1f)]
        [Tooltip("종료 알파값입니다. useCustomAlphaRange가 true일 때 사용됩니다.")]
        public float toAlpha = 1f;

        [Tooltip("true이면 현재 캐릭터의 RGB 값을 유지하고 알파만 변경합니다. false이면 tintColor를 사용합니다.")]
        public bool preserveCurrentRgb = true;

        [Tooltip("preserveCurrentRgb가 false일 때 사용할 페이드 색상입니다.")]
        public Color tintColor = Color.white;

        [Tooltip("클립 종료 시 최종 상태를 유지할지 여부입니다. false이면 트리거 이전 상태로 복원합니다.")]
        public bool holdFinalState = true;

        [Tooltip("FadeOut 최종 알파가 0에 가까울 때 GameObject를 비활성화할지 여부입니다.")]
        public bool deactivateOnFadeOutComplete = true;

        [Tooltip("Time.timeScale과 무관하게 진행할지 여부입니다.")]
        public bool useUnscaledTime = true;

        [Tooltip("알파 보간 easing 입니다.")]
        public Easing.EaseType easing = Easing.EaseType.Linear;

        /// <summary>
        /// 현재 설정을 기준으로 실제 시작/종료 알파 범위를 계산합니다.
        /// </summary>
        /// <param name="resolvedFromAlpha">계산된 시작 알파값입니다.</param>
        /// <param name="resolvedToAlpha">계산된 종료 알파값입니다.</param>
        public void ResolveAlphaRange(out float resolvedFromAlpha, out float resolvedToAlpha)
        {
            if (useCustomAlphaRange)
            {
                resolvedFromAlpha = Mathf.Clamp01(fromAlpha);
                resolvedToAlpha = Mathf.Clamp01(toAlpha);
                return;
            }

            if (fadeMode == CutsceneCharacterFadeMode.FadeIn)
            {
                resolvedFromAlpha = 0f;
                resolvedToAlpha = 1f;
                return;
            }

            resolvedFromAlpha = 1f;
            resolvedToAlpha = 0f;
        }
    }
}
