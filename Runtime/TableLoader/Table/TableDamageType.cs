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

        public StruckTableDamageType GetDataById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _byId.GetValueOrDefault(id);
        }

        protected override StruckTableDamageType BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableDamageType
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                ID = data.GetValueOrDefault("ID"),
                Name = data.GetValueOrDefault("Name")
            };
        }
    }
}
