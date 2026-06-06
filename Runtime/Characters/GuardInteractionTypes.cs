namespace GGemCo2DCore
{

    /// <summary>
    /// 스킬 타격이 가드 시스템에서 어떤 방어 압박 등급으로 처리될지 정의합니다.
    /// </summary>
    public enum GuardAttackType
    {
        /// <summary>
        /// 가드 불가능
        /// </summary>
        None = -1,
        
        /// <summary>
        /// 일반 공격입니다. 기본 가드 규칙으로 처리합니다.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// 중간 강도의 공격입니다. 설정에 따라 가드 성공, 밀림, 브레이크 등을 분기할 수 있습니다.
        /// </summary>
        Medium = 1,

        /// <summary>
        /// 강한 공격입니다. 설정에 따라 가드 브레이크 후보로 사용할 수 있습니다.
        /// </summary>
        Heavy = 2,

        /// <summary>
        /// 특수 공격입니다. 설정에 따라 강제 가드 브레이크 후보로 사용할 수 있습니다.
        /// </summary>
        Ultimate = 3,
        
        /// <summary>
        /// 필살기 공격입니다. 카운터로만 가능
        /// </summary>
        Special = 4,
    }
    /// <summary>
    /// 들어오는 공격이 가드 상태와 상호작용하는 방식을 정의합니다.
    /// </summary>
    public enum GuardInteractionMode
    {
        /// <summary>
        /// 기존 가드/저스트 가드 판정 규칙을 그대로 사용합니다.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// 현재 가드 상태를 무시하고 일반 피격으로 처리합니다.
        /// </summary>
        IgnoreGuard = 1,

        /// <summary>
        /// 일반 가드를 파괴하고 가드 브레이크 결과로 처리합니다.
        /// </summary>
        BreakGuard = 2,
    }

    /// <summary>
    /// 가드 브레이크 공격이 저스트 가드와 만났을 때의 처리 정책입니다.
    /// </summary>
    public enum GuardBreakJustGuardPolicy
    {
        /// <summary>
        /// 저스트 가드 타이밍이어도 가드를 파괴합니다.
        /// </summary>
        BreakEvenJustGuard = 0,

        /// <summary>
        /// 저스트 가드 타이밍이면 가드 브레이크를 막고 저스트 가드로 처리합니다.
        /// </summary>
        JustGuardCanBlock = 1,

        /// <summary>
        /// 저스트 가드 타이밍이어도 저스트 보상 없이 일반 가드로 처리합니다.
        /// </summary>
        TreatAsNormalGuard = 2,
    }

    /// <summary>
    /// 피격 직전 가드 판정의 최종 결과 타입입니다.
    /// </summary>
    public enum GuardResolutionOutcome
    {
        /// <summary>
        /// 가드 판정이 성립하지 않았거나 별도 결과가 없습니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// 일반 가드로 공격을 처리했습니다.
        /// </summary>
        Guarded = 1,

        /// <summary>
        /// 저스트 가드로 공격을 처리했습니다.
        /// </summary>
        JustGuarded = 2,

        /// <summary>
        /// 가드가 파괴되어 가드 브레이크로 처리했습니다.
        /// </summary>
        GuardBroken = 3,
    }
}
