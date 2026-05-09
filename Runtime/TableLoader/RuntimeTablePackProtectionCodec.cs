using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace GGemCo2DCore
{
    /// <summary>
    /// 런타임 테이블 팩 원본 바이너리에 압축/암호화 envelope를 적용합니다.
    /// </summary>
    /// <remarks>
    /// SaveData 암호화처럼 magic/version/AAD를 사용하지만, 테이블 팩은 빌드 산출물이므로 플랫폼 Keystore가 아닌 패키지 기반 키 파생을 사용합니다.
    /// </remarks>
    public static class RuntimeTablePackProtectionCodec
    {
        private const string Magic = "GGEMCO_TABLE_PACK_PROTECTED";
        private const int Version = 1;

        /// <summary>
        /// 테이블 팩 원본 바이너리에 압축과 암호화를 적용해 보호 envelope로 변환합니다.
        /// </summary>
        /// <param name="packageId">테이블 팩이 속한 패키지 식별자입니다.</param>
        /// <param name="rawBytes">보호 계층을 적용할 원본 테이블 팩 바이너리입니다.</param>
        /// <param name="options">압축/암호화 적용 옵션입니다.</param>
        /// <returns>Addressables에 저장할 보호 envelope 바이너리입니다.</returns>
        public static byte[] Protect(string packageId, byte[] rawBytes, RuntimeTablePackProtectionOptions options)
        {
            RuntimeTablePackProtectionOptions safeOptions = options ?? RuntimeTablePackProtectionOptions.Default;
            byte[] safeRawBytes = rawBytes ?? Array.Empty<byte>();
            byte[] payload = ApplyCompression(safeRawBytes, safeOptions.CompressionMode);
            var envelope = new ProtectedEnvelope
            {
                PackageId = packageId ?? string.Empty,
                KeyAlias = safeOptions.KeyAlias,
                CompressionMode = safeOptions.CompressionMode,
                EncryptionMode = safeOptions.EncryptionMode,
                RawLength = safeRawBytes.Length,
                Salt = Array.Empty<byte>(),
                Iv = Array.Empty<byte>(),
                Payload = payload,
                Mac = Array.Empty<byte>(),
            };

            if (safeOptions.EncryptionMode == RuntimeTablePackEncryptionMode.AesCbcHmacSha256)
            {
                var context = new RuntimeTablePackSecurityContext(envelope.PackageId, envelope.KeyAlias);
                RuntimeTablePackCryptoService.CreateSaltAndIv(out byte[] salt, out byte[] iv);
                RuntimeTablePackCryptoKeys keys = RuntimeTablePackCryptoService.DeriveKeys(context, salt);

                envelope.Salt = salt;
                envelope.Iv = iv;
                envelope.Payload = RuntimeTablePackCryptoService.EncryptAesCbc(payload, keys.EncryptionKey, iv);
                envelope.Mac = RuntimeTablePackCryptoService.ComputeMac(SerializeEnvelopeCore(envelope), keys.MacKey, context);
            }

            return SerializeEnvelope(envelope);
        }

        /// <summary>
        /// 바이너리가 보호 envelope 형식인지 확인합니다.
        /// </summary>
        /// <param name="bytes">검사할 바이너리 데이터입니다.</param>
        /// <returns>보호 envelope magic을 가진 데이터면 true를 반환합니다.</returns>
        public static bool IsProtected(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return false;

            try
            {
                using var stream = new MemoryStream(bytes);
                using var reader = new BinaryReader(stream, Encoding.UTF8);
                string magic = reader.ReadString();
                return string.Equals(magic, Magic, StringComparison.Ordinal);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 보호 envelope를 해제해 원본 테이블 팩 바이너리를 복원합니다.
        /// </summary>
        /// <param name="bytes">Addressables에서 로드한 보호 envelope 바이너리입니다.</param>
        /// <param name="rawBytes">복원된 원본 테이블 팩 바이너리입니다.</param>
        /// <param name="packageId">envelope에 기록된 패키지 식별자입니다.</param>
        /// <param name="error">복원 실패 시 원인 메시지입니다.</param>
        /// <returns>복원에 성공하면 true를 반환합니다.</returns>
        public static bool TryUnprotect(byte[] bytes, out byte[] rawBytes, out string packageId, out string error)
        {
            rawBytes = null;
            packageId = null;
            error = null;

            if (!TryReadEnvelope(bytes, out ProtectedEnvelope envelope, out error))
                return false;

            packageId = envelope.PackageId;

            try
            {
                byte[] payload = envelope.Payload ?? Array.Empty<byte>();
                if (envelope.EncryptionMode == RuntimeTablePackEncryptionMode.AesCbcHmacSha256)
                {
                    var context = new RuntimeTablePackSecurityContext(envelope.PackageId, envelope.KeyAlias);
                    RuntimeTablePackCryptoKeys keys = RuntimeTablePackCryptoService.DeriveKeys(context, envelope.Salt);
                    byte[] expectedMac = RuntimeTablePackCryptoService.ComputeMac(SerializeEnvelopeCore(envelope), keys.MacKey, context);
                    if (!RuntimeTablePackCryptoService.FixedTimeEquals(expectedMac, envelope.Mac))
                    {
                        error = "테이블 팩 암호문 무결성 검증에 실패했습니다.";
                        return false;
                    }

                    payload = RuntimeTablePackCryptoService.DecryptAesCbc(payload, keys.EncryptionKey, envelope.Iv);
                }
                else if (envelope.EncryptionMode != RuntimeTablePackEncryptionMode.None)
                {
                    error = $"지원하지 않는 테이블 팩 암호화 방식입니다. mode={envelope.EncryptionMode}";
                    return false;
                }

                rawBytes = RemoveCompression(payload, envelope.CompressionMode, envelope.RawLength);
                return true;
            }
            catch (Exception e)
            {
                error = $"테이블 팩 보호 계층을 해제하는 중 예외가 발생했습니다. {e.Message}";
                return false;
            }
        }

        /// <summary>
        /// 옵션에 맞게 테이블 팩 원본 바이너리를 압축합니다.
        /// </summary>
        /// <param name="bytes">압축할 원본 바이너리입니다.</param>
        /// <param name="mode">압축 방식입니다.</param>
        /// <returns>압축된 바이너리입니다.</returns>
        private static byte[] ApplyCompression(byte[] bytes, RuntimeTablePackCompressionMode mode)
        {
            if (mode == RuntimeTablePackCompressionMode.None)
                return bytes ?? Array.Empty<byte>();

            if (mode != RuntimeTablePackCompressionMode.GZip)
                throw new InvalidOperationException($"지원하지 않는 테이블 팩 압축 방식입니다. mode={mode}");

            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            {
                byte[] safeBytes = bytes ?? Array.Empty<byte>();
                gzip.Write(safeBytes, 0, safeBytes.Length);
            }

            return output.ToArray();
        }

        /// <summary>
        /// 옵션에 맞게 테이블 팩 payload의 압축을 해제합니다.
        /// </summary>
        /// <param name="bytes">압축 해제할 payload 바이너리입니다.</param>
        /// <param name="mode">압축 방식입니다.</param>
        /// <param name="expectedLength">압축 전 원본 길이입니다.</param>
        /// <returns>압축 해제된 원본 바이너리입니다.</returns>
        private static byte[] RemoveCompression(byte[] bytes, RuntimeTablePackCompressionMode mode, int expectedLength)
        {
            if (mode == RuntimeTablePackCompressionMode.None)
                return bytes ?? Array.Empty<byte>();

            if (mode != RuntimeTablePackCompressionMode.GZip)
                throw new InvalidOperationException($"지원하지 않는 테이블 팩 압축 방식입니다. mode={mode}");

            using var input = new MemoryStream(bytes ?? Array.Empty<byte>());
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);

            byte[] result = output.ToArray();
            if (expectedLength >= 0 && result.Length != expectedLength)
                throw new InvalidDataException($"압축 해제된 테이블 팩 길이가 다릅니다. expected={expectedLength}, actual={result.Length}");

            return result;
        }

        /// <summary>
        /// 보호 envelope를 HMAC을 제외한 검증 대상 바이너리로 직렬화합니다.
        /// </summary>
        /// <param name="envelope">직렬화할 envelope입니다.</param>
        /// <returns>HMAC 검증에 사용할 바이너리입니다.</returns>
        private static byte[] SerializeEnvelopeCore(ProtectedEnvelope envelope)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8);

            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(envelope.PackageId ?? string.Empty);
            writer.Write(envelope.KeyAlias ?? RuntimeTablePackProtectionOptions.DefaultKeyAlias);
            writer.Write((int)envelope.CompressionMode);
            writer.Write((int)envelope.EncryptionMode);
            writer.Write(envelope.RawLength);
            WriteBytes(writer, envelope.Salt);
            WriteBytes(writer, envelope.Iv);
            WriteBytes(writer, envelope.Payload);
            writer.Flush();
            return stream.ToArray();
        }

        /// <summary>
        /// 보호 envelope 전체를 최종 저장용 바이너리로 직렬화합니다.
        /// </summary>
        /// <param name="envelope">직렬화할 envelope입니다.</param>
        /// <returns>저장 가능한 보호 envelope 바이너리입니다.</returns>
        private static byte[] SerializeEnvelope(ProtectedEnvelope envelope)
        {
            using var stream = new MemoryStream();
            byte[] core = SerializeEnvelopeCore(envelope);
            stream.Write(core, 0, core.Length);

            using var writer = new BinaryWriter(stream, Encoding.UTF8);
            WriteBytes(writer, envelope.Mac);
            writer.Flush();
            return stream.ToArray();
        }

        /// <summary>
        /// 저장된 보호 envelope 바이너리를 구조체로 복원합니다.
        /// </summary>
        /// <param name="bytes">복원할 envelope 바이너리입니다.</param>
        /// <param name="envelope">복원된 envelope입니다.</param>
        /// <param name="error">복원 실패 시 원인 메시지입니다.</param>
        /// <returns>복원에 성공하면 true를 반환합니다.</returns>
        private static bool TryReadEnvelope(byte[] bytes, out ProtectedEnvelope envelope, out string error)
        {
            envelope = null;
            error = null;

            if (bytes == null || bytes.Length == 0)
            {
                error = "테이블 팩 보호 envelope가 비어 있습니다.";
                return false;
            }

            try
            {
                using var stream = new MemoryStream(bytes);
                using var reader = new BinaryReader(stream, Encoding.UTF8);

                string magic = reader.ReadString();
                if (!string.Equals(magic, Magic, StringComparison.Ordinal))
                {
                    error = $"지원하지 않는 테이블 팩 보호 형식입니다. magic={magic}";
                    return false;
                }

                int version = reader.ReadInt32();
                if (version != Version)
                {
                    error = $"지원하지 않는 테이블 팩 보호 버전입니다. version={version}, expected={Version}";
                    return false;
                }

                envelope = new ProtectedEnvelope
                {
                    PackageId = reader.ReadString(),
                    KeyAlias = reader.ReadString(),
                    CompressionMode = (RuntimeTablePackCompressionMode)reader.ReadInt32(),
                    EncryptionMode = (RuntimeTablePackEncryptionMode)reader.ReadInt32(),
                    RawLength = reader.ReadInt32(),
                    Salt = ReadBytes(reader),
                    Iv = ReadBytes(reader),
                    Payload = ReadBytes(reader),
                    Mac = ReadBytes(reader),
                };

                return true;
            }
            catch (Exception e)
            {
                error = $"테이블 팩 보호 envelope를 읽는 중 예외가 발생했습니다. {e.Message}";
                return false;
            }
        }

        /// <summary>
        /// 길이 접두사가 붙은 byte 배열을 기록합니다.
        /// </summary>
        /// <param name="writer">기록 대상 writer입니다.</param>
        /// <param name="bytes">기록할 byte 배열입니다.</param>
        private static void WriteBytes(BinaryWriter writer, byte[] bytes)
        {
            byte[] safeBytes = bytes ?? Array.Empty<byte>();
            writer.Write(safeBytes.Length);
            writer.Write(safeBytes);
        }

        /// <summary>
        /// 길이 접두사가 붙은 byte 배열을 읽습니다.
        /// </summary>
        /// <param name="reader">읽기 대상 reader입니다.</param>
        /// <returns>복원된 byte 배열입니다.</returns>
        private static byte[] ReadBytes(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length < 0)
                throw new InvalidDataException($"byte 배열 길이가 잘못되었습니다. length={length}");

            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length != length)
                throw new EndOfStreamException($"byte 배열을 끝까지 읽지 못했습니다. expected={length}, actual={bytes.Length}");

            return bytes;
        }

        /// <summary>
        /// 보호 envelope의 직렬화 필드입니다.
        /// </summary>
        private sealed class ProtectedEnvelope
        {
            public string PackageId;
            public string KeyAlias;
            public RuntimeTablePackCompressionMode CompressionMode;
            public RuntimeTablePackEncryptionMode EncryptionMode;
            public int RawLength;
            public byte[] Salt;
            public byte[] Iv;
            public byte[] Payload;
            public byte[] Mac;
        }
    }

    /// <summary>
    /// 테이블 팩 보호 계층에서 사용하는 AES/HMAC 유틸리티입니다.
    /// </summary>
    internal static class RuntimeTablePackCryptoService
    {
        private const int SaltLength = 16;
        private const int IvLength = 16;
        private const int KeyLength = 32;
        private const int MacKeyLength = 32;
        private const int Pbkdf2Iterations = 12000;

        /// <summary>
        /// AES-CBC에 사용할 salt와 IV를 생성합니다.
        /// </summary>
        /// <param name="salt">키 파생용 salt입니다.</param>
        /// <param name="iv">AES-CBC 초기화 벡터입니다.</param>
        public static void CreateSaltAndIv(out byte[] salt, out byte[] iv)
        {
            salt = new byte[SaltLength];
            iv = new byte[IvLength];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            rng.GetBytes(iv);
        }

        /// <summary>
        /// 테이블 팩 보안 문맥과 salt에서 AES 키와 HMAC 키를 파생합니다.
        /// </summary>
        /// <param name="context">테이블 팩 보안 문맥입니다.</param>
        /// <param name="salt">키 파생용 salt입니다.</param>
        /// <returns>AES 키와 HMAC 키 묶음입니다.</returns>
        public static RuntimeTablePackCryptoKeys DeriveKeys(RuntimeTablePackSecurityContext context, byte[] salt)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            byte[] safeSalt = salt == null || salt.Length == 0 ? Encoding.UTF8.GetBytes(context.PackageId) : salt;
            using var deriveBytes = new Rfc2898DeriveBytes(CreatePassphrase(context), safeSalt, Pbkdf2Iterations, HashAlgorithmName.SHA256);
            byte[] keyMaterial = deriveBytes.GetBytes(KeyLength + MacKeyLength);

            byte[] encryptionKey = new byte[KeyLength];
            byte[] macKey = new byte[MacKeyLength];
            Buffer.BlockCopy(keyMaterial, 0, encryptionKey, 0, KeyLength);
            Buffer.BlockCopy(keyMaterial, KeyLength, macKey, 0, MacKeyLength);

            return new RuntimeTablePackCryptoKeys(encryptionKey, macKey);
        }

        /// <summary>
        /// AES-CBC/PKCS7 방식으로 payload를 암호화합니다.
        /// </summary>
        /// <param name="plainBytes">암호화할 평문 payload입니다.</param>
        /// <param name="key">AES 키입니다.</param>
        /// <param name="iv">AES-CBC 초기화 벡터입니다.</param>
        /// <returns>암호화된 payload입니다.</returns>
        public static byte[] EncryptAesCbc(byte[] plainBytes, byte[] key, byte[] iv)
        {
            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(plainBytes ?? Array.Empty<byte>(), 0, plainBytes?.Length ?? 0);
        }

        /// <summary>
        /// AES-CBC/PKCS7 방식으로 payload를 복호화합니다.
        /// </summary>
        /// <param name="encryptedBytes">복호화할 암호문 payload입니다.</param>
        /// <param name="key">AES 키입니다.</param>
        /// <param name="iv">AES-CBC 초기화 벡터입니다.</param>
        /// <returns>복호화된 payload입니다.</returns>
        public static byte[] DecryptAesCbc(byte[] encryptedBytes, byte[] key, byte[] iv)
        {
            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encryptedBytes ?? Array.Empty<byte>(), 0, encryptedBytes?.Length ?? 0);
        }

        /// <summary>
        /// envelope 핵심 필드와 AAD를 HMAC-SHA256으로 검증 가능한 값으로 변환합니다.
        /// </summary>
        /// <param name="coreBytes">HMAC 대상 envelope 핵심 필드입니다.</param>
        /// <param name="macKey">HMAC 키입니다.</param>
        /// <param name="context">테이블 팩 보안 문맥입니다.</param>
        /// <returns>HMAC-SHA256 결과입니다.</returns>
        public static byte[] ComputeMac(byte[] coreBytes, byte[] macKey, RuntimeTablePackSecurityContext context)
        {
            using var hmac = new HMACSHA256(macKey);
            byte[] aadBytes = Encoding.UTF8.GetBytes(context?.AssociatedData ?? string.Empty);
            byte[] safeCoreBytes = coreBytes ?? Array.Empty<byte>();
            byte[] input = new byte[aadBytes.Length + safeCoreBytes.Length];
            Buffer.BlockCopy(aadBytes, 0, input, 0, aadBytes.Length);
            Buffer.BlockCopy(safeCoreBytes, 0, input, aadBytes.Length, safeCoreBytes.Length);
            return hmac.ComputeHash(input);
        }

        /// <summary>
        /// HMAC 비교 시 타이밍 차이를 줄이기 위해 고정 시간에 가깝게 byte 배열을 비교합니다.
        /// </summary>
        /// <param name="left">비교할 첫 번째 byte 배열입니다.</param>
        /// <param name="right">비교할 두 번째 byte 배열입니다.</param>
        /// <returns>두 배열이 같으면 true를 반환합니다.</returns>
        public static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;

            int diff = 0;
            for (int i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }

        /// <summary>
        /// 패키지별 키 파생에 사용할 내부 passphrase를 구성합니다.
        /// </summary>
        /// <param name="context">테이블 팩 보안 문맥입니다.</param>
        /// <returns>PBKDF2 입력으로 사용할 passphrase입니다.</returns>
        private static string CreatePassphrase(RuntimeTablePackSecurityContext context)
        {
            return string.Concat(
                "GGem",
                "Co2D",
                ":",
                ConfigDefine.NameSDK,
                ":table-pack:",
                context.PackageId,
                ":",
                context.KeyAlias,
                ":v1");
        }
    }

    /// <summary>
    /// 테이블 팩 암복호화에 사용하는 파생 키 묶음입니다.
    /// </summary>
    internal sealed class RuntimeTablePackCryptoKeys
    {
        /// <summary>
        /// AES 암복호화 키입니다.
        /// </summary>
        public byte[] EncryptionKey { get; }

        /// <summary>
        /// HMAC 검증 키입니다.
        /// </summary>
        public byte[] MacKey { get; }

        /// <summary>
        /// 파생 키 묶음을 생성합니다.
        /// </summary>
        /// <param name="encryptionKey">AES 암복호화 키입니다.</param>
        /// <param name="macKey">HMAC 검증 키입니다.</param>
        public RuntimeTablePackCryptoKeys(byte[] encryptionKey, byte[] macKey)
        {
            EncryptionKey = encryptionKey ?? Array.Empty<byte>();
            MacKey = macKey ?? Array.Empty<byte>();
        }
    }
}
