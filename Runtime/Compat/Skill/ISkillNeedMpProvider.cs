namespace GGemCo2DCore
{
    /// <summary>
    /// 스킬 UID로 스킬 사용 MP 정보를 제공하는 런타임 Provider 계약입니다.
    /// Core는 Skill 패키지를 직접 참조하지 않고 이 인터페이스를 통해 필요한 값만 조회합니다.
    /// </summary>
    public interface ISkillNeedMpProvider
    {
        /// <summary>
        /// 지정한 스킬 UID에 해당하는 NeedMp 값을 조회합니다.
        /// </summary>
        /// <param name="skillUid">조회할 액티브 스킬 UID입니다.</param>
        /// <param name="needMp">조회된 NeedMp 값입니다.</param>
        /// <returns>스킬 정보를 찾았으면 <see langword="true"/>, 없으면 <see langword="false"/>입니다.</returns>
        bool TryGetNeedMp(int skillUid, out int needMp);
    }
}
