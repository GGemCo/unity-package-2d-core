namespace GGemCo2DCore
{
    /// <summary>
    /// 방어 판정 이후의 피격 피해를 HP 계층에 적용하기 전에 소비하거나 일부만 통과시키는 확장 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// Core는 차징 게이지나 특수 보호막 같은 구체 정책을 알지 않으며,
    /// 상위 패키지가 이 인터페이스를 구현해 남은 피해와 후속 피격 반응 정책을 반환합니다.
    /// </remarks>
    public interface IIncomingHitDamageConsumptionResolver
    {
        /// <summary>
        /// 현재 피격 피해를 외부 자원으로 소비할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="incomingDamage">방어 판정 이후 HP 계층에 적용될 피해량입니다.</param>
        /// <param name="metadataDamage">현재 피격 메타데이터입니다.</param>
        /// <param name="result">처리 성공 시 남은 피해와 후속 피격 반응 정책입니다.</param>
        /// <returns>이번 피해를 처리했으면 <see langword="true"/>입니다.</returns>
        bool TryConsumeIncomingDamage(
            long incomingDamage,
            MetadataDamage metadataDamage,
            out IncomingHitDamageConsumptionResult result);
    }

    /// <summary>
    /// 외부 자원이 피격 피해를 소비한 결과와 후속 피격 처리 정책입니다.
    /// </summary>
    public readonly struct IncomingHitDamageConsumptionResult
    {
        /// <summary>
        /// 외부 자원 처리 후 HP 계층으로 전달할 남은 피해량입니다.
        /// </summary>
        public readonly long RemainingDamage;

        /// <summary>
        /// 남은 피해가 0이어도 공격 성공 피드백을 유지할지 여부입니다.
        /// </summary>
        public readonly bool PreserveHitFeedback;

        /// <summary>
        /// 일반 피격 액션 취소 통지를 억제할지 여부입니다.
        /// </summary>
        public readonly bool SuppressActionCancel;

        /// <summary>
        /// 일반 피격 상태 전환과 피격 애니메이션을 억제할지 여부입니다.
        /// </summary>
        public readonly bool SuppressDamageReaction;

        /// <summary>
        /// 피격 피해 소비 결과를 생성합니다.
        /// </summary>
        /// <param name="remainingDamage">HP 계층으로 전달할 남은 피해량입니다.</param>
        /// <param name="preserveHitFeedback">공격 성공 피드백 유지 여부입니다.</param>
        /// <param name="suppressActionCancel">일반 피격 액션 취소 억제 여부입니다.</param>
        /// <param name="suppressDamageReaction">일반 피격 반응 억제 여부입니다.</param>
        public IncomingHitDamageConsumptionResult(
            long remainingDamage,
            bool preserveHitFeedback,
            bool suppressActionCancel,
            bool suppressDamageReaction)
        {
            RemainingDamage = remainingDamage;
            PreserveHitFeedback = preserveHitFeedback;
            SuppressActionCancel = suppressActionCancel;
            SuppressDamageReaction = suppressDamageReaction;
        }
    }

    /// <summary>
    /// 치명적인 피격을 외부 시스템이 먼저 소비하고 사망을 방지할 수 있는 확장 포인트입니다.
    /// </summary>
    /// <remarks>
    /// Core는 차징 게이지, 특수 보호막 같은 구체 정책을 알지 않습니다.
    /// 해당 정책을 소유한 상위 패키지가 구현하며, 성공한 보호 결과는 일반 최종 HP 보정기보다 먼저 적용됩니다.
    /// </remarks>
    public interface IIncomingHitLethalProtectionResolver
    {
        /// <summary>
        /// HP가 0 이하가 되는 피격을 외부 보호 자원으로 처리할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="proposedHp">Core 계산 기준 최종 HP입니다.</param>
        /// <param name="metadataDamage">현재 피격 메타데이터입니다.</param>
        /// <param name="result">보호 성공 시 적용할 HP와 후속 피격 처리 정책입니다.</param>
        /// <returns>치명타를 보호 자원으로 처리했으면 <see langword="true"/>입니다.</returns>
        bool TryResolveLethalIncomingHit(
            long proposedHp,
            MetadataDamage metadataDamage,
            out IncomingHitLethalProtectionResult result);
    }

    /// <summary>
    /// 치명타 보호 이후 적용할 HP와 후속 피격 처리 정책입니다.
    /// </summary>
    public readonly struct IncomingHitLethalProtectionResult
    {
        /// <summary>
        /// 치명타 보호 후 적용할 HP입니다.
        /// </summary>
        public readonly long ResolvedHp;

        /// <summary>
        /// 같은 피격에서 일반 액션 취소 수신자를 다시 호출하지 않을지 여부입니다.
        /// </summary>
        public readonly bool SuppressActionCancel;

        /// <summary>
        /// 같은 피격에서 일반 피격 상태 전환과 피격 애니메이션을 억제할지 여부입니다.
        /// </summary>
        public readonly bool SuppressDamageReaction;

        /// <summary>
        /// 치명타 보호 결과를 생성합니다.
        /// </summary>
        /// <param name="resolvedHp">보호 후 적용할 HP입니다.</param>
        /// <param name="suppressActionCancel">일반 액션 취소 재호출을 억제할지 여부입니다.</param>
        /// <param name="suppressDamageReaction">일반 피격 반응을 억제할지 여부입니다.</param>
        public IncomingHitLethalProtectionResult(
            long resolvedHp,
            bool suppressActionCancel,
            bool suppressDamageReaction)
        {
            ResolvedHp = resolvedHp;
            SuppressActionCancel = suppressActionCancel;
            SuppressDamageReaction = suppressDamageReaction;
        }
    }

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
