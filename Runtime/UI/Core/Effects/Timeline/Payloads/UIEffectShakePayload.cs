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
        public float strength = 8f;

        /// <summary>
        /// 진동 횟수입니다.
        /// </summary>
        public int vibrato = 14;

        /// <summary>
        /// 흔들림이 적용될 축입니다.
        /// </summary>
        public UIEffectShakeAxis axis = UIEffectShakeAxis.XY;

        /// <summary>
        /// 수평 흔들림 시작 방향 정책입니다.
        /// </summary>
        public UIEffectShakeDirectionMode directionMode = UIEffectShakeDirectionMode.RandomHorizontal;
    }
}
