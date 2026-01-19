namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 옵션이 영향을 주는 도메인.
    /// - Stat: 수치 스탯(공격력/방어력/HP/저항 등)
    /// - State: 상태이상(독/빙결/기절 등)
    /// - DamageType: 데미지 타입(화염/냉기/번개 등)
    /// - Affect: Affect 시스템(착용 중 지속/트리거형 등)
    /// </summary>
    public enum ItemOptionKind
    {
        Stat = 0,
        State = 1,
        DamageType = 2,
        Affect = 3,
    }
}
