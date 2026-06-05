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
        public float fromAlpha = 0f;

        /// <summary>
        /// 목표 알파입니다.
        /// </summary>
        public float toAlpha = 1f;

        /// <summary>
        /// 현재 CanvasGroup 알파를 시작값으로 사용할지 여부입니다.
        /// </summary>
        public bool useCurrentAlphaAsFrom;

        /// <summary>
        /// 완료 시 CanvasGroup.interactable 값을 알파에 맞춰 갱신할지 여부입니다.
        /// </summary>
        public bool updateInteractableOnComplete = true;

        /// <summary>
        /// 완료 시 CanvasGroup.blocksRaycasts 값을 알파에 맞춰 갱신할지 여부입니다.
        /// </summary>
        public bool updateBlocksRaycastsOnComplete = true;

        /// <summary>
        /// 알파가 0일 때 입력을 비활성화할지 여부입니다.
        /// </summary>
        public bool disableInputWhenInvisible = true;
    }
}
