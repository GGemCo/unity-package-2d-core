namespace GGemCo2DCore
{
    /// <summary>
    /// 퀵슬롯에 저장되는 컨텐츠 종류.
    /// - Core 는 Skill 패키지를 직접 참조하지 않고 Kind 값만 저장한다.
    /// </summary>
    public enum QuickSlotContentKind
    {
        None = 0,
        Skill = 1,
        Item = 2
    }
}
