using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GGemCo2DCore
{
    /// <summary>
    /// 저장 데이터 암호화 정책을 해석하고 플랫폼별 암호화 구현체를 호출합니다.
    /// </summary>
    public static class SaveDataCryptoService
    {
        private static bool _providerUnavailableWarningLogged;
        private static bool _legacyAssociatedDataWarningLogged;

        /// <summary>
        /// 암호화 Envelope를 식별하기 위한 고정 문자열입니다.
        /// </summary>
        public const string EnvelopeMagic = "GGEMCO_SAVE";

        /// <summary>
        /// 저장 데이터 암호화에 사용할 기본 키 별칭입니다.
        /// </summary>
        public const string DefaultKeyAlias = "ggemco_save_key_v1";

        /// <summary>
        /// 저장 직전에 평문 JSON을 현재 설정에 맞는 파일 텍스트로 변환합니다.
        /// </summary>
        /// <param name="filePath">저장 대상 파일 경로입니다.</param>
        /// <param name="plainText">직렬화된 평문 JSON입니다.</param>
        /// <returns>파일에 기록할 텍스트입니다.</returns>
        public static string EncryptForWrite(string filePath, string plainText)
        {
            return EncryptForWrite(filePath, plainText, null);
        }

        /// <summary>
        /// 저장 직전에 평문 JSON을 논리 저장 식별자 기반 AAD로 암호화합니다.
        /// </summary>
        /// <param name="filePath">저장 대상 파일 경로입니다.</param>
        /// <param name="plainText">직렬화된 평문 JSON입니다.</param>
        /// <param name="identity">암호화 AAD를 구성할 논리 저장 식별자입니다.</param>
        /// <returns>파일에 기록할 텍스트입니다.</returns>
        public static string EncryptForWrite(string filePath, string plainText, SaveDataIdentity identity)
        {
            GGemCoSaveSettings saveSettings = GetSaveSettings();
            if (!ShouldEncryptForWrite(saveSettings))
            {
                return plainText;
            }

            ISaveDataCryptoProvider provider = CreateProvider();
            if (provider == null || !provider.IsAvailable)
            {
                if (IsEncryptionRequired(saveSettings))
                {
                    throw new SaveDataCryptoException("현재 플랫폼에서 저장 데이터 암호화를 사용할 수 없습니다.");
                }

                if (!_providerUnavailableWarningLogged)
                {
                    _providerUnavailableWarningLogged = true;
                    GcLogger.LogWarning("[SaveDataCryptoService] 현재 플랫폼에서 암호화 구현체를 찾지 못해 평문으로 저장합니다.");
                }

                return plainText;
            }

            try
            {
                return provider.EncryptToText(plainText, CreateContext(filePath, saveSettings, identity));
            }
            catch (Exception ex)
            {
                throw new SaveDataCryptoException("저장 데이터 암호화에 실패했습니다.", ex);
            }
        }

        /// <summary>
        /// 파일에서 읽은 텍스트를 역직렬화 가능한 평문 JSON으로 변환합니다.
        /// </summary>
        /// <param name="filePath">로드 대상 파일 경로입니다.</param>
        /// <param name="fileText">파일에서 읽은 원본 텍스트입니다.</param>
        /// <returns>역직렬화에 사용할 평문 JSON입니다.</returns>
        public static string DecryptAfterRead(string filePath, string fileText)
        {
            return DecryptAfterRead(filePath, fileText, null);
        }

        /// <summary>
        /// 파일에서 읽은 텍스트를 논리 저장 식별자 기반 AAD로 검증하고 평문 JSON으로 변환합니다.
        /// </summary>
        /// <param name="filePath">로드 대상 파일 경로입니다.</param>
        /// <param name="fileText">파일에서 읽은 원본 텍스트입니다.</param>
        /// <param name="identity">복호화 AAD를 구성할 논리 저장 식별자입니다.</param>
        /// <returns>역직렬화에 사용할 평문 JSON입니다.</returns>
        public static string DecryptAfterRead(string filePath, string fileText, SaveDataIdentity identity)
        {
            if (string.IsNullOrEmpty(fileText))
            {
                return fileText;
            }

            GGemCoSaveSettings saveSettings = GetSaveSettings();
            bool isEncrypted = IsEncryptedText(fileText);
            if (!isEncrypted)
            {
                if (IsEncryptionRequired(saveSettings))
                {
                    throw new SaveDataCryptoException("암호화가 필수인 설정에서는 평문 저장 파일을 불러올 수 없습니다.");
                }

                return fileText;
            }

            ISaveDataCryptoProvider provider = CreateProvider();
            if (provider == null || !provider.IsAvailable)
            {
                throw new SaveDataCryptoException("암호화된 저장 파일을 복호화할 수 있는 플랫폼 구현체가 없습니다.");
            }

            try
            {
                return provider.DecryptToText(fileText, CreateContext(filePath, saveSettings, identity));
            }
            catch (Exception ex)
            {
                if (identity != null && TryDecryptWithLegacyAssociatedData(provider, filePath, fileText, saveSettings, out string legacyPlainText))
                {
                    return legacyPlainText;
                }

                throw new SaveDataCryptoException("저장 데이터 복호화에 실패했습니다.", ex);
            }
        }

        /// <summary>
        /// 지정한 텍스트가 저장 데이터 암호화 Envelope인지 확인합니다.
        /// </summary>
        /// <param name="fileText">검사할 파일 텍스트입니다.</param>
        /// <returns>암호화 Envelope이면 true입니다.</returns>
        public static bool IsEncryptedText(string fileText)
        {
            if (string.IsNullOrEmpty(fileText))
            {
                return false;
            }

            try
            {
                JObject root = JObject.Parse(fileText);
                return string.Equals(root.Value<string>("magic"), EnvelopeMagic, StringComparison.Ordinal);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// 현재 로드된 저장 설정을 가져옵니다.
        /// </summary>
        /// <returns>로드된 저장 설정입니다. 없으면 null입니다.</returns>
        private static GGemCoSaveSettings GetSaveSettings()
        {
            return AddressableLoaderSettings.Instance ? AddressableLoaderSettings.Instance.saveSettings : null;
        }

        /// <summary>
        /// 저장 시 암호화를 적용해야 하는지 확인합니다.
        /// </summary>
        /// <param name="saveSettings">저장 설정입니다.</param>
        /// <returns>암호화를 적용해야 하면 true입니다.</returns>
        private static bool ShouldEncryptForWrite(GGemCoSaveSettings saveSettings)
        {
            return saveSettings != null && saveSettings.SaveDataEncryptionMode != SaveDataEncryptionMode.Disabled;
        }

        /// <summary>
        /// 저장 설정이 암호화 필수 모드인지 확인합니다.
        /// </summary>
        /// <param name="saveSettings">저장 설정입니다.</param>
        /// <returns>암호화 필수 모드이면 true입니다.</returns>
        private static bool IsEncryptionRequired(GGemCoSaveSettings saveSettings)
        {
            return saveSettings != null && saveSettings.SaveDataEncryptionMode == SaveDataEncryptionMode.Required;
        }

        /// <summary>
        /// 저장 파일별 암호화 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="filePath">저장 파일 경로입니다.</param>
        /// <param name="saveSettings">저장 설정입니다.</param>
        /// <param name="identity">암호화 AAD를 구성할 논리 저장 식별자입니다.</param>
        /// <returns>암호화 컨텍스트입니다.</returns>
        private static SaveDataCryptoContext CreateContext(string filePath, GGemCoSaveSettings saveSettings, SaveDataIdentity identity)
        {
            string keyAlias = saveSettings != null ? saveSettings.SaveDataEncryptionKeyAlias : DefaultKeyAlias;
            string associatedData = CreateAssociatedData(filePath, identity);
            return new SaveDataCryptoContext(filePath, keyAlias, associatedData);
        }

        /// <summary>
        /// 논리 저장 식별자를 우선 사용해 추가 인증 데이터를 구성합니다.
        /// </summary>
        /// <param name="filePath">저장 파일 경로입니다.</param>
        /// <param name="identity">암호화 AAD를 구성할 논리 저장 식별자입니다.</param>
        /// <returns>암호문 검증에 사용할 추가 인증 데이터입니다.</returns>
        private static string CreateAssociatedData(string filePath, SaveDataIdentity identity)
        {
            return identity != null ? identity.ToAssociatedData() : CreateLegacyAssociatedData(filePath);
        }

        /// <summary>
        /// 기존 암호화 파일 호환을 위해 파일 이름과 슬롯 폴더 이름으로 AAD를 구성합니다.
        /// </summary>
        /// <param name="filePath">저장 파일 경로입니다.</param>
        /// <returns>기존 파일명 기반 추가 인증 데이터입니다.</returns>
        private static string CreateLegacyAssociatedData(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return "save-data";
            }

            string fileName = Path.GetFileName(filePath);
            string slotName = Path.GetFileName(Path.GetDirectoryName(filePath) ?? string.Empty);
            return string.IsNullOrEmpty(slotName) ? fileName : $"{slotName}/{fileName}";
        }

        /// <summary>
        /// 이전 버전의 파일명 기반 AAD로 암호화된 저장 파일을 복호화합니다.
        /// </summary>
        /// <param name="provider">플랫폼 암호화 구현체입니다.</param>
        /// <param name="filePath">로드 대상 파일 경로입니다.</param>
        /// <param name="fileText">파일에서 읽은 원본 텍스트입니다.</param>
        /// <param name="saveSettings">저장 설정입니다.</param>
        /// <param name="plainText">복호화에 성공한 평문 JSON입니다.</param>
        /// <returns>기존 AAD로 복호화에 성공하면 true입니다.</returns>
        private static bool TryDecryptWithLegacyAssociatedData(
            ISaveDataCryptoProvider provider,
            string filePath,
            string fileText,
            GGemCoSaveSettings saveSettings,
            out string plainText)
        {
            plainText = null;

            try
            {
                string keyAlias = saveSettings != null ? saveSettings.SaveDataEncryptionKeyAlias : DefaultKeyAlias;
                var legacyContext = new SaveDataCryptoContext(filePath, keyAlias, CreateLegacyAssociatedData(filePath));
                plainText = provider.DecryptToText(fileText, legacyContext);

                if (!_legacyAssociatedDataWarningLogged)
                {
                    _legacyAssociatedDataWarningLogged = true;
                    GcLogger.LogWarning("[SaveDataCryptoService] 기존 파일명 기반 AAD 저장 파일을 복호화했습니다. 다음 저장 시 논리 슬롯 기반 AAD로 마이그레이션됩니다.");
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 현재 플랫폼에 맞는 저장 데이터 암호화 구현체를 생성합니다.
        /// </summary>
        /// <returns>플랫폼 암호화 구현체입니다.</returns>
        private static ISaveDataCryptoProvider CreateProvider()
        {
            return new AndroidKeystoreSaveDataCryptoProvider();
        }
    }
}
