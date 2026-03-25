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
}
