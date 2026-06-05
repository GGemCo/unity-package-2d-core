using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Move Clip에서 베이크된 위치 보간 Payload입니다.
    /// </summary>
    public sealed class UIEffectMovePayload : UIEffectPayloadBase
    {
        /// <summary>
        /// 시작 오프셋 또는 시작 절대 좌표입니다.
        /// </summary>
        public Vector2 fromOffset;

        /// <summary>
        /// 목표 오프셋 또는 목표 절대 좌표입니다.
        /// </summary>
        public Vector2 toOffset;

        /// <summary>
        /// 현재 위치를 시작값으로 사용할지 여부입니다.
        /// </summary>
        public bool useCurrentPositionAsFrom;

        /// <summary>
        /// true이면 캐시된 기준 위치에 오프셋을 더해 위치를 계산합니다.
        /// </summary>
        public bool relativeToInitialPosition = true;

        /// <summary>
        /// 완료 시 목표 위치로 강제 스냅할지 여부입니다.
        /// </summary>
        public bool snapToTargetOnComplete = true;
    }
}
