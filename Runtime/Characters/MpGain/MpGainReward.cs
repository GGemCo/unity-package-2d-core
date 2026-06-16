namespace GGemCo2DCore
{
    /// <summary>
    /// MP 획득 규칙 Provider가 반환하는 보상 정보입니다.
    /// </summary>
    public readonly struct MpGainReward
    {
        /// <summary>
        /// 보상 종류입니다. 같은 공격 중복 지급 기록을 분리하는 키로 사용됩니다.
        /// </summary>
        public MpGainRewardKind Kind { get; }

        /// <summary>
        /// 보정 전 기본 MP 획득량입니다.
        /// </summary>
        public int Amount { get; }

        /// <summary>
        /// 같은 AttackId 또는 같은 프레임에서 반복 지급을 허용할지 여부입니다.
        /// </summary>
        public bool AllowMultipleRewardsPerAttack { get; }

        /// <summary>
        /// 지급 가능한 보상인지 여부입니다.
        /// </summary>
        public bool IsValid => Kind != MpGainRewardKind.None && Amount > 0;

        /// <summary>
        /// MP 보상 정보를 생성합니다.
        /// </summary>
        /// <param name="kind">보상 종류입니다.</param>
        /// <param name="amount">보정 전 기본 MP 획득량입니다.</param>
        /// <param name="allowMultipleRewardsPerAttack">같은 공격 판정 반복 지급 허용 여부입니다.</param>
        private MpGainReward(
            MpGainRewardKind kind,
            int amount,
            bool allowMultipleRewardsPerAttack)
        {
            Kind = amount > 0 ? kind : MpGainRewardKind.None;
            Amount = amount > 0 ? amount : 0;
            AllowMultipleRewardsPerAttack = allowMultipleRewardsPerAttack;
        }

        /// <summary>
        /// 지급할 보상이 없음을 나타내는 값을 반환합니다.
        /// </summary>
        public static MpGainReward None => new(MpGainRewardKind.None, 0, false);

        /// <summary>
        /// 지급할 MP 보상 정보를 생성합니다.
        /// </summary>
        /// <param name="kind">보상 종류입니다.</param>
        /// <param name="amount">보정 전 기본 MP 획득량입니다.</param>
        /// <param name="allowMultipleRewardsPerAttack">같은 공격 판정 반복 지급 허용 여부입니다.</param>
        /// <returns>생성된 MP 보상 정보입니다.</returns>
        public static MpGainReward Create(
            MpGainRewardKind kind,
            int amount,
            bool allowMultipleRewardsPerAttack)
        {
            return new MpGainReward(kind, amount, allowMultipleRewardsPerAttack);
        }
    }
}
