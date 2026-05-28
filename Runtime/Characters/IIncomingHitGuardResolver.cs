using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 외부 패키지(Control 등)에서 피격 직전 가드/저스트가드 판정을 제공하기 위한 인터페이스입니다.
    /// Core는 이 인터페이스만 알고, 실제 판정 구현은 외부 패키지에서 담당합니다.
    /// </summary>
    public interface IIncomingHitGuardResolver
    {
        /// <summary>
        /// 들어오는 공격에 대해 가드/저스트가드 판정을 시도합니다.
        /// </summary>
        bool TryResolveIncomingHit(MetadataDamage metadataDamage, out GuardResolutionResult result);
    }

    /// <summary>
    /// 가드/저스트가드 판정 결과입니다.
    /// </summary>
    public struct GuardResolutionResult
    {
        /// <summary>실제로 가드가 성립했는지 여부</summary>
        public bool IsResolved;

        /// <summary>저스트 가드 성공 여부</summary>
        public bool IsJustGuard;

        /// <summary>가드 판정의 최종 결과 타입</summary>
        public GuardResolutionOutcome Outcome;

        /// <summary>가드 적용 후 실제 남는 데미지</summary>
        public long RemainingDamage;

        /// <summary>피격 리액션(데미지 모션/CC)을 막을지 여부</summary>
        public bool SuppressHitReaction;

        /// <summary>가드 판정 결과로 추가 적용할 Crowd Control UID입니다. 0이면 적용하지 않습니다.</summary>
        public int CrowdControlUid;

        /// <summary>
        /// 가드 결과 CC에 1회성으로 적용할 초기 애니메이션 오버라이드입니다.
        /// 가드 브레이크 애니메이션을 CC Duration/Easing에 맞출 때 사용합니다.
        /// </summary>
        public CrowdControlAnimationOverride CrowdControlAnimationOverride;

        /// <summary>표시용 텍스트. 비우면 기본 데미지 텍스트를 사용합니다.</summary>
        public string FeedbackText;

        /// <summary>표시용 텍스트 색상. 설정되지 않으면 default(Color)입니다.</summary>
        public Color FeedbackColor;
    }
}
