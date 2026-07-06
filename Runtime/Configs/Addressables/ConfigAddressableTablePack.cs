namespace GGemCo2DCore
{
    /// <summary>
    /// Core 런타임 테이블 pack Addressables 정의를 관리합니다.
    /// </summary>
    /// <remarks>
    /// pack 파일의 공통 키/경로 생성 규칙은 제공하되, 상위 패키지의 pack 정의는 각 패키지 Config에서 소유합니다.
    /// </remarks>
    public static class ConfigAddressableTablePack
    {
        private const string FileExt = ".bytes";

        /// <summary>
        /// Core 패키지 테이블 pack 식별자입니다.
        /// </summary>
        public const string PackageCore = "core";

        /// <summary>
        /// 패키지 식별자에 맞는 런타임 테이블 pack Addressables 정보를 생성합니다.
        /// </summary>
        /// <param name="packageId">패키지 식별자입니다.</param>
        /// <returns>테이블 pack Addressables 정보입니다.</returns>
        public static AddressableAssetInfo Make(string packageId)
        {
            string key = $"{ConfigAddressableKey.TablePack}_{packageId}";
            string path = $"{ConfigAddressablePath.TablePacks}/{packageId}_tables{FileExt}";
            return new AddressableAssetInfo(key, path, ConfigAddressableLabel.TablePack, packageId);
        }

        /// <summary>
        /// Core 테이블 pack Addressables 정보입니다.
        /// </summary>
        public static readonly AddressableAssetInfo Core = Make(PackageCore);
    }
}
