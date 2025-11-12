using System.Collections.Generic;

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
    public class TableSimulationGrowth : DefaultTable<StruckTableSimulationGrowth>
    {
        public override string Key => ConfigAddressableTable.SimulationGrowth;
        protected override StruckTableSimulationGrowth BuildRow(Dictionary<string, string> data)
        {
            if (data == null) return null;
            return new StruckTableSimulationGrowth
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                ItemUid = MathHelper.ParseInt(data["ItemUid"]),
                GrowthFileName = data["GrowthFileName"],
            };
        }
    }
}