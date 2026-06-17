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
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("스케일 시작 값입니다. useCurrentScaleAsFrom이 켜져 있으면 효과 시작 시점의 현재 스케일을 우선 사용합니다.")]
        public Vector3 fromScale = Vector3.one;

        /// <summary>
        /// 목표 스케일입니다.
        /// </summary>
        [Tooltip("스케일 종료 값입니다.")]
        public Vector3 toScale = Vector3.one;

        /// <summary>
        /// 현재 스케일을 시작값으로 사용할지 여부입니다.
        /// </summary>
        [Tooltip("켜면 fromScale 대신 효과 시작 시점의 현재 localScale을 시작값으로 사용합니다.")]
        public bool useCurrentScaleAsFrom;
    }
}
