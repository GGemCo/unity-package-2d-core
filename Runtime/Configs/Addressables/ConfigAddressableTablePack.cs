using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 런타임 테이블 팩 Addressables 정의를 관리합니다.
    /// </summary>
    /// <remarks>
    /// 개별 txt 테이블 정의는 유지하되, 런타임에서는 패키지별 .bytes 팩을 우선 로드하기 위한 키/경로 규칙입니다.
    /// </remarks>
    public static class ConfigAddressableTablePack
    {
        private const string FileExt = ".bytes";

        /// <summary>
        /// Core 패키지 테이블 팩 식별자입니다.
        /// </summary>
        public const string PackageCore = "core";

        /// <summary>
        /// Skill 패키지 테이블 팩 식별자입니다.
        /// </summary>
        public const string PackageSkill = "skill";

        /// <summary>
        /// Affect 패키지 테이블 팩 식별자입니다.
        /// </summary>
        public const string PackageAffect = "affect";

        /// <summary>
        /// 패키지 식별자에 맞는 런타임 테이블 팩 Addressables 정보를 생성합니다.
        /// </summary>
        /// <param name="packageId">패키지 식별자입니다. 예: core, skill, affect.</param>
        /// <returns>테이블 팩 Addressables 정보입니다.</returns>
        public static AddressableAssetInfo Make(string packageId)
        {
            string key = $"{ConfigAddressableKey.TablePack}_{packageId}";
            string path = $"{ConfigAddressablePath.TablePacks}/{packageId}_tables{FileExt}";
            return new AddressableAssetInfo(key, path, ConfigAddressableLabel.TablePack, packageId);
        }

        /// <summary>
        /// Core 테이블 팩 Addressables 정보입니다.
        /// </summary>
        public static readonly AddressableAssetInfo Core = Make(PackageCore);

        /// <summary>
        /// Skill 테이블 팩 Addressables 정보입니다.
        /// </summary>
        public static readonly AddressableAssetInfo Skill = Make(PackageSkill);

        /// <summary>
        /// Affect 테이블 팩 Addressables 정보입니다.
        /// </summary>
        public static readonly AddressableAssetInfo Affect = Make(PackageAffect);

        /// <summary>
        /// 패키지별 런타임 테이블 팩 전체 목록입니다.
        /// </summary>
        public static readonly List<AddressableAssetInfo> All = new()
        {
            Core,
            Skill,
            Affect
        };
    }
}
