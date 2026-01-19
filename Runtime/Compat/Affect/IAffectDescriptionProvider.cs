namespace GGemCo2DCore
{
    /// <summary>
    /// Affect(버프/디버프) 설명 문자열을 제공하는 런타임 Provider 계약.
    /// Core는 Affect 패키지를 직접 참조하지 않고, 이 인터페이스를 통해서만 기능을 사용합니다.
    /// </summary>
    public interface IAffectDescriptionProvider
    {
        /// <summary>
        /// AffectUid에 대한 설명(여러 줄 가능)을 반환합니다.
        /// </summary>
        /// <param name="affectUid">Affect 고유 ID</param>
        /// <returns>설명 문자열(없으면 빈 문자열)</returns>
        string GetDescription(int affectUid);

        /// <summary>
        /// 확률(예: 스킬/아이템 발동률) 접두 문구를 포함한 설명을 반환합니다.
        /// </summary>
        /// <param name="affectUid">Affect 고유 ID</param>
        /// <param name="chancePercent">확률(퍼센트 단위)</param>
        /// <returns>설명 문자열(없으면 빈 문자열)</returns>
        string GetDescriptionWithChancePrefix(int affectUid, float chancePercent);
    }
}