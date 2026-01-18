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
        public float DefaultValue;
    }

    /// <summary>
    /// Stat(속성) 정의 테이블
    /// </summary>
    public sealed class TableStat : DefaultTable<StruckTableStat>
    {
        public override string Key => ConfigAddressableTable.Stat;

        private readonly Dictionary<string, StruckTableStat> _byId = new();

        protected override void PreLoad()
        {
            _byId.Clear();
        }

        protected override void OnLoadedData(StruckTableStat data)
        {
            if (data == null) return;

            if (LocalizationManager.Instance != null && !string.IsNullOrWhiteSpace(data.ID))
                data.Name = LocalizationManager.Instance.GetStatusNameByKey(data.ID);

            if (!string.IsNullOrWhiteSpace(data.ID))
                _byId[data.ID] = data;
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

        protected override StruckTableStat BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableStat
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                ID = data.GetValueOrDefault("ID"),
                Name = data.GetValueOrDefault("Name"),
                DefaultValue = MathHelper.ParseFloat(data.GetValueOrDefault("DefaultValue"))
            };
        }
    }
}
