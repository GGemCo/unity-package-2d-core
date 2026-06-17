using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Move Clip에서 베이크된 위치 보간 Payload입니다.
    /// </summary>
    public sealed class UIEffectMovePayload : UIEffectPayloadBase
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("이동 시작 오프셋입니다. useCurrentPositionAsFrom이 켜져 있으면 효과 시작 시점의 현재 위치를 우선 사용합니다.")]
        public Vector2 fromOffset;

        [Tooltip("이동 종료 오프셋입니다.")]
        public Vector2 toOffset;

        [Tooltip("켜면 fromOffset 대신 효과 시작 시점의 현재 anchoredPosition을 시작값으로 사용합니다.")]
        public bool useCurrentPositionAsFrom;

        [Tooltip("켜면 오프셋 값을 대상의 초기 위치 기준 상대값으로 해석합니다.")]
        public bool relativeToInitialPosition = true;

        [Tooltip("효과 완료 시 최종 위치로 정확하게 스냅할지 여부입니다.")]
        public bool snapToTargetOnComplete = true;
    }
}
