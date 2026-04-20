namespace GGemCo2DCore
{
    /// <summary>
    /// UIElementStat의 +/- 입력을 수신하여 드래프트 투자 값을 변경하는 핸들러입니다.
    /// </summary>
    public interface IStatPointDraftChangeHandler
    {
        bool TryChangeDraft(CharacterConstants.IndexPlayerInfo statType, int delta);
    }
}
