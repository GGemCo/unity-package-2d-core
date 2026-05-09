using System;
using System.IO;

namespace GGemCo2DCore
{
    /// <summary>
    /// 저장 데이터 파일 입출력 시 암호화와 복호화를 중앙에서 처리합니다.
    /// </summary>
    public static class SaveDataFileService
    {
        /// <summary>
        /// 저장 데이터를 파일에 기록합니다.
        /// </summary>
        /// <param name="filePath">기록할 파일 경로입니다.</param>
        /// <param name="plainText">직렬화된 평문 저장 데이터입니다.</param>
        public static void WriteAllText(string filePath, string plainText)
        {
            WriteAllText(filePath, plainText, null);
        }

        /// <summary>
        /// 저장 데이터를 논리 저장 식별자와 함께 파일에 기록합니다.
        /// </summary>
        /// <param name="filePath">기록할 파일 경로입니다.</param>
        /// <param name="plainText">직렬화된 평문 저장 데이터입니다.</param>
        /// <param name="identity">암호화 AAD를 구성할 논리 저장 식별자입니다.</param>
        public static void WriteAllText(string filePath, string plainText, SaveDataIdentity identity)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("저장 파일 경로가 비어 있습니다.", nameof(filePath));
            }

            string directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string fileText = SaveDataCryptoService.EncryptForWrite(filePath, plainText ?? string.Empty, identity);
            File.WriteAllText(filePath, fileText);
        }

        /// <summary>
        /// 저장 데이터 파일을 읽고 역직렬화 가능한 평문 문자열로 반환합니다.
        /// </summary>
        /// <param name="filePath">읽을 파일 경로입니다.</param>
        /// <returns>평문 저장 데이터 문자열입니다.</returns>
        public static string ReadAllText(string filePath)
        {
            return ReadAllText(filePath, null);
        }

        /// <summary>
        /// 저장 데이터 파일을 논리 저장 식별자로 검증하고 평문 문자열로 반환합니다.
        /// </summary>
        /// <param name="filePath">읽을 파일 경로입니다.</param>
        /// <param name="identity">암호화 AAD를 구성할 논리 저장 식별자입니다.</param>
        /// <returns>평문 저장 데이터 문자열입니다.</returns>
        public static string ReadAllText(string filePath, SaveDataIdentity identity)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("저장 파일 경로가 비어 있습니다.", nameof(filePath));
            }

            string fileText = File.ReadAllText(filePath);
            return SaveDataCryptoService.DecryptAfterRead(filePath, fileText, identity);
        }
    }
}
