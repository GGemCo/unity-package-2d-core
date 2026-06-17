using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Shake Clip에서 베이크된 흔들림 Payload입니다.
    /// </summary>
    public sealed class UIEffectShakePayload : UIEffectPayloadBase
    {
        /// <summary>
        /// 흔들림 강도입니다.
        /// </summary>
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("UI가 기준 위치에서 벗어나는 최대 흔들림 강도입니다.")]
        public float strength = 8f;

        /// <summary>
        /// 진동 횟수입니다.
        /// </summary>
        [Tooltip("클립 지속 시간 안에서 흔들림 방향을 전환하는 횟수입니다.")]
        public int vibrato = 14;

        /// <summary>
        /// 흔들림이 적용될 축입니다.
        /// </summary>
        [Tooltip("흔들림을 적용할 축입니다.")]
        public UIEffectShakeAxis axis = UIEffectShakeAxis.XY;

        /// <summary>
        /// 수평 흔들림 시작 방향 정책입니다.
        /// </summary>
        [Tooltip("수평 흔들림이 시작될 방향을 결정하는 정책입니다.")]
        public UIEffectShakeDirectionMode directionMode = UIEffectShakeDirectionMode.RandomHorizontal;
    }
}
