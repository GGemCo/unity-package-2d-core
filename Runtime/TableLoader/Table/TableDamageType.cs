using System.Collections.Generic;

namespace GGemCo2DCore
{
    public sealed class StruckTableDamageType
    {
        public int Uid;
        public string ID;
        public string Name;
    }

    public sealed class TableDamageType : DefaultTable<StruckTableDamageType>
    {
        public override string Key => ConfigAddressableTable.DamageType;

        private readonly Dictionary<string, StruckTableDamageType> _byId = new();

        protected override void PreLoad() => _byId.Clear();

        protected override void OnLoadedData(StruckTableDamageType data)
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

        public StruckTableDamageType GetDataById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _byId.GetValueOrDefault(id);
        }

        protected override StruckTableDamageType BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableDamageType
            {
                Uid = reader.Int("Uid"),
                ID = reader.String("ID"),
                Name = reader.String("Name")
            };
        }
    }
}
