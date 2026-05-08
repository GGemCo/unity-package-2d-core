namespace GGemCo2DCore
{
    /// <summary>
    /// 저장 데이터 암호화와 복호화에 필요한 파일별 컨텍스트입니다.
    /// </summary>
    public sealed class SaveDataCryptoContext
    {
        /// <summary>
        /// 암호화 대상 저장 파일의 전체 경로입니다.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// 플랫폼 보안 저장소에서 사용할 키 별칭입니다.
        /// </summary>
        public string KeyAlias { get; }

        /// <summary>
        /// 암호문을 특정 저장 파일에 묶기 위해 사용하는 추가 인증 데이터입니다.
        /// </summary>
        public string AssociatedData { get; }

        /// <summary>
        /// 저장 데이터 암호화 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="filePath">암호화 대상 저장 파일의 전체 경로입니다.</param>
        /// <param name="keyAlias">플랫폼 보안 저장소에서 사용할 키 별칭입니다.</param>
        /// <param name="associatedData">암호문 검증에 사용할 추가 인증 데이터입니다.</param>
        public SaveDataCryptoContext(string filePath, string keyAlias, string associatedData)
        {
            FilePath = filePath ?? string.Empty;
            KeyAlias = string.IsNullOrEmpty(keyAlias) ? SaveDataCryptoService.DefaultKeyAlias : keyAlias;
            AssociatedData = associatedData ?? string.Empty;
        }
    }
}
