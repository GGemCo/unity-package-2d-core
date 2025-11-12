using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 속성 테이블 Structure
    /// </summary>
    public class StruckTableStatus
    {
        public int Uid;
        public string ID;
        public string Name;
    }
    /// <summary>
    /// 속성 테이블
    /// </summary>
    public class TableStatus : DefaultTable<StruckTableStatus>
    {
        public override string Key => ConfigAddressableTable.Status;
        private readonly Dictionary<string, StruckTableStatus> _dictionaryByID =
            new Dictionary<string, StruckTableStatus>();
        protected override void OnLoadedData(StruckTableStatus data)
        {
            string id = data.ID;
            if (LocalizationManager.Instance != null)
            {
                data.Name = LocalizationManager.Instance.GetStatusNameByKey(id);   
            }
            _dictionaryByID.TryAdd(id, data);
        }

        public StruckTableStatus GetDataById(string id)
        {
            return _dictionaryByID.GetValueOrDefault(id);
        }

        protected override StruckTableStatus BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableStatus
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                ID = data["ID"],
                Name = data["Name"],
            };
        }
    }
}