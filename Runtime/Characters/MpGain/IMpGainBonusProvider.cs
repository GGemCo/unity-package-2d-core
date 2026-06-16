namespace GGemCo2DCore
{
    /// <summary>
    /// MP 획득량을 최종 지급 직전에 보정하는 Provider 포트입니다.
    /// </summary>
    public interface IMpGainBonusProvider
    {
        /// <summary>
        /// 기본 MP 획득량에 보정을 적용한 최종 획득량을 계산합니다.
        /// </summary>
        /// <param name="baseAmount">보정 전 기본 MP 획득량입니다.</param>
        /// <returns>보정 후 실제 지급할 MP 획득량입니다.</returns>
        int EvaluateBonusMp(int baseAmount);
    }
}
