using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 런타임에서 한 번에 로드할 테이블 묶음 정보를 표현합니다.
    /// </summary>
    /// <remarks>
    /// 개별 txt 테이블의 파싱 방식은 유지하고, Addressables 요청 단위만 패키지별 묶음으로 줄이기 위한 DTO입니다.
    /// </remarks>
    public sealed class RuntimeTablePack
    {
        private readonly List<RuntimeTablePackEntry> _entries;

        /// <summary>
        /// 테이블 팩이 속한 패키지 식별자입니다. 예: core, skill, affect.
        /// </summary>
        public string PackageId { get; }

        /// <summary>
        /// 팩에 포함된 개별 테이블 원문 목록입니다.
        /// </summary>
        public IReadOnlyList<RuntimeTablePackEntry> Entries => _entries;

        /// <summary>
        /// 런타임 테이블 팩 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="packageId">패키지 식별자입니다.</param>
        /// <param name="entries">팩에 포함할 테이블 엔트리 목록입니다.</param>
        public RuntimeTablePack(string packageId, IReadOnlyList<RuntimeTablePackEntry> entries)
        {
            PackageId = packageId ?? string.Empty;
            _entries = entries != null ? new List<RuntimeTablePackEntry>(entries) : new List<RuntimeTablePackEntry>();
        }
    }

    /// <summary>
    /// 런타임 테이블 팩 내부의 단일 테이블 원문 항목입니다.
    /// </summary>
    public sealed class RuntimeTablePackEntry
    {
        /// <summary>
        /// 기존 테이블 파서가 사용하는 논리 테이블 이름입니다. 예: map, monster, skill.
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// 원본 개별 txt 테이블의 Addressables 키입니다.
        /// </summary>
        public string AddressableKey { get; }

        /// <summary>
        /// 원본 개별 txt 테이블의 프로젝트 상대 경로입니다.
        /// </summary>
        public string SourcePath { get; }

        /// <summary>
        /// 원문 내용 검증과 추적을 위한 해시 문자열입니다.
        /// </summary>
        public string ContentHash { get; }

        /// <summary>
        /// 기존 <see cref="ITableParser.LoadData"/>에 그대로 전달할 테이블 원문입니다.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// 단일 테이블 팩 엔트리를 생성합니다.
        /// </summary>
        /// <param name="tableName">논리 테이블 이름입니다.</param>
        /// <param name="addressableKey">원본 개별 테이블의 Addressables 키입니다.</param>
        /// <param name="sourcePath">원본 개별 테이블 경로입니다.</param>
        /// <param name="contentHash">원문 내용 해시입니다.</param>
        /// <param name="content">테이블 원문입니다.</param>
        public RuntimeTablePackEntry(
            string tableName,
            string addressableKey,
            string sourcePath,
            string contentHash,
            string content)
        {
            TableName = tableName ?? string.Empty;
            AddressableKey = addressableKey ?? string.Empty;
            SourcePath = sourcePath ?? string.Empty;
            ContentHash = contentHash ?? string.Empty;
            Content = content ?? string.Empty;
        }

        /// <summary>
        /// 기존 Addressables 테이블 정의와 원문을 이용해 팩 엔트리를 생성합니다.
        /// </summary>
        /// <param name="info">개별 테이블 Addressables 정의입니다.</param>
        /// <param name="content">테이블 원문입니다.</param>
        /// <returns>런타임 테이블 팩 엔트리입니다.</returns>
        public static RuntimeTablePackEntry FromAddressableInfo(AddressableAssetInfo info, string content)
        {
            string tableName = info?.Etc1 ?? string.Empty;
            string addressableKey = info?.Key ?? string.Empty;
            string sourcePath = info?.Path ?? string.Empty;
            string contentHash = RuntimeTablePackCodec.CalculateContentHash(content);

            return new RuntimeTablePackEntry(tableName, addressableKey, sourcePath, contentHash, content);
        }
    }
}
