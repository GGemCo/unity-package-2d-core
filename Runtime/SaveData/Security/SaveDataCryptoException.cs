using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 저장 데이터 암호화 또는 복호화 중 발생한 오류를 나타냅니다.
    /// </summary>
    public sealed class SaveDataCryptoException : Exception
    {
        /// <summary>
        /// 저장 데이터 암호화 오류를 생성합니다.
        /// </summary>
        /// <param name="message">오류 설명입니다.</param>
        public SaveDataCryptoException(string message) : base(message)
        {
        }

        /// <summary>
        /// 내부 예외를 포함한 저장 데이터 암호화 오류를 생성합니다.
        /// </summary>
        /// <param name="message">오류 설명입니다.</param>
        /// <param name="innerException">원인이 된 내부 예외입니다.</param>
        public SaveDataCryptoException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
