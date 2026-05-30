namespace GGemCo2DCore
{
    /// <summary>
    /// 현재 실행 중인 기본 공격 콤보 단계 정보를 제공하는 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// Core는 Control 패키지의 <c>ActionAttack</c> 구현을 직접 참조하지 않기 때문에,
    /// 상위 입력 계층은 이 인터페이스로 현재 기본 공격 콤보 상태만 전달합니다.
    /// </remarks>
    public interface IAttackComboStateProvider
    {
        /// <summary>
        /// 현재 기본 공격 콤보 단계 정보를 조회합니다.
        /// </summary>
        /// <param name="state">현재 기본 공격 콤보 단계 정보입니다.</param>
        /// <returns>유효한 기본 공격 콤보가 진행 중이면 <see langword="true"/>를 반환합니다.</returns>
        bool TryGetCurrentAttackComboState(out AttackComboRuntimeState state);
    }

    /// <summary>
    /// 기본 공격 콤보의 현재 단계와 전체 단계 수를 담는 런타임 값입니다.
    /// </summary>
    public readonly struct AttackComboRuntimeState
    {
        /// <summary>
        /// 현재 기본 공격 콤보 인덱스입니다.
        /// </summary>
        public int ComboIndex { get; }

        /// <summary>
        /// 기본 공격 콤보 전체 개수입니다.
        /// </summary>
        public int ComboCount { get; }

        /// <summary>
        /// 현재 기본 공격이 마지막 콤보 단계인지 여부입니다.
        /// </summary>
        public bool IsLastCombo => ComboCount > 0 && ComboIndex == ComboCount - 1;

        /// <summary>
        /// 기본 공격 콤보 상태 값을 생성합니다.
        /// </summary>
        /// <param name="comboIndex">현재 기본 공격 콤보 인덱스입니다.</param>
        /// <param name="comboCount">기본 공격 콤보 전체 개수입니다.</param>
        public AttackComboRuntimeState(int comboIndex, int comboCount)
        {
            ComboIndex = comboIndex;
            ComboCount = comboCount;
        }
    }
}
