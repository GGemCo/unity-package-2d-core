namespace GGemCo2DCore
{
    /// <summary>
    /// 상점 판매 테이블 Structure
    /// </summary>
    public class StruckTableSimulationGrowth
    {
        public int Uid;
        public int ItemUid;
        public string Memo;
        public string GrowthFileName;
    }
    public class TableSimulationGrowth : DefaultTable
    {
        public StruckTableSimulationGrowth GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableSimulationGrowth
            {
                Uid = int.Parse(data["Uid"]),
                ItemUid = int.Parse(data["ItemUid"]),
                GrowthFileName = data["GrowthFileName"],
            };
        }
    }
}