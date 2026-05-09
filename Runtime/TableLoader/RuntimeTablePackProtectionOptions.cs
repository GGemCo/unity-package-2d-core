namespace GGemCo2DCore
{
    /// <summary>
    /// 런타임 테이블 팩에 적용할 압축 방식을 정의합니다.
    /// </summary>
    public enum RuntimeTablePackCompressionMode
    {
        /// <summary>
        /// 테이블 팩 원본 바이너리를 압축하지 않습니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// GZip 스트림으로 테이블 팩 원본 바이너리를 압축합니다.
        /// </summary>
        GZip = 1,
    }

    /// <summary>
    /// 런타임 테이블 팩에 적용할 암호화 방식을 정의합니다.
    /// </summary>
    public enum RuntimeTablePackEncryptionMode
    {
        /// <summary>
        /// 테이블 팩 payload를 암호화하지 않습니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// AES-CBC로 암호화하고 HMAC-SHA256으로 무결성을 검증합니다.
        /// </summary>
        AesCbcHmacSha256 = 1,
    }

    /// <summary>
    /// 런타임 테이블 팩 보호 계층에 사용할 옵션입니다.
    /// </summary>
    /// <remarks>
    /// 기본값은 압축 후 암호화입니다. 원본 txt 테이블은 유지하고, Addressables에 올라가는 파생 팩만 보호합니다.
    /// </remarks>
    public sealed class RuntimeTablePackProtectionOptions
    {
        /// <summary>
        /// 테이블 팩 암호화 키 별칭 기본값입니다.
        /// </summary>
        public const string DefaultKeyAlias = "ggemco_table_pack_key_v1";

        /// <summary>
        /// 런타임 테이블 팩에 적용할 기본 보호 옵션입니다.
        /// </summary>
        public static RuntimeTablePackProtectionOptions Default =>
            new RuntimeTablePackProtectionOptions(
                RuntimeTablePackCompressionMode.GZip,
                RuntimeTablePackEncryptionMode.AesCbcHmacSha256,
                DefaultKeyAlias);

        /// <summary>
        /// 테이블 팩 payload 압축 방식입니다.
        /// </summary>
        public RuntimeTablePackCompressionMode CompressionMode { get; }

        /// <summary>
        /// 테이블 팩 payload 암호화 방식입니다.
        /// </summary>
        public RuntimeTablePackEncryptionMode EncryptionMode { get; }

        /// <summary>
        /// 키 파생에 사용할 논리 키 별칭입니다.
        /// </summary>
        public string KeyAlias { get; }

        /// <summary>
        /// 런타임 테이블 팩 보호 옵션을 생성합니다.
        /// </summary>
        /// <param name="compressionMode">테이블 팩 payload 압축 방식입니다.</param>
        /// <param name="encryptionMode">테이블 팩 payload 암호화 방식입니다.</param>
        /// <param name="keyAlias">키 파생에 사용할 논리 키 별칭입니다.</param>
        public RuntimeTablePackProtectionOptions(
            RuntimeTablePackCompressionMode compressionMode,
            RuntimeTablePackEncryptionMode encryptionMode,
            string keyAlias = DefaultKeyAlias)
        {
            CompressionMode = compressionMode;
            EncryptionMode = encryptionMode;
            KeyAlias = string.IsNullOrWhiteSpace(keyAlias) ? DefaultKeyAlias : keyAlias.Trim();
        }
    }

    /// <summary>
    /// 테이블 팩 암복호화에 필요한 패키지별 보안 문맥입니다.
    /// </summary>
    /// <remarks>
    /// SaveData의 슬롯/파일 AAD와 역할이 비슷하지만, 테이블 팩은 배포 리소스이므로 패키지 식별자를 기준으로 묶습니다.
    /// </remarks>
    public sealed class RuntimeTablePackSecurityContext
    {
        /// <summary>
        /// 테이블 팩이 속한 패키지 식별자입니다.
        /// </summary>
        public string PackageId { get; }

        /// <summary>
        /// 키 파생에 사용할 논리 키 별칭입니다.
        /// </summary>
        public string KeyAlias { get; }

        /// <summary>
        /// 암호문을 특정 테이블 팩 문맥에 묶기 위한 추가 인증 데이터입니다.
        /// </summary>
        public string AssociatedData { get; }

        /// <summary>
        /// 테이블 팩 보안 문맥을 생성합니다.
        /// </summary>
        /// <param name="packageId">테이블 팩이 속한 패키지 식별자입니다.</param>
        /// <param name="keyAlias">키 파생에 사용할 논리 키 별칭입니다.</param>
        public RuntimeTablePackSecurityContext(string packageId, string keyAlias)
        {
            PackageId = NormalizePart(packageId, "unknown");
            KeyAlias = string.IsNullOrWhiteSpace(keyAlias) ? RuntimeTablePackProtectionOptions.DefaultKeyAlias : keyAlias.Trim();
            AssociatedData = $"ggemco-table-pack:v1:package:{PackageId}:key:{KeyAlias}";
        }

        /// <summary>
        /// AAD 구성 요소를 안전한 기본 문자열로 정규화합니다.
        /// </summary>
        /// <param name="value">정규화할 문자열입니다.</param>
        /// <param name="fallback">값이 비었을 때 사용할 기본 문자열입니다.</param>
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
