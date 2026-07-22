namespace GGemCo2DCore
{
    /// <summary>
    /// 슈퍼아머 차감이 발생한 원인을 정의합니다.
    /// </summary>
    public enum SuperArmorDamageCause
    {
        /// <summary>
        /// 원인을 특정하지 않은 외부 차감입니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// 일반 피격의 경직 수치로 발생한 차감입니다.
        /// </summary>
        IncomingHit = 1,

        /// <summary>
        /// 플레이어의 저스트 가드 성공으로 발생한 차감입니다.
        /// </summary>
        JustGuard = 2,
    }

    /// <summary>
    /// 외부 시스템이 캐릭터의 슈퍼아머를 차감할 때 전달하는 요청입니다.
    /// </summary>
    public readonly struct SuperArmorDamageRequest
    {
        /// <summary>
        /// 차감할 슈퍼아머 수치입니다.
        /// </summary>
        public int Amount { get; }

        /// <summary>
        /// 동일 공격 판정을 구분하는 식별자입니다. 0이면 식별자를 사용하지 않습니다.
        /// </summary>
        public int AttackId { get; }

        /// <summary>
        /// 슈퍼아머 차감이 발생한 원인입니다.
        /// </summary>
        public SuperArmorDamageCause Cause { get; }

        /// <summary>
        /// 슈퍼아머가 0이 되었을 때 브레이크에 전달할 피격 리액션 타입입니다.
        /// </summary>
        public CharacterConstants.HitReactionType BreakReactionType { get; }

        /// <summary>
        /// 슈퍼아머 차감 요청을 생성합니다.
        /// </summary>
        /// <param name="amount">차감할 슈퍼아머 수치입니다.</param>
        /// <param name="attackId">동일 공격 판정을 구분하는 식별자입니다.</param>
        /// <param name="cause">차감이 발생한 원인입니다.</param>
        /// <param name="breakReactionType">브레이크에 전달할 피격 리액션 타입입니다.</param>
        public SuperArmorDamageRequest(
            int amount,
            int attackId,
            SuperArmorDamageCause cause,
            CharacterConstants.HitReactionType breakReactionType)
        {
            Amount = amount;
            AttackId = attackId;
            Cause = cause;
            BreakReactionType = breakReactionType;
        }
    }

    /// <summary>
    /// 슈퍼아머 차감 요청의 처리 결과입니다.
    /// </summary>
    public readonly struct SuperArmorDamageResult
    {
        /// <summary>
        /// 차감이 적용되지 않은 기본 결과입니다.
        /// </summary>
        public static SuperArmorDamageResult None => default;

        /// <summary>
        /// 차감 전 슈퍼아머 수치입니다.
        /// </summary>
        public int PreviousValue { get; }

        /// <summary>
        /// 차감 직후 슈퍼아머 수치입니다.
        /// 즉시 복구 정책이 실행되더라도 이 값은 복구 전 수치를 유지합니다.
        /// </summary>
        public int RemainingValue { get; }

        /// <summary>
        /// 실제로 차감된 슈퍼아머 수치입니다.
        /// </summary>
        public int AppliedAmount { get; }

        /// <summary>
        /// 슈퍼아머가 0에 도달하여 브레이크가 발생했는지 여부입니다.
        /// </summary>
        public bool WasBroken { get; }

        /// <summary>
        /// 요청에 의해 슈퍼아머 값이 실제로 변경되었는지 여부입니다.
        /// </summary>
        public bool WasApplied => AppliedAmount > 0;

        /// <summary>
        /// 슈퍼아머 차감 결과를 생성합니다.
        /// </summary>
        /// <param name="previousValue">차감 전 수치입니다.</param>
        /// <param name="remainingValue">차감 직후 수치입니다.</param>
        /// <param name="appliedAmount">실제 차감 수치입니다.</param>
        /// <param name="wasBroken">브레이크 발생 여부입니다.</param>
        public SuperArmorDamageResult(
            int previousValue,
            int remainingValue,
            int appliedAmount,
            bool wasBroken)
        {
            PreviousValue = previousValue;
            RemainingValue = remainingValue;
            AppliedAmount = appliedAmount;
            WasBroken = wasBroken;
        }
    }

    /// <summary>
    /// 외부 전투 정책에서 슈퍼아머 차감을 요청할 수 있는 캐릭터 포트입니다.
    /// </summary>
    public interface ISuperArmorDamageReceiver
    {
        /// <summary>
        /// 슈퍼아머를 차감하고 기존 브레이크 및 복구 정책을 실행합니다.
        /// </summary>
        /// <param name="request">적용할 슈퍼아머 차감 요청입니다.</param>
        /// <param name="result">실제 차감 및 브레이크 처리 결과입니다.</param>
        /// <returns>슈퍼아머가 실제로 차감되었으면 <see langword="true"/>입니다.</returns>
        bool TryApplySuperArmorDamage(
            in SuperArmorDamageRequest request,
            out SuperArmorDamageResult result);
    }
}
