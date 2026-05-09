namespace GGemCo2DCore
{
    /// <summary>
    /// 저장 파일 경로와 분리된 논리 저장 데이터 식별자입니다.
    /// </summary>
    public sealed class SaveDataIdentity
    {
        /// <summary>
        /// 저장 데이터 AAD 문자열에 사용하는 현재 버전입니다.
        /// </summary>
        public const string AadVersion = "v2";

        /// <summary>
        /// 계정 또는 프로필 구분이 없을 때 사용하는 기본 프로필 ID입니다.
        /// </summary>
        public const string DefaultProfileId = "local";

        /// <summary>
        /// Core 저장 데이터 영역 ID입니다.
        /// </summary>
        public const string ScopeCore = "core";

        /// <summary>
        /// Skill 저장 데이터 영역 ID입니다.
        /// </summary>
        public const string ScopeSkill = "skill";

        /// <summary>
        /// TimingBattle 저장 데이터 영역 ID입니다.
        /// </summary>
        public const string ScopeTimingBattle = "timing-battle";

        /// <summary>
        /// 저장 데이터가 속한 프로필 ID입니다.
        /// </summary>
        public string ProfileId { get; }

        /// <summary>
        /// 저장 데이터가 속한 논리 슬롯 번호입니다.
        /// </summary>
        public int SlotIndex { get; }

        /// <summary>
        /// 저장 데이터 영역 ID입니다.
        /// </summary>
        public string Scope { get; }

        /// <summary>
        /// 저장 데이터 논리 식별자를 생성합니다.
        /// </summary>
        /// <param name="slotIndex">저장 데이터가 속한 논리 슬롯 번호입니다.</param>
        /// <param name="scope">저장 데이터 영역 ID입니다.</param>
        /// <param name="profileId">저장 데이터가 속한 프로필 ID입니다.</param>
        public SaveDataIdentity(int slotIndex, string scope, string profileId = DefaultProfileId)
        {
            SlotIndex = slotIndex;
            Scope = NormalizePart(scope, ScopeCore);
            ProfileId = NormalizePart(profileId, DefaultProfileId);
        }

        /// <summary>
        /// Core 저장 데이터용 논리 식별자를 생성합니다.
        /// </summary>
        /// <param name="slotIndex">저장 데이터가 속한 논리 슬롯 번호입니다.</param>
        /// <returns>Core 저장 데이터 논리 식별자입니다.</returns>
        public static SaveDataIdentity Core(int slotIndex)
        {
            return new SaveDataIdentity(slotIndex, ScopeCore);
        }

        /// <summary>
        /// Skill 저장 데이터용 논리 식별자를 생성합니다.
        /// </summary>
        /// <param name="slotIndex">저장 데이터가 속한 논리 슬롯 번호입니다.</param>
        /// <returns>Skill 저장 데이터 논리 식별자입니다.</returns>
        public static SaveDataIdentity Skill(int slotIndex)
        {
            return new SaveDataIdentity(slotIndex, ScopeSkill);
        }

        /// <summary>
        /// TimingBattle 저장 데이터용 논리 식별자를 생성합니다.
        /// </summary>
        /// <param name="slotIndex">저장 데이터가 속한 논리 슬롯 번호입니다.</param>
        /// <returns>TimingBattle 저장 데이터 논리 식별자입니다.</returns>
        public static SaveDataIdentity TimingBattle(int slotIndex)
        {
            return new SaveDataIdentity(slotIndex, ScopeTimingBattle);
        }

        /// <summary>
        /// 암호화 추가 인증 데이터로 사용할 안정적인 문자열을 생성합니다.
        /// </summary>
        /// <returns>파일명과 무관한 논리 슬롯 기반 AAD 문자열입니다.</returns>
        public string ToAssociatedData()
        {
            return $"ggemco-save:{AadVersion}:profile:{ProfileId}:slot:{SlotIndex}:scope:{Scope}";
        }

        /// <summary>
        /// AAD 구성 요소를 안전한 기본 문자열로 정규화합니다.
        /// </summary>
        /// <param name="value">정규화할 문자열입니다.</param>
        /// <param name="fallback">값이 비어 있을 때 사용할 기본 문자열입니다.</param>
        /// <returns>AAD 구성에 사용할 정규화된 문자열입니다.</returns>
        private static string NormalizePart(string value, string fallback)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            return normalized
                .Replace(":", "_")
                .Replace("/", "_")
                .Replace("\\", "_")
                .ToLowerInvariant();
        }
    }
}
