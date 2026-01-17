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
