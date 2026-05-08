using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Android Keystore를 사용해 저장 데이터를 암호화하고 복호화하는 구현체입니다.
    /// </summary>
    internal sealed class AndroidKeystoreSaveDataCryptoProvider : ISaveDataCryptoProvider
    {
        private const string BridgeClassName = "com.ggemco.core.crypto.SaveDataCryptoBridge";

        /// <summary>
        /// Android Player 환경에서만 사용할 수 있는지 여부입니다.
        /// </summary>
        public bool IsAvailable
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Android Keystore 키로 평문 저장 데이터를 암호화합니다.
        /// </summary>
        /// <param name="plainText">직렬화된 평문 저장 데이터입니다.</param>
        /// <param name="context">저장 파일별 암호화 컨텍스트입니다.</param>
        /// <returns>파일에 기록할 암호화 Envelope입니다.</returns>
        public string EncryptToText(string plainText, SaveDataCryptoContext context)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaClass bridge = new AndroidJavaClass(BridgeClassName))
            {
                return bridge.CallStatic<string>(
                    "encrypt",
                    plainText ?? string.Empty,
                    context.KeyAlias,
                    context.AssociatedData);
            }
#else
            throw new SaveDataCryptoException("Android Keystore 암호화는 Android Player에서만 사용할 수 있습니다.");
#endif
        }

        /// <summary>
        /// Android Keystore 키로 암호화된 저장 데이터를 복호화합니다.
        /// </summary>
        /// <param name="encryptedText">파일에서 읽은 암호화 Envelope입니다.</param>
        /// <param name="context">저장 파일별 암호화 컨텍스트입니다.</param>
        /// <returns>역직렬화에 사용할 평문 저장 데이터입니다.</returns>
        public string DecryptToText(string encryptedText, SaveDataCryptoContext context)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaClass bridge = new AndroidJavaClass(BridgeClassName))
            {
                return bridge.CallStatic<string>(
                    "decrypt",
                    encryptedText ?? string.Empty,
                    context.KeyAlias,
                    context.AssociatedData);
            }
#else
            throw new SaveDataCryptoException("Android Keystore 복호화는 Android Player에서만 사용할 수 있습니다.");
#endif
        }
    }
}
