using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// Stat(속성) 정의 테이블 Structure.
    /// </summary>
    public sealed class StruckTableStat
    {
        public int Uid;
        public string ID;
        public string Name;

        /// <summary>
        /// 스탯 항목의 사용 분류입니다.
        /// - Base: BASE_* 기본 항목
        /// - Growth: STAT_* 성장 스탯 항목
        /// - Runtime: 런타임 전용 특수 항목
        /// </summary>
        public ConfigCommon.StatGroup Group;

        public float DefaultValue;
    }

    /// <summary>
    /// Stat(속성) 정의 테이블
    /// </summary>
    public sealed class TableStat : DefaultTable<StruckTableStat>
    {
        public override string Key => ConfigAddressableTable.Stat;

        private readonly Dictionary<string, StruckTableStat> _byId = new();
        private readonly Dictionary<ConfigCommon.StatGroup, List<StruckTableStat>> _byGroup = new();

        protected override void PreLoad()
        {
            _byId.Clear();
            _byGroup.Clear();
        }

        protected override void OnLoadedData(StruckTableStat data)
        {
            if (data == null) return;

            if (LocalizationManager.Instance != null && !string.IsNullOrWhiteSpace(data.ID))
                data.Name = LocalizationManager.Instance.GetStatusNameByKey(data.ID);

            if (!string.IsNullOrWhiteSpace(data.ID))
                _byId[data.ID] = data;

            if (!_byGroup.TryGetValue(data.Group, out var groupRows))
            {
                groupRows = new List<StruckTableStat>();
                _byGroup[data.Group] = groupRows;
            }

            groupRows.Add(data);
        }

        /// <summary>
        /// 현재 선택된 Locale 기준으로 Name 필드를 다시 로컬라이즈합니다.
        /// - 테이블은 로드 시점에 Name을 캐시하기 때문에, 런타임에서 Locale이 변경되면 재적용이 필요합니다.
        /// </summary>
        public void RefreshLocalizedNames(LocalizationManager loc = null)
        {
            loc ??= LocalizationManager.Instance;
            if (loc == null) return;

            foreach (var pair in GetDatas())
            {
                var row = pair.Value;
                if (row == null) continue;

                var id = row.ID;
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                var localized = loc.GetStatusNameByKey(id);
                if (!string.IsNullOrWhiteSpace(localized))
                    row.Name = localized;
            }
        }

        public StruckTableStat GetDataById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _byId.GetValueOrDefault(id);
        }

        /// <summary>
        /// 지정한 분류에 해당하는 stat 테이블 행 목록을 반환합니다.
        /// </summary>
        /// <param name="group">조회할 스탯 분류입니다.</param>
        /// <returns>해당 분류의 행 목록입니다. 없으면 빈 목록입니다.</returns>
        public IReadOnlyList<StruckTableStat> GetDataByGroup(ConfigCommon.StatGroup group)
        {
            return _byGroup.TryGetValue(group, out var rows) ? rows : System.Array.Empty<StruckTableStat>();
        }

        /// <summary>
        /// stat 테이블 행의 Group 컬럼을 읽습니다.
        /// - 기존 테이블처럼 Group 컬럼이 없거나 비어 있으면 ID prefix(BASE_*/STAT_*)로 분류를 추론합니다.
        /// </summary>
        /// <param name="reader">현재 행 파서입니다.</param>
        /// <param name="statId">현재 행의 스탯 ID입니다.</param>
        /// <returns>파싱 또는 추론된 스탯 분류입니다.</returns>
        private static ConfigCommon.StatGroup ReadGroup(TableRowReader reader, string statId)
        {
            ConfigCommon.StatGroup fallback = ConfigCommon.ResolveStatGroupById(statId);
            return reader.Enum("Group", fallback);
        }

        protected override StruckTableStat BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            string id = reader.String("ID");

            return new StruckTableStat
            {
                Uid = reader.Int("Uid"),
                ID = id,
                Name = reader.String("Name"),
                Group = ReadGroup(reader, id),
                DefaultValue = reader.Float("DefaultValue")
            };
        }
    }
}
