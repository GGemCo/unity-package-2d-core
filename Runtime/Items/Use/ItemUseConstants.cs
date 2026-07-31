namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 사용 시스템 공통 상수/열거형
    /// </summary>
    public enum ItemUseFailPolicy
    {
        /// <summary>
        /// 기본값
        /// - 모든 Action이 CanExecute/Execute를 통과해야 성공
        /// - 실패 시 적용/소모 없음(실패 메시지 반환)
        /// </summary>
        AllOrNothing = 0,
    }

    public enum ItemUseActionType
    {
        None = 0,

        /// <summary>PlayerData.CurrentExp 증가</summary>
        AddExp,
        /// <summary>PlayerData.UnspentStatPoints 증가</summary>
        AddStatPoints,
        /// <summary>HP 회복</summary>
        AddHp,
        /// <summary>MP 회복</summary>
        AddMp,

        /// <summary>
        /// 아이템 사용으로 "일반 최대 HP"를 영구적으로 증가합니다.
        /// - ParamIntA = amount
        /// </summary>
        AddMaxHpNormal,

        /// <summary>
        /// 아이템 사용으로 "임시 최대 HP"를 증가합니다.
        /// - ParamIntA = 누적 증가량 또는 충전 목표값
        /// - ParamStringB = <see cref="ItemTempHpApplyPolicy"/> 이름, 빈 값은 Add
        /// </summary>
        AddMaxHpTemp,

        /// <summary>액티브 스킬 지급(외부 패키지와 연동을 위한 훅)</summary>
        GrantSkill,

        /// <summary>패시브 스킬 지급(외부 패키지와 연동을 위한 훅)</summary>
        GrantSkillPassive,

        /// <summary>
        /// Affect 적용(옵션 패키지: com.ggemco.2d.affect)
        /// - Affect 미설치 시 실패 처리
        /// </summary>
        ApplyAffect,

        // <summary>추후 확장용(예: 아이템 지급 등)</summary>
        // GiveItem = 10,
    }

    /// <summary>
    /// 아이템으로 획득하는 임시 HP의 적용 방식을 정의합니다.
    /// </summary>
    public enum ItemTempHpApplyPolicy
    {
        /// <summary>
        /// 설정값만큼 임시 HP 최대치와 현재치를 누적해서 증가시킵니다.
        /// 기존 <see cref="ItemUseActionType.AddMaxHpTemp"/> 동작과 같습니다.
        /// </summary>
        Add = 0,

        /// <summary>
        /// 임시 HP 현재치를 설정값까지 충전합니다.
        /// 현재치가 이미 설정값 이상이면 아이템을 사용할 수 없습니다.
        /// </summary>
        RefillToTarget = 1,
    }
}
