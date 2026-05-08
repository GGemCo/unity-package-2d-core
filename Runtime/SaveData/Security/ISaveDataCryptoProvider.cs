namespace GGemCo2DCore
{
    /// <summary>
    /// 플랫폼별 저장 데이터 암호화 구현체가 제공해야 하는 기능입니다.
    /// </summary>
    public interface ISaveDataCryptoProvider
    {
        /// <summary>
        /// 현재 실행 환경에서 이 암호화 구현체를 사용할 수 있는지 여부입니다.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 평문 저장 데이터를 암호화된 텍스트로 변환합니다.
        /// </summary>
        /// <param name="plainText">직렬화된 평문 저장 데이터입니다.</param>
        /// <param name="context">저장 파일별 암호화 컨텍스트입니다.</param>
        /// <returns>파일에 기록할 암호화 텍스트입니다.</returns>
        string EncryptToText(string plainText, SaveDataCryptoContext context);

        /// <summary>
        /// 암호화된 저장 텍스트를 평문 저장 데이터로 복호화합니다.
        /// </summary>
        /// <param name="encryptedText">파일에서 읽어온 암호화 텍스트입니다.</param>
        /// <param name="context">저장 파일별 암호화 컨텍스트입니다.</param>
        /// <returns>역직렬화에 사용할 평문 저장 데이터입니다.</returns>
        string DecryptToText(string encryptedText, SaveDataCryptoContext context);
    }
}
