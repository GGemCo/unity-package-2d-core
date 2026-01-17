using System.Collections.Generic;

namespace GGemCo2DCore
{
    public sealed class StruckTableState
    {
        public int Uid;
        public string ID;
        public string Name;
    }

    public sealed class TableState : DefaultTable<StruckTableState>
    {
        public override string Key => ConfigAddressableTable.State;

        private readonly Dictionary<string, StruckTableState> _byId = new();

        protected override void PreLoad() => _byId.Clear();

        protected override void OnLoadedData(StruckTableState data)
        {
            if (data == null) return;

            if (LocalizationManager.Instance != null && !string.IsNullOrWhiteSpace(data.ID))
                data.Name = LocalizationManager.Instance.GetStatusNameByKey(data.ID);

            if (!string.IsNullOrWhiteSpace(data.ID))
                _byId[data.ID] = data;
        }

        public StruckTableState GetDataById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _byId.GetValueOrDefault(id);
        }

        protected override StruckTableState BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableState
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                ID = data.GetValueOrDefault("ID"),
                Name = data.GetValueOrDefault("Name")
            };
        }
    }
}
