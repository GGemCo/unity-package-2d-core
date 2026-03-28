namespace GGemCo2DCore
{
    /// <summary>
    /// 스킬 체인 가능 상태가 열렸을 때 재생할 피드백(예: 잔상, 이펙트, UI 강조)을 추상화합니다.
    /// Skill 패키지는 이 인터페이스만 호출하고, 실제 연출 구현은 Core 쪽 컴포넌트가 담당합니다.
    /// </summary>
    public interface ISkillChainReadyFeedback
    {
        /// <summary>
        /// 스킬 체인 가능 상태 연출을 시작합니다.
        /// 이미 재생 중이어도 안전하게 다시 호출할 수 있어야 합니다.
        /// </summary>
        void PlaySkillChainReady();

        /// <summary>
        /// 스킬 체인 가능 상태 연출을 정지합니다.
        /// 재생 중이 아니어도 안전하게 호출할 수 있어야 합니다.
        /// </summary>
        void StopSkillChainReady();
    }
}
