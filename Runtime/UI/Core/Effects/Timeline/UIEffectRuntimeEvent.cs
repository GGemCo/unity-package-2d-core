using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 베이크된 UI 효과 타임라인 이벤트 1건입니다.
    /// </summary>
    [Serializable]
    public struct UIEffectRuntimeEvent
    {
        /// <summary>
        /// 실행할 UI 효과 이벤트 종류입니다.
        /// </summary>
        public UIEffectTimelineEventType type;

        /// <summary>
        /// 시퀀스 시작 기준 이벤트 시작 시간입니다.
        /// </summary>
        public float startTime;

        /// <summary>
        /// 시퀀스 시작 기준 이벤트 종료 시간입니다.
        /// </summary>
        public float endTime;

        /// <summary>
        /// 같은 시간에 시작하는 이벤트의 실행 순서입니다.
        /// </summary>
        public int order;

        /// <summary>
        /// <see cref="UIEffectRuntimeSequence.payloads"/> 배열에서 참조할 Payload 인덱스입니다.
        /// </summary>
        public int payloadIndex;

        /// <summary>
        /// 이벤트 지속 시간을 반환합니다.
        /// </summary>
        public float Duration => endTime > startTime ? endTime - startTime : 0f;
    }
}
