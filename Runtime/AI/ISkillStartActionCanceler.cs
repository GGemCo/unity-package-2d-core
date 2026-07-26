namespace GGemCo2DCore
{
    /// <summary>
    /// 스킬 시작 직전에 기존 입력 액션 상태 머신을 정리할 수 있는 공용 인터페이스입니다.
    /// Skill 패키지는 Control 패키지의 구체 구현을 알지 않고, 이 인터페이스를 통해 점프/대시 등의 잔여 액션 취소를 요청합니다.
    /// </summary>
    public interface ISkillStartActionCanceler
    {
        /// <summary>
        /// 스킬 시작 전에 충돌 가능한 입력 액션을 취소합니다.
        /// </summary>
        void CancelActionsOnSkillStart();
    }

    /// <summary>
    /// 강제 발동 스킬 시작 직전에 플레이어의 모든 입력 액션 상태를 정리하는 공용 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// 긴급 탈출처럼 기존 행동을 완전히 덮어써야 하는 스킬만 사용합니다.
    /// Skill 패키지는 Control 패키지의 구체 구현을 참조하지 않고 이 포트를 통해 강제 취소를 요청합니다.
    /// </remarks>
    public interface IForcedSkillStartActionCanceler
    {
        /// <summary>
        /// 기본 공격, 이동 액션, 예약 입력과 강제 이동을 포함한 모든 플레이어 행동을 취소합니다.
        /// </summary>
        void CancelAllActionsOnForcedSkillStart();
    }
}
