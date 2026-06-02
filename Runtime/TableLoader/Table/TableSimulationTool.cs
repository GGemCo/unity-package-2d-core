using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 상점 판매 테이블 Structure
    /// </summary>
    public class StruckTableSimulationTool
    {
        public int Uid;
        public int ItemUid;
        public string Memo;
        public string DefinitionFileName;
    }
    public class TableSimulationTool : DefaultTable<StruckTableSimulationTool>
    {
        public override string Key => ConfigAddressableTable.SimulationTool;
        protected override StruckTableSimulationTool BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            if (data == null) return null;
            return new StruckTableSimulationTool
            {
                Uid = reader.Int("Uid"),
                ItemUid = reader.Int("ItemUid"),
                DefinitionFileName = reader.String("DefinitionFileName"),
            };
        }
    }
}