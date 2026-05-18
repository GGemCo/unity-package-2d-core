namespace GGemCo2DCore
{
    /// <summary>
    /// 피격 처리 최종 단계에서 HP 값을 보정할 수 있는 확장 포인트입니다.
    /// </summary>
    /// <remarks>
    /// Core는 구체 정책(페이즈 전환, 특수 연출 등)을 알지 못하므로,
    /// 외부 패키지가 본 인터페이스를 구현해 최종 HP를 조정하도록 제공합니다.
    /// </remarks>
    public interface IIncomingHitFinalHpResolver
    {
        /// <summary>
        /// 피격 계산 결과로 도출된 최종 HP를 보정합니다.
        /// </summary>
        /// <param name="proposedHp">Core 계산 기준 최종 HP입니다.</param>
        /// <param name="metadataDamage">현재 피격 메타데이터입니다.</param>
        /// <returns>보정된 최종 HP입니다.</returns>
        long ResolveFinalHpOnIncomingHit(long proposedHp, MetadataDamage metadataDamage);
    }
}
