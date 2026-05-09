using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GGemCo2DCore
{
    /// <summary>
    /// 런타임 테이블 팩의 바이너리 직렬화/역직렬화를 담당합니다.
    /// </summary>
    /// <remarks>
    /// 테이블 내용 자체는 기존 txt 원문을 유지하고, 여러 테이블을 하나의 Addressables 자산으로 묶기 위한 얇은 컨테이너 포맷입니다.
    /// </remarks>
    public static class RuntimeTablePackCodec
    {
        private const string Magic = "GGEMCO_TABLE_PACK";
        private const int Version = 1;

        /// <summary>
        /// 테이블 팩 엔트리 목록을 런타임 로드용 바이너리 데이터로 변환합니다.
        /// </summary>
        /// <param name="packageId">패키지 식별자입니다. 예: core, skill, affect.</param>
        /// <param name="entries">팩에 포함할 개별 테이블 엔트리 목록입니다.</param>
        /// <returns>Addressables에 등록할 .bytes 파일 내용입니다.</returns>
        public static byte[] Encode(string packageId, IReadOnlyList<RuntimeTablePackEntry> entries)
        {
            var safeEntries = entries ?? Array.Empty<RuntimeTablePackEntry>();

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8);

            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(packageId ?? string.Empty);
            writer.Write(safeEntries.Count);

            for (int i = 0; i < safeEntries.Count; i++)
            {
                RuntimeTablePackEntry entry = safeEntries[i];

                // null 엔트리가 들어오더라도 포맷 자체가 깨지지 않도록 빈 값으로 보정합니다.
                writer.Write(entry?.TableName ?? string.Empty);
                writer.Write(entry?.AddressableKey ?? string.Empty);
                writer.Write(entry?.SourcePath ?? string.Empty);
                writer.Write(entry?.ContentHash ?? string.Empty);
                writer.Write(entry?.Content ?? string.Empty);
            }

            writer.Flush();
            return stream.ToArray();
        }

        /// <summary>
        /// 런타임에 로드한 .bytes 데이터를 테이블 팩 객체로 복원합니다.
        /// </summary>
        /// <param name="bytes">Addressables에서 로드한 바이너리 데이터입니다.</param>
        /// <param name="pack">복원된 테이블 팩입니다.</param>
        /// <param name="error">복원 실패 시 원인 메시지입니다.</param>
        /// <returns>복원에 성공하면 true를 반환합니다.</returns>
        public static bool TryDecode(byte[] bytes, out RuntimeTablePack pack, out string error)
        {
            pack = null;
            error = null;

            if (bytes == null || bytes.Length == 0)
            {
                error = "테이블 팩 데이터가 비어 있습니다.";
                return false;
            }

            try
            {
                using var stream = new MemoryStream(bytes);
                using var reader = new BinaryReader(stream, Encoding.UTF8);

                string magic = reader.ReadString();
                if (!string.Equals(magic, Magic, StringComparison.Ordinal))
                {
                    error = $"지원하지 않는 테이블 팩 형식입니다. magic={magic}";
                    return false;
                }

                int version = reader.ReadInt32();
                if (version != Version)
                {
                    error = $"지원하지 않는 테이블 팩 버전입니다. version={version}, expected={Version}";
                    return false;
                }

                string packageId = reader.ReadString();
                int count = reader.ReadInt32();
                if (count < 0)
                {
                    error = $"테이블 팩 엔트리 수가 잘못되었습니다. count={count}";
                    return false;
                }

                var entries = new List<RuntimeTablePackEntry>(count);
                for (int i = 0; i < count; i++)
                {
                    string tableName = reader.ReadString();
                    string addressableKey = reader.ReadString();
                    string sourcePath = reader.ReadString();
                    string contentHash = reader.ReadString();
                    string content = reader.ReadString();

                    entries.Add(new RuntimeTablePackEntry(tableName, addressableKey, sourcePath, contentHash, content));
                }

                pack = new RuntimeTablePack(packageId, entries);
                return true;
            }
            catch (Exception e)
            {
                error = $"테이블 팩을 읽는 중 예외가 발생했습니다. {e.Message}";
                return false;
            }
        }

        /// <summary>
        /// 테이블 원문 내용의 변경 여부를 추적하기 위한 간단한 해시를 계산합니다.
        /// </summary>
        /// <param name="content">해시를 계산할 테이블 원문입니다.</param>
        /// <returns>16자리 16진수 해시 문자열입니다.</returns>
        public static string CalculateContentHash(string content)
        {
            unchecked
            {
                const ulong offsetBasis = 14695981039346656037UL;
                const ulong prime = 1099511628211UL;

                ulong hash = offsetBasis;
                byte[] bytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
                for (int i = 0; i < bytes.Length; i++)
                {
                    hash ^= bytes[i];
                    hash *= prime;
                }

                return hash.ToString("x16");
            }
        }
    }
}
