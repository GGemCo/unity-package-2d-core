using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Flash Clip에서 베이크된 색상 강조 Payload입니다.
    /// </summary>
    public sealed class UIEffectFlashPayload : UIEffectPayloadBase
    {
        /// <summary>
        /// 플래시에 사용할 색상입니다.
        /// </summary>
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("플래시 정점에서 적용할 색상입니다.")]
        public Color flashColor = Color.white;

        /// <summary>
        /// 플래시 색상의 최대 알파입니다.
        /// </summary>
        [Tooltip("플래시 정점에서 사용할 알파 값입니다.")]
        public float peakAlpha = 0.8f;

        /// <summary>
        /// 반복 횟수입니다.
        /// </summary>
        [Tooltip("클립 지속 시간 안에서 플래시를 반복할 횟수입니다.")]
        public int repeatCount = 1;

        /// <summary>
        /// 완료 시 원래 색상으로 복구할지 여부입니다.
        /// </summary>
        [Tooltip("효과가 끝난 뒤 시작 전 색상으로 복원할지 여부입니다.")]
        public bool restoreOriginalColorOnComplete = true;
    }
}
