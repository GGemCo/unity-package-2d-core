using System.Collections.Generic;

namespace GGemCo.Scripts
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
    public class TableStatus : DefaultTable
    {
        private readonly Dictionary<string, StruckTableStatus> dictionaryByID =
            new Dictionary<string, StruckTableStatus>();
        protected override void OnLoadedData(Dictionary<string, string> data)
        {
            int uid = int.Parse(data["Uid"]);
            string id = data["ID"];
            
            StruckTableStatus struckTableStatus = GetDataByUid(uid);
            dictionaryByID.TryAdd(id, struckTableStatus);
        }

        public StruckTableStatus GetDataById(string id)
        {
            return dictionaryByID.GetValueOrDefault(id);
        }

        private StruckTableStatus GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableStatus
            {
                Uid = int.Parse(data["Uid"]),
                ID = data["ID"],
                Name = data["Name"],
            };
        }
    }
}