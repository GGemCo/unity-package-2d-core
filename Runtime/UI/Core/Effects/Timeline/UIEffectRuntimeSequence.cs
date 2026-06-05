using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Timeline 기반 UI 효과 제작 결과를 런타임에서 가볍게 실행하기 위한 시퀀스 에셋입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "UIEffectRuntimeSequence", menuName = "GGemCo/UI/UI Effect Runtime Sequence")]
    public sealed class UIEffectRuntimeSequence : ScriptableObject
    {
        /// <summary>
        /// Addressables 또는 코드에서 식별할 때 사용할 시퀀스 키입니다.
        /// </summary>
        public string sequenceKey;

        /// <summary>
        /// 시퀀스 전체 길이입니다.
        /// </summary>
        public float duration;

        /// <summary>
        /// 시작 시간 순서로 정렬된 UI 효과 이벤트 목록입니다.
        /// </summary>
        public UIEffectRuntimeEvent[] events = new UIEffectRuntimeEvent[0];

        /// <summary>
        /// 이벤트별 상세 파라미터를 보관하는 Payload 서브 에셋 목록입니다.
        /// </summary>
        public UIEffectPayloadBase[] payloads = new UIEffectPayloadBase[0];
    }
}
