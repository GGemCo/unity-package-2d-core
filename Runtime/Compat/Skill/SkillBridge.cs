namespace GGemCo2DCore
{
    /// <summary>
    /// Core와 Skill 패키지 사이의 선택적 런타임 연결을 담당하는 브리지입니다.
    /// </summary>
    public static class SkillBridge
    {
        private static ISkillNeedMpProvider _needMpProvider = NullSkillNeedMpProvider.Instance;

        /// <summary>
        /// 스킬 NeedMp 정보를 제공하는 Provider입니다.
        /// Skill 패키지가 설치되지 않았거나 등록 전이면 Null Provider가 사용됩니다.
        /// </summary>
        public static ISkillNeedMpProvider NeedMpProvider
        {
            get => _needMpProvider ?? NullSkillNeedMpProvider.Instance;
            set => _needMpProvider = value ?? NullSkillNeedMpProvider.Instance;
        }

        /// <summary>
        /// 현재 실제 Skill Provider가 등록되어 있는지 여부입니다.
        /// </summary>
        public static bool HasNeedMpProvider => !ReferenceEquals(NeedMpProvider, NullSkillNeedMpProvider.Instance);

        /// <summary>
        /// 외부 Skill 패키지에서 NeedMp Provider를 안전하게 등록합니다.
        /// </summary>
        /// <param name="provider">등록할 NeedMp Provider입니다.</param>
        public static void SetNeedMpProvider(ISkillNeedMpProvider provider)
        {
            NeedMpProvider = provider;
        }
    }
}
