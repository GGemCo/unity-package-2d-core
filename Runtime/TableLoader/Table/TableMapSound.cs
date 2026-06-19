using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵별 BGM, 환경음 및 선로드 사운드 설정을 나타내는 테이블 행입니다.
    /// </summary>
    public sealed class StruckTableMapSound : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public int MapUid;
        public int SoundUid;
        public MapSoundRole Role;
        public string LayerKey;
        public bool AutoPlay;
        public float FadeDurationOverride;
        internal bool UseFadeDurationOverride;
        public bool Enabled;
        public string Memo;
    }

    /// <summary>
    /// map_sound 테이블을 로드하고 맵 UID별 사운드 행을 제공합니다.
    /// </summary>
    public sealed class TableMapSound : DefaultTable<StruckTableMapSound>
    {
        private readonly Dictionary<int, List<StruckTableMapSound>> _rowsByMapUid =
            new Dictionary<int, List<StruckTableMapSound>>();

        public override string Key => ConfigAddressableTable.MapSound;

        /// <summary>
        /// 테이블을 다시 로드하기 전에 맵별 보조 인덱스를 초기화합니다.
        /// </summary>
        protected override void PreLoad()
        {
            _rowsByMapUid.Clear();
        }

        /// <summary>
        /// map_sound 테이블 한 행을 런타임 데이터로 변환합니다.
        /// 신규 컬럼이 비어 있는 기존 데이터는 자동 재생 및 활성 상태를 기본값으로 사용합니다.
        /// </summary>
        /// <param name="data">헤더명 기준으로 파싱된 원본 문자열 데이터입니다.</param>
        /// <returns>파싱된 맵 사운드 행입니다.</returns>
        protected override StruckTableMapSound BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            string roleValue = reader.String("Role", string.Empty);
            string fadeDurationValue = reader.String("FadeDurationOverride", string.Empty);
            float fadeDurationOverride = Math.Max(0f, MathHelper.ParseFloat(fadeDurationValue));
            bool useFadeDurationOverride = fadeDurationOverride > 0f;

            return new StruckTableMapSound
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name", string.Empty),
                MapUid = reader.Int("MapUid", 0),
                SoundUid = reader.Int("SoundUid", 0),
                Role = string.IsNullOrWhiteSpace(roleValue)
                    ? MapSoundRole.None
                    : EnumHelper.ConvertEnum<MapSoundRole>(roleValue),
                LayerKey = reader.String("LayerKey", string.Empty),
                AutoPlay = reader.BoolLoose("AutoPlay", true),
                FadeDurationOverride = fadeDurationOverride,
                UseFadeDurationOverride = useFadeDurationOverride,
                Enabled = reader.BoolLoose("Enabled", true),
                Memo = reader.String("Memo", string.Empty),
            };
        }

        /// <summary>
        /// 로드된 행을 맵 UID별 보조 인덱스에 등록합니다.
        /// </summary>
        /// <param name="row">등록할 맵 사운드 행입니다.</param>
        protected override void OnLoadedData(StruckTableMapSound row)
        {
            if (row == null || row.MapUid <= 0)
                return;

            if (!_rowsByMapUid.TryGetValue(row.MapUid, out List<StruckTableMapSound> rows))
            {
                rows = new List<StruckTableMapSound>();
                _rowsByMapUid[row.MapUid] = rows;
            }

            rows.Add(row);
            rows.Sort((left, right) => left.Uid.CompareTo(right.Uid));
        }

        /// <summary>
        /// 지정한 맵에 연결된 사운드 행을 반환합니다.
        /// </summary>
        /// <param name="mapUid">조회할 맵 UID입니다.</param>
        /// <param name="enabledOnly">활성화된 행만 반환할지 여부입니다.</param>
        /// <returns>UID 순서로 정렬된 맵 사운드 행 목록입니다.</returns>
        public IReadOnlyList<StruckTableMapSound> GetRowsByMapUid(int mapUid, bool enabledOnly = true)
        {
            if (mapUid <= 0 || !_rowsByMapUid.TryGetValue(mapUid, out List<StruckTableMapSound> rows))
                return Array.Empty<StruckTableMapSound>();

            if (!enabledOnly)
                return rows;

            List<StruckTableMapSound> result = new List<StruckTableMapSound>(rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                StruckTableMapSound row = rows[i];
                if (row is { Enabled: true })
                    result.Add(row);
            }

            return result;
        }
    }
}
