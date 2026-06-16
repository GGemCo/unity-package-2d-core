namespace GGemCo2DCore
{
    /// <summary>
    /// 전투 피드백을 게임별 MP 획득 보상으로 변환하는 규칙 Provider입니다.
    /// </summary>
    public interface IMpGainRuleProvider
    {
        /// <summary>
        /// 현재 전투 피드백 컨텍스트에서 지급할 MP 보상이 있는지 확인합니다.
        /// </summary>
        /// <param name="context">MP 획득 판정을 위한 전투 피드백 컨텍스트입니다.</param>
        /// <param name="reward">지급할 MP 보상 정보입니다.</param>
        /// <returns>지급할 보상이 있으면 <see langword="true"/>입니다.</returns>
        bool TryGetMpGainReward(in MpGainContext context, out MpGainReward reward);
    }
}
