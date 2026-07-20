using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 에디터 자동 분석으로 생성된 사운드 사용처 한 건을 나타내는 테이블 행입니다.
    /// </summary>
    public sealed class StruckTableSoundUsageManifest : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public SoundUsageManifestScopeType ScopeType;
        public int ScopeUid;
        public int SoundUid;
        public SoundUsageManifestSourceType SourceType;
        public int SourceUid;
        public string SourcePath;
        public string Memo;
        public bool Enabled;
    }

    /// <summary>
    /// sound_usage_manifest 테이블을 로드하고 전역, 맵 및 UI 윈도우 범위별 사운드 UID를 제공합니다.
    /// </summary>
    public sealed class TableSoundUsageManifest : DefaultTable<StruckTableSoundUsageManifest>
    {
        private readonly Dictionary<ScopeKey, List<StruckTableSoundUsageManifest>> _rowsByScope =
            new Dictionary<ScopeKey, List<StruckTableSoundUsageManifest>>();

        public override string Key => ConfigAddressableTable.SoundUsageManifest;

        /// <summary>
        /// 테이블을 다시 로드하기 전에 범위별 보조 인덱스를 초기화합니다.
        /// </summary>
        protected override void PreLoad()
        {
            _rowsByScope.Clear();
        }

        /// <summary>
        /// sound_usage_manifest 테이블 한 행을 런타임 데이터로 변환합니다.
        /// </summary>
        /// <param name="data">헤더명 기준으로 파싱된 원본 문자열 데이터입니다.</param>
        /// <returns>파싱된 사운드 사용 매니페스트 행입니다.</returns>
        protected override StruckTableSoundUsageManifest BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            string scopeTypeValue = reader.String("ScopeType", string.Empty);
            string sourceTypeValue = reader.String("SourceType", string.Empty);

            return new StruckTableSoundUsageManifest
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name", string.Empty),
                ScopeType = string.IsNullOrWhiteSpace(scopeTypeValue)
                    ? SoundUsageManifestScopeType.None
                    : EnumHelper.ConvertEnum<SoundUsageManifestScopeType>(scopeTypeValue),
                ScopeUid = reader.Int("ScopeUid", 0),
                SoundUid = reader.Int("SoundUid", 0),
                SourceType = string.IsNullOrWhiteSpace(sourceTypeValue)
                    ? SoundUsageManifestSourceType.Unknown
                    : EnumHelper.ConvertEnum<SoundUsageManifestSourceType>(sourceTypeValue),
                SourceUid = reader.Int("SourceUid", 0),
                SourcePath = reader.String("SourcePath", string.Empty),
                Memo = reader.String("Memo", string.Empty),
                Enabled = reader.BoolLoose("Enabled", true),
            };
        }

        /// <summary>
        /// 로드된 행을 범위 종류와 범위 UID 조합으로 인덱싱합니다.
        /// </summary>
        /// <param name="row">등록할 매니페스트 행입니다.</param>
        protected override void OnLoadedData(StruckTableSoundUsageManifest row)
        {
            if (row == null || row.ScopeType == SoundUsageManifestScopeType.None || row.ScopeUid <= 0 || row.SoundUid <= 0)
                return;

            ScopeKey key = new ScopeKey(row.ScopeType, row.ScopeUid);
            if (!_rowsByScope.TryGetValue(key, out List<StruckTableSoundUsageManifest> rows))
            {
                rows = new List<StruckTableSoundUsageManifest>();
                _rowsByScope[key] = rows;
            }

            rows.Add(row);
            rows.Sort((left, right) => left.Uid.CompareTo(right.Uid));
        }

        /// <summary>
        /// 지정한 범위에 연결된 매니페스트 행을 반환합니다.
        /// </summary>
        /// <param name="scopeType">전역, 맵 또는 UI 윈도우 범위 종류입니다.</param>
        /// <param name="scopeUid">예약된 전역 UID, 맵 UID 또는 UI 윈도우 UID입니다.</param>
        /// <param name="enabledOnly">활성화된 행만 반환할지 여부입니다.</param>
        /// <returns>UID 순서로 정렬된 매니페스트 행 목록입니다.</returns>
        public IReadOnlyList<StruckTableSoundUsageManifest> GetRows(
            SoundUsageManifestScopeType scopeType,
            int scopeUid,
            bool enabledOnly = true)
        {
            if (scopeType == SoundUsageManifestScopeType.None || scopeUid <= 0)
                return Array.Empty<StruckTableSoundUsageManifest>();

            ScopeKey key = new ScopeKey(scopeType, scopeUid);
            if (!_rowsByScope.TryGetValue(key, out List<StruckTableSoundUsageManifest> rows))
                return Array.Empty<StruckTableSoundUsageManifest>();

            if (!enabledOnly)
                return rows;

            List<StruckTableSoundUsageManifest> result = new List<StruckTableSoundUsageManifest>(rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                StruckTableSoundUsageManifest row = rows[i];
                if (row is { Enabled: true })
                    result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// 지정한 범위에서 사용하는 대표 sound UID를 중복 없이 반환합니다.
        /// </summary>
        /// <param name="scopeType">전역, 맵 또는 UI 윈도우 범위 종류입니다.</param>
        /// <param name="scopeUid">예약된 전역 UID, 맵 UID 또는 UI 윈도우 UID입니다.</param>
        /// <returns>행 UID 순서를 유지한 대표 sound UID 목록입니다.</returns>
        public IReadOnlyList<int> GetSoundUids(SoundUsageManifestScopeType scopeType, int scopeUid)
        {
            IReadOnlyList<StruckTableSoundUsageManifest> rows = GetRows(scopeType, scopeUid);
            if (rows.Count == 0)
                return Array.Empty<int>();

            List<int> result = new List<int>(rows.Count);
            HashSet<int> registered = new HashSet<int>();
            for (int i = 0; i < rows.Count; i++)
            {
                int soundUid = rows[i]?.SoundUid ?? 0;
                if (soundUid > 0 && registered.Add(soundUid))
                    result.Add(soundUid);
            }

            return result;
        }

        /// <summary>
        /// 범위 종류와 범위 UID를 사전 키로 사용하기 위한 값 형식입니다.
        /// </summary>
        private readonly struct ScopeKey : IEquatable<ScopeKey>
        {
            private readonly SoundUsageManifestScopeType _scopeType;
            private readonly int _scopeUid;

            /// <summary>
            /// 범위 키를 생성합니다.
            /// </summary>
            /// <param name="scopeType">범위 종류입니다.</param>
            /// <param name="scopeUid">범위 UID입니다.</param>
            public ScopeKey(SoundUsageManifestScopeType scopeType, int scopeUid)
            {
                _scopeType = scopeType;
                _scopeUid = scopeUid;
            }

            /// <summary>
            /// 다른 범위 키와 값이 같은지 확인합니다.
            /// </summary>
            /// <param name="other">비교할 범위 키입니다.</param>
            /// <returns>범위 종류와 UID가 모두 같으면 true입니다.</returns>
            public bool Equals(ScopeKey other)
            {
                return _scopeType == other._scopeType && _scopeUid == other._scopeUid;
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                return obj is ScopeKey other && Equals(other);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)_scopeType * 397) ^ _scopeUid;
                }
            }
        }
    }
}
