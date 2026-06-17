using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Fade Clip에서 베이크된 알파 보간 Payload입니다.
    /// </summary>
    public sealed class UIEffectFadePayload : UIEffectPayloadBase
    {
        /// <summary>
        /// 시작 알파입니다. useCurrentAlphaAsFrom이 true이면 현재 값을 우선 사용합니다.
        /// </summary>
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("페이드 시작 알파 값입니다. useCurrentAlphaAsFrom이 켜져 있으면 효과 시작 시점의 현재 알파를 우선 사용합니다.")]
        public float fromAlpha = 0f;

        /// <summary>
        /// 목표 알파입니다.
        /// </summary>
        [Tooltip("페이드 종료 알파 값입니다.")]
        public float toAlpha = 1f;

        /// <summary>
        /// 현재 CanvasGroup 알파를 시작값으로 사용할지 여부입니다.
        /// </summary>
        [Tooltip("켜면 fromAlpha 대신 효과 시작 시점의 현재 CanvasGroup 알파 값을 시작값으로 사용합니다.")]
        public bool useCurrentAlphaAsFrom;

        /// <summary>
        /// 완료 시 CanvasGroup.interactable 값을 알파에 맞춰 갱신할지 여부입니다.
        /// </summary>
        [Tooltip("완료 시 최종 알파가 0보다 크면 CanvasGroup.interactable을 켜고, 0이면 끕니다.")]
        public bool updateInteractableOnComplete = true;

        /// <summary>
        /// 완료 시 CanvasGroup.blocksRaycasts 값을 알파에 맞춰 갱신할지 여부입니다.
        /// </summary>
        [Tooltip("완료 시 최종 알파가 0보다 크면 CanvasGroup.blocksRaycasts를 켜고, 0이면 끕니다.")]
        public bool updateBlocksRaycastsOnComplete = true;

        /// <summary>
        /// 알파가 0일 때 입력을 비활성화할지 여부입니다.
        /// </summary>
        [Tooltip("켜면 최종 알파가 0일 때 상호작용과 Raycast 차단을 함께 끕니다.")]
        public bool disableInputWhenInvisible = true;
    }
}
