using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Scale Clip에서 베이크된 스케일 보간 Payload입니다.
    /// </summary>
    public sealed class UIEffectScalePayload : UIEffectPayloadBase
    {
        /// <summary>
        /// 시작 스케일입니다.
        /// </summary>
        public Vector3 fromScale = Vector3.one;

        /// <summary>
        /// 목표 스케일입니다.
        /// </summary>
        public Vector3 toScale = Vector3.one;

        /// <summary>
        /// 현재 스케일을 시작값으로 사용할지 여부입니다.
        /// </summary>
        public bool useCurrentScaleAsFrom;
    }
}
