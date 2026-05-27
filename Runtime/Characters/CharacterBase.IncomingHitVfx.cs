namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="CharacterBase"/>의 피격 VFX 트리거 연동을 담당하는 partial 구현입니다.
    /// </summary>
    public partial class CharacterBase
    {
        /// <summary>
        /// 피격 애니메이션 이벤트(<c>GGemCoAniEventPlayerHit</c>)가 발생했을 때
        /// 플레이어 피격 VFX 재생을 요청합니다.
        /// </summary>
        /// <remarks>
        /// 실제 재생 여부는 <see cref="GGemCoPlayerSettings.IncomingHitVfxSettings"/>의
        /// 트리거 정책과 최소 재생 간격 조건을 함께 확인해 결정됩니다.
        /// </remarks>
        public void AnimationEventPlayerHit()
        {
            _characterDamageController?.TryPlayPlayerIncomingHitVfxByTrigger(
                GGemCoPlayerSettings.IncomingHitVfxTriggerType.OnAnimationEventPlayerHit);
        }
    }
}
