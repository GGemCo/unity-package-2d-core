namespace GGemCo2DCore
{
    /// <summary>
    /// 현재 기본 공격 콤보 단계의 데미지 공식 설정을 제공하는 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// Core는 Control 패키지의 <c>GGemCoAttackComboSettings</c>를 직접 참조하지 않습니다.
    /// Control 계층은 이 인터페이스를 구현하여 현재 콤보 단계의 공식 설정만 Core에 전달합니다.
    /// </remarks>
    public interface IAttackComboDamageFormulaProvider
    {
        /// <summary>
        /// 현재 기본 공격 콤보 단계에 설정된 데미지 공식 정보를 조회합니다.
        /// </summary>
        /// <param name="settings">현재 콤보 단계의 데미지 공식 설정입니다.</param>
        /// <returns>유효한 공식 설정을 제공할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        bool TryGetCurrentAttackComboDamageFormula(out AttackComboDamageFormulaSettings settings);
    }
}
