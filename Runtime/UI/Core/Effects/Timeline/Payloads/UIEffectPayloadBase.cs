using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 타임라인 Payload의 공통 기반 클래스입니다.
    /// </summary>
    public abstract class UIEffectPayloadBase : ScriptableObject
    {
        [Header(UIWindowConstants.TitleHeaderCommon)]
        [Tooltip("효과를 적용할 UI 대상 키입니다. UIEffectTargetRegistry에서 이 키로 대상을 찾습니다.")]
        public string targetKey;

        [Tooltip("같은 UI 대상 안에서 효과 간 간섭을 분리하기 위한 채널입니다.")]
        public UIEffectChannel channel = UIEffectChannel.Default;

        [Tooltip("같은 대상과 채널에 효과가 이미 재생 중일 때 새 효과를 처리하는 방식입니다.")]
        public UIEffectPlayPolicy playPolicy = UIEffectPlayPolicy.StopSameChannelAndPlay;

        [Tooltip("클립 전체 진행률에 적용할 기본 이징 타입입니다.")]
        public Easing.EaseType easeType = Easing.EaseType.Linear;
    }
}
