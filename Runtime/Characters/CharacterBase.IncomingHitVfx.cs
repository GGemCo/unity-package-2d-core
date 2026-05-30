namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="CharacterBase"/>의 피격 VFX 트리거 연동을 담당하는 partial 구현입니다.
    /// </summary>
    public partial class CharacterBase
    {
        /// <summary>
        /// 피격 애니메이션 이벤트(<c>GGemCoAniEventHit</c>)가 발생했을 때
        /// 캐릭터 피격 VFX 재생을 요청합니다.
        /// </summary>
        /// <remarks>
        /// 실제 재생 여부는 캐릭터별 피격 VFX 설정의 트리거 정책과
        /// 최소 재생 간격 조건을 함께 확인해 결정됩니다.
        /// </remarks>
        public void AnimationEventHit()
        {
            _characterDamageController?.TryPlayIncomingHitVfxByTrigger(
                IncomingHitVfxTriggerType.OnAnimationEventHit);
        }

        /// <summary>
        /// 기존 플레이어 피격 애니메이션 이벤트(<c>GGemCoAniEventPlayerHit</c>)와의 호환을 유지합니다.
        /// </summary>
        /// <remarks>
        /// 신규 캐릭터 공통 이벤트는 <see cref="AnimationEventHit"/>를 사용합니다.
        /// 기존 애니메이션 클립에 등록된 이벤트 이름을 즉시 변경하지 않아도 동일한 경로로 처리됩니다.
        /// </remarks>
        public void AnimationEventPlayerHit()
        {
            AnimationEventHit();
        }
    }
}
