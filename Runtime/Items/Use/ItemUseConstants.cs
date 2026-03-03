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
        AddExp = 1,
        /// <summary>PlayerData.UnspentStatPoints 증가</summary>
        AddStatPoints = 2,
        /// <summary>HP 회복</summary>
        AddHp = 3,
        /// <summary>MP 회복</summary>
        AddMp = 4,

        /// <summary>
        /// "소모형 추가 최대 HP(추가 하트)" 추가
        /// - 데미지를 먼저 흡수하고, 0이 되면 즉시 소멸
        /// - 회복/리젠으로 재충전되지 않음
        /// </summary>
        AddItemBonusHp = 7,

        /// <summary>스킬 지급(외부 패키지와 연동을 위한 훅)</summary>
        GrantSkill = 5,

        /// <summary>
        /// Affect 적용(옵션 패키지: com.ggemco.2d.affect)
        /// - Affect 미설치 시 실패 처리
        /// </summary>
        ApplyAffect = 6,

        // <summary>추후 확장용(예: 아이템 지급 등)</summary>
        // GiveItem = 10,
    }
}
