namespace GGemCo2DCore
{
    /// <summary>
    /// Skill 패키지가 등록되지 않았을 때 사용하는 기본 NeedMp Provider입니다.
    /// </summary>
    internal sealed class NullSkillNeedMpProvider : ISkillNeedMpProvider
    {
        /// <summary>
        /// 공유 기본 인스턴스입니다.
        /// </summary>
        public static readonly NullSkillNeedMpProvider Instance = new NullSkillNeedMpProvider();

        private NullSkillNeedMpProvider()
        {
        }

        /// <inheritdoc />
        public bool TryGetNeedMp(int skillUid, out int needMp)
        {
            needMp = 0;
            return false;
        }
    }
}
