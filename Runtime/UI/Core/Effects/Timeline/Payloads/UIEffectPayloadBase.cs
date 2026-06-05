using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 타임라인 Payload의 공통 기반 클래스입니다.
    /// </summary>
    public abstract class UIEffectPayloadBase : ScriptableObject
    {
        /// <summary>
        /// 효과를 적용할 대상 키입니다.
        /// </summary>
        public string targetKey;

        /// <summary>
        /// 효과 간 간섭을 제어할 채널입니다.
        /// </summary>
        public UIEffectChannel channel = UIEffectChannel.Default;

        /// <summary>
        /// 같은 대상/채널에 효과가 재생 중일 때의 처리 정책입니다.
        /// </summary>
        public UIEffectPlayPolicy playPolicy = UIEffectPlayPolicy.StopSameChannelAndPlay;

        /// <summary>
        /// 효과 진행에 사용할 이징 타입입니다.
        /// </summary>
        public Easing.EaseType easeType = Easing.EaseType.Linear;
    }
}
