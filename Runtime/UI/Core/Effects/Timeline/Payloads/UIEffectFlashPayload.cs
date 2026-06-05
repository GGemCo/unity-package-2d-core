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
        public Color flashColor = Color.white;

        /// <summary>
        /// 플래시 색상의 최대 알파입니다.
        /// </summary>
        public float peakAlpha = 0.8f;

        /// <summary>
        /// 반복 횟수입니다.
        /// </summary>
        public int repeatCount = 1;

        /// <summary>
        /// 완료 시 원래 색상으로 복구할지 여부입니다.
        /// </summary>
        public bool restoreOriginalColorOnComplete = true;
    }
}
